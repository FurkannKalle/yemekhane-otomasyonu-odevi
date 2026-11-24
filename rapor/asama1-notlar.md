# Aşama 1 · Docker Compose ve MySQL replikasyonu — notlar

## 1. Tüm servisler çalışıyor

```
PS> docker compose ps -a
NAME                        IMAGE                COMMAND                  SERVICE         CREATED          STATUS                      PORTS
yemekhane-frontend-1        yemekhane-frontend   "httpd-foreground -D…"   frontend        42 minutes ago   Up 42 minutes (healthy)     0.0.0.0:8080->80/tcp, [::]:8080->80/tcp
yemekhane-mysql-primary-1   mysql:8.4            "docker-entrypoint.s…"   mysql-primary   42 minutes ago   Up 42 minutes (healthy)     0.0.0.0:3307->3306/tcp, [::]:3307->3306/tcp
yemekhane-mysql-replica-1   mysql:8.4            "docker-entrypoint.s…"   mysql-replica   42 minutes ago   Up 42 minutes (healthy)     0.0.0.0:3308->3306/tcp, [::]:3308->3306/tcp
yemekhane-redis-1           redis:7.4-alpine     "docker-entrypoint.s…"   redis           42 minutes ago   Up 42 minutes (healthy)     0.0.0.0:6379->6379/tcp, [::]:6379->6379/tcp
yemekhane-replica-setup-1   mysql:8.4            "bash /replica-setup…"   replica-setup   42 minutes ago   Exited (0) 42 minutes ago
```

## 2. Replikasyon durumu

```
PS> docker compose exec -T mysql-replica mysql -uroot -pyemekhane_kok -e "SHOW REPLICA STATUS\G"
                  Source_Host: mysql-primary
           Replica_IO_Running: Yes
          Replica_SQL_Running: Yes
        Seconds_Behind_Source: 0
                Last_IO_Error:
               Last_SQL_Error:
    Replica_SQL_Running_State: Replica has read all relay log; waiting for more updates
      Last_IO_Error_Timestamp:
     Last_SQL_Error_Timestamp:
                Auto_Position: 1
```

Replikada iki iş parçacığı çalışır:
- **IO iş parçacığı:** `mysql-primary`'ye bağlanır, binlog'daki yeni değişiklikleri çeker ve replikanın relay log'una kaydeder.
- **SQL iş parçacığı:** Relay log'daki bu değişiklikleri sırayla replikanın veritabanına uygular.

Çıktıda `Replica_IO_Running` ve `Replica_SQL_Running` değerleri `Yes`, `Seconds_Behind_Source` değeri `0`, hata alanları da boş. Bu, replikasyonun hatasız çalıştığını ve replikanın kaynaktan geride olmadığını gösterir.

## 3. Kaynakta yapılan yükleme replikada görünüyor

```
# Önce (replika):
PS> docker compose exec -T mysql-replica mysql -uyemekhane_app -pUyg_2026 yemekhane -e "SELECT id, ad, bakiye FROM kullanicilar WHERE id = 7"
id      ad      bakiye
7       Mehmet  355.00

# Yükleme (kaynak, saklı yordamla):
PS> docker compose exec -T mysql-primary mysql -uyemekhane_app -pUyg_2026 yemekhane -e "CALL sp_bakiye_yukle(7, 100)"
hareket_id      bakiye
455     455.00

# Sonra (replika):
PS> docker compose exec -T mysql-replica mysql -uyemekhane_app -pUyg_2026 yemekhane -e "SELECT id, ad, bakiye FROM kullanicilar WHERE id = 7"
id      ad      bakiye
7       Mehmet  455.00
```

Bakiye yüklemesi `sp_bakiye_yukle` ile yalnızca kaynakta (`mysql-primary`) yapıldı. Replikada hiçbir yazma komutu çalıştırılmadığı halde bakiye 355 TL'den 455 TL'ye çıktı. Değişiklik kaynağın binlog'una yazıldı; replika onu IO iş parçacığıyla çekip SQL iş parçacığıyla kendi veritabanına uyguladı.

## 4. Replikaya yazma denemesi reddediliyor

```
PS> docker compose exec -T mysql-replica mysql -uyemekhane_app -pUyg_2026 yemekhane -e "SELECT @@read_only, @@super_read_only"
@@read_only     @@super_read_only
1       1
PS> docker compose exec -T mysql-replica mysql -uyemekhane_app -pUyg_2026 yemekhane -e "CALL sp_bakiye_yukle(7, 50)"
ERROR 1290 (HY000) at line 1: The MySQL server is running with the --super-read-only option so it cannot execute this statement
```

Replika `read_only` ve `super_read_only` açık olarak çalışıyor.
- Uygulama kullanıcısının (`yemekhane_app`) replikada yazma yetkisi olduğu halde istek `ERROR 1290` ile reddedildi. Yani engel yetkiden değil, sunucunun salt okunur olmasından geliyor.
- Böylece replikaya yanlışlıkla yazılması ve kaynakla arasındaki tutarlılığın bozulması önleniyor. Replikadaki her değişiklik yalnızca replikasyon yoluyla kaynaktan gelir.

## 5. Replika durdurulup başlatıldığında aradaki değişiklikleri yakalıyor

```
PS> docker compose stop mysql-replica
 ✔ Container yemekhane-mysql-replica-1 Stopped

# Replika kapalıyken kaynakta 200 TL yükleme:
PS> docker compose exec -T mysql-primary mysql -uyemekhane_app -pUyg_2026 yemekhane -e "CALL sp_bakiye_yukle(7, 200)"
hareket_id      bakiye
456     655.00

PS> docker compose start mysql-replica
 ✔ Container yemekhane-mysql-replica-1 Started

# Replikadaki bakiye:
PS> docker compose exec -T mysql-replica mysql -uyemekhane_app -pUyg_2026 yemekhane -e "SELECT id, ad, bakiye FROM kullanicilar WHERE id = 7"
id      ad      bakiye
7       Mehmet  655.00

PS> docker compose exec -T mysql-replica mysql -uroot -pyemekhane_kok -e "SHOW REPLICA STATUS\G"
           Replica_IO_Running: Yes
          Replica_SQL_Running: Yes
        Seconds_Behind_Source: 0
    Replica_SQL_Running_State: Replica has read all relay log; waiting for more updates
```

Replika kapalıyken kaynakta 200 TL yüklendi ve bakiye 655 TL oldu. Replika yeniden başlatılınca bakiye replikada da 655 TL göründü; replika kapalı olduğu sürede kaçırdığı işlemi kendiliğinden aldı.

Bunu GTID tabanlı otomatik konumlanma (`Auto_Position = 1`) sağlıyor:
- Her işlemin benzersiz bir GTID'si var.
- Replika hangi GTID'leri uyguladığını biliyor.
- Yeniden bağlandığında kaynaktan yalnızca eksik olanları istiyor.

## 6. Redis çalışıyor

```
PS> docker compose exec -T redis redis-cli ping
PONG
```
