namespace VeliAkademisi.Veri;

// Sistemi kullanan bir okul. Her veli bir okula aittir; okul yöneticileri yalnızca kendi okullarının velilerini görür.
public class Okul
{
    public int Id { get; set; }

    // Okulun adı, ör. "Ortaca Özel Eğitim Uygulama Okulu". Benzersizdir.
    public string Ad { get; set; } = "";

    // Pasif okulun yöneticileri giriş yapamaz, velilerinin bağlantıları çalışmaz; veriler silinmez.
    public bool Etkin { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; }

    public List<Veli> Veliler { get; set; } = new();
    public List<Kullanici> Kullanicilar { get; set; } = new();
}
