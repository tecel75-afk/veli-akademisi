using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Okulun yöneticileri: /yonetim/yoneticiler (yalnızca okul yöneticisi; yalnızca kendi okulu).
//  - Yeni yönetici ekleme (geçici şifreyle açılır)
//  - Şifresini unutan yöneticiye geçici şifre verme (eski şifresi ve açık oturumları geçersiz olur)
//  - Ayrılan yöneticinin hesabını kapatma / kapalı hesabı yeniden açma (geçici şifreyle)
// Geçici şifre işlemden hemen sonra bu sayfada bir kez gösterilir; hiçbir yerde saklanmaz.
// Kullanıcılar tablosunda okul süzgeci yoktur; bu yüzden her işlemde hesabın bu okula ait olduğu ayrıca denetlenir.
public class YoneticilerModel : PageModel
{
    readonly VeriTabani _db;

    public YoneticilerModel(VeriTabani db) => _db = db;

    [BindProperty] public string AdSoyad { get; set; } = "";
    [BindProperty] public string KullaniciAdi { get; set; } = "";
    [BindProperty] public bool Anladim { get; set; }

    public List<Kullanici> Yoneticiler { get; private set; } = new();
    public int EtkinSayisi { get; private set; }
    public int BenimNo => Oturum.KullaniciNo(User) ?? 0;
    public string? Hata { get; private set; }

    // İşlem sonucu (geçici şifre bir kez gösterilir).
    public string? SonucBasligi { get; private set; }
    public string? SonucKullanici { get; private set; }
    public string? SonucSifre { get; private set; }

    int Okul => Oturum.OkulNo(User) ?? throw new InvalidOperationException("Okul yöneticisi oturumunda okul bilgisi yok.");

    public async Task OnGetAsync() => await Yukle();

    public async Task<IActionResult> OnPostEkleAsync()
    {
        var adSoyad = (AdSoyad ?? "").Trim();
        var ad = Oturum.KullaniciAdiDuzelt(KullaniciAdi);
        Hata = await Hesaplar.Denetle(_db, adSoyad, ad);
        if (Hata is null)
        {
            var (_, gecici) = Hesaplar.GeciciSifreyleAc(_db, adSoyad, ad, Rol.OkulYoneticisi, Okul);
            await _db.SaveChangesAsync();
            (SonucBasligi, SonucKullanici, SonucSifre) = ($"{adSoyad} eklendi", ad, gecici);
            AdSoyad = KullaniciAdi = "";
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostGeciciSifreAsync(int id)
    {
        var k = await BuOkulun(id);
        if (k is null) return NotFound();
        if (k.Id == BenimNo) Hata = "Kendi şifrenizi \"Şifremi değiştir\" sayfasından değiştirin.";
        else if (!k.Etkin) Hata = "Kapalı hesaba geçici şifre verilmez; önce hesabı yeniden açın.";
        else if (!Anladim) Hata = "Geçici şifre verilmedi: önce onay kutusunu işaretleyin.";
        else
        {
            var gecici = Hesaplar.GeciciSifreVer(k);
            await _db.SaveChangesAsync();
            (SonucBasligi, SonucKullanici, SonucSifre) = ($"{k.AdSoyad} için geçici şifre", k.KullaniciAdi, gecici);
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostKapatAsync(int id)
    {
        var k = await BuOkulun(id);
        if (k is null) return NotFound();
        if (k.Id == BenimNo) Hata = "Kendi hesabınızı kapatamazsınız; okulunuzun diğer yöneticisi kapatabilir.";
        else if (!Anladim) Hata = "Hesap kapatılmadı: önce onay kutusunu işaretleyin.";
        else
        {
            Hesaplar.Kapat(k);
            await _db.SaveChangesAsync();
            SonucBasligi = $"{k.AdSoyad} hesabı kapatıldı";
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostAcAsync(int id)
    {
        var k = await BuOkulun(id);
        if (k is null) return NotFound();
        if (k.Etkin) Hata = "Bu hesap zaten açık.";
        else
        {
            k.Etkin = true;
            var gecici = Hesaplar.GeciciSifreVer(k);
            await _db.SaveChangesAsync();
            (SonucBasligi, SonucKullanici, SonucSifre) = ($"{k.AdSoyad} hesabı yeniden açıldı", k.KullaniciAdi, gecici);
        }
        await Yukle();
        return Page();
    }

    // Yalnızca bu okulun okul yöneticisi hesapları; başka okulun ya da başka roldeki hesap için null (→ 404).
    Task<Kullanici?> BuOkulun(int id) =>
        _db.Kullanicilar.FirstOrDefaultAsync(k => k.Id == id && k.OkulId == Okul && k.Rol == Rol.OkulYoneticisi);

    async Task Yukle()
    {
        Yoneticiler = (await _db.Kullanicilar.AsNoTracking()
                .Where(k => k.OkulId == Okul && k.Rol == Rol.OkulYoneticisi).ToListAsync())
            .OrderByDescending(k => k.Etkin).ThenBy(k => k.AdSoyad, Takip.TurkceSira).ToList();
        EtkinSayisi = Yoneticiler.Count(k => k.Etkin);
    }
}
