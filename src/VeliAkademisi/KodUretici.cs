using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace VeliAkademisi;

// Velinin bağlantısındaki gizli kodu üretir ve biçimini denetler.
public static partial class KodUretici
{
    // 128 bit (16 bayt) şifreleme kalitesinde rastgele sayı → base64url → 22 karakter.
    // Tahmin edilemez; adreste sorun çıkaracak karakter içermez (yalnızca A-Z a-z 0-9 - _).
    public static string Uret()
    {
        var bayt = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bayt).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // Adresten gelen kodun biçimi doğru mu? (Veritabanına sormadan önceki ilk eleme.)
    public static bool BicimDogruMu(string kod) => KodBicimi().IsMatch(kod);

    [GeneratedRegex("^[A-Za-z0-9_-]{22}$")]
    private static partial Regex KodBicimi();
}
