#!/bin/bash
# Hostkey console: enable Waldau booking relay (same secret as Izotoff) and install latest bot build.
set -e
if [ "$(id -u)" -ne 0 ]; then
  echo "Run as root" >&2
  exit 1
fi

if [ ! -f /etc/izotoff.env ]; then
  echo "NO /etc/izotoff.env" >&2
  exit 1
fi

SECRET=$(python3 - <<'PY'
from pathlib import Path
for line in Path("/etc/izotoff.env").read_text(encoding="utf-8", errors="replace").splitlines():
    if line.startswith("Telegram__RelaySecret="):
        print(line.split("=", 1)[1].strip())
        break
PY
)
if [ -z "$SECRET" ]; then
  echo "NO Telegram__RelaySecret in /etc/izotoff.env" >&2
  exit 1
fi

append_kv() {
  local file="$1" key="$2" value="$3"
  grep -v "^${key}=" "$file" > "${file}.tmp" || true
  mv "${file}.tmp" "$file"
  echo "${key}=${value}" >> "$file"
}

touch /etc/waldau.env
chmod 600 /etc/waldau.env
append_kv /etc/waldau.env Telegram__BotOnly true
append_kv /etc/waldau.env Telegram__AcceptRelay true
append_kv /etc/waldau.env Telegram__DisablePolling false
append_kv /etc/waldau.env Telegram__RelaySecret "$SECRET"
sed -i '/^Telegram__RelayUrl=/d' /etc/waldau.env || true
sed -i '/Telegram__ProxyUrl=/d' /etc/waldau.env || true

mkdir -p /etc/systemd/system/waldau.service.d
printf '[Service]\nEnvironment=ASPNETCORE_URLS=http://127.0.0.1:5000\n' > /etc/systemd/system/waldau.service.d/bot-only.conf

curl -fsSL -o /tmp/waldau-bot.tgz "https://github.com/Befivee/Waldau/releases/download/waldau-bot/waldau-bot.tgz"
mkdir -p /tmp/waldau-bot-unpack /var/www/waldau
tar -xzf /tmp/waldau-bot.tgz -C /tmp/waldau-bot-unpack
rsync -a --exclude 'waldau.db' --exclude 'waldau.db-*' --exclude 'App_Data/' --exclude 'uploads/' \
  /tmp/waldau-bot-unpack/ /var/www/waldau/

curl -fsSL -o /tmp/izotoff-bot.tgz "https://github.com/Befivee/Izotoff/releases/download/izotoff-bot/izotoff-bot.tgz"
mkdir -p /tmp/izotoff-bot-unpack /var/www/izotoff
tar -xzf /tmp/izotoff-bot.tgz -C /tmp/izotoff-bot-unpack
rsync -a --exclude 'izotoff.db' --exclude 'izotoff.db-*' --exclude 'App_Data/' --exclude 'uploads/' \
  /tmp/izotoff-bot-unpack/ /var/www/izotoff/

systemctl daemon-reload
systemctl restart waldau
systemctl restart izotoff
sleep 2
journalctl -u waldau -n 25 --no-pager | grep -iE 'Telegram|VK-бот|relay|ошибк|Accept' || true
echo WALDAU_RELAY_OK
