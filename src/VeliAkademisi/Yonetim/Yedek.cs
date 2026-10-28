using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Data.Sqlite;

namespace VeliAkademisi.Yonetim;

// Sistem yöneticisinin haftalık yedeği: veritabanının o anki tam kopyası, şifreli zip içinde.
//  - Kopya SQLite'ın kendi yedekleme yöntemiyle alınır; site çalışırken bile tutarlı bir kopya çıkar
//    (düz dosya kopyası bunu garanti etmez; bkz. tanım 16.4).
//  - Zip, indiren kişinin o an yazdığı şifreyle AES-256 ile şifrelenir; Windows'ta 7-Zip ile açılır.
//    Şifre hiçbir yerde saklanmaz: unutulursa yedek açılamaz.
//  - Son indirme zamanı veritabanının yanındaki "son-yedek.txt" dosyasına yazılır (7 gün hatırlatması için).
public static class Yedek
{
    public const int SifreEnAz = 12;
    public static readonly TimeSpan HatirlatmaSuresi = TimeSpan.FromDays(7);

    public static byte[] SifreliZip(string dbYolu, string sifre, string icDosyaAdi)
    {
        var gecici = Path.Combine(Path.GetTempPath(), $"va-yedek-{Guid.NewGuid():N}.db");
        try
        {
            using (var kaynak = new SqliteConnection($"Data Source={dbYolu};Mode=ReadOnly"))
            using (var hedef = new SqliteConnection($"Data Source={gecici};Pooling=False"))
            {
                kaynak.Open();
                hedef.Open();
                kaynak.BackupDatabase(hedef);
            }
            using var cikti = new MemoryStream();
            using (var zip = new ZipOutputStream(cikti) { IsStreamOwner = false })
            {
                zip.SetLevel(6);
                zip.Password = sifre;
                var bilgi = new FileInfo(gecici);
                zip.PutNextEntry(new ZipEntry(icDosyaAdi) { DateTime = DateTime.Now, Size = bilgi.Length, AESKeySize = 256 });
                using (var dosya = File.OpenRead(gecici)) dosya.CopyTo(zip);
                zip.CloseEntry();
            }
            return cikti.ToArray();
        }
        finally
        {
            try { File.Delete(gecici); } catch { }   // şifresiz geçici kopya her durumda silinir
        }
    }

    static string KayitDosyasi(string dbYolu) => Path.Combine(Path.GetDirectoryName(dbYolu)!, "son-yedek.txt");

    public static void IndirildiYaz(string dbYolu) =>
        File.WriteAllText(KayitDosyasi(dbYolu), DateTime.UtcNow.ToString("O"));

    public static DateTime? SonIndirme(string dbYolu)
    {
        try
        {
            return DateTime.TryParse(File.ReadAllText(KayitDosyasi(dbYolu)), null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var z) ? z : null;
        }
        catch (IOException) { return null; }
    }
}
