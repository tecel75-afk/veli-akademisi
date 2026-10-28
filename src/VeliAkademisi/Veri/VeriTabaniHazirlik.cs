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
        // Çok okullu yapıya geçişte (CokOkul) her veliye bir okul gerekir. Bu sürümden önce eklenmiş veliler varsa
        // hangi okula ait oldukları bilinemez; güncelleme uygulanmaz (veriler değişmez).
        if (db.Database.GetPendingMigrations().Any(g => g.EndsWith("_CokOkul")) && VeliVar(db))
            return "HATA: Veritabanında okulu belli olmayan veliler var; çok okullu yapıya geçiş uygulanmadı, veritabanına dokunulmadı.";
        db.Database.Migrate();
        // WAL günlük kipi: okuma (gece yedeği, Excel, takip tablosu) ile yazma (velilerin kayıtları) birbirini engellemez.
        // Varsayılan kipte, biri okurken gelen kayıtlar "database is locked" hatasıyla (500) kayboluyordu
        // (28 Ekim, eşzamanlı yazma denemesi: 1.000 istekten 997'si). Kip veritabanı dosyasına yazılır, kalıcıdır.
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
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

    static bool VeliVar(VeriTabani db)
    {
        var baglanti = db.Database.GetDbConnection();
        baglanti.Open();
        try
        {
            using var komut = baglanti.CreateCommand();
            if (!TabloVar(komut, "Veliler")) return false;
            komut.Parameters.Clear();
            komut.CommandText = "select count(*) from Veliler";
            return Convert.ToInt64(komut.ExecuteScalar()) > 0;
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
