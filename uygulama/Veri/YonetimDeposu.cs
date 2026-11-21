using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;
using Yemekhane.Servisler;

namespace Yemekhane.Veri;

public class YonetimDeposu(OkumaDb okuma, YazmaDb yazma, VeriKaynagi kaynak)
{
    public Task<List<YonetimMenuSatiri>> RaporAsync(OturumBilgisi oturum, DateOnly baslangic, int? yemekhaneId)
    {
        var db = kaynak.Okuma(oturum);   
        var bitis = baslangic.AddDays(7);
        return (from d in db.MenuDoluluklari
                join m in db.Menuler on d.MenuId equals m.Id
                join p in db.MenuPuanlari on d.MenuId equals p.MenuId
                where d.Tarih >= baslangic && d.Tarih < bitis
                      && (yemekhaneId == null || d.YemekhaneId == yemekhaneId)
                orderby d.Tarih, d.YemekhaneId, d.OgunId
                select new YonetimMenuSatiri
                {
                    MenuId = d.MenuId,
                    Tarih = d.Tarih,
                    Yemekhane = d.Yemekhane,
                    Ogun = d.Ogun,
                    OgrenciFiyati = m.OgrenciFiyati,
                    PersonelFiyati = m.PersonelFiyati,
                    Kapasite = d.Kapasite,
                    Rezerve = d.Rezerve,
                    DolulukYuzde = d.DolulukYuzde,
                    OySayisi = p.OySayisi,
                    OrtalamaPuan = p.OrtalamaPuan,
                }).ToListAsync();
    }

    public Task<List<YemekSecenegi>> YemeklerAsync() =>
        (from y in okuma.Yemekler
         join k in okuma.YemekKategorileri on y.KategoriId equals k.Id
         orderby k.Sira, y.Ad
         select new YemekSecenegi(y.Id, y.Ad, k.Ad, y.Kalori, y.Vejetaryen)).ToListAsync();

    public Task<Menu?> MenuGetirAsync(int id) =>
        yazma.Menuler.Include(m => m.Yemekler).FirstOrDefaultAsync(m => m.Id == id);

    public Task<int> RezervasyonSayisiAsync(int menuId) =>
        yazma.Rezervasyonlar.CountAsync(r => r.MenuId == menuId && r.Durum != "iptal");

    public async Task EkleAsync(Menu menu)
    {
        yazma.Menuler.Add(menu);
        await yazma.SaveChangesAsync();
    }

    public Task KaydetAsync() => yazma.SaveChangesAsync();
}