using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

namespace VeliAkademisi.Pages.Yonetim;

// Veli ekleme sayfası: /yonetim/veli-ekle
//  - Tek tek: Ad + Sınıf
//  - Toplu: her satıra "Ad;Sınıf". Bir satır bile hatalıysa hiçbiri eklenmez; hatalı satırlar gösterilir.
// Ekleme sonrası sayfa yeniden yüklenir (POST → yönlendirme → GET); böylece sayfayı yenilemek
// aynı velileri ikinci kez eklemez. Eklenenler adreste "?eklenen=3,4,5" olarak taşınır.
public class VeliEkleModel : PageModel
{
    const int AdEnCok = 100, SinifEnCok = 20, TopluEnCok = 200;

    readonly VeriTabani _db;
    readonly IConfiguration _ayarlar;

    public VeliEkleModel(VeriTabani db, IConfiguration ayarlar)
    {
        _db = db;
        _ayarlar = ayarlar;
    }

    [BindProperty] public string Ad { get; set; } = "";
    [BindProperty] public string Sinif { get; set; } = "";
    [BindProperty] public string Toplu { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? Eklenen { get; set; }

    public List<string> Hatalar { get; } = new();
    public List<Veli> EklenenVeliler { get; private set; } = new();
    public List<string> Uyarilar { get; } = new();

    // Bağlantıların başındaki site adresi yapılandırmadan gelir ("Site:Adres"); sonundaki "/" atılır.
    public string? SiteAdresi => _ayarlar["Site:Adres"]?.Trim().TrimEnd('/') is { Length: > 0 } a ? a : null;
    public string Baglanti(Veli v) => $"{SiteAdresi}/v/{v.Kod}";

    public async Task OnGetAsync()
    {
        // Az önce eklenenleri göster.
        var idler = (Eklenen ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var n) ? n : 0).Where(n => n > 0).ToList();
        if (idler.Count > 0)
        {
            EklenenVeliler = await _db.Veliler.Where(v => idler.Contains(v.Id)).OrderBy(v => v.Id).ToListAsync();
            // Aynı ad ve sınıfla birden fazla veli varsa uyar (yanlışlıkla iki kez eklenmiş olabilir).
            foreach (var v in EklenenVeliler)
                if (await _db.Veliler.CountAsync(x => x.Ad == v.Ad && x.Sinif == v.Sinif) > 1)
                    Uyarilar.Add($"\"{v.Ad}\" ({v.Sinif}) adıyla birden fazla veli var. Yanlışlıkla iki kez eklendiyse fazlasını takip tablosundan silebilirsiniz.");
        }
    }

    public async Task<IActionResult> OnPostTekAsync()
    {
        var (ad, sinif, hata) = Denetle(Ad, Sinif);
        if (hata is not null) { Hatalar.Add(hata); return Page(); }
        var v = YeniVeli(ad, sinif);
        await _db.SaveChangesAsync();
        return Redirect($"/yonetim/veli-ekle?eklenen={v.Id}");
    }

    public async Task<IActionResult> OnPostTopluAsync()
    {
        var satirlar = (Toplu ?? "").Replace("\r", "").Split('\n');
        var okunan = new List<(string Ad, string Sinif)>();
        for (int k = 0; k < satirlar.Length; k++)
        {
            var satir = satirlar[k].Trim();
            if (satir.Length == 0) continue;                       // boş satırlar atlanır
            var parca = satir.Split(';');
            if (parca.Length != 2) { Hatalar.Add($"{k + 1}. satır: \"Ad;Sınıf\" biçiminde olmalı (tek bir ; ile)."); continue; }
            var (ad, sinif, hata) = Denetle(parca[0], parca[1]);
            if (hata is not null) { Hatalar.Add($"{k + 1}. satır: {hata}"); continue; }
            okunan.Add((ad, sinif));
        }
        if (okunan.Count == 0 && Hatalar.Count == 0) Hatalar.Add("Eklenecek veli yazılmadı.");
        if (okunan.Count > TopluEnCok) Hatalar.Add($"Bir seferde en çok {TopluEnCok} veli eklenebilir.");
        if (Hatalar.Count > 0) { Hatalar.Insert(0, "Hiçbir veli eklenmedi. Hatalı satırları düzeltip yeniden gönderin."); return Page(); }

        var yeniler = okunan.Select(x => YeniVeli(x.Ad, x.Sinif)).ToList();
        await _db.SaveChangesAsync();   // hepsi tek seferde kaydedilir
        return Redirect($"/yonetim/veli-ekle?eklenen={string.Join(",", yeniler.Select(v => v.Id))}");
    }

    Veli YeniVeli(string ad, string sinif)
    {
        var v = new Veli { Ad = ad, Sinif = sinif, Kod = KodUretici.Uret(), OlusturmaTarihi = DateTime.UtcNow };
        _db.Veliler.Add(v);
        return v;
    }

    // Ad ve sınıfı temizler ve denetler. Hata yoksa hata null döner.
    static (string Ad, string Sinif, string? Hata) Denetle(string? ad, string? sinif)
    {
        ad = (ad ?? "").Trim();
        sinif = (sinif ?? "").Trim();
        if (ad.Length == 0) return (ad, sinif, "Ad boş olamaz.");
        if (sinif.Length == 0) return (ad, sinif, "Sınıf boş olamaz.");
        if (ad.Length > AdEnCok) return (ad, sinif, $"Ad en çok {AdEnCok} karakter olabilir.");
        if (sinif.Length > SinifEnCok) return (ad, sinif, $"Sınıf en çok {SinifEnCok} karakter olabilir.");
        return (ad, sinif, null);
    }
}
