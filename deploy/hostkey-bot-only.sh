#!/bin/bash
# Hostkey web console: Waldau Telegram/VK bots only (no public website).
set -e
if [ "$(id -u)" -ne 0 ]; then
  echo "Run as root" >&2
  exit 1
fi

SECRET=$(python3 -c 'import secrets; print(secrets.token_hex(16))')

mkdir -p /etc
touch /etc/waldau.env
chmod 600 /etc/waldau.env

append_kv() {
  local file="$1" key="$2" value="$3"
  grep -v "^${key}=" "$file" > "${file}.tmp" || true
  mv "${file}.tmp" "$file"
  echo "${key}=${value}" >> "$file"
}

append_kv /etc/waldau.env Telegram__BotOnly true
append_kv /etc/waldau.env Telegram__AcceptRelay true
append_kv /etc/waldau.env Telegram__DisablePolling false
append_kv /etc/waldau.env Telegram__RelaySecret "$SECRET"
sed -i '/Telegram__ProxyUrl=/d' /etc/waldau.env || true
sed -i '/^Telegram__RelayUrl=/d' /etc/waldau.env || true

if [ -f /etc/systemd/system/waldau.service ]; then
  mkdir -p /etc/systemd/system/waldau.service.d
  printf '[Service]\nEnvironment=ASPNETCORE_URLS=http://127.0.0.1:5000\n' > /etc/systemd/system/waldau.service.d/bot-only.conf
fi

systemctl daemon-reload
systemctl restart waldau || true

if ! command -v cloudflared >/dev/null 2>&1; then
  curl -fsSL -o /tmp/cloudflared.deb https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
  dpkg -i /tmp/cloudflared.deb || apt-get -y -f install
fi

cat >/etc/systemd/system/cloudflared-waldau.service <<'EOF'
[Unit]
Description=Cloudflare tunnel to Waldau Telegram bot
After=network-online.target waldau.service
Wants=network-online.target

[Service]
ExecStart=/usr/bin/cloudflared tunnel --no-autoupdate --url http://127.0.0.1:5000
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl enable --now cloudflared-waldau
sleep 3
echo "==== RELAY_SECRET ===="
echo "$SECRET"
echo "==== TUNNEL URL (ищите trycloudflare.com) ===="
journalctl -u cloudflared-waldau -n 40 --no-pager || true
echo WALDAU_BOT_ONLY_OK
