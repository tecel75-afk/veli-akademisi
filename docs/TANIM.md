# Veli Akademisi · Site tanımı

Sürüm: 26 Ekim 2026. Bu belge, 21 Ekim 2026 tarihli site tanımının ve 26 Ekim 2026 tarihli devir notunun
yerine geçer. Kod bu belgeye göre yazılır, kabul testleri bu belgeye göre yapılır.
Belirsiz bir nokta çıkarsa varsayımda bulunulmaz; soru yöneticiye sorulur.

Belgenin tamamı, bölüm 16'daki teknik maddeler ve bölüm 13'teki gün planı dahil, 26 Ekim 2026'da
yönetici tarafından onaylanmıştır.

## 1. Amaç
Velilere yönelik 8 etkileşimli, sesli hikâye sayfası hazırdır ve seslendirilmiştir. Bu sayfalar şu an
hiçbir şey kaydetmez. Site:
- her veliye özel, kalıcı bir bağlantı verir,
- velinin hangi hikâyede nereye kadar geldiğini ve sorulara verdiği cevapları (A/B/C) kaydeder,
- yöneticinin (yalnızca rehber öğretmen) bunları görüp Excel'e aktarabileceği bir yönetim sayfası sunar.

## 2. Çalışma düzeni
- Kodu Claude yazar. Codex bu işte kullanılmaz.
- Adım adım: önce yalnızca okuma/teşhis, sonra en dar değişiklik. Her adımın çıktısı görülmeden
  sonrakine geçilmez. Doğrulanmayan şeye "doğrulandı" denmez.
- Yönetici günde yaklaşık 2 saat ayırır. Kodu birlikte okuma, bütün iş bittikten sonra, tercihen
  hafta sonu ve geniş bir zaman ayrılarak yapılır (26 Ekim kararı). Kodda kısa Türkçe yorumlar bulunur.
- Sunucu komutlarını yönetici çalıştırır (SSH ile, kendi bilgisayarından); çıktıyı paylaşır.
  Şifreler ve anahtarlar sohbete yazılmaz.
- Her adım ayrı bir kayıt (commit); açıklamalar Türkçe.

## 3. Teknoloji ve yayın düzeni
- ASP.NET Core, **.NET 10** (sunucudaki çalışma ortamı 10.0.12), C#.
  Yönetim sayfası Razor Pages; veri işlemleri küçük JSON API uçlarıyla.
- Veritabanı: SQLite (EF Core). Veritabanı dosyası `wwwroot` dışında durur.
- Excel: ClosedXML.
- **Derleme GitHub'da yapılır** (GitHub Actions, `.github/workflows/derle.yml`). Sunucuya SDK kurulmaz.
  Derlenen paket GitHub'da "Sürüm" (Release) olarak yayımlanır; sunucu onu oturum açmadan indirir.
- Sunucu: Turhost VPS, Ubuntu 24.04.5 LTS, 2 çekirdek, 3,8 GB RAM. Kestrel + nginx ters vekil + systemd.
- Adres: `https://veliakademisi.bep-uda-projeler.com` (DNS A kaydı açılacak).
- HTTPS zorunlu: Let's Encrypt (certbot), HTTP → HTTPS yönlendirme, HSTS.
- **Önizleme dönemi:** site gerçek adresinde çalışır ama nginx'te ek bir şifrenin arkasında durur.
  Yönetici onay verince şifre kaldırılır ve veli bağlantıları gönderilir ("yayına alma" budur).
- Arayüz dili Türkçe.

## 4. Depo ve lisans
- Depo: `veli-akademisi`, **herkese açık**. Eski BEP_UDA kodu hiçbir şey için referans alınmaz.
- Kod: MIT (`LICENSE`). İçerik (`icerik/`: hikâye metinleri, sayfalar, görseller): CC0 1.0 (`icerik/LICENSE`).
- Depoya **girmeyenler:**
  - ses dosyaları (ElevenLabs Yasaklı Kullanım Politikası madde 12 gereği; sunucuya ayrıca yüklenir),
  - seslendirme tabloları (`seslendirme/*.xlsx`) ve `BENIOKU.md` (ses sahiplerinin adları geçiyor),
  - veritabanı dosyaları, gizli ayarlar, şifreler.
