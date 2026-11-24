# Aşama 4 · Değişiklik isteği: yarım porsiyon ve alerjen uyarısı — notlar

## 1. İstek

- **Yarım porsiyon:** Öğle ve akşam menülerinde seçilebilir, ücreti tam fiyatın %60'ıdır. Kahvaltıda yarım porsiyon yoktur. Geçmiş rezervasyonlar tam porsiyon sayılır.
- **Alerjen uyarısı:** Kullanıcı profilinde alerjenlerini seçer. Menüde bu alerjenleri içeren yemekler uyarıyla gösterilir.
- **Uygulama biçimi:** Değişiklik tek bir migration dosyasıyla (`db/init/04_degisiklik.sql`), **çalışan sisteme**, mevcut veri bozulmadan uygulanır. Ardından uygulama güncellenir ve Redis'teki eski yapıdaki menü kayıtları temizlenir.

## 2. Migration: `db/init/04_degisiklik.sql`

| # | Değişiklik | Neden böyle |
|---|---|---|
| 1a | `ogunler.yarim_porsiyon_orani DECIMAL(3,2) NULL`. Öğle ve akşam `0.60`, kahvaltı `NULL`. `CHECK (0 < oran < 1)` | "Hangi öğünde, yüzde kaç" kuralı kodda değil veride durur. Oran değişirse tek bir `UPDATE` yeter. |
| 1b | `rezervasyonlar.porsiyon ENUM('tam','yarim') NOT NULL DEFAULT 'tam'`, `ALGORITHM=INSTANT` | Mevcut satırlar varsayılan değerle **tam** olur. `INSTANT` tabloyu kopyalamaz ve kilitlemez; değişiklik çalışan sistemde anında biter. |
| 2 | Yeni tablo `kullanici_alerjenleri (kullanici_id, alerjen_id)`, iki sütunlu birincil anahtar, iki yabancı anahtar | Kullanıcı ile alerjen arasında çoka çok ilişki. Kullanıcı silinirse seçimleri de silinir (`ON DELETE CASCADE`). |
| 3 | `v_menu_detay` görünümüne `yarim_porsiyon_orani` eklendi | Günün menüsü ekranı yarım porsiyon fiyatını da gösterir. Bu yüzden Redis'teki menü kaydının yapısı değişir (5. bölüm). |
| 4 | `sp_rezervasyon_yap(kullanici_id, menu_id, porsiyon)` | Geçersiz porsiyon reddedilir. Oranı tanımsız öğünde (kahvaltı) yarım porsiyon "Bu öğünde yarım porsiyon seçilemez." hatasıyla reddedilir. Ücret, rolüne göre fiyat × oran olarak hesaplanır ve 2 basamağa yuvarlanır. İptal edilmiş bir rezervasyon yeniden yapılırsa yeni porsiyon yazılır. Yarım porsiyonun bakiye hareketi "Rezervasyon (yarım porsiyon)" açıklamasıyla kaydedilir. İptal yordamı değişmedi: kayıttaki ücreti iade ettiği için yarım porsiyonda 24 TL'yi doğru iade eder. |

**Temiz kurulumda:** Dosya `db/init/` klasöründe olduğu için sıfırdan kurulumda `01_sema.sql` ve `02_ornek_veri.sql`'den sonra kendiliğinden çalışır.

## 3. Çalışan sisteme uygulama sırası

1. **Önceki durumun kaydı:** Rezervasyon sayısı, toplam bakiye ve Redis'teki eski yapıdaki menü kaydı.
2. **Migration kaynakta (`mysql-primary`) çalıştırılır.** Değişiklikler binlog üzerinden replikaya kendiliğinden geçer.
3. **Hemen ardından yeni uygulama kurulur.**
   - Yeni uygulama yordamı 3 parametreyle çağırır; migration'dan önce kurulsaydı rezervasyon çalışmazdı.
   - Eski uygulama da migration'dan sonra 2 parametreyle çağırdığı için hata verir.
   - Bu yüzden iki adım art arda yapılır.
