using System.Globalization;
using VeliAkademisi.Veri;

namespace VeliAkademisi.Yonetim;

// Yönetim sayfalarının (takip tablosu, veli ayrıntısı, ileride Excel) ortak hesapları.
public static class Takip
{
    // Türkiye 2016'dan beri sürekli UTC+3'tedir (yaz/kış saati yok). Veritabanı UTC tutar; ekranda +3 eklenir.
    static readonly TimeSpan TurkiyeFarki = TimeSpan.FromHours(3);

    // Türkçe sıralama ve büyük/küçük harf: "ayşe" ile "AYŞE" aynı sayılır, "İ/ı" doğru işlenir.
    public static readonly CultureInfo Turkce = CultureInfo.GetCultureInfo("tr-TR");
    public static readonly StringComparer TurkceSira = StringComparer.Create(Turkce, ignoreCase: true);

    public static string Zaman(DateTime? utc) =>
        utc is null ? "—" : (utc.Value + TurkiyeFarki).ToString("dd.MM.yyyy HH:mm", Turkce);

    public static string Harf(int secim) => "ABC"[secim].ToString();

    // İlerleme yüzdesi: ulaşılan en ileri adıma göre (geri dönülse de düşmez).
    // Kapanışa ulaşılmadıkça en çok %99 gösterilir; %100 yerine "Tamamlandı" yazılır.
    public static int Yuzde(Ilerleme i, int toplam) => Math.Min(99, (i.EnIleriAdim + 1) * 100 / toplam);

    // Takip tablosundaki hücre yazısı.
    public static string Hucre(Ilerleme? i, int toplam) =>
        i is null ? "Başlanmadı" : i.Tamamlandi ? "Tamamlandı" : $"%{Yuzde(i, toplam)}";

    // Bir sorudaki ilk ve son seçim (aynı soruda birden çok seçim olabilir: şık değiştirme, tekrar izleme).
    public record SoruCevabi(Soru Soru, Cevap? Ilk, Cevap? Son)
    {
        // Son seçim ideal şık mı? İdeal şıkkı olmayan (bütün şıkları eşit) sorularda ya da cevapsızsa null.
        public bool? IdealMi => Son is null || Soru.IdealSik is null ? null : Son.Secim == Soru.IdealSik;
    }

    // Bir velinin bir hikâyedeki bütün soruları ve cevapları, soru sırasıyla.
    public static List<SoruCevabi> Cevaplar(HikayeBilgisi bilgi, IEnumerable<Cevap> veliCevaplari)
    {
        var gruplar = veliCevaplari.Where(c => c.Hikaye == bilgi.Kod)
            .GroupBy(c => c.Adim).ToDictionary(g => g.Key, g => g.OrderBy(c => c.Id).ToList());
        return bilgi.Sorular.Values.OrderBy(s => s.No).Select(s =>
            gruplar.TryGetValue(s.Adim, out var l) ? new SoruCevabi(s, l[0], l[^1]) : new SoruCevabi(s, null, null)).ToList();
    }
}
