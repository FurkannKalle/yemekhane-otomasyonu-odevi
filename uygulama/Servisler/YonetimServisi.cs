using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Yemekhane.Models;
using Yemekhane.Veri;

namespace Yemekhane.Servisler;

public class YonetimServisi(YonetimDeposu depo, MenuOnbellegi onbellek)
{
    public async Task<MenuFormu?> FormAsync(int id)
    {
        var m = await depo.MenuGetirAsync(id);
        return m is null ? null : new MenuFormu
        {
            Id = m.Id,
            YemekhaneId = m.YemekhaneId,
            Tarih = m.Tarih.ToString("yyyy-MM-dd"),
            OgunId = m.OgunId,
            OgrenciFiyati = m.OgrenciFiyati.ToString("0.00"),
            PersonelFiyati = m.PersonelFiyati.ToString("0.00"),
            YemekIdler = m.Yemekler.Select(y => y.YemekId).ToList(),
        };
    }

    public async Task<string?> KaydetAsync(MenuFormu f)
    {
        if (!DateOnly.TryParseExact(f.Tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarih))
            return "Geçerli bir tarih seçin.";
        var ogrenci = TutarCozucu.Coz(f.OgrenciFiyati);
        var personel = TutarCozucu.Coz(f.PersonelFiyati);
        if (ogrenci is null || personel is null || ogrenci > 10000 || personel > 10000)
            return "Fiyatları 0 ile 10.000 TL arasında bir sayı olarak girin.";
        if (f.YemekIdler.Count == 0)
            return "Menüye en az bir yemek ekleyin.";

        try
        {
            if (f.Id is null)
            {
                await depo.EkleAsync(new Menu
                {
                    YemekhaneId = f.YemekhaneId,
                    Tarih = tarih,
                    OgunId = f.OgunId,
                    OgrenciFiyati = ogrenci.Value,
                    PersonelFiyati = personel.Value,
                    Yemekler = f.YemekIdler.Distinct().Select(id => new MenuYemek { YemekId = id }).ToList(),
                });
            }
            else
            {
                var menu = await depo.MenuGetirAsync(f.Id.Value);
                if (menu is null) return "Menü bulunamadı.";

                var kimlikDegisti = menu.YemekhaneId != f.YemekhaneId || menu.Tarih != tarih || menu.OgunId != f.OgunId;
                if (kimlikDegisti && await depo.RezervasyonSayisiAsync(menu.Id) > 0)
                    return "Rezervasyonu olan menünün yemekhanesi, tarihi ya da öğünü değiştirilemez.";

                var eski = (menu.YemekhaneId, menu.Tarih, menu.OgunId);

                menu.YemekhaneId = f.YemekhaneId;
                menu.Tarih = tarih;
                menu.OgunId = f.OgunId;
                menu.OgrenciFiyati = ogrenci.Value;
                menu.PersonelFiyati = personel.Value;

                var secilen = f.YemekIdler.ToHashSet();
                menu.Yemekler.RemoveAll(y => !secilen.Contains(y.YemekId));
                foreach (var id in secilen.Except(menu.Yemekler.Select(y => y.YemekId)).ToList())
                    menu.Yemekler.Add(new MenuYemek { YemekId = id });

                await depo.KaydetAsync();

                if (kimlikDegisti)
                    await onbellek.YenileAsync(eski.YemekhaneId, eski.Tarih, eski.OgunId);
            }
        }
        catch (DbUpdateException hata) when (hata.InnerException is MySqlException { Number: 1062 })
        {
            return "Bu yemekhane, tarih ve öğün için zaten bir menü var.";
        }
        catch (DbUpdateException hata) when (hata.InnerException is MySqlException { Number: 1452 })
        {
            return "Seçilen yemekhane, öğün ya da yemeklerden biri bulunamadı.";
        }

        await onbellek.YenileAsync(f.YemekhaneId, tarih, f.OgunId);
        return null;
    }
}