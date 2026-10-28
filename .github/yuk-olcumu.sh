#!/usr/bin/env bash
# Yük ölçümü: 1.000 veli (ilçe için öngörülen en yüksek sayı, 28 Ekim) ile yönetim ekranlarının ve veli
# uçlarının ne kadar sürede cevap verdiğini ölçer. En kötü durum için 1.000 velinin hepsi tek okulda;
# her veli 8 hikâyenin hepsini bitirmiş ve bütün sorulara iki kez cevap vermiş sayılır.
# Bir süre sınırı aşılırsa hata koduyla biter.
# Kullanım: .github/yuk-olcumu.sh <derlenmiş klasör>
set -euo pipefail
trap 'echo "::error::Yük ölçümü $LINENO. satırda durdu (komut: $BASH_COMMAND)"' ERR

YAYIN="$(cd "$1" && pwd)"
GECICI="$(mktemp -d)"
cd "$GECICI"
export VeriTabani__Yol="$GECICI/veri/v.db" Ses__Yol="$GECICI/ses" ASPNETCORE_URLS="http://127.0.0.1:5081" Site__Adres="http://127.0.0.1:5081"
ADRES="http://127.0.0.1:5081"
VA() { dotnet "$YAYIN/VeliAkademisi.dll" "$@"; }
hata=0

echo "1) Veriler hazırlanıyor"
printf 'Ölçüm\nolcum.sistem\nolcum-sistem-sifresi\n' | VA yonetici-ekle >/dev/null 2>&1
VA okul-ekle "Ölçüm Okulu" >/dev/null
GECICI_SIFRE="$(VA okul-yoneticisi-ekle 1 "Ölçüm Yönetici" olcum.a | sed -n 's/^Geçici şifre: //p')"
VA okul-yoneticisi-ekle 1 "Ölçüm Yönetici 2" olcum.b >/dev/null
VA veli-ekle "İlk Veli" "1-A" 1 >/dev/null   # kodu gerçek biçimde olsun diye bir veli komutla eklenir
python3 - "$VeriTabani__Yol" "$YAYIN/icerik" <<'PY'
import sqlite3, sys, json, os, random, secrets, base64
db, icerik = sys.argv[1], sys.argv[2]
hikayeler = ["m1", "m2a", "m2b", "m3", "m4", "m5", "m6", "m7"]
bilgi = {}
for h in hikayeler:
    s = json.load(open(os.path.join(icerik, h, "steps.json"), encoding="utf-8"))
    bilgi[h] = (len(s), [k for k, x in enumerate(s) if x["t"] == "q"])
c = sqlite3.connect(db)
zaman = "2026-10-28 10:00:00"
veliler = []
for n in range(2, 1001):
    kod = base64.urlsafe_b64encode(secrets.token_bytes(16)).decode().rstrip("=")
    veliler.append((1, f"Veli {n} Hanım", f"{n % 12 + 1}-{'ABCD'[n % 4]}", kod, zaman))
c.executemany("insert into Veliler (OkulId, Ad, Sinif, Kod, OlusturmaTarihi) values (?,?,?,?,?)", veliler)
idler = [r[0] for r in c.execute("select Id from Veliler")]
ilr, cvp = [], []
for v in idler:
    for h in hikayeler:
        toplam, sorular = bilgi[h]
        ilr.append((v, h, toplam - 1, toplam - 1, 1, zaman, zaman))
        for a in sorular:
            for _ in range(2):
                cvp.append((v, h, a, random.randint(0, 2), zaman))