- Okul adı depoda hiçbir yerde geçmez.
- README'de: "Görseller ve seslendirme yapay zekâ ile üretilmiştir."

## 5. Hazır içerik
Her hikâye `icerik/<kod>/` altında: `index.html` (onaylı sayfa), `steps.json` (adımlar), `img/*.webp`.
Sesler sunucuda aynı klasörün `ses/` alt klasöründedir.

| Kod | Modül | Hikâye | Adım | Soru | İdeal şıklı soru |
|---|---|---|---|---|---|
| m1 | Modül 1 · Davranış Sorunları | Tablet Kapanınca | 91 | 5 | 4 |
| m2a | Modül 2 · Mahremiyet (Senaryo A) | Elif'in Kalpleri | 71 | 4 | 3 |
| m2b | Modül 2 · Mahremiyet (Senaryo B) | Kapı Kapalıysa | 66 | 3 | 2 |
| m3 | Modül 3 · Günlük Yaşam ve Öz Bakım | Ben Yapayım, Çabuk Olsun | 67 | 3 | 2 |
| m4 | Modül 4 · Güvenli ve Bağımsız Yaşam | Markete Kadar | 71 | 3 | 2 |
| m5 | Modül 5 · Evde Rutinler ve Tutarlı Tutum | Annem Evet Dedi | 68 | 3 | 2 |
| m6 | Modül 6 · İletişim ve Sosyal Etkileşim | Ne İstediğini Nasıl Anlayacağız? | 65 | 3 | 2 |
| m7 | Modül 7 · Aileyi Güçlendirme | Ben Yorulmam Sanıyordum | 77 | 3 | 2 |

Her hikâyede ayrıca 3 düşünme sorusu vardır.

İçerikle ilgili doğrulanmış bilgiler (26 Ekim, okuma incelemesi):
- 8 sayfanın betiği aynıdır; farklar yalnızca başlık, süre ve dosya adı önekidir.
- Her sayfaya gömülü adım listesi, yanındaki `steps.json` ile birebir aynıdır.
- Son adım her hikâyede kapanış ekranıdır (`t: "close"`).
- İdeal şık `steps.json`'da sorunun `ov.i` alanındadır; `all: true` olan sorularda bütün şıklar eşittir.
- Sayfa görsellere ve seslere göreli yolla ulaşır (`img/...`, `ses/...`).
- Ses dosyası adları: adım `<kod>_sNNN.mp3`, geri bildirim `_c1.._c3`, köprü `_k1/_k2`, ortam sesi `ortam.wav`.
  Dosya yoksa sayfa tarayıcının sesiyle okur.

## 6. Veli tarafı
Veli tarafı olabildiğince sadedir: veli hikâyeleri izler ve sorulara cevap verir. Başka özellik yoktur.
- Her veliye **tek, kalıcı bağlantı**: `/v/{kod}`. `kod`: 128 bit rastgele, URL-güvenli
  (base64url, ~22 karakter), tahmin edilemez.
- Şifre, kayıt, bilgilendirme ekranı yoktur.
- Hikâye sayfasının adresi: `/v/{kod}/{hikaye}/` (bkz. 16.1).
- **Tek dokunuşla devam:** bağlantı açılınca başlangıç ekranında tek düğme bulunur:
  "Kaldığınız yerden devam edin". Dokununca veli doğrudan kaldığı adıma gider. (Telefon tarayıcıları
  dokunuş olmadan ses başlatmaya izin vermediği için tek dokunuş zorunludur.) İlk girişte düğme
  Modül 1'in başını açar.
- **Hangi hikâye açılır:** her zaman tamamlanmamış en ileri hikâye. Hepsi tamamlanmışsa son bakılan hikâye.
- **Sıralı açılma:** m1 → m2a → m2b → m3 → m4 → m5 → m6 → m7. Bir hikâyenin kapanışına ulaşılınca sıradaki
  açılır. Açılmış hikâyeler açık kalır.
- **Modüller düğmesi:** üst çubukta. Liste: 8 hikâye; her birinde "Tamamlandı / Devam ediyor / Kilitli".
  Kilitliler tıklanamaz. Tamamlanmış bir hikâye listeden açılırsa baştan başlar.
