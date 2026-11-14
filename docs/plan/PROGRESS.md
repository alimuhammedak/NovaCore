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
| 8 | ⏸ PR #35 açık, merge edilmedi: adımlar ve doğrulama bitti (V8-1…V8-10 kapalı), `sonar` kırmızı (Kullanıcıya kalanlar) | `faz/8-bassiz-giris` | 2026-11-09 | | |
| 9 | ⏸ PR #36 açık, merge edilmedi: adımlar ve incelemeler bitti; #35'in üstünde, `sonar` kırmızı | `faz/9-bakim` (`faz/8-bassiz-giris` + `main`) | 2026-11-13 | | |
| 6e | 🔄 devam ediyor | `faz/6e-dinamik-denetim` (`faz/9-bakim`'in üstüne açıldı) | 2026-11-13 | | |

Durum değerleri: `⏳ başlamadı` · `🔄 devam ediyor` · `⏸ duraklatıldı` (devir notuna bak) · `✅ tamam`

## Gece koşusu (D-096)

Durum: **sürüyor (2026-11-13).** Bir kez kota sınırına takıldı, kaldığı yerden sürdü. Kullanıcı `/novacore-night` ile başlatır; kurallar `.claude/skills/novacore-night/SKILL.md`'de. Koşu her turda bu bölümü okur ve günceller.

### İş kuyruğu

| # | İş | Durum | Not |
|---|---|---|---|
| 1 | Faz 8: CI sonucu, inceleme, bulgular, PR, merge | ⏸ PR #35 | İki inceleme, on bulgu, hepsi kapalı. `sonar` dışında bütün job'lar yeşil; son push (`6e7fdb6`) run `43392510435`, sonucu Faz 9'un ilk adımında okunur. Merge koşulu `sonar` yüzünden tutmuyor |
| 2 | Faz 9: F9.1 sınıflandırma, F9.2 maddeler, F9.3 Dependabot, F9.4 belgeler, PR, merge | ⏸ PR #36 | Bütün adımlar ve iki inceleme bitti; son push `1f3c034`. Merge koşulu tutmuyor (`sonar`; #35'e bağlı) |
| 3 | Faz 6e: F6e.1 → F6e.5, PR, merge | 🔄 | Docker bu kez çalışıyor (29.2.1). F6e.4'ün adını verdiği iki araç kurulu değil: kurulu `security-review` ile koşulur. `workflow_dispatch` ilk koşusu workflow `main`'e girmeden alınamaz |
| 4 | Sabah raporu ve devir notu | ⏳ | |

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

### Kullanıcıya kalanlar

Ön onaylı olmayan ya da ortamın engellediği işler (D-096 (5)). Koşu bunlarda durmaz, buraya yazıp sıradaki işe geçer.

| İş | Neden | Ne gerekli |
|---|---|---|
| `sonar` job'u | "Authentication with the server has failed"; `main`'de de 2026-11-09'dan beri kırmızı (run `42651867223`), kodla ilgisi yok | Kullanıcı `SONAR_TOKEN` secret'ını yeniler. Yenilenene kadar "bütün kontroller yeşil" koşulu tutmaz: PR'lar merge edilmez, fazlar üst üste açılır (D-096 (4)) |
| `main`'de `secret-scan` kırmızı (6 bulgu) | Tarama bütün branch'lerin geçmişini `main`'in allowlist'iyle okuyor; bulgular Faz 8/9 branch'lerindeki test parolaları. `main`'e açılan her PR'da (Dependabot) da kırmızı | PR #35 merge edilince kendiliğinden düzelir (önce `SONAR_TOKEN`) |
| Dependabot #2, #28, #30, #31, #32, #34 | Güncellemeleri Faz 9 branch'inde yapıldı (F9.3); PR'lar açık duruyor | Faz 9 merge edilince Dependabot kendisi kapatır; kapatmazsa kullanıcı kapatır |
| CodeRabbit CLI incelemesi (Faz 8) | "session could not be authenticated" | `coderabbit auth login`, sonra `coderabbit review --agent --base main` |
| DAST'ın ilk koşusu (F6e.2'nin kabulü) | `workflow_dispatch` yalnız varsayılan branch'teki workflow'u tetikler; Faz 6e merge edilmedi | Merge'den sonra `gh workflow run dast.yml`, raporlar F6e.5'e |
| S-1'in kalan yolu: kurban kendi kaydını yapmadan saldırganın kaydından gelen onay linkine tıklarsa hesap saldırganın şifresiyle onaylanır | Kapatmanın yolu onay anında şifre istemek: her kullanıcının kayıt akışını ve `RegisterRequest` sözleşmesini değiştirir (ürün kararı) | Kullanıcı karar verir; onaylarsa kayıt "önce e-posta, sonra şifre" akışına geçer |
| S-7: açık kaydı kapatılabilir yapmak | Ürün kararı; Y-19 bir seçenek için gerçek tüketici ister. Kayıt olan herkes platform `UserPermissions`'ını alıyor | Kullanıcı karar verir: CRM-Nominal açık kayıt kullanacak mı; kullanmayacaksa `Identity:SignIn:AllowRegistration` (ya da kaydın hiç eşlenmemesi) planlanır |
| F6e.4'ün adlı araçları (Cloudflare `security-audit-skill`, Claude Security plugin) | Kurulu değil; indirme/kurma ön onaylı değil | Kullanıcı kurar, iki kapsamla koşar |
| Dependabot #21 (`global.json` → SDK 10.0.403) | Bu makinede yalnız 10.0.300 kurulu (F9.3 kuralı) | SDK 10.0.403 kurulur, PR merge edilir |
| SonarCloud'un CORS bulgusunu "false positive" işaretlemek (L96) | Dış işlem; `AllowAnyOrigin` yalnız Development'ta (`CoreWebExtensions.cs:182`) | Kullanıcı SonarCloud'da işaretler |

