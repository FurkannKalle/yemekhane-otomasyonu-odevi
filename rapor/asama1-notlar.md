## 1\. Tüm servisler çalışıyor

Komut: docker compose ps -a
NAME                        IMAGE                COMMAND                  SERVICE         CREATED          STATUS                      PORTS
yemekhane-frontend-1        yemekhane-frontend   "httpd-foreground -D…"   frontend        42 minutes ago   Up 42 minutes (healthy)     0.0.0.0:8080->80/tcp, \[::]:8080->80/tcp
yemekhane-mysql-primary-1   mysql:8.4            "docker-entrypoint.s…"   mysql-primary   42 minutes ago   Up 42 minutes (healthy)     0.0.0.0:3307->3306/tcp, \[::]:3307->3306/tcp
yemekhane-mysql-replica-1   mysql:8.4            "docker-entrypoint.s…"   mysql-replica   42 minutes ago   Up 42 minutes (healthy)     0.0.0.0:3308->3306/tcp, \[::]:3308->3306/tcp
yemekhane-redis-1           redis:7.4-alpine     "docker-entrypoint.s…"   redis           42 minutes ago   Up 42 minutes (healthy)     0.0.0.0:6379->6379/tcp, \[::]:6379->6379/tcp
yemekhane-replica-setup-1   mysql:8.4            "bash /replica-setup…"   replica-setup   42 minutes ago   Exited (0) 42 minutes ago



\## 1b. Replikasyon durumu



Komut: docker compose exec -T mysql-replica mysql -uroot -pyemekhane\_kok -e "SHOW REPLICA STATUS\\G"



mysql: \[Warning] Using a password on the command line interface can be insecure.



&#x20;                 Source\_Host: mysql-primary

&#x20;          Replica\_IO\_Running: Yes

&#x20;         Replica\_SQL\_Running: Yes

&#x20;       Seconds\_Behind\_Source: 0

&#x20;               Last\_IO\_Error:

&#x20;              Last\_SQL\_Error:

&#x20;   Replica\_SQL\_Running\_State: Replica has read all relay log; waiting for more updates

&#x20;     Last\_IO\_Error\_Timestamp:

&#x20;    Last\_SQL\_Error\_Timestamp:

&#x20;               Auto\_Position: 1

Açıklama: Replikada iki iş parçacığı çalışır IO iş parçacığı  mysql-primarye bağlanıp binlogdaki yeni değişiklikleri çeker ve replikanın relay loguna kaydeder SQL iş parçacığı da relay logdaki bu değişiklikleri sırayla replikanın veritabanına uygular. Çıktıda Replica\_IO\_Running ve Replica\_SQL\_Running değerlerinin Yes Seconds\_Behind\_Source değerinin 0 ve hata alanlarının boş olması replikasyonun hatasız çalıştığını ve replikanın kaynaktan geride olmadığını gösterir.







\## 2. Kaynakta yapılan yükleme replikada görünüyor



Önce (replika):

docker compose exec -T mysql-replica mysql -uyemekhane\_app -pUyg\_2026 yemekhane -e "SELECT id, ad, bakiye FROM kullanicilar WHERE id = 7"

id      ad      bakiye

7       Mehmet  355.00



Yükleme (kaynak, stored procedure ile):

docker compose exec -T mysql-primary mysql -uyemekhane\_app -pUyg\_2026 yemekhane -e "CALL sp\_bakiye\_yukle(7, 100)"

hareket\_id      bakiye

455     455.00



Sonra (replika):

docker compose exec -T mysql-replica mysql -uyemekhane\_app -pUyg\_2026 yemekhane -e "SELECT id, ad, bakiye FROM kullanicilar WHERE id = 7"

id      ad      bakiye

7       Mehmet  455.00



Açıklama: Bakiye yüklemesi sp\_bakiye\_yukle ile yalnızca kaynakta (mysql-primary) yapıldı Replikada hiçbir yazma komutu çalıştırılmadığı halde bakiye 355 TLden 455 TLye çıktı değişiklik kaynağın binloguna yazıldı replika onu IO iş parçacığıyla çekip SQL iş parçacığıyla kendi veritabanına uyguladı.



\## 3. Replikaya yazma denemesi reddediliyor



Replikanın salt okunur ayarları:

