using VeliAkademisi.Veri;
using VeliAkademisi.Yonetim;

namespace VeliAkademisi.Pages.Yonetim;

// Okulun yöneticileri: /yonetim/yoneticiler — okul yöneticisi, yalnızca kendi okulu (işlemler: OkulYoneticileri.cs).
public class YoneticilerModel : OkulYoneticileriModel
{
    public YoneticilerModel(VeriTabani db) : base(db) { }

    protected override int Okul => Oturum.OkulNo(User) ?? throw new InvalidOperationException("Okul yöneticisi oturumunda okul bilgisi yok.");

    public async Task OnGetAsync() => await Yukle();
}
