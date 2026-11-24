# Aşama 2 · Uygulama, saklı yordamlar ve okuma/yazma ayrımı — notlar

**Seçilen yol: MVC.** Uygulama ASP.NET Core 8 MVC (C#) ile yazıldı. Veritabanı erişimi Entity Framework Core ile, MySQL sağlayıcısı olarak Pomelo kullanılarak yapılıyor. Sayfalar sunucuda Razor görünümleriyle üretiliyor. Statik dosyalar (CSS) Apache (`frontend`) üzerinden sunuluyor, diğer bütün istekler uygulamaya (`uygulama`, port 8000) aktarılıyor.

## 1. Katmanlı yapı

| Katman | Klasör | Görevi |
|---|---|---|
| Sunum | `Controllers/`, `Views/`, `ViewComponents/` | HTTP isteğini alır, oturumu denetler, sonucu sayfaya ya da yönlendirmeye çevirir. **SQL ya da LINQ yazmaz.** |
| İş kuralları | `Servisler/` | Fiyatın role göre seçilmesi, rezervasyon durumunun belirlenmesi, form doğrulama, oturum ve menü önbelleği. |
| Veri erişimi | `Veri/` | İki `DbContext` (`YazmaDb` → kaynak, `OkumaDb` → replika), depolar (`...Deposu`), saklı yordam çağrıları, `VeriKaynagi`. |
| Model | `Models/` | Tablo/görünüm varlıkları ve sayfa modelleri (ViewModel). |

- **Para işlemleri yalnızca saklı yordamlarla yapılır:** `sp_rezervasyon_yap`, `sp_rezervasyon_iptal`, `sp_bakiye_yukle`. Uygulama bakiyeyi hiçbir yerde kendisi hesaplayıp yazmaz. Kurallar (kapasite, son saat, bakiye, mükerrer rezervasyon) yordamın içinde, satır kilidiyle (`FOR UPDATE`) ve tek bir işlem (transaction) içinde denetlenir.
- **Güvenlik:**
  - Şifreler bcrypt özetiyle doğrulanır.
  - Bütün POST formlarında sahte istek koruması (antiforgery) vardır.
  - Oturum çerezi `HttpOnly`'dir.
  - Yönetici sayfası rolü sunucuda denetler.
  - SQL'e giden değerler parametre olarak gönderilir (`$"CALL ...({a}, {b})"` biçimindeki ara değerler EF Core tarafından parametreye çevrilir).

## 2. Bir isteğin yolu: rezervasyon yapma

Öğrenci haftalık menüde bir öğünün düğmesine bastığında tarayıcı `POST /Rezervasyon/Yap` isteği gönderir. İstek şu yoldan geçer:

**1. Apache (`frontend`).** `/css/` gibi statik yollar dışındaki her isteği uygulamaya aktarır.

**2. Oturum ara katmanı (`Program.cs`).** Çerezdeki token ile Redis'teki oturuma bakar. İstek POST olduğu için oturuma son yazma zamanını (`SonYazma`) işler.

```csharp
var oturum = await oturumlar.DogrulaAsync(token, HttpMethods.IsPost(context.Request.Method));
if (oturum is not null) context.Items["oturum"] = oturum;
```

**3. Rota.** `{controller}/{action=Index}/{id?}` kuralı isteği `RezervasyonController.Yap` eylemine bağlar. Controller `OturumluController`'dan türediği için oturum yoksa giriş sayfasına yönlendirilir. `[ValidateAntiForgeryToken]` form belirtecini denetler.

**4. Controller → veri katmanı.** Controller yalnızca depoyu çağırır. Yordamın hata mesajını hiç değiştirmeden kullanıcıya taşır.

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Yap(int menuId, int yemekhaneId, string baslangic, string porsiyon = "tam")
{
    try
    {
        var sonuc = await rezervasyonlar.YapAsync(Oturum.Id, menuId, porsiyon);
        var porsiyonAdi = porsiyon == "yarim" ? "yarım porsiyon" : "tam porsiyon";
        TempData["Basari"] = $"Rezervasyon yapıldı ({porsiyonAdi}). Ücret {sonuc.Ucret:C}, kalan bakiye {sonuc.Bakiye:C}.";
    }
    catch (MySqlException hata) when (hata.Number == 1644)
    {
        TempData["Hata"] = hata.Message;
    }
    return RedirectToAction("Haftalik", "Menu", new { yemekhaneId, baslangic });
}
```

(`porsiyon` parametresi Aşama 4'te eklendi.)

**5. Depo → kaynak veritabanı.** Yazma işlemleri `YazmaDb` ile, yani **kaynakta** (`mysql-primary`) çalışır:

```csharp
public async Task<RezervasyonSonucu> YapAsync(int kullaniciId, int menuId, string porsiyon)
{
    var sonuc = await yazma.Database
        .SqlQuery<RezervasyonSonucu>($"CALL sp_rezervasyon_yap({kullaniciId}, {menuId}, {porsiyon})")
        .ToListAsync();
    return sonuc[0];
}
```

**6. Yanıt (PRG).** Başarıda ya da hatada tarayıcı `GET /Menu/Haftalik` sayfasına yönlendirilir; böylece sayfayı yenilemek rezervasyonu tekrarlamaz. Sayfa üstündeki mesaj `TempData`'dan gelir. Üst bilgideki bakiye `BakiyeViewComponent` → `KullaniciDeposu.BakiyeAsync` yoluyla okunur. Bu okuma, rezervasyondan sonraki 5 saniye içinde olduğu için **kaynaktan** yapılır (4. bölüm).

## 3. Saklı yordam hata mesajları arayüzde

Yordamlar kural ihlalinde `SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '...'` çalıştırır. MySQL bunu **1644** numaralı hata olarak döndürür. Uygulama yalnızca bu hatayı yakalar ve mesajı değiştirmeden kullanıcıya gösterir. Kurallar tek yerde, yordamda durur; uygulamada aynı kontrol bir daha yazılmaz.

`sp_rezervasyon_yap`'ın kullanıcıya gösterilen mesajları:
- "Bakiye yetersiz."
- "Yemekhane kapasitesi dolu."
- "Bu menü için rezervasyon süresi doldu."
- "Bu menü için zaten rezervasyonunuz var."
- Aşama 4 ile eklenen: "Bu öğünde yarım porsiyon seçilemez."

Ekran 11'de bakiyesi 12,50 TL olan Ayşe Demir'in rezervasyon denemesi **"Bakiye yetersiz."** mesajıyla reddediliyor.

## 4. Okuma/yazma ayrımı

İki ayrı `DbContext` var. Hangi işin hangisini kullandığı veri katmanında belli:

| İş | Bağlam | Sunucu |
|---|---|---|
| Yordamlar (rezervasyon, iptal, bakiye yükleme), değerlendirme, menü kaydetme | `YazmaDb` | `mysql-primary` (kaynak) |
| Menüler, listeler, raporlar, giriş | `OkumaDb` | `mysql-replica` (replika) |
| Kullanıcının **kendi** verisi (bakiye, rezervasyonlar, hareketler, alerjenler) | `VeriKaynagi.Okuma(oturum)` | Son yazmadan sonraki 5 saniye **kaynak**, sonra replika |

```csharp
public class VeriKaynagi(OkumaDb okuma, YazmaDb yazma)
{
    public YemekhaneDb Okuma(OturumBilgisi oturum) =>
        oturum.SonYazma > DateTime.UtcNow.AddSeconds(-5) ? yazma : okuma;
}
```

### Kanıt: MySQL sorgu sayaçları

Her MySQL sunucusu kaç `SELECT` (`Com_select`) ve kaç yordam çağrısı (`Com_call_procedure`) çalıştırdığını sayar. Sayaçlar üç anda iki sunucuda okundu:
1. Başlangıçta.
2. Öğrenci (Mehmet Yıldız) yalnızca Rezervasyonlarım, Bakiye ve Haftalık menü sayfalarını gezdikten sonra.
3. Haftalık menüden bir rezervasyon yaptıktan hemen sonra.

```
PS> docker compose exec mysql-primary mysql -uroot -pyemekhane_kok -e "SHOW GLOBAL STATUS WHERE Variable_name IN ('Com_select','Com_call_procedure');"
PS> docker compose exec mysql-replica mysql -uroot -pyemekhane_kok -e "SHOW GLOBAL STATUS WHERE Variable_name IN ('Com_select','Com_call_procedure');"
```

| An | Kaynak `Com_select` | Kaynak `Com_call_procedure` | Replika `Com_select` | Replika `Com_call_procedure` |
|---|---|---|---|---|
| 1. Başlangıç | 7 | 0 | 16 | 0 |
| 2. Sayfalar gezildikten sonra | 8 | 0 | 25 | 0 |
| 3. Rezervasyondan hemen sonra | 16 | **1** | 27 | 0 |

`mysql` komut satırı istemcisi her bağlandığında kendisi bir `SELECT` çalıştırdığı için, her ölçüm o sunucunun `Com_select` sayacını 1 artırır. Bu pay düşüldüğünde:

- **1 → 2, yalnızca okuma:** Uygulama kaynakta **0**, replikada **8** okuma yaptı. Listeleme sayfaları replikadan okunuyor.
- **2 → 3, rezervasyon:**
  - Yordam çağrısı kaynakta 1 arttı, replikada hiç artmadı. Yazma yalnızca kaynağa gidiyor; replikaya değişiklik replikasyonla geliyor.
  - Rezervasyondan hemen sonraki sayfada kaynakta **7** okuma yapıldı. Bunlar öğrencinin bakiyesi ve rezervasyon durumu; 5 saniyelik pencerede kaynaktan okundu.
  - Replikadaki tek okuma, herkese ortak olan yemekhane listesi.

## 5. Neden rezervasyondan hemen sonra bakiye kaynaktan okunuyor? (Replikasyon gecikmesi)

MySQL replikasyonu **eşzamansızdır**. Kaynak bir işlemi onayladığında değişiklik önce kaynağın binlog'una yazılır; replika onu kendi IO iş parçacığıyla çekip SQL iş parçacığıyla uygular. Bu iki adım çok kısa da olsa zaman alır. Yük altında ya da ağ yavaşladığında bu süre saniyelere çıkabilir (replikasyon gecikmesi).

Rezervasyondan sonra yönlendirilen sayfa bakiyeyi replikadan okusaydı, replika işlemi henüz uygulamamışsa öğrenci **eski bakiyeyi** ve menüyü hâlâ "rezervasyon yapılabilir" olarak görürdü. Parasının düşmediğini sanıp tekrar denemesi ya da destek istemesi mümkün olurdu. Kendi yaptığı değişikliği göremeyen kullanıcı sistemi hatalı sanar.

Bu yüzden her POST isteğinde oturuma son yazma zamanı işleniyor. Kullanıcının **kendi** verisi bu andan sonraki 5 saniye boyunca kaynaktan okunuyor ("kendi yazdığını okuma", read-your-writes). Bu süre replikanın yakalaması için fazlasıyla yeterli. Başka kullanıcıların istekleri ve herkese ortak listeler bu sırada da replikadan okunmaya devam ediyor; böylece kaynağın yükü artmıyor. Bakiyenin kendisi ise zaten yalnızca kaynakta, yordam içinde satır kilidiyle değiştiği için gecikme hiçbir durumda yanlış bir para hareketine yol açmıyor; etkilediği tek şey ekranda gösterilen değer.

## 6. Ekranlar

Yedi ekran ve Aşama 4'te eklenen profil sayfası hem masaüstünde hem 400 px genişliğinde (telefon) denendi:
- Dar ekranda formlar tek sütuna iner.
- Tablolar (bakiye hareketleri, yönetici raporu) etiketli kartlara dönüşür.
- Düğmeler parmakla basılabilecek boyuttadır.
- Üstteki menü çubuğu dar ekranda yana kaydırılabilir; bütün bağlantılara telefondan ulaşılır.

Ekran görüntüleri raporun sonundaki "Ekranlar" bölümünde.
