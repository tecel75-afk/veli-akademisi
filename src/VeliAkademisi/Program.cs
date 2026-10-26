// Uygulamanın başlangıç noktası.

using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı dosyasının yeri yapılandırmadan okunur ("VeriTabani:Yol").
// Verilmezse uygulama klasörünün içindeki "veri" klasörü kullanılır.
// Sunucuda bu yol, sitenin dışındaki bir klasöre ayarlanır (wwwroot dışında).
var dbYolu = builder.Configuration["VeriTabani:Yol"];
if (string.IsNullOrWhiteSpace(dbYolu))
    dbYolu = Path.Combine(builder.Environment.ContentRootPath, "veri", "veliakademisi.db");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbYolu))!);

builder.Services.AddDbContext<VeriTabani>(o => o.UseSqlite($"Data Source={dbYolu}"));

var app = builder.Build();

// Veritabanı dosyası yoksa tablolarla birlikte oluşturulur; varsa dokunulmaz.
using (var kapsam = app.Services.CreateScope())
{
    kapsam.ServiceProvider.GetRequiredService<VeriTabani>().Database.EnsureCreated();
}

// "/" adresine gelen isteğe düz bir yazıyla cevap ver (geçici).
app.MapGet("/", () => "Veli Akademisi çalışıyor.");

app.Run();
