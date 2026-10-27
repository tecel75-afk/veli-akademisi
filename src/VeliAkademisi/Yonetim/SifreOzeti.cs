using System.Security.Cryptography;

namespace VeliAkademisi.Yonetim;

// Yönetici şifresinin tek yönlü özeti (PBKDF2-SHA256).
// Özetten şifre geri elde edilemez; giriş sırasında yazılan şifrenin özeti çıkarılıp karşılaştırılır.
// Biçim: pbkdf2-sha256$<tekrar sayısı>$<tuz (base64)>$<özet (base64)>
public static class SifreOzeti
{
    const int Tekrar = 600_000;   // her denemeyi yavaşlatır; tahminle şifre bulmayı zorlaştırır
    const string Onek = "pbkdf2-sha256";

    public static string Olustur(string sifre)
    {
        var tuz = RandomNumberGenerator.GetBytes(16);   // her özete özgü rastgele değer
        var ozet = Rfc2898DeriveBytes.Pbkdf2(sifre, tuz, Tekrar, HashAlgorithmName.SHA256, 32);
        return $"{Onek}${Tekrar}${Convert.ToBase64String(tuz)}${Convert.ToBase64String(ozet)}";
    }

    public static bool Dogrula(string sifre, string? kayitliOzet)
    {
        if (string.IsNullOrWhiteSpace(kayitliOzet)) return false;
        var parca = kayitliOzet.Trim().Split('$');
        if (parca.Length != 4 || parca[0] != Onek || !int.TryParse(parca[1], out var tekrar)) return false;
        try
        {
            var tuz = Convert.FromBase64String(parca[2]);
            var beklenen = Convert.FromBase64String(parca[3]);
            var gelen = Rfc2898DeriveBytes.Pbkdf2(sifre, tuz, tekrar, HashAlgorithmName.SHA256, beklenen.Length);
            // Sabit süreli karşılaştırma: kaç baytın tuttuğu süreden anlaşılamaz.
            return CryptographicOperations.FixedTimeEquals(gelen, beklenen);
        }
        catch (FormatException) { return false; }
    }
}
