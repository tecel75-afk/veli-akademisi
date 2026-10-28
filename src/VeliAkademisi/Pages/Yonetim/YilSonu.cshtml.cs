using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Yıl sonu silme: /yonetim/yil-sonu
// Okulun bütün velileri, ilerlemeleri ve cevapları silinir; başka okullarınkine dokunulmaz
// (silme sorguları da okul süzgecinden geçer, bkz. VeriTabani). İki onay ister:
//   1) onay kutusu, 2) kutuya "TÜMÜNÜ SİL" yazmak (büyük/küçük harf fark etmez).
public class YilSonuModel : PageModel
{
    public const string OnayYazisi = "TÜMÜNÜ SİL";
    readonly VeriTabani _db;

    public YilSonuModel(VeriTabani db) => _db = db;

    [BindProperty] public bool Anladim { get; set; }
    [BindProperty] public string Yazi { get; set; } = "";
    public string? Hata { get; private set; }
    public bool Silindi { get; private set; }
    public int VeliSayisi { get; private set; }

    public async Task OnGetAsync(bool silindi = false)
    {
        Silindi = silindi;
        VeliSayisi = await _db.Veliler.CountAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Anladim || (Yazi ?? "").Trim().ToUpper(Takip.Turkce) != OnayYazisi)
        {
            Hata = $"Hiçbir şey silinmedi. Onay kutusunu işaretleyip kutuya \"{OnayYazisi}\" yazmalısınız.";
            VeliSayisi = await _db.Veliler.CountAsync();
            return Page();
        }
        // Üç tablo tek işlemde boşaltılır: ya hepsi silinir ya hiçbiri.
        await using var islem = await _db.Database.BeginTransactionAsync();
        await _db.Cevaplar.ExecuteDeleteAsync();
        await _db.Ilerlemeler.ExecuteDeleteAsync();
        await _db.Veliler.ExecuteDeleteAsync();
        await islem.CommitAsync();
        return Redirect("/yonetim/yil-sonu?silindi=true");
    }
}