- **Hikâye sonu:** materyaller kapanış ekranının altında, ayrı bir ekran olmadan gösterilir (6a).
  "Sonraki modüle geç" alttaki sabit çubukta, "Baştan izle"nin solundadır (son hikâyede yoktur).
  (27 Ekim: sayfa içine konduğunda kaydırırken çubuğun altında kaldığı ölçüldü; bu yüzden çubuğa alındı.)
- **Yapay zekâ notu:** "Seslendirme ve görseller yapay zekâ ile hazırlanmıştır." Hikâyenin başlangıç
  ekranında, başlat düğmesinin hemen altında küçük ve soluk bir satır (27 Ekim kararı).
- Geçersiz ya da silinmiş kod: "Bu bağlantı artık geçerli değil. Lütfen rehber öğretmeninizle iletişime
  geçin." Hata ayrıntısı gösterilmez.
- Tüm veli sayfaları `noindex`.

### 6a. Materyaller ekranı
- Hikâyeye ait birkaç görsel; dokununca büyür, "İndir" ile telefona kaydedilir.
- Yazdırılabilir A4 PDF: "Kartları yazdırmak için PDF olarak indir" düğmesi (28 Ekim).
- Altında Tohum Otizm Vakfı'nın ilgili sayfasına bağlantı (yeni sekmede). *28 Ekim kararı: uygulama ilçe
  geneline yayılacağı için Tohum bağlantıları şimdilik eklenmedi; biçim destekliyor.*
- Tanım: `icerik/<kod>/materyaller.json`; görseller `icerik/<kod>/materyal/` klasöründe. Biçim:
  `{ "gorseller": [ { "dosya": "kart1.webp", "baslik": "..." } ], "pdf": { "dosya": "m1-kartlar.pdf", "yazi": "..." },
  "baglanti": { "adres": "https://...", "yazi": "..." } }` (her biri isteğe bağlı)
- 28 Ekim: rehberlik servisinin hazırladığı kartlar eklendi. Her hikâyeye kendi PDF sayfaları (m2 PDF'i: 1–2. sayfa m2a,
  3–4. sayfa m2b), sayfaların 150 dpi WebP görüntüleri ve sayfa başlıkları. PDF'ler meta verisiz yeniden yazıldı.
  Sonradan doldurulur; dosya yoksa ya da boşsa materyal kartı gösterilmez. Dosya hatalıysa (olmayan görsel,
  https olmayan adres) site açılmaz ve hatanın yeri yazılır.

### 6b. Ana ekrana ekleme (PWA)
- Site telefonun ana ekranına eklenebilir. Mağaza uygulaması yapılmaz. Ekleme isteğe bağlıdır;
  WhatsApp bağlantısı her zaman geçerli kalır.
- Her veliye kendi adresinden kendi uygulama tanımı (manifest) verilir: `start_url` ve `scope`
  `/v/{kod}/`'dur. Böylece ana ekrandaki simge her zaman o velinin bağlantısını açar.
- Kimlik yalnızca adresteki koddadır, çerezde değil (iPhone'da ana ekran uygulaması Safari'nin
  kayıtlarını görmez).
- İlk sürümde bildirim ve çevrimdışı çalışma yoktur.
- Uygulama adı ve simgesi okul müdürünün onayından sonra belirlenir. O zamana kadar ad "Veli Akademisi",
  simge geçici bir simgedir (`wwwroot/simge/`). Ad yapılandırmadan okunur (`Uygulama:Ad`); onaylanınca
  yalnızca bu ayar ve simge dosyaları değişir.
- Bilinen kısıtlar:
  - WhatsApp'ın kendi tarayıcısında "Ana ekrana ekle" yoktur; veli önce sayfayı Safari'de (iPhone)
    ya da Chrome'da (Android) açmalıdır. Velilere verilecek anlatım bu adımla başlar.
  - Yönetici bir velinin kodunu yenilerse eski simge çalışmaz; veli simgeyi silip yeni bağlantıdan
    yeniden ekler. Yönetim sayfasında kod yenileme düğmesinin yanında bu uyarı yazılıdır.

