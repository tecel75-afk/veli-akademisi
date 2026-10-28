using Microsoft.AspNetCore.Mvc.RazorPages;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Erişim yok: /yonetim/erisim — kişinin rolüne kapalı bir sayfa istendiğinde buraya gelinir.
public class ErisimModel : PageModel
{
    public string AnaSayfa => Oturum.AnaSayfa(User);
    public void OnGet() { }
}
