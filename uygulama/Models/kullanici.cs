using System.ComponentModel.DataAnnotations.Schema;

namespace Yemekhane.Models;

[Table("kullanicilar")]
public class Kullanici
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public string Soyad { get; set; } = "";
    public string Eposta { get; set; } = "";
    public string SifreHash { get; set; } = "";
    public string Rol { get; set; } = "";       
    public string? OgrenciNo { get; set; }
    public decimal Bakiye { get; set; }
    public bool Aktif { get; set; }
}