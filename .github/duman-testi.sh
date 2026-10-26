#!/usr/bin/env bash
# Duman testi: derlenen siteyi gerçekten çalıştırır ve temel adresleri dener.
# Herhangi bir kontrol tutmazsa betik hata koduyla biter ve GitHub derlemesi "başarısız" olur.
# Kullanım: .github/duman-testi.sh <derlenmiş klasör>
set -euo pipefail
trap 'echo "::error::Betik $LINENO. satırda beklenmedik şekilde durdu (komut: $BASH_COMMAND)"' ERR

YAYIN="$(cd "$1" && pwd)"
KLASOR="$(cd "$(dirname "$0")" && pwd)"   # bu betiğin klasörü (tarayıcı testi burada)
GECICI="$(mktemp -d)"
# Site, deponun klasöründen değil başka bir klasörden çalıştırılır; böylece dosyaları
# gerçekten derlenmiş paketin içinden okuduğu sınanır.
cd "$GECICI"
export VeriTabani__Yol="$GECICI/veri/veliakademisi.db"
export Ses__Yol="$GECICI/ses"
export ASPNETCORE_URLS="http://127.0.0.1:5080"
ADRES="http://127.0.0.1:5080"

hata=0
kontrol() {  # kontrol "<açıklama>" <beklenen> <gerçek>
  if [ "$2" == "$3" ]; then echo "  TAMAM  $1"; else echo "  HATA   $1 — beklenen: [$2], gelen: [$3]"; echo "::error::$1 — beklenen: [$2], gelen: [$3]"; hata=1; fi
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
# Sunucuda her hikâyede bulunan ortam sesinin yerine geçerli, 1 saniyelik sessiz bir WAV.
# (Bu dosya yoksa onaylı sayfa açılışta "ses verisi çözülemedi" hatası verir; gerçekte dosya var.)
python3 -c "import wave,sys; w=wave.open(sys.argv[1],'wb'); w.setnchannels(1); w.setsampwidth(2); w.setframerate(8000); w.writeframes(b'\0\0'*8000); w.close()" "$Ses__Yol/m1/ortam.wav"

echo "3) Site başlatılıyor"
dotnet "$YAYIN/VeliAkademisi.dll" > "$GECICI/site.log" 2>&1 &
for i in $(seq 1 30); do curl -s "$ADRES/" > /dev/null && break; sleep 1; done

echo "4) Kontroller"
kontrol "ana sayfa yazısı" "Veli Akademisi çalışıyor." "$(curl -s "$ADRES/")"
TABLOLAR="$(python3 -c "import sqlite3,sys; print(','.join(sorted(r[0] for r in sqlite3.connect(sys.argv[1]).execute(\"select name from sqlite_master where type='table' and name not like 'sqlite_%'\"))))" "$VeriTabani__Yol")"
kontrol "veritabanı tabloları" "Cevaplar,Ilerlemeler,Veliler" "$TABLOLAR"

kontrol "/v/{kod} → Modül 1'e yönlendirme" "302 $ADRES/v/$KOD/m1/" "$(yonlendirme "$ADRES/v/$KOD")"
kontrol "/v/{kod}/m1 → sonuna / eklenerek yönlendirme" "302 $ADRES/v/$KOD/m1/" "$(yonlendirme "$ADRES/v/$KOD/m1")"
kontrol "yeni velide m1 sayfası açılıyor" 200 "$(durum "$ADRES/v/$KOD/m1/")"
for h in m2a m2b m3 m4 m5 m6 m7; do
  kontrol "yeni velide kilitli hikâye açılmaz, bağlantıya döner: $h" "302 $ADRES/v/$KOD" "$(yonlendirme "$ADRES/v/$KOD/$h/")"
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

echo "6) Kaldığı yerden devam ve sıralı açılma"
KOD3="$(dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle "Sıra Deneme" "1-C" | sed -n 's/^Kod: //p')"
# /api/durum cevabından istenen alanları "a|b|c" biçiminde çıkarır.
dur() { curl -s "$ADRES/api/durum?kod=$KOD3&hikaye=$1" | python3 -c "
import json,sys; d=json.load(sys.stdin); s={h['kod']:h['durum'] for h in d['hikayeler']}
print(f\"{d['baslangicAdimi']}|{str(d['yarimKaldi']).lower()}|{s['m1']}|{s['m2a']}|{s['m2b']}\")"; }
ilr() { post /api/ilerleme "{\"kod\":\"$KOD3\",\"hikaye\":\"$1\",\"adim\":$2}"; }
toplam() { python3 -c "import json,sys; print(len(json.load(open(sys.argv[1],encoding='utf-8'))))" "$YAYIN/icerik/$1/steps.json"; }

kontrol "ilk durum: m1 baştan, yarım değil; m1 başlanmadı, m2a ve m2b kilitli" "0|false|baslanmadi|kilitli|kilitli" "$(dur m1)"
kontrol "bağlantı m1'e gider" "302 $ADRES/v/$KOD3/m1/" "$(yonlendirme "$ADRES/v/$KOD3")"
kontrol "kilitli hikâyenin durumu sorulamaz (403)" 403 "$(durum "$ADRES/api/durum?kod=$KOD3&hikaye=m2a")"
kontrol "kilitli hikâyeye ilerleme kaydedilmez (403)" 403 "$(ilr m2a 1)"
kontrol "kilitli hikâyeye cevap kaydedilmez (403)" 403 "$(post /api/cevap "{\"kod\":\"$KOD3\",\"hikaye\":\"m2a\",\"adim\":1,\"secim\":0}")"
kontrol "kilitli hikâyenin görseli verilmez (404)" 404 "$(durum "$ADRES/v/$KOD3/m2a/img/$(ls "$YAYIN/icerik/m2a/img" | head -1)")"

ilr m1 7 > /dev/null
kontrol "m1 adım 7'de bırakıldı: oradan devam, yarım kaldı" "7|true|devam|kilitli|kilitli" "$(dur m1)"
kontrol "bağlantı yine m1'e gider" "302 $ADRES/v/$KOD3/m1/" "$(yonlendirme "$ADRES/v/$KOD3")"

ilr m1 $(( $(toplam m1) - 1 )) > /dev/null
kontrol "m1 bitti: m1 tamamlandı (yeniden açılırsa baştan), m2a açıldı" "0|false|tamamlandi|baslanmadi|kilitli" "$(dur m1)"
kontrol "bağlantı artık m2a'ya gider" "302 $ADRES/v/$KOD3/m2a/" "$(yonlendirme "$ADRES/v/$KOD3")"
kontrol "m2a sayfası açılıyor" 200 "$(durum "$ADRES/v/$KOD3/m2a/")"
kontrol "m2b hâlâ kilitli" "302 $ADRES/v/$KOD3" "$(yonlendirme "$ADRES/v/$KOD3/m2b/")"
kontrol "bitmiş m1'e geri dönülebilir" 200 "$(durum "$ADRES/v/$KOD3/m1/")"

ilr m1 3 > /dev/null   # bitmiş m1 tekrar izleniyor
kontrol "bitmiş hikâye tekrar izlense de tamamlandı kalır, bağlantı m2a'ya gider" "302 $ADRES/v/$KOD3/m2a/" "$(yonlendirme "$ADRES/v/$KOD3")"

for h in m2a m2b m3 m4 m5 m6 m7; do ilr $h $(( $(toplam $h) - 1 )) > /dev/null; done
kontrol "hepsi bitti: bağlantı en son bakılan hikâyeye (m7) gider" "302 $ADRES/v/$KOD3/m7/" "$(yonlendirme "$ADRES/v/$KOD3")"
sleep 1; ilr m3 2 > /dev/null
kontrol "hepsi bitti, m3 tekrar izlendi: bağlantı m3'e gider" "302 $ADRES/v/$KOD3/m3/" "$(yonlendirme "$ADRES/v/$KOD3")"

echo "7) Tarayıcı testi (gerçek Chrome ile kayıt kancası)"
# Ayrı bir deneme velisi: önceki velinin hız sınırı bu dakika dolmuş olabilir.
KOD2="$(dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle "Tarayıcı Deneme" "1-B" | sed -n 's/^Kod: //p')"
if node "$KLASOR/tarayici/test.mjs" "$ADRES" "$KOD2" "$SORU"; then
  kontrol "tarayıcı testi" tamam tamam
else
  kontrol "tarayıcı testi" tamam başarısız
fi
# Tarayıcının gönderdiği kayıtlar veritabanına doğru düşmüş mü?
VELI2="(select Id from Veliler where Kod='$KOD2')"
kontrol "  veritabanında: m1 ilerlemesi soru adımında" "$SORU" "$(satir "select SonAdim from Ilerlemeler where VeliId=$VELI2 and Hikaye='m1'")"
kontrol "  veritabanında: iki seçim, sırayla B sonra C" "1,2" "$(satir "select group_concat(Secim) from (select Secim from Cevaplar where VeliId=$VELI2 and Adim=$SORU order by Id)")"

echo "8) Site günlüğü"
sed 's/^/     /' "$GECICI/site.log"

if [ $hata -ne 0 ]; then echo "SONUÇ: kontrollerden en az biri tutmadı."; exit 1; fi
echo "SONUÇ: bütün kontroller tuttu."
