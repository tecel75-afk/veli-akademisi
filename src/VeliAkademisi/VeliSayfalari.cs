using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

namespace VeliAkademisi;

// Velinin gördüğü adresler:
//   /v/{kod}                      → velinin açacağı hikâyeye yönlendirir
//   /v/{kod}/{hikaye}/            → hikâye sayfası (icerik/{hikaye}/index.html)
//   /v/{kod}/{hikaye}/img/{dosya} → hikâyenin görselleri (depodaki icerik klasöründen)
//   /v/{kod}/{hikaye}/ses/{dosya} → hikâyenin sesleri (sunucudaki ayrı ses klasöründen)
public static partial class VeliSayfalari
{
    const string GecersizMesaj = "Bu bağlantı artık geçerli değil. Lütfen rehber öğretmeninizle iletişime geçin.";

    static readonly FileExtensionContentTypeProvider Turler = new();

    // Hikâye sayfasının sonuna eklenen satırlar: kayıt kancası ve Modüller düğmesi (wwwroot klasöründen).
    const string KancaEtiketi = "\n<script src=\"/kanca.js\"></script>\n<script src=\"/moduller.js\"></script>\n";

    public static void Ekle(WebApplication app, string icerikYolu, string sesYolu)
    {
        var v = app.MapGroup("/v/{kod}");

        // Velinin bağlantısı: velinin açacağı hikâyeye yönlendirir
        // (tamamlanmamış en ileri hikâye; hepsi bitmişse en son bakılan).
        v.MapGet("/", async (string kod, VeriTabani db) =>
        {
            var veli = await VeliBul(db, kod);
            if (veli is null) return Gecersiz();
            veli.SonGiris = DateTime.UtcNow;
            await db.SaveChangesAsync();
            var durum = await VeliDurumu.Hesapla(db, veli.Id);
            return Results.Redirect($"/v/{kod}/{durum.AcilacakHikaye()}/");
        });

        // Hikâye sayfası. Adres "/" ile bitmezse "/" eklenmiş hâline yönlendirilir,
        // çünkü sayfa görsellere ve seslere göreli yolla (img/..., ses/...) ulaşır.
        // (Adres eşleştirme sondaki "/" işaretini ayırt etmediği için tek tanımda bakılır.)
        v.MapGet("/{hikaye}", async (string kod, string hikaye, HttpContext http, VeriTabani db) =>
        {
            if (!Hikayeler.GecerliMi(hikaye)) return Results.NotFound();
            var veli = await VeliBul(db, kod);
            if (veli is null) return Gecersiz();
            // Kilitli hikâyenin adresi elle yazılırsa açılmaz; veli kendi bağlantısına yönlendirilir (16.2).
            if (!(await VeliDurumu.Hesapla(db, veli.Id)).AcikMi(hikaye)) return Results.Redirect($"/v/{kod}");
            if (!http.Request.Path.Value!.EndsWith('/')) return Results.Redirect($"/v/{kod}/{hikaye}/");
            // Onaylı sayfa olduğu gibi okunur; yalnızca en sonuna kayıt kancası eklenir.
            var sayfa = await File.ReadAllTextAsync(Path.Combine(icerikYolu, hikaye, "index.html"));
            return Results.Content(sayfa + KancaEtiketi, "text/html; charset=utf-8");
        });

        // Görseller ve sesler.
        v.MapGet("/{hikaye}/img/{dosya}", async (string kod, string hikaye, string dosya, VeriTabani db) =>
        {
            if (!Hikayeler.GecerliMi(hikaye) || !DosyaAdiGuvenliMi(dosya)) return Results.NotFound();
            if (!await AcikHikayeMi(db, kod, hikaye)) return Results.NotFound();
            return Dosya(Path.Combine(icerikYolu, hikaye, "img", dosya));
        });
        v.MapGet("/{hikaye}/ses/{dosya}", async (string kod, string hikaye, string dosya, VeriTabani db) =>
        {
            if (!Hikayeler.GecerliMi(hikaye) || !DosyaAdiGuvenliMi(dosya)) return Results.NotFound();
            if (!await AcikHikayeMi(db, kod, hikaye)) return Results.NotFound();
            return Dosya(Path.Combine(sesYolu, hikaye, dosya));
        });
    }

    // Kod geçerli bir veliye ait mi ve bu hikâye o veli için açık mı? (Görseller ve sesler için.)
    static async Task<bool> AcikHikayeMi(VeriTabani db, string kod, string hikaye)
    {
        var veli = await VeliBul(db, kod);
        return veli is not null && (await VeliDurumu.Hesapla(db, veli.Id)).AcikMi(hikaye);
    }

    // Kodu önce biçimce, sonra veritabanında denetler. Bulamazsa null döner.
    internal static async Task<Veli?> VeliBul(VeriTabani db, string kod)
    {
        if (!KodUretici.BicimDogruMu(kod)) return null;
        return await db.Veliler.FirstOrDefaultAsync(x => x.Kod == kod);
    }

    // Dosya varsa türüyle birlikte verir, yoksa 404. (Ses dosyası yoksa sayfa tarayıcı sesine geçer.)
    static IResult Dosya(string yol)
    {
        if (!File.Exists(yol)) return Results.NotFound();
        if (!Turler.TryGetContentType(yol, out var tur)) tur = "application/octet-stream";
        return Results.File(yol, tur, enableRangeProcessing: true);
    }

    // Yalnızca harf, rakam, alt çizgi, tire ve tek bir uzantı noktası. "../" gibi klasör atlamalarını engeller.
    static bool DosyaAdiGuvenliMi(string ad) => DosyaAdi().IsMatch(ad);

    [GeneratedRegex(@"^[A-Za-z0-9_-]+\.[A-Za-z0-9]+$")]
    private static partial Regex DosyaAdi();

    // Geçersiz ya da silinmiş kod: nazik mesaj, hata ayrıntısı yok.
    static IResult Gecersiz() => Results.Content($$"""
        <!doctype html><html lang="tr"><head><meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <meta name="robots" content="noindex"><title>Veli Akademisi</title>
        <style>body{font-family:system-ui,sans-serif;margin:0;padding:32px 16px;background:#F6F8F7;color:#1F2D2B}
        p{max-width:480px;margin:0 auto;font-size:20px;line-height:1.5}</style></head>
        <body><p>{{GecersizMesaj}}</p></body></html>
        """, "text/html; charset=utf-8", statusCode: 404);
}
