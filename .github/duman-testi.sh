#!/usr/bin/env bash
# Duman testi: derlenen siteyi gerçekten çalıştırır ve temel adresleri dener.
# Herhangi bir kontrol tutmazsa betik hata koduyla biter ve GitHub derlemesi "başarısız" olur.
# Kullanım: .github/duman-testi.sh <derlenmiş klasör>
set -euo pipefail

YAYIN="$1"
GECICI="$(mktemp -d)"
export VeriTabani__Yol="$GECICI/veri/veliakademisi.db"
export Ses__Yol="$GECICI/ses"
export ASPNETCORE_URLS="http://127.0.0.1:5080"
ADRES="http://127.0.0.1:5080"

hata=0
kontrol() {  # kontrol "<açıklama>" <beklenen> <gerçek>
  if [ "$2" == "$3" ]; then echo "  TAMAM  $1"; else echo "  HATA   $1 — beklenen: [$2], gelen: [$3]"; hata=1; fi
}
durum() { curl -s -o /dev/null -w "%{http_code}" "$1"; }
yonlendirme() { curl -s -o /dev/null -w "%{http_code} %{redirect_url}" "$1"; }

echo "1) Deneme velisi ekleniyor (veli-ekle komutu)"
CIKTI="$(dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle "Deneme Veli" "1-A")"
echo "$CIKTI" | sed 's/^/     /'
KOD="$(echo "$CIKTI" | sed -n 's/^Kod: //p')"
kontrol "kod 22 karakter" 22 "${#KOD}"

echo "2) Sahte bir ses dosyası konuyor (ses klasöründen sunulduğunu görmek için)"
mkdir -p "$Ses__Yol/m1" && printf 'ID3deneme' > "$Ses__Yol/m1/m1_s001.mp3"

echo "3) Site başlatılıyor"
dotnet "$YAYIN/VeliAkademisi.dll" > "$GECICI/site.log" 2>&1 &
for i in $(seq 1 30); do curl -s "$ADRES/" > /dev/null && break; sleep 1; done

echo "4) Kontroller"
kontrol "ana sayfa yazısı" "Veli Akademisi çalışıyor." "$(curl -s "$ADRES/")"
TABLOLAR="$(python3 -c "import sqlite3,sys; print(','.join(sorted(r[0] for r in sqlite3.connect(sys.argv[1]).execute(\"select name from sqlite_master where type='table' and name not like 'sqlite_%'\"))))" "$VeriTabani__Yol")"
kontrol "veritabanı tabloları" "Cevaplar,Ilerlemeler,Veliler" "$TABLOLAR"

kontrol "/v/{kod} → Modül 1'e yönlendirme" "302 $ADRES/v/$KOD/m1/" "$(yonlendirme "$ADRES/v/$KOD")"
kontrol "/v/{kod}/m1 → sonuna / eklenerek yönlendirme" "302 $ADRES/v/$KOD/m1/" "$(yonlendirme "$ADRES/v/$KOD/m1")"
for h in m1 m2a m2b m3 m4 m5 m6 m7; do
  kontrol "hikâye sayfası açılıyor: $h" 200 "$(durum "$ADRES/v/$KOD/$h/")"
done
grep -q "Tablet Kapanınca" <(curl -s "$ADRES/v/$KOD/m1/") && kontrol "m1 sayfasının içeriği doğru" evet evet || kontrol "m1 sayfasının içeriği doğru" evet hayır

BASLIKLAR="$(curl -s -D - -o /dev/null "$ADRES/v/$KOD/m1/" | tr -d '\r')"
kontrol "X-Robots-Tag başlığı" "x-robots-tag: noindex, nofollow" "$(echo "$BASLIKLAR" | grep -i '^x-robots-tag:' | tr 'A-Z' 'a-z')"
kontrol "Referrer-Policy başlığı" "referrer-policy: no-referrer" "$(echo "$BASLIKLAR" | grep -i '^referrer-policy:' | tr 'A-Z' 'a-z')"

kontrol "görsel veriliyor" "200 image/webp" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$ADRES/v/$KOD/m1/img/m1_b1_kare1.webp")"
kontrol "ses ayrı klasörden veriliyor" "200 audio/mpeg" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$ADRES/v/$KOD/m1/ses/m1_s001.mp3")"
kontrol "olmayan ses dosyası → 404" 404 "$(durum "$ADRES/v/$KOD/m1/ses/m1_s999.mp3")"
kontrol "olmayan hikâye → 404" 404 "$(durum "$ADRES/v/$KOD/m9/")"
kontrol "klasör atlama denemesi → 404" 404 "$(durum "$ADRES/v/$KOD/m1/img/..%2F..%2Fsteps.json")"

GECERSIZ="AAAAAAAAAAAAAAAAAAAAAA"
kontrol "geçersiz kod → 404" 404 "$(durum "$ADRES/v/$GECERSIZ")"
grep -q "Bu bağlantı artık geçerli değil" <(curl -s "$ADRES/v/$GECERSIZ") && kontrol "geçersiz kod mesajı" evet evet || kontrol "geçersiz kod mesajı" evet hayır
kontrol "biçimi bozuk kod → 404" 404 "$(durum "$ADRES/v/kisa/m1/")"
kontrol "geçersiz kodla görsel → 404" 404 "$(durum "$ADRES/v/$GECERSIZ/m1/img/m1_b1_kare1.webp")"

SONGIRIS="$(python3 -c "import sqlite3,sys; print(sqlite3.connect(sys.argv[1]).execute('select SonGiris is not null from Veliler').fetchone()[0])" "$VeriTabani__Yol")"
kontrol "bağlantı açılınca SonGiris dolduruldu" 1 "$SONGIRIS"

echo "5) Site günlüğü"
sed 's/^/     /' "$GECICI/site.log"

if [ $hata -ne 0 ]; then echo "SONUÇ: kontrollerden en az biri tutmadı."; exit 1; fi
echo "SONUÇ: bütün kontroller tuttu."
