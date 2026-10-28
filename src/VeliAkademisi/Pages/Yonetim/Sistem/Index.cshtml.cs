using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim.Sistem;

// Sistem yöneticisinin ana sayfası: /yonetim/sistem (yalnızca sistem yöneticisi; bkz. Oturum.RolKapisi).
//  - Okullar: liste, yeni okul (iki yöneticisiyle birlikte açılır), okul ayrıntısına bağlantı
//  - İlçe izleyicileri: hesap açma, geçici şifre, kapatma / yeniden açma
// Sistem yöneticisi veli verisi (ad, sınıf, ilerleme, cevap) görmez; burada yalnızca okul ve hesap bilgisi vardır.
public class IndexModel : PageModel
{
    readonly VeriTabani _db;
    readonly VeriYolu _yol;

    public IndexModel(VeriTabani db, VeriYolu yol)
    {
        _db = db;
        _yol = yol;
    }

    // Haftalık yedek: son indirme ve hatırlatma.
    public DateTime? SonYedek { get; private set; }
    public bool YedekHatirlat => SonYedek is null || DateTime.UtcNow - SonYedek > Yedek.HatirlatmaSuresi;
    [BindProperty] public string YedekSifre { get; set; } = "";
    [BindProperty] public string YedekSifreTekrar { get; set; } = "";
    [BindProperty] public bool SifreyiYazdim { get; set; }

    // /yonetim/sistem?handler=yedek (POST): veritabanının tam kopyası, yazılan şifreyle şifreli zip olarak iner.
    public async Task<IActionResult> OnPostYedekAsync()
    {
        if ((YedekSifre ?? "").Length < Yedek.SifreEnAz) Hata = $"Yedek şifresi en az {Yedek.SifreEnAz} karakter olmalı. Yedek indirilmedi.";
        else if (YedekSifre != YedekSifreTekrar) Hata = "Yedek şifresi iki kutuda aynı değil. Yedek indirilmedi.";
        else if (!SifreyiYazdim) Hata = "Yedek indirilmedi: önce şifreyi bir yere yazdığınızı onaylayın (şifre unutulursa yedek açılmaz).";
        if (Hata is not null) { await Yukle(); return Page(); }

        var damga = (DateTime.UtcNow + TimeSpan.FromHours(3)).ToString("yyyy-MM-dd-HHmm");
        var zip = Yedek.SifreliZip(_yol.Db, YedekSifre!, $"veliakademisi-yedek-{damga}.db");
        Yedek.IndirildiYaz(_yol.Db);
        return File(zip, "application/zip", $"veli-akademisi-yedek-{damga}.zip");
    }

    public record OkulSatiri(Okul Okul, int AcikYonetici);
    public List<OkulSatiri> Okullar { get; private set; } = new();
    public List<Kullanici> Izleyiciler { get; private set; } = new();
    public string? Hata { get; private set; }
    public string? SonucBasligi { get; private set; }
    public List<(string AdSoyad, string KullaniciAdi, string Sifre)> SonucHesaplar { get; } = new();

    // Yeni okul formu: okulun adı ve ilk iki yöneticisi.
    [BindProperty] public string OkulAdi { get; set; } = "";
    [BindProperty] public string Y1AdSoyad { get; set; } = "";
    [BindProperty] public string Y1KullaniciAdi { get; set; } = "";
    [BindProperty] public string Y2AdSoyad { get; set; } = "";
    [BindProperty] public string Y2KullaniciAdi { get; set; } = "";
    // İlçe izleyicisi formu.
    [BindProperty] public string IzAdSoyad { get; set; } = "";
    [BindProperty] public string IzKullaniciAdi { get; set; } = "";
    [BindProperty] public bool Anladim { get; set; }

    public async Task OnGetAsync() => await Yukle();

