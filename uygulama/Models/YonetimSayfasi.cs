namespace Yemekhane.Models;

public class YonetimSayfasi
{
    public List<YemekhaneKaydi> Yemekhaneler { get; set; } = [];
    public List<Ogun> Ogunler { get; set; } = [];
    public List<YemekSecenegi> Yemekler { get; set; } = [];
    public MenuFormu Form { get; set; } = new();
    public DateOnly Baslangic { get; set; }
    public int? YemekhaneId { get; set; }              
    public List<YonetimMenuSatiri> Rapor { get; set; } = [];
}

public class MenuFormu
{
    public int? Id { get; set; }                       
    public int YemekhaneId { get; set; }
    public string Tarih { get; set; } = "";            
    public int OgunId { get; set; }
    public string OgrenciFiyati { get; set; } = "";
    public string PersonelFiyati { get; set; } = "";
    public List<int> YemekIdler { get; set; } = [];
}

public record YemekSecenegi(int Id, string Ad, string Kategori, int Kalori, bool Vejetaryen);

public class YonetimMenuSatiri
{
    public int MenuId { get; set; }
    public DateOnly Tarih { get; set; }
    public string Yemekhane { get; set; } = "";
    public string Ogun { get; set; } = "";
    public decimal OgrenciFiyati { get; set; }
    public decimal PersonelFiyati { get; set; }
    public int Kapasite { get; set; }
    public int Rezerve { get; set; }
    public decimal DolulukYuzde { get; set; }
    public int OySayisi { get; set; }
    public decimal? OrtalamaPuan { get; set; }
}