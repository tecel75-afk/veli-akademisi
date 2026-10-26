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

echo "5) Kayıt kancası ve kayıt uçları"
kontrol "kanca.js veriliyor" "200 javascript" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$ADRES/kanca.js" | sed -E 's/ .*javascript.*/ javascript/')"
SAYFA="$GECICI/m1_sunulan.html"; curl -s "$ADRES/v/$KOD/m1/" > "$SAYFA"
kontrol "sayfanın sonunda kanca etiketi var" '<script src="/kanca.js"></script>' "$(tail -n 1 "$SAYFA")"
# Kanca etiketinden önceki kısım, depodaki onaylı dosyayla birebir aynı olmalı.
head -c "$(stat -c %s "$YAYIN/icerik/m1/index.html")" "$SAYFA" | cmp -s - "$YAYIN/icerik/m1/index.html" \
  && kontrol "onaylı sayfa değişmeden sunuluyor" evet evet || kontrol "onaylı sayfa değişmeden sunuluyor" evet hayır

# steps.json'dan m1'in toplam adımı, ilk sorusu ve ilk düşünme sorusu
read -r TOPLAM SORU DUSUNME < <(python3 -c "
import json,sys; s=json.load(open(sys.argv[1],encoding='utf-8'))
print(len(s), next(k for k,x in enumerate(s) if x['t']=='q'), next(k for k,x in enumerate(s) if x['t']=='think'))" "$YAYIN/icerik/m1/steps.json")
echo "     m1: toplam $TOPLAM adım, ilk soru adım $SORU, ilk düşünme sorusu adım $DUSUNME"

post() { curl -s -o /dev/null -w "%{http_code}" -X POST -H 'Content-Type: application/json' -d "$2" "$ADRES$1"; }
satir() { python3 -c "import sqlite3,sys; r=sqlite3.connect(sys.argv[1]).execute(sys.argv[2]).fetchone(); print('|'.join(str(x) for x in r) if r else 'yok')" "$VeriTabani__Yol" "$1"; }
ILR="select EnIleriAdim,SonAdim,Tamamlandi from Ilerlemeler where Hikaye='m1'"

kontrol "ilerleme: adım 5 kaydedildi (204)" 204 "$(post /api/ilerleme "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":5}")"
kontrol "  veritabanında: en ileri 5, son 5, tamamlanmadı" "5|5|0" "$(satir "$ILR")"
kontrol "ilerleme: geri dönüş, adım 2 (204)" 204 "$(post /api/ilerleme "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":2}")"
kontrol "  veritabanında: en ileri 5 kaldı, son 2" "5|2|0" "$(satir "$ILR")"
kontrol "ilerleme: var olmayan adım → 400" 400 "$(post /api/ilerleme "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":$TOPLAM}")"
kontrol "ilerleme: eksi adım → 400" 400 "$(post /api/ilerleme "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":-1}")"
kontrol "ilerleme: kapanış adımı (204)" 204 "$(post /api/ilerleme "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":$((TOPLAM-1))}")"
kontrol "  veritabanında: tamamlandı" "$((TOPLAM-1))|$((TOPLAM-1))|1" "$(satir "$ILR")"
kontrol "ilerleme: kapanıştan sonra baştan izleme (204)" 204 "$(post /api/ilerleme "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":0}")"
kontrol "  veritabanında: tamamlandı olarak kaldı" "$((TOPLAM-1))|0|1" "$(satir "$ILR")"
kontrol "ilerleme: geçersiz kod → 404" 404 "$(post /api/ilerleme "{\"kod\":\"$GECERSIZ\",\"hikaye\":\"m1\",\"adim\":1}")"
kontrol "ilerleme: geçersiz hikâye → 400" 400 "$(post /api/ilerleme "{\"kod\":\"$KOD\",\"hikaye\":\"m9\",\"adim\":1}")"

kontrol "cevap: soruda B seçildi (204)" 204 "$(post /api/cevap "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":$SORU,\"secim\":1}")"
kontrol "cevap: aynı soruda C seçildi (204)" 204 "$(post /api/cevap "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":$SORU,\"secim\":2}")"
kontrol "  veritabanında: iki seçim, sırayla B sonra C" "1,2" "$(satir "select group_concat(Secim) from (select Secim from Cevaplar where Adim=$SORU order by Id)")"
kontrol "cevap: olmayan şık → 400" 400 "$(post /api/cevap "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":$SORU,\"secim\":3}")"
kontrol "cevap: soru olmayan adım → 400" 400 "$(post /api/cevap "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":1,\"secim\":0}")"
kontrol "cevap: düşünme sorusu kaydedilmez → 400" 400 "$(post /api/cevap "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":$DUSUNME,\"secim\":0}")"
kontrol "cevap: geçersiz kod → 404" 404 "$(post /api/cevap "{\"kod\":\"$GECERSIZ\",\"hikaye\":\"m1\",\"adim\":$SORU,\"secim\":0}")"

# Hız sınırı: aynı koddan kısa sürede çok istek → bir noktadan sonra 429.
# (Dakika dönümüne denk gelebileceği için yalnızca son cevaba değil, 429 gelip gelmediğine bakılır.)
RED=0; for n in $(seq 1 130); do [ "$(post /api/ilerleme "{\"kod\":\"$KOD\",\"hikaye\":\"m1\",\"adim\":1}")" == 429 ] && RED=$((RED+1)) || true; done
kontrol "hız sınırı: dakikada 120'den sonrası reddedilir (429)" evet "$([ $RED -gt 0 ] && echo evet || echo hayır)"

echo "6) Site günlüğü"
sed 's/^/     /' "$GECICI/site.log"

if [ $hata -ne 0 ]; then echo "SONUÇ: kontrollerden en az biri tutmadı."; exit 1; fi
echo "SONUÇ: bütün kontroller tuttu."