4. **Redis'teki eski menü kayıtları silinir.** Silme işlemi yeni uygulama kurulduktan **sonra** yapılır; önce yapılsaydı arada çalışan eski uygulama Redis'i yeniden eski yapıda doldurabilirdi.

## 4. Kanıtlar: migration ve replika

**Önceki durum (kaynak):**

```
PS> docker compose exec mysql-primary mysql -uroot -pyemekhane_kok yemekhane -e "SELECT COUNT(*) AS rezervasyon_sayisi, (SELECT SUM(bakiye) FROM kullanicilar) AS toplam_bakiye FROM rezervasyonlar;"
+--------------------+---------------+
| rezervasyon_sayisi | toplam_bakiye |
+--------------------+---------------+
|                370 |      10360.00 |
+--------------------+---------------+
```

**Migration kaynakta:**

```
PS> docker compose exec mysql-primary sh -c "mysql --default-character-set=utf8mb4 -uroot -pyemekhane_kok yemekhane < /docker-entrypoint-initdb.d/04_degisiklik.sql"
mysql: [Warning] Using a password on the command line interface can be insecure.
```

Hata yok; yalnızca şifre uyarısı var. Hemen ardından `docker compose up -d --build uygulama` ile yeni uygulama kuruldu ve `healthy` durumuna geçti.

**Replikada `SHOW CREATE TABLE`:**

```
PS> docker compose exec mysql-replica mysql -uroot -pyemekhane_kok yemekhane -e "SHOW CREATE TABLE rezervasyonlar\G SHOW CREATE TABLE kullanici_alerjenleri\G"
*************************** 1. row ***************************
       Table: rezervasyonlar
Create Table: CREATE TABLE `rezervasyonlar` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `kullanici_id` int unsigned NOT NULL,
  `menu_id` int unsigned NOT NULL,
  `durum` enum('aktif','kullanildi','iptal') CHARACTER SET utf8mb4 COLLATE utf8mb4_tr_0900_ai_ci NOT NULL DEFAULT 'aktif',
  `porsiyon` enum('tam','yarim') COLLATE utf8mb4_tr_0900_ai_ci NOT NULL DEFAULT 'tam',
  `ucret` decimal(8,2) NOT NULL,
  `olusturma` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `kullanma_zamani` datetime DEFAULT NULL,
  `iptal_zamani` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_rezervasyon` (`kullanici_id`,`menu_id`),
  KEY `ix_rezervasyon_menu_durum` (`menu_id`,`durum`),
  CONSTRAINT `fk_rez_kullanici` FOREIGN KEY (`kullanici_id`) REFERENCES `kullanicilar` (`id`),
  CONSTRAINT `fk_rez_menu` FOREIGN KEY (`menu_id`) REFERENCES `menuler` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=371 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_tr_0900_ai_ci
*************************** 1. row ***************************
       Table: kullanici_alerjenleri
