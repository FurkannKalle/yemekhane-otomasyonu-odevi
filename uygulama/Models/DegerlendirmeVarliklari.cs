using System.ComponentModel.DataAnnotations.Schema;

namespace Yemekhane.Models;

[Table("degerlendirmeler")]
public class Degerlendirme
{
    public int Id { get; set; }
    public int KullaniciId { get; set; }
    public int MenuId { get; set; }
    public int Puan { get; set; }          
    public string? Yorum { get; set; }

}

public class DegerlendirmeSatiri
{
    public int MenuId { get; set; }
    public DateOnly Tarih { get; set; }
    public string Ogun { get; set; } = "";
    public string Yemekhane { get; set; } = "";
    public int? Puan { get; set; }         
    public string? Yorum { get; set; }
}