## 7. Hikâye sayfalarına yapılacak ekler
Hikâye sayfalarının görünümü, metni, akışı ve sesi **değiştirilmez**. Yapılacak eklerin tam listesi:
1. Kayıt kancası (bölüm 8).
2. Başlangıç ekranında "Kaldığınız yerden devam edin" düğmesi (bölüm 6).
3. Üst çubukta "Modüller" düğmesi.
4. Kapanıştan sonra materyaller ekranı ve "Sonraki modüle geç".
5. Uygulama tanımı (manifest) bağlantısı ve simgeler (bölüm 6b).
6. Yazı tipleri Google'dan değil sitenin kendi sunucusundan verilir (aynı yazı tipleri; görünüm değişmez).
7. Yapay zekâ notu.
8. Telefon ekranı satırı (`<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">`).
   Onaylı sayfalarda yoktur; olmadan telefon sayfayı 980 px çizip küçültür (ölçüldü: yazı 6,4 px görünür).
   27 Ekim 2026'da yönetici onayıyla eklendi.
Bu listede olmayan bir değişiklik gerekirse önce yöneticiye sorulur.

## 8. Kayıt kancası
Sayfanın betiğine küçük çağrılar eklenir. Çağrı başarısız olursa sayfa sessizce çalışmaya devam eder.
- **Adım değişince** (`render`): `POST /api/ilerleme` `{kod, hikaye, adim}`. Sunucu her veli+hikâye için
  en ileri adımı ve son adımı saklar. Toplam adım sayısını sunucu `steps.json`'dan okur (bkz. 16.3).
  Kapanış adımına ulaşılınca (`adim == toplam-1`) hikâye "tamamlandı" olur ve sıradaki açılır.
- **Sayfa açılırken:** `GET /api/durum?kod=..` → açılacak hikâye ve adım, açık hikâyeler listesi,
  her hikâyenin durumu.
- **Şık seçilince:** `POST /api/cevap` `{kod, hikaye, adim, secim}` (`secim` 0/1/2 = A/B/C).
  Her seçim zaman damgasıyla ayrı kaydedilir. "Baştan izle" ile tekrar izlenip yapılan seçimler de
  kaydedilir; "son seçim"i günceller, "ilk seçim" değişmez.
- Düşünme sorularında hiçbir şey kaydedilmez.
- Kod sayfanın adresinden okunur; sayfa kodsuz açılırsa hiçbir şey gönderilmez.
- API uçlarına hız sınırı: dakikada 120 istek/kod.

## 9. Veri modeli
- `Okul`: Id, Ad (benzersiz), Etkin, OlusturmaTarihi. *(28 Ekim: çok okullu yapı, bölüm 18)*
- `Kullanici`: Id, AdSoyad, KullaniciAdi (benzersiz), SifreOzeti, Rol (sistem yöneticisi / okul yöneticisi /
  ilçe izleyicisi), OkulId (yalnızca okul yöneticisinde), GeciciSifre, Etkin, GuvenlikDamgasi, OlusturmaTarihi, SonGiris.
- `Veli`: Id, OkulId, Ad (ör. "Ayşe Hanım"), Sinif (serbest metin, ör. "4-A"), Kod (benzersiz), OlusturmaTarihi, SonGiris.
- `Ilerleme`: VeliId, Hikaye, EnIleriAdim, SonAdim, Tamamlandi, IlkGiris, SonGuncelleme.
- `Cevap`: Id, VeliId, Hikaye, Adim, Secim, Zaman.
- Soru metinleri, şıklar ve ideal şık veritabanına yazılmaz; `steps.json`'dan okunur.
- Şema değişiklikleri EF Core şema güncellemeleriyle (Migrations, `Veri/Gecisler`) yapılır; veriler korunur.

## 10. Kişisel veriler (KVKK)
- Tutulmaz: telefon numarası, T.C. kimlik no, çocuğun adı, tanısı, IP adresi kaydı.
  nginx erişim günlüklerinde IP kısaltılır ya da günlükler 7 günle sınırlanır.
- Yazı tipleri sitenin kendi sunucusundan verilir; veli sayfaları dış bir sunucuya istek göndermez
  (Tohum bağlantısı veli dokununca açılır).
