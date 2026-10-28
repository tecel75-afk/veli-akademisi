using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

namespace VeliAkademisi.Yonetim;

// Yönetim oturumu: girişte çereze yazılan bilgiler, her istekte hesabın hâlâ geçerli olduğunun denetimi,
// rollere göre giriş sonrası açılan sayfa ve sayfalara rol denetimi.
public static partial class Oturum
{
    // Çerezdeki bilgilerin adları.
    public const string OkulBilgisi = "okul";        // okul yöneticisinin okul numarası
    public const string DamgaBilgisi = "damga";      // hesabın güvenlik damgası (şifre değişince eskir)
    public const string GeciciBilgisi = "gecici";    // "1": önce kendi şifresini belirlemeli

    public static string RolAdi(Rol rol) => rol switch
    {
        Rol.SistemYoneticisi => "Sistem",
        Rol.OkulYoneticisi => "Okul",
        _ => "Ilce",
    };

    // Girişten sonra açılan sayfa.
    public static string AnaSayfa(ClaimsPrincipal k) =>
        k.IsInRole("Sistem") ? "/yonetim/sistem" : k.IsInRole("Ilce") ? "/yonetim/ilce" : "/yonetim";

    // Kullanıcı adı: boşluklar atılır, küçük harfe çevrilir. Yalnızca a-z, 0-9, nokta, tire, alt çizgi; 3–40 karakter.
    public static string KullaniciAdiDuzelt(string? ad) => (ad ?? "").Trim().ToLowerInvariant();
    public static bool KullaniciAdiGecerli(string ad) => KullaniciAdiBicimi().IsMatch(ad);

    [GeneratedRegex("^[a-z0-9._-]{3,40}$")]
    private static partial Regex KullaniciAdiBicimi();

    public static string YeniDamga() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    public const int SifreEnAz = 10;

    // Oturumu açar (ya da şifre değişince yeniler).
    public static Task GirisYap(HttpContext http, Kullanici k)
    {
        var bilgiler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, k.Id.ToString()),
            new(ClaimTypes.Name, k.AdSoyad),
            new(ClaimTypes.Role, RolAdi(k.Rol)),
            new(DamgaBilgisi, k.GuvenlikDamgasi),
        };
        if (k.OkulId is int o) bilgiler.Add(new(OkulBilgisi, o.ToString()));
        if (k.GeciciSifre) bilgiler.Add(new(GeciciBilgisi, "1"));
        var kimlik = new ClaimsIdentity(bilgiler, CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(kimlik));
    }

    public static int? KullaniciNo(ClaimsPrincipal k) =>
        int.TryParse(k.FindFirstValue(ClaimTypes.NameIdentifier), out var n) ? n : null;

    public static int? OkulNo(ClaimsPrincipal k) =>
        k.IsInRole("Okul") && int.TryParse(k.FindFirstValue(OkulBilgisi), out var n) ? n : null;

    // Her yönetim isteğinde: hesap hâlâ var mı, açık mı, şifresi değişmemiş mi, okulu etkin mi?
    // Biri tutmazsa oturum kapanır (ör. başka cihazda şifre değiştirildiyse ya da hesap kapatıldıysa).
    public static async Task Denetle(CookieValidatePrincipalContext c)
    {
        var no = c.Principal is null ? null : KullaniciNo(c.Principal);
        var db = c.HttpContext.RequestServices.GetRequiredService<VeriTabani>();
        var k = no is null ? null : await db.Kullanicilar.AsNoTracking().Include(x => x.Okul).FirstOrDefaultAsync(x => x.Id == no);
        var gecerli = k is not null && k.Etkin
            && k.GuvenlikDamgasi == c.Principal!.FindFirstValue(DamgaBilgisi)
            && (k.Rol != Rol.OkulYoneticisi || k.Okul is { Etkin: true });
        if (!gecerli)
        {
            c.RejectPrincipal();
            await c.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    // Rol kapısı (bütün /yonetim istekleri için tek yerde):
    //  - /yonetim/sistem… yalnızca sistem yöneticisi, /yonetim/ilce… yalnızca ilçe izleyicisi,
    //  - giriş, çıkış, şifre ve erişim sayfaları dışındaki bütün diğer /yonetim sayfaları yalnızca okul yöneticisi.
    //  - Geçici şifreli hesap, kendi şifresini belirleyene kadar yalnızca şifre sayfasını (ve çıkışı) açabilir.
    // Yeni bir sayfa eklenip burada anılmazsa, varsayılan olarak yalnızca okul yöneticisine açılır.
    public static async Task RolKapisi(HttpContext http, Func<Task> sonraki)
    {
        var yol = http.Request.Path;
        var kullanici = http.User;
        if (yol.StartsWithSegments("/yonetim") && kullanici.Identity?.IsAuthenticated == true)
        {
            bool Altinda(string s) => yol.StartsWithSegments(s, StringComparison.OrdinalIgnoreCase);
            var serbest = Altinda("/yonetim/giris") || Altinda("/yonetim/cikis") || Altinda("/yonetim/erisim");
            if (kullanici.HasClaim(GeciciBilgisi, "1") && !serbest && !Altinda("/yonetim/sifre"))
            {
                http.Response.Redirect("/yonetim/sifre");
                return;
            }
            bool izinli = serbest || Altinda("/yonetim/sifre") ||
                (Altinda("/yonetim/sistem") ? kullanici.IsInRole("Sistem")
                 : Altinda("/yonetim/ilce") ? kullanici.IsInRole("Ilce")
                 : kullanici.IsInRole("Okul"));
            if (!izinli)
            {
                http.Response.Redirect("/yonetim/erisim");
                return;
            }
        }
        await sonraki();
    }
}
