using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;

namespace Yemekhane.Veri;

public abstract class YemekhaneDb : DbContext
{
    protected YemekhaneDb(DbContextOptions options) : base(options) { }

    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<YemekhaneKaydi> Yemekhaneler => Set<YemekhaneKaydi>();
    public DbSet<Ogun> Ogunler => Set<Ogun>();
    public DbSet<MenuDetay> MenuDetaylari => Set<MenuDetay>();
    public DbSet<MenuDoluluk> MenuDoluluklari => Set<MenuDoluluk>();
    public DbSet<Menu> Menuler => Set<Menu>();
    public DbSet<Rezervasyon> Rezervasyonlar => Set<Rezervasyon>();
    public DbSet<BakiyeHareketi> BakiyeHareketleri => Set<BakiyeHareketi>();
    public DbSet<Degerlendirme> Degerlendirmeler => Set<Degerlendirme>();
    public DbSet<MenuYemek> MenuYemekleri => Set<MenuYemek>();
    public DbSet<Yemek> Yemekler => Set<Yemek>();
    public DbSet<YemekKategorisi> YemekKategorileri => Set<YemekKategorisi>();
    public DbSet<MenuPuan> MenuPuanlari => Set<MenuPuan>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        foreach (var varlik in model.Model.GetEntityTypes())
            foreach (var alan in varlik.GetProperties())
                alan.SetColumnName(Regex.Replace(alan.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
    }
}

public class YazmaDb(DbContextOptions<YazmaDb> options) : YemekhaneDb(options);

public class OkumaDb(DbContextOptions<OkumaDb> options) : YemekhaneDb(options);