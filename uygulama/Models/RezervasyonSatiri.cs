namespace Yemekhane.Models;

public class RezervasyonSatiri
{
    public int Id { get; set; }
    public string Durum { get; set; } = "";          
    public decimal Ucret { get; set; }
    public DateOnly Tarih { get; set; }
    public string Ogun { get; set; } = "";
    public string Yemekhane { get; set; } = "";
    public DateTime RezervasyonSon { get; set; }
    public DateTime? KullanmaZamani { get; set; }
    public DateTime? IptalZamani { get; set; }
    public bool IptalEdilebilir { get; set; }        
}