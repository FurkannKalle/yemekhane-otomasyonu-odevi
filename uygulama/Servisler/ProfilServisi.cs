using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Yemekhane.Veri;

namespace Yemekhane.Servisler;

// Aşama 4: profil sayfasındaki alerjen seçiminin kaydı
public class ProfilServisi(AlerjenDeposu alerjenler)
{
    public async Task<string?> KaydetAsync(int kullaniciId, List<int> alerjenIdler)
    {
        try
        {
            await alerjenler.KaydetAsync(kullaniciId, alerjenIdler.Distinct().ToList());
            return null;
        }
        catch (DbUpdateException hata) when (hata.InnerException is MySqlException { Number: 1452 })
        {
            return "Seçilen alerjenlerden biri bulunamadı.";
        }
    }
}
