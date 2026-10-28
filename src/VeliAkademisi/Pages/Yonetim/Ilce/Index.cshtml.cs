using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim.Ilce;

// İlçe izleme: /yonetim/ilce — yalnızca ilçe izleyicisi (bkz. Oturum.RolKapisi).
// Okul başına isimsiz sayılar: veli sayısı, en az bir hikâyeye başlayan veli sayısı, her hikâyeyi tamamlayan veli sayısı.
// Veli adı, sınıfı, kodu ve cevabı bu sayfaya hiç gelmez: sorgular yalnızca sayıları döndürür.
// (Okul süzgeci burada bilerek devre dışıdır — IgnoreQueryFilters — çünkü bütün okulların toplamları gerekir.)
public class IndexModel : PageModel
{
    readonly VeriTabani _db;

    public IndexModel(VeriTabani db) => _db = db;

    public record Satir(string Okul, bool Etkin, int Veli, int Baslayan, IReadOnlyList<int> Tamamlayan);
    public List<Satir> Satirlar { get; private set; } = new();
    public Satir? Toplam { get; private set; }

    public async Task OnGetAsync()
    {
        var okullar = await _db.Okullar.AsNoTracking().ToListAsync();
        var veli = await _db.Veliler.IgnoreQueryFilters().GroupBy(v => v.OkulId)
            .Select(g => new { g.Key, Sayi = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Sayi);
        var baslayan = await _db.Ilerlemeler.IgnoreQueryFilters().Select(i => new { i.Veli!.OkulId, i.VeliId }).Distinct()
            .GroupBy(x => x.OkulId).Select(g => new { g.Key, Sayi = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Sayi);
        var tamam = await _db.Ilerlemeler.IgnoreQueryFilters().Where(i => i.Tamamlandi)
            .GroupBy(i => new { i.Veli!.OkulId, i.Hikaye }).Select(g => new { g.Key.OkulId, g.Key.Hikaye, Sayi = g.Count() }).ToListAsync();

        Satirlar = okullar.OrderByDescending(o => o.Etkin).ThenBy(o => o.Ad, Takip.TurkceSira).Select(o => new Satir(
            o.Ad, o.Etkin, veli.GetValueOrDefault(o.Id), baslayan.GetValueOrDefault(o.Id),
            Hikayeler.Sira.Select(h => tamam.FirstOrDefault(t => t.OkulId == o.Id && t.Hikaye == h)?.Sayi ?? 0).ToList())).ToList();
        Toplam = new Satir("Toplam", true, Satirlar.Sum(s => s.Veli), Satirlar.Sum(s => s.Baslayan),
            Hikayeler.Sira.Select((_, k) => Satirlar.Sum(s => s.Tamamlayan[k])).ToList());
    }
}
