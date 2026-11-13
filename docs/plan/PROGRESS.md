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
| 9 | 🔄 devam ediyor | `faz/9-bakim` (Faz 8 merge edilemediği için `faz/8-bassiz-giris`'in üstüne açıldı, `main` içine alındı: `af4e7da`) | 2026-11-13 | | |
| 6e | ⏳ başlamadı | | | | |

Durum değerleri: `⏳ başlamadı` · `🔄 devam ediyor` · `⏸ duraklatıldı` (devir notuna bak) · `✅ tamam`

## Gece koşusu (D-096)

Durum: **sürüyor (2026-11-13).** Bir kez kota sınırına takıldı, kaldığı yerden sürdü. Kullanıcı `/novacore-night` ile başlatır; kurallar `.claude/skills/novacore-night/SKILL.md`'de. Koşu her turda bu bölümü okur ve günceller.

### İş kuyruğu

| # | İş | Durum | Not |
|---|---|---|---|
| 1 | Faz 8: CI sonucu, inceleme, bulgular, PR, merge | ⏸ PR #35 | İki inceleme, on bulgu, hepsi kapalı. `sonar` dışında bütün job'lar yeşil; son push (`6e7fdb6`) run `43392510435`, sonucu Faz 9'un ilk adımında okunur. Merge koşulu `sonar` yüzünden tutmuyor |
| 2 | Faz 9: F9.1 sınıflandırma, F9.2 maddeler, F9.3 Dependabot, F9.4 belgeler, PR, merge | 🔄 | Dependabot #26, #27, #29, #33, #19 merge edildi; F9.1, F9.2a–c, F9.2e, F9.2f bitti; sıradaki F9.2a2 + F9.2d, sonra F9.2g–j |
| 3 | Faz 6e: F6e.1 → F6e.5, PR, merge | ⏳ | F6e.3 yerel Docker ister; F6e.4'ün adını verdiği iki araç kurulu değil |
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

### Kullanıcıya kalanlar

Ön onaylı olmayan ya da ortamın engellediği işler (D-096 (5)). Koşu bunlarda durmaz, buraya yazıp sıradaki işe geçer.

| İş | Neden | Ne gerekli |
|---|---|---|
| `sonar` job'u | "Authentication with the server has failed"; `main`'de de 2026-11-09'dan beri kırmızı (run `42651867223`), kodla ilgisi yok | Kullanıcı `SONAR_TOKEN` secret'ını yeniler. Yenilenene kadar "bütün kontroller yeşil" koşulu tutmaz: PR'lar merge edilmez, fazlar üst üste açılır (D-096 (4)) |
| CodeRabbit CLI incelemesi (Faz 8) | "session could not be authenticated" | `coderabbit auth login`, sonra `coderabbit review --agent --base main` |
| Dependabot #21 (`global.json` → SDK 10.0.403) | Bu makinede yalnız 10.0.300 kurulu (F9.3 kuralı) | SDK 10.0.403 kurulur, PR merge edilir |
| SonarCloud'un CORS bulgusunu "false positive" işaretlemek (L96) | Dış işlem; `AllowAnyOrigin` yalnız Development'ta (`CoreWebExtensions.cs:182`) | Kullanıcı SonarCloud'da işaretler |

## Adımlar

Biçim: `- [x] F0.3 (abc1234): not`. Not en çok iki satırdır: ne yapıldı, koşulan test sınıfları, doğrulamaya kalan. Gerekçe D/R kaydına, ayrıntı commit mesajına yazılır.

Biten fazların adımları: [PROGRESS-ARSIV.md](PROGRESS-ARSIV.md#adımlar).

### Faz 8
Başlangıç 2026-11-09, `main` @ `f319d57`. Plan onaylandı (plan modu). Doğrulama yalnız faz sonunda (D-093, kullanıcı, 2026-11-09). Kullanıcı isteği: kod adımları Codex'e (`codex:codex-rescue`, effort high) dağıtılır; diff incelemesi, build, adımın testleri, format, commit ve PROGRESS yazan oturumda (Claude). F8.1'in araştırması ve kararları yazan oturumda (MS Learn MCP, D-079). Q-40 sürüyor. Kararlar D-091'den, araştırmalar R-137'den başlayarak yazılır.

**Bu fazda yapılmayacaklar:** ROPC ya da şifre karşılığında tarayıcıya token; `/account`'a ya da yeni uçlara CORS; paketten SPA sunmak; CRM'in React ekranları ve metinleri; kilitli hesabı ayrı kodla açığa çıkarmak; BFF paketi; HTML sayfalarının tasarımı/yerelleştirilmesi; sürüm numarası ve yayın (D-090 (6)); `MapIdentityApi`; IETF First-Party Apps taslağı.

- [x] F8.1–F8.8 (`1aaf0f9` … `163c4af`): adım notları ve oturum sonu koşusu arşivde ([PROGRESS-ARSIV.md](PROGRESS-ARSIV.md), "### Faz 8"; D-097 (2)).

#### Doğrulama (gece koşusu, 2026-11-13)

İnceleme: iki Opus okuyan ajanı (`main...HEAD` kod incelemesi, elle: `code-review` skill'i kotaya takıldı; PLAN/DECISIONS denetimi). CodeRabbit CLI koşmadı (oturum açık değil, D-067 (3)). Kapsam ve Yapılmayacaklar temiz.

| ID | Bulgu | Yer | Durum |
|---|---|---|---|
| V8-1 | CI `secret-scan`: 3 bulgu, `main..HEAD` aralığında | run `43310617390` | doğrulandı (`5a7a956`; CI `43331827821`'de `secret-scan` yeşil): iki test parolası, `tests/` altında birebir allowlist |
| V8-2 | Yanlış kod sayacının `IdentityResult`'ı yok sayılıyor: eşzamanlı yanlış tahminler sayacı düşürür, 5/100 sınırı aşılır | `PasswordResetCodes.cs:107-112` | doğrulandı (`1777b77`; CI `43334884102`): deneme karşılaştırmadan önce stamp ile sayılır, sayımı kaybeden istek kodu siler. Test önce kırmızı, sonra yeşil |
| V8-3 | forgot/reset'te süreyle hesap sayımı: bilinmeyen/onaysız adres hash ve yazma olmadan döner (D-030 (1), D-091 (4)) | `PasswordCodeEndpoints.cs:190-218` | doğrulandı (`518b339`; CI `43334884102`): kodu olmayan adres aynı hash işini yapar. Hash sayısı testi önce kırmızı, sonra yeşil. Kalan fark: birer veritabanı yazması |
| V8-4 | Kilitli ve onaysız hesap hash hesaplamadan döner; D-091 (1) "aynı süre". Test kilitli hesabın hash sayısını sınamıyor | `SignInFlow.cs:51,116` | doğrulandı (`067821e`; CI `43336677900`; R-138): kilitli hesap şifreyi sahte hash'e karşı sınar. Test önce kırmızı (`[1,1,0,1,0]`), sonra yeşil |
| V8-5 | Davet oluşturmada beklenen hata (`InvalidUserName`, `DuplicateEmail`) exception → 500; `ProducesProblem` yok | `AccountInvitations.cs:156`, `InvitationEndpoints.cs:240,269` | doğrulandı (CI `43390653707`; `8fc5500`): `InviteAsync` `Result<Guid>` döner; `invitation.invalid_email` 400, `invitation.address_taken` 409, `ProducesProblem`, OpenAPI yalnız ekleme. Kullanıcı deposu kaybedilen insert'i `DuplicateUserName` bildirir (kayıt yarışındaki 500 de gider). Üç test önce kırmızı (500), sonra yeşil |
| V8-6 | Davet/kod "kullanıldı" işaretlendikten sonra şifre adımı düşerse yanar; yorum "kullanılmamış kalır" diyor | `AccountInvitations.cs:131-142`, `PasswordResetCodes.cs:84-91` | doğrulandı (CI `43390653707`; `0aeeaea`): işaret (ya da kodun silinmesi) ve şifre tek transaction'da, execution strategy içinde. Test önce kırmızı (yeniden deneme 409 `invitation.used` / 400), sonra yeşil |
| V8-7 | Kuralı yakalamayan/eksik testler: onaysız hesabın reset'i, kabul etmeden giriş (totolojik), davetin tek kullanım yarışı, `InvitationPath` doğrulaması, `/password` ve `/invitations` CORS'u (D-095 (6)), JSON 2FA'da kurtarma kodu, `A_new_code_replaces_the_old_one`'daki `if` | `PasswordCodeResetTests`, `InvitationTests`, `SessionSignInTests` | doğrulandı (CI `43390653707`; `b9c6183`, `fe1f24f`, `0b44d78`): şifre kodu, JSON 2FA kurtarma kodu, gerçek 429 ve beş davet testi (kabul yarışı, `InvitationPath`, CORS…). Yeni testler ilk koşuda yeşil; iki kural geçici bozulup kırmızı görüldü |
| V8-8 | CI `secret-scan` yeniden kırmızı: 3 bulgu, bu gecenin test commit'lerindeki yeni test parolaları | run `43340697870` | doğrulandı (CI `43390653707`; `b317c0b`): iki test parolası allowlist'e; `tests/CLAUDE.md`'ye "allowlist'teki değeri kullan" kuralı; yeniden tarama temiz |
| V8-9 | Hesap başına 100'lük seri sayacı hâlâ aşılabiliyor: seri karşılaştırmadan SONRA ayrı stamp'lı kayıtla yazılıyor, kaybedilince tahmin sayılmıyor (eşzamanlı `/forgot` ile hiçbiri sayılmaz). Kod başına 5 sınırı sağlam | `PasswordResetCodes.cs:110-113,169-175` | kapatıldı (`a619a94`): deneme ve seri iki token, tek kayıtla karşılaştırmadan önce sayılır; doğru kod+reddedilen şifre ikisini geri verir. İki test önce kırmızı (karşılaştırma 2/seri 1; `/forgot` yarışında 1/0), sonra yeşil |
| V8-10 | Yorum yanlış: "a retrying strategy retries all of it"; ilk `SaveChanges`'ten sonra stamp izleyicide yeni kaldığı için yeniden deneme kesin `ConcurrencyFailure` alır (güvenli tarafta). Keyed hasher'ın "singleton olduğu için aynı maliyet" gerekçesi de geçersiz | `PasswordResetCodes.cs:132`, `AccountInvitations.cs:147`, `IdentityServerHostingExtensions.cs:60` | kapatıldı (`6eecf96`; R-139): yalnız yorumlar; yeniden deneme hep `ConcurrencyFailure` alır (güvenli taraf). Keyed hasher kaldı: host'un hasher'ı 600k iterasyon, D-091 (4) ile çelişir. Gerisi Sonraya |

İkinci inceleme (2026-11-13, Opus, `c7bddf0..0b44d78`): V8-9, V8-10; V8-3…V8-6 ve yarış yardımcıları temiz. Yarış testleri yalnız SQLite'ta koşuyor.

Yanlış pozitif: kök CLAUDE.md'deki başsız giriş satırının `f6185ab` ile gitmesi (kullanıcının bilinçli küçültmesi; bilgi kimlik eklentisinin CLAUDE.md'sinde).

### Faz 9
Başlangıç 2026-11-13, `faz/8-bassiz-giris` @ `6e7fdb6` + `main` @ `aeb9f38`. Plan onayı D-096 (2). Dependabot, `main`'de: #26, #27, #29, #33, #19 merge edildi (kontroller yeşil, çakışmasız); #21 kullanıcıya kaldı; #2, #28, #30, #31, #32, #34 kırmızı → F9.3.

- [x] F9.1: 98 madde, üç Sonnet sınıflandırıcı + Opus denetimi (`yapılacak`ların hepsi, diğerlerinden 17). Satır numaraları `SONRAYA-BIRAKILANLAR.md` @ `74d511d`.

| Alt adım | Satır: madde | Model | Kırmızı test |
|---|---|---|---|
| F9.2a | L25 `PackageDependencyTests` boşlukları (bin/obj, `Update`'li referans); L26 tekrarlı kaynak tarama kodu | Sonnet | hayır |
| F9.2b | L48 fazla `EnsureCreatedAsync` çağrıları (silinir); L94 Qodana cref ve `!` temizliği | Sonnet | hayır |
| F9.2c | L63 `src/Modules/CLAUDE.md`'de Guid v7 "SQL Server clustering" cümlesi | Sonnet | hayır |
| F9.2d | L74 `MessageResponse` üst düzeye | Sonnet | hayır |
| F9.2e | L32 OTel `service.version` host assembly'sinden | Opus | evet |
| F9.2f | L56 seed client secret'ı her açılışta yeniden hash'liyor | Opus | evet |
| F9.2g | L57 `IdentityPermissionResolver` rol başına N+1 | Opus | sorgu sayısı |
| F9.2h | L96 SonarCloud: `RequestAborted`, iki assertion'sız test, `RunAsync` | Opus | iptal için evet |
| F9.2i | L65 `OutboxInterceptor` tamponu başarısız kayıtta | Opus | evet |
| F9.2j | L85 eşzamanlı silinmiş satırda 409 yerine 404 | Opus | evet |

- [x] F9.2a (`ef2cde2`, Sonnet): kaynak taramaları `Solution.SourceFiles`'tan (bin/obj dışlı); `Update`'li referansta NRE testi önce kırmızı. `*.Architecture` 26/26. Opus incelemesi: testin paket satırı `PrivateAssets` yüzünden bir şey sınamıyor, `Directory.Build.*` okuması testsiz → F9.2a2. L25'in `GlobalPackageReference` kısmı yapılmadı (listede kalır).
- [x] F9.2b (`90d0fde`, Sonnet; Opus incelemesi temiz): dört fazla `EnsureCreatedAsync` silindi, iki cref, `LoginPage`'de `!`, `ProductCatalog.cs` `Internal/` klasörüne. 6 sınıf 60/60. L94'ün kullanılmayan `using` kısmı doğrulanamadı (listede kalır).
- [x] F9.2c (`bc5f29f`, Sonnet; Opus incelemesi temiz): `src/Modules/CLAUDE.md`'de Guid v7 cümlesi sağlayıcıya göre; R-140. Anahtar stratejisi listede kalır.
- [x] F9.2e (`7310497`): `service.version` `ApplicationName`'in gösterdiği assembly'den (R-141). `ObservabilityTests` önce kırmızı (`0.1.0.0`), sonra 4/4.
- [x] F9.2f (`bcfd95e`): seed, secret değişmediyse hash'i korur ve hiçbir alan değişmediyse yazmaz (R-142; DA-7). `ClientSeedTests` önce kırmızı (2/3), sonra 3/3; `ClientCredentialsTests`, `IdentityServerConfigurationTests` yeşil.

**geçersiz** (arşive): L17, L21, L34, L36, L43, L46, L71, L88, L108; L18 F9.3 bitince (PR #31 kapsıyor).
**kalır:** koşul gerçekleşmedi L11–L15, L20, L33, L45, L51, L60, L62, L68, L72 (F9.3'ten sonra yeniden bak), L73, L75–L79, L84, L86, L89, L90, L92, L93, L103–L105, L107, L112, L113, L116–L118, L121–L127 · ürün/public API/tasarım kararı L22, L24, L29–L31, L41, L42, L52–L55, L61, L66, L67, L80–L83, L114 · D-031 (3)'ü yeniden açar L64 · korunan dosya L27, L49, L69, L95 · bastırma L23, L28 · ortam/upstream L19, L35, L58, L91, L102, L115, L119, L120 · kuşkulu L47.

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
