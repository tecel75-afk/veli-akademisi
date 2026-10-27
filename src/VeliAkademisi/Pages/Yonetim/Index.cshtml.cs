using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VeliAkademisi.Pages.Yonetim;

// Yönetim ana sayfası: /yonetim (yalnızca giriş yapılmışsa açılır)
public class IndexModel : PageModel
{
    public void OnGet() { }

    // "Çıkış" düğmesi: oturumu kapatır, giriş sayfasına döner.
    public async Task<IActionResult> OnPostCikisAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/yonetim/giris");
    }
}
