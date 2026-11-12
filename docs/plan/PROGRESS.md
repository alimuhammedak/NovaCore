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
| 8 | ⏸ duraklatıldı (doğrulamayı gece koşusu yapar, D-096) | `faz/8-bassiz-giris` | 2026-11-09 | | |
| 9 | ⏳ başlamadı | | | | |
| 6e | ⏳ başlamadı | | | | |

Durum değerleri: `⏳ başlamadı` · `🔄 devam ediyor` · `⏸ duraklatıldı` (devir notuna bak) · `✅ tamam`

## Gece koşusu (D-096)

Durum: **hazır, başlamadı.** Kullanıcı `/novacore-night` ile başlatır; kurallar `.claude/skills/novacore-night/SKILL.md`'de. Koşu her turda bu bölümü okur ve günceller.

### İş kuyruğu

| # | İş | Durum | Not |
|---|---|---|---|
| 1 | Faz 8: CI sonucu, inceleme, bulgular, PR, merge | ⏳ | Branch 2026-11-13'te push edildi; ilk CI koşusu `43310617390` |
| 2 | Faz 9: F9.1 sınıflandırma, F9.2 maddeler, F9.3 Dependabot, F9.4 belgeler, PR, merge | ⏳ | |
| 3 | Faz 6e: F6e.1 → F6e.5, PR, merge | ⏳ | F6e.3 yerel Docker ister; F6e.4'ün adını verdiği iki araç kurulu değil |
| 4 | Sabah raporu ve devir notu | ⏳ | |

### Karar adayları (DA)

Gece alınan kalıcı kararlar. Sabah tek düzenlemeyle DECISIONS'a işlenir (D-096 (6)).

| ID | Karar | Gerekçe / kaynak |
|---|---|---|

### Kullanıcıya kalanlar

Ön onaylı olmayan ya da ortamın engellediği işler (D-096 (5)). Koşu bunlarda durmaz, buraya yazıp sıradaki işe geçer.

| İş | Neden | Ne gerekli |
|---|---|---|

## Adımlar

Biçim: `- [x] F0.3 (abc1234): not`. Not en çok iki satırdır: ne yapıldı, koşulan test sınıfları, doğrulamaya kalan. Gerekçe D/R kaydına, ayrıntı commit mesajına yazılır.

