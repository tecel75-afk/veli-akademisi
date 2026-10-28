using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VeliAkademisi.Veri;

// Yalnızca geliştirme aracı (dotnet ef) içindir: şema güncelleme dosyası üretirken veritabanı
// bağlamını siteyi çalıştırmadan oluşturur. Sitenin çalışmasında kullanılmaz.
public class TasarimFabrikasi : IDesignTimeDbContextFactory<VeriTabani>
{
    public VeriTabani CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<VeriTabani>().UseSqlite("Data Source=tasarim.db").Options);
}