## Adımlar

Biçim: `- [x] F0.3 (abc1234): not`. Not en çok iki satırdır: ne yapıldı, koşulan test sınıfları, doğrulamaya kalan. Gerekçe D/R kaydına, ayrıntı commit mesajına yazılır.

Biten fazların adımları: [PROGRESS-ARSIV.md](PROGRESS-ARSIV.md#adımlar).

### Faz 8
Başlangıç 2026-11-09, `main` @ `f319d57`; giriş notu ve fazın yapılmayacakları arşivde ("### Faz 8").

- [x] F8.1–F8.8 (`1aaf0f9` … `163c4af`): adım notları ve oturum sonu koşusu arşivde ([PROGRESS-ARSIV.md](PROGRESS-ARSIV.md), "### Faz 8"; D-097 (2)).

#### Doğrulama: V8-1…V8-10 kapandı (DA-1…DA-6); tablo arşivde ("### Faz 8"). PR #35 açık.

### Faz 9
Başlangıç 2026-11-13, `faz/8-bassiz-giris` @ `6e7fdb6` + `main` @ `aeb9f38`. Plan onayı D-096 (2). Dependabot, `main`'de: #26, #27, #29, #33, #19 merge edildi (kontroller yeşil, çakışmasız); #21 kullanıcıya kaldı; #2, #28, #30, #31, #32, #34 kırmızı → F9.3.

- [x] F9.1: 98 madde, üç Sonnet sınıflandırıcı + Opus denetimi (`yapılacak`ların hepsi, diğerlerinden 17). Satır numaraları `SONRAYA-BIRAKILANLAR.md` @ `74d511d`.

F9.1'in alt adım tablosu arşivde (PROGRESS-ARSIV.md, "### Faz 9 F9.1 tablosu").

- [x] F9.2a (`ef2cde2`, Sonnet): kaynak taramaları `Solution.SourceFiles`'tan (bin/obj dışlı); `Update`'li referansta NRE testi önce kırmızı. `*.Architecture` 26/26. Opus incelemesi: testin paket satırı `PrivateAssets` yüzünden bir şey sınamıyor, `Directory.Build.*` okuması testsiz → F9.2a2. L25'in `GlobalPackageReference` kısmı yapılmadı (listede kalır).
- [x] F9.2b (`90d0fde`, Sonnet; Opus incelemesi temiz): dört fazla `EnsureCreatedAsync` silindi, iki cref, `LoginPage`'de `!`, `ProductCatalog.cs` `Internal/` klasörüne. 6 sınıf 60/60. L94'ün kullanılmayan `using` kısmı doğrulanamadı (listede kalır).
- [x] F9.2c (`bc5f29f`, Sonnet; Opus incelemesi temiz): `src/Modules/CLAUDE.md`'de Guid v7 cümlesi sağlayıcıya göre; R-140. Anahtar stratejisi listede kalır.
- [x] F9.2e (`7310497`): `service.version` `ApplicationName`'in gösterdiği assembly'den (R-141). `ObservabilityTests` önce kırmızı (`0.1.0.0`), sonra 4/4.
- [x] F9.2f (`bcfd95e`): seed, secret değişmediyse hash'i korur ve hiçbir alan değişmediyse yazmaz (R-142; DA-7). `ClientSeedTests` önce kırmızı (2/3), sonra 3/3; `ClientCredentialsTests`, `IdentityServerConfigurationTests` yeşil.

- [x] F9.2a2 (`8a35f87`): `PackageDependencyTests` `Update`'li paketi gerçekten sınıyor; `Directory.Build.*` yürüyüşü depo kökünde durur, okuması testli. İki kural geçici bozulup kırmızı görüldü.
- [x] F9.2d (`9fc17b1`): `MessageResponse` üst düzey internal record; `v1.json` değişmedi. 6 sınıf 61/61.

- [x] F9.2g (`c8446b9`): rol izinleri tek join'le (UserRoles→Roles→RoleClaims); rol artık id ile bulunur. Sorgu sayısı testi önce kırmızı (1 rol 5, 5 rol 13), sonra eşit (4). 8 izin sınıfı 32/32.
- [x] F9.2h (`669b2d9`; R-143): `/connect` uçları `RequestAborted` geçirir (test önce kırmızı), `await app.RunAsync()` (Api, şablon, README), iki teste doğrulama. 11 sınıf 89/89. CORS bulgusu kullanıcıya kaldı.

- [x] F9.2i (`7d9dcc9`; R-144): event tamponu yalnız `SavedChanges`'te temizlenir, yeniden denemede izlenen outbox satırlarının event'leri atlanır; iki yeni public override. `OutboxInterceptorTests` önce kırmızı (4/14), sonra 14/14.
- [x] F9.2j (`8a891f4`; R-145): çakışmada `GetDatabaseValuesAsync` null ise 404, değilse 409. `StockConcurrencyTests` önce kırmızı (2), sonra 6/6; OpenAPI değişmedi.

- [x] F9.3 (`13375bd`, `dc06cda`, `94b6db9`, `f475897`, `003b218`; Sonnet, Opus incelemesi sürüyor): codeql init/analyze v4.39.3 (#28, #30), Scalar 2.18.7 (#34), OpenIddict 7.8.0 (#32), Microsoft paketleri 10.0.14 / 10.11.0 + şablonun props'u (#31; L18 kapanır), Jaeger 2.22.0 (#2). Kod uyarlaması yok; R-146…R-149. Kilitli restore ve build temiz. #21 kullanıcıda.
- Faz sonu incelemesi (Opus, F9.2'nin davranış commit'leri): kapsam içi hata yok. F9.4'e: `src/Modules/CLAUDE.md:69`'daki 404 yönergesi kiracılı context'te `TenantQueryGuard`'a takılır; sorgu sayısı testine alt sınır. Gerisi Sonraya.

- F9.3'ün Opus incelemesi: temiz (sürümler Dependabot'la aynı, kilitli restore hatasız, codeql SHA'sı `v4.39.3`'ün commit'i, yeni geçişli paket yok); üç eksik → F9.3f, F9.4.
- [x] F9.3f (`38af086`): `dotnet-ef` 10.0.14 (kök ve şablon), bayat servis yorumu. Pkcs: SqlClient 6.1.7 net10'da hâlâ 9.0.11 istiyor, doğrudan referans kalır (Sonraya metni düzeltildi).
- [x] F9.2k (`82c672d`; R-150): `GetDatabaseValuesAsync` kiracılı context'te `TenantQueryGuard`'a takılıyor (test önce kırmızı); yönerge ve Catalog'un iki handler'ı filtreli `AnyAsync` kalıbına geçti; sorgu sayısı testine alt sınır. 3 sınıf 48/48.
- [x] F9.4 (`a623cb1`): CHANGELOG `[Unreleased]` (F9.2g/h, bağımlılıklar), README, `tests/CLAUDE.md` (yazan oturum `*.Architecture`'ı da koşar), kimlik eklentisinin CLAUDE.md'si.

#### Doğrulama (Faz 9)

| ID | Bulgu | Yer | Durum |
|---|---|---|---|
| V9-1 | CI `build-and-test` kırmızı (run `43395986910`): F9.2e'nin eklediği `Assembly.Load`, Y-20'yi koruyan `ExplicitRegistrationTests`'i kırıyor. Yazan yalnız `ObservabilityTests`'i koştu | `OpenTelemetryHostingExtensions.cs` (`7310497`) | doğrulandı (CI `43399963544`'te `build-and-test` yeşil; `83cea0c`): yeni overload `AddCoreObservability(Assembly)`, host `typeof(Program).Assembly` verir; parametresiz olan sürüm raporlamaz. `ExplicitRegistrationTests` önce kırmızı, `*.Architecture` 27/27 (orkestratör de koştu) |

**Arşive taşındı (2026-11-13):** biten L26, L32, L48, L56, L57, L65, L74, L85; geçersiz L17, L21, L34, L36, L43, L46, L71, L88, L108. **Kısmen, listede kalır:** L25 (`GlobalPackageReference`), L63 (anahtar stratejisi), L94 (kullanılmayan `using`'ler), L96 (CORS bulgusu SonarCloud'da). L18 F9.3'le kapandı, arşivde.
**kalır:** koşul gerçekleşmedi L11–L15, L20, L33, L45, L51, L60, L62, L68, L72 (F9.3'ten sonra yeniden bak), L73, L75–L79, L84, L86, L89, L90, L92, L93, L103–L105, L107, L112, L113, L116–L118, L121–L127 · ürün/public API/tasarım kararı L22, L24, L29–L31, L41, L42, L52–L55, L61, L66, L67, L80–L83, L114 · D-031 (3)'ü yeniden açar L64 · korunan dosya L27, L49, L69, L95 · bastırma L23, L28 · ortam/upstream L19, L35, L58, L91, L102, L115, L119, L120 · kuşkulu L47.

### Faz 6e
Başlangıç 2026-11-13, `faz/9-bakim` @ `1f3c034`. Plan onayı D-096 (2). Docker çalışıyor; PLAN'ın adını verdiği Cloudflare `security-audit-skill` ve Claude Security plugin kurulu değil (kullanıcıya kaldı).

- [x] F6e.1 (`2b6df56`): `dast` client yalnız `eng/dast/dast.env`'de (ortam değişkenleri, index 100); `client_credentials`, dört okuma izni, secret koşuda `openssl rand`. Kod değişmedi. `DastClientTests` (5) önce kırmızı, sonra yeşil.
- [x] F6e.2 (`d07f199`): `dast.yml` (haftalık + `workflow_dispatch`), `eng/dast.sh`, `/connect` ve `/account` için dar hedef listesi. Schemathesis 4.33.1, ZAP 2.17.0 (digest'e sabit; D-044: CI aracı, NuGet değil). Bulgu job'u kırmızı yapmaz, taramanın koşamaması yapar (DA-9). Yerelde Schemathesis 3 örnekle: `/account/*` formlarında ve `/connect/revoke`'ta bozuk form girdisine 500 (S-14). ZAP yerelde denenmedi (indirme takıldı). İlk koşu merge'den sonra (kullanıcıya kaldı).
- [x] F6e.4: kurulu `security-review` ölçütleriyle dört Opus koşusu (salt okunur): auth çekirdeği ×2 (uçtan içeri; kontrol listesi/değişmezlerden koda), bütün repo ×2 (dizin dizin; OWASP API Top 10 + tehdit modeli). Skill yalnız diff okuduğu için ölçütleri elle, bütün koda uygulandı. Faz 6b'nin düzeltmeleri yerinde; bağımlılık taraması temiz.

#### F6e.4 bulguları ve F6e.5 triajı

| ID | Bulgu (koşular) | Önem | Triaj |
|---|---|---|---|
| S-1 | Kayıtla hesabın önceden ele geçirilmesi: onaysız adrese ikinci kayıt sessizce yutulur, onay linki ilk kaydedenin şifresiyle açar; 2FA ile kalıcılaşır, davet üyeliği saldırgana gider (A1-1, A2-1, R2-2) | orta | kapatıldı (`6d709a4`; R-155): onaysız hesaba başka şifreyle ikinci kayıt şifreyi siler (stamp yenilenir, eski onay linkleri ölür), posta kutusuna şifre kurma bağlantısı gider. 2 test önce kırmızı. Kalan: kurban hiç kayıt olmadan saldırganın onay linkine tıklarsa → `needs_validation` (onayda şifre istemek ürün kararı) |
| S-2 | Davetle açılmış parolasız hesaba herkes onay e-postası yollatabilir; onaylanırsa davet kullanılamaz (A2-2) | düşük | kapatıldı (`e4d724a`): `resend` ve `confirm-email` şifresiz hesaba dokunmaz. 2 test önce kırmızı |
| S-3 | `MultiTenancy:Roles`'a `identity:*` platform izni yazılabiliyor; belge yasaklıyor, açılışta denetim yok (A1-2) | düşük-orta | testli düzeltme |
| S-4 | Rol atamasında yükseltme koruması rolün adına bakıyor, içerdiği `identity:*` izinlerine değil (R2-5) | orta (OLASI) | testli düzeltme |
| S-5 | Davet denetim kaydında daveti yapan yok (A2-3) | düşük | testli düzeltme |
| S-6 | E-posta gönderen anonim uçlarda alıcı adres başına sınır yok; IPv6 /64 döndürerek posta bombası, kod geçersizleştirme, 1000'lik kuyruğun dolması (R2-1) | orta | testli düzeltme denenir |
| S-7 | Açık kayıt kapatılamıyor; kayıt olan herkes platform `UserPermissions`'ını alır (R2-3) | orta | **kullanıcıya:** ürün kararı (Y-19: seçenek için tüketici gerekir) |
| S-8 | Ödeme oluşturmada idempotency anahtarı yok (R2-4) | düşük | `needs_validation`: API tasarım kararı, referans modül |
| S-9 | Testi olmayan değişmezler: scope yükseltme reddi (#17), başka client'ın token'ını iptal (#14), HTML `ReturnUrl` için `//evil` ve `/\evil`, üretimdeki 30 sn leeway sonrası zincir iptali (A2) | — | test commit'i |
| S-10 | Bağlantıyla sıfırlama token'ı 6 saat ve onay token'ıyla ortak sağlayıcı (#31) | düşük | `needs_validation` |
| S-11 | Outbox handler hata mesajı ileride kişisel veriyi `Error` sütununa ve loga taşıyabilir (D1) | düşük | `needs_validation` |
| S-12 | `includeInactive=true` her ürün okuyucusuna pasif ürünleri gösteriyor; karar kaydı yok (D2) | — | `needs_validation`: ürün kararı |
| S-13 | Şablonun `Program.cs:75`'inde `GetValue<int?>("HealthChecks:ProbePort")` (R-048'e aykırı) | — | düzeltme (güvenlik dışı) |
| S-14 | Bozuk form girdisine 500: `/account/*` formları ve `/connect/revoke` (F6e.2'nin yerel Schemathesis denemesi) | düşük-orta | testli düzeltme |

Doğrulanamayanlar (`needs_validation`): son admin korumasında check-then-act yarışı (biliniyor, L94); CSP'nin `form_post` otomatik gönderimini engelleyip engellemediği (#8); `alg:none`/`jku` reddi (#21); kurumsal NAT arkasında `strict` kovasının `/connect/token` yenilemelerini de kapsaması. Biliniyor (Sonraya): L60, L44, L94, L95, L96, L103, L106, L107, L108, L11.

## Açık sorular (Q)

Yalnız cevabı beklenen sorular. Cevaplanan soru cevabıyla birlikte arşivin "Cevaplanmış sorular" bölümüne taşınır; kalıcı sonucu DECISIONS'a D-kaydı olarak yazılır. Sıradaki ID: **Q-64**. Q-40 sürüyor: izlenmeyen `.agents/`, `.codex/`, `AGENTS.md` dosyalarına dokunulmaz.

| ID | Soru | Ne zaman gerekli | Cevap |
|---|---|---|---|

## Son devir notu

> Yalnız en son not durur. Yeni not yazılırken bir önceki arşivin "Eski devir notları" bölümünün en üstüne taşınır (D-097).

**2026-11-13, faz dışı: gece koşusu kuruldu (D-096), PROGRESS bölündü (D-097), Faz 9 plana eklendi (Q-63).** Branch `faz/8-bassiz-giris`, ilk kez push edildi; CI koşuyor. Kod değişmedi.

- **Sıradaki:** kullanıcı `/novacore-night` ile koşuyu başlatır; kuyruk yukarıdaki "Gece koşusu" bölümünde. `/novacore-verify 8` ayrıca çalıştırılmaz: Faz 8'in doğrulamasını koşu yapar (CI ve yazmamış bir inceleme alt ajanı).
- **Bilinmesi gerekenler:** Faz 8'in testleri önce kırmızı görülmedi (D-094 (3)). `invitations` migration'ı SQL Server ve PostgreSQL'de, şablon smoke'u da F8.7'den beri ilk kez bu CI koşusunda çalışıyor. Bu makinede Docker kapalıydı ve SDK yalnız 10.0.300.
- **Bekleyen soru:** yok. Q-40 sürüyor. Sürüm numarası ve yayın kullanıcının (D-090 (6)).
