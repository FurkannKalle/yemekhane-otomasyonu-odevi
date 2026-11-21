namespace Yemekhane.Models;

public class HaftalikSayfa
{
    public List<YemekhaneKaydi> Yemekhaneler { get; set; } = [];
    public int YemekhaneId { get; set; }
    public DateOnly Baslangic { get; set; }
    public List<HaftalikMenu> Menuler { get; set; } = [];
}

public class HaftalikMenu
{

    public int MenuId { get; set; }
    public DateOnly Tarih { get; set; }
    public int OgunId { get; set; }
    public string Ogun { get; set; } = "";
    public DateTime RezervasyonSon { get; set; }
    public bool RezervasyonAcik { get; set; }
    public int BosYer { get; set; }
    public decimal OgrenciFiyati { get; set; }
    public decimal PersonelFiyati { get; set; }
    public string? RezervasyonDurumu { get; set; }   

    
    public decimal Fiyat { get; set; }
    public string Durum { get; set; } = "";           
}