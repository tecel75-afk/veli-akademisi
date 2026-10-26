using System.Text.Json;

namespace VeliAkademisi;

// Bir hikâyedeki çoktan seçmeli soru.
public record Soru(int Adim, int No, string Metin, IReadOnlyList<string> Siklar, int? IdealSik);

// Bir hikâyenin sunucunun bilmesi gereken bilgileri.
public record HikayeBilgisi(string Kod, int Toplam, IReadOnlyDictionary<int, Soru> Sorular);

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
            _bilgiler[kod] = new HikayeBilgisi(kod, adimlar.Count, sorular);
        }
    }

    public HikayeBilgisi Bilgi(string hikaye) => _bilgiler[hikaye];
}