Create Table: CREATE TABLE `kullanici_alerjenleri` (
  `kullanici_id` int unsigned NOT NULL,
  `alerjen_id` tinyint unsigned NOT NULL,
  PRIMARY KEY (`kullanici_id`,`alerjen_id`),
  KEY `fk_ka_alerjen` (`alerjen_id`),
  CONSTRAINT `fk_ka_alerjen` FOREIGN KEY (`alerjen_id`) REFERENCES `alerjenler` (`id`),
  CONSTRAINT `fk_ka_kullanici` FOREIGN KEY (`kullanici_id`) REFERENCES `kullanicilar` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_tr_0900_ai_ci
```

**Replikada öğün oranları, eski rezervasyonlar, veri bütünlüğü ve yordamın yeni parametresi:**

```
PS> docker compose exec mysql-replica mysql -uroot -pyemekhane_kok yemekhane -e "SELECT id, ad, yarim_porsiyon_orani FROM ogunler ORDER BY sira; SELECT porsiyon, COUNT(*) AS adet FROM rezervasyonlar GROUP BY porsiyon; SELECT COUNT(*) AS rezervasyon_sayisi, (SELECT SUM(bakiye) FROM kullanicilar) AS toplam_bakiye FROM rezervasyonlar; SELECT PARAMETER_NAME, DATA_TYPE FROM information_schema.PARAMETERS WHERE SPECIFIC_SCHEMA = 'yemekhane' AND SPECIFIC_NAME = 'sp_rezervasyon_yap' ORDER BY ORDINAL_POSITION;"
+----+----------+----------------------+
| id | ad       | yarim_porsiyon_orani |
+----+----------+----------------------+
|  1 | Kahvalt? |                 NULL |
|  2 |  ?le     |                 0.60 |
|  3 | Ak?am    |                 0.60 |
+----+----------+----------------------+
+----------+------+
| porsiyon | adet |
+----------+------+
| tam      |  370 |
+----------+------+
+--------------------+---------------+
| rezervasyon_sayisi | toplam_bakiye |
+--------------------+---------------+
|                370 |      10360.00 |
+--------------------+---------------+
+----------------+-----------+
| PARAMETER_NAME | DATA_TYPE |
+----------------+-----------+
| p_kullanici_id | int       |
| p_menu_id      | int       |
| p_porsiyon     | varchar   |
+----------------+-----------+
```

- **Değişiklik replikaya geçti:** `porsiyon` sütunu, `kullanici_alerjenleri` tablosu ve yordamın üçüncü parametresi replikada var. Bunların hiçbiri replikada ayrıca çalıştırılmadı.
- **Geçmiş rezervasyonlar tam:** 370 kaydın hepsi `tam` porsiyon oldu.
- **Mevcut veri bozulmadı:** Rezervasyon sayısı ve toplam bakiye, migration'dan önce kaynakta ölçülen değerlerle (370 ve 10.360,00) birebir aynı.
- **`?` işaretleri:** Öğün adlarındaki `?` işaretleri, bu komutta `mysql` istemcisine Türkçe karakter ayarı (`--default-character-set=utf8mb4`) verilmediği için ekrana basılırken oluştu. Veri doğru; uygulama ekranlarında "Kahvaltı", "Öğle", "Akşam" düzgün görünüyor.

## 5. Kanıtlar: Redis'teki eski yapıdaki menü kayıtları

**Önce:** Eski uygulamanın Redis'e yazdığı menü kaydında `YarimPorsiyonOrani` alanı **yok**:

```
PS> docker compose exec redis redis-cli --raw GET menu:1:2026-11-24:2
[{"MenuId":68,"Tarih":"2026-11-24","YemekhaneId":1,"Yemekhane":"Merkez Yemekhane","OgunId":2,"Ogun":"Öğle","RezervasyonSon":"2026-11-24T10:00:00","OgrenciFiyati":40.00,"PersonelFiyati":90.00,"Yemek":"Şehriyeli Tavuk Çorbası", ...}, ...]
```

**Sorun:** Yeni uygulama bu kaydı Redis'ten okudu (`İSABET`). Alan eksik olduğu için günün menüsünde yarım porsiyon fiyatı görünmedi:

```
uygulama-1  | info: Yemekhane.Servisler.MenuOnbellegi[0]
uygulama-1  |       Menü önbelleği İSABET menu:1:2026-11-24:2 (Redis'ten okundu)
```

**Temizlik:** `menu:` ile başlayan bütün anahtarlar silindi; oturumlara dokunulmadı:

```
PS> docker compose exec redis sh -c "redis-cli --scan --pattern 'menu:*' | xargs -r redis-cli DEL"
(integer) 2
PS> docker compose exec redis redis-cli --scan --pattern "menu:*"
(çıktı yok: menu: anahtarı kalmadı)
```

**Sonra:** Menü veritabanından okunup yeni yapıda yazıldı. Kayıtta `"YarimPorsiyonOrani":0.60` var; ekranda "Yarım porsiyon ₺24,00" görünüyor:

```
uygulama-1  | info: Yemekhane.Servisler.MenuOnbellegi[0]
uygulama-1  |       Menü önbelleği KAYIP menu:1:2026-11-24:2 (veritabanından okundu)
PS> docker compose exec redis redis-cli --raw GET menu:1:2026-11-24:2
[{"MenuId":68,"Tarih":"2026-11-24","YemekhaneId":1,"Yemekhane":"Merkez Yemekhane","OgunId":2,"Ogun":"Öğle","RezervasyonSon":"2026-11-24T10:00:00","YarimPorsiyonOrani":0.60,"OgrenciFiyati":40.00,"PersonelFiyati":90.00,"Yemek":"Şehriyeli Tavuk Çorbası", ...}, ...]
```

(Uzun JSON çıktılarının yalnızca ilk yemeği gösterildi; `...` ile kısaltılan kısımlar diğer yemeklerdir.)

## 6. Kanıtlar: yeni özellikler çalışıyor

**Yarım porsiyon rezervasyonu.** Öğrenci (Mehmet Yıldız), haftalık menüde 24 Kasım öğle menüsünün (menü 68) "Yarım porsiyon · ₺24,00" düğmesine bastı. Kayıt ve bakiye hareketi replikada:

```
PS> docker compose exec mysql-replica mysql --default-character-set=utf8mb4 -uroot -pyemekhane_kok yemekhane -e "SELECT r.id, r.menu_id, r.porsiyon, r.ucret, h.tutar, h.aciklama FROM rezervasyonlar r JOIN bakiye_hareketleri h ON h.rezervasyon_id = r.id WHERE r.kullanici_id = 7 AND r.porsiyon = 'yarim';"
+-----+---------+----------+-------+--------+-------------------------------+
| id  | menu_id | porsiyon | ucret | tutar  | aciklama                      |
+-----+---------+----------+-------+--------+-------------------------------+
| 371 |      68 | yarim    | 24.00 | -24.00 | Rezervasyon (yarım porsiyon)  |
+-----+---------+----------+-------+--------+-------------------------------+
```

**Kahvaltıda yarım porsiyon yordam tarafından reddediliyor.** Arayüzde kahvaltı için yarım porsiyon düğmesi zaten yok. Kural ayrıca yordamda da duruyor; form elle değiştirilse bile geçemez:

```
PS> docker compose exec mysql-primary mysql --default-character-set=utf8mb4 -uroot -pyemekhane_kok yemekhane -e "SET @kahvalti = (SELECT id FROM menuler WHERE yemekhane_id = 1 AND tarih = '2026-11-25' AND ogun_id = 1); CALL sp_rezervasyon_yap(7, @kahvalti, 'yarim');"
ERROR 1644 (45000) at line 1: Bu öğünde yarım porsiyon seçilemez.
```

**Alerjen seçimi ve menüde uyarı.**
- Öğrenci profilinde Gluten ve Süt'ü seçti; seçim replikaya geçti.
- Günün menüsü Redis'ten geldiği halde (`İSABET`) uyarılar öğrenciye özel hesaplandı. Kişiye özel bilgi önbelleğe konmuyor; menü kaydı herkes için ortak kalıyor.

```
PS> docker compose logs --tail 2 uygulama
uygulama-1  | info: Yemekhane.Servisler.MenuOnbellegi[0]
uygulama-1  |       Menü önbelleği İSABET menu:1:2026-11-24:2 (Redis'ten okundu)
PS> docker compose exec mysql-replica mysql --default-character-set=utf8mb4 -uroot -pyemekhane_kok yemekhane -e "SELECT k.kullanici_id, a.ad AS alerjen FROM kullanici_alerjenleri k JOIN alerjenler a ON a.id = k.alerjen_id;"
+--------------+---------+
| kullanici_id | alerjen |
+--------------+---------+
|            7 | Gluten  |
|            7 | Süt     |
```

Bu özelliklerin arayüzdeki karşılıkları raporun sonundaki "Ekranlar" bölümünde:
- Ekran 4–7: porsiyon düğmeleri, başarı mesajı, Rezervasyonlarım'da "Yarım porsiyon".
- Ekran 10: profilde alerjen seçimi.
- Ekran 14: menüde alerjen uyarısı.

## 7. Uygulamadaki değişiklikler

| Katman | Dosya | Değişiklik |
|---|---|---|
| Model | `MenuVarliklari.cs` | `Ogun` ve `MenuDetay`'a `YarimPorsiyonOrani`, `Rezervasyon`'a `Porsiyon` eklendi |
| Model | `AlerjenVarliklari.cs` (yeni) | `Alerjen`, `KullaniciAlerjeni` (iki sütunlu anahtar), `ProfilSayfasi` |
| Model | `GunlukMenuSayfasi.cs`, `HaftalikSayfa.cs`, `RezervasyonSatiri.cs` | `YarimFiyat`, `AlerjenUyarilari`, `Porsiyon` |
| Veri | `RezervasyonDeposu.cs` | Yordama porsiyon 3. parametre olarak gidiyor; listede porsiyon görünüyor |
| Veri | `MenuDeposu.cs` | Haftalık sorguya `ogunler` katıldı; öğünün oranı geliyor |
| Veri | `AlerjenDeposu.cs` (yeni) | Alerjen listesi (replika); kullanıcının seçimi (kaydettikten sonra kaynaktan); kayıt tek işlemde: eskiler silinir, yeniler eklenir |
| Servis | `MenuServisi.cs` | Yarım fiyat hesabı (yordamla aynı yuvarlama); yemeklerin alerjenleri ile kullanıcının alerjenlerinin eşleştirilmesi |
| Servis | `ProfilServisi.cs` (yeni) | Kaydetme; geçersiz alerjen hatası (1452) Türkçe mesaja çevriliyor |
| Controller | `RezervasyonController.cs`, `ProfilController.cs` (yeni) | Porsiyon parametresi; profil sayfası (GET) ve kaydetme (POST) |
| Görünüm | `Haftalik.cshtml` | Öğle/akşamda "Tam porsiyon" ve "Yarım porsiyon" düğmeleri; kahvaltıda tek düğme |
| Görünüm | `Gunluk.cshtml` | Yarım porsiyon fiyatı; üstte uyarı bandı; ilgili yemekte "⚠ Alerjeniniz: …" |
| Görünüm | `Rezervasyon/Index.cshtml`, `Profil/Index.cshtml` (yeni), `_Layout.cshtml` | Porsiyon bilgisi; alerjen onay kutuları; menüye "Profil" bağlantısı |

## 8. Temiz kurulum testi

1. Sistem bütün verisiyle silindi: `docker compose down -v`.
2. Depo GitHub'dan boş bir klasöre klonlandı.
3. Yalnızca `docker compose up -d` çalıştırıldı.

Sonuçlar:
- Bütün servisler `healthy` oldu, `replica-setup` `Exited (0)` ile çıktı.
- Replikasyon `Replica_IO_Running: Yes`, `Replica_SQL_Running: Yes` durumunda çalıştı.
- `04_degisiklik.sql` kurulum sırasında kendiliğinden uygulandı: öğle/akşam oranı `0.60`, bütün rezervasyonlar `tam`, `kullanici_alerjenleri` tablosu var.
- Uygulamada yarım porsiyon düğmeleri ve profil sayfası çalıştı.

Rapordaki ekran görüntüleri bu temiz kurulumda alındı.
