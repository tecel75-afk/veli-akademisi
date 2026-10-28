using Microsoft.EntityFrameworkCore;

namespace VeliAkademisi.Veri;

// Veritabanının C# tarafındaki karşılığı. Her DbSet bir tabloya karşılık gelir.
public class VeriTabani : DbContext
{
    readonly OkulKapsami _kapsam;

    public VeriTabani(DbContextOptions<VeriTabani> secenekler, OkulKapsami kapsam) : base(secenekler) => _kapsam = kapsam;

    public DbSet<Okul> Okullar => Set<Okul>();
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<Veli> Veliler => Set<Veli>();
    public DbSet<Ilerleme> Ilerlemeler => Set<Ilerleme>();
    public DbSet<Cevap> Cevaplar => Set<Cevap>();

    // Okul süzgecinin kullandığı değerler; her sorguda o anki istekten okunur (bkz. OkulKapsami).
    bool TumOkullar => _kapsam.TumOkullar;
    int? KapsamOkulu => _kapsam.OkulId;

    // Tabloların kuralları: uzunluk sınırları, benzersizlik, silme davranışı.
    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Okul>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Ad).IsUnique();
        });

        m.Entity<Kullanici>(e =>
        {
            e.Property(x => x.AdSoyad).HasMaxLength(100).IsRequired();
            e.Property(x => x.KullaniciAdi).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.KullaniciAdi).IsUnique();   // iki kişi aynı kullanıcı adını alamaz
            e.Property(x => x.SifreOzeti).HasMaxLength(200).IsRequired();
            e.Property(x => x.GuvenlikDamgasi).HasMaxLength(64).IsRequired();
            // Okulu olan kullanıcı varken okul silinemez (okullar silinmez, pasife alınır).
            e.HasOne(x => x.Okul).WithMany(o => o.Kullanicilar)
             .HasForeignKey(x => x.OkulId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Veli>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(100).IsRequired();
            e.Property(x => x.Sinif).HasMaxLength(20).IsRequired();
            e.Property(x => x.Kod).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.Kod).IsUnique();   // iki velinin kodu aynı olamaz (bütün okullarda)
            e.HasOne(x => x.Okul).WithMany(o => o.Veliler)
             .HasForeignKey(x => x.OkulId).OnDelete(DeleteBehavior.Restrict);
            // OKUL SÜZGECİ: kapsam dışındaki okulların velileri hiçbir sorguda görünmez.
            e.HasQueryFilter(v => TumOkullar || v.OkulId == KapsamOkulu);
        });

        m.Entity<Ilerleme>(e =>
        {
            e.Property(x => x.Hikaye).HasMaxLength(8).IsRequired();
            e.HasIndex(x => new { x.VeliId, x.Hikaye }).IsUnique();   // veli + hikâye başına tek satır
            e.HasOne(x => x.Veli).WithMany(v => v.Ilerlemeler)
             .HasForeignKey(x => x.VeliId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(i => TumOkullar || i.Veli!.OkulId == KapsamOkulu);
        });

        m.Entity<Cevap>(e =>
        {
            e.Property(x => x.Hikaye).HasMaxLength(8).IsRequired();
            e.HasIndex(x => new { x.VeliId, x.Hikaye, x.Adim });
            e.HasOne(x => x.Veli).WithMany(v => v.Cevaplar)
             .HasForeignKey(x => x.VeliId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(c => TumOkullar || c.Veli!.OkulId == KapsamOkulu);
        });
    }
}
