using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Veli ayrıntısı: /yonetim/veli/{id}
// Bağlantı (kopyalama), her hikâyenin durumu, her sorunun ilk ve son seçimi; yeni kod üretme.
public class VeliModel : PageModel
{
    readonly VeriTabani _db;
    readonly HikayeVerisi _hv;
    readonly IConfiguration _ayarlar;

    public VeliModel(VeriTabani db, HikayeVerisi hv, IConfiguration ayarlar)
    {
        _db = db;
        _hv = hv;
        _ayarlar = ayarlar;
    }

    public Veli Veli { get; private set; } = null!;
    public record HikayeBolumu(HikayeTanimi Tanim, string Durum, List<Takip.SoruCevabi> Sorular);
    public List<HikayeBolumu> Bolumler { get; private set; } = new();
    public string? Hata { get; private set; }
    public bool KodYenilendi { get; private set; }

    [BindProperty] public bool Anladim { get; set; }
    [BindProperty] public bool SilAnladim { get; set; }

    public string? SiteAdresi => _ayarlar["Site:Adres"]?.Trim().TrimEnd('/') is { Length: > 0 } a ? a : null;
    public string Baglanti => $"{SiteAdresi}/v/{Veli.Kod}";

    public async Task<IActionResult> OnGetAsync(int id, bool yenilendi = false)
    {
        if (!await Yukle(id)) return NotFound();
        KodYenilendi = yenilendi;
        return Page();
    }

    // Yeni kod üretme: eski bağlantı (ve ana ekrandaki simge) geçersiz olur. Onay kutusu işaretlenmelidir.
    public async Task<IActionResult> OnPostYeniKodAsync(int id)
    {
        var veli = await _db.Veliler.FindAsync(id);
        if (veli is null) return NotFound();
        if (!Anladim)
        {
            await Yukle(id);
            Hata = "Yeni kod üretilmedi: önce onay kutusunu işaretleyin.";
            return Page();
        }
        veli.Kod = KodUretici.Uret();
        await _db.SaveChangesAsync();
        return Redirect($"/yonetim/veli/{id}?yenilendi=true");
    }

    // Veliyi bütün ilerleme ve cevaplarıyla siler (veritabanı kuralı: veli silinince kayıtları da silinir).
    public async Task<IActionResult> OnPostSilAsync(int id)
    {
        var veli = await _db.Veliler.FindAsync(id);
        if (veli is null) return NotFound();
        if (!SilAnladim)
        {
            await Yukle(id);
            Hata = "Veli silinmedi: önce silme onay kutusunu işaretleyin.";
            return Page();
        }
        _db.Veliler.Remove(veli);
        await _db.SaveChangesAsync();
        return Redirect("/yonetim");
    }

    async Task<bool> Yukle(int id)
    {
        var veli = await _db.Veliler.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        if (veli is null) return false;
        Veli = veli;
        var ilerlemeler = await _db.Ilerlemeler.AsNoTracking().Where(x => x.VeliId == id).ToDictionaryAsync(x => x.Hikaye);
        var cevaplar = await _db.Cevaplar.AsNoTracking().Where(x => x.VeliId == id).ToListAsync();
        Bolumler = Hikayeler.Tanimlar.Select(t =>
        {
            var bilgi = _hv.Bilgi(t.Kod);
            return new HikayeBolumu(t, Takip.Hucre(ilerlemeler.GetValueOrDefault(t.Kod), bilgi.Toplam),
                Takip.Cevaplar(bilgi, cevaplar));
        }).ToList();
        return true;
    }
}
