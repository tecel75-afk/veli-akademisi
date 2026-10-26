using Microsoft.EntityFrameworkCore;

namespace VeliAkademisi.Veri;

// Veritabanının C# tarafındaki karşılığı. Her DbSet bir tabloya karşılık gelir.
public class VeriTabani : DbContext
{
    public VeriTabani(DbContextOptions<VeriTabani> secenekler) : base(secenekler) { }

    public DbSet<Veli> Veliler => Set<Veli>();
    public DbSet<Ilerleme> Ilerlemeler => Set<Ilerleme>();
    public DbSet<Cevap> Cevaplar => Set<Cevap>();

    // Tabloların kuralları: uzunluk sınırları, benzersizlik, silme davranışı.
    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Veli>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(100).IsRequired();
            e.Property(x => x.Sinif).HasMaxLength(20).IsRequired();
            e.Property(x => x.Kod).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.Kod).IsUnique();   // iki velinin kodu aynı olamaz
        });

        m.Entity<Ilerleme>(e =>
        {
            e.Property(x => x.Hikaye).HasMaxLength(8).IsRequired();
            e.HasIndex(x => new { x.VeliId, x.Hikaye }).IsUnique();   // veli + hikâye başına tek satır
            e.HasOne(x => x.Veli).WithMany(v => v.Ilerlemeler)
             .HasForeignKey(x => x.VeliId).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Cevap>(e =>
        {
            e.Property(x => x.Hikaye).HasMaxLength(8).IsRequired();
            e.HasIndex(x => new { x.VeliId, x.Hikaye, x.Adim });
            e.HasOne(x => x.Veli).WithMany(v => v.Cevaplar)
             .HasForeignKey(x => x.VeliId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
