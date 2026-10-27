#!/usr/bin/env bash
# Veli Akademisi · sunucu kurulumu
#
# Her adım ayrı çalıştırılır ve çıktısı kontrol edilir (KURULUM.md):
#   sudo bash kurulum.sh hazirlik             1) gerekli paketler, .NET denetimi
#   sudo bash kurulum.sh uygulama [paket.zip] 2) kullanıcı, klasörler, ayar dosyası, servis, uygulama
#   sudo bash kurulum.sh nginx                3) nginx ayarı + önizleme şifresi
#   sudo bash kurulum.sh https                4) HTTPS sertifikası (DNS kaydı açıldıktan sonra)
#   sudo bash kurulum.sh sesler <ses.zip>     5) ses dosyaları
#   sudo bash kurulum.sh sifre                6) yönetici şifresi
#   sudo bash kurulum.sh yedek                7) günlük otomatik yedek
#   sudo bash kurulum.sh guvenlik-duvari      8) güvenlik duvarı (yalnızca SSH, HTTP, HTTPS açık)
#   sudo bash kurulum.sh durum                   her zaman: servis, site ve son günlük satırları
#   sudo bash kurulum.sh veli-ekle "Ad" "Sınıf"  deneme velisi (yönetim sayfası yerine komutla)
#
# Bir adım hata verirse durur ve neyin tutmadığını yazar; sonraki adıma geçilmez.
set -euo pipefail

ALAN="veliakademisi.bep-uda-projeler.com"
KOK="/opt/veliakademisi"                 # uygulama ve sesler
VERI="/var/lib/veliakademisi"            # veritabanı ve oturum anahtarları (servis yalnızca buraya yazar)
AYAR="/etc/veliakademisi"                # ayarlar.env (site adresi, klasörler, şifre özeti)
YEDEKLER="/var/backups/veliakademisi"
KULLANICI="veliakademisi"
SERVIS="veliakademisi"
BURASI="$(cd "$(dirname "$0")" && pwd)"  # bu betiğin klasörü (kurulum.zip'ten çıkan dosyalar)

baslik() { printf '\n=== %s ===\n' "$1"; }
tamam()  { printf '  TAMAM  %s\n' "$1"; }
dur()    { printf '\n  HATA   %s\n  Kurulum bu adımda durdu. Çıktıyı olduğu gibi gönderin.\n' "$1" >&2
           [ -n "${GITHUB_ACTIONS:-}" ] && echo "::error::kurulum.sh: $1"; exit 1; }

[ "$(id -u)" -eq 0 ] || dur "Bu betik yönetici (root) olarak çalışmalı: başına sudo yazın."

dotnet_yolu() {
  local y; y="$(command -v dotnet || true)"
  [ -n "$y" ] || dur ".NET bulunamadı (dotnet komutu yok)."
  readlink -f "$y"
}

# Uygulamanın kendi komutlarını (veli-ekle, sifre-hash) servisin ayarlarıyla ve servisin kullanıcısıyla çalıştırır.
uygulama_komutu() {
  local d; d="$(dotnet_yolu)"
  ( set -a; . "$AYAR/ayarlar.env"; set +a; export HOME="$VERI"
    cd "$KOK/uygulama" && sudo -E -u "$KULLANICI" -- "$d" "$KOK/uygulama/VeliAkademisi.dll" "$@" )
}

ayar_yaz() {  # ayar_yaz ANAHTAR DEĞER → ayarlar.env içinde satırı ekler ya da değiştirir (tek tırnakla)
  local f="$AYAR/ayarlar.env" t; t="$(mktemp)"
  grep -v "^$1=" "$f" > "$t" || true
  printf "%s='%s'\n" "$1" "$2" >> "$t"
  install -m 640 -o root -g "$KULLANICI" "$t" "$f"; rm -f "$t"
}

saglik() {  # site yerelde cevap veriyor mu? (en çok 30 sn bekler)
  for _ in $(seq 1 30); do
    if [ "$(curl -s http://127.0.0.1:5080/ || true)" = "Veli Akademisi çalışıyor." ]; then return 0; fi
    sleep 1
  done
  return 1
}

