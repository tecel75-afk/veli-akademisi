#!/usr/bin/env bash
# Kurulum provası: deploy/kurulum.sh, yöneticinin sunucuda çalıştıracağı adımlarla burada (GitHub'ın Ubuntu
# makinesinde) baştan sona çalıştırılır. Yalnızca HTTPS (gerçek alan adı ister) ve güvenlik duvarı denenmez.
# Kullanım: sudo env "PATH=$PATH" bash .github/kurulum-provasi.sh <veli-akademisi.zip>
set -euo pipefail
trap 'echo "::error::Kurulum provası $LINENO. satırda durdu (komut: $BASH_COMMAND)"' ERR
PAKET="$(readlink -f "$1")"
K="bash $(dirname "$0")/../deploy/kurulum.sh"
ALAN="veliakademisi.bep-uda-projeler.com"
A=(-H "Host: $ALAN")                 # nginx'e gerçek alan adıyla gelinmiş gibi
O=(-u "onizleme:prova-onizleme-1")    # önizleme şifresi
hata=0
kontrol() { if [ "$2" == "$3" ]; then echo "  TAMAM  $1"; else echo "  HATA   $1 — beklenen: [$2], gelen: [$3]"; echo "::error::Kurulum: $1 — beklenen: [$2], gelen: [$3]"; hata=1; fi; }
kod() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
gq() { grep "$@" > /dev/null; }   # boru içinde arama: bütün girdiyi okur (grep -q + pipefail yanlış "hayır" verebilir)
# yon <curl seçenekleri> → "<durum kodu> <Location başlığı>" (site göreli adresle yönlendirir; tarayıcı kendi alan adını ekler)
yon() { curl -s -o /dev/null -D - "$@" | tr -d '\r' | awk 'NR==1{k=$2} tolower($1)=="location:"{l=$2} END{print k" "l}'; }

$K hazirlik
$K uygulama "$PAKET"
kontrol "servis çalışıyor" active "$(systemctl is-active veliakademisi)"
kontrol "servis kendi kullanıcısıyla çalışıyor" veliakademisi "$(ps -o user= -p "$(systemctl show -p MainPID --value veliakademisi)" | tr -d ' ')"
ONIZLEME_SIFRE="prova-onizleme-1" $K nginx
kontrol "şifreyle ana sayfa" "Veli Akademisi çalışıyor." "$(curl -s "${A[@]}" "${O[@]}" http://127.0.0.1/)"
kontrol "yanlış önizleme şifresi → 401" 401 "$(kod "${A[@]}" -u onizleme:yanlis http://127.0.0.1/)"

printf 'Prova Yönetici\nprova\nprova-yonetici-123\n' | $K yonetici
OKUL="$($K okul-ekle "Prova Okulu" | sed -n 's/^Okul no: //p')"
kontrol "okul eklendi" 1 "$OKUL"
KOD="$($K veli-ekle "Prova Veli" "1-A" "$OKUL" | sed -n 's/^Kod: //p')"
kontrol "veli eklendi (22 karakterlik kod)" 22 "${#KOD}"
# Teşhis: servisin gerçekte gördüğü ayarlar ve veritabanı dosyalarının yerleri
PID="$(systemctl show -p MainPID --value veliakademisi)"
echo "::notice::Servisin ortamı: $(tr '\0' '\n' < "/proc/$PID/environ" | grep -E '^(VeriTabani|Ses|Site|ASPNETCORE)' | tr '\n' ' ')"
echo "::notice::Ayar dosyası: $(tr '\n' ' ' < /etc/veliakademisi/ayarlar.env)"
echo "::notice::Veritabanı dosyaları: $(find / -xdev -name 'veliakademisi.db' 2>/dev/null | tr '\n' ' ')"
kontrol "veritabanı servis kullanıcısına ait" veliakademisi "$(stat -c %U /var/lib/veliakademisi/veliakademisi.db)"
kontrol "veli bağlantısı (şifreyle) → Modül 1" "302 /v/$KOD/m1/" "$(yon "${A[@]}" "${O[@]}" "http://127.0.0.1/v/$KOD")"
kontrol "veli bağlantısı şifresiz açılmaz (önizleme)" 401 "$(kod "${A[@]}" "http://127.0.0.1/v/$KOD/m1/")"
kontrol "uygulama tanımı şifresiz alınabiliyor (ana ekrana ekleme için)" 200 "$(kod "${A[@]}" "http://127.0.0.1/v/$KOD/manifest.webmanifest")"
kontrol "simge şifresiz alınabiliyor" 200 "$(kod "${A[@]}" http://127.0.0.1/simge/simge-192.png)"

# Sistem yöneticisi girişi nginx ve servis üzerinden (hesap, servisin veritabanına komutla açıldı).
C="$(mktemp)"
T="$(curl -s -c "$C" -b "$C" "${A[@]}" "${O[@]}" http://127.0.0.1/yonetim/giris | python3 -c "
import re,sys; m=re.search(r'name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"', sys.stdin.read()); print(m.group(1) if m else '')")"
kontrol "sistem yöneticisi girişi" "302 /yonetim/sistem" \
  "$(yon -c "$C" -b "$C" "${A[@]}" "${O[@]}" -X POST --data-urlencode "KullaniciAdi=prova" \
     --data-urlencode "Sifre=prova-yonetici-123" --data-urlencode "__RequestVerificationToken=$T" http://127.0.0.1/yonetim/giris)"
kontrol "giriş sonrası sistem sayfası" 200 "$(kod -b "$C" "${A[@]}" "${O[@]}" http://127.0.0.1/yonetim/sistem)"

