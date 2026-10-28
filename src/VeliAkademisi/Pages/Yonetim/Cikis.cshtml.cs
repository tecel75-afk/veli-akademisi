using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VeliAkademisi.Pages.Yonetim;

// Çıkış: /yonetim/cikis (yalnızca POST; sayfaların "Çıkış" düğmesi buraya gönderir). Oturumu kapatır.
public class CikisModel : PageModel
{
    public IActionResult OnGet() => Redirect("/yonetim");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/yonetim/giris");
    }
}