docker compose exec -T mysql-replica mysql -uyemekhane\_app -pUyg\_2026 yemekhane -e "SELECT @@read\_only, @@super\_read\_only"

@@read\_only     @@super\_read\_only

1       1



Replikada bakiye yükleme denemesi:

docker compose exec -T mysql-replica mysql -uyemekhane\_app -pUyg\_2026 yemekhane -e "CALL sp\_bakiye\_yukle(7, 50)"

ERROR 1290 (HY000) at line 1: The MySQL server is running with the --super-read-only option so it cannot execute this statement



Açıklama: Replika read\_only ve super\_read\_only açık olarak çalışıyor Uygulama kullanıcısının (yemekhane\_app) replikada yazma yetkisi olduğu halde ERROR 1290 ile reddedildi yani engel yetkiden değil sunucunun salt okunur olmasından geliyor Böylece replikaya yanlışlıkla yazılıp kaynakla arasındaki tutarlılığın bozulması önleniyor replikadaki her değişiklik yalnızca replikasyon yoluyla kaynaktan gelir.



\## 4. Replika durdurulup başlatıldığında aradaki değişiklikleri yakalıyor



Replikayı durdur:

docker compose stop mysql-replica

&#x20;✔ Container yemekhane-mysql-replica-1 Stopped



Replika kapalıyken kaynakta 200 TL yükleme:

docker compose exec -T mysql-primary mysql -uyemekhane\_app -pUyg\_2026 yemekhane -e "CALL sp\_bakiye\_yukle(7, 200)"

hareket\_id      bakiye

456     655.00



Replikayı başlat:

docker compose start mysql-replica

&#x20;✔ Container yemekhane-mysql-replica-1 Started



docker compose ps -a

NAME                        IMAGE                COMMAND                  SERVICE         CREATED        STATUS                    PORTS

yemekhane-frontend-1        yemekhane-frontend   "httpd-foreground -D…"   frontend        17 hours ago   Up 30 minutes (healthy)   0.0.0.0:8080->80/tcp, \[::]:8080->80/tcp

yemekhane-mysql-primary-1   mysql:8.4            "docker-entrypoint.s…"   mysql-primary   17 hours ago   Up 30 minutes (healthy)   0.0.0.0:3307->3306/tcp, \[::]:3307->3306/tcp

yemekhane-mysql-replica-1   mysql:8.4            "docker-entrypoint.s…"   mysql-replica   17 hours ago   Up 8 seconds (healthy)    0.0.0.0:3308->3306/tcp, \[::]:3308->3306/tcp

yemekhane-redis-1           redis:7.4-alpine     "docker-entrypoint.s…"   redis           17 hours ago   Up 30 minutes (healthy)   0.0.0.0:6379->6379/tcp, \[::]:6379->6379/tcp

yemekhane-replica-setup-1   mysql:8.4            "bash /replica-setup…"   replica-setup   17 hours ago   Exited (0) 17 hours ago



Replikadaki bakiye:

docker compose exec -T mysql-replica mysql -uyemekhane\_app -pUyg\_2026 yemekhane -e "SELECT id, ad, bakiye FROM kullanicilar WHERE id = 7"

id      ad      bakiye

7       Mehmet  655.00



Replikasyon durumu:

docker compose exec -T mysql-replica mysql -uroot -pyemekhane\_kok -e "SHOW REPLICA STATUS\\G"

&#x20;          Replica\_IO\_Running: Yes

&#x20;         Replica\_SQL\_Running: Yes

&#x20;       Seconds\_Behind\_Source: 0

&#x20;   Replica\_SQL\_Running\_State: Replica has read all relay log; waiting for more updates



Açıklama: Replika kapalıyken kaynakta 200 TL yüklendi ve bakiye 655 TL oldu Replika yeniden başlatılınca bakiye replikada da 655 TL göründü yani replika kapalı olduğu sürede kaçırdığı işlemi kendiliğinden aldı Bunu GTID tabanlı otomatik konumlanma (Auto\_Position = 1) sağlıyor her işlemin benzersiz bir GTIDsi var replika hangi GTIDleri uyguladığını biliyor ve yeniden bağlandığında kaynaktan yalnızca eksik olanları istiyor.



\## 5. Redis çalışıyor



docker compose exec -T redis redis-cli ping

PONG

