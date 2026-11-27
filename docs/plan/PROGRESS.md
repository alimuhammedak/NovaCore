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
| 6e | ⏸ duraklatıldı (PR #37 merge `d2be1d1`, 2026-11-27; DAST bulguları `faz/6e-dast-ilk-kosu`'da, kalan: S-16'nın sonu, push, DAST yeniden, doğrulama; F6e.3 D-102 (2)) | `faz/6e-dinamik-denetim` (`faz/9-bakim`'in üstüne açıldı) | 2026-11-13 | | |
| 10 | ⏳ başlamadı (6e'nin merge'ünden sonra, D-102) | `faz/10-surum-0.2.0` (`main`'den) | | | |

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

DA-1…DA-12 2026-11-27'de DECISIONS'a işlendi: D-099 (DA-1…6), D-100 (DA-7, 8), D-101 (DA-9…12). Tablo arşivde.

### Kullanıcıya kalanlar

Ön onaylı olmayan ya da ortamın engellediği işler (D-096 (5)). Koşu bunlarda durmaz, buraya yazıp sıradaki işe geçer.

| İş | Neden | Ne gerekli |
|---|---|---|
| CodeRabbit CLI incelemesi (Faz 8) | "session could not be authenticated" | `coderabbit auth login`, sonra `coderabbit review --agent --base main` |
| SonarCloud: `dast.yml:43` S2068 ("password") | V6e-1'in false positive'i (değer `openssl rand` ifadesi); kalite kapısını kırmızı yapıyor | Kullanıcı SonarCloud'da "Safe/False positive" işaretler (L96 ile birlikte) |
| Dependabot #21 (`global.json` → SDK 10.0.403) | Bu makinede yalnız 10.0.300 kurulu (F9.3 kuralı) | SDK 10.0.403 kurulur, PR merge edilir |
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
| V6e-4 | Bekleyen daveti olan şifresiz hesaba anonim `register`: davetliye "yeniden kayıt" metinli şifre bağlantısı ve Warning audit gider (güvenlik açığı değil) | Opus inceleme, `AccountEndpoints.cs:741-760` | D-102 (6): davranış değişmez, README'ye (F10.3) |
| V6e-5 | DAST'ın ilk koşusu hiçbir tarama koşmadan kırmızı: Schemathesis imajı (uid 1000) runner'ın (uid 1001) rapor klasöründe `--report-dir` açamadı | run `46002347264`, `eng/dast.sh:72-84` | kapatıldı (`045f686`, branch `faz/6e-dast-ilk-kosu`): rapor klasörü ZAP'ınki gibi önceden açılıp yazılabilir yapılır. xUnit'le gösterilemez (CI betiği, kapsayıcı kullanıcısı); run `46007644116` taramaları koşturdu |
| V6e-6 | Run `46007644116` yeşil ve "bulgulu" ama Schemathesis'in bütün işlemleri JUnit `<error>`: sertifikayı okuyamadı (`dotnet dev-certs` PEM'i yalnız sahibine açar); betik exit 1'i bulgu saydı | run `46007644116`, `eng/dast.sh` | kapatıldı (`8dfc457`): kopya herkese okunur; JUnit'te `<error>` varsa tarama eksik sayılır, job kırmızı (D-101 (1)). Run `46008665907`: 0 hata, 33 kontrol bulgusu |

#### F6e.4 bulguları ve F6e.5 triajı

| ID | Bulgu (koşular) | Önem | Triaj |
|---|---|---|---|
| S-1 | Kayıtla hesabın önceden ele geçirilmesi: onaysız adrese ikinci kayıt sessizce yutulur, onay linki ilk kaydedenin şifresiyle açar; 2FA ile kalıcılaşır, davet üyeliği saldırgana gider (A1-1, A2-1, R2-2) | orta | kapatıldı (`6d709a4`; R-155): onaysız hesaba başka şifreyle ikinci kayıt şifreyi siler (stamp yenilenir, eski onay linkleri ölür), posta kutusuna şifre kurma bağlantısı gider. 2 test önce kırmızı. Kalan: kurban hiç kayıt olmadan saldırganın onay linkine tıklarsa → kabul edilmiş risk, README (D-102 (4)) |
| S-2 | Davetle açılmış parolasız hesaba herkes onay e-postası yollatabilir; onaylanırsa davet kullanılamaz (A2-2) | düşük | kapatıldı (`e4d724a`): `resend` ve `confirm-email` şifresiz hesaba dokunmaz. 2 test önce kırmızı |
| S-3 | `MultiTenancy:Roles`'a `identity:*` platform izni yazılabiliyor; belge yasaklıyor, açılışta denetim yok (A1-2) | düşük-orta | kapatıldı (`a39e0e7`): `MultiTenancyOptions` doğrulaması `identity:` önekli izni reddeder (host durur). Test önce kırmızı |
| S-4 | Rol atamasında yükseltme koruması rolün adına bakıyor, içerdiği `identity:*` izinlerine değil (R2-5) | orta (OLASI) | kapatıldı (`a484278`): rolün izinlerinde herhangi bir `identity:*` varsa atama/kaldırma `identity:roles:manage` ister (D-053 (6)'nın genişlemesi). Test önce kırmızı (204) |
| S-5 | Davet denetim kaydında daveti yapan yok (A2-3) | düşük | kapatıldı (`33c65ca`): davet denetim kaydı daveti yapanın id'sini yazar (`ICurrentUser`). Test önce kırmızı |
| S-6 | E-posta gönderen anonim uçlarda alıcı adres başına sınır yok; IPv6 /64 döndürerek posta bombası, kod geçersizleştirme, 1000'lik kuyruğun dolması (R2-1) | orta | ✅ `ca29f6a`: anonim e-posta uçlarında alıcı adres başına sınır (`Identity:EmailLimit`, varsayılan 5/saat, herkese aynı 429) ve IPv6 /64 bölümleme (R-157); 5 kırmızı → yeşil. Varsayılan 5/saat kalır (D-102 (5)); örnek başına bellekte sayım |
| S-7 | Açık kayıt kapatılamıyor; kayıt olan herkes platform `UserPermissions`'ını alır (R2-3) | orta | D-102 (3): `Identity:SignIn:AllowRegistration`, varsayılan `true` (F10.3) |
| S-8 | Ödeme oluşturmada idempotency anahtarı yok (R2-4) | düşük | D-102 (7): eklenmez; README notu, Sonraya (F10.3) |
| S-9 | Testi olmayan değişmezler: scope yükseltme reddi (#17), başka client'ın token'ını iptal (#14), HTML `ReturnUrl` için `//evil` ve `/\evil`, üretimdeki 30 sn leeway sonrası zincir iptali (A2) | — | ✅ `63d588c`: dört değişmez testte; hata bulunmadı |
| S-10 | Bağlantıyla sıfırlama token'ı 6 saat ve onay token'ıyla ortak sağlayıcı (#31) | düşük | `needs_validation` |
| S-11 | Outbox handler hata mesajı ileride kişisel veriyi `Error` sütununa ve loga taşıyabilir (D1) | düşük | `needs_validation` |
| S-12 | `includeInactive=true` her ürün okuyucusuna pasif ürünleri gösteriyor; karar kaydı yok (D2) | — | D-102 (8): yalnız `catalog:products:update` (F10.3) |
| S-13 | Şablonun `Program.cs:75`'inde `GetValue<int?>("HealthChecks:ProbePort")` (R-048'e aykırı) | — | ✅ `e18bd98`: `HealthCheckProbeOptions` (`[Range]`, `[OptionsValidator]`, `ValidateOnStart`); smoke CI'da |
| S-14 | Bozuk form girdisine 500: `/account/*` formları ve `/connect/revoke` (F6e.2'nin yerel Schemathesis denemesi) | düşük-orta | ✅ `0923d61`: çekirdeğin `ExceptionMapping`'i `InvalidDataException` ve `AntiforgeryValidationException`'ı 400 yapar (R-156); `MalformedFormTests` 40/96 kırmızıydı. needs_validation: `/connect/*` artık OAuth `invalid_request` yerine ProblemDetails döner; sunucunun kendi `InvalidDataException`'ı da 400 olur |
| S-15 | OpenAPI belgesi 401/403'ü belgelemiyor: izin isteyen uçlar (`/admin/*`, `/products`, `/payments`, `/auth/2fa/*`, `logout-all`) ve bazı 400'ler `application/json` diye belgeli, dönen `problem+json` (DAST run `46008665907`: ST103 15 uç, ST105 3) | düşük-orta (üretilen istemciler) | 401/403 kapatıldı (`dfb1dd3`): çekirdeğin `AuthorizationResponsesTransformer`'ı, 19 işlem; `OpenApiTests` 4 kırmızı → yeşil. ST105 kapatıldı (`965100b`, R-160): `confirm-email`'in 400'ü iki gövdeli (uç bazlı transformer); admin rol uçlarının 404'ü `{userId:guid}`'e uymayan yoldan: FP |
| S-16 | Şemaya uyan istek 400 alıyor: gövde şemasında DataAnnotations kısıtları eksik (boş `Email`, `ReturnUrl` 4096), sayfa parametreleri `integer\|string` ve sınırsız (ST202, `/api/v1/auth/*`, liste uçları) | düşük | kısmen kapatıldı (`79ebf69`, R-160, D-103 (1)): attribute'lar `property:` hedefli, şemada `maxLength`/`minLength`/`minimum`; 8 kırmızı → yeşil. Açık: `[Required]` boş dizgiyi şemada dışlamıyor, `[EmailAddress]` format vermiyor, sayfa parametreleri sınırsız. `ReturnUrl`'nin "bu host'ta yol" kuralı FP |
| S-20 | Şema adı çakışması: `CreateProduct.Request`, `UpdateProduct.Request`, `CreatePayment.Request` tek `Request` şemasında; `POST /api/v1/payments` ürünün gövdesiyle belgeli (`Response` aynı). S-16'nın ölçümünde bulundu | orta (üretilen istemciler) | kapatıldı (`19fdd59`, R-160, D-103 (3)): public tipin içindeki tipin şema adı tanımlayan tiplerle başlar (`CreatePaymentRequest`); 2 kırmızı → yeşil |
| S-17 | Elle yazılan protokol şeması eksik: `/account/forgot-password` ve `/account/login/2fa`'nın GET'i yok (ST204/ST205); OpenIddict TRACE'e 405 yerine OAuth `invalid_request` 400 döner | — | kapatıldı (`d1a244c`): iki GET şemada; TRACE FP (OpenIddict) |
| S-18 | CSP'de `form-action` ve `base-uri` yok; ikisi `default-src`'ye düşmez (ZAP, `/account/*`) | orta | kapatıldı (`b12ebdc`, R-159): `base-uri 'none'`; `NegativeProtocolTests` 3 kırmızı → yeşil. `form-action` eklenmez (Chrome yönlendirme zincirini engeller), gerekçe README'de |
| S-19 | ZAP'ın kalanı: HSTS yok, COEP ve Permissions-Policy yok, giriş formunda "kullanıcı girdisi attribute'ta" (`Password=ZAP`), bilgi satırları | düşük | HSTS FP (DAST Development'ta, `UseHsts` yalnız dışında); yansıma FP (şifre alanında `value` yok, e-posta encode'lu); COEP/Permissions-Policy `needs_validation` |

DAST'ta sunucu hatası (5xx) çıkmadı.

Doğrulanamayanlar (`needs_validation`): son admin korumasında check-then-act yarışı (biliniyor, L94); CSP'nin `form_post` otomatik gönderimini engelleyip engellemediği (#8); `alg:none`/`jku` reddi (#21); kurumsal NAT arkasında `strict` kovasının `/connect/token` yenilemelerini de kapsaması. Biliniyor (Sonraya): L60, L44, L94, L95, L96, L103, L106, L107, L108, L11.

## Açık sorular (Q)

Yalnız cevabı beklenen sorular. Cevaplanan soru cevabıyla birlikte arşivin "Cevaplanmış sorular" bölümüne taşınır; kalıcı sonucu DECISIONS'a D-kaydı olarak yazılır. Sıradaki ID: **Q-64**. Q-40 sürüyor: izlenmeyen `.agents/`, `.codex/`, `AGENTS.md` dosyalarına dokunulmaz.

| ID | Soru | Ne zaman gerekli | Cevap |
|---|---|---|---|

## Son devir notu

> Yalnız en son not durur. Yeni not yazılırken bir önceki arşivin "Eski devir notları" bölümünün en üstüne taşınır (D-097).

**2026-11-27, Faz 6e: S-15 kapandı, S-16 kısmen, S-20 bulundu ve kapandı.** Üç commit (`965100b`, `79ebf69`, `19fdd59`) `faz/6e-dast-ilk-kosu`'da, push edilmedi. Sürprizler: .NET 10 bir durum kodu için yalnız son `Produces*`'ı tutar; record attribute'ları `property:` hedefi olmadan şemaya girmez; aynı adlı iç içe `Request`/`Response` tipleri tek şemayı paylaşıyordu (`/payments` ürünün gövdesiyle belgeliydi, S-20). Kararlar D-103, R-160.

- **Akış (D-104):** `/novacore-phase 6e` bir çağrıda fazın kalanını bitirir; adımları yazan alt ajanlar yazar.
- **Sıradaki:** S-16'nın kalanı: `[Required]` (boş dizgi yasak) → `minLength: 1` ve `[EmailAddress]` → `format: email` için çekirdekte bir şema transformer'ı (seçenekler R-160'ta; önce `OpenApiTests`'te kırmızı), sayfa parametrelerinin sınırı (`PaginationRequest`, Abstractions). Sonra branch push, DAST yeniden koşulur (`gh workflow run dast.yml --ref faz/6e-dast-ilk-kosu`), ardından `/novacore-verify 6e`.
- **Doğrulamaya kalanlar:** tam paket, SQL Server/PostgreSQL katmanları, `check-openapi` (şema adları `Request`/`Response` → `Create*Request` vb., yeni `maxLength`/`minLength`; oasdiff'in kırıcı saymadığı görülmeli), şablon smoke (`CreateTodo`'nun `property:` hedefi), coverage.
- **Bekleyen karar:** yok. Q-40 sürüyor.
