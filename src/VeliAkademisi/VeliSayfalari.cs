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
//   /v/{kod}/{hikaye}/materyal/{dosya} → hikâye sonundaki materyal görselleri (icerik klasöründen)
public static partial class VeliSayfalari
{
    const string GecersizMesaj = "Bu bağlantı artık geçerli değil. Lütfen rehber öğretmeninizle iletişime geçin.";

    static readonly FileExtensionContentTypeProvider Turler = new();

    // Hikâye sayfasının sonuna eklenen satırlar: kayıt kancası ve Modüller düğmesi (wwwroot klasöründen).
    const string KancaEtiketi = "\n<script src=\"/kanca.js\"></script>\n<script src=\"/moduller.js\"></script>\n";

    // Yazı tipleri (4a): onaylı sayfadaki iki Google satırı, sitenin kendi yazı tipi dosyasıyla değiştirilir.
    // Aynı yazı tipleri (Baloo 2, Nunito) kullanılır; görünüm değişmez, veliden Google'a istek gitmez (KVKK).
    // 8 sayfada da bu iki satır birebir aynıdır (26-27 Ekim incelemesi).
    const string GoogleOnBaglanti = "<link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">\n";
    const string GoogleYaziTipi = "<link rel=\"stylesheet\" href=\"https://fonts.googleapis.com/css2?family=Baloo+2:wght@600;700&family=Nunito:wght@500;700;800&display=swap\">";
    const string KendiYaziTipi = "<link rel=\"stylesheet\" href=\"/yazitipleri/yazitipleri.css\">";

    // Telefon ekranı (27 Ekim kararı, tanım 7.8): onaylı sayfalarda bu satır yok; olmayınca telefon sayfayı
    // 980 px genişliğinde çizip küçültür ve yazılar okunamaz. Başlıktan (</title>) hemen sonra eklenir.
    const string Baslik = "</title>\n";
    const string EkranSatiri = "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, viewport-fit=cover\">\n";

    // Ana ekrana ekleme (4d, tanım 6b): her veliye kendi uygulama tanımı (manifest) verilir; simge velinin kendi
    // bağlantısını açar. iPhone için ayrıca simge ve ad satırları. Uygulama adı yapılandırmadan gelir ("Uygulama:Ad").
    public const string TemaRengi = "#1E6B66";
    static string AnaEkranSatirlari(string kod, string ad) =>
        $"<link rel=\"manifest\" href=\"/v/{kod}/manifest.webmanifest\">\n" +
        $"<meta name=\"theme-color\" content=\"{TemaRengi}\">\n" +
        "<link rel=\"apple-touch-icon\" href=\"/simge/simge-180.png\">\n" +
        "<meta name=\"apple-mobile-web-app-capable\" content=\"yes\">\n" +
        "<meta name=\"mobile-web-app-capable\" content=\"yes\">\n" +
        $"<meta name=\"apple-mobile-web-app-title\" content=\"{System.Net.WebUtility.HtmlEncode(ad)}\">\n";

    // Onaylı sayfaya, veliye gönderilmeden önce yapılan eklerin tamamı (tanım belgesi bölüm 7) burada toplanır.
    internal static string SayfayiHazirla(string sayfa, string kod, string uygulamaAdi)
    {
        int b = sayfa.IndexOf(Baslik, StringComparison.Ordinal);
        if (b >= 0) sayfa = sayfa.Insert(b + Baslik.Length, EkranSatiri + AnaEkranSatirlari(kod, uygulamaAdi));
        return sayfa.Replace(GoogleOnBaglanti, "").Replace(GoogleYaziTipi, KendiYaziTipi) + KancaEtiketi;
    }

    // Velinin kendi uygulama tanımı: /v/{kod}/manifest.webmanifest
    static string UygulamaTanimi(string kod, string ad) => System.Text.Json.JsonSerializer.Serialize(new
    {
        id = $"/v/{kod}/",
        name = ad,
        short_name = ad,
        lang = "tr",
        start_url = $"/v/{kod}/",      // simgeye dokununca velinin bağlantısı açılır → kaldığı hikâyeye gider
        scope = $"/v/{kod}/",
        display = "standalone",
        background_color = "#EEF2F1",
        theme_color = TemaRengi,
        icons = new object[]
        {
            new { src = "/simge/simge-192.png", sizes = "192x192", type = "image/png", purpose = "any" },
            new { src = "/simge/simge-512.png", sizes = "512x512", type = "image/png", purpose = "any" },
            new { src = "/simge/simge-maskable-512.png", sizes = "512x512", type = "image/png", purpose = "maskable" },
        },
    });

    public static void Ekle(WebApplication app, string icerikYolu, string sesYolu)
    {
        var v = app.MapGroup("/v/{kod}");
        var uygulamaAdi = app.Configuration["Uygulama:Ad"] is { Length: > 0 } a ? a : "Veli Akademisi";

        // Velinin kendi uygulama tanımı (ana ekrana ekleme).
        v.MapGet("/manifest.webmanifest", async (string kod, VeriTabani db) =>
            await VeliBul(db, kod) is null
                ? Results.NotFound()
                : Results.Text(UygulamaTanimi(kod, uygulamaAdi), "application/manifest+json; charset=utf-8"));

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
            // Onaylı sayfa okunur; bölüm 7'deki ekler yapılarak sunulur (bkz. SayfayiHazirla).
            var sayfa = await File.ReadAllTextAsync(Path.Combine(icerikYolu, hikaye, "index.html"));
            return Results.Content(SayfayiHazirla(sayfa, kod, uygulamaAdi), "text/html; charset=utf-8");
        });

        // Görseller ve sesler.
        v.MapGet("/{hikaye}/img/{dosya}", async (string kod, string hikaye, string dosya, VeriTabani db) =>
        {
            if (!Hikayeler.GecerliMi(hikaye) || !DosyaAdiGuvenliMi(dosya)) return Results.NotFound();
            if (!await AcikHikayeMi(db, kod, hikaye)) return Results.NotFound();
            return Dosya(Path.Combine(icerikYolu, hikaye, "img", dosya));
        });
        v.MapGet("/{hikaye}/materyal/{dosya}", async (string kod, string hikaye, string dosya, VeriTabani db) =>
        {
            if (!Hikayeler.GecerliMi(hikaye) || !DosyaAdiGuvenliMi(dosya)) return Results.NotFound();
            if (!await AcikHikayeMi(db, kod, hikaye)) return Results.NotFound();
            return Dosya(Path.Combine(icerikYolu, hikaye, "materyal", dosya));
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
