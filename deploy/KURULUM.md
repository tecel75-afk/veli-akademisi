# Veli Akademisi · Sunucu kurulumu (önizleme)

Bu kılavuz, siteyi sunucuya **şifreli önizleme** olarak kurar. Siteyi siz ve yalnızca önizleme şifresini
bilenler görebilir. Velilere bağlantı gönderilmez; "Yayına alma" ayrı ve sonraki bir iştir.

**Çalışma biçimi:** Her adımda tek komut yazılır. Komutun çıktısını (ekrandaki yazıları) seçip kopyalayarak
sohbete yapıştırırsınız. Her şey yolundaysa sonraki adıma geçeriz. Bir adım `HATA` yazarsa orada durulur.
**Şifreler sohbete yazılmaz.**

Bu betikler, sunucudaki adımların aynısıyla GitHub'ın Ubuntu 24.04 makinesinde baştan sona denendi
(kurulum provası). Denenemeyen iki adım var: HTTPS (gerçek alan adı gerekir) ve güvenlik duvarı.

---

## 0) Gerekenler

- Sunucunun IP adresi ve root şifresi (Turhost'tan).
- Windows bilgisayar ve **PowerShell**. Başlat menüsüne "PowerShell" yazıp açın.
  - PowerShell'de yapıştırmak için: **sağ tık** ya da **Ctrl+V**.
  - Seçip kopyalamak için: fareyle seçin, **Enter** ya da **sağ tık**.
- Ses paketi: `Veli_Akademisi_8_Hikaye_Sesli.zip` (bilgisayarınızda).

## 1) DNS kaydı (Turhost paneli)

`bep-uda-projeler.com` alan adının DNS ayarlarına yeni bir **A kaydı** eklenir:

| Ad (host) | Tür | Değer |
|---|---|---|
| `veliakademisi` | A | `109.232.220.177` (VPS'in adresi; Turhost'ta doğrulandı) |

Turhost panelindeki menünün adını bilmiyorum. Bulamazsanız ekranın görüntüsünü gönderin, birlikte bakalım.
Kaydın internete yayılması birkaç dakika ile birkaç saat sürebilir. Bu yüzden DNS kaydı ilk iş yapılır.
HTTPS adımı (6. adım) kaydın yayılmasını bekler. Diğer adımlar beklemez.

## 2) Sunucuya bağlanma

PowerShell'de (bu sunucuda SSH 22 değil **23422** numaralı kapıyı kullanıyor; Turhost "Erişim Bilgileri"):

```
ssh -p 23422 root@109.232.220.177
```

İlk bağlantıda `Are you sure you want to continue connecting` diye sorar: `yes` yazıp Enter'a basın.
Şifre sorulunca root şifresini yazın. Yazarken ekranda **hiçbir şey görünmez**, bu normaldir.

## 3) Kurulum dosyalarını indirme

```
cd /root && curl -fsSLO https://github.com/tecel75-afk/veli-akademisi/releases/download/son/kurulum.zip && python3 -m zipfile -e kurulum.zip kurulum && cd kurulum && ls
```

Beklenen: 8 dosya adı (`kurulum.sh`, `guncelle.sh`, `KURULUM.md` …).

## 4) Adım adım kurulum

Her satırı ayrı çalıştırın; çıktısını gönderin.

| # | Komut | Ne yapar |
|---|---|---|
| 4.1 | `sudo bash kurulum.sh hazirlik` | Gerekli paketleri kurar; .NET 10 ve nginx'i denetler. |
| 4.2 | `sudo bash kurulum.sh uygulama` | Servis kullanıcısını, klasörleri ve ayar dosyasını oluşturur; son sürümü GitHub'dan indirip kurar. |
| 4.3 | `sudo bash kurulum.sh nginx` | nginx ayarını kurar. **Önizleme şifresi** sorulur (kullanıcı adı: `onizleme`); iki kez yazılır, görünmez. |
| 4.4 | `sudo bash kurulum.sh yonetici` | **Sistem yöneticisi hesabı**: ad soyad, kullanıcı adı ve şifre (en az 10 karakter) sorulur; şifre iki kez yazılır, görünmez. |
| 4.5 | `sudo bash kurulum.sh yedek` | Her gece 03:30'da otomatik yedek kurar ve ilk yedeği hemen alır. |

Önizleme şifresi ile sistem yöneticisi şifresi **farklı** olsun. İkisini de bir yere not edin.

## 5) Sesleri yükleme

**Yeni bir PowerShell penceresi** açın (sunucuya bağlı olanı kapatmayın). Bu pencere sizin bilgisayarınızdadır.
Dosyanın yerini kendi bilgisayarınıza göre yazın (örnek):

```
scp -P 23422 "C:\Users\KULLANICI\Downloads\Veli_Akademisi_8_Hikaye_Sesli.zip" root@109.232.220.177:/root/
```

Yükleme bitince sunucuya bağlı pencerede:

```
sudo bash kurulum.sh sesler /root/Veli_Akademisi_8_Hikaye_Sesli.zip
```

Beklenen: `mp3: 729   ortam.wav: 8` ve `TAMAM`.

## 6) HTTPS (DNS kaydı yayıldıktan sonra)

```
sudo bash kurulum.sh https
```

DNS kaydı henüz görünmüyorsa betik bunu söyler ve durur. O durumda bir süre bekleyip yeniden deneriz.

## 7) Güvenlik duvarı

```
sudo bash kurulum.sh guvenlik-duvari
```

Yalnızca SSH, HTTP (80) ve HTTPS (443) açık kalır. Betik SSH'nin gerçekte kullandığı kapıyı (bu sunucuda 23422)
sunucuya sorar ve önce onu açar; bulamazsa güvenlik duvarını hiç açmadan durur. Çıktıda `SSH kapısı: 23422`
satırını mutlaka görün.
Yine de bir sorun olursa Turhost'un VNC konsolundan sunucuya her zaman girilebilir.

## 8) Deneme

1. Bilgisayarda tarayıcıyla `https://veliakademisi.bep-uda-projeler.com/yonetim` adresini açın.
   Önce **önizleme** kullanıcı adı ve şifresi (`onizleme` + önizleme şifresi), sonra **sistem yöneticisi**
   kullanıcı adı ve şifresi istenir. Sistem yöneticisi sayfası (Okullar) açılır.
2. "Okul ekle" ile okulu, ilk iki yöneticisiyle birlikte ekleyin. İki geçici şifre bir kez gösterilir; not alın.
3. Okul yöneticisi olarak girin (başka bir tarayıcıda ya da gizli pencerede): geçici şifreyle girince önce kendi
   şifrenizi belirlersiniz. "Veli ekle" ile bir deneme velisi ekleyin, bağlantıyı WhatsApp'tan telefonunuza gönderin.
4. Telefonda bağlantıyı açın (önizleme şifresi istenir). Hikâyeyi başlatın, bir soruda şık seçin.
5. Okul yöneticisi sayfasında ilerlemeyi ve seçimi görün; Excel'i indirin.
6. Sistem yöneticisi sayfasında "Haftalık yedek" bölümünden şifreli yedeği indirin; 7-Zip ile açılabildiğini görün.

Kabul ölçütlerinin (TANIM.md bölüm 14) telefonla yapılanlarını bu aşamada birlikte işaretleriz.

## Roller (kim neyi yapar)

| Rol | Ne yapar | Ne göremez |
|---|---|---|
| Sistem yöneticisi (sunucu sorumlusu) | Okul ekler / pasife alır; okulun ilk iki yöneticisini açar; ilçe izleyicisi hesabı açar; haftalık şifreli yedeği indirir | Veli adı, sınıfı, ilerlemesi, cevabı |
| Okul yöneticisi (okul başına en az iki) | Kendi okulunun velileri, takip tablosu, Excel, yıl sonu silme; okulun yöneticileri (ekleme, geçici şifre, kapatma) | Başka okulların hiçbir bilgisi |
| İlçe izleyicisi | Okul başına isimsiz sayılar (veli, başlayan, hikâye başına tamamlayan) | Veli adı, sınıfı, kodu, cevabı |

Şifresini unutan okul yöneticisine okulun **diğer yöneticisi** geçici şifre verir. Sistem yöneticisi bunu yalnızca
son çare olarak (iki yönetici de ulaşılamıyorsa) okulun ayrıntı sayfasından yapar.

## Her zaman kullanılabilecek komutlar

| Komut | Ne yapar |
|---|---|
| `sudo bash /root/kurulum/kurulum.sh durum` | Servisin durumu, sürüm, son günlük satırları |
| `sudo veliakademisi-guncelle` | GitHub'daki son sürümü kurar; açılmazsa önceki sürüme kendiliğinden döner |
| `sudo veliakademisi-yedek` | Hemen bir yedek alır (`/var/backups/veliakademisi/`) |
| `sudo bash /root/kurulum/kurulum.sh okul-ekle "Okulun adı"` | Okul ekler (normalde sistem yöneticisi sayfasından yapılır) |

## Çok okullu sürüme geçiş (27 Ekim'de kurulan sunucu için, bir kez)

27 Ekim'de kurulan veritabanı eski yöntemle (şema güncelleme kaydı olmadan) kuruldu. Çok okullu sürüm böyle bir
veritabanında **açılmaz ve ona dokunmaz**; güncelleme betiği de kendiliğinden önceki sürüme döner. Geçiş, içinde yalnızca
deneme verisi olduğu doğrulandıktan sonra (28 Ekim kararı) şöyle yapılır; her adım ayrı çalıştırılır, çıktısı kontrol edilir:

1. Veritabanında ne olduğuna bakılır (yalnızca okuma): `sudo sqlite3 /var/lib/veliakademisi/veliakademisi.db "select Id, Ad, Sinif from Veliler"`
2. Yedek alınır: `sudo veliakademisi-yedek`
3. Servis durdurulur, eski veritabanı silinmeden kenara alınır (`veliakademisi.db.eski-27ekim`).
4. Yeni sürüm kurulur: `sudo veliakademisi-guncelle` (site boş veritabanıyla açılır).
5. Sistem yöneticisi hesabı açılır: `sudo bash /root/kurulum/kurulum.sh yonetici`
6. Eski ayar satırı `Yonetim__SifreHash` artık kullanılmaz; ayar dosyasından silinir.
7. 8) Deneme bölümündeki adımlar yapılır.

