using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;

namespace Yemekhane.Veri;

public class MenuDeposu(OkumaDb okuma)
{
        public Task<List<YemekhaneKaydi>> YemekhanelerAsync() =>
        okuma.Yemekhaneler.AsNoTracking().OrderBy(y => y.Id).ToListAsync();

    public Task<List<Ogun>> OgunlerAsync() =>
        okuma.Ogunler.AsNoTracking().OrderBy(o => o.Sira).ToListAsync();

    public Task<List<MenuDetay>> DetayAsync(int yemekhaneId, DateOnly tarih, int ogunId) =>
        okuma.MenuDetaylari
            .Where(d => d.YemekhaneId == yemekhaneId && d.Tarih == tarih && d.OgunId == ogunId)
            .OrderBy(d => d.KategoriSira).ThenBy(d => d.Yemek)
            .ToListAsync();

    public Task<DolulukBilgisi?> DolulukAsync(int menuId) =>
        okuma.MenuDoluluklari
            .Where(d => d.MenuId == menuId)
            .Select(d => new DolulukBilgisi(d.Kapasite, d.BosYer, DateTime.Now < d.RezervasyonSon))
            .FirstOrDefaultAsync();
    public Task<List<HaftalikMenu>> HaftalikAsync(int kullaniciId, int yemekhaneId, DateOnly baslangic)
    {
        var bitis = baslangic.AddDays(7);
        return (from d in okuma.MenuDoluluklari
                join m in okuma.Menuler on d.MenuId equals m.Id
                where d.YemekhaneId == yemekhaneId && d.Tarih >= baslangic && d.Tarih < bitis
                orderby d.Tarih, d.OgunId
                select new HaftalikMenu
                {
                    MenuId = d.MenuId,
                    Tarih = d.Tarih,
                    OgunId = d.OgunId,
                    Ogun = d.Ogun,
                    RezervasyonSon = d.RezervasyonSon,
                    RezervasyonAcik = DateTime.Now < d.RezervasyonSon,
                    BosYer = d.BosYer,
                    OgrenciFiyati = m.OgrenciFiyati,
                    PersonelFiyati = m.PersonelFiyati,
                    RezervasyonDurumu = okuma.Rezervasyonlar
                        .Where(r => r.MenuId == d.MenuId && r.KullaniciId == kullaniciId)
                        .Select(r => r.Durum)
                        .FirstOrDefault(),
                }).ToListAsync();
    }
}