using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Yemekhane.Models;
using Yemekhane.Veri;

namespace Yemekhane.Servisler;

public class DegerlendirmeServisi(DegerlendirmeDeposu depo)
{
    public async Task<string?> EkleAsync(int kullaniciId, int menuId, int puan, string? yorum)
    {
        if (puan is < 1 or > 5) return "Puan 1 ile 5 arasında olmalıdır.";
        if (yorum?.Length > 500) return "Yorum en fazla 500 karakter olabilir.";

        var (kullanildi, oyVar) = await depo.DurumAsync(kullaniciId, menuId);
        if (!kullanildi) return "Yalnızca kullandığınız menüleri değerlendirebilirsiniz.";
        if (oyVar) return "Bu menüyü zaten değerlendirdiniz.";

        try
        {
            await depo.EkleAsync(new Degerlendirme
            {
                KullaniciId = kullaniciId,
                MenuId = menuId,
                Puan = puan,
                Yorum = string.IsNullOrWhiteSpace(yorum) ? null : yorum.Trim(),
            });
        }
        catch (DbUpdateException hata) when (hata.InnerException is MySqlException { Number: 1062 })
        {
            return "Bu menüyü zaten değerlendirdiniz.";
        }
        return null;
    }
}