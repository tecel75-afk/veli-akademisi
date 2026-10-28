// Uygulamanın başlangıç noktası.
//
// Site olarak çalışır (dotnet VeliAkademisi.dll) ya da sunucuda bir komut çalıştırıp çıkar
// (komutların listesi ve açıklamaları: Komutlar.cs; ör. dotnet VeliAkademisi.dll yonetici-ekle).

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

var komut = args.Length > 0 && Komutlar.VarMi(args[0]);

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

builder.Services.AddScoped<OkulKapsami>();                     // isteğin görebileceği okul (bkz. VeriTabani)
builder.Services.AddDbContext<VeriTabani>(o => o.UseSqlite($"Data Source={dbYolu}"));
builder.Services.AddSingleton(new HikayeVerisi(icerikYolu));   // 8 hikâyenin steps.json bilgileri
builder.Services.AddSingleton(new HizSiniri(120));             // kayıt uçları: dakikada 120 istek/kod
builder.Services.AddSingleton<GirisKilidi>();                  // yönetici girişi: aynı adla 5 hatada 15 dk kilit
builder.Services.AddSingleton(new VeriYolu(dbYolu));           // yedek indirme için

// Oturum çerezlerini şifreleyen anahtarlar veritabanının yanında saklanır;
// böylece site yeniden başlayınca yöneticinin oturumu düşmez.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(veriKlasoru, "anahtarlar")))
    .SetApplicationName("VeliAkademisi");

// Yönetici girişi (kişisel hesaplar): çerezli oturum. Çerez yalnızca /yonetim altında geçerlidir,
// JavaScript tarafından okunamaz, başka sitelerden gelen isteklerle gönderilmez.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/yonetim/giris";
        o.AccessDeniedPath = "/yonetim/erisim";
        o.Cookie.Name = "va_yonetim";
        o.Cookie.Path = "/yonetim";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // sunucuda HTTPS olduğu için "Secure" olur
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;                                  // işlem yaptıkça süre uzar
        // Her istekte hesap denetlenir: kapatılmış hesap, değişmiş şifre ya da pasif okul → oturum kapanır.
        o.Events.OnValidatePrincipal = Oturum.Denetle;
    });
builder.Services.AddAuthorization();

// Yönetim sayfaları (Razor Pages). /yonetim klasörünün tamamı giriş ister; giriş sayfası hariç.
// Hangi rolün hangi sayfayı açabileceği tek yerde denetlenir: Oturum.RolKapisi.
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
    using var kapsam = app.Services.CreateScope();
    return Komutlar.Calistir(args, kapsam.ServiceProvider.GetRequiredService<VeriTabani>());
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

// Okul kapsamı: veli sayfaları ve kayıt uçları veliyi gizli koduyla bulur (bütün okullarda);
// okul yöneticisi yalnızca kendi okulunu görür. Diğer istekler için kapsam boştur: veli verisi görünmez.
app.Use(async (http, sonraki) =>
{
    var k = http.RequestServices.GetRequiredService<OkulKapsami>();
    if (http.Request.Path.StartsWithSegments("/v") || http.Request.Path.StartsWithSegments("/api")) k.TumOkullar = true;
    else if (http.Request.Path.StartsWithSegments("/yonetim")) k.OkulId = Oturum.OkulNo(http.User);
    await sonraki();
});
app.Use(Oturum.RolKapisi);

app.UseAuthorization();

VeliSayfalari.Ekle(app, icerikYolu, sesYolu);
KayitApi.Ekle(app);
app.MapRazorPages();

app.Run();
return 0;
