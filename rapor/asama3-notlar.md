# Aşama 3 · Redis önbellek ve oturum — notlar

Redis bu projede iki iş için kullanılıyor: **menü önbelleği** ve **oturum deposu**. Ayrıca ASP.NET Core'un form (antiforgery) ve TempData şifreleme anahtarları da Redis'te tutuluyor; böylece uygulama container'ında hiçbir durum kalmıyor.

| Görev | Anahtar | Süre | Ne zaman silinir |
|---|---|---|---|
| Menü önbelleği | `menu:{yemekhane_id}:{tarih}:{ogun_id}` | Menünün tarihindeki günün sonuna kadar | Yönetici o menüyü kaydettiğinde |
| Oturum | `oturum:{token}` | 30 dakika, her istekte yenilenir | Çıkış yapıldığında |
| Şifreleme anahtarları | `veri-koruma:anahtarlar` | Süresiz (ASP.NET Core kendisi yeniler) | — |

## 1. Menü önbelleği

### Tasarım

- Önbellek işi tek bir sınıfta: `Servisler/MenuOnbellegi.cs`. `MenuServisi` günün menüsünü doğrudan veritabanından değil, bu sınıftan ister.
- İstek yolu: `MenuController.Gunluk` → `MenuServisi.GunlukAsync` → `MenuOnbellegi.DetayAsync` → Redis `GET`.
  - **İsabet:** Kayıt Redis'te varsa JSON'dan çözülüp döner, veritabanına gidilmez.
  - **Kayıp:** Kayıt yoksa `MenuDeposu.DetayAsync` ile `v_menu_detay` görünümü **replikadan** okunur, JSON'a çevrilip süresiyle birlikte `SET` edilir.
  - Her iki durum da uygulama günlüğüne `İSABET` / `KAYIP` olarak yazılır.
- **Saklanan:** yemekler, kategori, kalori, vejetaryen bilgisi, alerjenler, öğrenci ve personel fiyatı, son rezervasyon saati. Bunlar gün içinde yalnızca yönetici değiştirirse değişir.
- **Saklanmayan:** boş yer. Her rezervasyonda ve iptalde değiştiği için her istekte `v_menu_doluluk` görünümünden ayrıca okunur. Önbelleğe konsaydı kullanıcı dolmuş bir menüyü boş görebilirdi.
- **Rolden bağımsız:** iki fiyat da kayıtta duruyor; kullanıcıya hangisinin gösterileceği önbellekten sonra rolüne göre seçiliyor. Öğrenci ve personel aynı kaydı kullanıyor.
- **Süre:** menünün tarihindeki günün sonu (gece 24:00). Geçmiş bir günün menüsüne bakılırsa süre bugünün sonuna kadar verilir (aksi halde süre eksi çıkardı). O gün menü yoksa hiçbir şey saklanmaz.

### Kanıt 1 — İlk istek veritabanından, ikincisi Redis'ten

Kanıtı temiz almak için bugünün (23 Kasım) kaydı önce elle silindi (`DEL`), sonra sayfa iki kez yenilendi.

```
PS> docker compose exec redis redis-cli DEL menu:1:2026-11-23:2
(integer) 1
PS> docker compose logs --tail 4 uygulama
uygulama-1  | info: Yemekhane.Servisler.MenuOnbellegi[0]
uygulama-1  |       Menü önbelleği KAYIP menu:1:2026-11-23:2 (veritabanından okundu)
uygulama-1  | info: Yemekhane.Servisler.MenuOnbellegi[0]
uygulama-1  |       Menü önbelleği İSABET menu:1:2026-11-23:2 (Redis'ten okundu)
```

İlk istekte kayıt yok (`KAYIP`): menü veritabanından okunup Redis'e yazılıyor. İkinci istekte aynı anahtar Redis'te bulunuyor (`İSABET`) ve veritabanına gidilmiyor.

### Kanıt 2 — Anahtarlar ve süreler

21 Kasım ve 22 Kasım öğle menüleri açıldıktan sonra, 21 Kasım saat 23:13 civarında:

