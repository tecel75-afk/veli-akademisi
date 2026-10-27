#!/usr/bin/env bash
# Veli Akademisi · uygulamayı günceller (ilk kurulumda da bu kullanılır).
#   sudo veliakademisi-guncelle              → GitHub'daki son sürümü indirip kurar
#   sudo veliakademisi-guncelle paket.zip    → verilen paketi kurar
# Yeni sürüm açılmazsa önceki sürüme kendiliğinden geri döner. Veriler ve sesler etkilenmez.
set -euo pipefail

PAKET_ADRESI="https://github.com/tecel75-afk/veli-akademisi/releases/download/son/veli-akademisi.zip"
KOK="/opt/veliakademisi"
SERVIS="veliakademisi"

[ "$(id -u)" -eq 0 ] || { echo "Yönetici (root) olarak çalışmalı: başına sudo yazın." >&2; exit 1; }

# Site cevap veriyor mu ve cevabı gerçekten bu servis mi veriyor? (5080'i başka bir program tutuyorsa yanıltmasın.)
saglik() {
  for _ in $(seq 1 30); do
    local pid; pid="$(systemctl show -p MainPID --value "$SERVIS")"
    if [ "$pid" != "0" ] && ss -ltnpH '( sport = :5080 )' | grep -q "pid=$pid," \
       && [ "$(curl -s http://127.0.0.1:5080/ || true)" = "Veli Akademisi çalışıyor." ]; then return 0; fi
    sleep 1
  done
  return 1
}

# 5080 numaralı kapı başka bir program tarafından tutuluyorsa durulur.
dolu="$(ss -ltnpH '( sport = :5080 )' || true)"
if [ -n "$dolu" ] && ! systemctl is-active --quiet "$SERVIS"; then
  echo "  HATA   5080 numaralı kapıyı başka bir program kullanıyor: $dolu" >&2
  exit 1
fi

g="$(mktemp -d)"; trap 'rm -rf "$g"' EXIT
if [ -n "${1:-}" ]; then
  cp "$1" "$g/paket.zip"
  echo "  paket: $1"
else
  echo "  son sürüm indiriliyor: $PAKET_ADRESI"
  curl -fsSL --retry 3 -o "$g/paket.zip" "$PAKET_ADRESI"
fi
unzip -q "$g/paket.zip" -d "$g/yeni"
[ -f "$g/yeni/VeliAkademisi.dll" ] || { echo "  HATA   paket beklenen dosyaları içermiyor." >&2; exit 1; }
[ -f "$g/yeni/SURUM.txt" ] && sed 's/^/  sürüm: /' "$g/yeni/SURUM.txt"

systemctl stop "$SERVIS" 2>/dev/null || true
rm -rf "$KOK/onceki"
[ -d "$KOK/uygulama" ] && mv "$KOK/uygulama" "$KOK/onceki"
mv "$g/yeni" "$KOK/uygulama"
chown -R root:root "$KOK/uygulama"; chmod -R a+rX "$KOK/uygulama"
systemctl start "$SERVIS"

if saglik; then
  echo "  TAMAM  uygulama kuruldu ve cevap veriyor"
else
  echo "  HATA   yeni sürüm cevap vermedi; önceki sürüme dönülüyor." >&2
  journalctl -u "$SERVIS" -n 20 --no-pager >&2 || true
  if [ -d "$KOK/onceki" ]; then
    systemctl stop "$SERVIS" || true
    rm -rf "$KOK/uygulama"; mv "$KOK/onceki" "$KOK/uygulama"
    systemctl start "$SERVIS"
    saglik && echo "  önceki sürüm yeniden çalışıyor." >&2
  fi
  exit 1
fi
