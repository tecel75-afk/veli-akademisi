namespace VeliAkademisi.Yonetim;

// Yönetici girişinin kilidi: art arda 5 hatalı denemeden sonra giriş 15 dakika kapanır.
// Tek ve ortak bir sayaçtır (kişiye göre değil); IP adresi tutulmaz (KVKK, tanım 16.6).
// Yalnızca bellekte tutulur; site yeniden başlarsa sıfırlanır.
public class GirisKilidi
{
    const int EnCokHata = 5;
    static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);

    readonly object _kilit = new();
    int _hataSayisi;
    DateTime _kilitBitisi = DateTime.MinValue;

    // Giriş şu an kilitli mi? Kilitliyse kalan süreyi de verir.
    public bool KilitliMi(out TimeSpan kalan)
    {
        lock (_kilit)
        {
            kalan = _kilitBitisi - DateTime.UtcNow;
            return kalan > TimeSpan.Zero;
        }
    }

    public void Hatali()
    {
        lock (_kilit)
        {
            _hataSayisi++;
            if (_hataSayisi >= EnCokHata)
            {
                _kilitBitisi = DateTime.UtcNow + KilitSuresi;
                _hataSayisi = 0;
            }
        }
    }

    public void Basarili()
    {
        lock (_kilit) { _hataSayisi = 0; }
    }
}