- Veliye bağlantı gönderilmeden önce veli onam metni okul müdürü tarafından onaylanır.
- Yöneticiler için yalnızca ad soyad ve kullanıcı adı tutulur; telefon ve e-posta tutulmaz.
- Sistem yöneticisi ve ilçe izleyicisi veli adı, sınıfı, kodu ve cevabı görmez (bölüm 18).

## 11. Yönetim sayfası (`/yonetim`)
*28 Ekim'de çok okullu yapıya geçildi; roller ve okul ayrımı bölüm 18'de. Bu bölüm okul yöneticisinin ekranlarını anlatır.*
- Kişisel hesaplar: kullanıcı adı + şifre (yalnızca özeti tutulur), çerez oturumu; aynı kullanıcı adıyla
  5 hatalı denemede o ad 15 dakika kilitlenir.
- **Veli ekle:** Ad + Sınıf → kod üretilir → tam bağlantı + "Kopyala". Toplu ekleme: her satıra "Ad;Sınıf".
- **Takip tablosu:** satır = veli, sütun = 8 hikâye; hücre: Başlanmadı / %ilerleme / Tamamlandı.
  Sınıfa göre süzme, ada göre arama.
- **Veli ayrıntısı:** her sorunun metni, ilk ve son seçim, zamanları. Bağlantıyı yeniden kopyalama.
  Yeni kod üretme (eski bağlantı ve ana ekran simgesi geçersiz olur; uyarı yazılır).
- **Excel'e aktar:** iki sayfa.
  1. Takip tablosu.
  2. Cevaplar: her soru tek satır — veli, sınıf, hikâye, soru no, soru metni, ilk seçim (harf ve metin),
     ilk seçim zamanı, son seçim (harf ve metin), son seçim zamanı, "ideal şık seçildi mi?"
     (son seçime göre Evet/Hayır; bütün şıkları eşit sorularda boş).
- **Silme:** tek veli (tüm kayıtlarıyla); "Tüm velileri ve kayıtları sil" (yıl sonu; iki kez onay; yalnızca kendi okulu).
- **Yedeği indir:** 28 Ekim'de okul ekranından kaldırıldı (tam yedek bütün okulların verisini içerir);
  şifreli haftalık yedeği sistem yöneticisi indirir (bölüm 18).
- **Yöneticiler:** okulun yöneticileri; yönetici ekleme, şifresini unutana geçici şifre, hesap kapatma.
- Telefonda da kullanılabilir.

