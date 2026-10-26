namespace VeliAkademisi.Veri;

// Bir velinin bir hikâyedeki ilerlemesi. Her veli + hikâye için tek satır.
public class Ilerleme
{
    public int Id { get; set; }

    public int VeliId { get; set; }
    public Veli? Veli { get; set; }

    // Hikâye kodu: m1, m2a, m2b, m3, m4, m5, m6, m7.
    public string Hikaye { get; set; } = "";

    // Ulaşılan en ileri adım (0'dan başlar). Geri dönülse de küçülmez.
    public int EnIleriAdim { get; set; }

    // En son bulunulan adım. "Bölüm başı" ile geri dönülürse küçülebilir.
    public int SonAdim { get; set; }

    // Kapanış ekranına ulaşıldıysa true. Bir kez true olunca öyle kalır.
    public bool Tamamlandi { get; set; }

    public DateTime IlkGiris { get; set; }
    public DateTime SonGuncelleme { get; set; }
}