Biten fazların adımları: [PROGRESS-ARSIV.md](PROGRESS-ARSIV.md#adımlar).

### Faz 8
Başlangıç 2026-11-09, `main` @ `f319d57`. Plan onaylandı (plan modu). Doğrulama yalnız faz sonunda (D-093, kullanıcı, 2026-11-09). Kullanıcı isteği: kod adımları Codex'e (`codex:codex-rescue`, effort high) dağıtılır; diff incelemesi, build, adımın testleri, format, commit ve PROGRESS yazan oturumda (Claude). F8.1'in araştırması ve kararları yazan oturumda (MS Learn MCP, D-079). Q-40 sürüyor. Kararlar D-091'den, araştırmalar R-137'den başlayarak yazılır.

**Bu fazda yapılmayacaklar:** ROPC ya da şifre karşılığında tarayıcıya token; `/account`'a ya da yeni uçlara CORS; paketten SPA sunmak; CRM'in React ekranları ve metinleri; kilitli hesabı ayrı kodla açığa çıkarmak; BFF paketi; HTML sayfalarının tasarımı/yerelleştirilmesi; sürüm numarası ve yayın (D-090 (6)); `MapIdentityApi`; IETF First-Party Apps taslağı.

- [x] F8.1 (1aaf0f9): Tasarım kararları D-091, araştırma R-137 (MS Learn antiforgery ve Identity token sağlayıcıları, OWASP Forgot Password, NIST SP 800-63B-4 §3.1.3/§4.2 — NIST bölümleri araçtan özet olarak geldi, değerler R-137'de işaretli —, openiddict-samples Velusia). Kod yok; build/test gerekmedi. Ön karardan sapma: yok; davetin platform ucu ve ürünün kendi ucu ayrıldı (D-091 (5)).
- [x] F8.2 (8df7b64): Giriş kuralları `SignInFlow`'da (internal, scoped; D-092), `LoginPage` yalnız form ve HTML. Codex iki kez denendi, ikisinde de model reddedildi (`gpt-6.1-sol`, `gpt-6.1`: "not supported when using Codex with a ChatGPT account"), kod yazmadı; kullanıcı kararıyla adımı yazan oturum yazdı. Kimlik eklentisine `InternalsVisibleTo NovaCore.Api.Tests` (depoda ilk). Yeni `SignInFlowTests` (6) önce kırmızı (servis kayıtlı değil), sonra yeşil; `SignInHardeningTests`, `TwoFactorTests`, `AuthorizationCodeFlowTests`, `TwoFactorRaceTests`, `PasswordResetTests`, `SessionTests` değişmeden yeşil: 87/87. Build 0 uyarı, format temiz. Doğrulamaya kalan: tam paket, SQL Server/PostgreSQL `SignInScenarios`.
- [x] F8.3 (b7107ee): JSON giriş uçları `/api/v1/auth/session` (`GET antiforgery`, `POST login`, `POST login/2fa`; `SessionEndpoints`): `SignInFlow`'u çağırır, grup `strict` + `DisableCors` + `AllowAnonymous`, OpenAPI'de (yalnız ekleme, `docs/openapi/v1.json`). Antiforgery `HeaderName = X-XSRF-TOKEN` (HTML formları değişmeden yeşil). İstekler public record (`SessionSignInRequest`, `SessionCodeRequest`, PublicAPI.Unshipped), `returnUrl` için internal `[LocalUrl]` (`RedirectHttpResult.IsLocalUrl`) → 400 `ValidationProblem`. D-091 (1)'e ek: antiforgery'siz istek 400 ProblemDetails `account.antiforgery_invalid` (D-091 yalnız "400" diyordu). Yeni `SessionSignInTests` (16) önce kırmızı (16/16), sonra yeşil: üç hesap durumu aynı gövde/durum, hash sayısı eşit (süre), onaysız hesap, antiforgery'siz istek, üç yerel olmayan `returnUrl`, başarıdan sonra `/connect/authorize` kod verir ve yanıtta token yok, 2FA kodu (yanlış/doğru), oturumsuz kod adımı, `strict`, CORS preflight. `ModuleRegistrationTests`, `ValidationTests` (+2 satır), `OpenApiContractTests` güncellendi; HTML/2FA/sıfırlama/oturum sınıflarıyla 102/102 yeşil. Build 0 uyarı, format temiz. Doğrulamaya kalan: tam paket, SQL Server/PostgreSQL katmanları, `eng/check-openapi.sh`.
- **Oturum kuralı değişti (D-094, kullanıcı, 2026-11-09):** F8.4–F8.8 tek oturumda; testler adımla yazılıp commit'lendi, sonda bir kez koşuldu ("önce kırmızı" görülmedi).
- [x] F8.4 (3d59f79): `Identity:SignIn:LoginPath` (validated options, `IdentitySignInOptions.IsLocalPath`: tek `/`, şema/host/sorgu/parça yok) çerezin `LoginPath`'ini kurar; boşsa `/account/login`. Yeni `LoginPathTests` (10): ayarlı yol + `ReturnUrl`, ekran → oturum ucu → `/connect/authorize` kod verir, varsayılan yol, 7 geçersiz yol host'u açmaz.
- [x] F8.5 (7e74100): `PasswordResetCodes` (internal, scoped) + `/api/v1/auth/password/{forgot,reset}` (strict, anonim, CORS kapalı, OpenAPI'de). Kurallar D-091 (4) ve D-095 (5); `SignInFlow` ve bağlantılı sıfırlama hesap sayacını siler. Yeni `PasswordCodeResetTests` (8): aynı 202 ve yalnız onaylı hesaba e-posta, tek kullanım + oturumların kapanması + giriş yok, 5 yanlıştan sonra doğru kod geçmez, yanlış kod kilitlemez, yeni kod eskisinin yerini alır, 100'lük seri girişle açılır, bilinmeyen/onaysız hesap aynı cevap, süresi dolan kod (`FakeTimeProvider`, kendi host'u). `ModuleRegistrationTests`, `ValidationTests` (+2), `docs/openapi/v1.json` (yalnız ekleme).
- [x] F8.6 (295d1f7, f4c5f3c): `AccountInvitations` (public, internal yapıcı) + `invitations` tablosu (SQL Server migration'ı Api'de, PostgreSQL'inki testlerde) + `/api/v1/auth/invitations/{status,accept}` ve `POST /api/v1/admin/invitations` (`identity:invitations:create`). D-095 (1)–(4), (6), (7): durum POST gövdesiyle (token yolda/logda değil), bağlantı `Identity:SignIn:InvitationPath`'e, 410 uçta kurulur. Yeni `InvitationTests` (6): bir kez kabul + şifre kurulur + giriş, kullanılmış 409, bilinmeyen 404, izinsiz 403/anonim 401, onaylı hesap ele geçirilemez (409, eski şifre geçer), kabul etmeden giriş yok, süresi dolan 410 (kendi host'u). `ModuleRegistrationTests`, `ValidationTests` (+3), OpenAPI (yalnız ekleme). Sondaki koşuda `AdminTests`'in Administrator izin listesi yeni izni içermiyordu → güncellendi (f4c5f3c). `PostgreSqlModelTests` yeşil, SQL Server `has-pending-model-changes` temiz.
- [x] F8.7 (ac3005a): şablonun BuiltIn kimlik setlerine `Invitations` migration'ı (paketlenmiş şablondan `MyService` projeleri, iki sağlayıcı; `has-pending-model-changes` ikisinde temiz), `appsettings.json`'a `Identity:SignIn` (yorumlu). **Smoke (sekiz bileşim) koşmadı** → doğrulamada.
- [x] F8.8 (163c4af): README "Kendi giriş ekranı" (akış, aynı kaynak, dev proxy, çerezler, hata kodları → ekran), kimlik eklentisinin CLAUDE.md'si (yeni değişmezler; `LoginPage` → `SignInFlow` atıfları), kök CLAUDE.md, CHANGELOG (Added/Changed, `invitations` için yükseltme notu), memory `novacore-security`.
- **Oturum sonu koşusu (D-094 (2)):** 19 sınıf (yeni üçü + `ModuleRegistrationTests`, `ValidationTests`, `OpenApiContractTests`, `AdminTests`, `SignInFlowTests`, `SessionSignInTests`, `PasswordResetTests`, `SignInHardeningTests`, `TwoFactorTests`, `TwoFactorRaceTests`, `AuthorizationCodeFlowTests`, `SessionTests`, `IdentityServerConfigurationTests`, `RegistrationTests`, `ConfigurationValidationTests`, `IdentityServerWiringTests`) 215 test: ilk koşuda 1 kırmızı (`AdminTests`, yukarıda), düzeltmeden sonra `AdminTests` 12/12. `PostgreSqlModelTests` 4/4. Build 0 uyarı, format temiz.

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
