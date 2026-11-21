using System.ComponentModel.DataAnnotations.Schema;

namespace Yemekhane.Models;

[Table("bakiye_hareketleri")]
public class BakiyeHareketi
{
    public int Id { get; set; }
    public int KullaniciId { get; set; }
    public string Tur { get; set; } = "";        
    public decimal Tutar { get; set; }           
    public int? RezervasyonId { get; set; }
    public string? Aciklama { get; set; }
    public DateTime Olusturma { get; set; }
}

public class YuklemeSonucu
{
    [Column("hareket_id")] public int HareketId { get; set; }
    [Column("bakiye")] public decimal Bakiye { get; set; }
}

public class BakiyeSayfasi
{
    public decimal Bakiye { get; set; }
    public List<BakiyeHareketi> Hareketler { get; set; } = [];
}