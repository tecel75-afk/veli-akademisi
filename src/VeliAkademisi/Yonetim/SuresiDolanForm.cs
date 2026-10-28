using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace VeliAkademisi.Yonetim;

// Yönetim sayfalarındaki bir formun güvenlik belirteci (antiforgery) geçersiz çıkarsa çıplak "HTTP ERROR 400"
// yerine: bu sitenin güvenlik çerezleri silinir ve aynı sayfa "süresi dolmuştu" iletisiyle yeniden açılır.
// İşlem YAPILMAZ; kullanıcı formu yeniden gönderir (bkz. _Yonetim.cshtml'deki ileti).
// Neden çerezler de silinir: 28 Ekim'de bir tarayıcıda sayfa yeniden açılsa da 400 sürdü; aynı adla başka bir yola
// ya da ana alan adına (bep-uda-projeler.com) yazılmış eski bir çerezin gönderilmeye devam ettiği düşünülüyor.
// Site yeni çerez verse de o eski çerez kalıyordu; burada her iki yerdeki kopya da silinir.
public class SuresiDolanForm : IAlwaysRunResultFilter
{
    const string Onek = ".AspNetCore.Antiforgery.";

    public void OnResultExecuting(ResultExecutingContext c)
    {
        if (c.Result is not IAntiforgeryValidationFailedResult) return;
        var http = c.HttpContext;
        foreach (var ad in http.Request.Cookies.Keys.Where(k => k.StartsWith(Onek, StringComparison.Ordinal)).ToList())
        {
            foreach (var yol in new[] { "/", "/yonetim" })
            {
                http.Response.Cookies.Delete(ad, new CookieOptions { Path = yol });
                if (AnaAlan(http.Request.Host.Host) is string ana)
                    http.Response.Cookies.Delete(ad, new CookieOptions { Path = yol, Domain = ana });
            }
        }
        // Yalnızca sitenin kendi adresine dönülür; sorgu (ör. ReturnUrl) korunur.
        var sorgu = QueryHelpers.ParseQuery(http.Request.QueryString.Value)
            .Where(x => x.Key != "suresiDoldu").ToDictionary(x => x.Key, x => (string?)x.Value.ToString());
        sorgu["suresiDoldu"] = "1";
        c.Result = new RedirectResult(QueryHelpers.AddQueryString(http.Request.Path.Value ?? "/yonetim", sorgu));
    }

    public void OnResultExecuted(ResultExecutedContext c) { }

    // "veliakademisi.bep-uda-projeler.com" → "bep-uda-projeler.com"; IP adresi ya da tek parçalı adda null.
    static string? AnaAlan(string host)
    {
        if (System.Net.IPAddress.TryParse(host, out _)) return null;
        var parca = host.Split('.');
        return parca.Length >= 3 ? string.Join('.', parca.Skip(1)) : null;
    }
}
