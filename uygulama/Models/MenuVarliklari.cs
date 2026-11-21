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
    public int OgunId { get; set; }
    public string Ogun { get; set; } = "";
    public int Kapasite { get; set; }
    public int Rezerve { get; set; }
    public int BosYer { get; set; }
    public decimal DolulukYuzde { get; set; }
    public DateTime RezervasyonSon { get; set; }
}