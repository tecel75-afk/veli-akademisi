namespace VeliAkademisi.Yonetim;

// Yönetici girişinin kilidi: bir kullanıcı adıyla art arda 5 hatalı denemeden sonra o ad 15 dakika kilitlenir.
// Kilit kullanıcı adına göredir: bir kişinin hataları başka okulların yöneticilerini kilitlemez.
// Sistemde olmayan adlar da aynı biçimde sayılır (adın var olup olmadığı kilitten anlaşılmaz).
// IP adresi tutulmaz (KVKK, tanım 16.6). Yalnızca bellekte tutulur; site yeniden başlarsa sıfırlanır.
public class GirisKilidi
{
    const int EnCokHata = 5;
    const int EnCokKayit = 10_000;   // bellek sınırı: çok sayıda farklı adla deneme yapılırsa eski kayıtlar atılır
    static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);

    record Durum(int HataSayisi, DateTime KilitBitisi, DateTime SonDeneme);

    readonly object _kilit = new();
    readonly Dictionary<string, Durum> _durumlar = new();

    // Bu ad şu an kilitli mi? Kilitliyse kalan süreyi de verir.
    public bool KilitliMi(string ad, out TimeSpan kalan)
    {
        lock (_kilit)
        {
            kalan = _durumlar.TryGetValue(ad, out var d) ? d.KilitBitisi - DateTime.UtcNow : TimeSpan.Zero;
            return kalan > TimeSpan.Zero;
        }
    }

    public void Hatali(string ad)
    {
        lock (_kilit)
        {
            var simdi = DateTime.UtcNow;
            if (_durumlar.Count >= EnCokKayit) Temizle(simdi);
            var d = _durumlar.GetValueOrDefault(ad) ?? new Durum(0, DateTime.MinValue, simdi);
            var sayi = d.HataSayisi + 1;
            _durumlar[ad] = sayi >= EnCokHata
                ? new Durum(0, simdi + KilitSuresi, simdi)
                : d with { HataSayisi = sayi, SonDeneme = simdi };
        }
    }

    public void Basarili(string ad)
    {
        lock (_kilit) { _durumlar.Remove(ad); }
    }

    // Kilidi bitmiş ve 15 dakikadır denenmemiş kayıtlar atılır; yine de yer yoksa en eski denenen kayıt atılır.
    // (Hepsini silmek, saldırganın binlerce sahte adla kilitleri sıfırlamasına yol açardı. Her deneme
    // şifre özeti hesabı yüzünden yavaş olduğu için bir kaydı bu yolla düşürmek de çok uzun sürer.)
    void Temizle(DateTime simdi)
    {
        foreach (var (ad, d) in _durumlar.ToList())
            if (d.KilitBitisi < simdi && simdi - d.SonDeneme > KilitSuresi) _durumlar.Remove(ad);
        if (_durumlar.Count >= EnCokKayit)
            _durumlar.Remove(_durumlar.MinBy(x => x.Value.SonDeneme).Key);
    }
}
