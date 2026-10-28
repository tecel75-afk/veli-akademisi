using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Giriş sayfası: /yonetim/giris — kullanıcı adı + şifre (her hesap kişiye özel).
public class GirisModel : PageModel
{
    // Kullanıcı adı sistemde yoksa da şifre özeti hesaplanır; böylece adın var olup olmadığı yanıt süresinden anlaşılmaz.
    static readonly string SahteOzet = SifreOzeti.Olustur(Guid.NewGuid().ToString());

    readonly VeriTabani _db;
    readonly GirisKilidi _kilit;

    public GirisModel(VeriTabani db, GirisKilidi kilit)
    {
        _db = db;
        _kilit = kilit;
    }

    [BindProperty] public string KullaniciAdi { get; set; } = "";
    [BindProperty] public string Sifre { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }   // girişten sonra dönülecek sayfa
    public string? Hata { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var ad = Oturum.KullaniciAdiDuzelt(KullaniciAdi);

        // Kilitliyken doğru şifre de kabul edilmez.
        if (_kilit.KilitliMi(ad, out var kalan))
        {
            Hata = $"Bu kullanıcı adıyla çok fazla hatalı deneme yapıldı. Lütfen {Math.Ceiling(kalan.TotalMinutes)} dakika sonra yeniden deneyin.";
            return Page();
        }

        var k = await _db.Kullanicilar.Include(x => x.Okul).FirstOrDefaultAsync(x => x.KullaniciAdi == ad);
        var dogru = SifreOzeti.Dogrula(Sifre ?? "", k?.SifreOzeti ?? SahteOzet) && k is not null;
        if (!dogru)
        {
            _kilit.Hatali(ad);
            Hata = "Kullanıcı adı ya da şifre yanlış.";
            return Page();
        }
        _kilit.Basarili(ad);

        // Şifre doğru; hesap ya da okul kapalıysa nedeni söylenir.
        if (!k!.Etkin) { Hata = "Bu hesap kapatılmış. Okulunuzun diğer yöneticisine başvurun."; return Page(); }
        if (k.Rol == Rol.OkulYoneticisi && k.Okul is not { Etkin: true })
        {
            Hata = "Okulunuzun kullanımı şu an kapalı.";
            return Page();
        }

        k.SonGiris = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await Oturum.GirisYap(HttpContext, k);

        if (k.GeciciSifre) return Redirect("/yonetim/sifre");
        // Girişten önce istenen sayfaya dönülür; ama yalnızca sitenin kendi adresiyse (dışarıya yönlendirme yapılmaz)
        // ve bu kişinin rolü o sayfayı açabiliyorsa. Değilse kişinin kendi ana sayfası açılır
        // (ör. sistem yöneticisi /yonetim adresinden girerse Okullar sayfasına gider, "erişim yok"a değil).
        var ana = k.Rol switch { Rol.SistemYoneticisi => "/yonetim/sistem", Rol.IlceIzleyicisi => "/yonetim/ilce", _ => "/yonetim" };
        var kimlik = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, Oturum.RolAdi(k.Rol)) }, "giris"));
        var donus = Url.IsLocalUrl(ReturnUrl) && Oturum.IzinliMi(kimlik, new PathString(ReturnUrl!.Split('?', '#')[0])) ? ReturnUrl! : ana;
        return LocalRedirect(donus);
    }
}
