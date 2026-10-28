using Microsoft.AspNetCore.Mvc;
using VeliAkademisi.Veri;

namespace VeliAkademisi.Pages.Yonetim.Sistem;

// Bir okulun ayrıntısı: /yonetim/sistem/okul/{id} (yalnızca sistem yöneticisi).
//  - Okulu pasife alma / yeniden açma: pasif okulun yöneticileri giriş yapamaz, velilerinin bağlantıları çalışmaz;
//    veriler silinmez.
//  - Okulun yöneticileri: son çare olarak (ör. iki yönetici de ayrıldıysa) yönetici ekleme, geçici şifre, kapatma.
//    Günlük işler okulun kendi "Yöneticiler" sayfasındadır.
// Veli bilgisi bu sayfada görünmez.
public class OkulModel : OkulYoneticileriModel
{
    public OkulModel(VeriTabani db) : base(db) { }

    [FromRoute(Name = "id")] public int OkulNo { get; set; }
    protected override int Okul => OkulNo;
    public Okul OkulBilgisi { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync() => await OkulVar() ? Page() : NotFound();

    public async Task<IActionResult> OnPostPasifAsync() => await Durum(false);
    public async Task<IActionResult> OnPostEtkinAsync() => await Durum(true);

    async Task<IActionResult> Durum(bool etkin)
    {
        var okul = await _db.Okullar.FindAsync(OkulNo);
        if (okul is null) return NotFound();
        if (!etkin && !Anladim) Hata = "Okul pasife alınmadı: önce onay kutusunu işaretleyin.";
        else
        {
            okul.Etkin = etkin;
            await _db.SaveChangesAsync();
            SonucBasligi = etkin ? "Okul yeniden açıldı" : "Okul pasife alındı";
        }
        await Yukle();
        return Page();
    }

    // Okul yoksa (adres elle değiştirildiyse) işlemler de 404 döner.
    async Task<bool> OkulVar()
    {
        if (await _db.Okullar.FindAsync(OkulNo) is null) return false;
        await Yukle();
        return true;
    }

    protected override async Task Yukle()
    {
        OkulBilgisi = (await _db.Okullar.FindAsync(OkulNo))!;
        await base.Yukle();
    }
}
