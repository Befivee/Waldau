# Замок Вальдау — веб-сайт

Адаптивный сайт музея и туристического объекта «Замок Вальдау» (пос. Низовье, Калининградская область).  
Продакшен: [вальдау.рф](https://вальдау.рф)

## Стек

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core + SQLite (`waldau.db`)
- Razor Views, partial-компоненты
- SCSS → CSS (`wwwroot/scss/` → `wwwroot/css/main.css`)
- Vanilla JavaScript, jQuery Validation (форма записи)
- Telegram Bot API и VK Long Poll API (админ-панель в мессенджерах)
- GitHub Actions — автодеплой на VPS

## Страницы сайта

| URL | Описание |
|-----|----------|
| `/` | Главная: экскурсии и мероприятия из БД, блок «Как добраться» |
| `/Excursion` | Список экскурсий с кнопкой «Записаться» |
| `/Event` | Список мероприятий с кнопкой «Записаться» |
| `/About` | О нас: история замка, блок «Наша семья» (7 карточек команды) |
| `/Directions` | Как добраться |
| `/Contacts` | Контакты |
| `/Privacy` | Политика конфиденциальности |
| `/Booking/Success` | Страница после успешной отправки (не модальная форма) |
| `/health` | Проверка работоспособности приложения |
| `/sitemap.xml` | Карта сайта |

Запись на экскурсию — **модальное окно** на сайте (AJAX). Отдельных страниц `/Excursion/Details` и `/Event/Details` нет: вся информация на списках.

Старые URL `/Tours` и `/Events` перенаправляются на `/Excursion` и `/Event`.

## База данных

SQLite, файл `waldau.db` рядом с приложением.

**Таблицы:** `Bookings`, `Events`, `Excursions` (без связей между собой).

При старте приложения автоматически применяются миграции (`DbInitializer.SeedAsync`). Seed-данные создаются только если таблица экскурсий пуста.

Строка подключения в `appsettings.json`:

```
Data Source=waldau.db
```

Новая миграция (локально):

```bash
cd WaldauCastle
dotnet ef migrations add ИмяМиграции --output-dir Data/Migrations
```

На сервере отдельно `dotnet ef database update` обычно не нужен — миграции накатываются при запуске.

## Запуск локально

```bash
cd WaldauCastle
dotnet run
```

Откройте адрес из консоли (обычно `http://localhost:5210`).

Telegram- и VK-боты включаются только при корректных настройках в конфигурации (см. ниже).

## Конфигурация и секреты

**Локально / в репозитории** — только пустые заглушки в `appsettings.json`:

- `Telegram:BotToken`, `AdminChatId`, `SecondAdminChatId`
- `VK:AccessToken`, `GroupId`, `AdminUserId`

**На продакшене** секреты задаются в `/etc/waldau.env` (не в GitHub):

```
Telegram__BotToken=...
Telegram__AdminChatId=...
Telegram__SecondAdminChatId=...
VK__AccessToken=...
VK__GroupId=...
VK__AdminUserId=...
```

После изменения: `sudo systemctl restart waldau`.

GitHub Secrets (`SSH_KEY`, `SSH_HOST`, `SSH_USER`) используются **только для деплоя**, не для runtime-приложения.

## Администрирование

### Telegram- и VK-боты

Админы управляют контентом через ботов (reply-клавиатура под полем ввода):

- **Заявки** — просмотр, пагинация, удаление; автоудаление после даты визита (MSK)
- **Мероприятия** — CRUD, загрузка изображения (или `-` для стандартного)
- **Экскурсии** — CRUD, цена, длительность, изображение (или `-` для стандартного)
- **Статистика**

Уведомления о новых заявках отправляются всем admin chat id (основной и второй Telegram-админ).

### Веб-админка `/Admin`

CRUD мероприятий через браузер (дополнительно к ботам).

## Деплой

### Автоматически (рекомендуется)

Push в ветку `main` → GitHub Actions (`.github/workflows/deploy.yml`):

1. `dotnet publish`
2. `scp` в `/var/www/waldau`
3. `systemctl restart waldau`

```bash
git add .
git commit -m "Описание изменений"
git push origin main
```

Статус: вкладка **Actions** в репозитории [Befivee/Waldau](https://github.com/Befivee/Waldau).

### Вручную с Windows

```powershell
cd WaldauCastle
.\deploy.ps1
```

Или по шагам:

```powershell
dotnet publish WaldauCastle.csproj -c Release -o publish
scp -r publish\* root@188.225.45.211:/var/www/waldau/
ssh root@188.225.45.211 "systemctl restart waldau"
```

**Важно:** `dotnet publish` выполняется **локально** (нужен .NET SDK). На сервере только runtime.

При деплое **не перезаписываются:** `waldau.db`, `/etc/waldau.env`, загруженные файлы в `wwwroot/uploads/`.

## SCSS

Если установлен Node.js:

```bash
cd WaldauCastle
npx sass wwwroot/scss/main.scss wwwroot/css/main.css --watch
```

Готовый `main.css` уже в репозитории — для деплоя пересборка не обязательна.

## Структура проекта

```
WaldauCastle/
├── Controllers/          # Home, Excursion, Event, Booking, About, Privacy, …
├── Models/               # Booking, Event, Excursion, SiteInfo
├── ViewModels/
├── Data/                 # ApplicationDbContext, DbInitializer, Migrations
├── Options/              # TelegramBotOptions, VKOptions, SiteSettings
├── Services/
│   ├── Bot/              # Общая логика ботов (пагинация, callback, тексты)
│   ├── Telegram/         # Telegram-бот
│   ├── VK/               # VK-бот
│   ├── BookingService, EventService, ExcursionService, …
│   └── EventImageService # Загрузка изображений (events/, excursions/)
├── Views/
│   ├── Shared/           # Layout, Header, Footer, BookingModal
│   ├── Shared/Components/# _ExcursionCard, _EventCard
│   ├── About/            # О нас, «Наша семья»
│   ├── Admin/            # CRUD мероприятий (веб)
│   └── Excursion/, Event/, Booking/, …
├── wwwroot/
│   ├── scss/, css/, js/
│   ├── images/
│   └── uploads/          # events/, excursions/ (на сервере)
├── .github/workflows/    # deploy.yml
└── deploy.ps1            # Ручной деплой
```

## Основные возможности

- Sticky-шапка, burger-меню на мобильных
- Модальная форма записи с валидацией телефона (+7), согласием на ПДн, AJAX без перезагрузки страницы
- Уведомления о заявках в Telegram (нескольким админам)
- Карточки экскурсий и мероприятий без отдельных detail-страниц
- Изображения мероприятий и экскурсий (боты + веб-админка для событий)
- SEO: meta-теги, `sitemap.xml`, `robots.txt`
- Обработка 404 и серверных ошибок

## Контент

- Константы сайта: `Models/SiteInfo.cs`
- Начальные экскурсии и мероприятия: `Data/DbInitializer.cs`
- Карточки команды на странице «О нас»: `Views/About/Index.cshtml`
