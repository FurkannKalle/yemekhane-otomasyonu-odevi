using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;

namespace Yemekhane.Veri;

public class RezervasyonDeposu(YazmaDb yazma)
{
    public async Task<RezervasyonSonucu> YapAsync(int kullaniciId, int menuId)
    {
        var sonuc = await yazma.Database
            .SqlQuery<RezervasyonSonucu>($"CALL sp_rezervasyon_yap({kullaniciId}, {menuId})")
            .ToListAsync();
        return sonuc[0];   
    }
}