using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Bir okulun yöneticileri üzerindeki işlemler; iki sayfa kullanır:
//   /yonetim/yoneticiler           okul yöneticisi, kendi okulu (Yoneticiler.cshtml)
//   /yonetim/sistem/okul/{id}      sistem yöneticisi, son çare olarak (Sistem/Okul.cshtml)
//  - Yeni yönetici ekleme (geçici şifreyle açılır)
//  - Şifresini unutan yöneticiye geçici şifre verme (eski şifresi ve açık oturumları geçersiz olur)
//  - Ayrılan yöneticinin hesabını kapatma / kapalı hesabı yeniden açma (geçici şifreyle)
// Geçici şifre işlemden hemen sonra bu sayfada bir kez gösterilir; hiçbir yerde saklanmaz.
// Kullanıcılar tablosunda okul süzgeci yoktur; bu yüzden her işlemde hesabın bu okula ait olduğu ayrıca denetlenir.
public abstract class OkulYoneticileriModel : PageModel
{
    protected readonly VeriTabani _db;

    protected OkulYoneticileriModel(VeriTabani db) => _db = db;

    [BindProperty] public string AdSoyad { get; set; } = "";
    [BindProperty] public string KullaniciAdi { get; set; } = "";
    [BindProperty] public bool Anladim { get; set; }

    public List<Kullanici> Yoneticiler { get; private set; } = new();
    public int EtkinSayisi { get; private set; }
    public int BenimNo => Oturum.KullaniciNo(User) ?? 0;
    public string? Hata { get; protected set; }

    // İşlem sonucu (geçici şifre bir kez gösterilir).
    public string? SonucBasligi { get; protected set; }
    public string? SonucKullanici { get; private set; }
    public string? SonucSifre { get; private set; }

    // İşlem yapılan okul.
    protected abstract int Okul { get; }

    public async Task<IActionResult> OnPostEkleAsync()
    {
        if (!await _db.Okullar.AnyAsync(o => o.Id == Okul)) return NotFound();
        var adSoyad = (AdSoyad ?? "").Trim();
        var ad = Oturum.KullaniciAdiDuzelt(KullaniciAdi);
        Hata = await Hesaplar.Denetle(_db, adSoyad, ad);
        if (Hata is null)
        {
            var (_, gecici) = Hesaplar.GeciciSifreyleAc(_db, adSoyad, ad, Rol.OkulYoneticisi, Okul);
            await _db.SaveChangesAsync();
            (SonucBasligi, SonucKullanici, SonucSifre) = ($"{adSoyad} eklendi", ad, gecici);
            AdSoyad = KullaniciAdi = "";
            ModelState.Clear();   // form kutuları gönderilen değerlerle değil, boş olarak çizilsin
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostGeciciSifreAsync(int kullanici)
    {
        var k = await BuOkulun(kullanici);
        if (k is null) return NotFound();
        if (k.Id == BenimNo) Hata = "Kendi şifrenizi \"Şifremi değiştir\" sayfasından değiştirin.";
        else if (!k.Etkin) Hata = "Kapalı hesaba geçici şifre verilmez; önce hesabı yeniden açın.";
        else if (!Anladim) Hata = "Geçici şifre verilmedi: önce onay kutusunu işaretleyin.";
        else
        {
            var gecici = Hesaplar.GeciciSifreVer(k);
            await _db.SaveChangesAsync();
            (SonucBasligi, SonucKullanici, SonucSifre) = ($"{k.AdSoyad} için geçici şifre", k.KullaniciAdi, gecici);
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostKapatAsync(int kullanici)
    {
        var k = await BuOkulun(kullanici);
        if (k is null) return NotFound();
        if (k.Id == BenimNo) Hata = "Kendi hesabınızı kapatamazsınız; okulunuzun diğer yöneticisi kapatabilir.";
        else if (!Anladim) Hata = "Hesap kapatılmadı: önce onay kutusunu işaretleyin.";
        else
        {
            Hesaplar.Kapat(k);
            await _db.SaveChangesAsync();
            SonucBasligi = $"{k.AdSoyad} hesabı kapatıldı";
        }
        await Yukle();
        return Page();
    }

    public async Task<IActionResult> OnPostAcAsync(int kullanici)
    {
        var k = await BuOkulun(kullanici);
        if (k is null) return NotFound();
        if (k.Etkin) Hata = "Bu hesap zaten açık.";
        else
        {
            k.Etkin = true;
            var gecici = Hesaplar.GeciciSifreVer(k);
            await _db.SaveChangesAsync();
            (SonucBasligi, SonucKullanici, SonucSifre) = ($"{k.AdSoyad} hesabı yeniden açıldı", k.KullaniciAdi, gecici);
        }
        await Yukle();
        return Page();
    }

    // (Hesap numarası "kullanici" adıyla gelir; "id" sistem sayfasında okulun numarasıdır.)
    // Yalnızca bu okulun okul yöneticisi hesapları; başka okulun ya da başka roldeki hesap için null (→ 404).
    Task<Kullanici?> BuOkulun(int id) =>
        _db.Kullanicilar.FirstOrDefaultAsync(k => k.Id == id && k.OkulId == Okul && k.Rol == Rol.OkulYoneticisi);

    protected virtual async Task Yukle()
    {
        Yoneticiler = (await _db.Kullanicilar.AsNoTracking()
                .Where(k => k.OkulId == Okul && k.Rol == Rol.OkulYoneticisi).ToListAsync())
            .OrderByDescending(k => k.Etkin).ThenBy(k => k.AdSoyad, Takip.TurkceSira).ToList();
        EtkinSayisi = Yoneticiler.Count(k => k.Etkin);
    }
}
