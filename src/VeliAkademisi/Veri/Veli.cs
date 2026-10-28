namespace VeliAkademisi.Veri;

// Bir veli. Kişisel veri olarak yalnızca ad ve sınıf tutulur (KVKK kararı).
public class Veli
{
    public int Id { get; set; }

    // Yönetim sayfasında görünen ad, ör. "Ayşe Hanım".
    public string Ad { get; set; } = "";

    // Velinin okulu (zorunlu).
    public int OkulId { get; set; }
    public Okul? Okul { get; set; }

    // Serbest metin, ör. "4-A".
    public string Sinif { get; set; } = "";

    // Velinin bağlantısındaki gizli kod: /v/{Kod}. Benzersizdir.
    public string Kod { get; set; } = "";

    // Tüm zamanlar UTC olarak saklanır; ekranda Türkiye saatine çevrilir.
    public DateTime OlusturmaTarihi { get; set; }

    // Bağlantının en son açıldığı zaman (hiç açılmadıysa boş).
    public DateTime? SonGiris { get; set; }

    // Bu velinin kayıtları. Veli silinince bunlar da silinir.
    public List<Ilerleme> Ilerlemeler { get; set; } = new();
    public List<Cevap> Cevaplar { get; set; } = new();
}