    // Okul, ilk iki yöneticisiyle birlikte tek seferde açılır (iki yönetici kuralı: Hesaplar.OkulEnAzYonetici).
    public async Task<IActionResult> OnPostOkulEkleAsync()
    {
        var ad = (OkulAdi ?? "").Trim();
        var y1 = ((Y1AdSoyad ?? "").Trim(), Oturum.KullaniciAdiDuzelt(Y1KullaniciAdi));
        var y2 = ((Y2AdSoyad ?? "").Trim(), Oturum.KullaniciAdiDuzelt(Y2KullaniciAdi));
        if (ad.Length == 0) Hata = "Okulun adı boş olamaz.";
        else if (ad.Length > 150) Hata = "Okulun adı en çok 150 karakter olabilir.";
        else if (await _db.Okullar.AnyAsync(o => o.Ad == ad)) Hata = "Bu adla bir okul zaten var.";
        else if (y1.Item2 == y2.Item2) Hata = "İki yöneticinin kullanıcı adları farklı olmalı.";
        else if (await Hesaplar.Denetle(_db, y1.Item1, y1.Item2) is string h1) Hata = "1. yönetici: " + h1;
        else if (await Hesaplar.Denetle(_db, y2.Item1, y2.Item2) is string h2) Hata = "2. yönetici: " + h2;
        if (Hata is null)
        {
            // Okul ve iki yöneticisi tek işlemde: ya hepsi kaydedilir ya hiçbiri.
            await using var islem = await _db.Database.BeginTransactionAsync();
            var okul = new Okul { Ad = ad, OlusturmaTarihi = DateTime.UtcNow };
            _db.Okullar.Add(okul);
            await _db.SaveChangesAsync();
            foreach (var (adSoyad, kAdi) in new[] { y1, y2 })
            {
                var (_, gecici) = Hesaplar.GeciciSifreyleAc(_db, adSoyad, kAdi, Rol.OkulYoneticisi, okul.Id);
                SonucHesaplar.Add((adSoyad, kAdi, gecici));
            }
            await _db.SaveChangesAsync();
            await islem.CommitAsync();
            SonucBasligi = $"{okul.Ad} eklendi";
            OkulAdi = Y1AdSoyad = Y1KullaniciAdi = Y2AdSoyad = Y2KullaniciAdi = "";
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostIzleyiciEkleAsync()
    {
        var adSoyad = (IzAdSoyad ?? "").Trim();
        var ad = Oturum.KullaniciAdiDuzelt(IzKullaniciAdi);
        Hata = await Hesaplar.Denetle(_db, adSoyad, ad);
        if (Hata is null)
        {
            var (_, gecici) = Hesaplar.GeciciSifreyleAc(_db, adSoyad, ad, Rol.IlceIzleyicisi, null);
            await _db.SaveChangesAsync();
            SonucBasligi = $"İlçe izleyicisi {adSoyad} eklendi";
            SonucHesaplar.Add((adSoyad, ad, gecici));
            IzAdSoyad = IzKullaniciAdi = "";
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostIzleyiciGeciciSifreAsync(int id)
    {
        var k = await Izleyici(id);
        if (k is null) return NotFound();
        if (!k.Etkin) Hata = "Kapalı hesaba geçici şifre verilmez; önce hesabı yeniden açın.";
        else if (!Anladim) Hata = "Geçici şifre verilmedi: önce onay kutusunu işaretleyin.";
        else
        {
            var gecici = Hesaplar.GeciciSifreVer(k);
            await _db.SaveChangesAsync();
            SonucBasligi = $"{k.AdSoyad} için geçici şifre";
            SonucHesaplar.Add((k.AdSoyad, k.KullaniciAdi, gecici));
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostIzleyiciKapatAsync(int id)
    {
        var k = await Izleyici(id);
        if (k is null) return NotFound();
        if (!Anladim) Hata = "Hesap kapatılmadı: önce onay kutusunu işaretleyin.";
        else
        {
            Hesaplar.Kapat(k);
            await _db.SaveChangesAsync();
            SonucBasligi = $"{k.AdSoyad} hesabı kapatıldı";
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostIzleyiciAcAsync(int id)
    {
        var k = await Izleyici(id);
        if (k is null) return NotFound();
        if (k.Etkin) Hata = "Bu hesap zaten açık.";
        else
        {
            k.Etkin = true;
            var gecici = Hesaplar.GeciciSifreVer(k);
            await _db.SaveChangesAsync();
            SonucBasligi = $"{k.AdSoyad} hesabı yeniden açıldı";
            SonucHesaplar.Add((k.AdSoyad, k.KullaniciAdi, gecici));
        }
        await Yukle();
        return Page();
    }

    Task<Kullanici?> Izleyici(int id) =>
        _db.Kullanicilar.FirstOrDefaultAsync(k => k.Id == id && k.Rol == Rol.IlceIzleyicisi);

    async Task Yukle()
    {
        SonYedek = Yedek.SonIndirme(_yol.Db);
        var acik = await _db.Kullanicilar.Where(k => k.Rol == Rol.OkulYoneticisi && k.Etkin && k.OkulId != null)
            .GroupBy(k => k.OkulId!.Value).Select(g => new { g.Key, Sayi = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Sayi);
        Okullar = (await _db.Okullar.AsNoTracking().ToListAsync())
            .OrderByDescending(o => o.Etkin).ThenBy(o => o.Ad, Takip.TurkceSira)
            .Select(o => new OkulSatiri(o, acik.GetValueOrDefault(o.Id))).ToList();
        Izleyiciler = (await _db.Kullanicilar.AsNoTracking().Where(k => k.Rol == Rol.IlceIzleyicisi).ToListAsync())
            .OrderByDescending(k => k.Etkin).ThenBy(k => k.AdSoyad, Takip.TurkceSira).ToList();
    }
}
