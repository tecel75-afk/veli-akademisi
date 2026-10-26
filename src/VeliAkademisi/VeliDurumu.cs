using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

namespace VeliAkademisi;

// Bir hikâyenin veli açısından durumu.
public enum HikayeDurumu { Kilitli, Baslanmadi, DevamEdiyor, Tamamlandi }

// Bir velinin 8 hikâyedeki durumunu hesaplar. Kurallar (tanım belgesi, bölüm 6):
//  - Sıralı açılma: m1 her zaman açıktır; diğerleri, sıradaki bir önceki hikâye tamamlanınca açılır.
//  - Bağlantı açılınca gidilecek hikâye: tamamlanmamış en ileri açık hikâye;
//    hepsi tamamlanmışsa en son bakılan hikâye.
//  - Başlangıç adımı: yarım kalmış hikâyede son bulunulan adım; başlanmamış ya da tamamlanmış hikâyede baş (0).
public class VeliDurumu
{
    readonly Dictionary<string, Ilerleme> _ilerlemeler;

    VeliDurumu(IEnumerable<Ilerleme> ilerlemeler) => _ilerlemeler = ilerlemeler.ToDictionary(x => x.Hikaye);

    public static async Task<VeliDurumu> Hesapla(VeriTabani db, int veliId) =>
        new(await db.Ilerlemeler.Where(x => x.VeliId == veliId).ToListAsync());

    public HikayeDurumu Durum(string hikaye)
    {
        if (_ilerlemeler.TryGetValue(hikaye, out var i))
            return i.Tamamlandi ? HikayeDurumu.Tamamlandi : HikayeDurumu.DevamEdiyor;
        return AcikMi(hikaye) ? HikayeDurumu.Baslanmadi : HikayeDurumu.Kilitli;
    }

    public bool AcikMi(string hikaye)
    {
        int sira = Hikayeler.SiraNo(hikaye);
        if (sira == 0) return true;
        // Bir kez açılmış (ilerlemesi olan) hikâye hep açık kalır.
        if (_ilerlemeler.ContainsKey(hikaye)) return true;
        return _ilerlemeler.TryGetValue(Hikayeler.Sira[sira - 1], out var onceki) && onceki.Tamamlandi;
    }

    // Bağlantı açılınca gidilecek hikâye.
    public string AcilacakHikaye()
    {
        var tamamlanmamis = Hikayeler.Sira.Where(h => AcikMi(h) && Durum(h) != HikayeDurumu.Tamamlandi).ToList();
        if (tamamlanmamis.Count > 0) return tamamlanmamis[^1];          // en ileri olanı
        return _ilerlemeler.Values.OrderByDescending(x => x.SonGuncelleme).First().Hikaye;  // hepsi bitmiş
    }

    // Hikâye sayfası açılınca başlanacak adım ve "yarım kaldı mı" bilgisi.
    public (int Adim, bool YarimKaldi) Baslangic(string hikaye)
    {
        if (_ilerlemeler.TryGetValue(hikaye, out var i) && !i.Tamamlandi && i.SonAdim > 0)
            return (i.SonAdim, true);
        return (0, false);
    }
}
