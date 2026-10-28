namespace VeliAkademisi.Veri;

// Yönetim sayfalarına giren kişi. Her hesap kişiye özeldir.
public enum Rol
{
    SistemYoneticisi = 0,   // teknik: okulları ve okulların ilk yöneticilerini açar; veli verisini görmez
    OkulYoneticisi = 1,     // kendi okulunun velileri, takip, Excel
    IlceIzleyicisi = 2,     // okul başına isimsiz sayılar
}

public class Kullanici
{
    public int Id { get; set; }

    // Ekranda görünen ad, ör. "Ayşe Yılmaz".
    public string AdSoyad { get; set; } = "";

    // Girişte yazılan ad: küçük harf, harf/rakam/nokta/tire/alt çizgi. Benzersizdir.
    public string KullaniciAdi { get; set; } = "";

    // Şifrenin tek yönlü özeti (bkz. Yonetim/SifreOzeti). Şifrenin kendisi hiçbir yerde tutulmaz.
    public string SifreOzeti { get; set; } = "";

    public Rol Rol { get; set; }

    // Okul yöneticisinin okulu; sistem yöneticisi ve ilçe izleyicisinde boş.
    public int? OkulId { get; set; }
    public Okul? Okul { get; set; }

    // Geçici şifreyle açılmış/yenilenmiş hesap: ilk girişte kendi şifresini belirlemeden hiçbir sayfaya geçemez.
    public bool GeciciSifre { get; set; }

    // Kapatılmış hesap giriş yapamaz.
    public bool Etkin { get; set; } = true;

    // Şifre değişince ya da hesap kapatılınca yenilenir; eski oturumlar (başka cihazlardakiler dahil) geçersiz olur.
    public string GuvenlikDamgasi { get; set; } = "";

    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? SonGiris { get; set; }
}
