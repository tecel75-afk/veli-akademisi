using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

namespace VeliAkademisi.Yonetim;

// Hesap açma ve geçici şifre işleri; yönetim ekranları ve sunucu komutları aynı kuralları kullanır.
public static class Hesaplar
{
    // Bir okulda veli eklenebilmesi için gereken en az etkin yönetici sayısı (28 Ekim kararı):
    // biri şifresini unutursa öteki yenileyebilsin, iş sistem yöneticisine gelmesin.
    public const int OkulEnAzYonetici = 2;

    public static Task<int> EtkinYoneticiSayisi(VeriTabani db, int okulId) =>
        db.Kullanicilar.CountAsync(k => k.OkulId == okulId && k.Rol == Rol.OkulYoneticisi && k.Etkin);

    // Ad soyad ve kullanıcı adını denetler; hata yoksa null.
    public static async Task<string?> Denetle(VeriTabani db, string adSoyad, string kullaniciAdi)
    {
        if (adSoyad.Length == 0) return "Ad soyad boş olamaz.";
        if (adSoyad.Length > 100) return "Ad soyad en çok 100 karakter olabilir.";
        if (!Oturum.KullaniciAdiGecerli(kullaniciAdi))
            return "Kullanıcı adı 3–40 karakter olmalı; yalnızca küçük harf (a-z, Türkçe harf olmadan), rakam, nokta, tire, alt çizgi.";
        if (await db.Kullanicilar.AnyAsync(k => k.KullaniciAdi == kullaniciAdi)) return "Bu kullanıcı adı zaten kullanılıyor.";
        return null;
    }

    // Yeni hesap: geçici şifreyle açılır; geçici şifre döner (bir kez gösterilir, saklanmaz).
    public static (Kullanici Hesap, string GeciciSifre) GeciciSifreyleAc(VeriTabani db, string adSoyad, string kullaniciAdi, Rol rol, int? okulId)
    {
        var gecici = GeciciSifre.Uret();
        var k = Yeni(adSoyad, kullaniciAdi, rol, okulId, gecici, gecici: true);
        db.Kullanicilar.Add(k);
        return (k, gecici);
    }

    public static Kullanici Yeni(string adSoyad, string kullaniciAdi, Rol rol, int? okulId, string sifre, bool gecici) => new()
    {
        AdSoyad = adSoyad, KullaniciAdi = kullaniciAdi, Rol = rol, OkulId = okulId,
        SifreOzeti = SifreOzeti.Olustur(sifre), GeciciSifre = gecici,
        GuvenlikDamgasi = Oturum.YeniDamga(), OlusturmaTarihi = DateTime.UtcNow,
    };

    // Şifresini unutan ya da yeniden açılan hesaba geçici şifre: eski şifre ve açık oturumları geçersiz olur.
    public static string GeciciSifreVer(Kullanici k)
    {
        var gecici = GeciciSifre.Uret();
        k.SifreOzeti = SifreOzeti.Olustur(gecici);
        k.GeciciSifre = true;
        k.GuvenlikDamgasi = Oturum.YeniDamga();
        return gecici;
    }

    // Hesabı kapatır: giriş yapamaz, açık oturumları hemen kapanır.
    public static void Kapat(Kullanici k)
    {
        k.Etkin = false;
        k.GuvenlikDamgasi = Oturum.YeniDamga();
    }
}