# Sesler: küçük bir deneme paketi (8 hikâyenin ortam.wav'ı + bir mp3)
S="$(mktemp -d)"
for h in m1 m2a m2b m3 m4 m5 m6 m7; do
  mkdir -p "$S/icerik/$h/ses"
  python3 -c "import wave,sys; w=wave.open(sys.argv[1],'wb'); w.setnchannels(1); w.setsampwidth(2); w.setframerate(8000); w.writeframes(b'\0\0'*800); w.close()" "$S/icerik/$h/ses/ortam.wav"
done
printf 'ID3deneme' > "$S/icerik/m1/ses/m1_s001.mp3"
(cd "$S" && zip -qr ses.zip icerik)
BEKLENEN_MP3=1 $K sesler "$S/ses.zip"
kontrol "ses dosyası site üzerinden veriliyor" "200 audio/mpeg" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "${A[@]}" "${O[@]}" "http://127.0.0.1/v/$KOD/m1/ses/m1_s001.mp3")"
kontrol "eksik ses paketi reddedilir" 1 "$(mkdir -p "$S/eksik/icerik/m1/ses" && printf x > "$S/eksik/icerik/m1/ses/m1_s001.mp3" && (cd "$S/eksik" && zip -qr ../eksik.zip icerik) && $K sesler "$S/eksik.zip" >/dev/null 2>&1; echo $?)"

# Ses güncelleme: yalnızca var olan dosyalar değişir; hatalı bir dosya varsa hiçbiri değişmez; eskiler yedeklenir.
mkdir -p "$S/guncel/alt" && printf 'ID3yeni' > "$S/guncel/alt/m1_s001.mp3" && (cd "$S/guncel" && zip -qr ../guncel.zip alt)
mkdir -p "$S/hatali" && printf 'ID3hatali' > "$S/hatali/m1_s001.mp3" && printf 'x' > "$S/hatali/m9_s001.mp3" && (cd "$S/hatali" && zip -qr ../hatali.zip .)
SES_ONCE="$(cat /opt/veliakademisi/ses/m1/m1_s001.mp3)"
kontrol "ses güncelleme: hatalı adlı dosya varsa durur" evet "$($K ses-guncelle "$S/hatali.zip" >/dev/null 2>&1 && echo hayır || echo evet)"
kontrol "  hiçbir dosya değişmedi" "$SES_ONCE" "$(cat /opt/veliakademisi/ses/m1/m1_s001.mp3)"
$K ses-guncelle "$S/guncel.zip"
kontrol "ses güncelleme: dosya yenilendi" "ID3yeni" "$(cat /opt/veliakademisi/ses/m1/m1_s001.mp3)"
kontrol "  eski hâli yedeklendi" "$SES_ONCE" "$(cat /var/backups/veliakademisi/ses-*/m1/m1_s001.mp3)"
kontrol "  site yeni sesi veriyor" "ID3yeni" "$(curl -s "${A[@]}" "${O[@]}" "http://127.0.0.1/v/$KOD/m1/ses/m1_s001.mp3")"

$K yedek
kontrol "yedek dosyası alındı" 1 "$(ls /var/backups/veliakademisi/veliakademisi-*.db | wc -l)"
kontrol "yedek geçerli (veli sayısı 1)" 1 "$(sqlite3 /var/backups/veliakademisi/veliakademisi-*.db 'select count(*) from Veliler')"

GUNLUK="$(cat /var/log/nginx/veliakademisi-erisim.gunluk)"
kontrol "günlükte IP kısaltılmış (127.0.0.0)" evet "$(echo "$GUNLUK" | gq '^127\.0\.0\.0 ' && echo evet || echo hayır)"
kontrol "günlükte veli kodu yok (/v/***)" "evet|hayır" "$(echo "$GUNLUK" | gq '/v/\*\*\*' && echo evet || echo hayır)|$(echo "$GUNLUK" | gq "$KOD" && echo evet || echo hayır)"

# Güncelleme: aynı paket yeniden kurulur; önceki sürüm saklanır, veriler korunur.
/usr/local/sbin/veliakademisi-guncelle "$PAKET"
kontrol "güncellemeden sonra önceki sürüm saklandı" evet "$([ -f /opt/veliakademisi/onceki/VeliAkademisi.dll ] && echo evet || echo hayır)"
kontrol "güncellemeden sonra veriler yerinde" 22 "$(KOD2="$(sqlite3 /var/lib/veliakademisi/veliakademisi.db 'select Kod from Veliler')"; echo ${#KOD2})"
kontrol "güncellemeden sonra oturum sürüyor (anahtarlar korunmuş)" 200 "$(kod -b "$C" "${A[@]}" "${O[@]}" http://127.0.0.1/yonetim/sistem)"
# Bozuk paket: kurulmamalı, eski sürüm çalışmaya devam etmeli
printf 'bozuk' > "$S/bozuk.zip"
kontrol "bozuk paket kurulmaz (hata koduyla durur)" evet "$(/usr/local/sbin/veliakademisi-guncelle "$S/bozuk.zip" >/dev/null 2>&1 && echo hayır || echo evet)"
kontrol "bozuk paketten sonra site çalışıyor" "Veli Akademisi çalışıyor." "$(curl -s http://127.0.0.1:5080/)"

$K durum
[ $hata -eq 0 ] || { echo "SONUÇ: kurulum provasında tutmayan kontroller var."; exit 1; }
echo "SONUÇ: kurulum provası başarılı."
