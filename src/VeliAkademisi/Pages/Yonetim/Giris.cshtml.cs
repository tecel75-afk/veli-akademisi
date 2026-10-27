using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Yönetici giriş sayfası: /yonetim/giris
public class GirisModel : PageModel
{
    readonly IConfiguration _ayarlar;
    readonly GirisKilidi _kilit;

    public GirisModel(IConfiguration ayarlar, GirisKilidi kilit)
    {
        _ayarlar = ayarlar;
        _kilit = kilit;
    }

    [BindProperty] public string Sifre { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }   // girişten sonra dönülecek sayfa
    public string? Hata { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Kilitliyken doğru şifre de kabul edilmez.
        if (_kilit.KilitliMi(out var kalan))
        {
            Hata = $"Çok fazla hatalı deneme. Lütfen {Math.Ceiling(kalan.TotalMinutes)} dakika sonra yeniden deneyin.";
            return Page();
        }

        // Şifrenin özeti yapılandırmada durur ("Yonetim:SifreHash"); şifrenin kendisi hiçbir yerde tutulmaz.
        if (!SifreOzeti.Dogrula(Sifre, _ayarlar["Yonetim:SifreHash"]))
        {
            _kilit.Hatali();
            Hata = "Şifre yanlış.";
            return Page();
        }

        _kilit.Basarili();
        var kimlik = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "yonetici") },
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(kimlik));

        // Yalnızca sitenin kendi adreslerine dönülür (dışarıya yönlendirme yapılmaz).
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/yonetim");
    }
}