```
PS> docker compose exec redis redis-cli --scan
"oturum:fdca9e48e6f2a96237d73ed7530b96a3eefe313854356f0887318a1648c2427d"
"menu:1:2026-11-22:2"
"menu:1:2026-11-21:2"
PS> docker compose exec redis redis-cli TTL menu:1:2026-11-21:2
(integer) 2816
PS> docker compose exec redis redis-cli TTL menu:1:2026-11-22:2
(integer) 89210
```

- Anahtarlar ödevde istenen `menu:{yemekhane_id}:{tarih}:{ogun_id}` biçiminde; aynı listede oturum anahtarı da görünüyor.
- 2816 saniye ≈ 47 dakika: 23:13'ten gece yarısına kalan süre. Yani 21 Kasım menüsü o gün bitince Redis'ten kendiliğinden silinir.
- 89210 saniye ≈ 2816 + 86400: 22 Kasım menüsü 22 Kasım gece yarısına kadar tutulur.

### Geçersizleştirme

- Yönetici panelinde bir menü kaydedildiğinde (`YonetimServisi.KaydetAsync`), kayıt başarılı olduktan sonra `MenuOnbellegi.YenileAsync` çağrılıyor:
  1. Menünün anahtarı Redis'ten silinir (`DEL`).
  2. Menü **kaynaktan (primary)** okunup Redis'e yeniden yazılır.
- **Neden replikadan değil kaynaktan:** kaydın hemen ardından replika değişikliği henüz almamış olabilir (replikasyon gecikmesi). O anda replikadan okunup önbelleğe yazılsaydı, eski menü gün sonuna kadar Redis'te kalır ve herkese gösterilirdi. Kaynak değişikliği kesin olarak içerdiği için bu risk yok.
- Menünün yemekhanesi, tarihi ya da öğünü değiştirildiyse (yalnızca rezervasyonu olmayan menülerde izin veriliyor) **eski anahtar da** yenileniyor; eski konumda artık menü olmadığı için kayıt silinmiş olarak kalıyor.

### Kanıt 3 — Yönetici menüyü değiştirince eski kayıt siliniyor

22 Kasım Merkez Yemekhane öğle menüsünde yönetici (Leyla Arslan) ana yemeği tavuktan **Fırında Levrek**'e değiştirdi:

```
PS> docker compose logs --tail 4 uygulama
uygulama-1  | info: Yemekhane.Servisler.MenuOnbellegi[0]
uygulama-1  |       Menü önbelleği SİLİNDİ menu:1:2026-11-22:2 (yönetici menüyü değiştirdi)
uygulama-1  | info: Yemekhane.Servisler.MenuOnbellegi[0]
uygulama-1  |       Menü önbelleği YENİLENDİ menu:1:2026-11-22:2 (kaynaktan okundu)
PS> docker compose exec redis redis-cli --raw GET menu:1:2026-11-22:2
[{"MenuId":56,"Tarih":"2026-11-22","YemekhaneId":1,"Yemekhane":"Merkez Yemekhane","OgunId":2,"Ogun":"Öğle","RezervasyonSon":"2026-11-22T10:00:00","OgrenciFiyati":40.00,"PersonelFiyati":90.00,"Yemek":"Mercimek Çorbası","Kategori":"Çorba","KategoriSira":1,"Kalori":180,"Vejetaryen":true,"Alerjenler":"Kereviz","AlerjenIdler":"9"},{"MenuId":56,"Tarih":"2026-11-22","YemekhaneId":1,"Yemekhane":"Merkez Yemekhane","OgunId":2,"Ogun":"Öğle","RezervasyonSon":"2026-11-22T10:00:00","OgrenciFiyati":40.00,"PersonelFiyati":90.00,"Yemek":"Fırında Levrek","Kategori":"Ana Yemek","KategoriSira":2,"Kalori":350,"Vejetaryen":false,"Alerjenler":"Balık","AlerjenIdler":"4"},{"MenuId":56,"Tarih":"2026-11-22","YemekhaneId":1,"Yemekhane":"Merkez Yemekhane","OgunId":2,"Ogun":"Öğle","RezervasyonSon":"2026-11-22T10:00:00","OgrenciFiyati":40.00,"PersonelFiyati":90.00,"Yemek":"Patates Püresi","Kategori":"Yardımcı Yemek","KategoriSira":3,"Kalori":220,"Vejetaryen":true,"Alerjenler":"Süt","AlerjenIdler":"7"},{"MenuId":56,"Tarih":"2026-11-22","YemekhaneId":1,"Yemekhane":"Merkez Yemekhane","OgunId":2,"Ogun":"Öğle","RezervasyonSon":"2026-11-22T10:00:00","OgrenciFiyati":40.00,"PersonelFiyati":90.00,"Yemek":"Fıstıklı Baklava","Kategori":"Tatlı ve Meyve","KategoriSira":5,"Kalori":430,"Vejetaryen":true,"Alerjenler":"Gluten, Sert kabuklu yemişler, Süt","AlerjenIdler":"1,7,8"}]
PS> docker compose exec redis redis-cli TTL menu:1:2026-11-22:2
(integer) 88762
```

