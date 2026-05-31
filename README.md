# Замок Вальдау — веб-сайт

Современный адаптивный туристический портал для средневекового замка Вальдау (пос. Низовье, Калининградская область).

## Стек

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core + SQLite
- Razor Views + partial-компоненты
- SCSS → CSS (`wwwroot/scss/`, сборка в `wwwroot/css/main.css`)
- Vanilla JavaScript + jQuery Validation (формы)
- Bootstrap Utilities (публичный сайт) / Bootstrap (админ-раздел)

## Страницы

| URL | Описание |
|-----|----------|
| `/` | Главная (экскурсии и мероприятия из БД) |
| `/Excursion` | Экскурсии |
| `/Excursion/Details/{id}` | Карточка экскурсии |
| `/Event` | Мероприятия |
| `/Event/Details/{id}` | Карточка мероприятия |
| `/Booking/Create` | Форма записи |
| `/Booking/Success` | Успешная отправка заявки |
| `/Admin` | CRUD мероприятий |
| `/About` | О замке |
| `/Directions` | Как добраться |
| `/Contacts` | Контакты |

Старые URL `/Tours` и `/Events` перенаправляются на новые контроллеры.

## База данных

Строка подключения в `appsettings.json`:

```
Data Source=waldau.db
```

При запуске приложения автоматически применяются миграции и заполняются seed-данные (экскурсии и мероприятия).

Миграции:

```bash
cd WaldauCastle
dotnet ef migrations add InitialCreate --output-dir Data\Migrations
dotnet ef database update
```

## Запуск

```bash
cd WaldauCastle
dotnet run
```

Откройте в браузере адрес из консоли (обычно `http://localhost:5210`).

## Сборка стилей (SCSS)

Если установлен Node.js:

```bash
cd WaldauCastle
npx sass wwwroot/scss/main.scss wwwroot/css/main.css --watch
```

Готовый `main.css` уже включён в репозиторий.

## Структура проекта

```
WaldauCastle/
├── Controllers/       # Home, Excursion, Event, Booking, Admin, About, …
├── Models/            # Booking, Event, Excursion, SiteInfo
├── ViewModels/        # BookingCreateViewModel, HomeIndexViewModel
├── Data/              # ApplicationDbContext, DbInitializer, Migrations
├── Services/          # ExcursionService, EventService, BookingService
├── Views/
│   ├── Shared/        # Layout, Header, Footer, BookingModal
│   ├── Shared/Components/  # ExcursionCard, EventCard
│   ├── Admin/         # CRUD мероприятий
│   ├── Excursion/, Event/, Booking/
└── wwwroot/
    ├── scss/          # Исходники стилей
    ├── css/           # main.css, admin.css
    ├── js/main.js
    └── images/
```

## Функции

- Sticky-шапка с burger-меню на мобильных
- Модальная и полноценная форма бронирования с валидацией
- Сохранение заявок в SQLite
- Администрирование мероприятий (список, создание, редактирование, удаление)
- Fade-in при скролле, parallax на hero
- Обработка ошибок (404 и общие ошибки)

## Контент

Константы сайта: `Models/SiteInfo.cs`.  
Начальные экскурсии и мероприятия: `Data/DbInitializer.cs`.
