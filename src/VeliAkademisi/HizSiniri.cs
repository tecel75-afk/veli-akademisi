using System.Collections.Concurrent;

namespace VeliAkademisi;

// Basit hız sınırı: her kod için dakikada en çok N istek.
// Yalnızca bellekte tutulur; diske hiçbir şey (IP dahil) yazılmaz. Site yeniden başlarsa sayaçlar sıfırlanır.
public class HizSiniri
{
    readonly int _dakikadaEnCok;
    readonly ConcurrentDictionary<string, (long Dakika, int Sayi)> _sayaclar = new();

    public HizSiniri(int dakikadaEnCok) => _dakikadaEnCok = dakikadaEnCok;

    // İstek sınır içindeyse true döner ve sayar; sınır aşıldıysa false.
    public bool IzinVarMi(string kod)
    {
        long buDakika = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;
        var yeni = _sayaclar.AddOrUpdate(kod,
            _ => (buDakika, 1),
            (_, eski) => eski.Dakika == buDakika ? (buDakika, eski.Sayi + 1) : (buDakika, 1));
        return yeni.Sayi <= _dakikadaEnCok;
    }
}
