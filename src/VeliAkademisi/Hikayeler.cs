namespace VeliAkademisi;

// Bir hikâyenin listede görünen bilgileri.
public record HikayeTanimi(string Kod, string Modul, string Ad);

// Sitedeki hikâyeler ve açılma sırası. Sıra ve adlar tanım belgesindeki tablodan alınmıştır.
public static class Hikayeler
{
    public static readonly IReadOnlyList<HikayeTanimi> Tanimlar = new[]
    {
        new HikayeTanimi("m1",  "Modül 1 · Davranış Sorunları",               "Tablet Kapanınca"),
        new HikayeTanimi("m2a", "Modül 2 · Mahremiyet (Senaryo A)",           "Elif'in Kalpleri"),
        new HikayeTanimi("m2b", "Modül 2 · Mahremiyet (Senaryo B)",           "Kapı Kapalıysa"),
        new HikayeTanimi("m3",  "Modül 3 · Günlük Yaşam ve Öz Bakım",         "Ben Yapayım, Çabuk Olsun"),
        new HikayeTanimi("m4",  "Modül 4 · Güvenli ve Bağımsız Yaşam",        "Markete Kadar"),
        new HikayeTanimi("m5",  "Modül 5 · Evde Rutinler ve Tutarlı Tutum",   "Annem Evet Dedi"),
        new HikayeTanimi("m6",  "Modül 6 · İletişim ve Sosyal Etkileşim",     "Ne İstediğini Nasıl Anlayacağız?"),
        new HikayeTanimi("m7",  "Modül 7 · Aileyi Güçlendirme",               "Ben Yorulmam Sanıyordum"),
    };

    public static readonly IReadOnlyList<string> Sira = Tanimlar.Select(t => t.Kod).ToList();

    // Hikâyenin sıradaki yeri (m1 = 0). Listede yoksa -1.
    public static int SiraNo(string hikaye)
    {
        for (int k = 0; k < Sira.Count; k++) if (Sira[k] == hikaye) return k;
        return -1;
    }

    // Adresteki hikâye kodu bu listede yoksa geçersizdir.
    public static bool GecerliMi(string hikaye) => Sira.Contains(hikaye);
}
