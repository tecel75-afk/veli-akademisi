using System.Text.Json;

namespace VeliAkademisi;

// Bir hikâyedeki çoktan seçmeli soru.
public record Soru(int Adim, int No, string Metin, IReadOnlyList<string> Siklar, int? IdealSik);

// Hikâyenin sonunda velinin görüp indirebileceği bir görsel (icerik/{kod}/materyal/{Dosya}).
public record MateryalGorseli(string Dosya, string Baslik);
// Hikâyenin sonundaki dış bağlantı (ör. Tohum Otizm Vakfı'nın ilgili sayfası).
public record DisBaglanti(string Adres, string Yazi);
public record Materyaller(IReadOnlyList<MateryalGorseli> Gorseller, DisBaglanti? Baglanti)
{
    public static readonly Materyaller Bos = new(Array.Empty<MateryalGorseli>(), null);
    public bool BosMu => Gorseller.Count == 0 && Baglanti is null;
}

// Bir hikâyenin sunucunun bilmesi gereken bilgileri.
public record HikayeBilgisi(string Kod, int Toplam, IReadOnlyDictionary<int, Soru> Sorular, Materyaller Materyaller);

// Site açılırken 8 hikâyenin steps.json dosyalarını okur ve bellekte tutar.
// Toplam adım sayısı, sorular ve ideal şıklar buradan bilinir; tarayıcının söylediğine güvenilmez.
public class HikayeVerisi
{
    readonly Dictionary<string, HikayeBilgisi> _bilgiler = new();

    public HikayeVerisi(string icerikYolu)
    {
        foreach (var kod in Hikayeler.Sira)
        {
            using var belge = JsonDocument.Parse(File.ReadAllText(Path.Combine(icerikYolu, kod, "steps.json")));
            var adimlar = belge.RootElement.EnumerateArray().ToList();
            var sorular = new Dictionary<int, Soru>();
            int no = 0;
            for (int adim = 0; adim < adimlar.Count; adim++)
            {
                var s = adimlar[adim];
                if (s.GetProperty("t").GetString() != "q") continue;   // yalnızca çoktan seçmeli sorular
                no++;
                var siklar = s.GetProperty("o").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                // İdeal şık "ov.i" alanındadır; "all: true" sorularda yoktur (bütün şıklar eşit).
                int? ideal = s.TryGetProperty("ov", out var ov) ? ov.GetProperty("i").GetInt32() : null;
                sorular[adim] = new Soru(adim, no, s.GetProperty("q").GetString() ?? "", siklar, ideal);
            }
            _bilgiler[kod] = new HikayeBilgisi(kod, adimlar.Count, sorular, MateryalleriOku(icerikYolu, kod));
        }
    }

    public HikayeBilgisi Bilgi(string hikaye) => _bilgiler[hikaye];

    // icerik/{kod}/materyaller.json (isteğe bağlı). Biçim:
    //   { "gorseller": [ { "dosya": "kart1.webp", "baslik": "Önce-sonra kartı" } ],
    //     "baglanti":  { "adres": "https://...", "yazi": "Tohum Otizm Vakfı: ..." } }
    // Dosya yoksa ya da boşsa materyal gösterilmez. Hatalıysa site açılmaz ve hatanın nerede olduğu yazılır
    // (yanlış bir dosyanın sessizce atlanması yerine).
    static Materyaller MateryalleriOku(string icerikYolu, string kod)
    {
        var yol = Path.Combine(icerikYolu, kod, "materyaller.json");
        if (!File.Exists(yol) || new FileInfo(yol).Length == 0) return Materyaller.Bos;
        string Hata(string m) => $"{kod}/materyaller.json: {m}";
        using var belge = JsonDocument.Parse(File.ReadAllText(yol));
        var kok = belge.RootElement;
        var gorseller = new List<MateryalGorseli>();
        if (kok.TryGetProperty("gorseller", out var g))
            foreach (var x in g.EnumerateArray())
            {
                var dosya = x.GetProperty("dosya").GetString() ?? "";
                var baslik = x.TryGetProperty("baslik", out var b) ? b.GetString() ?? "" : "";
                if (!System.Text.RegularExpressions.Regex.IsMatch(dosya, @"^[A-Za-z0-9_-]+\.(webp|png|jpg|jpeg)$"))
                    throw new InvalidDataException(Hata($"geçersiz dosya adı \"{dosya}\" (yalnızca harf, rakam, - _ ve .webp/.png/.jpg)"));
                if (!File.Exists(Path.Combine(icerikYolu, kod, "materyal", dosya)))
                    throw new InvalidDataException(Hata($"\"materyal/{dosya}\" bulunamadı"));
                gorseller.Add(new MateryalGorseli(dosya, baslik));
            }
        DisBaglanti? baglanti = null;
        if (kok.TryGetProperty("baglanti", out var bg) && bg.ValueKind == JsonValueKind.Object)
        {
            var adres = bg.GetProperty("adres").GetString() ?? "";
            if (!Uri.TryCreate(adres, UriKind.Absolute, out var u) || u.Scheme != "https")
                throw new InvalidDataException(Hata($"bağlantı adresi https:// ile başlamalı (\"{adres}\")"));
            baglanti = new DisBaglanti(adres, bg.TryGetProperty("yazi", out var y) ? y.GetString() ?? adres : adres);
        }
        return new Materyaller(gorseller, baglanti);
    }
}
