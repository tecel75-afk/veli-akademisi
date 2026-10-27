using Microsoft.Data.Sqlite;

namespace VeliAkademisi.Yonetim;

// "Yedeği indir": veritabanının o anki tam kopyası.
// SQLite'ın kendi yedekleme yöntemiyle alınır; site çalışırken bile tutarlı (bozulmamış) bir kopya çıkar
// (düz dosya kopyası bunu garanti etmez; bkz. tanım 16.4).
public static class Yedek
{
    public static byte[] Al(string dbYolu)
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
            return File.ReadAllBytes(gecici);
        }
        finally
        {
            try { File.Delete(gecici); } catch { }   // geçici kopya her durumda silinir
        }
    }
}
