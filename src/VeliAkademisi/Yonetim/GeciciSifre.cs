using System.Security.Cryptography;

namespace VeliAkademisi.Yonetim;

// Geçici şifre: yeni açılan ya da şifresini unutan hesaba verilir; ilk girişte değiştirilmesi zorunludur.
// Biçim: xxxx-xxxx-xxxx (12 harf/rakam + 2 tire). Karıştırılabilen işaretler (0/o, 1/l/i) kullanılmaz;
// telefonda okunup yazılması kolaydır. Şifreleme kalitesinde rastgele üretilir (~60 bit).
public static class GeciciSifre
{
    const string Isaretler = "abcdefghjkmnpqrstuvwxyz23456789";

    public static string Uret()
    {
        var s = new char[14];
        for (int k = 0; k < 14; k++)
            s[k] = k is 4 or 9 ? '-' : Isaretler[RandomNumberGenerator.GetInt32(Isaretler.Length)];
        return new string(s);
    }
}
