using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi;

// Sunucuda çalıştırılan komutlar (site açılmaz; komut çalışır ve çıkar):
//   yonetici-ekle                                → sistem yöneticisi hesabı açar (ad, kullanıcı adı ve şifre sorulur;
//                                                  şifre ekranda görünmez). Kurulumda bir kez kullanılır.
//   okul-ekle "Okulun adı"                       → okul ekler, okul numarasını yazar.
//   okul-yoneticisi-ekle <okul no> "Ad Soyad" <kullanıcı adı>
//                                                → okul yöneticisi hesabı açar, geçici şifreyi bir kez yazar.
//   veli-ekle "Ad" "Sınıf" <okul no>             → veli ekler, kodunu ve bağlantı yolunu yazar.
// Okul ve yönetici işleri yönetim ekranlarından da yapılır; komutlar kurulum, deneme ve zor durumlar içindir.
public static class Komutlar
{
    static readonly string[] Adlar = { "yonetici-ekle", "okul-ekle", "okul-yoneticisi-ekle", "veli-ekle" };

    public static bool VarMi(string ad) => Adlar.Contains(ad);

    public static int Calistir(string[] a, VeriTabani db)
    {
        switch (a[0])
        {
            case "yonetici-ekle" when a.Length == 1: return YoneticiEkle(db);
            case "okul-ekle" when a.Length == 2: return OkulEkle(db, a[1]);
            case "okul-yoneticisi-ekle" when a.Length == 4: return OkulYoneticisiEkle(db, a[1], a[2], a[3]);
            case "veli-ekle" when a.Length == 4: return VeliEkle(db, a[1], a[2], a[3]);
        }
        Console.Error.WriteLine("Kullanım: yonetici-ekle | okul-ekle \"Okulun adı\" | okul-yoneticisi-ekle <okul no> \"Ad Soyad\" <kullanıcı adı> | veli-ekle \"Ad\" \"Sınıf\" <okul no>");
        return 1;
    }

    static int Hata(string ileti) { Console.Error.WriteLine(ileti); return 1; }

    static int YoneticiEkle(VeriTabani db)
    {
        var adSoyad = Oku("Ad soyad: ", gizli: false).Trim();
        var ad = Oturum.KullaniciAdiDuzelt(Oku("Kullanıcı adı: ", gizli: false));
        if (adSoyad.Length is 0 or > 100) return Hata("Ad soyad boş olamaz, en çok 100 karakter olabilir.");
        if (!Oturum.KullaniciAdiGecerli(ad)) return Hata("Kullanıcı adı 3–40 karakter olmalı; yalnızca a-z, 0-9, nokta, tire, alt çizgi.");
        if (db.Kullanicilar.Any(k => k.KullaniciAdi == ad)) return Hata("Bu kullanıcı adı zaten var.");
        var sifre = Oku("Şifre (en az 10 karakter): ", gizli: true);
        if (sifre.Length < Oturum.SifreEnAz) return Hata($"Şifre en az {Oturum.SifreEnAz} karakter olmalı.");
        if (!Console.IsInputRedirected && Oku("Şifre (tekrar): ", gizli: true) != sifre) return Hata("İki şifre aynı değil.");
        db.Kullanicilar.Add(Hesaplar.Yeni(adSoyad, ad, Rol.SistemYoneticisi, null, sifre, gecici: false));
        db.SaveChanges();
        Console.WriteLine($"Sistem yöneticisi hesabı açıldı: {ad}");
        return 0;
    }

    static int OkulEkle(VeriTabani db, string ad)
    {
        ad = ad.Trim();
        if (ad.Length is 0 or > 150) return Hata("Okulun adı boş olamaz, en çok 150 karakter olabilir.");
        if (db.Okullar.Any(o => o.Ad == ad)) return Hata("Bu adla bir okul zaten var.");
        var okul = new Okul { Ad = ad, OlusturmaTarihi = DateTime.UtcNow };
        db.Okullar.Add(okul);
        db.SaveChanges();
        Console.WriteLine($"Okul eklendi: {okul.Ad}");
        Console.WriteLine($"Okul no: {okul.Id}");
        return 0;
    }

    static int OkulYoneticisiEkle(VeriTabani db, string okulNo, string adSoyad, string kullaniciAdi)
    {
        var okul = int.TryParse(okulNo, out var no) ? db.Okullar.Find(no) : null;
        if (okul is null) return Hata("Bu numarada bir okul yok.");
        adSoyad = adSoyad.Trim();
        var ad = Oturum.KullaniciAdiDuzelt(kullaniciAdi);
        if (adSoyad.Length is 0 or > 100) return Hata("Ad soyad boş olamaz, en çok 100 karakter olabilir.");
        if (!Oturum.KullaniciAdiGecerli(ad)) return Hata("Kullanıcı adı 3–40 karakter olmalı; yalnızca a-z, 0-9, nokta, tire, alt çizgi.");
        if (db.Kullanicilar.Any(k => k.KullaniciAdi == ad)) return Hata("Bu kullanıcı adı zaten var.");
        var (_, gecici) = Hesaplar.GeciciSifreyleAc(db, adSoyad, ad, Rol.OkulYoneticisi, okul.Id);
        db.SaveChanges();
        Console.WriteLine($"Okul yöneticisi hesabı açıldı: {ad} ({okul.Ad})");
        Console.WriteLine($"Geçici şifre: {gecici}");
        Console.WriteLine("Geçici şifre bir daha gösterilmez; ilk girişte kişi kendi şifresini belirler.");
        return 0;
    }

    static int VeliEkle(VeriTabani db, string ad, string sinif, string okulNo)
    {
        var okul = int.TryParse(okulNo, out var no) ? db.Okullar.Find(no) : null;
        if (okul is null) return Hata("Bu numarada bir okul yok.");
        var veli = new Veli { OkulId = okul.Id, Ad = ad, Sinif = sinif, Kod = KodUretici.Uret(), OlusturmaTarihi = DateTime.UtcNow };
        db.Veliler.Add(veli);
        db.SaveChanges();
        Console.WriteLine($"Eklendi: {veli.Ad} ({veli.Sinif})");
        Console.WriteLine($"Kod: {veli.Kod}");
        Console.WriteLine($"Bağlantı yolu: /v/{veli.Kod}");
        return 0;
    }

    // Ekrandan bir satır okur. gizli: yazılanlar ekranda görünmez (şifre için).
    // Girdi bir dosyadan ya da başka bir komuttan geliyorsa (ör. testlerde) satır satır okunur.
    static string Oku(string soru, bool gizli)
    {
        Console.Error.Write(soru);
        if (!gizli || Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var s = new System.Text.StringBuilder();
        while (true)
        {
            var tus = Console.ReadKey(intercept: true);
            if (tus.Key == ConsoleKey.Enter) break;
            if (tus.Key == ConsoleKey.Backspace) { if (s.Length > 0) s.Length--; continue; }
            if (!char.IsControl(tus.KeyChar)) s.Append(tus.KeyChar);
        }
        Console.Error.WriteLine();
        return s.ToString();
    }
}
