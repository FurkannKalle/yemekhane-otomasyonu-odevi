using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Yemekhane.Models;

[Table("yemekhaneler")]
public class YemekhaneKaydi
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public int Kapasite { get; set; }
}

[Table("ogunler")]
public class Ogun
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public int Sira { get; set; }
    public decimal? YarimPorsiyonOrani { get; set; }   // Aşama 4: null ise bu öğünde yarım porsiyon yok
}

[Keyless, Table("v_menu_detay")]
public class MenuDetay
{
    public int MenuId { get; set; }
    public DateOnly Tarih { get; set; }
    public int YemekhaneId { get; set; }
    public string Yemekhane { get; set; } = "";
    public int OgunId { get; set; }
    public string Ogun { get; set; } = "";
    public DateTime RezervasyonSon { get; set; }
    public decimal? YarimPorsiyonOrani { get; set; }   // Aşama 4
    public decimal OgrenciFiyati { get; set; }
    public decimal PersonelFiyati { get; set; }
    public string Yemek { get; set; } = "";
    public string Kategori { get; set; } = "";
    public int KategoriSira { get; set; }
    public int Kalori { get; set; }
    public bool Vejetaryen { get; set; }
    public string? Alerjenler { get; set; }
    public string? AlerjenIdler { get; set; }
}

[Keyless, Table("v_menu_doluluk")]
public class MenuDoluluk
{
    public int MenuId { get; set; }
    public DateOnly Tarih { get; set; }
    public int YemekhaneId { get; set; }
    public string Yemekhane { get; set; } = "";
    public int OgunId { get; set; }
    public string Ogun { get; set; } = "";
    public int Kapasite { get; set; }
    public int Rezerve { get; set; }
    public int BosYer { get; set; }
    public decimal DolulukYuzde { get; set; }
    public DateTime RezervasyonSon { get; set; }
}

[Table("menuler")]
public class Menu
{
    public int Id { get; set; }
    public int YemekhaneId { get; set; }
    public DateOnly Tarih { get; set; }
    public int OgunId { get; set; }
    public decimal OgrenciFiyati { get; set; }
    public decimal PersonelFiyati { get; set; }
    public List<MenuYemek> Yemekler { get; set; } = [];
}

[Table("rezervasyonlar")]
public class Rezervasyon
{
    public int Id { get; set; }
    public int KullaniciId { get; set; }
    public int MenuId { get; set; }
    public string Durum { get; set; } = "";
    public string Porsiyon { get; set; } = "tam";      // Aşama 4: 'tam' ya da 'yarim'
    public decimal Ucret { get; set; }
    public DateTime Olusturma { get; set; }
    public DateTime? KullanmaZamani { get; set; }
    public DateTime? IptalZamani { get; set; }
}

public class RezervasyonSonucu
{
    [Column("rezervasyon_id")] public int RezervasyonId { get; set; }
    [Column("ucret")] public decimal Ucret { get; set; }
    [Column("bakiye")] public decimal Bakiye { get; set; }
}

public class IptalSonucu
{
    [Column("rezervasyon_id")] public int RezervasyonId { get; set; }
    [Column("iade")] public decimal Iade { get; set; }
    [Column("bakiye")] public decimal Bakiye { get; set; }
}

[Table("menu_yemekleri"), PrimaryKey(nameof(MenuId), nameof(YemekId))]
public class MenuYemek
{
    public int MenuId { get; set; }
    public int YemekId { get; set; }
}

[Table("yemekler")]
public class Yemek
{
    public int Id { get; set; }
    public int KategoriId { get; set; }
    public string Ad { get; set; } = "";
    public int Kalori { get; set; }
    public bool Vejetaryen { get; set; }
}

[Table("yemek_kategorileri")]
public class YemekKategorisi
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public int Sira { get; set; }
}

[Keyless, Table("v_menu_puan")]
public class MenuPuan
{
    public int MenuId { get; set; }
    public int OySayisi { get; set; }
    public decimal? OrtalamaPuan { get; set; }
}
