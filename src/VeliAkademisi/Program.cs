// Uygulamanın başlangıç noktası.
//
// İki biçimde çalışır:
//   1) Site olarak (normal):            dotnet VeliAkademisi.dll
//   2) Komut olarak, veli eklemek için: dotnet VeliAkademisi.dll veli-ekle "Ayşe Hanım" "4-A"
//      (Yönetim sayfası hazır olana kadar deneme velisi oluşturmak için.)

using Microsoft.EntityFrameworkCore;
using VeliAkademisi;
using VeliAkademisi.Veri;

var komut = args.Length > 0 && args[0] == "veli-ekle";

// Komut biçiminde, komutun kendi bilgileri site ayarı sanılmasın diye siteye iletilmez.
var builder = WebApplication.CreateBuilder(komut ? Array.Empty<string>() : args);
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
Directory.CreateDirectory(Path.GetDirectoryName(dbYolu)!);

builder.Services.AddDbContext<VeriTabani>(o => o.UseSqlite($"Data Source={dbYolu}"));
builder.Services.AddSingleton(new HikayeVerisi(icerikYolu));   // 8 hikâyenin steps.json bilgileri
builder.Services.AddSingleton(new HizSiniri(120));             // kayıt uçları: dakikada 120 istek/kod

var app = builder.Build();

// Veritabanı dosyası yoksa tablolarla birlikte oluşturulur; varsa dokunulmaz.
using (var kapsam = app.Services.CreateScope())
{
    kapsam.ServiceProvider.GetRequiredService<VeriTabani>().Database.EnsureCreated();
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

// Her cevaba eklenen başlıklar:
// - Referrer-Policy: dış bağlantılara (ör. Tohum) velinin kodunu taşıyan adres gönderilmez.
// - X-Robots-Tag: veli sayfaları arama motorlarına kapalıdır.
app.Use(async (http, sonraki) =>
{
    http.Response.Headers["Referrer-Policy"] = "no-referrer";
    if (http.Request.Path.StartsWithSegments("/v"))
        http.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    await sonraki();
});

// "/" adresine gelen isteğe düz bir yazıyla cevap ver (geçici).
app.MapGet("/", () => "Veli Akademisi çalışıyor.");

// wwwroot klasöründeki dosyalar (şimdilik yalnızca kanca.js) doğrudan sunulur.
app.UseStaticFiles();

VeliSayfalari.Ekle(app, icerikYolu, sesYolu);
KayitApi.Ekle(app);

app.Run();
return 0;