adim_hazirlik() {
  baslik "1) Hazırlık"
  apt-get update -q
  DEBIAN_FRONTEND=noninteractive apt-get install -y -q unzip sqlite3 curl nginx certbot python3-certbot-nginx openssl
  tamam "paketler kurulu: unzip, sqlite3, curl, nginx, certbot, openssl"
  local d; d="$(dotnet_yolu)"
  tamam ".NET yolu: $d"
  "$d" --list-runtimes | grep -q '^Microsoft.AspNetCore.App 10\.' || dur "ASP.NET Core 10 çalışma ortamı bulunamadı."
  "$d" --list-runtimes | grep '^Microsoft.AspNetCore.App 10\.' | sed 's/^/         /'
  tamam "ASP.NET Core 10 çalışma ortamı var"
  systemctl is-active --quiet nginx || systemctl enable --now nginx   # kapalıysa açılır ve açılışta başlar
  systemctl is-active --quiet nginx && tamam "nginx çalışıyor" || dur "nginx çalışmıyor (başlatılamadı)."
  # 80 numaralı kapıyı (HTTP) nginx'ten başka bir program tutuyorsa site ona gider; kurulum burada durur.
  local kapi; kapi="$(ss -ltnpH '( sport = :80 )' 2>/dev/null | grep -o '("[^"]*"' | tr -d '("' | sort -u | tr '\n' ' ')"
  [ "$(echo "$kapi" | tr -d ' ')" = "nginx" ] && tamam "80 numaralı kapıyı yalnızca nginx kullanıyor" \
    || dur "80 numaralı kapıyı kullanan programlar: '${kapi:-hiçbiri}' (yalnızca nginx olmalı)."
}

adim_uygulama() {
  baslik "2) Uygulama"
  id "$KULLANICI" >/dev/null 2>&1 || useradd --system --no-create-home --home-dir "$VERI" --shell /usr/sbin/nologin "$KULLANICI"
  tamam "servis kullanıcısı: $KULLANICI (oturum açamaz)"
  install -d -m 755 -o root -g root "$KOK" "$KOK/ses"
  install -d -m 750 -o "$KULLANICI" -g "$KULLANICI" "$VERI"
  install -d -m 750 -o root -g "$KULLANICI" "$AYAR"
  install -d -m 700 -o root -g root "$YEDEKLER"
  tamam "klasörler: $KOK, $VERI, $AYAR, $YEDEKLER"
  if [ ! -f "$AYAR/ayarlar.env" ]; then
    install -m 640 -o root -g "$KULLANICI" /dev/null "$AYAR/ayarlar.env"
    ayar_yaz ASPNETCORE_ENVIRONMENT "Production"
    ayar_yaz ASPNETCORE_URLS "http://127.0.0.1:5080"
    ayar_yaz Site__Adres "https://$ALAN"
    ayar_yaz VeriTabani__Yol "$VERI/veliakademisi.db"
    ayar_yaz Ses__Yol "$KOK/ses"
    tamam "ayar dosyası oluşturuldu: $AYAR/ayarlar.env"
  else
    tamam "ayar dosyası zaten var, dokunulmadı: $AYAR/ayarlar.env"
  fi
  sed "s|@DOTNET@|$(dotnet_yolu)|" "$BURASI/veliakademisi.service" > "/etc/systemd/system/$SERVIS.service"
  install -m 755 "$BURASI/guncelle.sh" /usr/local/sbin/veliakademisi-guncelle
  systemctl daemon-reload
  systemctl enable --quiet "$SERVIS"
  tamam "servis tanımlandı ve açılışta başlayacak: $SERVIS"
  /usr/local/sbin/veliakademisi-guncelle "${1:-}"
}

adim_nginx() {
  baslik "3) nginx ve önizleme şifresi"
  install -m 644 "$BURASI/nginx-veliakademisi-gunluk.conf" /etc/nginx/conf.d/veliakademisi-gunluk.conf
  install -m 644 "$BURASI/nginx-veliakademisi.conf" /etc/nginx/sites-available/veliakademisi
  ln -sf /etc/nginx/sites-available/veliakademisi /etc/nginx/sites-enabled/veliakademisi
  install -m 644 "$BURASI/logrotate-veliakademisi" /etc/logrotate.d/veliakademisi
  local h=/etc/nginx/veliakademisi-onizleme.htpasswd
  if [ ! -f "$h" ]; then
    echo "Önizleme şifresi belirleyin (kullanıcı adı: onizleme). Yazarken ekranda görünmez; iki kez sorulur."
    local o
    if [ -n "${ONIZLEME_SIFRE:-}" ]; then o="$(openssl passwd -apr1 "$ONIZLEME_SIFRE")"; else o="$(openssl passwd -apr1)"; fi
    printf 'onizleme:%s\n' "$o" > "$h"
    chown root:www-data "$h"; chmod 640 "$h"
    tamam "önizleme şifresi kaydedildi"
  else
    tamam "önizleme şifresi zaten var, dokunulmadı"
  fi
  nginx -t 2>&1 | sed 's/^/         /'
  nginx -t >/dev/null 2>&1 || dur "nginx ayarında hata var."
  systemctl reload nginx
  tamam "nginx yeniden yüklendi"
  local k; k="$(curl -s -o /dev/null -w '%{http_code}' -H "Host: $ALAN" http://127.0.0.1/)"
  [ "$k" = "401" ] && tamam "site şifre istiyor (401)" || dur "site şifre istemedi (gelen: $k; cevap: $(curl -s -H "Host: $ALAN" http://127.0.0.1/ | head -c 120 | tr '\n' ' '); etkin siteler: $(ls /etc/nginx/sites-enabled | tr '\n' ' '); nginx.conf içerir: $(grep -E '^\s*include' /etc/nginx/nginx.conf | tr -s ' ' | tr '\n' ' '))."
}

adim_https() {
  baslik "4) HTTPS sertifikası"
  local ad ip
  ad="$(getent ahostsv4 "$ALAN" | awk 'NR==1{print $1}')"
  ip="$(hostname -I | tr ' ' '\n' | grep -E '^[0-9.]+$' | head -1)"
  echo "         $ALAN → ${ad:-(bulunamadı)}   sunucunun adresi: $ip"
  [ -n "$ad" ] || dur "DNS kaydı henüz görünmüyor. Turhost'ta A kaydını açtıktan sonra biraz bekleyip yeniden deneyin."
  [ "$ad" = "$ip" ] || echo "  UYARI  Adresler farklı görünüyor; sunucu bir ağ geçidinin arkasındaysa bu normal olabilir. Certbot denetleyecek."
  certbot --nginx -d "$ALAN" --redirect --agree-tos --register-unsafely-without-email --non-interactive
  tamam "sertifika alındı; HTTP istekleri HTTPS'e yönlendiriliyor"
  systemctl list-timers 2>/dev/null | grep -q certbot && tamam "sertifika kendiliğinden yenilenecek (certbot zamanlayıcısı)" \
    || echo "  UYARI  certbot yenileme zamanlayıcısı görünmüyor; durumu gönderin."
}

adim_sesler() {
  baslik "5) Ses dosyaları"
  local z="${1:-}"; [ -f "$z" ] || dur "Ses paketi bulunamadı: '$z'. Kullanım: sudo bash kurulum.sh sesler /root/Veli_Akademisi_8_Hikaye_Sesli.zip"
  local g; g="$(mktemp -d)"
  unzip -q "$z" 'icerik/*/ses/*' -d "$g" || dur "Paketin içinde icerik/<hikâye>/ses/ klasörleri bulunamadı."
  local kod eksik=0
  for kod in m1 m2a m2b m3 m4 m5 m6 m7; do
    [ -d "$g/icerik/$kod/ses" ] || { echo "  EKSİK  $kod: ses klasörü yok"; eksik=1; continue; }
    [ -f "$g/icerik/$kod/ses/ortam.wav" ] || { echo "  EKSİK  $kod: ortam.wav yok"; eksik=1; }
    install -d -m 755 "$KOK/ses/$kod"
    cp -f "$g/icerik/$kod/ses/"* "$KOK/ses/$kod/"
  done
  rm -rf "$g"
  chmod -R a+rX "$KOK/ses"
  local mp3 wav; mp3="$(find "$KOK/ses" -name '*.mp3' | wc -l)"; wav="$(find "$KOK/ses" -name 'ortam.wav' | wc -l)"
  echo "         mp3: $mp3   ortam.wav: $wav"
  [ "$eksik" -eq 0 ] && [ "$wav" -eq 8 ] || dur "Bazı hikâyelerin sesleri eksik (yukarıda)."
  [ "$mp3" -eq "${BEKLENEN_MP3:-729}" ] || dur "mp3 sayısı ${BEKLENEN_MP3:-729} olmalıydı, $mp3 bulundu."
  tamam "sesler yerinde: $KOK/ses"
}

adim_sifre() {
  baslik "6) Yönetici şifresi"
  echo "Yönetim sayfasının şifresini belirleyin (en az 10 karakter). Yazarken ekranda görünmez; iki kez sorulur."
  local ozet; ozet="$(uygulama_komutu sifre-hash)" || dur "Şifre özeti üretilemedi (yukarıdaki iletiye bakın)."
  [[ "$ozet" == pbkdf2-sha256\$* ]] || dur "Şifre özeti beklenen biçimde değil."
  ayar_yaz Yonetim__SifreHash "$ozet"
  systemctl restart "$SERVIS"
  saglik || dur "Servis yeniden başladıktan sonra site cevap vermedi."
  tamam "şifre kaydedildi (yalnızca özeti), servis yeniden başlatıldı"
}

adim_yedek() {
  baslik "7) Günlük yedek"
  install -m 755 "$BURASI/yedek.sh" /usr/local/sbin/veliakademisi-yedek
  printf '# Veli Akademisi: her gece 03:30 veritabanı yedeği (son 14 gün)\n30 3 * * * root /usr/local/sbin/veliakademisi-yedek\n' \
    > /etc/cron.d/veliakademisi
  chmod 644 /etc/cron.d/veliakademisi
  /usr/local/sbin/veliakademisi-yedek
  ls -l "$YEDEKLER" | tail -n +2 | sed 's/^/         /'
  tamam "yedek alındı; her gece 03:30'da kendiliğinden alınacak, son 14 gün tutulacak"
}

adim_guvenlik_duvari() {
  baslik "8) Güvenlik duvarı"
  command -v ufw >/dev/null || DEBIAN_FRONTEND=noninteractive apt-get install -y -q ufw
  ufw allow OpenSSH >/dev/null
  ufw allow 'Nginx Full' >/dev/null
  ufw --force enable >/dev/null
  ufw status | sed 's/^/         /'
  tamam "yalnızca SSH (22), HTTP (80) ve HTTPS (443) açık"
}

adim_durum() {
  baslik "Durum"
  systemctl is-active "$SERVIS" | sed 's/^/         servis: /'
  [ -f "$KOK/uygulama/SURUM.txt" ] && sed 's/^/         sürüm: /' "$KOK/uygulama/SURUM.txt"
  saglik && tamam "site yerelde cevap veriyor" || echo "  HATA   site yerelde cevap vermiyor"
  echo "         son günlük satırları:"
  journalctl -u "$SERVIS" -n 15 --no-pager 2>/dev/null | sed 's/^/           /' || true
}

adim_veli_ekle() {
  [ $# -eq 2 ] || dur 'Kullanım: sudo bash kurulum.sh veli-ekle "Ad" "Sınıf"'
  uygulama_komutu veli-ekle "$1" "$2"
}

case "${1:-}" in
  hazirlik)        adim_hazirlik ;;
  uygulama)        adim_uygulama "${2:-}" ;;
  nginx)           adim_nginx ;;
  https)           adim_https ;;
  sesler)          adim_sesler "${2:-}" ;;
  sifre)           adim_sifre ;;
  yedek)           adim_yedek ;;
  guvenlik-duvari) adim_guvenlik_duvari ;;
  durum)           adim_durum ;;
  veli-ekle)       shift; adim_veli_ekle "$@" ;;
  *) sed -n '2,17p' "$0"; exit 1 ;;
esac
