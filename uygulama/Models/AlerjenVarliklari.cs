using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Yemekhane.Models;

[Table("alerjenler")]
public class Alerjen
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
}

// Aşama 4: kullanıcının profilinde seçtiği alerjenler (çoka çok)
[Table("kullanici_alerjenleri"), PrimaryKey(nameof(KullaniciId), nameof(AlerjenId))]
public class KullaniciAlerjeni
{
    public int KullaniciId { get; set; }
    public int AlerjenId { get; set; }
}

public class ProfilSayfasi
{
    public List<Alerjen> Alerjenler { get; set; } = [];
    public HashSet<int> Secili { get; set; } = [];
}
