namespace VeliAkademisi.Veri;

// Bir isteğin hangi okulun velilerini görebileceği. Her istek için ayrı bir nesnedir.
// Varsayılan "hiçbiri"dir: kapsam açıkça verilmezse veliler, ilerlemeler ve cevaplar hiç görünmez.
//   - Veli sayfaları ve kayıt uçları (/v, /api): veli gizli koduyla bulunur → TumOkullar.
//   - Okul yöneticisi (/yonetim): OkulId = kendi okulu.
//   - Sistem yöneticisi ve ilçe izleyicisi: kapsam verilmez; isimsiz sayılar ayrı sorgularla alınır.
public class OkulKapsami
{
    public bool TumOkullar { get; set; }
    public int? OkulId { get; set; }
}