## 12. Kurulum ve işletim
- `deploy/` altında: systemd birim dosyası, nginx site yapılandırması, `KURULUM.md`.
- Sunucu komutları SSH ile çalıştırılır. Ses dosyaları sunucuya SFTP ile (ör. WinSCP) yüklenir.
- Günlük yedek: `cron` ile, son 14 gün tutulur (yöntem: 16.4).
- Yapılandırma `appsettings.Production.json` + ortam değişkenleri; gizli bilgiler depoya girmez. Ayarlar:
  `Site:Adres` (bağlantıların başı, ör. https://veliakademisi.bep-uda-projeler.com), `Yonetim:SifreHash`
  (`sifre-hash` komutuyla üretilir), `VeriTabani:Yol`, `Icerik:Yol`, `Ses:Yol`, `Uygulama:Ad`.
- Her hikâyenin ses klasöründe `ortam.wav` bulunmalıdır (yoksa sayfa açılışta hata verir; 26 Ekim testi).
- Kurulumdan önce: `apt update`; `bepadmin` kullanıcısının durumu; eski SSL sertifikaları;
  güvenlik duvarı; root'a şifreyle SSH girişinin kapatılması.

## 13. Gün planı (her gün yöneticinin onayıyla)
1. Gün: proje iskeleti, veritabanı, veli bağlantısı `/v/{kod}`, kayıt kancası.
   Günün sonunda ya da 2. Günün başında: sunucu kurulumu ve önizleme (şifre arkasında).
2. Gün: kaldığı yerden devam, sıralı açılma, Modüller düğmesi.
3. Gün: yönetim sayfası, Excel'e aktarma, yedek indirme.
4. Gün: PWA, yazı tipleri, yapay zekâ notu, materyaller ekranı.
5. Gün: kabul testleri (bölüm 14).
Kod okuma: bütün iş bittikten sonra, tercihen hafta sonu (26 Ekim kararı).

## 14. Kabul ölçütleri
1. Yönetici veli ekler → bağlantı oluşur → telefonda açılır; tek dokunuşla Modül 1'in başı gelir;
   diğer hikâyeler kilitli.
2. Bir hikâyede ilerlenir, bir soruda B seçilir → takip tablosunda ilerleme, ayrıntıda "B" görünür.
3. Hikâye kapanışa kadar izlenir → "Tamamlandı", materyaller ekranı çıkar, sıradaki hikâye açılır.
4. Veli sayfayı kapatıp bağlantıyı yeniden açar → tek dokunuşla kaldığı adımdan devam eder;
   Modüller listesinden bitmiş hikâyeye dönebilir, o hikâye baştan başlar.
5. Excel iki sayfayla iner; Türkçe karakterler doğru; ilk/son seçim ve ideal şık sütunları doğru.
6. Veli silinir → bağlantısı "geçerli değil" mesajı verir.
7. Hikâye sayfaları onaylı hâliyle aynı görünür ve çalışır; sunucudaki sesler çalar.
8. Site yalnızca HTTPS ile açılır; `/yonetim` şifresiz açılmaz.
9. iPhone'da ve Android'de ana ekrana eklenen simge velinin kendi bağlantısını açar ve kaldığı yerden
   devam ettirir.
10. WhatsApp'ın kendi tarayıcısında sayfa açılır, ses çalar, materyal indirilebilir.
Hangi ölçütün otomatik testle, hangisinin elle sınanacağı: bkz. 16.5.

**Gerçek telefonda sınananlar (sunucuda, önizleme sırasında):**
- 27 Ekim, iPhone + WhatsApp: ölçüt 1, 2, 3, 4, 7 (ses çaldı), 10 (WhatsApp tarayıcısında açılma ve ses).
- 28 Ekim, iPhone: materyaller (kart görselleri büyüyor, PDF iniyor ve açılıyor; ölçüt 3 ve 10'un materyal kısmı).
- 28 Ekim, iPhone (Safari → Ana Ekrana Ekle): ölçüt 9 — simge göründü, tam ekran açıldı, kaldığı hikâyeye götürdü.
  Önizleme şifresi ana ekran uygulamasında yeniden soruluyor; yayına alınınca bu şifre kalkacağı için velileri etkilemez.
- 28 Ekim, Android (Chrome, Kurul üyesinin telefonu): ses çaldı; ana ekrana simge olarak eklendi; kapatıp simgeden
  açınca kaldığı yerden devam etti (ölçüt 9 ve 7). Android'de WhatsApp'ın kendi tarayıcısı ayrıca denenmedi.

## 15. Kapsam dışı
Veli için ek özellikler (profil, mesajlaşma, bildirim), veli şifresi, SMS/WhatsApp gönderimi,
öğretmen hesapları, düşünme sorusu yazı alanı, ses kaydı, bilgilendirme ekranı, çevrimdışı çalışma.

## 16. Teknik maddeler (onaylandı, 26 Ekim 2026)
1. **Hikâye adresi** `/v/{kod}/{hikaye}/` olur (sondaki `/` dahil). Sayfadaki göreli `img/` ve `ses/`
   yolları bu sayede değişmeden çalışır (26 Ekim incelemesinde doğrulandı).
2. **Kilit sunucuda da uygulanır:** kilitli bir hikâyenin adresini elle yazan veli onu açamaz.
3. **Toplam adım sayısı tarayıcıdan alınmaz**, sunucu `steps.json`'dan okur. Excel'deki **soru no**,
   hikâyedeki çoktan seçmeli soruların sırasıdır (1, 2, 3…; düşünme soruları sayılmaz).
4. **SQLite yedeği** `sqlite3 .backup` ile alınır (site çalışırken düz kopya bozuk dosya üretebilir).
5. **Testlerin ayrımı:** ölçüt 1–6 ve 8 otomatik testle; 7, 9 ve 10 gerçek telefonda elle, kontrol listesiyle.
6. **Giriş kilidi yalnızca bellekte tutulur;** IP adresi diske yazılmaz.
7. **`Referrer-Policy: no-referrer`:** dış bağlantılara (Tohum) velinin kişisel kodu taşınmaz.

## 17. Açık işler (yöneticide)
- Okul müdürünün onayı: uygulama adı ve simgesi; veli onam metni.
- `materyaller.json` dosyalarının içerikleri.
- Yapay zekâ notunun yeri.
- ElevenLabs'in "devlet kurumu kullanımı" maddesi şirkete yazılı soruldu (26 Ekim 2026). Şirket yorum
  yapmadı; risk yönetici tarafından 26 Ekim 2026'da kabul edildi. Abonelik sırasında üretilen içeriğin
  ticari lisansı abonelik bittikten sonra da süresiz geçerlidir (ElevenLabs yardım sayfası). Abonelik
  sonrası üretilecek seslerde atıf gerekir; eksik ses kalmaması için iptal tarihi: 13 Kasım 2026.

## 17a. Anlatıcı sesinin güncellenmesi (28 Ekim 2026)
- Kurul üyesinin eleştirisi: anlatıcı Nursel'in sesi üzüntülü yerlerde aşırı duygusal ve yapmacık. Karar: Nursel'in
  bütün satırları daha nötr, duygusu hafif bir tonla yeniden üretildi (ElevenLabs; stability 0.65, style 0.3).
- 501 dosya: kareler (başka karakterlerin tırnak içindeki 48 sözü hariç), köprü cümleleri, bölüm başlıkları, kapanışlar.
  Adlar, süreler ve dosyaların açılması sunucuya gönderilmeden denetlendi; `kurulum.sh ses-guncelle` ile kuruldu.
- Yedekler sunucuda: `/var/backups/veliakademisi/ses-28ekim-oncesi.tgz` (bütün sesler) ve
  `/var/backups/veliakademisi/ses-2026-10-28-205922/` (değişen 501 dosyanın eski hâli).
- Telefonda dinlendi (iPhone, ana ekran simgesinden): yeni sesler çalıyor, ton uygun bulundu.

## 18. Çok okullu yapı (28 Ekim 2026)
Okul müdürünün görüşmesinden sonra uygulamanın ilçedeki okullara yayılması gündeme geldi; kod İlçe MEM kararı
beklenmeden yazıldı, yayına alma İlçe MEM onayından sonra yapılır. Tasarım belgesi: "Veli Akademisi · Çok Okullu
Yapı Tasarım Taslağı". Kararlar (28 Ekim):
1. **Tek kurulum, çok okul.** Veli tarafı değişmez. Her okul yalnızca kendi velilerini görür.
2. **Roller:** sistem yöneticisi (sunucu sorumlusu; okul açar/pasife alır, ilk iki yöneticiyi ve ilçe izleyicisini
   açar, haftalık yedeği indirir; veli verisi görmez), okul yöneticisi (kendi okulunun veli işleri ve yöneticileri),
   ilçe izleyicisi (İlçe MEM'de projelere bakan kişi; okul başına isimsiz sayılar).
3. **Okul başına en az iki açık yönetici;** iki olmadan veli eklenemez. Şifresini unutana diğer yönetici geçici
   şifre verir; e-postayla şifre sıfırlama yoktur.
4. **Okul ayrımı tek yerde** (veritabanı katmanında genel süzgeç): kapsam verilmeyen istekte veli verisi hiç
   görünmez. Başka okulun velisinin adresi "bulunamadı" döner.
5. **Pasif okul:** yöneticileri giremez, velilerinin bağlantıları çalışmaz; veriler silinmez.
6. **Kapasite:** en fazla 1.000 veli; her derlemede 1.000 veliyle yük ölçümü yapılır (`.github/yuk-olcumu.sh`).
7. **Sunucu dışı yedek:** sistem yöneticisi haftada bir şifreli (AES-256, 7-Zip ile açılan) tam yedeği indirir;
   okulun bilgisayarında ya da kilitli USB bellekte saklanır, son 4 hafta tutulur; yurt dışındaki bulut
   hizmetleri kullanılmaz. Şifre saklanmaz; 7 gün geçerse ekranda hatırlatma çıkar.
8. **Geçiş:** 27 Ekim'de kurulan veritabanında yalnızca deneme verisi olduğu doğrulanırsa yedeği alınıp yeni yapıyla
   boş kurulur (KURULUM.md, "Çok okullu sürüme geçiş").
