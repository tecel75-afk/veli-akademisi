#!/usr/bin/env bash
# Veli Akademisi · veritabanı yedeği (her gece cron ile; elle de çalıştırılabilir)
# SQLite'ın kendi yedekleme komutuyla alınır (site çalışırken bile tutarlı kopya; tanım 16.4). Son 14 gün tutulur.
set -euo pipefail
DB="/var/lib/veliakademisi/veliakademisi.db"
HEDEF="/var/backups/veliakademisi"
[ -f "$DB" ] || { echo "Veritabanı yok: $DB" >&2; exit 1; }
install -d -m 700 "$HEDEF"
sqlite3 "$DB" ".backup '$HEDEF/veliakademisi-$(date +%F).db'"
chmod 600 "$HEDEF"/veliakademisi-*.db
find "$HEDEF" -name 'veliakademisi-*.db' -mtime +13 -delete
