using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

namespace VeliAkademisi;

// Tarayıcıdaki kancanın (kanca.js) gönderdiği istekler.
public record IlerlemeIstegi(string? Kod, string? Hikaye, int Adim);
public record CevapIstegi(string? Kod, string? Hikaye, int Adim, int Secim);

// Kayıt uçları:
//   POST /api/ilerleme  {kod, hikaye, adim}         → velinin bulunduğu adımı kaydeder
//   POST /api/cevap     {kod, hikaye, adim, secim}  → sorudaki seçimi kaydeder (0=A, 1=B, 2=C)
//   GET  /api/durum?kod=..&hikaye=..                → sayfanın açılışta sorduğu bilgiler (bkz. aşağıda)
// Başarılıysa 204 (içeriksiz) ya da 200 döner. Hatalı veri 400, kilitli hikâye 403,
// bilinmeyen kod 404, hız sınırı aşımı 429.
public static class KayitApi
{
    public static void Ekle(WebApplication app)
    {
        // Sayfa açılırken: bu hikâyede nereden başlanacağı ve 8 hikâyenin durumu (Modüller listesi için).
        app.MapGet("/api/durum", async (string? kod, string? hikaye, VeriTabani db, HikayeVerisi hv, HizSiniri hiz) =>
        {
            var (veli, hata) = await Denetle(db, hiz, kod, hikaye);
            if (hata is not null) return hata;
            var durum = await VeliDurumu.Hesapla(db, veli!.Id);
            var (adim, yarim) = durum.Baslangic(hikaye!);
            var m = hv.Bilgi(hikaye!).Materyaller;
            int sira = Hikayeler.SiraNo(hikaye!);
            return Results.Ok(new
            {
                // Hikâye sonu: materyaller ve sıradaki hikâye (son hikâyede null).
                materyaller = new
                {
                    gorseller = m.Gorseller.Select(g => new { dosya = g.Dosya, baslik = g.Baslik }),
                    baglanti = m.Baglanti is null ? null : new { adres = m.Baglanti.Adres, yazi = m.Baglanti.Yazi },
                    pdf = m.Pdf is null ? null : new { dosya = m.Pdf.Dosya, yazi = m.Pdf.Yazi },
                },
                sonraki = sira + 1 < Hikayeler.Sira.Count ? Hikayeler.Sira[sira + 1] : null,
                hikaye,
                baslangicAdimi = adim,     // yarım kaldıysa son bulunulan adım, değilse 0
                yarimKaldi = yarim,        // true ise düğme "Kaldığınız yerden devam edin" olur
                hikayeler = Hikayeler.Tanimlar.Select(t => new
                {
                    kod = t.Kod, modul = t.Modul, ad = t.Ad,
                    durum = durum.Durum(t.Kod) switch
                    {
                        HikayeDurumu.Tamamlandi => "tamamlandi",
                        HikayeDurumu.DevamEdiyor => "devam",
                        HikayeDurumu.Baslanmadi => "baslanmadi",
                        _ => "kilitli",
                    },
                }),
            });
        });

        app.MapPost("/api/ilerleme", async (IlerlemeIstegi istek, VeriTabani db, HikayeVerisi hv, HizSiniri hiz) =>
        {
            var (veli, hata) = await Denetle(db, hiz, istek.Kod, istek.Hikaye);
            if (hata is not null) return hata;
            var bilgi = hv.Bilgi(istek.Hikaye!);
            if (istek.Adim < 0 || istek.Adim >= bilgi.Toplam) return Results.BadRequest();

            // Tek komutla "yoksa ekle, varsa güncelle" (SQLite UPSERT). Önce okuyup sonra yazmak, aynı veliden aynı anda
            // gelen ilk kayıtlarda ikisinin de satır eklemeye çalışmasına ve birinin 500 ile kaybolmasına yol açıyordu
            // (28 Ekim, eşzamanlı deneme: 400 kayıttan 12'si).
            //  - SonAdim: son gelen adım; EnIleriAdim: geri dönülse de küçülmez.
            //  - Son adım kapanış ekranıdır; oraya ulaşılınca hikâye tamamlanmış sayılır ve öyle kalır.
            var simdi = DateTime.UtcNow;
            var tamamlandi = istek.Adim == bilgi.Toplam - 1;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Ilerlemeler (VeliId, Hikaye, EnIleriAdim, SonAdim, Tamamlandi, IlkGiris, SonGuncelleme)
                VALUES ({veli!.Id}, {istek.Hikaye}, {istek.Adim}, {istek.Adim}, {tamamlandi}, {simdi}, {simdi})
                ON CONFLICT (VeliId, Hikaye) DO UPDATE SET
                    SonAdim = excluded.SonAdim,
                    EnIleriAdim = max(EnIleriAdim, excluded.EnIleriAdim),
                    Tamamlandi = Tamamlandi OR excluded.Tamamlandi,
                    SonGuncelleme = excluded.SonGuncelleme
                """);
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

    // Ortak denetim: hikâye geçerli mi, kod geçerli bir veliye ait mi, hız sınırı aşıldı mı,
    // hikâye bu veli için açık mı (kilitli hikâyeye kayıt yapılmaz, 16.2)?
    static async Task<(Veli? Veli, IResult? Hata)> Denetle(VeriTabani db, HizSiniri hiz, string? kod, string? hikaye)
    {
        if (hikaye is null || !Hikayeler.GecerliMi(hikaye)) return (null, Results.BadRequest());
        if (kod is null) return (null, Results.NotFound());
        var veli = await VeliSayfalari.VeliBul(db, kod);   // biçim, kod ve okulun etkin olması denetlenir
        if (veli is null) return (null, Results.NotFound());
        if (!hiz.IzinVarMi(kod)) return (null, Results.StatusCode(429));
        if (!(await VeliDurumu.Hesapla(db, veli.Id)).AcikMi(hikaye)) return (null, Results.StatusCode(403));
        return (veli, null);
    }
}
