// Uygulamanın başlangıç noktası.
//
// Üç biçimde çalışır:
//   1) Site olarak (normal):              dotnet VeliAkademisi.dll
//   2) Veli eklemek için:                 dotnet VeliAkademisi.dll veli-ekle "Ayşe Hanım" "4-A"
//      (Yönetim sayfası hazır olana kadar deneme velisi oluşturmak için.)
//   3) Yönetici şifresinin özeti için:    dotnet VeliAkademisi.dll sifre-hash
//      (Şifreyi sorar, ekranda göstermez; çıkan özet yapılandırmaya "Yonetim:SifreHash" olarak yazılır.)

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

if (args.Length > 0 && args[0] == "sifre-hash") return SifreHashKomutu();

var komut = args.Length > 0 && args[0] == "veli-ekle";

// Komut biçiminde, komutun kendi bilgileri site ayarı sanılmasın diye siteye iletilmez.
// Kök klasör, komutun çalıştırıldığı yer değil, sitenin kendi klasörüdür; böylece wwwroot ve icerik
// her zaman derlenmiş paketin içinden okunur.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = komut ? Array.Empty<string>() : args,
    ContentRootPath = AppContext.BaseDirectory,
});
var kok = builder.Environment.ContentRootPath;

// Yollar yapılandırmadan okunur; verilmezse uygulama klasörünün içi kullanılır.
// Sunucuda üçü de ayarlanır (bkz. deploy/KURULUM.md).
string Yol(string anahtar, params string[] varsayilan)
{
    var y = builder.Configuration[anahtar];
    return Path.GetFullPath(string.IsNullOrWhiteSpace(y) ? Path.Combine(new[] { kok }.Concat(varsayilan).ToArray()) : y);
}
var dbYolu = Yol("VeriTabani:Yol", "veri", "veliakademisi.db"); // veritabanı dosyası
var icerikYolu = Yol("Icerik:Yol", "icerik");                     // hikâye sayfaları ve görseller (depodan gelir)
var sesYolu = Yol("Ses:Yol", "ses");                              // ses dosyaları (sunucuya ayrıca yüklenir)
var veriKlasoru = Path.GetDirectoryName(dbYolu)!;
Directory.CreateDirectory(veriKlasoru);

builder.Services.AddDbContext<VeriTabani>(o => o.UseSqlite($"Data Source={dbYolu}"));
builder.Services.AddSingleton(new HikayeVerisi(icerikYolu));   // 8 hikâyenin steps.json bilgileri
builder.Services.AddSingleton(new HizSiniri(120));             // kayıt uçları: dakikada 120 istek/kod
builder.Services.AddSingleton<GirisKilidi>();                  // yönetici girişi: 5 hatada 15 dk kilit
builder.Services.AddSingleton(new VeriYolu(dbYolu));           // yedek indirme için

// Oturum çerezlerini şifreleyen anahtarlar veritabanının yanında saklanır;
// böylece site yeniden başlayınca yöneticinin oturumu düşmez.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(veriKlasoru, "anahtarlar")))
    .SetApplicationName("VeliAkademisi");

// Yönetici girişi: çerezli oturum. Çerez yalnızca /yonetim altında geçerlidir,
// JavaScript tarafından okunamaz, başka sitelerden gelen isteklerle gönderilmez.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/yonetim/giris";
        o.Cookie.Name = "va_yonetim";
        o.Cookie.Path = "/yonetim";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // sunucuda HTTPS olduğu için "Secure" olur
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;                                  // işlem yaptıkça süre uzar
    });
builder.Services.AddAuthorization();

// Yönetim sayfaları (Razor Pages). /yonetim klasörünün tamamı giriş ister; giriş sayfası hariç.
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Yonetim");
    o.Conventions.AllowAnonymousToPage("/Yonetim/Giris");
});
builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);   // adresler küçük harfle üretilir
// Sayfalarda Türkçe harfler (ş, ğ, İ...) kodlanmadan, olduğu gibi yazılır. (Güvenlik için gereken
// < > & " gibi işaretler yine kodlanır.)
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

