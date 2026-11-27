# İlerleme

> Yalnız **canlı durum** (D-097): faz tablosu, süren fazın adımları ve doğrulaması, gece koşusu, cevaplanmamış sorular, en son devir notu. Bu dosya 25 KB'ı aşmaz; aşıyorsa oturum başında önce arşive taşınır.
> Kapanan her kayıt kapandığı anda [PROGRESS-ARSIV.md](PROGRESS-ARSIV.md)'e gider: biten fazın adımları, bir önceki devir notu, cevaplanan soru. Arşiv oturum başında okunmaz; bir kaydın geçmişi gerekirse orada `grep` ile aranır.
> Kapsam dışı bırakılan konular [SONRAYA-BIRAKILANLAR.md](SONRAYA-BIRAKILANLAR.md)'dedir. Adım tanımları [PLAN.md](PLAN.md)'dedir.

## Fazlar

| Faz | Durum | Branch | Başlangıç | Bitiş | Merge |
|---|---|---|---|---|---|
| 0 | ✅ tamam | `faz/0-stabilizasyon` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 1 | ✅ tamam | `faz/1-kutuphane-altyapisi` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 2a | ✅ tamam | `faz/2a-paket-yapisi` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 2b | ✅ tamam | `faz/2b-konfigurasyon` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 2c | ✅ tamam | `faz/2c-yerlesik-karsiliklar` | 2026-09-26 | 2026-09-26 | 2026-09-27 (kullanıcı onayı, `--no-ff`; push 2026-09-27) |
| 3a | ✅ tamam | `faz/3a-kimlik-dogrulama` | 2026-09-27 | 2026-09-27 | 2026-09-27 (kullanıcı onayı, `--no-ff`; push 2026-09-27) |
| 3b | ✅ tamam | `faz/3b-idp` | 2026-09-27 | 2026-09-27 | 2026-09-27 (kullanıcı onayı, `--no-ff`; push 2026-09-28, kullanıcı isteği) |
| 4a | ✅ tamam | `faz/4a-outbox` | 2026-09-28 | 2026-09-28 | 2026-09-28 (kullanıcı onayı, `--no-ff`; push 2026-09-28, kullanıcı isteği) |
| 4b | ✅ tamam | `faz/4b-referans-moduller` | 2026-09-28 | 2026-09-28 | 2026-09-28 (kullanıcı onayı, `--no-ff`; push edilmedi) |
| 4c | ✅ tamam | `faz/4c-tuketici-hazirligi` | 2026-10-20 | 2026-10-20 | 2026-10-20 (kullanıcı onayı, `--no-ff`; push edilmedi) |
| 4d | ✅ tamam | `faz/4d-kimlik-uretim` | 2026-10-20 | 2026-10-20 | 2026-10-20 (kullanıcı onayı, `--no-ff`; push 2026-10-20, kullanıcı isteği) |
| 5 | ✅ tamam | `faz/5-sablon-yayin` | 2026-10-20 | 2026-10-20 | 2026-10-20 (kullanıcı onayı, `--no-ff`; push 2026-10-20; push'taki CI yeşil (`build-and-test`, `template-smoke`); `v0.1.0-preview.1` tag'i kullanıcı onayıyla, release.yml yeşil, 8 paket GitHub Packages'ta) |
| 6a | ✅ tamam | `faz/6a-ci-sertlestirme` | 2026-10-21 | 2026-10-21 | 2026-10-21 (kullanıcı onayı, `--no-ff`; push 2026-10-21, kullanıcı isteği) |
| 6b | ✅ tamam | `faz/6b-kimlik-guvenlik-siniri` | 2026-10-21 | 2026-10-22 | 2026-10-22 (kullanıcı onayı, `--no-ff`, `4909fe4`; push 2026-10-22, kullanıcı isteği) |
| 6c | ✅ tamam | `faz/6c-test-altyapisi` | 2026-10-22 | 2026-10-23 | 2026-10-23 (kullanıcı onayı, `--no-ff`, `64b3ab6`; push 2026-10-23, kullanıcı isteği) |
| 6d | ✅ tamam | `faz/6d-analizor-sikilastirma` | 2026-10-23 | 2026-10-23 | 2026-10-23 (kullanıcı onayı, `--no-ff`, `231b05f`; push 2026-10-23, kullanıcı isteği) |
| 7a | ✅ tamam | `faz/7a-veritabani-ozgurlugu` | 2026-10-23 | 2026-10-24 | 2026-10-24 (kullanıcı onayı, PR #11 merge commit'i `cf895d8`) |
| 7b | ✅ tamam | `faz/7b-cok-kiracililik` | 2026-10-24 | 2026-10-25 | 2026-10-27 (kullanıcı onayı, PR #13 merge commit'i `dba8318`) |
| 8 | ✅ tamam | `faz/8-bassiz-giris` | 2026-11-09 | 2026-11-14 | 2026-11-27 (kullanıcı onayı, PR #35 merge commit'i `611903f`; CodeRabbit'in 12 yorumu sonradan bulgu olarak okunur) |
| 9 | ✅ tamam | `faz/9-bakim` (`faz/8-bassiz-giris` + `main`) | 2026-11-13 | 2026-11-14 | 2026-11-27 (kullanıcı onayı, PR #36 merge commit'i `06316de`) |
| 6e | ⏸ merge edildi (PR #37, `d2be1d1`, 2026-11-27, kullanıcı onayı); açık: F6e.3 (kullanıcıya), V6e-5'in DAST koşusu (`faz/6e-dast-ilk-kosu`, push bekliyor) ve raporunun F6e.5 triajı | `faz/6e-dinamik-denetim` (`faz/9-bakim`'in üstüne açıldı) | 2026-11-13 | | |

Durum değerleri: `⏳ başlamadı` · `🔄 devam ediyor` · `⏸ duraklatıldı` (devir notuna bak) · `✅ tamam`

## Gece koşusu (D-096)

Durum: **bitti (2026-11-14).** Kuyruk boşaldı; üç PR (#35, #36, #37) 2026-11-27'de merge edildi. Kota sınırına birkaç kez takıldı, kaldığı yerden sürdü. Kullanıcı `/novacore-night` ile başlatır; kurallar `.claude/skills/novacore-night/SKILL.md`'de. Koşu her turda bu bölümü okur ve günceller.

### İş kuyruğu

| # | İş | Durum | Not |
|---|---|---|---|
| 1 | Faz 8: CI sonucu, inceleme, bulgular, PR, merge | ✅ `611903f` | İki inceleme, on bulgu, hepsi kapalı. Bütün kontroller yeşil (`17790b1`); PR #35 2026-11-27'de merge edildi (kullanıcı) |
| 2 | Faz 9: F9.1 sınıflandırma, F9.2 maddeler, F9.3 Dependabot, F9.4 belgeler, PR, merge | ✅ `06316de` | Bütün adımlar ve iki inceleme bitti; kontroller yeşil (`d33a33c`); PR #36 2026-11-27'de merge edildi (kullanıcı) |
| 3 | Faz 6e: F6e.1 → F6e.5, PR, merge | ⏸ merge `d2be1d1` | F6e.1, F6e.2, F6e.4, F6e.5 (S-1…S-6, S-9, S-13, S-14) ve faz sonu incelemesi bitti. Kontroller yeşil (`1b0c22d`; SonarCloud kalite kapısı yalnız `dast.yml:43` S2068 yüzünden kırmızı, V6e-1'in false positive'i); PR #37 2026-11-27'de merge edildi (kullanıcı). DAST ilk koşusu: run `46002347264`. **F6e.3 (OpenID conformance suite) yapılmadı:** suite'in indirilip kurulması ön onaylı değil (F6e.4'ün araçlarıyla aynı gerekçe); PLAN'a göre sürüm öncesi şart → Kullanıcıya kalanlar |
| 4 | Sabah raporu ve devir notu | ✅ | 2026-11-14 |

### Karar adayları (DA)

Gece alınan kalıcı kararlar. Sabah tek düzenlemeyle DECISIONS'a işlenir (D-096 (6)).

| ID | Karar | Gerekçe / kaynak |
|---|---|---|
| DA-1 | Şifre yenileme kodunun denemesi kod karşılaştırılmadan önce concurrency stamp ile sayılır; sayımı kaybeden istek kodu siler (kapalı tarafa düşer). | V8-2: `UserManager` yazmalarının sonucu yok sayılınca eşzamanlı tahminler 5/100 sınırını aşıyordu; `1777b77` |
| DA-2 | Kod hasher'ı keyed singleton'dır (`PasswordResetCodes.HasherKey`); forgot ve reset'te kodu olmayan adres aynı hash işini yapar. | V8-3: D-030 (1), D-091 (4) "her adres aynı cevap" süre bakımından; `518b339` |
| DA-3 | Kilitli hesabın şifresi gerçek hash'e değil yalnız sahte hash'e karşı sınanır: kilitliyken doğru şifre de bir şey açığa vurmaz, süre diğer retlerle aynıdır. | V8-4; R-138 (`SignInManager.CheckPasswordSignInCoreAsync`); `067821e` |
| DA-4 | Aynı adrese eşzamanlı iki davet ikisi de 200 alır ve aynı hesabı davet eder (biri 409 almaz); kullanıcı deposu kaybedilen insert'i `DuplicateUserName` olarak bildirir. | V8-5; `8fc5500` |
| DA-5 | Davetin "kullanıldı" işareti ve şifre yenileme kodunun silinmesi yeni şifreyle aynı transaction'da commit edilir. | V8-6: şifre kaydı eşzamanlı yazmaya yenilince davet/kod yanıyordu; `0aeeaea` |
| DA-6 | Kodun deneme sayısı ve hesabın serisi tek kayıtta (tek stamp kontrolü), karşılaştırmadan önce sayılır (DA-1'i tamamlar). | V8-9: ayrı kayıt kaybedilince tahmin seriye yazılmıyordu; `a619a94` |
| DA-7 | Seed bir client'ı yalnız bir alanı değiştiyse yazar; secret `ValidateClientSecretAsync` ile sınanır, eşleşiyorsa saklı hash korunur. | F9.2f (L56): her açılışta yeniden hash + yazma, çok örnekli açılışta satır yarışı; R-142; `bcfd95e` |
| DA-8 | Bir eklenti host'un assembly'sini kendisi bulmaz (`Assembly.Load`, `GetEntryAssembly` yok); host `Program.cs`'te açıkça verir. `service.version` için `OpenTelemetry:ServiceVersion` ayarı eklenmedi (Y-19; `OTEL_RESOURCE_ATTRIBUTES` yetiyor). | V9-1 (Y-20'nin testi); `83cea0c` |
| DA-9 | DAST job'u bulgu yüzünden kırmızı olmaz (raporlar artifact); taramanın kendisi koşamazsa kırmızı olur. Tarayıcı görüntülerinin digest'lerini Dependabot güncellemez. | F6e.2; `d07f199` |
| DA-10 | Onaysız hesaba farklı şifreyle ikinci kayıt gelirse şifre silinir ve sahibine şifre kurma bağlantısı gider; sıfırlama sayfası şifresiz onaysız hesabı kabul edip aynı adımda onaylar (R-084'teki değişmezin yerine). Audit olayı EventId 46. | S-1; R-155 (Sudhodanan & Paverd §6.2.2); `6d709a4` |
| DA-11 | Rol atamasında yükseltme koruması rolün adına değil izinlerine bakar: rolün izinlerinde `identity:*` varsa `identity:roles:manage` gerekir (D-053 (6)'yı genişletir). | S-4; `a484278` |
| DA-12 | Çok kiracılılık eklentisi kurum rollerinde `identity:` önekli izni açılışta reddeder (D-080 (3)'ün "eklenti denetlemez" cümlesinin yerine). | S-3; `a39e0e7` |

### Kullanıcıya kalanlar

Ön onaylı olmayan ya da ortamın engellediği işler (D-096 (5)). Koşu bunlarda durmaz, buraya yazıp sıradaki işe geçer.

| İş | Neden | Ne gerekli |
|---|---|---|
| Dependabot PR'ları | Faz 9 merge edildi (F9.3'ün güncellemeleri `main`'de); açık PR'lar eski `main`'e karşı, `secret-scan` o yüzden kırmızıydı | Dependabot üstü kalanları kapatır; kalanlar `@dependabot rebase` ile yeni `main`'e karşı koşar |
| CodeRabbit CLI incelemesi (Faz 8) | "session could not be authenticated" | `coderabbit auth login`, sonra `coderabbit review --agent --base main` |
| CodeRabbit PR incelemesi | Repo 10 yıldızın altında, otomatik inceleme yok; bot PR'larını hiç incelemez | PR başına `@coderabbitai review` (2026-11-27'de #35, #36, #37'ye yazıldı; yorumlar bir sonraki oturumda bulgu olarak okunur) |
| SonarCloud: `dast.yml:43` S2068 ("password") | V6e-1'in false positive'i (değer `openssl rand` ifadesi); kalite kapısını kırmızı yapıyor | Kullanıcı SonarCloud'da "Safe/False positive" işaretler (L96 ile birlikte) |
| S-1'in kalan yolu: kurban kendi kaydını yapmadan saldırganın kaydından gelen onay linkine tıklarsa hesap saldırganın şifresiyle onaylanır | Kapatmanın yolu onay anında şifre istemek: her kullanıcının kayıt akışını ve `RegisterRequest` sözleşmesini değiştirir (ürün kararı) | Kullanıcı karar verir; onaylarsa kayıt "önce e-posta, sonra şifre" akışına geçer |
| S-7: açık kaydı kapatılabilir yapmak | Ürün kararı; Y-19 bir seçenek için gerçek tüketici ister. Kayıt olan herkes platform `UserPermissions`'ını alıyor | Kullanıcı karar verir: CRM-Nominal açık kayıt kullanacak mı; kullanmayacaksa `Identity:SignIn:AllowRegistration` (ya da kaydın hiç eşlenmemesi) planlanır |
| F6e.4'ün adlı araçları (Cloudflare `security-audit-skill`, Claude Security plugin) | Kurulu değil; indirme/kurma ön onaylı değil | Kullanıcı kurar, iki kapsamla koşar |
| F6e.3: OpenID conformance suite (yerel Docker) | Suite'in kaynağı/imajları indirilip kurulmalı, ön onaylı değil; PLAN "sürüm öncesi" diyor (D-089 (2) yalnız 0.1.0 için muaf tuttu) | Kullanıcı suite'i kurar ya da indirmeyi onaylar; bir sonraki sürümden önce koşulur, bulgular F6e.5 kuralıyla |
| Dependabot #21 (`global.json` → SDK 10.0.403) | Bu makinede yalnız 10.0.300 kurulu (F9.3 kuralı) | SDK 10.0.403 kurulur, PR merge edilir |
| S-6 varsayılanları (`Identity:EmailLimit` 5/saat, aşınca 429) | Ürün ayarı; R-157 | Kullanıcı değerleri onaylar ya da değiştirir |
| V6e-4: bekleyen daveti olan şifresiz hesaba anonim kayıt | Hiçbir şey yapmamak ya da ayrı metin/audit: ürün davranışı | Kullanıcı seçer |
| SonarCloud'un CORS bulgusunu "false positive" işaretlemek (L96) | Dış işlem; `AllowAnyOrigin` yalnız Development'ta (`CoreWebExtensions.cs:182`) | Kullanıcı SonarCloud'da işaretler |

## Adımlar

Biçim: `- [x] F0.3 (abc1234): not`. Not en çok iki satırdır: ne yapıldı, koşulan test sınıfları, doğrulamaya kalan. Gerekçe D/R kaydına, ayrıntı commit mesajına yazılır.

Biten fazların adımları: [PROGRESS-ARSIV.md](PROGRESS-ARSIV.md#adımlar).

### Faz 6e
Başlangıç 2026-11-13, `faz/9-bakim` @ `1f3c034`. Plan onayı D-096 (2). Docker çalışıyor; PLAN'ın adını verdiği Cloudflare `security-audit-skill` ve Claude Security plugin kurulu değil (kullanıcıya kaldı).

- [x] F6e.1 (`2b6df56`): `dast` client yalnız `eng/dast/dast.env`'de (ortam değişkenleri, index 100); `client_credentials`, dört okuma izni, secret koşuda `openssl rand`. Kod değişmedi. `DastClientTests` (5) önce kırmızı, sonra yeşil.
- [x] F6e.2 (`d07f199`): `dast.yml` (haftalık + `workflow_dispatch`), `eng/dast.sh`, `/connect` ve `/account` için dar hedef listesi. Schemathesis 4.33.1, ZAP 2.17.0 (digest'e sabit; D-044: CI aracı, NuGet değil). Bulgu job'u kırmızı yapmaz, taramanın koşamaması yapar (DA-9). Yerelde Schemathesis 3 örnekle: `/account/*` formlarında ve `/connect/revoke`'ta bozuk form girdisine 500 (S-14). ZAP yerelde denenmedi (indirme takıldı). İlk koşu merge'den sonra (kullanıcıya kaldı).
- [x] F6e.4: kurulu `security-review` ölçütleriyle dört Opus koşusu (salt okunur): auth çekirdeği ×2 (uçtan içeri; kontrol listesi/değişmezlerden koda), bütün repo ×2 (dizin dizin; OWASP API Top 10 + tehdit modeli). Skill yalnız diff okuduğu için ölçütleri elle, bütün koda uygulandı. Faz 6b'nin düzeltmeleri yerinde; bağımlılık taraması temiz.

#### Doğrulama (Faz 6e)

| ID | Bulgu | Yer | Durum |
|---|---|---|---|
| V6e-1 | CI `secret-scan` kırmızı: 1 bulgu, `1f3c034..4814ed1` aralığında (F6e.1/F6e.2 ya da S-1/S-2 commit'leri) | run `43579950658` | doğrulandı yerelde (`d5954f4`): `dast.yml:44`'teki çalışma anında üretilen parola ifadesi (generic-password FP); yalnız bu yol ve bu ifade için allowlist; yerel tam tarama temiz, CI bekleniyor |
| V6e-2 | Bellek DoS: HTML `/account/forgot-password` e-postasının uzunluğu sınırsız (4 MB'a kadar); `EmailRecipientLimit` her farklı değeri bir saat partition olarak tutar | Opus inceleme, `PasswordResetPage.cs:60-67`, `EmailRecipientLimit.cs:56-60` | kapandı (`3144da9`): form e-postası `EmailAddress` + 256; `TryAcquire` uzun anahtarı reddeder; 4 kırmızı → yeşil |
| V6e-3 | `InvalidDataException`→400 kaynağa bakmıyor: modülün bozuk arşiv açması (Zip/GZip/Brotli) 500 yerine logsuz 400 olur | Opus inceleme, `ExceptionMapping.cs:28,50-51,61-62` | kapandı (`c8b3d17`, R-158): form okuyucunun `InvalidDataException`'ı okunduğu yerde `BadHttpRequestException`'a sarılır; çıplak `InvalidDataException` 500 + log; 2 kırmızı → yeşil |
| V6e-4 | Bekleyen daveti olan şifresiz hesaba anonim `register`: davetliye "yeniden kayıt" metinli şifre bağlantısı ve Warning audit gider (güvenlik açığı değil) | Opus inceleme, `AccountEndpoints.cs:741-760` | kullanıcıya: davranış ürün kararı (Kullanıcıya kalanlar) |
| V6e-5 | DAST'ın ilk koşusu hiçbir tarama koşmadan kırmızı: Schemathesis imajı (uid 1000) runner'ın (uid 1001) rapor klasöründe `--report-dir` açamadı | run `46002347264`, `eng/dast.sh:72-84` | kapatıldı (`045f686`, branch `faz/6e-dast-ilk-kosu`): rapor klasörü ZAP'ınki gibi önceden açılıp yazılabilir yapılır. xUnit'le gösterilemez (CI betiği, kapsayıcı kullanıcısı); kontrolü push sonrası `workflow_dispatch` koşusu |

#### F6e.4 bulguları ve F6e.5 triajı

| ID | Bulgu (koşular) | Önem | Triaj |
|---|---|---|---|
| S-1 | Kayıtla hesabın önceden ele geçirilmesi: onaysız adrese ikinci kayıt sessizce yutulur, onay linki ilk kaydedenin şifresiyle açar; 2FA ile kalıcılaşır, davet üyeliği saldırgana gider (A1-1, A2-1, R2-2) | orta | kapatıldı (`6d709a4`; R-155): onaysız hesaba başka şifreyle ikinci kayıt şifreyi siler (stamp yenilenir, eski onay linkleri ölür), posta kutusuna şifre kurma bağlantısı gider. 2 test önce kırmızı. Kalan: kurban hiç kayıt olmadan saldırganın onay linkine tıklarsa → `needs_validation` (onayda şifre istemek ürün kararı) |
| S-2 | Davetle açılmış parolasız hesaba herkes onay e-postası yollatabilir; onaylanırsa davet kullanılamaz (A2-2) | düşük | kapatıldı (`e4d724a`): `resend` ve `confirm-email` şifresiz hesaba dokunmaz. 2 test önce kırmızı |
| S-3 | `MultiTenancy:Roles`'a `identity:*` platform izni yazılabiliyor; belge yasaklıyor, açılışta denetim yok (A1-2) | düşük-orta | kapatıldı (`a39e0e7`): `MultiTenancyOptions` doğrulaması `identity:` önekli izni reddeder (host durur). Test önce kırmızı |
| S-4 | Rol atamasında yükseltme koruması rolün adına bakıyor, içerdiği `identity:*` izinlerine değil (R2-5) | orta (OLASI) | kapatıldı (`a484278`): rolün izinlerinde herhangi bir `identity:*` varsa atama/kaldırma `identity:roles:manage` ister (D-053 (6)'nın genişlemesi). Test önce kırmızı (204) |
| S-5 | Davet denetim kaydında daveti yapan yok (A2-3) | düşük | kapatıldı (`33c65ca`): davet denetim kaydı daveti yapanın id'sini yazar (`ICurrentUser`). Test önce kırmızı |
| S-6 | E-posta gönderen anonim uçlarda alıcı adres başına sınır yok; IPv6 /64 döndürerek posta bombası, kod geçersizleştirme, 1000'lik kuyruğun dolması (R2-1) | orta | ✅ `ca29f6a`: anonim e-posta uçlarında alıcı adres başına sınır (`Identity:EmailLimit`, varsayılan 5/saat, herkese aynı 429) ve IPv6 /64 bölümleme (R-157); 5 kırmızı → yeşil. Varsayılanlar ürün kararı (Kullanıcıya kalanlar); örnek başına bellekte sayım |
| S-7 | Açık kayıt kapatılamıyor; kayıt olan herkes platform `UserPermissions`'ını alır (R2-3) | orta | **kullanıcıya:** ürün kararı (Y-19: seçenek için tüketici gerekir) |
| S-8 | Ödeme oluşturmada idempotency anahtarı yok (R2-4) | düşük | `needs_validation`: API tasarım kararı, referans modül |
| S-9 | Testi olmayan değişmezler: scope yükseltme reddi (#17), başka client'ın token'ını iptal (#14), HTML `ReturnUrl` için `//evil` ve `/\evil`, üretimdeki 30 sn leeway sonrası zincir iptali (A2) | — | ✅ `63d588c`: dört değişmez testte; hata bulunmadı |
| S-10 | Bağlantıyla sıfırlama token'ı 6 saat ve onay token'ıyla ortak sağlayıcı (#31) | düşük | `needs_validation` |
| S-11 | Outbox handler hata mesajı ileride kişisel veriyi `Error` sütununa ve loga taşıyabilir (D1) | düşük | `needs_validation` |
| S-12 | `includeInactive=true` her ürün okuyucusuna pasif ürünleri gösteriyor; karar kaydı yok (D2) | — | `needs_validation`: ürün kararı |
| S-13 | Şablonun `Program.cs:75`'inde `GetValue<int?>("HealthChecks:ProbePort")` (R-048'e aykırı) | — | ✅ `e18bd98`: `HealthCheckProbeOptions` (`[Range]`, `[OptionsValidator]`, `ValidateOnStart`); smoke CI'da |
| S-14 | Bozuk form girdisine 500: `/account/*` formları ve `/connect/revoke` (F6e.2'nin yerel Schemathesis denemesi) | düşük-orta | ✅ `0923d61`: çekirdeğin `ExceptionMapping`'i `InvalidDataException` ve `AntiforgeryValidationException`'ı 400 yapar (R-156); `MalformedFormTests` 40/96 kırmızıydı. needs_validation: `/connect/*` artık OAuth `invalid_request` yerine ProblemDetails döner; sunucunun kendi `InvalidDataException`'ı da 400 olur |

Doğrulanamayanlar (`needs_validation`): son admin korumasında check-then-act yarışı (biliniyor, L94); CSP'nin `form_post` otomatik gönderimini engelleyip engellemediği (#8); `alg:none`/`jku` reddi (#21); kurumsal NAT arkasında `strict` kovasının `/connect/token` yenilemelerini de kapsaması. Biliniyor (Sonraya): L60, L44, L94, L95, L96, L103, L106, L107, L108, L11.

## Açık sorular (Q)

Yalnız cevabı beklenen sorular. Cevaplanan soru cevabıyla birlikte arşivin "Cevaplanmış sorular" bölümüne taşınır; kalıcı sonucu DECISIONS'a D-kaydı olarak yazılır. Sıradaki ID: **Q-64**. Q-40 sürüyor: izlenmeyen `.agents/`, `.codex/`, `AGENTS.md` dosyalarına dokunulmaz.

| ID | Soru | Ne zaman gerekli | Cevap |
|---|---|---|---|

## Son devir notu

> Yalnız en son not durur. Yeni not yazılırken bir önceki arşivin "Eski devir notları" bölümünün en üstüne taşınır (D-097).

**2026-11-27, Faz 6e: DAST'ın ilk koşusu.** Run `46002347264` hiçbir tarama koşmadan kırmızıydı (V6e-5): Schemathesis rapor klasörünü açamadı. `045f686` (`faz/6e-dast-ilk-kosu`, `main`'den) klasörü ZAP'ınki gibi önceden açar; yerelde Docker Desktop bunu göstermez, yalnız `bash -n` koşuldu.

- **Sıradaki:** branch push'lanır, `gh workflow run dast.yml --ref faz/6e-dast-ilk-kosu` ile DAST koşulur; raporu (artifact `dast-report`) F6e.5 kuralıyla triaj edilir, bulgular `S-15…` olarak; düzeltmeler ve PR `main`'e. ZAP CI'da ilk kez koşacak. Sonra: CodeRabbit'in #35 yorumları (Faz 8), DA-1…DA-12 tek düzenlemeyle DECISIONS'a, D-032 (`DECISIONS.md` `protectedPaths`'e, kullanıcı).
- **Bilinmesi gerekenler:** Schemathesis'in `.hypothesis/examples` veritabanı da `/dast`'a yazamaz; belgesine göre uyarı verip başka yer kullanır. V6e-3'ün bedeli ve `/account/login`'in 256 sınırı arşivdeki notta.
- **Bekleyen karar:** push. Kullanıcıya kalanlar tablosu (S-6 varsayılanları, V6e-4, S-7, S-1'in kalan yolu). Q-40 sürüyor.
