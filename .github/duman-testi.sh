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

echo "0) Hesaplar ve okullar (komutlarla)"
VA() { dotnet "$YAYIN/VeliAkademisi.dll" "$@"; }
SISTEM_SIFRE="deneme-sistem-2026"
kontrol "sistem yöneticisi: kısa şifre reddedilir" 1 "$(printf 'Deneme Sistem\nsistem\nkisa\n' | VA yonetici-ekle >/dev/null 2>&1; echo $?)"
kontrol "sistem yöneticisi: geçersiz kullanıcı adı reddedilir" 1 "$(printf 'Deneme Sistem\nsi stem\n%s\n' "$SISTEM_SIFRE" | VA yonetici-ekle >/dev/null 2>&1; echo $?)"
kontrol "sistem yöneticisi hesabı açıldı" "Sistem yöneticisi hesabı açıldı: sistem" "$(printf 'Deneme Sistem\nSistem\n%s\n' "$SISTEM_SIFRE" | VA yonetici-ekle 2>/dev/null)"
kontrol "aynı kullanıcı adı ikinci kez açılmaz" 1 "$(printf 'Başka\nsistem\n%s\n' "$SISTEM_SIFRE" | VA yonetici-ekle >/dev/null 2>&1; echo $?)"
OKUL_A="$(VA okul-ekle "Deneme Okulu A" | sed -n 's/^Okul no: //p')"
OKUL_B="$(VA okul-ekle "Deneme Okulu B" | sed -n 's/^Okul no: //p')"
kontrol "iki okul eklendi" "1 2" "$OKUL_A $OKUL_B"
kontrol "aynı adla okul eklenmez" 1 "$(VA okul-ekle "Deneme Okulu A" >/dev/null 2>&1; echo $?)"
GECICI_A="$(VA okul-yoneticisi-ekle "$OKUL_A" "Ayşe Yönetici" ayse.a | sed -n 's/^Geçici şifre: //p')"
GECICI_B="$(VA okul-yoneticisi-ekle "$OKUL_B" "Bora Yönetici" bora.b | sed -n 's/^Geçici şifre: //p')"
kontrol "geçici şifre biçimi (xxxx-xxxx-xxxx)" evet "$(echo "$GECICI_A" | grep -qE '^[a-z2-9]{4}-[a-z2-9]{4}-[a-z2-9]{4}$' && echo evet || echo hayır)"
kontrol "olmayan okula yönetici açılmaz" 1 "$(VA okul-yoneticisi-ekle 99 "X Y" xyz >/dev/null 2>&1; echo $?)"
kontrol "şifreler veritabanında yalnızca özet olarak" "pbkdf2-sha256|0" "$(python3 -c "import sqlite3,sys; c=sqlite3.connect(sys.argv[1]); print(c.execute(\"select distinct substr(SifreOzeti,1,13) from Kullanicilar\").fetchone()[0], c.execute('select count(*) from Kullanicilar where SifreOzeti like ? or SifreOzeti like ?', ('%'+sys.argv[2]+'%','%'+sys.argv[3]+'%')).fetchone()[0], sep='|')" "$VeriTabani__Yol" "$SISTEM_SIFRE" "$GECICI_A")"
kontrol "olmayan okula veli eklenmez" 1 "$(VA veli-ekle "X" "1" 99 >/dev/null 2>&1; echo $?)"
KOD_B="$(VA veli-ekle "Bora Veli" "2-A" "$OKUL_B" | sed -n 's/^Kod: //p')"

echo "1) Deneme velisi ekleniyor (veli-ekle komutu)"
CIKTI="$(dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle "Deneme Veli" "1-A" "$OKUL_A")"
echo "$CIKTI" | sed 's/^/     /'
KOD="$(echo "$CIKTI" | sed -n 's/^Kod: //p')"
kontrol "kod 22 karakter" 22 "${#KOD}"

echo "2) Sahte bir ses dosyası konuyor (ses klasöründen sunulduğunu görmek için)"
mkdir -p "$Ses__Yol/m1" && printf 'ID3deneme' > "$Ses__Yol/m1/m1_s001.mp3"
# Sunucuda her hikâyede bulunan ortam sesinin yerine geçerli, 1 saniyelik sessiz bir WAV.
# (Bu dosya yoksa onaylı sayfa açılışta "ses verisi çözülemedi" hatası verir; gerçekte dosya var.)
for h in m1 m2a m2b m3 m4 m5 m6 m7; do
  mkdir -p "$Ses__Yol/$h"
  python3 -c "import wave,sys; w=wave.open(sys.argv[1],'wb'); w.setnchannels(1); w.setsampwidth(2); w.setframerate(8000); w.writeframes(b'\0\0'*8000); w.close()" "$Ses__Yol/$h/ortam.wav"
done

export Site__Adres="http://127.0.0.1:5080/"   # sondaki "/" bilerek: site bunu atmalı

echo "2c) Deneme materyali (içeriğin geçici kopyasına; yayın paketi değişmez)"
cp -r "$YAYIN/icerik" "$GECICI/icerik"
mkdir -p "$GECICI/icerik/m1/materyal"
cp "$YAYIN/icerik/m1/img/m1_b2_kare3.webp" "$GECICI/icerik/m1/materyal/kart-deneme.webp"
cat > "$GECICI/icerik/m1/materyaller.json" <<'JSON'
{ "gorseller": [ { "dosya": "kart-deneme.webp", "baslik": "Önce–sonra kartı (deneme)" } ],
  "baglanti": { "adres": "https://www.tohumotizm.org.tr/", "yazi": "Tohum Otizm Vakfı" } }
JSON
export Icerik__Yol="$GECICI/icerik"
# Hatalı bir materyaller.json sessizce atlanmamalı: site açılmamalı ve hatanın yeri yazılmalı.
cp -r "$GECICI/icerik" "$GECICI/icerik-hatali"
sed -i 's|https://www.tohumotizm.org.tr/|http://www.tohumotizm.org.tr/|' "$GECICI/icerik-hatali/m1/materyaller.json"
HATA_CIKTI="$(Icerik__Yol="$GECICI/icerik-hatali" VeriTabani__Yol="$GECICI/hatali.db" dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle X 1 1 2>&1 || true)"
kontrol "hatalı materyaller.json: site açılmaz, hatanın yeri yazılır" evet "$(echo "$HATA_CIKTI" | grep -q 'm1/materyaller.json: bağlantı adresi https:// ile başlamalı' && echo evet || echo hayır)"

echo "2d) Eski yöntemle (şema güncelleme kaydı olmadan) kurulmuş veritabanı"
# 27 Ekim sürümünün kurduğu yapının aynısı. Site açılmamalı ve dosyaya dokunmamalı.
ESKI_DB="$GECICI/eski.db"
python3 - "$ESKI_DB" <<'PY'
import sqlite3, sys
c = sqlite3.connect(sys.argv[1])
c.executescript("""
CREATE TABLE "Veliler" ("Id" INTEGER NOT NULL CONSTRAINT "PK_Veliler" PRIMARY KEY AUTOINCREMENT, "Ad" TEXT NOT NULL,
  "Sinif" TEXT NOT NULL, "Kod" TEXT NOT NULL, "OlusturmaTarihi" TEXT NOT NULL, "SonGiris" TEXT NULL);
INSERT INTO Veliler (Ad, Sinif, Kod, OlusturmaTarihi) VALUES ('Eski', '1', 'eskikod', '2026-10-27');
""")
c.commit(); c.close()
PY
ONCE="$(sha256sum < "$ESKI_DB")"
ESKI_CIKTI="$(VeriTabani__Yol="$ESKI_DB" dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle X 1 1 2>&1; echo "çıkış:$?")"
kontrol "eski yapı: site açılmaz (çıkış kodu 1)" "çıkış:1" "$(echo "$ESKI_CIKTI" | tail -1)"
kontrol "eski yapı: nedeni yazılır" evet "$(echo "$ESKI_CIKTI" | grep -q 'eski yöntemle kurulmuş' && echo evet || echo hayır)"
kontrol "eski yapı: dosyaya dokunulmadı" "$ONCE" "$(sha256sum < "$ESKI_DB")"

echo "3) Site başlatılıyor"
dotnet "$YAYIN/VeliAkademisi.dll" > "$GECICI/site.log" 2>&1 &
SITE_PID=$!
# Test bitince (başarılı ya da değil) deneme sitesi kapatılır; 5080 numaralı kapı sonraki adımlara boş kalır.
trap 'kill "$SITE_PID" 2>/dev/null || true; wait "$SITE_PID" 2>/dev/null || true' EXIT
for i in $(seq 1 30); do curl -s "$ADRES/" > /dev/null && break; sleep 1; done

echo "4) Kontroller"
kontrol "ana sayfa yazısı" "Veli Akademisi çalışıyor." "$(curl -s "$ADRES/")"
TABLOLAR="$(python3 -c "import sqlite3,sys; print(','.join(sorted(r[0] for r in sqlite3.connect(sys.argv[1]).execute(\"select name from sqlite_master where type='table' and name not like 'sqlite_%'\"))))" "$VeriTabani__Yol")"
kontrol "veritabanı tabloları (şema güncelleme kaydıyla)" "Cevaplar,Ilerlemeler,Kullanicilar,Okullar,Veliler,__EFMigrationsHistory,__EFMigrationsLock" "$TABLOLAR"
kontrol "şema güncellemeleri kaydedildi" "20261028103255_IlkSema,20261028104055_CokOkul" "$(python3 -c "import sqlite3,sys; print(','.join(r[0] for r in sqlite3.connect(sys.argv[1]).execute('select MigrationId from __EFMigrationsHistory order by MigrationId')))" "$VeriTabani__Yol")"

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

SONGIRIS="$(python3 -c "import sqlite3,sys; print(sqlite3.connect(sys.argv[1]).execute('select SonGiris is not null from Veliler where Kod=?', (sys.argv[2],)).fetchone()[0])" "$VeriTabani__Yol" "$KOD")"
kontrol "bağlantı açılınca SonGiris dolduruldu" 1 "$SONGIRIS"

echo "5) Kayıt kancası ve kayıt uçları"
kontrol "kanca.js veriliyor" "200 javascript" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$ADRES/kanca.js" | sed -E 's/ .*javascript.*/ javascript/')"
SAYFA="$GECICI/m1_sunulan.html"; curl -s "$ADRES/v/$KOD/m1/" > "$SAYFA"
kontrol "sayfanın sonunda kanca ve Modüller etiketleri var" '<script src="/kanca.js"></script>|<script src="/moduller.js"></script>' "$(tail -n 2 "$SAYFA" | paste -sd'|')"
kontrol "moduller.js veriliyor" "200 javascript" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$ADRES/moduller.js" | sed -E 's/ .*javascript.*/ javascript/')"
# Sunulan 8 sayfa, onaylı dosyadan yalnızca belgelenmiş eklerle (tanım bölüm 7) farklı olmalı:
# Google yazı tipi satırları → sitenin kendi yazı tipi dosyası; sonda iki betik etiketi.
cat > "$GECICI/sayfa_denetle.py" <<'PY'
import sys, urllib.request
kok, adres, kod = sys.argv[1:4]
sorun = []
for h in sys.argv[4:]:
    asil = open(f"{kok}/{h}/index.html", encoding="utf-8").read()
    asil = asil.replace("</title>\n", '</title>\n<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">\n'
        f'<link rel="manifest" href="/v/{kod}/manifest.webmanifest">\n<meta name="theme-color" content="#1E6B66">\n'
        '<link rel="apple-touch-icon" href="/simge/simge-180.png">\n<meta name="apple-mobile-web-app-capable" content="yes">\n'
        '<meta name="mobile-web-app-capable" content="yes">\n<meta name="apple-mobile-web-app-title" content="Veli Akademisi">\n', 1)
    beklenen = (asil.replace('<link rel="preconnect" href="https://fonts.googleapis.com">\n', "")
                    .replace('<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Baloo+2:wght@600;700&family=Nunito:wght@500;700;800&display=swap">',
                             '<link rel="stylesheet" href="/yazitipleri/yazitipleri.css">')
                + '\n<script src="/kanca.js"></script>\n<script src="/moduller.js"></script>\n')
    gelen = urllib.request.urlopen(f"{adres}/v/{kod}/{h}/").read().decode("utf-8")
    if gelen != beklenen: sorun.append(h)
    if "fonts.googleapis.com" in gelen or "gstatic" in gelen: sorun.append(h + ":google")
print(",".join(sorun) or "yok")
PY
kontrol "m1: sunulan sayfa = onaylı sayfa + yalnızca belgelenmiş ekler; Google bağlantısı yok" "yok" \
  "$(python3 "$GECICI/sayfa_denetle.py" "$YAYIN/icerik" "$ADRES" "$KOD" m1)"
kontrol "materyal görseli veriliyor" "200 image/webp" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$ADRES/v/$KOD/m1/materyal/kart-deneme.webp")"
kontrol "olmayan materyal → 404" 404 "$(durum "$ADRES/v/$KOD/m1/materyal/yok.webp")"
kontrol "materyalde klasör atlama → 404" 404 "$(durum "$ADRES/v/$KOD/m1/materyal/..%2Fsteps.json")"
kontrol "durum: m1 materyalleri ve sıradaki hikâye" "1|Önce–sonra kartı (deneme)|https://www.tohumotizm.org.tr/|m2a" \
  "$(curl -s "$ADRES/api/durum?kod=$KOD&hikaye=m1" | python3 -c "
import json,sys; d=json.load(sys.stdin); m=d['materyaller']
print(f\"{len(m['gorseller'])}|{m['gorseller'][0]['baslik']}|{m['baglanti']['adres']}|{d['sonraki']}\")")"
echo "5b) Ana ekrana ekleme (uygulama tanımı ve simgeler)"
MANIFEST="$(curl -s -D "$GECICI/manifest.baslik" "$ADRES/v/$KOD/manifest.webmanifest")"
kontrol "uygulama tanımı veriliyor (türü doğru)" "application/manifest+json" "$(tr -d '\r' < "$GECICI/manifest.baslik" | grep -i '^content-type:' | sed 's/^[^:]*: //; s/;.*//')"
kontrol "uygulama tanımı velinin kendi bağlantısını açar" "/v/$KOD/|/v/$KOD/|/v/$KOD/|standalone|Veli Akademisi|tr" \
  "$(echo "$MANIFEST" | python3 -c "import json,sys; m=json.load(sys.stdin); print(m['start_url'],m['scope'],m['id'],m['display'],m['name'],m['lang'],sep='|')")"
SIMGELER="$(echo "$MANIFEST" | python3 -c "
import json,sys,struct,urllib.request
m=json.load(sys.stdin); sonuc=[]
for s in m['icons'] + [{'src':'/simge/simge-180.png','sizes':'180x180'}]:
    v=urllib.request.urlopen(sys.argv[1]+s['src']).read()
    w,h=struct.unpack('>II', v[16:24]) if v[:8]==b'\x89PNG\r\n\x1a\n' else (0,0)
    sonuc.append('tamam' if f'{w}x{h}'==s['sizes'] else f\"{s['src']}:{w}x{h}\")
print(','.join(sorted(set(sonuc))))" "$ADRES")"
kontrol "simgeler var, PNG ve boyutları doğru (192, 512, maskable 512, iPhone 180)" "tamam" "$SIMGELER"
kontrol "geçersiz kodla uygulama tanımı → 404" 404 "$(durum "$ADRES/v/$GECERSIZ/manifest.webmanifest")"
kontrol "yazı tipi dosyası veriliyor" "200" "$(durum "$ADRES/yazitipleri/yazitipleri.css")"
for f in baloo-2-latin-700-normal nunito-latin-ext-800-normal; do
  kontrol "  $f.woff2" "200 font/woff2" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$ADRES/yazitipleri/$f.woff2")"
done

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
KOD3="$(dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle "Sıra Deneme" "1-C" "$OKUL_A" | sed -n 's/^Kod: //p')"
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
kontrol "durum: m7 son hikâye (sıradaki yok), materyali yok" "None|0|True" \
  "$(curl -s "$ADRES/api/durum?kod=$KOD3&hikaye=m7" | python3 -c "
import json,sys; d=json.load(sys.stdin); m=d['materyaller']; print(d['sonraki'], len(m['gorseller']), m['baglanti'] is None, sep='|')")"
kontrol "8 sayfanın hepsi: onaylı sayfa + yalnızca belgelenmiş ekler; Google bağlantısı yok" "yok" \
  "$(python3 "$GECICI/sayfa_denetle.py" "$YAYIN/icerik" "$ADRES" "$KOD3" m1 m2a m2b m3 m4 m5 m6 m7)"

echo "7) Tarayıcı testi (gerçek Chrome ile kayıt kancası)"
# Ayrı bir deneme velisi: önceki velinin hız sınırı bu dakika dolmuş olabilir.
KOD2="$(dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle "Tarayıcı Deneme" "1-B" "$OKUL_A" | sed -n 's/^Kod: //p')"
KOD4="$(dotnet "$YAYIN/VeliAkademisi.dll" veli-ekle "Materyal Deneme" "1-D" "$OKUL_A" | sed -n 's/^Kod: //p')"
if node "$KLASOR/tarayici/test.mjs" "$ADRES" "$KOD2" "$SORU" "$KOD3" "$KOD4"; then
  kontrol "tarayıcı testi" tamam tamam
else
  kontrol "tarayıcı testi" tamam başarısız
fi
# Tarayıcının gönderdiği kayıtlar veritabanına doğru düşmüş mü?
VELI2="(select Id from Veliler where Kod='$KOD2')"
kontrol "  veritabanında: m1 ilerlemesi soru adımında" "$SORU" "$(satir "select SonAdim from Ilerlemeler where VeliId=$VELI2 and Hikaye='m1'")"
kontrol "  veritabanında: iki seçim, sırayla B sonra C" "1,2" "$(satir "select group_concat(Secim) from (select Secim from Cevaplar where VeliId=$VELI2 and Adim=$SORU order by Id)")"

echo "8) Yönetici girişi (kişisel hesaplar)"
CEREZ="$GECICI/cerez.txt"
belirtec_al() {  # belirtec_al <adres> [çerez] → o sayfadaki formun güvenlik belirteci (antiforgery)
  curl -s -c "${2:-$CEREZ}" -b "${2:-$CEREZ}" "$1" | python3 -c "
import re,sys; m=re.search(r'<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"', sys.stdin.read()); print(m.group(1) if m else '')"; }
belirtec() { belirtec_al "$ADRES/yonetim/giris" "${1:-$CEREZ}"; }
giris() {  # giris <kullanıcı adı> <şifre> [çerez] → "<durum kodu> <yönlendirme>" ; sayfa $GECICI/giris.html'e yazılır
  local c="${3:-$CEREZ}" T; T="$(belirtec "$c")"
  curl -s -o "$GECICI/giris.html" -D "$GECICI/giris.baslik" -w "%{http_code} %{redirect_url}" -c "$c" -b "$c" \
    -X POST --data-urlencode "KullaniciAdi=$1" --data-urlencode "Sifre=$2" --data-urlencode "__RequestVerificationToken=$T" "$ADRES/yonetim/giris"; }
sifre_degistir() {  # sifre_degistir <çerez> <mevcut> <yeni> [yeni tekrar] → "<durum> <yönlendirme>"; sayfa $GECICI/sifre.html
  local c="$1" T; T="$(belirtec_al "$ADRES/yonetim/sifre" "$c")"
  curl -s -o "$GECICI/sifre.html" -w "%{http_code} %{redirect_url}" -c "$c" -b "$c" -X POST \
    --data-urlencode "Mevcut=$2" --data-urlencode "Yeni=$3" --data-urlencode "YeniTekrar=${4:-$3}" \
    --data-urlencode "__RequestVerificationToken=$T" "$ADRES/yonetim/sifre"; }
cikis() {  # cikis <çerez> [sayfa] → "<durum> <yönlendirme>" (belirteç, Çıkış düğmesinin bulunduğu sayfadan)
  local c="$1" T; T="$(belirtec_al "${2:-$ADRES/yonetim}" "$c")"
  curl -s -o /dev/null -w '%{http_code} %{redirect_url}' -c "$c" -b "$c" -X POST --data-urlencode "__RequestVerificationToken=$T" "$ADRES/yonetim/cikis"; }
git_c() { curl -s -o /dev/null -w '%{http_code} %{redirect_url}' -b "$1" "$2"; }   # git_c <çerez> <adres>

rm -f "$CEREZ"
Y="$(yonlendirme "$ADRES/yonetim")"
kontrol "/yonetim girişsiz açılmaz, giriş sayfasına gider" "302 $ADRES/yonetim/giris" "${Y%%\?*}"
kontrol "giriş sayfası açılıyor" 200 "$(durum "$ADRES/yonetim/giris")"
kontrol "giriş sayfası arama motorlarına kapalı" "x-robots-tag: noindex, nofollow" "$(curl -s -D - -o /dev/null "$ADRES/yonetim/giris" | tr -d '\r' | grep -i '^x-robots-tag:' | tr 'A-Z' 'a-z')"
kontrol "giriş formunda güvenlik belirteci var" evet "$([ -n "$(belirtec)" ] && echo evet || echo hayır)"
kontrol "belirteçsiz giriş isteği reddedilir (400)" 400 "$(curl -s -o /dev/null -w '%{http_code}' -X POST --data-urlencode "KullaniciAdi=ayse.a" --data-urlencode "Sifre=$GECICI_A" "$ADRES/yonetim/giris")"

kontrol "yanlış şifre: sayfa yeniden gelir" "200 " "$(giris ayse.a yanlis-sifre-123)"
kontrol "  'Kullanıcı adı ya da şifre yanlış.' mesajı" evet "$(grep -q "Kullanıcı adı ya da şifre yanlış." "$GECICI/giris.html" && echo evet || echo hayır)"
kontrol "olmayan kullanıcı adı: aynı mesaj" evet "$(giris yok.boyle sifre-sifre-1 >/dev/null; grep -q "Kullanıcı adı ya da şifre yanlış." "$GECICI/giris.html" && echo evet || echo hayır)"
kontrol "geçici şifreyle giriş: önce şifre sayfasına gider" "302 $ADRES/yonetim/sifre" "$(giris "  AYSE.A " "$GECICI_A")"
SC="$(tr -d '\r' < "$GECICI/giris.baslik" | grep -i '^set-cookie: va_yonetim=' | tr 'A-Z' 'a-z')"
for b in "path=/yonetim" "httponly" "samesite=strict"; do
  kontrol "  oturum çerezi: $b" evet "$(echo "$SC" | grep -q "$b" && echo evet || echo hayır)"
done
kontrol "  geçici şifreyle takip tablosu açılmaz, şifre sayfasına gider" "302 $ADRES/yonetim/sifre" "$(git_c "$CEREZ" "$ADRES/yonetim")"
kontrol "  geçici şifreyle veli ayrıntısı da açılmaz" "302 $ADRES/yonetim/sifre" "$(git_c "$CEREZ" "$ADRES/yonetim/veli/1")"
kontrol "  şifre sayfası 'Kendi şifrenizi belirleyin' der" evet "$(curl -s -b "$CEREZ" "$ADRES/yonetim/sifre" | grep -q 'Kendi şifrenizi belirleyin' && echo evet || echo hayır)"
SIFRE_A="ayse-kendi-sifresi-1"
kontrol "  kısa yeni şifre kabul edilmez" "200 " "$(sifre_degistir "$CEREZ" "$GECICI_A" kisa)"
kontrol "  iki kutu farklıysa kabul edilmez" "200 " "$(sifre_degistir "$CEREZ" "$GECICI_A" "$SIFRE_A" "$SIFRE_A-x")"
kontrol "  mevcut şifre yanlışsa kabul edilmez" "200 " "$(sifre_degistir "$CEREZ" yanlis-yanlis "$SIFRE_A")"
kontrol "  kendi şifresini belirledi" "302 $ADRES/yonetim/sifre?degisti=true" "$(sifre_degistir "$CEREZ" "$GECICI_A" "$SIFRE_A")"
kontrol "  veritabanında geçici işareti kalktı" 0 "$(satir "select GeciciSifre from Kullanicilar where KullaniciAdi='ayse.a'")"
kontrol "giriş sonrası yönetim sayfası açılıyor" 200 "$(curl -s -o "$GECICI/yonetim.html" -w '%{http_code}' -b "$CEREZ" "$ADRES/yonetim")"
kontrol "  yönetim sayfasının içeriği" evet "$(grep -q "Takip tablosu" "$GECICI/yonetim.html" && echo evet || echo hayır)"
kontrol "  okulun adı ve kişinin adı görünüyor" evet "$(grep -q 'Deneme Okulu A' "$GECICI/yonetim.html" && grep -q 'Ayşe Yönetici' "$GECICI/yonetim.html" && echo evet || echo hayır)"
kontrol "  'Yedeği indir' düğmesi okul ekranında yok" hayır "$(grep -q 'handler=yedek' "$GECICI/yonetim.html" && echo evet || echo hayır)"
kontrol "  yedek adresi veritabanı vermez" "text/html" "$(curl -s -o /dev/null -w '%{content_type}' -b "$CEREZ" "$ADRES/yonetim?handler=yedek" | cut -d';' -f1)"
kontrol "okul yöneticisi sistem sayfasını açamaz" "302 $ADRES/yonetim/erisim" "$(git_c "$CEREZ" "$ADRES/yonetim/sistem")"
kontrol "okul yöneticisi ilçe sayfasını açamaz" "302 $ADRES/yonetim/erisim" "$(git_c "$CEREZ" "$ADRES/yonetim/ilce")"
kontrol "eski geçici şifre artık geçmez" "200 " "$(giris ayse.a "$GECICI_A" "$GECICI/cerez-x.txt")"

echo "8a) Okulun yöneticileri"
yon_islem() {  # yon_islem <çerez> <handler> [id] [alan=değer]... → "<durum> <yönlendirme>"; sayfa $GECICI/yon.html
  local c="$1" h="$2" id="${3:-}"; shift 3 || shift $#
  local T; T="$(belirtec_al "$ADRES/yonetim/yoneticiler" "$c")"
  local veri=(); for a in "$@"; do veri+=(--data-urlencode "$a"); done
  curl -s -o "$GECICI/yon.html" -w "%{http_code} %{redirect_url}" -c "$c" -b "$c" -X POST \
    --data-urlencode "__RequestVerificationToken=$T" "${veri[@]}" "$ADRES/yonetim/yoneticiler?handler=$h${id:+&id=$id}"; }
gecici_oku() { python3 -c "import re,sys; m=re.search(r'id=\"gecici-sifre\">([^<]+)<', open(sys.argv[1],encoding='utf-8').read()); print(m.group(1) if m else '')" "$GECICI/yon.html"; }
yon_no() { satir "select Id from Kullanicilar where KullaniciAdi='$1'"; }

kontrol "tek yönetici varken veli eklenemez (uyarı gösterilir)" evet "$(curl -s -b "$CEREZ" "$ADRES/yonetim/veli-ekle" | grep -q 'id="yonetici-eksik"' && echo evet || echo hayır)"
TV="$(belirtec_al "$ADRES/yonetim/veli-ekle")"
kontrol "  tek yönetici varken veli ekleme isteği de reddedilir" "0" "$(curl -s -o /dev/null -b "$CEREZ" -c "$CEREZ" -X POST --data-urlencode "__RequestVerificationToken=$TV" --data-urlencode "Ad=Yasak Veli" --data-urlencode "Sinif=1" "$ADRES/yonetim/veli-ekle?handler=tek"; satir "select count(*) from Veliler where Ad='Yasak Veli'")"
kontrol "yöneticiler sayfası açılıyor, eksik yönetici uyarısı var" evet "$(curl -s -b "$CEREZ" "$ADRES/yonetim/yoneticiler" | grep -q 'id="yonetici-eksik"' && echo evet || echo hayır)"
kontrol "geçersiz kullanıcı adıyla yönetici eklenmez" "200 |0" "$(yon_islem "$CEREZ" ekle "" "AdSoyad=Can Yönetici" "KullaniciAdi=can ğ"; echo "|$(satir "select count(*) from Kullanicilar where AdSoyad='Can Yönetici'")")"
kontrol "var olan kullanıcı adıyla yönetici eklenmez" evet "$(yon_islem "$CEREZ" ekle "" "AdSoyad=Başka" "KullaniciAdi=bora.b" >/dev/null; grep -q 'zaten kullanılıyor' "$GECICI/yon.html" && echo evet || echo hayır)"
yon_islem "$CEREZ" ekle "" "AdSoyad=Can Yönetici" "KullaniciAdi=Can.A" > /dev/null
GECICI_C="$(gecici_oku)"
kontrol "A ikinci yöneticiyi ekledi; geçici şifre bir kez gösterildi" "evet|ayse.a,can.a|$OKUL_A" "$([ -n "$GECICI_C" ] && echo evet || echo hayır)|$(satir "select group_concat(KullaniciAdi) from (select KullaniciAdi from Kullanicilar where OkulId=$OKUL_A order by KullaniciAdi)")|$(satir "select OkulId from Kullanicilar where KullaniciAdi='can.a'")"
kontrol "  sayfayı yeniden açınca geçici şifre görünmez" hayır "$(curl -s -b "$CEREZ" "$ADRES/yonetim/yoneticiler" | grep -q 'gecici-sifre' && echo evet || echo hayır)"
kontrol "  artık eksik yönetici uyarısı yok" hayır "$(curl -s -b "$CEREZ" "$ADRES/yonetim/veli-ekle" | grep -q 'id="yonetici-eksik"' && echo evet || echo hayır)"
CC="$GECICI/cerez-c.txt"
kontrol "yeni yönetici geçici şifreyle girdi" "302 $ADRES/yonetim/sifre" "$(giris can.a "$GECICI_C" "$CC")"
kontrol "  kendi şifresini belirledi" "302 $ADRES/yonetim/sifre?degisti=true" "$(sifre_degistir "$CC" "$GECICI_C" "can-kendi-sifresi-1")"
CAN="$(yon_no can.a)"
kontrol "onaysız geçici şifre verilmez" evet "$(yon_islem "$CEREZ" gecicisifre "$CAN" >/dev/null; grep -q 'önce onay kutusunu' "$GECICI/yon.html" && echo evet || echo hayır)"
yon_islem "$CEREZ" gecicisifre "$CAN" "Anladim=true" > /dev/null
GECICI_C2="$(gecici_oku)"
kontrol "şifresini unutan yöneticiye geçici şifre verildi" evet "$([ -n "$GECICI_C2" ] && [ "$GECICI_C2" != "$GECICI_C" ] && echo evet || echo hayır)"
Y="$(git_c "$CC" "$ADRES/yonetim")"
kontrol "  onun açık oturumu kapandı" "302 $ADRES/yonetim/giris" "${Y%%\?*}"
kontrol "  eski şifresi geçmez" "200 " "$(giris can.a "can-kendi-sifresi-1" "$CC")"
kontrol "  geçici şifreyle girer, şifre sayfasına gider" "302 $ADRES/yonetim/sifre" "$(giris can.a "$GECICI_C2" "$CC")"
kontrol "kendine geçici şifre verilmez" evet "$(yon_islem "$CEREZ" gecicisifre "$(yon_no ayse.a)" "Anladim=true" >/dev/null; grep -q 'Şifremi değiştir' "$GECICI/yon.html" && grep -q 'Kendi şifrenizi' "$GECICI/yon.html" && echo evet || echo hayır)"
kontrol "kendi hesabını kapatamaz" "1" "$(yon_islem "$CEREZ" kapat "$(yon_no ayse.a)" "Anladim=true" >/dev/null; satir "select Etkin from Kullanicilar where KullaniciAdi='ayse.a'")"
kontrol "onaysız hesap kapatılmaz" 1 "$(yon_islem "$CEREZ" kapat "$CAN" >/dev/null; satir "select Etkin from Kullanicilar where KullaniciAdi='can.a'")"
kontrol "hesap kapatıldı" 0 "$(yon_islem "$CEREZ" kapat "$CAN" "Anladim=true" >/dev/null; satir "select Etkin from Kullanicilar where KullaniciAdi='can.a'")"
Y="$(git_c "$CC" "$ADRES/yonetim/sifre")"
kontrol "  kapalı hesabın açık oturumu kapandı" "302 $ADRES/yonetim/giris" "${Y%%\?*}"
kontrol "  kapalı hesap giriş yapamaz" evet "$(giris can.a "$GECICI_C2" "$CC" >/dev/null; grep -q 'Bu hesap kapatılmış' "$GECICI/giris.html" && echo evet || echo hayır)"
kontrol "  tek açık yönetici kaldı: veli eklenemez" evet "$(curl -s -b "$CEREZ" "$ADRES/yonetim/veli-ekle" | grep -q 'id="yonetici-eksik"' && echo evet || echo hayır)"
yon_islem "$CEREZ" ac "$CAN" > /dev/null
GECICI_C3="$(gecici_oku)"
kontrol "hesap yeniden açıldı, yeni geçici şifreyle" "1|1|evet" "$(satir "select Etkin from Kullanicilar where KullaniciAdi='can.a'")|$(satir "select GeciciSifre from Kullanicilar where KullaniciAdi='can.a'")|$([ -n "$GECICI_C3" ] && echo evet || echo hayır)"

echo "8b) Veli ekleme"
sayfa_belirteci() {  # sayfa_belirteci <adres> → o sayfadaki formun güvenlik belirteci (oturumlu)
  curl -s -c "$CEREZ" -b "$CEREZ" "$1" | python3 -c "
import re,sys; m=re.search(r'<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"', sys.stdin.read()); print(m.group(1) if m else '')"; }
ekle() {  # ekle <handler> <alan=değer>... → "<durum> <yönlendirme>"; sayfa $GECICI/ekle.html
  local h="$1"; shift; local T; T="$(sayfa_belirteci "$ADRES/yonetim/veli-ekle")"
  local veri=(); for a in "$@"; do veri+=(--data-urlencode "$a"); done
  curl -s -o "$GECICI/ekle.html" -w "%{http_code} %{redirect_url}" -c "$CEREZ" -b "$CEREZ" -X POST \
    --data-urlencode "__RequestVerificationToken=$T" "${veri[@]}" "$ADRES/yonetim/veli-ekle?handler=$h"; }
icerir() { grep -qF -- "$2" "$1" && echo evet || echo hayır; }
veli_sayisi() { satir "select count(*) from Veliler where OkulId=$OKUL_A"; }   # A okulunun velileri

kontrol "yönetim sayfasında 'Veli ekle' bağlantısı" evet "$(icerir "$GECICI/yonetim.html" 'href="/yonetim/veli-ekle"')"
kontrol "veli ekleme sayfası açılıyor" 200 "$(durum_c() { curl -s -o /dev/null -w '%{http_code}' -b "$CEREZ" "$1"; }; durum_c "$ADRES/yonetim/veli-ekle")"
ONCE="$(veli_sayisi)"
Y="$(ekle tek "Ad=Ayşe Hanım" "Sinif=4-A")"
kontrol "tek veli eklendi, sonuç sayfasına yönlendirildi" "302 $ADRES/yonetim/veli-ekle?eklenen=" "${Y%%=*}="
curl -s -b "$CEREZ" "${Y#* }" > "$GECICI/ekle.html"
kontrol "  sonuç sayfasında ad ve sınıf (Türkçe harfler doğru)" evet "$(icerir "$GECICI/ekle.html" 'Ayşe Hanım')"
BAG="$(grep -o 'value="http://127.0.0.1:5080/v/[A-Za-z0-9_-]\{22\}"' "$GECICI/ekle.html" | head -1 | sed 's/^value="//; s/"$//')"
kontrol "  tam bağlantı gösteriliyor (adresin sonundaki / atılmış)" evet "$([ -n "$BAG" ] && echo evet || echo hayır)"
kontrol "  Kopyala düğmesi var" evet "$(icerir "$GECICI/ekle.html" 'class="btn kopyala"')"
kontrol "  bağlantı çalışıyor (Modül 1'e gider)" "302 $BAG/m1/" "$(yonlendirme "$BAG")"
kontrol "  veritabanında 1 veli arttı" "$((ONCE+1))" "$(veli_sayisi)"
kontrol "  veli, ekleyen yöneticinin okuluna eklendi" "$OKUL_A" "$(satir "select OkulId from Veliler where Ad='Ayşe Hanım'")"

kontrol "adı boş veli eklenmez" "200 " "$(ekle tek "Ad=  " "Sinif=4-A")"
kontrol "  'Ad boş olamaz.' mesajı" evet "$(icerir "$GECICI/ekle.html" 'Ad boş olamaz.')"
kontrol "  veritabanı değişmedi" "$((ONCE+1))" "$(veli_sayisi)"

Y="$(ekle toplu "Toplu=Mehmet Bey;3-B
Fatma Hanım ; 5-A

Ayşe Hanım;4-A")"
kontrol "toplu: 3 veli eklendi" "302" "${Y%% *}"
kontrol "  veritabanında 3 veli arttı" "$((ONCE+4))" "$(veli_sayisi)"
kontrol "  boşluklar temizlendi" "Fatma Hanım|5-A" "$(satir "select Ad,Sinif from Veliler where Ad like 'Fatma%'")"
curl -s -b "$CEREZ" "${Y#* }" > "$GECICI/ekle.html"
kontrol "  sonuç sayfasında 3 bağlantı" 3 "$(grep -c 'class="baglanti"' "$GECICI/ekle.html")"
kontrol "  aynı ad ve sınıf için uyarı (Ayşe Hanım, 4-A)" evet "$(icerir "$GECICI/ekle.html" 'adıyla birden fazla veli var')"

kontrol "toplu: hatalı satır varsa hiçbiri eklenmez" "200 " "$(ekle toplu "Toplu=Hasan Bey;2-A
Hasan Bey
Zehra Hanım;")"
kontrol "  2. satır hatası gösterildi" evet "$(icerir "$GECICI/ekle.html" '2. satır')"
kontrol "  3. satır hatası gösterildi" evet "$(icerir "$GECICI/ekle.html" '3. satır: Sınıf boş olamaz.')"
kontrol "  veritabanı değişmedi" "$((ONCE+4))" "$(veli_sayisi)"

kontrol "girişsiz veli ekleme isteği kabul edilmez" "302" "$(curl -s -o /dev/null -w '%{http_code}' -X POST --data-urlencode "Ad=X" --data-urlencode "Sinif=1" "$ADRES/yonetim/veli-ekle?handler=tek")"
kontrol "  veritabanı değişmedi" "$((ONCE+4))" "$(veli_sayisi)"

echo "8c) Takip tablosu ve veli ayrıntısı"
sayfa_al() { curl -s -b "$CEREZ" "$1" > "$2"; }
hucre() {  # hucre <html> <veli adı> <hikâye> → o velinin satırındaki hücre yazısı
  python3 -c "
import re,sys,html
s=open(sys.argv[1],encoding='utf-8').read()
for tr in re.findall(r'<tr data-veli=.*?</tr>', s, re.S):
    if '>'+sys.argv[2]+'</a>' in tr:
        m=re.search(r'data-hikaye=\"'+sys.argv[3]+r'\"[^>]*>([^<]*)<', tr); print(html.unescape(m.group(1)) if m else 'yok'); break
else: print('satır yok')" "$1" "$2" "$3"; }
satir_sayisi() { grep -c '<tr data-veli=' "$1" || true; }

sayfa_al "$ADRES/yonetim" "$GECICI/takip.html"
kontrol "takip tablosunda bütün veliler" "$(veli_sayisi)" "$(satir_sayisi "$GECICI/takip.html")"
kontrol "  'Sıra Deneme' m1: Tamamlandı" "Tamamlandı" "$(hucre "$GECICI/takip.html" "Sıra Deneme" m1)"
kontrol "  'Sıra Deneme' m7: Tamamlandı" "Tamamlandı" "$(hucre "$GECICI/takip.html" "Sıra Deneme" m7)"
BEKLENEN="%$(python3 -c "import sqlite3,sys,json; e=sqlite3.connect(sys.argv[1]).execute(\"select EnIleriAdim from Ilerlemeler i join Veliler v on v.Id=i.VeliId where v.Ad='Tarayıcı Deneme' and i.Hikaye='m1'\").fetchone()[0]; t=len(json.load(open(sys.argv[2],encoding='utf-8'))); print(min(99,(e+1)*100//t))" "$VeriTabani__Yol" "$YAYIN/icerik/m1/steps.json")"
kontrol "  'Tarayıcı Deneme' m1: en ileri adıma göre yüzde" "$BEKLENEN" "$(hucre "$GECICI/takip.html" "Tarayıcı Deneme" m1)"
kontrol "  'Tarayıcı Deneme' m2a: Başlanmadı" "Başlanmadı" "$(hucre "$GECICI/takip.html" "Tarayıcı Deneme" m2a)"
kontrol "  'Mehmet Bey' m1: Başlanmadı" "Başlanmadı" "$(hucre "$GECICI/takip.html" "Mehmet Bey" m1)"

sayfa_al "$ADRES/yonetim?sinif=1-C" "$GECICI/takip.html"
kontrol "sınıf süzme (1-C): yalnızca 1 veli" 1 "$(satir_sayisi "$GECICI/takip.html")"
kontrol "  o veli 'Sıra Deneme'" "Tamamlandı" "$(hucre "$GECICI/takip.html" "Sıra Deneme" m1)"
sayfa_al "$ADRES/yonetim?ara=AY%C5%9EE" "$GECICI/takip.html"
kontrol "arama 'AYŞE' (büyük harf): iki 'Ayşe Hanım'" 2 "$(satir_sayisi "$GECICI/takip.html")"
sayfa_al "$ADRES/yonetim?ara=fatma%20han%C4%B1m" "$GECICI/takip.html"
kontrol "arama 'fatma hanım' (küçük harf, ı): 1 veli" 1 "$(satir_sayisi "$GECICI/takip.html")"
sayfa_al "$ADRES/yonetim?ara=yok-boyle-veli" "$GECICI/takip.html"
kontrol "sonuçsuz arama: boş tablo mesajı" evet "$(icerir "$GECICI/takip.html" 'Gösterilecek veli yok.')"

VID="$(satir "select Id from Veliler where Ad='Tarayıcı Deneme'")"
sayfa_al "$ADRES/yonetim/veli/$VID" "$GECICI/veli.html"
kontrol "veli ayrıntısı açılıyor" evet "$(icerir "$GECICI/veli.html" 'Tarayıcı Deneme')"
ILK_SORU="$(python3 -c "import json,sys,html; s=json.load(open(sys.argv[1],encoding='utf-8')); print(next(x['q'] for x in s if x['t']=='q'))" "$YAYIN/icerik/m1/steps.json")"
kontrol "  m1 1. sorunun metni" evet "$(icerir "$GECICI/veli.html" "$ILK_SORU")"
kontrol "  1. soru: ilk seçim B, son seçim C" 'data-ilk="B" data-son="C"' "$(grep -o 'data-soru="1" data-ilk="[A-C]*" data-son="[A-C]*"' "$GECICI/veli.html" | head -1 | sed 's/data-soru="1" //')"
kontrol "  bağlantı ve Kopyala düğmesi" evet "$(icerir "$GECICI/veli.html" "value=\"http://127.0.0.1:5080/v/$KOD2\"")"
kontrol "olmayan veli → 404" 404 "$(curl -s -o /dev/null -w '%{http_code}' -b "$CEREZ" "$ADRES/yonetim/veli/99999")"
kontrol "veli ayrıntısı girişsiz açılmaz" 302 "$(curl -s -o /dev/null -w '%{http_code}' "$ADRES/yonetim/veli/$VID")"

yeni_kod() {  # yeni_kod <veli id> [Anladim=true] → "<durum> <yönlendirme>"
  local T; T="$(sayfa_belirteci "$ADRES/yonetim/veli/$1")"
  local ek=(); [ -n "${2:-}" ] && ek=(--data-urlencode "$2")
  curl -s -o "$GECICI/veli.html" -w "%{http_code} %{redirect_url}" -c "$CEREZ" -b "$CEREZ" -X POST \
    --data-urlencode "__RequestVerificationToken=$T" "${ek[@]}" "$ADRES/yonetim/veli/$1?handler=yenikod"; }
kontrol "yeni kod: onay kutusu işaretlenmeden üretilmez" "200 " "$(yeni_kod "$VID")"
kontrol "  uyarı mesajı" evet "$(icerir "$GECICI/veli.html" 'önce onay kutusunu işaretleyin')"
kontrol "  kod değişmedi" "$KOD2" "$(satir "select Kod from Veliler where Id=$VID")"
kontrol "yeni kod: onaylı istek" "302 $ADRES/yonetim/veli/$VID?yenilendi=true" "$(yeni_kod "$VID" "Anladim=true")"
YENIKOD="$(satir "select Kod from Veliler where Id=$VID")"
kontrol "  kod değişti" evet "$([ "$YENIKOD" != "$KOD2" ] && [ ${#YENIKOD} -eq 22 ] && echo evet || echo hayır)"
kontrol "  eski bağlantı artık geçersiz" 404 "$(durum "$ADRES/v/$KOD2")"
kontrol "  yeni bağlantı çalışıyor" 302 "$(curl -s -o /dev/null -w '%{http_code}' "$ADRES/v/$YENIKOD")"
kontrol "  ilerleme ve cevaplar silinmedi" "2" "$(satir "select count(*) from Cevaplar where VeliId=$VID")"

echo "8c2) Okul ayrımı: B okulunun yöneticisi A okulunun velilerini göremez, değiştiremez"
CB="$GECICI/cerez-b.txt"
kontrol "B yöneticisi geçici şifreyle girdi" "302 $ADRES/yonetim/sifre" "$(giris bora.b "$GECICI_B" "$CB")"
SIFRE_B="bora-kendi-sifresi-1"
kontrol "  kendi şifresini belirledi" "302 $ADRES/yonetim/sifre?degisti=true" "$(sifre_degistir "$CB" "$GECICI_B" "$SIFRE_B")"
B_TAKIP="$(curl -s -b "$CB" "$ADRES/yonetim")"
kontrol "B'nin takip tablosunda yalnızca kendi velisi" "1|evet|hayır" "$(echo "$B_TAKIP" | grep -c '<tr data-veli=')|$(echo "$B_TAKIP" | grep -q 'Bora Veli' && echo evet || echo hayır)|$(echo "$B_TAKIP" | grep -q 'Tarayıcı Deneme' && echo evet || echo hayır)"
kontrol "B, A'nın velisinin ayrıntısını açamaz (404)" 404 "$(curl -s -o /dev/null -w '%{http_code}' -b "$CB" "$ADRES/yonetim/veli/$VID")"
TB="$(belirtec_al "$ADRES/yonetim" "$CB")"
kontrol "B, A'nın velisine yeni kod üretemez (404)" 404 "$(curl -s -o /dev/null -w '%{http_code}' -b "$CB" -c "$CB" -X POST --data-urlencode "__RequestVerificationToken=$TB" --data-urlencode "Anladim=true" "$ADRES/yonetim/veli/$VID?handler=yenikod")"
kontrol "B, A'nın velisini silemez (404)" 404 "$(curl -s -o /dev/null -w '%{http_code}' -b "$CB" -c "$CB" -X POST --data-urlencode "__RequestVerificationToken=$TB" --data-urlencode "SilAnladim=true" "$ADRES/yonetim/veli/$VID?handler=sil")"
kontrol "  A'nın velisi yerinde, kodu değişmedi" "$YENIKOD" "$(satir "select Kod from Veliler where Id=$VID")"
curl -s -o "$GECICI/rapor-b.xlsx" -b "$CB" "$ADRES/yonetim?handler=excel"
kontrol "B'nin Excel'inde yalnızca kendi velisi (Takip ve Cevaplar)" "Bora Veli|0" "$(python3 -c "
import sys, openpyxl
k = openpyxl.load_workbook(sys.argv[1])
print(','.join(r[0].value for r in k['Takip'].iter_rows(min_row=2)), k['Cevaplar'].max_row - 1, sep='|')" "$GECICI/rapor-b.xlsx")"
kontrol "B, A'nın yöneticisine geçici şifre veremez (404)" "404 " "$(yon_islem "$CB" gecicisifre "$CAN" "Anladim=true")"
kontrol "B, A'nın yöneticisinin hesabını kapatamaz (404)" "404 |1" "$(yon_islem "$CB" kapat "$CAN" "Anladim=true")|$(satir "select Etkin from Kullanicilar where KullaniciAdi='can.a'")"
kontrol "B'nin yöneticiler sayfasında A'nın yöneticileri yok" hayır "$(curl -s -b "$CB" "$ADRES/yonetim/yoneticiler" | grep -q 'ayse.a\|can.a' && echo evet || echo hayır)"
kontrol "B'de tek yönetici var: veli eklenemez" evet "$(curl -s -b "$CB" "$ADRES/yonetim/veli-ekle" | grep -q 'id="yonetici-eksik"' && echo evet || echo hayır)"
kontrol "B'nin yıl sonu sayfası yalnızca kendi velisini sayar" evet "$(curl -s -b "$CB" "$ADRES/yonetim/yil-sonu" | grep -q '<span id="veli-sayisi">1</span>' && echo evet || echo hayır)"

echo "8d) Excel ve silme"
indir() { curl -s -o "$2" -D "$2.baslik" -w '%{http_code}' -b "$CEREZ" "$1"; }   # indir <adres> <dosya>
dosya_adi() { tr -d '\r' < "$1.baslik" | grep -i '^content-disposition:' | grep -o 'filename=[^;]*' | head -1 | sed 's/filename=//; s/"//g'; }

# İdeal şıkkı olmayan (bütün şıkları eşit) bir soruya bir cevap: Excel'de "ideal" sütunu boş kalmalı.
ESIT="$(python3 -c "import json,sys; s=json.load(open(sys.argv[1],encoding='utf-8')); print(next(k for k,x in enumerate(s) if x['t']=='q' and x.get('all')))" "$YAYIN/icerik/m1/steps.json")"
kontrol "eşit şıklı soruya cevap kaydedildi" 204 "$(post /api/cevap "{\"kod\":\"$KOD3\",\"hikaye\":\"m1\",\"adim\":$ESIT,\"secim\":0}")"

kontrol "Excel indiriliyor" 200 "$(indir "$ADRES/yonetim?handler=excel" "$GECICI/rapor.xlsx")"
kontrol "  dosya adı (okulun adıyla)" evet "$(dosya_adi "$GECICI/rapor.xlsx" | grep -qE '^veli-akademisi-deneme-okulu-a-[0-9]{4}-[0-9]{2}-[0-9]{2}-[0-9]{4}\.xlsx$' && echo evet || echo hayır)"
IDEAL1="$(python3 -c "import json,sys; s=json.load(open(sys.argv[1],encoding='utf-8')); print('ABC'[next(x for x in s if x['t']=='q')['ov']['i']])" "$YAYIN/icerik/m1/steps.json")"
EXCEL="$(python3 - "$GECICI/rapor.xlsx" "$IDEAL1" <<'PY'
import sys, datetime, openpyxl
k = openpyxl.load_workbook(sys.argv[1]); ideal1 = sys.argv[2]
t, c = k["Takip"], k["Cevaplar"]
sonuc = [",".join(k.sheetnames), str(t.max_row - 1)]
takip = {r[0].value: [x.value for x in r] for r in t.iter_rows(min_row=2)}
sonuc.append(takip.get("Ayşe Hanım", ["yok"]*3)[2])                      # Türkçe harfler ve "Başlanmadı"
sonuc.append(takip.get("Sıra Deneme", ["yok"]*10)[9])                    # M7 sütunu
satirlar = [[x.value for x in r] for r in c.iter_rows(min_row=2)]
tar = next((r for r in satirlar if r[0] == "Tarayıcı Deneme" and r[2].startswith("M1") and r[3] == 1), None)
sonuc.append("yok" if tar is None else f"{tar[5]}>{tar[8]}|ideal:{tar[11]}|tarih:{isinstance(tar[7], datetime.datetime)}")
sonuc.append("Hayır" if ideal1 != "C" else "Evet")                       # beklenen ideal sonucu (son seçim C)
hiza = c.cell(row=2, column=5).alignment.vertical == "top" and all(c.cell(row=2, column=k).alignment.horizontal == "center" for k in (4, 6, 9, 12))
esit = [r for r in satirlar if r[0] == "Sıra Deneme" and r[2].startswith("M1")]
sonuc.append("esit-bos" if esit and esit[0][11] in (None, "") else "esit-dolu")
sonuc.append("hiza-tamam" if hiza else "hiza-yanlis")
print("#".join(str(x) for x in sonuc))
PY
)"
IFS='#' read -r SAYFALAR TAKIPSATIR AYSE SIRA7 TARAYICI BEKIDEAL ESITSONUC HIZA <<< "$EXCEL"
kontrol "  iki sayfa: Takip, Cevaplar" "Takip,Cevaplar" "$SAYFALAR"
kontrol "  Takip: her veli bir satır" "$(veli_sayisi)" "$TAKIPSATIR"
kontrol "  Takip: 'Ayşe Hanım' M1 Başlanmadı (Türkçe harfler doğru)" "Başlanmadı" "$AYSE"
kontrol "  Takip: 'Sıra Deneme' M7 Tamamlandı" "Tamamlandı" "$SIRA7"
kontrol "  Cevaplar: ilk B, son C, ideal sütunu, tarih hücresi" "B>C|ideal:$BEKIDEAL|tarih:True" "$TARAYICI"
kontrol "  Cevaplar: eşit şıklı soruda ideal sütunu boş" "esit-bos" "$ESITSONUC"
kontrol "  Cevaplar: hücreler üste hizalı, numara ve harf sütunları ortalı" "hiza-tamam" "$HIZA"

kontrol "girişsiz Excel indirilemez" 302 "$(curl -s -o /dev/null -w '%{http_code}' "$ADRES/yonetim?handler=excel")"

veli_sil() {  # veli_sil <id> [SilAnladim=true]
  local T; T="$(sayfa_belirteci "$ADRES/yonetim/veli/$1")"
  local ek=(); [ -n "${2:-}" ] && ek=(--data-urlencode "$2")
  curl -s -o "$GECICI/veli.html" -w "%{http_code} %{redirect_url}" -c "$CEREZ" -b "$CEREZ" -X POST \
    --data-urlencode "__RequestVerificationToken=$T" "${ek[@]}" "$ADRES/yonetim/veli/$1?handler=sil"; }
ONCE="$(veli_sayisi)"
kontrol "tek veli silme: onaysız silinmez" "200 " "$(veli_sil "$VID")"
kontrol "  veli yerinde" "$ONCE" "$(veli_sayisi)"
kontrol "tek veli silme: onaylı" "302 $ADRES/yonetim" "$(veli_sil "$VID" "SilAnladim=true")"
kontrol "  veli silindi" "$((ONCE-1))" "$(veli_sayisi)"
kontrol "  cevapları ve ilerlemesi de silindi" "0|0" "$(satir "select (select count(*) from Cevaplar where VeliId=$VID),(select count(*) from Ilerlemeler where VeliId=$VID)")"
kontrol "  bağlantısı artık geçersiz" 404 "$(durum "$ADRES/v/$YENIKOD")"

yil_sonu() {  # yil_sonu <Yazi> [Anladim=true]
  local T; T="$(sayfa_belirteci "$ADRES/yonetim/yil-sonu")"
  local ek=(); [ -n "${2:-}" ] && ek=(--data-urlencode "$2")
  curl -s -o "$GECICI/yilsonu.html" -w "%{http_code} %{redirect_url}" -c "$CEREZ" -b "$CEREZ" -X POST \
    --data-urlencode "__RequestVerificationToken=$T" --data-urlencode "Yazi=$1" "${ek[@]}" "$ADRES/yonetim/yil-sonu"; }
ONCE="$(veli_sayisi)"
kontrol "yıl sonu: yazı doğru ama kutu işaretsiz → silinmez" "200 " "$(yil_sonu "TÜMÜNÜ SİL")"
kontrol "yıl sonu: kutu işaretli ama yazı yanlış → silinmez" "200 " "$(yil_sonu "sil" "Anladim=true")"
kontrol "  'Hiçbir şey silinmedi' mesajı" evet "$(icerir "$GECICI/yilsonu.html" 'Hiçbir şey silinmedi')"
kontrol "  veriler yerinde" "$ONCE" "$(veli_sayisi)"
kontrol "yıl sonu: iki onay (küçük harfle 'tümünü sil')" "302 $ADRES/yonetim/yil-sonu?silindi=true" "$(yil_sonu "tümünü sil" "Anladim=true")"
kontrol "  A okulunun bütün kayıtları silindi" "0|0|0" "$(satir "select (select count(*) from Veliler where OkulId=$OKUL_A),(select count(*) from Ilerlemeler i join Veliler v on v.Id=i.VeliId where v.OkulId=$OKUL_A),(select count(*) from Cevaplar c join Veliler v on v.Id=c.VeliId where v.OkulId=$OKUL_A)")"
kontrol "  veritabanında A'ya ait ilerleme/cevap kalmadı" "0|0" "$(satir "select (select count(*) from Ilerlemeler where VeliId not in (select Id from Veliler)),(select count(*) from Cevaplar where VeliId not in (select Id from Veliler))")"
kontrol "  eski bağlantılar geçersiz" 404 "$(durum "$ADRES/v/$KOD3")"
kontrol "  B okulunun velisi yerinde, bağlantısı çalışıyor" "1|302" "$(satir "select count(*) from Veliler where OkulId=$OKUL_B")|$(curl -s -o /dev/null -w '%{http_code}' "$ADRES/v/$KOD_B")"

kontrol "Çıkış: giriş sayfasına döner" "302 $ADRES/yonetim/giris" "$(cikis "$CEREZ")"
Y="$(curl -s -o /dev/null -w '%{http_code} %{redirect_url}' -b "$CEREZ" "$ADRES/yonetim")"
kontrol "çıkıştan sonra yönetim sayfası açılmaz" "302 $ADRES/yonetim/giris" "${Y%%\?*}"

# Kilit: 5 hatalı denemeden sonra doğru şifre de kabul edilmez. (Testin sonunda; çünkü kilit 15 dk sürer.)
rm -f "$CEREZ"
for n in 1 2 3 4 5; do giris ayse.a "yanlis-$n-sifre" > /dev/null; done
echo "8f) Oturum ve kilit"
# Şifre başka bir cihazda değişirse eski oturum kapanır.
CB2="$GECICI/cerez-b2.txt"
kontrol "B ikinci cihazdan girdi" "302 $ADRES/yonetim" "$(giris bora.b "$SIFRE_B" "$CB2")"
kontrol "  ikinci cihazda şifre değişti" "302 $ADRES/yonetim/sifre?degisti=true" "$(sifre_degistir "$CB2" "$SIFRE_B" "$SIFRE_B-yeni")"
kontrol "  ikinci cihazın oturumu sürüyor" 200 "$(curl -s -o /dev/null -w '%{http_code}' -b "$CB2" "$ADRES/yonetim")"
Y="$(git_c "$CB" "$ADRES/yonetim")"
kontrol "  ilk cihazın oturumu kapandı" "302 $ADRES/yonetim/giris" "${Y%%\?*}"
# Sistem yöneticisi kendi sayfasına gider; okul sayfalarını açamaz.
CS="$GECICI/cerez-s.txt"
kontrol "sistem yöneticisi girişi: sistem sayfasına gider" "302 $ADRES/yonetim/sistem" "$(giris sistem "$SISTEM_SIFRE" "$CS")"
kontrol "  sistem sayfası açılıyor" 200 "$(curl -s -o /dev/null -w '%{http_code}' -b "$CS" "$ADRES/yonetim/sistem")"
kontrol "  takip tablosunu açamaz" "302 $ADRES/yonetim/erisim" "$(git_c "$CS" "$ADRES/yonetim")"
kontrol "  Excel indiremez" "302 $ADRES/yonetim/erisim" "$(git_c "$CS" "$ADRES/yonetim?handler=excel")"
kontrol "  veli ekleme sayfasını açamaz" "302 $ADRES/yonetim/erisim" "$(git_c "$CS" "$ADRES/yonetim/veli-ekle")"
kontrol "  erişim sayfası açılıyor" 200 "$(curl -s -o /dev/null -w '%{http_code}' -b "$CS" "$ADRES/yonetim/erisim")"
kontrol "  sistem sayfasındaki Çıkış düğmesi çalışıyor" "302 $ADRES/yonetim/giris" "$(cikis "$CS" "$ADRES/yonetim/sistem")"
kontrol "şifre sayfasındaki Çıkış düğmesi çalışıyor" "302 $ADRES/yonetim/giris" "$(cikis "$CB2" "$ADRES/yonetim/sifre")"
# Kilit kullanıcı adına göre: A'nın hataları B'yi kilitlemez.
kontrol "5 hatadan sonra doğru şifre kabul edilmez" "200 " "$(giris ayse.a "$SIFRE_A")"
kontrol "  kilit mesajı" evet "$(grep -q "çok fazla hatalı deneme" "$GECICI/giris.html" && echo evet || echo hayır)"
kontrol "  başka kullanıcı (B) kilitlenmedi" "302 $ADRES/yonetim" "$(giris bora.b "$SIFRE_B-yeni" "$GECICI/cerez-b3.txt")"
kontrol "anahtarlar veritabanının yanında saklanıyor" evet "$(ls "$GECICI/veri/anahtarlar"/*.xml >/dev/null 2>&1 && echo evet || echo hayır)"

echo "8e) Uygulama günlüğü (KVKK): veli kodları ve istek adresleri yazılmıyor"
kontrol "günlükte hiçbir veli kodu geçmiyor" yok "$(grep -qE "$KOD|$KOD3|$YENIKOD" "$GECICI/site.log" && echo var || echo yok)"
kontrol "günlükte istek adresleri yazılmıyor" yok "$(grep -q 'Request starting' "$GECICI/site.log" && echo var || echo yok)"

echo "9) Site günlüğü"
sed 's/^/     /' "$GECICI/site.log"

if [ $hata -ne 0 ]; then echo "SONUÇ: kontrollerden en az biri tutmadı."; exit 1; fi
echo "SONUÇ: bütün kontroller tuttu."
