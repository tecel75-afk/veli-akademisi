using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

namespace VeliAkademisi;

// Tarayıcıdaki kancanın (kanca.js) gönderdiği istekler.
public record IlerlemeIstegi(string? Kod, string? Hikaye, int Adim);
public record CevapIstegi(string? Kod, string? Hikaye, int Adim, int Secim);

// Kayıt uçları:
//   POST /api/ilerleme  {kod, hikaye, adim}         → velinin bulunduğu adımı kaydeder
//   POST /api/cevap     {kod, hikaye, adim, secim}  → sorudaki seçimi kaydeder (0=A, 1=B, 2=C)
// Başarılıysa 204 (içeriksiz) döner. Hatalı veri 400, bilinmeyen kod 404, hız sınırı aşımı 429.
public static class KayitApi
{
    public static void Ekle(WebApplication app)
    {
        app.MapPost("/api/ilerleme", async (IlerlemeIstegi istek, VeriTabani db, HikayeVerisi hv, HizSiniri hiz) =>
        {
            var (veli, hata) = await Denetle(db, hiz, istek.Kod, istek.Hikaye);
            if (hata is not null) return hata;
            var bilgi = hv.Bilgi(istek.Hikaye!);
            if (istek.Adim < 0 || istek.Adim >= bilgi.Toplam) return Results.BadRequest();

            var simdi = DateTime.UtcNow;
            var ilerleme = await db.Ilerlemeler.FirstOrDefaultAsync(x => x.VeliId == veli!.Id && x.Hikaye == istek.Hikaye);
            if (ilerleme is null)
            {
                ilerleme = new Ilerleme { VeliId = veli!.Id, Hikaye = istek.Hikaye!, IlkGiris = simdi };
                db.Ilerlemeler.Add(ilerleme);
            }
            ilerleme.SonAdim = istek.Adim;
            ilerleme.EnIleriAdim = Math.Max(ilerleme.EnIleriAdim, istek.Adim);
            // Son adım kapanış ekranıdır; oraya ulaşılınca hikâye tamamlanmış sayılır ve öyle kalır.
            if (istek.Adim == bilgi.Toplam - 1) ilerleme.Tamamlandi = true;
            ilerleme.SonGuncelleme = simdi;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapPost("/api/cevap", async (CevapIstegi istek, VeriTabani db, HikayeVerisi hv, HizSiniri hiz) =>
        {
            var (veli, hata) = await Denetle(db, hiz, istek.Kod, istek.Hikaye);
            if (hata is not null) return hata;
            // Adım gerçekten çoktan seçmeli bir soru olmalı ve seçilen şık o soruda bulunmalı.
            // (Düşünme soruları burada hiç yoktur; onlar için kayıt yapılmaz.)
            if (!hv.Bilgi(istek.Hikaye!).Sorular.TryGetValue(istek.Adim, out var soru)) return Results.BadRequest();
            if (istek.Secim < 0 || istek.Secim >= soru.Siklar.Count) return Results.BadRequest();

            db.Cevaplar.Add(new Cevap
            {
                VeliId = veli!.Id, Hikaye = istek.Hikaye!, Adim = istek.Adim, Secim = istek.Secim, Zaman = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    // Ortak denetim: hikâye geçerli mi, kod geçerli bir veliye ait mi, hız sınırı aşıldı mı?
    // Not: kilit denetimi (16.2) 2. Günde eklenecek.
    static async Task<(Veli? Veli, IResult? Hata)> Denetle(VeriTabani db, HizSiniri hiz, string? kod, string? hikaye)
    {
        if (hikaye is null || !Hikayeler.GecerliMi(hikaye)) return (null, Results.BadRequest());
        if (kod is null || !KodUretici.BicimDogruMu(kod)) return (null, Results.NotFound());
        var veli = await db.Veliler.FirstOrDefaultAsync(x => x.Kod == kod);
        if (veli is null) return (null, Results.NotFound());
        if (!hiz.IzinVarMi(kod)) return (null, Results.StatusCode(429));
        return (veli, null);
    }
}
