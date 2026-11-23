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
    // Veritabanından gelenler
    public int MenuId { get; set; }
    public DateOnly Tarih { get; set; }
    public int OgunId { get; set; }
    public string Ogun { get; set; } = "";
    public DateTime RezervasyonSon { get; set; }
    public bool RezervasyonAcik { get; set; }
    public int BosYer { get; set; }
    public decimal OgrenciFiyati { get; set; }
    public decimal PersonelFiyati { get; set; }
    public decimal? YarimPorsiyonOrani { get; set; }   // Aşama 4
    public string? RezervasyonDurumu { get; set; }

    // Servis katmanında hesaplananlar
    public decimal Fiyat { get; set; }
    public decimal? YarimFiyat { get; set; }           // Aşama 4: null ise yarım porsiyon yok
    public string Durum { get; set; } = "";
}
