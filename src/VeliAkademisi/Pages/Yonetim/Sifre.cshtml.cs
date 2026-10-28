using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Şifre değiştirme: /yonetim/sifre — her kullanıcı için.
// Geçici şifreyle giren kişi bu sayfada kendi şifresini belirlemeden başka sayfaya geçemez (bkz. Oturum.RolKapisi).
// Şifre değişince güvenlik damgası yenilenir: kişinin başka cihazlardaki oturumları kapanır, bu cihazdaki sürer.
public class SifreModel : PageModel
{
    readonly VeriTabani _db;

    public SifreModel(VeriTabani db) => _db = db;

    [BindProperty] public string Mevcut { get; set; } = "";
    [BindProperty] public string Yeni { get; set; } = "";
    [BindProperty] public string YeniTekrar { get; set; } = "";
    public string? Hata { get; private set; }
    public bool Gecici => User.HasClaim(Oturum.GeciciBilgisi, "1");
    public bool Degisti { get; private set; }
    public string AnaSayfa => Oturum.AnaSayfa(User);

    public void OnGet(bool degisti = false) => Degisti = degisti;

    public async Task<IActionResult> OnPostAsync()
    {
        var k = await _db.Kullanicilar.FindAsync(Oturum.KullaniciNo(User));
        if (k is null) return Redirect("/yonetim/giris");

        if (!SifreOzeti.Dogrula(Mevcut ?? "", k.SifreOzeti)) Hata = "Şu anki şifreniz yanlış. Şifre değiştirilmedi.";
        else if ((Yeni ?? "").Length < Oturum.SifreEnAz) Hata = $"Yeni şifre en az {Oturum.SifreEnAz} karakter olmalı. Şifre değiştirilmedi.";
        else if (Yeni != YeniTekrar) Hata = "Yeni şifre iki kutuda aynı değil. Şifre değiştirilmedi.";
        else if (Yeni == Mevcut) Hata = "Yeni şifre şu anki şifreyle aynı olamaz. Şifre değiştirilmedi.";
        if (Hata is not null) return Page();

        k.SifreOzeti = SifreOzeti.Olustur(Yeni!);
        k.GeciciSifre = false;
        k.GuvenlikDamgasi = Oturum.YeniDamga();
        await _db.SaveChangesAsync();
        await Oturum.GirisYap(HttpContext, k);   // bu cihazdaki oturum yeni damgayla sürer
        return Redirect("/yonetim/sifre?degisti=true");
    }
}