var app = builder.Build();

// Veritabanı yoksa oluşturulur; varsa eksik şema güncellemeleri (Veri/Gecisler) sırayla uygulanır.
// Eski yöntemle (güncelleme kaydı olmadan) kurulmuş bir veritabanına dokunulmaz: site açılmaz ve neden açılmadığını yazar.
using (var kapsam = app.Services.CreateScope())
{
    var hata = VeriTabaniHazirlik.Hazirla(kapsam.ServiceProvider.GetRequiredService<VeriTabani>());
    if (hata is not null) { Console.Error.WriteLine(hata); return 1; }
}

if (komut)
{
    // veli-ekle "Ad" "Sınıf" → veliyi ekler, kodunu ve bağlantı yolunu yazar, çıkar.
    if (args.Length != 3) { Console.Error.WriteLine("Kullanım: veli-ekle \"Ad\" \"Sınıf\""); return 1; }
    using var kapsam = app.Services.CreateScope();
    var db = kapsam.ServiceProvider.GetRequiredService<VeriTabani>();
    var veli = new Veli { Ad = args[1], Sinif = args[2], Kod = KodUretici.Uret(), OlusturmaTarihi = DateTime.UtcNow };
    db.Veliler.Add(veli);
    db.SaveChanges();
    Console.WriteLine($"Eklendi: {veli.Ad} ({veli.Sinif})");
    Console.WriteLine($"Kod: {veli.Kod}");
    Console.WriteLine($"Bağlantı yolu: /v/{veli.Kod}");
    return 0;
}

// Sunucuda site nginx'in arkasında çalışır. nginx'in bildirdiği "isteğin HTTPS ile geldiği" bilgisi
// kabul edilir (yalnızca aynı makineden gelirse); böylece çerezler "Secure" işaretlenir.
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto });

// Her cevaba eklenen başlıklar:
// - Referrer-Policy: dış bağlantılara (ör. Tohum) velinin kodunu taşıyan adres gönderilmez.
// - X-Robots-Tag: veli ve yönetim sayfaları arama motorlarına kapalıdır.
app.Use(async (http, sonraki) =>
{
    http.Response.Headers["Referrer-Policy"] = "no-referrer";
    if (http.Request.Path.StartsWithSegments("/v") || http.Request.Path.StartsWithSegments("/yonetim"))
        http.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    await sonraki();
});

// "/" adresine gelen isteğe düz bir yazıyla cevap ver (geçici).
app.MapGet("/", () => "Veli Akademisi çalışıyor.");

// wwwroot klasöründeki dosyalar (kanca.js, moduller.js) doğrudan sunulur.
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

VeliSayfalari.Ekle(app, icerikYolu, sesYolu);
KayitApi.Ekle(app);
app.MapRazorPages();

app.Run();
return 0;

// sifre-hash: şifreyi ekranda göstermeden iki kez sorar, özetini yazar.
// (Girdi bir dosyadan ya da başka bir komuttan geliyorsa, örneğin testlerde, tek satır okunur.)
static int SifreHashKomutu()
{
    string Oku(string soru)
    {
        Console.Error.Write(soru);
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var s = new System.Text.StringBuilder();
        while (true)
        {
            var tus = Console.ReadKey(intercept: true);
            if (tus.Key == ConsoleKey.Enter) break;
            if (tus.Key == ConsoleKey.Backspace) { if (s.Length > 0) s.Length--; continue; }
            if (!char.IsControl(tus.KeyChar)) s.Append(tus.KeyChar);
        }
        Console.Error.WriteLine();
        return s.ToString();
    }

    var sifre = Oku("Yönetici şifresi: ");
    if (sifre.Length < 10) { Console.Error.WriteLine("Şifre en az 10 karakter olmalı."); return 1; }
    if (!Console.IsInputRedirected && Oku("Şifre (tekrar): ") != sifre)
    {
        Console.Error.WriteLine("İki şifre aynı değil.");
        return 1;
    }
    Console.WriteLine(SifreOzeti.Olustur(sifre));
    return 0;
}