## Yapılan güvenlik işleri (28 Ekim 2026)

- **SSH yalnızca anahtarla:** `/etc/ssh/sshd_config.d/00-veliakademisi.conf` (şifreyle giriş kapalı, root yalnızca
  anahtarla). Anahtar yöneticinin bilgisayarında (`id_ed25519`, parolalı). Root şifresi değişmedi: Turhost'un VNC
  ekranından girmek içindir. Anahtar kaybolursa VNC'den girilip yeni anahtar eklenir.
- Eski kurulumdan kalanlar: `github-actions-bep` anahtarı silindi (önceki liste: `/root/.ssh/authorized_keys.yedek-28ekim`);
  `bepadmin` hesabı kilitlendi (açmak için: `usermod --unlock --expiredate '' bepadmin`).
- Kaldırılanlar: Google Chrome ve deposu, Chromium, CUPS (yazıcı servisi) ve masaüstü paketleri (126 paket).
- Kalanlar (BEP_UDA'nın yeniden kurulumunda karar verilecek): `bep-uda-projeler.com` sertifikası ve nginx'in `default` sitesi.

## Sonraki ayrı işler (şimdi değil)

- **Yayına alma:** önizleme şifresinin kaldırılması (İlçe MEM ve müdür onayı, onam metni ve deneme bittikten sonra).

## Sunucudaki yerler (bilgi için)

| Ne | Nerede |
|---|---|
| Uygulama | `/opt/veliakademisi/uygulama` (bir önceki sürüm: `/opt/veliakademisi/onceki`) |
| Sesler | `/opt/veliakademisi/ses/<hikâye>/` |
| Veritabanı ve oturum anahtarları | `/var/lib/veliakademisi/` |
| Ayarlar (site adresi, klasörler) | `/etc/veliakademisi/ayarlar.env` |
| Son haftalık yedek indirme zamanı | `/var/lib/veliakademisi/son-yedek.txt` |
| Gece yedekleri (14 gün) | `/var/backups/veliakademisi/` |
| nginx günlükleri (7 gün; IP kısaltılmış, veli kodu gizli) | `/var/log/nginx/veliakademisi-*.gunluk` |
