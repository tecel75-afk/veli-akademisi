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
   Önce **önizleme** kullanıcı adı ve şifresi (`onizleme` + önizleme şifresi), sonra **yönetim** şifresi istenir.
2. "Veli ekle" ile kendinize bir deneme velisi ekleyin, bağlantıyı kopyalayıp WhatsApp'tan kendi telefonunuza gönderin.
3. Telefonda bağlantıyı açın (önizleme şifresi istenir). Hikâyeyi başlatın, bir soruda şık seçin.
4. Yönetim sayfasında ilerlemeyi ve seçimi görün; Excel'i indirin.

Kabul ölçütlerinin (TANIM.md bölüm 14) telefonla yapılanlarını bu aşamada birlikte işaretleriz.

## Her zaman kullanılabilecek komutlar

| Komut | Ne yapar |
|---|---|
| `sudo bash /root/kurulum/kurulum.sh durum` | Servisin durumu, sürüm, son günlük satırları |
| `sudo veliakademisi-guncelle` | GitHub'daki son sürümü kurar; açılmazsa önceki sürüme kendiliğinden döner |
| `sudo veliakademisi-yedek` | Hemen bir yedek alır (`/var/backups/veliakademisi/`) |

## Sonraki ayrı işler (şimdi değil)

- **Yayına alma:** önizleme şifresinin kaldırılması (müdür onayı, onam metni ve deneme bittikten sonra).
- **SSH güvenliği:** root'un şifreyle girişinin kapatılması. Bilgisayarınızda bir SSH anahtarı oluşturulur, sunucuya
  tanıtılır, sonra şifreyle giriş kapatılır. Yanlış yapılırsa sunucuya SSH ile girilemez (VNC her zaman çalışır);
  bu yüzden ayrı ve dikkatli bir adım olarak yapılacak.
- Eski kurulumdan kalanlar: `bepadmin` kullanıcısı ve eski SSL sertifikaları (devir notu, 25 Ekim) incelenip
  gerekiyorsa temizlenecek.

## Sunucudaki yerler (bilgi için)

| Ne | Nerede |
|---|---|
| Uygulama | `/opt/veliakademisi/uygulama` (bir önceki sürüm: `/opt/veliakademisi/onceki`) |
| Sesler | `/opt/veliakademisi/ses/<hikâye>/` |
| Veritabanı ve oturum anahtarları | `/var/lib/veliakademisi/` |
| Ayarlar (site adresi, şifre özeti) | `/etc/veliakademisi/ayarlar.env` |
| Gece yedekleri (14 gün) | `/var/backups/veliakademisi/` |
| nginx günlükleri (7 gün; IP kısaltılmış, veli kodu gizli) | `/var/log/nginx/veliakademisi-*.gunluk` |
