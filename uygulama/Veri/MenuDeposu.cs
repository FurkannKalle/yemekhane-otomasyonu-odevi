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
}