c.executemany("insert into Ilerlemeler (VeliId, Hikaye, EnIleriAdim, SonAdim, Tamamlandi, IlkGiris, SonGuncelleme) values (?,?,?,?,?,?,?)", ilr)
c.executemany("insert into Cevaplar (VeliId, Hikaye, Adim, Secim, Zaman) values (?,?,?,?,?)", cvp)
# İlçe izleyicisi (sitenin şifre özeti biçimiyle: pbkdf2-sha256$tekrar$tuz$özet)
import hashlib
tuz = secrets.token_bytes(16)
ozet = hashlib.pbkdf2_hmac("sha256", b"olcum-ilce-sifresi", tuz, 600000, 32)
c.execute("insert into Kullanicilar (AdSoyad, KullaniciAdi, SifreOzeti, Rol, GeciciSifre, Etkin, GuvenlikDamgasi, OlusturmaTarihi) values (?,?,?,?,?,?,?,?)",
          ("Ölçüm İlçe", "olcum.ilce", f"pbkdf2-sha256$600000${base64.b64encode(tuz).decode()}${base64.b64encode(ozet).decode()}", 2, 0, 1, secrets.token_hex(16), zaman))
c.commit()
print(f"     veli: {len(idler)}  ilerleme: {len(ilr)}  cevap: {len(cvp)}")
PY
KOD="$(python3 -c "import sqlite3,sys; print(sqlite3.connect(sys.argv[1]).execute(\"select Kod from Veliler where Ad='İlk Veli'\").fetchone()[0])" "$VeriTabani__Yol")"
for h in m1 m2a m2b m3 m4 m5 m6 m7; do mkdir -p "$Ses__Yol/$h"; done

echo "2) Site başlatılıyor"
dotnet "$YAYIN/VeliAkademisi.dll" > "$GECICI/site.log" 2>&1 &
SITE_PID=$!
trap 'kill "$SITE_PID" 2>/dev/null || true; wait "$SITE_PID" 2>/dev/null || true' EXIT
for _ in $(seq 1 30); do curl -s "$ADRES/" > /dev/null && break; sleep 1; done

C="$GECICI/cerez.txt"
belirtec() { curl -s -c "$C" -b "$C" "$1" | python3 -c "
import re,sys; m=re.search(r'name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"', sys.stdin.read()); print(m.group(1) if m else '')"; }
giris() { local T; T="$(belirtec "$ADRES/yonetim/giris")"
  curl -s -o /dev/null -c "$C" -b "$C" -X POST --data-urlencode "KullaniciAdi=$1" --data-urlencode "Sifre=$2" \
    --data-urlencode "__RequestVerificationToken=$T" "$ADRES/yonetim/giris"; }
giris olcum.a "$GECICI_SIFRE"
T="$(belirtec "$ADRES/yonetim/sifre")"
curl -s -o /dev/null -c "$C" -b "$C" -X POST --data-urlencode "Mevcut=$GECICI_SIFRE" --data-urlencode "Yeni=olcum-kendi-sifresi" \
  --data-urlencode "YeniTekrar=olcum-kendi-sifresi" --data-urlencode "__RequestVerificationToken=$T" "$ADRES/yonetim/sifre"

# olc <açıklama> <sınır (sn)> <adres> [curl seçenekleri]: ilk istek ısınma; sonraki 3 istekten en yavaşı ölçülür.
olc() {
  local aciklama="$1" sinir="$2" adres="$3"; shift 3
  curl -s -o /dev/null "$@" "$adres"
  local en=0 s
  for _ in 1 2 3; do
    s="$(curl -s -o /dev/null -w '%{http_code} %{time_total}' "$@" "$adres")"
    [ "${s%% *}" = 200 ] || { echo "  HATA   $aciklama — durum ${s%% *}"; echo "::error::Yük: $aciklama — durum ${s%% *}"; hata=1; return; }
    en="$(python3 -c "print(max(float('$en'), float('${s#* }')))")"
  done
  if python3 -c "import sys; sys.exit(0 if $en <= $sinir else 1)"; then
    printf '  TAMAM  %-52s %6.2f sn (sınır %s sn)\n' "$aciklama" "$en" "$sinir"
  else
    printf '  HATA   %-52s %6.2f sn (sınır %s sn)\n' "$aciklama" "$en" "$sinir"; echo "::error::Yük: $aciklama $en sn"; hata=1
  fi
}

echo "3) Ölçümler (1.000 veli, tek okulda)"
olc "takip tablosu (1.000 satır)"                 3 "$ADRES/yonetim" -b "$C"
olc "takip tablosu, sınıf süzgeci"                3 "$ADRES/yonetim?sinif=4-A" -b "$C"
olc "Excel (1.000 veli, bütün cevaplar)"         15 "$ADRES/yonetim?handler=excel" -b "$C"
olc "veli ayrıntısı"                              2 "$ADRES/yonetim/veli/500" -b "$C"
olc "veli: hikâye sayfası açılışı (durum)"        1 "$ADRES/api/durum?kod=$KOD&hikaye=m1"
olc "veli: hikâye sayfası (m1)"                  1 "$ADRES/v/$KOD/m1/"
SATIR="$(curl -s -b "$C" "$ADRES/yonetim" | grep -c '<tr data-veli=' || true)"
if [ "$SATIR" = 1000 ]; then echo "  TAMAM  takip tablosunda 1.000 satır"; else echo "  HATA   takip tablosu satır sayısı: $SATIR"; hata=1; fi

C2="$GECICI/cerez-s.txt"; C_ESKI="$C"; C="$C2"
giris olcum.sistem olcum-sistem-sifresi
olc "sistem yöneticisi sayfası"                   2 "$ADRES/yonetim/sistem" -b "$C"
C="$GECICI/cerez-i.txt"
giris olcum.ilce olcum-ilce-sifresi
olc "ilçe izleme sayfası"                         2 "$ADRES/yonetim/ilce" -b "$C"
C="$C_ESKI"

echo "4) Eşzamanlı kayıt: 40 veli aynı anda, bu sırada veritabanı dışarıdan okunurken (gece yedeği gibi)"
# 28 Ekim'de bulundu: WAL kipi olmadan bu durumda kayıtların neredeyse hepsi "database is locked" (500) ile kayboluyordu.
python3 - "$VeriTabani__Yol" <<'PY' > "$GECICI/kodlar.txt"
import sqlite3, sys
for (k,) in sqlite3.connect(sys.argv[1]).execute("select Kod from Veliler order by Id limit 200"): print(k)
PY
python3 -c "
import sqlite3,sys,time
t=time.time()
while time.time()-t<15:
    c=sqlite3.connect(sys.argv[1]); c.execute('begin'); c.execute('select count(*) from Cevaplar').fetchone(); time.sleep(0.05); c.commit(); c.close()
" "$VeriTabani__Yol" &
OKUYAN=$!
for tur in 1 2 3; do
  xargs -P 40 -I{} curl -s -o /dev/null -w '%{http_code}\n' -X POST -H 'Content-Type: application/json' \
    -d '{"kod":"{}","hikaye":"m1","adim":'"$tur"'}' "$ADRES/api/ilerleme" < "$GECICI/kodlar.txt"
done > "$GECICI/eszamanli.txt"
wait "$OKUYAN" || true
BASARILI="$(grep -c '^204$' "$GECICI/eszamanli.txt" || true)"
TOPLAM="$(wc -l < "$GECICI/eszamanli.txt")"
if [ "$BASARILI" = "$TOPLAM" ] && [ "$TOPLAM" = 600 ]; then echo "  TAMAM  600 eşzamanlı kaydın hepsi kabul edildi"
else echo "  HATA   eşzamanlı kayıt: $TOPLAM istekten $BASARILI başarılı"; echo "::error::Yük: eşzamanlı kayıt $BASARILI/$TOPLAM"; hata=1; fi
KIP="$(python3 -c "import sqlite3,sys; print(sqlite3.connect(sys.argv[1]).execute('pragma journal_mode').fetchone()[0])" "$VeriTabani__Yol")"
if [ "$KIP" = wal ]; then echo "  TAMAM  veritabanı WAL kipinde"; else echo "  HATA   veritabanı kipi: $KIP"; hata=1; fi

[ $hata -eq 0 ] || { echo "SONUÇ: süre sınırı aşıldı ya da kayıtlar reddedildi."; exit 1; }
echo "SONUÇ: bütün ölçümler sınırın altında, eşzamanlı kayıtların hepsi kabul edildi."
