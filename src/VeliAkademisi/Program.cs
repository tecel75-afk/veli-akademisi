// Uygulamanın başlangıç noktası.
// Şimdilik yalnızca derleme denemesi için: ana sayfa tek satırlık bir yazı döndürür.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// "/" adresine gelen isteğe düz bir yazıyla cevap ver.
app.MapGet("/", () => "Veli Akademisi çalışıyor.");

app.Run();
