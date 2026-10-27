using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using VeliAkademisi.Veri;

namespace VeliAkademisi.Yonetim;

// "Excel'e aktar": iki sayfalık .xlsx dosyası.
//   1) Takip    : her veli bir satır; 8 hikâyenin durumu (Başlanmadı / %ilerleme / Tamamlandı).
//   2) Cevaplar : cevaplanmış her soru bir satır; ilk ve son seçim (harf, metin, zaman),
//                 "İdeal şık seçildi mi?" (son seçime göre; bütün şıkları eşit sorularda boş).
public static class ExcelRaporu
{
    static readonly TimeSpan TurkiyeFarki = TimeSpan.FromHours(3);   // bkz. Takip.Zaman
    const string TarihBicimi = "dd.mm.yyyy hh:mm";

    public static async Task<byte[]> Olustur(VeriTabani db, HikayeVerisi hv)
    {
        var veliler = (await db.Veliler.AsNoTracking().ToListAsync())
            .OrderBy(v => v.Sinif, Takip.TurkceSira).ThenBy(v => v.Ad, Takip.TurkceSira).ToList();
        var ilerlemeler = (await db.Ilerlemeler.AsNoTracking().ToListAsync()).ToDictionary(x => (x.VeliId, x.Hikaye));
        var cevaplar = (await db.Cevaplar.AsNoTracking().ToListAsync()).ToLookup(x => x.VeliId);

        using var kitap = new XLWorkbook();

        // --- 1) Takip ---
        var takip = kitap.Worksheets.Add("Takip");
        var basliklar = new List<string> { "Veli", "Sınıf" };
        basliklar.AddRange(Hikayeler.Tanimlar.Select(t => $"{t.Kod.ToUpper(Takip.Turkce)} · {t.Ad}"));
        basliklar.Add("Son giriş");
        BaslikYaz(takip, basliklar);
        int r = 2;
        foreach (var v in veliler)
        {
            takip.Cell(r, 1).Value = v.Ad;
            takip.Cell(r, 2).Value = v.Sinif;
            for (int k = 0; k < Hikayeler.Sira.Count; k++)
            {
                var h = Hikayeler.Sira[k];
                takip.Cell(r, 3 + k).Value = Takip.Hucre(ilerlemeler.GetValueOrDefault((v.Id, h)), hv.Bilgi(h).Toplam);
            }
            TarihYaz(takip.Cell(r, 3 + Hikayeler.Sira.Count), v.SonGiris);
            r++;
        }
        Genislikler(takip, new[] { 24, 10 }.Concat(Enumerable.Repeat(16, Hikayeler.Sira.Count)).Append(17).ToArray());

        // --- 2) Cevaplar ---
        var sayfa = kitap.Worksheets.Add("Cevaplar");
        BaslikYaz(sayfa, new[] { "Veli", "Sınıf", "Hikâye", "Soru no", "Soru",
            "İlk seçim", "İlk seçim metni", "İlk seçim zamanı",
            "Son seçim", "Son seçim metni", "Son seçim zamanı", "İdeal şık seçildi mi?" });
        r = 2;
        foreach (var v in veliler)
        foreach (var t in Hikayeler.Tanimlar)
        foreach (var s in Takip.Cevaplar(hv.Bilgi(t.Kod), cevaplar[v.Id]).Where(x => x.Ilk is not null))
        {
            sayfa.Cell(r, 1).Value = v.Ad;
            sayfa.Cell(r, 2).Value = v.Sinif;
            sayfa.Cell(r, 3).Value = $"{t.Kod.ToUpper(Takip.Turkce)} · {t.Ad}";
            sayfa.Cell(r, 4).Value = s.Soru.No;
            sayfa.Cell(r, 5).Value = s.Soru.Metin;
            sayfa.Cell(r, 6).Value = Takip.Harf(s.Ilk!.Secim);
            sayfa.Cell(r, 7).Value = s.Soru.Siklar[s.Ilk.Secim];
            TarihYaz(sayfa.Cell(r, 8), s.Ilk.Zaman);
            sayfa.Cell(r, 9).Value = Takip.Harf(s.Son!.Secim);
            sayfa.Cell(r, 10).Value = s.Soru.Siklar[s.Son.Secim];
            TarihYaz(sayfa.Cell(r, 11), s.Son.Zaman);
            sayfa.Cell(r, 12).Value = s.IdealMi switch { true => "Evet", false => "Hayır", _ => "" };
            r++;
        }
        Genislikler(sayfa, new[] { 24, 10, 30, 8, 60, 9, 40, 17, 9, 40, 17, 12 });
        sayfa.Column(5).Style.Alignment.WrapText = true;
        sayfa.Column(7).Style.Alignment.WrapText = true;
        sayfa.Column(10).Style.Alignment.WrapText = true;

        using var akis = new MemoryStream();
        kitap.SaveAs(akis);
        return akis.ToArray();
    }

    static void BaslikYaz(IXLWorksheet s, IEnumerable<string> basliklar)
    {
        int c = 1;
        foreach (var b in basliklar) s.Cell(1, c++).Value = b;
        var satir = s.Row(1);
        satir.Style.Font.Bold = true;
        satir.Style.Alignment.WrapText = true;
        s.SheetView.FreezeRows(1);   // kaydırırken başlık satırı yerinde kalır
    }

    static void TarihYaz(IXLCell hucre, DateTime? utc)
    {
        if (utc is null) return;
        hucre.Value = utc.Value + TurkiyeFarki;
        hucre.Style.DateFormat.Format = TarihBicimi;
    }

    static void Genislikler(IXLWorksheet s, int[] genislik)
    {
        for (int k = 0; k < genislik.Length; k++) s.Column(k + 1).Width = genislik[k];
    }
}
