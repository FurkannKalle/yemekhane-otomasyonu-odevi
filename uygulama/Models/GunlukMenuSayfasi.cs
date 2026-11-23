namespace Yemekhane.Models;

public class GunlukMenuSayfasi
{
    public List<YemekhaneKaydi> Yemekhaneler { get; set; } = [];
    public List<Ogun> Ogunler { get; set; } = [];
    public int YemekhaneId { get; set; }
    public DateOnly Tarih { get; set; }
    public int OgunId { get; set; }
    public GunlukMenu? Menu { get; set; }
}

public class GunlukMenu
{
    public string Yemekhane { get; set; } = "";
    public string Ogun { get; set; } = "";
    public DateOnly Tarih { get; set; }
    public decimal Fiyat { get; set; }
    public decimal? YarimFiyat { get; set; }          // Aşama 4: null ise bu öğünde yarım porsiyon yok
    public int ToplamKalori { get; set; }
    public int Kapasite { get; set; }
    public int BosYer { get; set; }
    public DateTime RezervasyonSon { get; set; }
    public bool RezervasyonAcik { get; set; }
    public List<MenuDetay> Yemekler { get; set; } = [];

    // Aşama 4: yemek adı → o yemekte bulunan, kullanıcının seçtiği alerjenler (yalnızca eşleşen yemekler)
    public Dictionary<string, List<string>> AlerjenUyarilari { get; set; } = new();
}

public record DolulukBilgisi(int Kapasite, int BosYer, bool RezervasyonAcik);
