using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Okul yöneticisinin ana sayfası: /yonetim (yalnızca okul yöneticisi; bkz. Oturum.RolKapisi)
// Bütün veli sorguları okul süzgecinden geçer: yalnızca kendi okulunun velileri görünür (bkz. VeriTabani).
// Takip tablosu: satır = veli, sütun = 8 hikâye. Sınıfa göre süzme (?sinif=), ada göre arama (?ara=).
public class IndexModel : PageModel
{
    readonly VeriTabani _db;
    readonly HikayeVerisi _hv;

    public IndexModel(VeriTabani db, HikayeVerisi hv)
    {
        _db = db;
        _hv = hv;
    }

    static string Damga() => (DateTime.UtcNow + TimeSpan.FromHours(3)).ToString("yyyy-MM-dd-HHmm");

    // /yonetim?handler=excel → iki sayfalık Excel dosyası (Takip, Cevaplar)
    public async Task<IActionResult> OnGetExcelAsync() =>
        File(await ExcelRaporu.Olustur(_db, _hv),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"veli-akademisi-{Damga()}.xlsx");

    [BindProperty(SupportsGet = true)] public string? Sinif { get; set; }
    [BindProperty(SupportsGet = true)] public string? Ara { get; set; }

    public record Satir(Veli Veli, IReadOnlyList<string> Hucreler);
    public List<Satir> Satirlar { get; private set; } = new();
    public List<string> Siniflar { get; private set; } = new();
    public int ToplamVeli { get; private set; }
    public string OkulAdi { get; private set; } = "";

    public async Task OnGetAsync()
    {
        OkulAdi = (await _db.Okullar.FindAsync(Oturum.OkulNo(User)))?.Ad ?? "";
        // Veli sayısı küçük olduğu için (bir okul) hepsi okunup süzme bellekte yapılır;
        // böylece Türkçe büyük/küçük harf (İ/ı, Ş/ş) doğru karşılaştırılır.
        var veliler = await _db.Veliler.AsNoTracking().ToListAsync();
        var ilerlemeler = (await _db.Ilerlemeler.AsNoTracking().ToListAsync())
            .ToDictionary(x => (x.VeliId, x.Hikaye));
        ToplamVeli = veliler.Count;
        Siniflar = veliler.Select(v => v.Sinif).Distinct().OrderBy(s => s, Takip.TurkceSira).ToList();

        var ara = (Ara ?? "").Trim().ToLower(Takip.Turkce);
        Satirlar = veliler
            .Where(v => string.IsNullOrEmpty(Sinif) || v.Sinif == Sinif)
            .Where(v => ara.Length == 0 || v.Ad.ToLower(Takip.Turkce).Contains(ara))
            .OrderBy(v => v.Sinif, Takip.TurkceSira).ThenBy(v => v.Ad, Takip.TurkceSira)
            .Select(v => new Satir(v, Hikayeler.Sira.Select(h =>
                Takip.Hucre(ilerlemeler.GetValueOrDefault((v.Id, h)), _hv.Bilgi(h).Toplam)).ToList()))
            .ToList();
    }
}
