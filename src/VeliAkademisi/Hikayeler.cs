namespace VeliAkademisi;

// Sitedeki hikâyeler ve açılma sırası. Sıra tanım belgesindeki sıradır.
public static class Hikayeler
{
    public static readonly IReadOnlyList<string> Sira = new[] { "m1", "m2a", "m2b", "m3", "m4", "m5", "m6", "m7" };

    // Adresteki hikâye kodu bu listede yoksa geçersizdir.
    public static bool GecerliMi(string hikaye) => Sira.Contains(hikaye);
}
