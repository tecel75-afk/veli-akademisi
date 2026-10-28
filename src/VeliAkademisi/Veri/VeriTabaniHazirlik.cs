using Microsoft.EntityFrameworkCore;

namespace VeliAkademisi.Veri;

// Site açılırken veritabanını hazırlar.
//  - Veritabanı yoksa: oluşturulur ve bütün şema güncellemeleri (Veri/Gecisler) uygulanır.
//  - Varsa: henüz uygulanmamış güncellemeler sırayla uygulanır; veriler korunur.
//  - Eski yöntemle (EnsureCreated, güncelleme kaydı olmadan) kurulmuşsa hiçbir şey değiştirilmez ve hata döner.
//    Böyle bir veritabanının üzerine tablo kurmaya çalışmak hata verir ya da verileri tehlikeye atar;
//    bu durumda veritabanı yedeklenip elle kaldırılmalıdır (bkz. deploy/KURULUM.md).
public static class VeriTabaniHazirlik
{
    // Hata yoksa null döner.
    public static string? Hazirla(VeriTabani db)
    {
        if (EskiYontemleKurulmus(db))
            return "HATA: Veritabanı eski yöntemle kurulmuş (şema güncelleme kaydı yok). Site açılmadı, veritabanına dokunulmadı.\n" +
                   "Veritabanı yedeklenip kaldırılmalı; site yeniden başlatılınca yeni yapıyla boş olarak kurulur.";
        db.Database.Migrate();
        return null;
    }

    // Veliler tablosu var ama güncelleme kaydı tablosu (__EFMigrationsHistory) yoksa: eski yöntem.
    static bool EskiYontemleKurulmus(VeriTabani db)
    {
        var baglanti = db.Database.GetDbConnection();
        baglanti.Open();
        try
        {
            using var komut = baglanti.CreateCommand();
            var veliler = TabloVar(komut, "Veliler");
            var kayit = TabloVar(komut, "__EFMigrationsHistory");
            return veliler && !kayit;
        }
        finally { baglanti.Close(); }
    }

    static bool TabloVar(System.Data.Common.DbCommand komut, string ad)
    {
        komut.CommandText = "select count(*) from sqlite_master where type = 'table' and name = $ad";
        komut.Parameters.Clear();
        var p = komut.CreateParameter();
        p.ParameterName = "$ad";
        p.Value = ad;
        komut.Parameters.Add(p);
        return Convert.ToInt64(komut.ExecuteScalar()) > 0;
    }
}
