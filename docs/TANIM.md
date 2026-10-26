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
- **Hikâye sonu:** kapanış ekranından sonra materyaller ekranı (6a), ardından "Sonraki modüle geç"
  (son hikâyede yoktur).
- **Yapay zekâ notu:** veli tarafında "Seslendirme ve görseller yapay zekâ ile hazırlanmıştır." notu
  görünür. Yeri kodlama sırasında yöneticiyle belirlenir.
- Geçersiz ya da silinmiş kod: "Bu bağlantı artık geçerli değil. Lütfen rehber öğretmeninizle iletişime
  geçin." Hata ayrıntısı gösterilmez.
- Tüm veli sayfaları `noindex`.

### 6a. Materyaller ekranı
- Hikâyeye ait birkaç görsel; dokununca büyür, "İndir" ile telefona kaydedilir.
- Altında Tohum Otizm Vakfı'nın ilgili sayfasına bağlantı (yeni sekmede).
- Tanım: `icerik/<kod>/materyaller.json` (görsel dosya adları, kısa başlıklar, dış bağlantı).
  Sonradan doldurulur; dosya yoksa ya da boşsa materyaller ekranı atlanır.

### 6b. Ana ekrana ekleme (PWA)
- Site telefonun ana ekranına eklenebilir. Mağaza uygulaması yapılmaz. Ekleme isteğe bağlıdır;
  WhatsApp bağlantısı her zaman geçerli kalır.
- Her veliye kendi adresinden kendi uygulama tanımı (manifest) verilir: `start_url` ve `scope`
  `/v/{kod}/`'dur. Böylece ana ekrandaki simge her zaman o velinin bağlantısını açar.
- Kimlik yalnızca adresteki koddadır, çerezde değil (iPhone'da ana ekran uygulaması Safari'nin
  kayıtlarını görmez).
- İlk sürümde bildirim ve çevrimdışı çalışma yoktur.
- Uygulama adı ve simgesi okul müdürünün onayından sonra belirlenir.
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

## 9. Veri modeli (öneri)
- `Veli`: Id, Ad (ör. "Ayşe Hanım"), Sinif (serbest metin, ör. "4-A"), Kod (benzersiz), OlusturmaTarihi, SonGiris.
- `Ilerleme`: VeliId, Hikaye, EnIleriAdim, SonAdim, Tamamlandi, IlkGiris, SonGuncelleme.
- `Cevap`: Id, VeliId, Hikaye, Adim, Secim, Zaman.
- Soru metinleri, şıklar ve ideal şık veritabanına yazılmaz; `steps.json`'dan okunur.

## 10. Kişisel veriler (KVKK)
- Tutulmaz: telefon numarası, T.C. kimlik no, çocuğun adı, tanısı, IP adresi kaydı.
  nginx erişim günlüklerinde IP kısaltılır ya da günlükler 7 günle sınırlanır.
- Yazı tipleri sitenin kendi sunucusundan verilir; veli sayfaları dış bir sunucuya istek göndermez
  (Tohum bağlantısı veli dokununca açılır).
- Veliye bağlantı gönderilmeden önce veli onam metni okul müdürü tarafından onaylanır.

## 11. Yönetim sayfası (`/yonetim`)
- Tek yönetici. Şifreli giriş (şifrenin hash'i yapılandırmada; depoda düz şifre yok), çerez oturumu,
  5 hatalı denemede 15 dakika kilit.
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
- **Silme:** tek veli (tüm kayıtlarıyla); "Tüm velileri ve kayıtları sil" (yıl sonu; iki kez onay).
- **Yedeği indir:** veritabanının güncel yedeğini indirir; yönetici haftada bir kendi bilgisayarına kaydeder.
- Telefonda da kullanılabilir.

## 12. Kurulum ve işletim
- `deploy/` altında: systemd birim dosyası, nginx site yapılandırması, `KURULUM.md`.
- Sunucu komutları SSH ile çalıştırılır. Ses dosyaları sunucuya SFTP ile (ör. WinSCP) yüklenir.
- Günlük yedek: `cron` ile, son 14 gün tutulur (yöntem: 16.4).
- Yapılandırma `appsettings.Production.json` + ortam değişkenleri; gizli bilgiler depoya girmez.
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
