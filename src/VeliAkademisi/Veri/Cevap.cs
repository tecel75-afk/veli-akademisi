namespace VeliAkademisi.Veri;

// Bir sorudaki tek bir seçim. Veli şık değiştirirse ya da hikâyeyi tekrar izlerse
// her seçim ayrı bir satır olarak eklenir; böylece ilk ve son seçim ayrı görülebilir.
public class Cevap
{
    public int Id { get; set; }

    public int VeliId { get; set; }
    public Veli? Veli { get; set; }

    // Hikâye kodu: m1, m2a, ...
    public string Hikaye { get; set; } = "";

    // Sorunun hikâyedeki adım numarası (0'dan başlar, steps.json'daki sırası).
    public int Adim { get; set; }

    // Seçilen şık: 0 = A, 1 = B, 2 = C.
    public int Secim { get; set; }

    // Seçimin yapıldığı zaman (UTC).
    public DateTime Zaman { get; set; }
}
