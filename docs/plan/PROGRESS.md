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
| 6e | ✅ tamam (F6e.3 D-102 (2); SQL Server/PostgreSQL katmanları Faz 10'dan sonra, tag'den önce) | `faz/6e-dinamik-denetim` (PR #37, `d2be1d1`) + `faz/6e-dast-ilk-kosu` | 2026-11-13 | 2026-11-27 | 2026-11-27 (kullanıcı onayı, PR #49 merge commit'i `83ad2ef`; CI yeşil) |
| 10 | 🔄 devam ediyor | `faz/10-surum-0.2.0` (`main`'den, `21a3c7e`) | 2026-11-28 | | |

Durum değerleri: `⏳ başlamadı` · `🔄 devam ediyor` · `⏸ duraklatıldı` (devir notuna bak) · `✅ tamam`

## Gece koşusu (D-096)

Durum: **koşuyor (2026-11-28), Faz 10.** Önceki koşu (Faz 8, 9, 6e) 2026-11-14'te bitti; PR #35, #36, #37 2026-11-27'de merge edildi. Kullanıcı `/novacore-night` ile başlatır; kurallar `.claude/skills/novacore-night/SKILL.md`'de. Koşu her turda bu bölümü okur ve günceller. Yerel test adım başına ~2 dk ile sınırlı; tam paket (SQL Server, PostgreSQL dahil) merge'den sonra, tag'den önce yerelde (D-106).

### İş kuyruğu

| # | İş | Durum | Not |
|---|---|---|---|
| 1 | Faz 10: F10.1 branch, F10.2 inceleme yorumları, F10.3 ürün kararları, F10.4 Dependabot, F10.5 sürüm hazırlığı | ✅ | Adımlar PLAN'da; tag ve yayın yapılmaz (D-102 (1)) |
| 2 | Faz 10: faz sonu incelemesi, PR, merge (D-106 (3)) | 🔄 | |
| 3 | Sabah raporu ve devir notu | ⏳ | Tam paketin Docker katmanları ve `v0.2.0` tag'i kullanıcıya (D-106 (4)) |

### Karar adayları (DA)

DA-1…DA-12 2026-11-27'de DECISIONS'a işlendi: D-099 (DA-1…6), D-100 (DA-7, 8), D-101 (DA-9…12). Tablo arşivde.

| DA | Karar | Gerekçe |
|---|---|---|
| DA-13 | S-10: onay ve sıfırlama linklerinin ortak `TokenLifespan`'ı 6 saat kalır; yalnız yorum düzeltildi | NIST SP 800-63B-4 e-postalanan kurtarma sırrına 24 saate kadar izin verir (R-137); kısa yol 15 dakikalık koddur. `306b6f7` |
| DA-14 | S-11: handler'dan atılan exception kişisel veri değil id taşır (mesaj `OutboxMessage.Error`'a ve loga gider); kod değişmez | Bugünkü handler'lar kişisel veri taşımıyor; yalnız tip adını saklamak sütunun teşhis değerini öldürür. `306b6f7` |
| DA-15 | F10.5: package validation baseline (`PackageValidationBaselineVersion`) `Directory.Build.props`'ta değil, yalnız pack eden iş akışlarında `-p:` ile (restore ve pack adımı), `packages: read` ve `GITHUB_TOKEN` yalnız orada; kök `nuget.config` source mapping'le | SDK baseline'ı her restore'da `PackageDownload` olarak ekliyor (`Microsoft.NET.ApiCompat.targets:18-24`): props'ta olsaydı her geliştirici restore'u ve her CI job'u GitHub Packages token'ı isterdi (D-050). |


### Kullanıcıya kalanlar

Ön onaylı olmayan ya da ortamın engellediği işler (D-096 (5)). Koşu bunlarda durmaz, buraya yazıp sıradaki işe geçer.

| İş | Neden | Ne gerekli |
|---|---|---|
| Tam test paketinin SQL Server ve PostgreSQL katmanları (Faz 6e doğrulaması, 98 test) | Kullanıcı kararı (2026-11-27): 6e'de koşmaz | Faz 10 bittikten sonra, 0.2.0 tag'inden önce Docker açıkken `dotnet test --solution NovaCore.slnx` |
| CodeRabbit CLI incelemesi (Faz 8) | "session could not be authenticated" | `coderabbit auth login`, sonra `coderabbit review --agent --base main` |
| SonarCloud: `dast.yml:43` S2068 ("password") | V6e-1'in false positive'i (değer `openssl rand` ifadesi); kalite kapısını kırmızı yapıyor | Kullanıcı SonarCloud'da "Safe/False positive" işaretler (L96 ile birlikte) |
| Dependabot #21 (`global.json` → SDK 10.0.403) | Bu makinede yalnız 10.0.300 kurulu (F9.3 kuralı) | SDK 10.0.403 kurulur, PR merge edilir |
| CodeRabbit PR #35 …609, …617, …626: `novacore-night/SKILL.md` §1.4 (PROGRESS düzenlemesini orkestratör commit'ler), §3.2 stash `-u`, §4.5 birleşmemiş fazdan açılan branch | Koşu `.claude/` altına yazmaz | Kullanıcı skill'i düzenler ya da onaylar |
| CodeRabbit PR #35 …648: DECISIONS D-094 3 hücre, D-095 7 hücre (D-094'ün gerekçe ve onay hücreleri D-095'te) | DECISIONS koşuda yazılmaz (D-096 (5)) | Kullanıcı hücreleri taşır |
| Dependabot #48, #42, #41, #30, #28 | Güncellemeleri branch'te yapıldı; Dependabot `main`'e merge'den sonra kendisi kapatmazsa | Merge'den sonra açık kalanları kapatmak |
| SonarCloud'un CORS bulgusunu "false positive" işaretlemek (L96) | Dış işlem; `AllowAnyOrigin` yalnız Development'ta (`CoreWebExtensions.cs:182`) | Kullanıcı SonarCloud'da işaretler |

## Adımlar

Biçim: `- [x] F0.3 (abc1234): not`. Not en çok iki satırdır: ne yapıldı, koşulan test sınıfları, doğrulamaya kalan. Gerekçe D/R kaydına, ayrıntı commit mesajına yazılır.

Biten fazların adımları: [PROGRESS-ARSIV.md](PROGRESS-ARSIV.md#adımlar).

### Faz 10

- [x] F10.1: 6e'nin merge kaydı doğrulandı (PR #49, `83ad2ef`). Branch'ten önce `main`'de yeşil Dependabot PR'ları merge edildi: #47, #40, #45, #44 (`21a3c7e`); #48 csproj'lara doğrudan referans eklediği için F10.4'e kaldı.

- [x] F10.4a (767323a), F10.4b (879741c), F10.4d (89c462f): setup-dotnet 6.1.0, codeql-action 4.39.4 (scorecard dahil), postgres 18.6 (#42, #28, #30, #41'in yerine); dependabot `dotnet-sdk` major yok sayılır, Api pack uyarısı kalktı. #39 kapatıldı. `WorkflowPinTests`, `DatabaseImageTests`; Opus incelemesi temiz. CI yeşil (`89c462f`).
- [x] F10.4c (445f9ff): #48'in sürümleri birebir (Microsoft.* 10.0.15, 10.11.1, `dotnet-ef`), csproj eklemeleri olmadan; şablonun props'u ve aracı; lock dosyaları yeniden üretildi. `PackageDependencyTests` 7/7; Opus incelemesi temiz (F10.2f dahil).
- F10.2 sınıflandırması (PR #35'te 12 satır içi + 1 review gövdesi; PR #37'de CodeRabbit yorumu yok, tek yorum Sonar S2068):
  - …688, …701, …715, gövde l.140-143 (`PasswordResetCodes`): düzeltildi, F10.2a `dc9241d`, F10.2b `a26231a`, F10.2c `f0e7288`, F10.2d `65ab787` (`PasswordCodeResetTests` kırmızı → yeşil 21/21).
  - …676 (`AccountInvitations` süre dolumu yarışı): düzeltildi, F10.2e `b4d715b` (`InvitationTests` kırmızı → yeşil 25/25; test host'u `DateTimeOffset`'i UTC tick olarak saklıyor). …638 (CLAUDE.md PostgreSQL migration komutu), …669 (README doğrulama anahtarları): düzeltildi, F10.2f `a508a09`.
  - …656: zaten düzeltildi (`0e4d755`). …725 (`HttpResponseMessage` dispose): kalır, ~98 yerde aynı kalıp, CA2000 yok. Sonar 4719236607: kalır, false positive (V6e-1).
  - …609, …617, …626 (`novacore-night/SKILL.md`), …648 (DECISIONS D-094/D-095 hücre kayması): kullanıcıya kalır (`.claude/` ve DECISIONS koşuda yazılmaz).

- [x] F10.3a (6fd1909): `Identity:SignIn:AllowRegistration` (varsayılan `true`); `false` iken `POST /auth/register` eşlenmez (anonim istek fallback policy yüzünden 401 alır, test route'un yokluğunu doğrular). HTML kayıt sayfası yok. Şablon appsettings, README. `RegistrationTests` kırmızı → yeşil, `OpenApiContractTests` değişmedi.
- [x] F10.3c (306b6f7): README'ye S-1'in kalan yolu (D-102 (4)), V6e-4 (D-102 (6)), S-8 (D-102 (7)); S-10 yorumu, S-11 kuralı (`src/Modules/CLAUDE.md`). Karar adayları DA-13, DA-14.
- [x] F10.3b (0ed3fb9): `GetProducts` pasifleri yalnız `includeInactive` ve `catalog:products:update` ile gösterir (`ICurrentUser.HasPermission`; izinsiz bayrak sessizce aktiflere düşer, D-102 (8)). `CatalogEndpointsTests` kırmızı → yeşil 5/5, OpenAPI değişmedi.

- [x] F10.5a (7df5a92), F10.5b (9008429), F10.5c (0798604): kök `nuget.config` (source mapping), ci.yml `package-validation` job'u ve release.yml baseline 0.1.0 ile pack (DA-15, R-162); PublicAPI Unshipped → Shipped (EFCore 2, OpenTelemetry 1, Identity 99); `VersionPrefix` 0.2.0, CHANGELOG `[0.2.0] - 2026-11-28`. Yerelde baseline'sız pack uyarısız; baseline'lı kabul CI'ın `package-validation` job'unda.

#### Doğrulama

| ID | Bulgu | Durum |
|---|---|---|
| V10-1 | `nuget.config`'de `auditSources` yok: NuGet Audit GitHub kaynağını da sorgular, kimliksiz restore 401 → NU1900 → hata (code-review + CodeRabbit CLI) | açık |
| V10-2 | `AccountInvitations` koşullu güncelleme 0 satırda `SingleAsync` ile yeniden okuyor; arada silinen davet 500 döner | açık |
| V10-3 | `PasswordResetCodes.IssueAsync` her başarısız token yazımında null dönüyor, `/forgot` 202 ile log/audit'siz susuyor; doc yalnız yarıştan söz ediyor | açık |
| V10-4 | `src/BuildingBlocks/CLAUDE.md:141` Shipped'in 0.1.0'ı tuttuğunu söylüyor (artık 0.2.0) | açık |
| V10-5 | README ~503: handler'ın ürettiği `errors.newPassword` (camelCase) ile otomatik doğrulamanın PascalCase anahtarları ayrı belgelenmeli (CodeRabbit CLI) | açık |
| V10-6 | CodeRabbit CLI sorusu: `PasswordResetCodes` ~167 telafisinin `expected` koşulu, arada başarılı giriş sayacı silip yeni yanlış deneme 1 yaptıysa o denemeyi silebilir mi | açık |

## Açık sorular (Q)

Yalnız cevabı beklenen sorular. Cevaplanan soru cevabıyla birlikte arşivin "Cevaplanmış sorular" bölümüne taşınır; kalıcı sonucu DECISIONS'a D-kaydı olarak yazılır. Sıradaki ID: **Q-64**. Q-40 sürüyor: izlenmeyen `.agents/`, `.codex/`, `AGENTS.md` dosyalarına dokunulmaz.

| ID | Soru | Ne zaman gerekli | Cevap |
|---|---|---|---|

## Son devir notu

> Yalnız en son not durur. Yeni not yazılırken bir önceki arşivin "Eski devir notları" bölümünün en üstüne taşınır (D-097).

**2026-11-27, Faz 6e kapandı ve merge edildi (PR #49, `83ad2ef`, kullanıcı onayı, CI yeşil).** Adımları ve doğrulaması arşivin "Adımlar" bölümünde. DAST'ın ilk koşusundan çıkan her şey kapandı: V6e-5/V6e-6 (tarayıcının koşabilmesi), S-15…S-20 (OpenAPI belgesinin 401/403'ü, doğrulama kısıtları, şema adları, CSP `base-uri`), son doğrulamanın V6e-7/V6e-8'i (`ed41255`). Kararlar D-101, D-102, D-103, D-105; araştırma R-155…R-161.

- **Yapılmayan:** F6e.3 (OpenID conformance) D-102 (2) gereği bir sonraki sürümden önce. Tam paketin SQL Server ve PostgreSQL katmanları (98 test) kullanıcı kararıyla Faz 10'dan sonra, 0.2.0 tag'inden önce. CodeRabbit CLI oturum açamadı (`coderabbit auth login`).
- **Sürprizler:** DAST her koşuda bir katman açtı (izin, sertifika, sonra gerçek bulgular); .NET 10'da ikinci `Produces*` birinciyi siler (D-103 (2)); sunucu sayfa parametrelerini kırpıyor (D-105 (2)).
- **Faz 10'un bilmesi gerekenler:** request record'larının attribute'ları `[property: ...]` (D-103 (1), şablon dahil). Faz 10 6e'nin merge'ünden sonra `main`'den açılır; Docker baştan açık olsun. Bu fazın uzamasının nedeni küçük bulguların da yazan/doğrulayan turu gerektirmesiydi; süreç değişikliği (küçük bulguyu Sonraya yazmak ya da doğrulamada düzeltmek) kullanıcının kararıdır.
- **Bekleyen:** PR'ın CI'ı (`check-openapi`, coverage, SQL Server katmanı CI'da), merge onayı. Q-40 sürüyor.