- Eski kayıt silindi, yerine kaynaktan okunan yeni menü yazıldı: JSON'da **Fırında Levrek** var, tavuk yok.
- Süre yeniden kuruldu, yine 22 Kasım gece yarısına kadar.
- Öğrenci (Mehmet Yıldız) 22 Kasım menüsünü açtığında arayüzde yeni menü göründü.

## 2. Oturum

### Tasarım

- Giriş başarılı olunca `OturumServisi.AcAsync` 32 baytlık rastgele bir token üretir ve `oturum:{token}` anahtarına kullanıcının numarasını, adını ve rolünü JSON olarak 30 dakika süreyle yazar. Token tarayıcıya `HttpOnly`, `SameSite=Lax` bir çerezle verilir; JavaScript okuyamaz. Şifre ya da şifre özeti Redis'e yazılmaz.
- Her istekte `Program.cs`'teki ara katman çerezdeki token ile Redis'e bakar. Bunu `GETEX` komutuyla yapar: kaydı okurken süresini yeniden 30 dakikaya kurar. Böylece kullanıcı işlem yaptıkça oturum uzar; 30 dakika boşta kalırsa kendiliğinden düşer.
- POST isteklerinde oturuma son yazma zamanı da işlenir (`SonYazma`). Kullanıcı bir şey yazdıktan sonraki 5 saniye boyunca kendi verisi replikadan değil kaynaktan okunur (Aşama 2'deki "kendi yazdığını görme" çözümü).
- Çıkışta `OturumServisi.KapatAsync` anahtarı Redis'ten siler (`DEL`) ve çerezi kaldırır.

### Uygulama container'ının durumsuz olması

- Oturum bilgisi uygulamanın belleğinde değil Redis'te. Uygulama container'ı yeniden başlasa ya da sıfırdan kurulsa bile kullanıcı giriş yapmış olarak kalır.
- ASP.NET Core form güvenlik belirtecini (antiforgery) ve TempData mesajlarını gizli bir anahtarla şifreler (Data Protection). Bu anahtar varsayılan olarak container'ın içinde durur ve container yeniden kurulunca kaybolur; o zaman eski çerezler çözülemez, günlüğe hata düşer ve yeniden kurulumdan önce açılmış bir form gönderilirse 400 Bad Request alınır. Bu yüzden anahtarlar da Redis'e taşındı (`PersistKeysToStackExchangeRedis`, anahtar `veri-koruma:anahtarlar`).
- Redis `--appendonly yes` ile çalışıyor ve verisi `redis-veri` biriminde; Redis'in kendisi yeniden başlasa da oturumlar ve anahtarlar kaybolmuyor.

### Kanıt 4 — Oturum kaydı ve 30 dakikalık kayan süre

```
PS> docker compose exec redis redis-cli --raw GET oturum:fdca9e48e6f2a96237d73ed7530b96a3eefe313854356f0887318a1648c2427d
{"Id":7,"Ad":"Mehmet","Rol":"ogrenci","SonYazma":null}
PS> docker compose exec redis redis-cli TTL oturum:fdca9e48e6f2a96237d73ed7530b96a3eefe313854356f0887318a1648c2427d
(integer) 1684
# bir süre bekledikten sonra:
PS> docker compose exec redis redis-cli TTL oturum:fdca9e48e6f2a96237d73ed7530b96a3eefe313854356f0887318a1648c2427d
(integer) 1646
# Mehmet'in penceresinde sayfa yenilendikten sonra:
PS> docker compose exec redis redis-cli TTL oturum:fdca9e48e6f2a96237d73ed7530b96a3eefe313854356f0887318a1648c2427d
(integer) 1795
```

- Kayıtta kullanıcı numarası (7), ad ve rol duruyor; şifre yok. `SonYazma` boş, çünkü son 5 saniyede yazma işlemi yapılmadı.
- İlk değer 1684: son istekten bu yana yaklaşık 2 dakika geçmiş. Beklerken 1646'ya düştü. Sayfa yenilenince 1795'e döndü (komutu yazana kadar geçen birkaç saniye hariç 1800): süre her istekte yeniden 30 dakikaya kuruluyor.

### Kanıt 5 — Uygulama container'ı sıfırdan kurulunca oturum düşmüyor

```
PS> docker compose up -d --force-recreate uygulama
[+] up 5/5
 ✔ Container yemekhane-mysql-primary-1 Healthy      1.7s
 ✔ Container yemekhane-mysql-replica-1 Healthy      1.0s
 ✔ Container yemekhane-redis-1         Healthy      1.7s
 ✔ Container yemekhane-uygulama-1      Started      1.9s
 ✔ Container yemekhane-replica-setup-1 Exited       0.7s
PS> docker compose ps uygulama
NAME                   IMAGE                COMMAND                  SERVICE    CREATED          STATUS                    PORTS
yemekhane-uygulama-1   yemekhane-uygulama   "dotnet Yemekhane.dll"   uygulama   22 seconds ago   Up 20 seconds (healthy)   8000/tcp
# Mehmet'in penceresinde sayfa yenilendikten sonra:
PS> docker compose logs --tail 8 uygulama
uygulama-1  | info: Microsoft.Hosting.Lifetime[0]
uygulama-1  |       Application started. Press Ctrl+C to shut down.
uygulama-1  | info: Microsoft.Hosting.Lifetime[0]
uygulama-1  |       Hosting environment: Production
uygulama-1  | info: Microsoft.Hosting.Lifetime[0]
uygulama-1  |       Content root path: /app
uygulama-1  | info: Yemekhane.Servisler.MenuOnbellegi[0]
uygulama-1  |       Menü önbelleği İSABET menu:1:2026-11-21:2 (Redis'ten okundu)
```

- `--force-recreate` eski container'ı silip yenisini oluşturdu (`CREATED 22 seconds ago`); bu, yalnızca yeniden başlatmaktan daha zor bir sınama.
- Mehmet'in penceresinde sayfa yenilendiğinde giriş sayfasına yönlendirilmedi; üst bilgide adı ve bakiyesi görünmeye devam etti. Günlüğün son satırı da bunu gösteriyor: günün menüsü yalnızca oturumu olan kullanıcıya açılıyor (oturum yoksa istek menüye ulaşmadan giriş sayfasına yönlendirilir). Yani yeni container Mehmet'in oturumunu Redis'ten tanıdı.
- Menü kaydı da Redis'te durduğu için yeni container ilk istekte bile veritabanına gitmeden yanıt verdi (`İSABET`).
- Günlükte form belirteciyle ilgili hata yok.

### Kanıt 6 — Çıkışta oturum anahtarı siliniyor

```
# Mehmet ve Leyla giriş yapmışken:
PS> docker compose exec redis redis-cli --scan --pattern "oturum:*"
"oturum:0143bc77bea5ed65ede14eef1cb092884107e7f09b6e3d05697dcab3a2f11565"
"oturum:65d1fa4ae83415070eec382dc9e43b0dae34080bb40819d0b5320f3ee31f3414"
# Leyla çıkış yaptıktan sonra:
PS> docker compose exec redis redis-cli --scan --pattern "oturum:*"
"oturum:0143bc77bea5ed65ede14eef1cb092884107e7f09b6e3d05697dcab3a2f11565"
```

- İki kullanıcı giriş yapmışken iki oturum anahtarı vardı. Leyla çıkış yapınca onun anahtarı (`65d1fa4a…`) Redis'ten silindi; yalnızca Mehmet'inki kaldı.
- Mehmet'in buradaki anahtarı Kanıt 4'tekinden farklı, çünkü Mehmet arada yeniden giriş yaptı; her girişte yeni bir rastgele token üretilir.
