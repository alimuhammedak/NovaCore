# NovaCore — .NET 10 Tek-Geliştirici Modüler Monolit Çekirdeği

Tek geliştirici + Web API + minimum karmaşıklık için tasarlanmış, **derleme-zamanı modüler monolit**
mimarisi. İnce bir paylaşılan **BuildingBlocks çekirdeği** + her modülde **Vertical Slice (dikey dilim)**
ile organize edilmiş iş modülleri.

> **Lisans felsefesi:** MediatR ve AutoMapper artık ticari (Lucky Penny). NovaCore bunları **kullanmaz**.
> Yerine: sade CQRS (mediator yok), **Mapperly** (Apache-2.0), .NET 10'un yerleşik validation'ı,
> **Scalar** (MIT) ve Microsoft-yerleşik bileşenler. Çekirdek paketler yalnızca Microsoft paketlerine bağlıdır;
> üçüncü taraf kütüphaneler (Serilog, OpenTelemetry, Scalar) opsiyonel eklenti paketlerindedir. Hepsi MIT/Apache.

---

## İçindekiler
- [Yeni Proje (Tüketici Rehberi)](#yeni-proje-tüketici-rehberi)
  - [Kurulum](#kurulum) · [Şablon ne üretir](#şablon-ne-üretir) · [Sorumluluk modeli](#sorumluluk-modeli) ·
    [Kontrol listesi](#yeni-proje-kontrol-listesi) · [Ayarları ezmek](#ayarları-ezmek) ·
    [Nesne düzeyi yetki](#nesne-düzeyi-yetki) · [BFF](#tarayıcı-uygulamaları-bff) ·
    [Migration dağıtımı](#migrationların-dağıtımı) · [Güncelleme](#güncelleme)
- [Bu Repoda Çalışmak](#bu-repoda-çalışmak)
- [Mimari](#mimari)
- [Proje Ağacı](#proje-ağacı)
- [Yeni Modül Ekleme](#yeni-modül-ekleme)
- [Modüller Arası İletişim](#modüller-arası-i̇letişim)
- [Cross-Cutting Concern'ler](#cross-cutting-concernler)
- [Güvenlik & Yetkilendirme](#güvenlik--yetkilendirme)
- [Konfigürasyon](#konfigürasyon)
- [Veritabanı & Migration](#veritabanı--migration)
- [Test](#test)
- [Çekirdeği Dağıtma (Template / NuGet)](#çekirdeği-dağıtma)
- [Paket / Lisans Tablosu](#paket--lisans-tablosu)
- [Kararı Değiştirecek Eşikler](#kararı-değiştirecek-eşikler)

---

## Yeni Proje (Tüketici Rehberi)

NovaCore bir projeye iki parça hâlinde gelir: **paketler** (`BMD.NovaCore.*`, GitHub Packages) ve onlara bağlı bir
iskelet üreten **şablon** (`BMD.NovaCore.Templates`). Paketler NovaCore'un sorumluluğundadır ve sürümle güncellenir.
Şablonun ürettiği kod ilk andan itibaren projenindir.

### Kurulum

```bash
# 1) Bir kez: paketlerin feed'i. GitHub Packages'ın NuGet kaydı, paket public olsa da kurulum için read:packages
#    yetkili bir classic token ister (DECISIONS D-050).
#    Kaynağın adı novacore olmalı: üretilen nuget.config aynı adla tanımlar, kimlik bilgisi buradan gelir.
dotnet nuget add source https://nuget.pkg.github.com/alimuhammedak/index.json -n novacore -u <github-kullanıcısı> -p <token>

# 2) Şablon paketi
dotnet new install BMD.NovaCore.Templates::0.1.0-preview.1

# 3) Proje
dotnet new novacore -n SiparisServisi                    # kimlik sağlayıcı host'un içinde (varsayılan: --auth BuiltIn)
dotnet new novacore -n SiparisServisi --auth External    # harici sağlayıcı (Entra ID, Keycloak): yalnızca token doğrulanır
cd SiparisServisi && dotnet build && dotnet test         # SQL Server gerekmez: testler SQLite kullanır
```

- Linux ve macOS'ta token'ı saklamak için `--store-password-in-clear-text` gerekir; Windows'ta şifreli saklanır.
- **Paket ID'leri `BMD.NovaCore.*`, içlerindeki assembly ve namespace'ler `NovaCore.*`'dır** (`BMD.NovaCore.AspNetCore` →
  `using NovaCore.AspNetCore;`). nuget.org'da başka sahiplerin `NovaCore.*` paketleri var; üretilen `nuget.config`
  `BMD.NovaCore.*`'ı yalnızca bu feed'e eşler (package source mapping), başka bir kaynak bu adlarla paket veremez.
- Feed kaynağı kullanıcı düzeyinde eklendiği için, kendi `nuget.config`'i olmayan başka bir merkezi paket yönetimli
  projede NU1507 uyarısı görülebilir. NovaCore projeleri kaynaklarını `<clear/>` ile kendileri tanımlar.
- `--auth BuiltIn`: `NovaCore.Identity.OpenIddict` host'ta çalışır, Development'ta `Jwt:Authority` host'un kendisidir
  (https profili). Scalar (`/scalar/v1`) Authorization Code + PKCE ile giriş yapar.
- `--auth External`: kimlik sağlayıcı yoktur. `Jwt:Authority`'ye sağlayıcının issuer adresini yazın ve o sağlayıcının
  kullanıcıları için bir `IPermissionResolver` kaydedin; izinler sunucuda çözüldüğü için host onsuz açılmaz.

### Şablon ne üretir

| Yol | İçerik |
|---|---|
| `src/Api/<Ad>.Api` | `Program.cs` (kompozisyon kökü), üretim kontrol listesini yorum olarak taşıyan `appsettings.json`, Development dışı health probe örneği; BuiltIn'de kimlik sağlayıcının migration'ları |
| `src/Modules/Todos/<Ad>.Modules.Todos` | Örnek modül: dört slice (oluştur, oku, sayfalı listele, tamamla), izinler, validation, `Result` → ProblemDetails, `todos` şeması, migration, outbox. Kendi modülünüzü yanına ekleyin (bkz. [Yeni Modül Ekleme](#yeni-modül-ekleme)), örneği silin |
| `tests/<Ad>.Api.Tests` | `WebApplicationFactory` + in-memory SQLite + bellek içi OpenID Connect issuer; izinler `CreateClientWithPermissions(...)` ile verilir |
| `.github/workflows/ci.yml` | Restore (NuGet audit), build, test, context başına migration kontrolü, varsayılan dalda migration bundle'ları, açık paket kontrolü. Repo secret'ı `NOVACORE_PACKAGES_TOKEN` (read:packages) gerekir. Action'lar commit SHA'sına sabit, job izinleri en az düzeyde |
| `.github/dependabot.yml` | Action SHA'larını haftalık günceller (7 gün bekleme). NuGet güncellemeleri yok: GitHub Packages için Dependabot'a özel registry ve secret gerekir |
| Kök | `nuget.config`, `Directory.Build.props` (uyarılar hata, analyzer'lar, NuGet audit), `Directory.Packages.props` (merkezi sürümler, `NovaCoreVersion`), `global.json`, `.editorconfig`, `dotnet-tools.json` (dotnet-ef) |

Log hattı tektir: `AddCoreObservability` log, metrik ve trace'i OpenTelemetry'den geçirir, `OTEL_EXPORTER_OTLP_ENDPOINT`
varsa OTLP'ye gönderir. Serilog eklentisi (`BMD.NovaCore.Serilog`) isteyen proje için ayrıca durur.

### Sorumluluk modeli

| Katman | İçerik | Sahibi | Nasıl değişir |
|---|---|---|---|
| **Paketler** | Doğru yapılması zor, güvenlik açısından kritik ve her projede aynı olması gereken mekanizmalar | NovaCore | SemVer sürümü, PublicAPI takibi, CHANGELOG |
| **Şablon çıktısı** | `Program.cs`, `appsettings*.json`, test projesi, CI, örnek modül | Üretildiği andan itibaren proje | Projede düzenlenir; paket güncellemesi bu kodu değiştirmez |
| **Proje** | İş modülleri, ortam kararları, işletim | Proje | — |

Bir şeyin nereye gideceğine karar kuralı: güvenlik ya da tutarlılık için kritik, doğru yapılması zor ve en az iki
projede gereken → paket. İskeleti ortak ama değeri projeye göre değişen → şablon. Ortama bağlı (proxy, anahtar deposu,
secret deposu, veritabanı hesabı, IdP barındırma) → proje; paket yalnızca doğru yeri ve sırayı sağlar.

| Konu | Paket | Şablon | Proje |
|---|---|---|---|
| HTTP pipeline | `UseCoreWeb` sırayı belirler (forwarded headers, security headers, exception handler, CORS, rate limiter, kimlik doğrulama, yetkilendirme) | `UseCoreWeb`'i çağırır | Kendi middleware'ini `UseCoreWeb`'den sonra ekler |
| Token doğrulama | JwtBearer, yalnızca asimetrik, `Jwt:Authority` | `Jwt:Authority`/`Audience` değerleri | IdP'yi seçer ve işletir |
| Yetkilendirme | `RequirePermission`, varsayılan kapalı (fallback policy), izin önbelleği, resolver sözleşmesi | Örnek modülün izinleri | İzin tanımları, harici IdP'de resolver, **nesne düzeyi yetki** |
| Hata sözleşmesi | ProblemDetails, `Result` → HTTP, exception eşlemesi | — | Domain hataları, kendi `IExceptionHandler`'ları |
| Veri | `AddModuleDbContext`, outbox, audit | Sağlayıcı (`UseSqlServer`), migration bundle'ları | Şema, migration üretimi ve **uygulanması**, yedekleme |
| Gözlem | OpenTelemetry ve Serilog eklentileri | OpenTelemetry hattı | Backend (OTLP collector), alarmlar |
| Proxy ve TLS | `UseForwardedHeaders` en başta, güvenilen proxy listesi doğrulanır | `ForwardedHeaders:*` | Reverse proxy, TLS sertifikası, HTTPS yönlendirmesi |
| Data Protection | Kimlik eklentisi Development dışında anahtar deposu yoksa açılmaz | Kontrol listesinde | Anahtar deposu (volume, veritabanı, Key Vault) |
| Health | Kontroller (her modül DB'si, liveness) | Yönetim portunda probe örneği | Orchestrator probe ayarı |
| Frontend kimliği | — | — | BFF ya da server-rendered UI (aşağıda) |
| Test | — | Test projesi, factory, test IdP | Testler |

### Yeni proje kontrol listesi

Üretilen `appsettings.json`'daki `PRODUCTION:` notları aynı listedir.

- [ ] `Jwt:Authority` ve `Jwt:Audience`. BuiltIn: host'un public https adresi. External: sağlayıcının issuer adresi ve
      bir `IPermissionResolver`.
- [ ] `Cors:AllowedOrigins`: tarayıcıdan çağıracak origin'ler. Liste boşsa Development dışında hiçbir origin'e izin yok.
- [ ] `AllowedHosts`: API'nin cevap verdiği alan adları (şablonda `*`).
- [ ] Bağlantı dizesi. Şablondaki Windows kimlik doğrulaması yalnızca yereldir; uygulamanın hesabı DDL yetkisi taşımaz.
- [ ] Migration'lar CI'ın bundle'larıyla, ayrı bir hesapla uygulanır; `Database:ApplyMigrations` Development dışında `false`.
- [ ] Reverse proxy arkasındaysa `ForwardedHeaders:KnownProxies`/`KnownIPNetworks`. TLS ve HTTPS yönlendirmesi edge'de.
- [ ] Probe'lar: `HealthChecks:ProbePort` ve host'un o portu da dinlemesi (`ASPNETCORE_HTTP_PORTS=8080;8081`).
- [ ] `OTEL_EXPORTER_OTLP_ENDPOINT`: log, metrik ve trace'in gideceği collector.
- [ ] Secret'lar Development'ta user-secrets'ta, diğer ortamlarda ortam değişkeninde ya da bir vault'ta; CI'da
      `NOVACORE_PACKAGES_TOKEN`.
- [ ] Yalnızca BuiltIn, eksikse host açılmaz: OpenIddict imza ve şifreleme sertifikaları
      (`services.AddOpenIddict().AddServer(s => s.AddSigningCertificate(...).AddEncryptionCertificate(...))`), gerçek
      bir `IEmailSender` (onay ve parola sıfırlama linkleri), bir Data Protection anahtar deposu
      (`services.AddDataProtection().PersistKeysTo...(...)`). Seed yöneticisinin parolası ve client secret'ları secret
      deposundan.

### Ayarları ezmek

Paketler ayarlarını options pattern'le kurar, kayıt anında hiçbir değer okunmaz. Bu yüzden proje her birini
`Program.cs`'te, ilgili `AddCore*` çağrısından sonra değiştirebilir: `services.Configure<T>` paketin `Configure`'ından
sonra, `services.PostConfigure<T>` bütün `Configure`'lardan sonra çalışır.

| Options | Paketin kurduğu | Not |
|---|---|---|
| `JwtBearerOptions` (`JwtBearerDefaults.AuthenticationScheme`) | `Authority`, audience, yalnızca asimetrik algoritmalar, `ClockSkew` 30 sn, `MapInboundClaims = false` | **`Configure` kullanın, `PostConfigure` değil:** JwtBearer discovery'yi kendi `PostConfigure`'ında `Authority`'den kurar, sonra değişen `Authority` etkisiz kalır |
| `AuthorizationOptions` | `FallbackPolicy` = kimliği doğrulanmış kullanıcı | Açık uç için policy'yi gevşetmek yerine o uca `.AllowAnonymous()` |
| `RateLimiterOptions` | IP başına global fixed-window (`RateLimiting:*`), `"strict"` policy | Kendi policy'lerinizi `AddPolicy` ile ekleyin |
| `CorsOptions` | `CoreWebExtensions.CorsPolicyName` policy'si, `Cors:AllowedOrigins`'ten | Aynı adla `AddPolicy` policy'yi değiştirir |
| `ForwardedHeadersOptions` | `ForwardedHeaders:*` doluysa yalnızca `X-Forwarded-For` ve `X-Forwarded-Proto`, yalnızca listedekilerden | |
| `ExceptionHandlerOptions`, `ProblemDetailsOptions` | Exception → durum kodu ve ProblemDetails (`traceId`, `instance`) | Bir exception'ı kendiniz eşlemek için `IExceptionHandler` kaydedin: paketin eşlemesi middleware'in yedek yolunda olduğu için sizinki her zaman önce çalışır |
| `HybridCacheOptions` | Varsayılan süre `Cache:DefaultExpiration` | L2 için bir `IDistributedCache` (Redis) kaydetmek yeter |
| `OpenApiOptions` (belge `v1`) | Bearer security scheme | Kendi transformer'larınızı ekleyin |
| Kimlik eklentisi: `IdentityOptions`, `PasswordHasherOptions`, `DataProtectionTokenProviderOptions`, uygulama cookie'si, `OpenIddictServerOptions` | Parola en az 12, lockout 5 deneme / 15 dk, onaylı e-posta, PBKDF2 600.000, onay token'ı 6 saat, cookie 30 dk kayan, access token 15 dk, refresh 7 gün | Değerleri yükseltmek serbest; düşürmek eklentinin güvenlik varsayımlarını bozar (bkz. [Güvenlik](#güvenlik--yetkilendirme)) |

```csharp
// Program.cs: AddCoreWeb'den sonra
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
    options => options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(10));
builder.Services.PostConfigure<RateLimiterOptions>(options => options.AddPolicy("uploads", context =>
    RateLimitPartition.GetConcurrencyLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new ConcurrencyLimiterOptions { PermitLimit = 2, QueueLimit = 0 })));
```

Ezilemeyenler: `UseCoreWeb`'in middleware sırası (kimlik doğrulama ile yetkilendirme arasında bir yer yok), izin
önbelleğinin anahtarı (`sub` + security stamp) ve outbox ile token temizliğinin sabitleri (5 sn tur, 10 deneme, 7 gün
saklama; 14 gün). Bunlar bir proje ihtiyaç duyunca açılır (DECISIONS Y-14, Y-19).

### Nesne düzeyi yetki

`RequirePermission` işlev düzeyindedir: "bu kullanıcı todo tamamlayabilir mi". "Bu kullanıcı **bu** todo'yu
tamamlayabilir mi" sorusu kaydın kendisine bakar ve projenindir. OWASP API Security Top 10'da birinci sıradaki risk
budur (API1:2023 Broken Object Level Authorization). Microsoft'un yolu resource-based authorization'dır:

```csharp
// Kaydın sahibi: oluşturan handler ICurrentUser.UserId'yi (token'daki sub) yazar. CreatedBy kullanıcı adını tutar,
// denetim içindir; sahiplik için değil.
public sealed class TodoOwnerRequirement : IAuthorizationRequirement;

internal sealed class TodoOwnerHandler : AuthorizationHandler<TodoOwnerRequirement, Todo>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TodoOwnerRequirement requirement, Todo todo)
    {
        if (todo.OwnerId == context.User.FindFirstValue(NovaClaims.Subject))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

// Modülün AddModule'ünde: services.AddSingleton<IAuthorizationHandler, TodoOwnerHandler>();
// Handler'da, kaydı okuduktan sonra (ClaimsPrincipal user ve IAuthorizationService authorization parametreleriyle):
if (!(await authorization.AuthorizeAsync(user, todo, new TodoOwnerRequirement())).Succeeded)
    return Result.Failure(Error.NotFound("todo.not_found", $"Todo '{id}' was not found.")).ToHttpResult();
```

Başkasının kaydına 404 dönmek kaydın varlığını da gizler. Listelerde aynı kural sorguya girer
(`Where(t => t.OwnerId == currentUser.UserId)`): sayfalanmış bir listeyi bellekte süzmek sayfaları bozar.

### Tarayıcı uygulamaları: BFF

Microsoft, web uygulamaları için tarayıcıda çalışan public OIDC/OAuth client'larını artık önermiyor; önerilen mimari
**Backend for Frontend**: token'lar sunucuda kalır, tarayıcıda yalnızca HttpOnly bir cookie olur. NovaCore'da bir BFF
paketi yok (iki proje ihtiyaç duyana kadar, Y-14). Seçenekler:

- **SPA + BFF host:** ayrı bir ASP.NET Core host'u confidential client olarak (`Identity:Clients`'ta `ClientSecret` ve
  `RedirectUris`) OIDC ile oturum açar, cookie tutar ve API çağrılarını access token'ı ekleyerek API'ye iletir (YARP).
- **Server-rendered UI** (Blazor Web App, Razor Pages): API'yi sunucu tarafından çağırır; tarayıcı token görmez.
- **Yalnızca API:** mobil ya da masaüstü uygulamalar Authorization Code + PKCE ile public client olabilir.

### Migration'ların dağıtımı

- Üretilen CI varsayılan dala her push'ta context başına bir **migration bundle** üretir (`migration-bundles`
  artifact'i, 7 gün). Dağıtımda, uygulama sürümünden önce tek seferlik bir iş olarak, şema yetkili ayrı bir hesapla
  çalıştırılır: `ASPNETCORE_ENVIRONMENT=Production ./efbundle --connection "$DEPLOYMENT_CONNECTION_STRING"`. Bundle'lar
  framework-dependent'tır (`linux-x64`); runtime'ı olmayan bir ortam için CI'da `--self-contained` ekleyin.
- SQL'in gözden geçirilmesi gerekiyorsa script: `dotnet ef migrations script --idempotent --project <modül> --startup-project <modül> --context <Context> -o <ad>.sql`.
- `Database:ApplyMigrations=true` (açılışta uygula) yalnızca Development ya da tek örnekli basit kurulumlar içindir: her
  replika migration denemesine girer ve uygulamanın hesabı şema yetkisi ister.
- Her context kendi şemasında kendi `__EFMigrationsHistory` tablosunu tutar; bundle'lar birbirinden bağımsızdır.

### Güncelleme

`Directory.Packages.props`'taki `NovaCoreVersion`'ı değiştirin: bütün `BMD.NovaCore.*` paketleri aynı sürümde kalır.
Önce [CHANGELOG](CHANGELOG.md)'u okuyun; 0.x sürümlerinde minor sürüm kırıcı değişiklik içerebilir. Şablonun ürettiği
kod güncellemeyle değişmez; yeni bir şablon özelliğini istiyorsanız yeni sürümle bir proje üretip farkı alın.

---

## Bu Repoda Çalışmak

Bu bölüm NovaCore'un kendisini (paketler, referans Api host'u, Catalog ve Payments modülleri) geliştirmek içindir.

Gereksinim: **.NET 10 SDK**, tam olarak `global.json`'daki sürüm (10.0.300; `rollForward: disable`, lock dosyaları için),
**SQL Server** (yerel instance, Windows auth — bkz. `appsettings.json`).

```bash
# 1) Bağımlılıkları derle + test et (veritabanı GEREKMEZ — testler SQLite kullanır)
dotnet build
dotnet test

# 2) Migration'ları uygula (modül başına ayrı şema: catalog / payments / identity)
dotnet tool restore
dotnet ef database update --project src/Modules/Catalog/NovaCore.Modules.Catalog   --context CatalogDbContext   --startup-project src/Modules/Catalog/NovaCore.Modules.Catalog
dotnet ef database update --project src/Modules/Payments/NovaCore.Modules.Payments  --context PaymentsDbContext  --startup-project src/Modules/Payments/NovaCore.Modules.Payments
dotnet ef database update --project src/Api/NovaCore.Api  --context IdentityServerDbContext  --startup-project src/Api/NovaCore.Api

# 3) İlk admin + roller + izinler için seed (parolayı user-secrets ile ver — repoya YAZMA). Admin izinleri, admin
#    parolayla girip 2FA'yı açınca gelir: POST /api/v1/auth/2fa/setup, sonra /2fa/enable (parola + authenticator kodu)
dotnet user-secrets --project src/Api/NovaCore.Api set "Identity:Seed:AdminPassword" "Admin!Passw0rd-2026"

# 4) Çalıştır: https profili. Host kendi kimlik sağlayıcısıdır; Development'ta Jwt:Authority = https://localhost:5443
Identity__Seed__SeedOnStartup=true dotnet run --project src/Api/NovaCore.Api --launch-profile https
```

Açılınca tarayıcı **Scalar API arayüzünü** (`/scalar/v1`) açar. Bağlantı dizesi (varsayılan):
`Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;`

| Uç nokta | Açıklama |
|---|---|
| `GET /health` | Readiness: tüm health check'ler, her modülün DB bağlantısı dahil (yalnızca Development) |
| `GET /alive` | Liveness: yalnızca "live" etiketli (yalnızca Development) |
| `GET /openapi/v1.json` | OpenAPI 3.1 dokümanı |
| `/scalar/v1` | Scalar API arayüzü (yalnızca Development) |
| `/connect/authorize` · `token` · `revoke` · `endsession` | OpenID Connect sağlayıcı: Authorization Code + PKCE, refresh, client credentials (bkz. [Güvenlik](#güvenlik--yetkilendirme)) |
| `/account/login` · `forgot-password` | Sağlayıcının giriş sayfası (gerekirse 2FA adımı) ve parola sıfırlama |
| `/api/v1/auth/register` · `confirm-email` · `resend-confirmation-email` · `me` · `change-password` · `logout-all` | Hesap API'si |
| `/api/v1/auth/2fa/*` | TOTP iki-faktör (setup/enable/disable) |
| `/api/v1/admin/*` | Kullanıcı/rol/izin yönetimi (permission korumalı) |
| `/api/v1/products` | Catalog modülü (CRUD, permission korumalı) |
| `/api/v1/payments` | Payments modülü (Catalog'a contract ve event'lerle bağlı) |

> SQL Server olmadan da uygulama açılır ve `/openapi`, validation/401 kapıları çalışır. `/health` bu durumda
> `Unhealthy` (503) döner, çünkü modül veritabanlarına ulaşamaz. DB'ye dokunan endpoint'ler düzgün bir
> **RFC 9457 ProblemDetails** ile yanıt verir.
>
> `/health` ve `/alive` yalnızca Development'ta map'lenir (DECISIONS D-022). Production'da probe gerekiyorsa host
> kendi kaydını yapar. `RequireHost` Host header'ına güvendiği için sahtelenebilir; port kontrolü
> `ConnectionInfo.LocalPort` ile yapılmalıdır. Şablonun `Program.cs`'i bunu `UseHealthChecks(path, port)` ile
> `HealthChecks:ProbePort`'ta yapar.

Şablondan yeni proje açmak için bkz. [Yeni Proje (Tüketici Rehberi)](#yeni-proje-tüketici-rehberi). Şablonu bu
repodan denemek: `bash eng/template-smoke.sh` (bütün paketleri yerel bir feed'e pack eder, iki kimlik seçeneğiyle proje
üretip derler ve test eder).

### CI, tedarik zinciri ve güvenlik taramaları

Repo public'tir (DECISIONS D-042). Kararlar D-048, D-051 ve D-052'de; kaynaklar `docs/plan/RESEARCH.md` R-094…R-098'de.

| Ne | Nerede | Not |
|---|---|---|
| Build, test, migration kontrolü, açık paket kontrolü | `ci.yml` → `build-and-test` | Restore `--locked-mode` ile; ardından lisans kapısı (`eng/check-licenses.sh`) |
| Şablonun tüketici smoke testi | `ci.yml` → `template-smoke` | Üretilen projelerin workflow'ları da lint'lenir |
| Secret taraması | `ci.yml` → `secret-scan` | Betterleaks 1.9.0, bütün geçmiş, `.betterleaks.toml` |
| Workflow denetimi | `ci.yml` → `workflow-lint` | actionlint + zizmor, digest'e sabit imajlarla (`eng/lint-workflows.sh`) |
| Statik analiz | `codeql.yml` | C#, manual build (kaynak üreteçlerinin kodu da analiz edilir); uyarılar Security sekmesinde |
| Tedarik zinciri puanı | `scorecard.yml` | OpenSSF Scorecard, main'e push'ta ve haftalık |
| Güncellemeler | `.github/dependabot.yml` | NuGet, action'lar, SDK, compose imajları; 7 gün bekleme |
| Yayın | `release.yml` | `build` ve `publish` ayrı job'lar; `publish` `release` environment'ında onay bekler |

```bash
# Paket grafiği lock dosyalarındadır (Api, modüller, test). Bir paket sürümünü değiştirince lock dosyalarını yenileyin:
dotnet restore NovaCore.slnx --force-evaluate
dotnet restore NovaCore.slnx --locked-mode          # CI'ın yaptığı kontrol

bash eng/check-licenses.sh                           # her paket MIT/Apache-2.0 (Microsoft ve Polly istisnası: D-052)
bash eng/lint-workflows.sh                           # Docker gerekir; GH_TOKEN varsa zizmor online denetimleri de yapar

# Opt-in: staged değişiklikler commit'ten önce taranır (Betterleaks 1.9.0 PATH'te olmalı)
git config core.hooksPath .githooks
```

- **Action'lar** tam commit SHA'sına sabittir (`uses: actions/checkout@<sha> # v7.0.1`). Şablonun workflow'u
  `#if` satırları yüzünden Dependabot'un okuyabileceği bir YAML değil. `WorkflowPinTests`, şablondaki SHA'ların repo'yla
  aynı olduğunu doğrular; Dependabot repo'yu güncelleyince test, şablon da güncellenene kadar kırmızı kalır.
- **Test kimlik bilgileri** `.betterleaks.toml`'da değer listesiyle muaf tutulur: değer tam eşleşmeli ve `tests/` altında
  olmalı. Yeni bir test değeri bu listeye eklenir; yol bazlı muafiyet yoktur.
- **Repo ayarları:** secret scanning ve push protection açık, Dependabot uyarıları ve güvenlik güncellemeleri açık.
  `main`'de force push ve silme kapalı. `v*` tag'lerini yalnızca admin oluşturur, taşır, siler.
- **Yayınlanan paketin doğrulanması:** her `.nupkg`, `.snupkg` ve SBOM (`*.cdx.json`, workflow artifact'inde) imzalı
  build provenance taşır:
  ```bash
  gh attestation verify BMD.NovaCore.AspNetCore.<sürüm>.nupkg --repo alimuhammedak/NovaCore
  ```
- `docker-compose*.yml` servisleri yalnızca `127.0.0.1`'de dinler, imajlar tag + digest ile sabittir.

---

## Mimari

**Hibrit:** makro seviyede **modüler monolit**, mikro seviyede **vertical slice**, altta ince bir
**paylaşılan çekirdek**. Karmaşık bir modül gerekirse o modül kendi içinde Clean/DDD katmanlaması
kullanabilir — diğerleri etkilenmez.

```
Api (ince host / composition root) ──►  eklentiler: NovaCore.Serilog / .OpenTelemetry / .Scalar
        │  referans                      (üçüncü taraf paketler; en fazla AspNetCore'a bağlanır)
        ▼
Modules.*  ──(yalnızca Contracts üzerinden)──►  diğer Modules.*
        │
        ▼
NovaCore.AspNetCore        NovaCore.EntityFrameworkCore     (çekirdek: yalnızca Microsoft paketleri;
        │                           │                        ikisi birbirine bağlanmaz)
        └─────────────┬─────────────┘
                      ▼
           NovaCore.Abstractions (yalnızca BCL)
                      │
                      ▼
.NET 10 BCL / ASP.NET Core / EF Core
```

**Bağımlılık kuralları**
- `NovaCore.Abstractions` hiçbir framework'e bağlı değildir (yalnızca BCL).
- Çekirdek paketler (`NovaCore.Abstractions`, `NovaCore.AspNetCore`, `NovaCore.EntityFrameworkCore`) yalnızca
  Microsoft paketlerine bağlanır. AspNetCore ile EntityFrameworkCore birbirine bağlanmaz; eklentiler en fazla
  AspNetCore ve Abstractions'a bağlanır. Bu kurallar **mimari testlerle** zorlanır (`ModularityTests`,
  `PackageDependencyTests`).
- Modüller **birbirine doğrudan referans vermez**; yalnızca bir modülün `*.Contracts` projesine bağlanır.
  Bu da bir mimari test ile zorlanır.
- `BuildingBlocks` hiçbir modüle bağlı değildir.

### Çekirdek (her projede ortak) vs Proje (her projeye özel)
| Çekirdek (BuildingBlocks) | Projeye özel (Modules / Api) |
|---|---|
| `Result<T>`, `Error`, `PagedList<T>` | Domain entity'leri (Product, Payment...) |
| Global exception handling + ProblemDetails | İş modülleri ve endpoint'leri |
| `IModule` + açık kayıt (`AddModule<TModule>()`) | DbContext'ler + migration'lar |
| HybridCache kaydı ve varsayılan süre (`AddCoreCaching`) | İş kuralları, request validation kuralları |
| Health + resilient HttpClient (`AddCoreDefaults`); Serilog ve OpenTelemetry eklentileri | Modüle özel konfigürasyon |
| OpenAPI (+ Scalar eklentisi), rate limiting, CORS | |
| JWT bearer auth (`Jwt:Authority`) + permission-based authz altyapısı | Kimlik sağlayıcının store'u ve migration'ları (host'ta) |
| Kimlik sağlayıcı eklentisi (`NovaCore.Identity.OpenIddict`) | Client'lar, seed yöneticisi, üretim sertifikaları |
| Auditing interceptor, transactional outbox ve dispatcher'ı | |

---

## Proje Ağacı

```
NovaCore.slnx
├── Directory.Build.props          # tüm projeler: net10.0, nullable, analyzer'lar, TreatWarningsAsErrors, VersionPrefix
├── Directory.Build.targets        # IsPackable projeler: paket metadata'sı, README, PublicAPI dosyaları, doc/analyzer korumaları
├── Directory.Packages.props       # merkezî paket sürümleri (CPM)
├── docker-compose.yml             # opsiyonel SQL Server + Seq + Jaeger (imaj sürümleri pinli)
├── docker-compose.keycloak.yml    # opsiyonel harici IdP: Keycloak + örnek realm (deploy/keycloak/)
├── .env.example                   # compose parolaları için şablon (.env git-ignored)
├── eng/template-smoke.sh          # şablonu paketlerden kurup her kimlik seçeneğiyle build/test eden smoke testi
│
├── src/
│   ├── BuildingBlocks/
│   │   ├── NovaCore.Abstractions/        # Result, PagedList, Entity, IDomainEvent/DomainEvent, ICurrentUser (yalnızca BCL)
│   │   ├── NovaCore.AspNetCore/          # IModule + AddModule<T>, AddCoreDefaults/AddCoreWeb, ProblemDetails, Result→HTTP, JWT, authz, OpenAPI, HybridCache
│   │   ├── NovaCore.EntityFrameworkCore/ # AddModuleDbContext, AddCorePersistence: auditing, startup migration, outbox + dispatcher
│   │   ├── NovaCore.Serilog/             # eklenti: AddCoreLogging (Console + opsiyonel Seq)
│   │   ├── NovaCore.OpenTelemetry/       # eklenti: AddCoreObservability (logs + metrics + tracing, OTLP)
│   │   ├── NovaCore.Scalar/              # eklenti: MapCoreScalar (Scalar UI, yalnızca Development)
│   │   └── NovaCore.Identity.OpenIddict/ # eklenti: AddCoreIdentityServer/MapCoreIdentityServer (OIDC sağlayıcı: Identity + OpenIddict)
│   │
│   ├── Modules/
│   │   ├── Catalog/
│   │   │   ├── NovaCore.Modules.Catalog.Contracts/  # IProductCatalog + ProductInfo, StockReserved/StockReservationFailed (dışa açılan tek yüzey)
│   │   │   └── NovaCore.Modules.Catalog/            # CatalogModule, Domain, Features/, Infrastructure/ (schema: catalog)
│   │   └── Payments/
│   │       ├── NovaCore.Modules.Payments.Contracts/ # PaymentCreated (Catalog'un tepki verdiği event)
│   │       └── NovaCore.Modules.Payments/           # PaymentsModule (schema: payments) — Catalog'a Contracts ile bağlı
│   │
│   ├── Api/
│   │   └── NovaCore.Api/           # Program.cs: composition root, her şeyi açıkça ekler
│   │       └── Identity/           # kimlik sağlayıcının migration'ları + design-time factory (schema: identity)
│   │
│   └── Templates/
│       └── NovaCore.Templates/     # `dotnet new novacore` şablon paketi (BMD.NovaCore.Templates); şablon content/novacore'da
│
└── tests/
    └── NovaCore.Api.Tests/         # unit + integration (WebApplicationFactory) + architecture testleri
```

### `Program.cs` — tüm host
```csharp
var builder = WebApplication.CreateBuilder(args);

// Eklentiler (üçüncü taraf paketler)
builder.AddCoreLogging();                          // Serilog: Console + opsiyonel Seq
builder.AddCoreObservability();                    // OpenTelemetry metrics + tracing, OTLP yalnızca konfigüre ise

// Çekirdek
builder.AddCoreDefaults();                         // health + resilient HttpClients
builder.AddCoreWeb();                              // ProblemDetails, OpenAPI, JWT auth, rate limit, CORS
builder.AddCorePersistence();                      // audit interceptor, domain event'ler + outbox dispatcher, opt-in startup migration

// Kimlik sağlayıcı (eklenti): Jwt:Authority'nin gösterdiği token'ları üretir; store'u ve migration'ları host'ta
builder.AddCoreIdentityServer();
builder.Services.AddModuleDbContext<IdentityServerDbContext>((services, db) => db.UseSqlServer(/* ... */));

// Modüller: açık kayıt; endpoint'ler bu sırayla bağlanır
builder.AddModule<CatalogModule>();
builder.AddModule<PaymentsModule>();

var app = builder.Build();

app.UseSerilogRequestLogging();                    // istek başına bir log satırı
app.UseCoreWeb();                                  // security headers, exception handling, auth, rate limiter, OpenAPI (Development)
app.MapCoreScalar();                               // Scalar UI (Development)
app.MapCoreIdentityServer();                       // /connect/*, giriş sayfası, /api/v1/auth/*, /api/v1/admin/*
app.MapModules();                                  // modüllerin endpoint'leri
app.MapDefaultEndpoints();                         // /health, /alive (Development)

app.Run();
```

---

## Yeni Modül Ekleme

"Ödeme modülü ekleme" senaryosu tam olarak şöyle: **yeni proje + `IModule` + Api referansı + `Program.cs`'te bir
satır.** Modüller assembly taranarak bulunmaz; host her modülü `builder.AddModule<TModule>()` ile açıkça ekler.

1. `dotnet new classlib -o src/Modules/Orders/NovaCore.Modules.Orders`. Bu repoda isim `NovaCore.Modules.` ile
   başlamalı (mimari testler modülleri bu önekle bulur); şablondan üretilen projede `<Ad>.Modules.Orders`.
2. `NovaCore.AspNetCore` ve `NovaCore.EntityFrameworkCore`'a referans ver (üretilen projede paket referansı:
   `BMD.NovaCore.AspNetCore`, `BMD.NovaCore.EntityFrameworkCore`; şablonun `Todos` modülünün csproj'u örnektir).
   Validation için paket gerekmez, ASP.NET Core framework'ü getiriyor.
3. `IModule` implement et:

```csharp
public sealed class OrdersModule : IModule
{
    public string Name => "Orders";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<OrdersDbContext>((sp, o) =>
        {
            o.UseSqlServer(config.GetConnectionString("Database"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", OrdersDbContext.Schema));
            o.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });
        services.AddValidation();   // .NET 10 validation; generator yalnızca bu çağrının assembly'sini kapsar
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/orders").WithTags("Orders");
        CreateOrder.Map(group);
        return endpoints;
    }
}
```
4. `NovaCore.Api`'ye proje referansı ekle ve `Program.cs`'e `builder.AddModule<OrdersModule>();` yaz. Bitti.

### Bir özelliğin (feature) anatomisi — Vertical Slice
Her özellik tek dosya: request (validation attribute'larıyla) + response + route + handler bir arada. Mediator yok;
handler bağımlılıklarını doğrudan minimal-API DI'dan alır. Örnek: [`CreateProduct.cs`](src/Modules/Catalog/NovaCore.Modules.Catalog/Features/CreateProduct.cs).

Handler'ın dönüş tipi yanıt sözleşmesidir: `Task<Results<Ok<ProductDto>, ProblemHttpResult>>` gibi bir union.
`result.ToHttpResult()` bu union'ı üretir (`Result` → `NoContent`, `Result<T>` → `Ok<T>`, özel başarı için
`ToHttpResult(value => TypedResults.Created(...))`). Başarı yanıtını OpenAPI belgesine union'ın kendisi yazar, bu yüzden
`.Produces<T>()` yazılmaz; derleyici de bildirilmeyen bir yanıtı reddeder. Hata durum kodları `.ProducesProblem(404)`
ile bildirilir, çünkü ProblemDetails'in durumu çalışma anında belli olur. `EndpointContractTests` OpenAPI'de görünen hiçbir
handler'ın düz `IResult` dönmediğini doğrular.

Validation, .NET 10'un yerleşik Minimal API validation'ıdır. Kurallar request record'unun parametrelerindeki
DataAnnotations'tır (`[Required, MaxLength(200)] string Name`). Hazır bir attribute'un ifade edemediği kural, tek bir
değer için özel bir `ValidationAttribute`'tur (ör. `CreatePayment`'taki `NotEmpty`), birden çok özellik için
`IValidatableObject`'tir. Geçersiz istek handler'a ulaşmadan 400 validation ProblemDetails alır (`errors` alan adına
göre). Modül `services.AddValidation()` çağırmazsa hiçbir şey doğrulanmaz ve bir hata da oluşmaz. Bu yüzden
`ValidationTests` her validation'lı endpoint'i sabitler.

---

## Modüller Arası İletişim

**Kural:** doğrudan DbContext/tablo erişimi YASAK. İletişim ya **public contract interface** (in-process
metot çağrısı) ya da **domain event** ile olur.

Örnek: Payments → Catalog. Payments yalnızca `NovaCore.Modules.Catalog.Contracts` projesine bağlıdır,
Catalog modülünün kendisine değil. `CreatePayment`, ürünü `IProductCatalog` üzerinden doğrular ve fiyatlar:

```csharp
var product = await catalog.GetProductAsync(request.ProductId, ct);  // in-process, DB sınırını aşmadan
if (product is null)             return Error.NotFound(...);
if (!product.IsActive)           return Error.Validation(...);
if (product.Stock < request.Qty) return Error.Conflict(...);
```

Bu, ileride bir modülü mikroservise ayırmayı ucuz tutar: contract aynı kalır, çağrı in-process'ten
HTTP/mesaj kuyruğuna döner.

**Stok rezervasyonu: iki yönlü event akışı.** Stoğu düşmek iki modülün ayrı DbContext'lerine yazmak demek ve bu
context'ler ortak bir transaction paylaşmaz. Bu yüzden ödeme ile stok, outbox üzerinden event'lerle tutarlı tutulur
(DECISIONS D-034):

1. `CreatePayment` yukarıdaki kontrolleri yapar ve ödemeyi **Pending** olarak kaydeder. `Payment`'ın raise ettiği
   `PaymentCreated` (`NovaCore.Modules.Payments.Contracts`) aynı transaction'da outbox'a yazılır. Yanıt 201 + `Pending`.
2. Catalog'un `ReserveStockForPayment` handler'ı stoğu `Product.RemoveStock` ile düşer ya da talebi reddeder (ürün
   yok, aktif değil ya da stok yetmiyor). Cevap, ödemenin id'siyle anahtarlanan bir `StockReservation` kaydıdır. Bu
   kayıt, stok değişikliği ve cevap event'i (`StockReserved` / `StockReservationFailed`, `Catalog.Contracts`) tek
   `SaveChanges`'te yazılır. Aynı talep ikinci kez teslim edilirse kaydı bulur ve hiçbir şey yapmaz.
3. Payments'ın `SettlePayment` handler'ı ödemeyi **Completed** ya da **Failed** yapar. Ödeme yalnızca bir kez
   sonuçlanır; aynı cevap tekrar gelirse değişiklik olmaz.

Stoğu kesinleştiren kontrol rezervasyondur; `CreatePayment`'taki kontrol yalnızca yaygın durumlara hızlı cevap verir.
Son birim için gelen iki istek ikisi de 201 alabilir, ama biri sonra Failed olur. Teslimler başarılıysa sonuç iki
dispatcher turu içinde (yaklaşık 10 sn) `GET /api/v1/payments`'ta görünür; başarısız her teslim bir tur daha ekler.

Stoğa aynı anda yazan iki işlem birbirinin üstüne yazamaz, çünkü `Product.Stock` bir concurrency token'dır. Ürün uç
noktaları (`PUT`/`DELETE`) okuduktan sonra değişmiş bir stokta 409 `product.concurrency_conflict` döner; rezervasyonun
teslimi ise düşer ve outbox onu bir sonraki turda güncel stokla yeniden dener.

### Domain event'ler ve outbox

Bir entity olayı `Raise(new SiparisVerildi(...))` ile kaydeder. Event, `DomainEvent`'ten türeyen bir record'dur.
Kaydeden SaveChanges event'i modülün `outbox_messages` tablosuna aynı transaction içinde yazar (**transactional
outbox**): entity değişikliği ve event ya birlikte kalıcı olur ya hiç olmaz. Bu işi `OutboxInterceptor` yapar;
`AddModuleDbContext` onu her modül context'ine ekler, modül tabloyu `modelBuilder.AddOutbox()` ile map'ler.

`AddCorePersistence`'ın kaydettiği `OutboxDispatcher` (bir `BackgroundService`) outbox'lardaki event'leri
`IDomainEventHandler<T>`'lere dağıtır:
- açılışta bir tur çalışır, sonra 5 saniyede bir. Önce en eski mesaj alınır, ama hata alan bir mesaj sonraki turda
  daha yenilerinden sonra denenir: sıra garantisi yoktur;
- her mesaj için bir DI scope açar; o mesajın handler'ları bu scope'u paylaşır;
- işini bitiren handler'ı `outbox_consumers`'a (processed tablosu, anahtarı mesaj satırının `Id`'si + handler) yazar.
  Hata olursa mesaj sonraki turda yeniden denenir, ama başarılı handler'lar tekrar çalışmaz;
- 10 denemeden sonra mesajı inceleme için outbox'ta bırakır;
- işlenmiş mesajı 7 gün sonra siler; handler kayıtları FK'nin cascade delete'iyle onunla gider. İncelemeye bırakılan
  mesaj silinmez.

Event'leri dağıtmanın tek yolu budur: entity event'lerini outbox'ı atlayıp hemen çalıştıran bir dispatcher yoktur.

Garanti **en az bir kez**dir: bir handler'ın kendi kaydıyla bu işaret arasında süreç ölürse handler bir kez daha
çalışır. Handler'lar bu yüzden idempotent yazılır. Veritabanı başına tek dispatcher varsayılır.

Handler DI'a açıkça kaydedilir: `services.AddScoped<IDomainEventHandler<PaymentCreated>, ReserveStockForPayment>()`.
Başka bir modülün tepki vereceği event, yayımlayan modülün `*.Contracts` projesinde durur. Durum değiştiren bir
handler, tekrar teslime karşı iş anahtarıyla bir kayıt tutar ve onu değişikliğiyle aynı `SaveChanges`'te yazar
(`StockReservation`). `outbox_consumers` kaydı bunun yerine geçmez, çünkü handler'ın kendi kaydıyla atomik değildir.

---

## Cross-Cutting Concern'ler

| Concern | Yaklaşım (.NET 10) | NovaCore'da |
|---|---|---|
| **Exception handling** | `UseExceptionHandler` + `AddProblemDetails` (RFC 9457), `StatusCodeSelector` | Eşleme middleware'in kendi yedek yolunda: projenin `IExceptionHandler`'ları her zaman önce çalışır. `AppException` → durumu + mesajı, `BadHttpRequestException` → kendi durumu, diğer her şey → 500, mesajsız; yalnızca 500'ler loglanır |
| **Logging** | Serilog (structured) + OTel korelasyon | `AddCoreLogging` (`NovaCore.Serilog` eklentisi) — Console + opsiyonel Seq |
| **Tracing/Metrics/Logs** | OpenTelemetry | `AddCoreObservability` (`NovaCore.OpenTelemetry` eklentisi) — `ILogger` log'ları da (biçimlenmiş mesaj ve scope'larla); OTLP yalnızca endpoint konfigüre ise. Serilog kullanan host'ta log'lar Serilog'dan gider |
| **Caching** | HybridCache (L1 + opsiyonel L2) | `HybridCache` doğrudan enjekte edilir (zaten soyut bir sınıf, sarmalayıcı yok). `AddCoreCaching` varsayılan süreyi `Cache:DefaultExpiration`'dan alır |
| **Validation** | .NET 10 yerleşik validation (`AddValidation`, DataAnnotations) | Her modül `AddModule`'de `services.AddValidation()` çağırır; kurallar request record'unda |
| **Mapping** | Mapperly (source-gen) | `[Mapper]` statik partial sınıflar, EF-translatable `ProjectToDto` |
| **Auth** | JWT doğrulama (`Jwt:Authority`) + OpenIddict/Identity sağlayıcı eklentisi + permission-based | bkz. [Güvenlik & Yetkilendirme](#güvenlik--yetkilendirme) |
| **Auditing** | EF Core `SaveChangesInterceptor` | `AuditableEntityInterceptor` — CreatedBy/ModifiedAt merkezî |
| **Domain event'ler** | Transactional outbox + `BackgroundService` | `OutboxInterceptor` aynı transaction'da yazar, `OutboxDispatcher` en az bir kez dağıtır (bkz. [Modüller Arası İletişim](#modüller-arası-iletişim)) |
| **Rate limiting** | Yerleşik `AddRateLimiter` | IP bazlı fixed-window + `"strict"` adlı policy |
| **Health** | Yerleşik Health Checks + `AddDbContextCheck` | `/health` (her modül DB'si, "ready" etiketi) + `/alive`, yalnızca Development |
| **API docs** | `Microsoft.AspNetCore.OpenApi` + Scalar | Bearer security scheme transformer ile; Scalar UI `NovaCore.Scalar` eklentisinde (`MapCoreScalar`) |
| **Modül kaydı** | Açık kayıt | `builder.AddModule<TModule>()` (assembly taraması yok) |
| **Result** | Result pattern | Sıfır-bağımlılık `Result<T>` + `ToHttpResult()`, tipli union döner (`Results<Ok<T>, ProblemHttpResult>`) |

`Result<T>` **beklenen** hatalar (NotFound/Validation/Conflict) için; gerçek istisnai durumlar için
exception. Exception'ı akış kontrolü için kullanmaktan kaçınılır.

---

## Güvenlik & Yetkilendirme

İki ayrı parça: **çekirdek token'ı yalnızca doğrular** (`NovaCore.AspNetCore`, D-002), **token'ı bir OpenID Connect
sağlayıcısı üretir**. Api host'u bu sağlayıcıyı kendisi çalıştırır: `NovaCore.Identity.OpenIddict` eklentisi (ASP.NET
Core Identity + OpenIddict, `identity` şeması). Harici bir sağlayıcı (Keycloak, Entra) da aynı çekirdekle kullanılabilir.
Tasarım OWASP cheat sheet'lerine, RFC 9700 (OAuth 2.0 Security BCP) ve Microsoft Learn'e göre sertleştirilmiştir.

### Doğrulama (çekirdek)
- API access token'ı `Jwt:Authority`'ye karşı doğrular; issuer ve imza anahtarları sağlayıcının discovery belgesinden ve
  JWKS'inden gelir. `Jwt:Authority` her ortamda zorunlu, Development dışında `https`.
- Yalnızca asimetrik algoritmalar (RS/PS/ES) kabul edilir, simetrik anahtar yoktur (MS JWT rehberi: *"Asymmetric keys
  should always be used"*). Audience zorunlu, `ClockSkew = 30s`, `MapInboundClaims = false`, prod'da
  `RequireHttpsMetadata = true`.

### Kimlik sağlayıcı (`NovaCore.Identity.OpenIddict`)
- **Akışlar:** Authorization Code + **PKCE (yalnızca S256)**, refresh token, client credentials. Uçlar `/connect/*`;
  tarayıcı girişi `/account/login` (sade HTML, antiforgery, yalnızca yerel ReturnUrl), gerekirse 2FA adımı, parola
  sıfırlama `/account/forgot-password`.
- **Token'lar:** imzalı JWT access token (RFC 9068; şifresiz, çünkü çekirdek her sağlayıcıyı standart bir resource
  server gibi doğrular), **15 dk**; `sub`, `name`, `email`, `role` claim'leri, **izinler token'a girmez**. Issuer =
  `Jwt:Authority`, `aud` = `Jwt:Audience`.
- **Refresh:** 7 gün, her kullanımda döner (rolling). Kullanılmış bir refresh token OpenIddict'in 30 sn'lik payından
  sonra yeniden gelirse **o authorization'ın bütün token'ları iptal edilir** (hırsızlık tespiti). Her refresh'te hesap
  yeniden denetlenir: silinmiş, girişi engellenmiş ya da kilitli kullanıcı yeni token alamaz.
- **Temizlik:** açılışta ve saatte bir, 14 günden eski ve artık kullanılamayan kayıtlar silinir (OpenIddict'in
  `PruneAsync`'i, OpenIddict.Quartz'ın varsayılanları; yeni paket yok): süresi dolmuş, iptal edilmiş ya da kullanılmış
  token'lar ve token'ı kalmamış geçersiz authorization'lar. Geçerli kalıcı authorization kalır.
- **Anahtarlar:** Development'ta geçici (ephemeral) RSA. Diğer ortamlarda host sertifikaları OpenIddict'in kendi
  API'siyle ekler (`services.AddOpenIddict().AddServer(o => o.AddSigningCertificate(...).AddEncryptionCertificate(...))`);
  eksikse uygulama açılmaz. EC anahtarla ES256 da çalışır.
- **Client'lar** `Identity:Clients`'tan gelir: redirect URI'si olan client kullanıcı girişi yapar (public ya da
  secret'lı), olmayan client bir servistir (client credentials, secret zorunlu) ve `Permissions`'ında yazan izinleri
  taşır. Onay ekranı yoktur (birinci taraf client'lar).

### Harici sağlayıcıyla deneme (Keycloak, opsiyonel)
`docker compose -f docker-compose.yml -f docker-compose.keycloak.yml up -d keycloak` örnek `novacore` realm'ini içe
aktarır (`.env`'de `KEYCLOAK_ADMIN_PASSWORD` gerekir, bkz. `.env.example`; admin konsolu `http://localhost:8180/admin`).
Realm'de Scalar için public PKCE client'ı (`novacore-scalar`), `novacore.api` audience'ı ve realm rollerini `role`
claim'ine yazan mapper var; kullanıcı yok. Keycloak'ı Authority yapan bir host `Jwt:Authority =
http://localhost:8180/realms/novacore` (http yalnızca Development'ta) ayarlar, `AddCoreIdentityServer`'ı çağırmaz ve
Keycloak kullanıcıları için bir `IPermissionResolver` kaydeder.

### Hesaplar (authentication)
- **Parola saklama:** Identity PBKDF2-HMAC-SHA256, **600.000 iterasyon** (OWASP Password Storage).
- **Parola politikası:** uzunluk öncelikli — **min 12**, max 128, zorunlu kompozisyon kuralı yok
  (OWASP Authentication: "no composition rules").
- **Brute-force:** hesap kilidi (**5 başarısız → 15 dk**, hatalı 2FA ya da kurtarma kodu da sayılır), giriş sayfası, token ucu ve
  hesap API'sinde rate limiting (`strict`), bilinmeyen hesap / yanlış parola / kilitli hesap için **tek hata mesajı**.
  Bilinmeyen e-posta da bir parola hash'i doğrular (host'un hasher'ıyla bir kez üretilen sahte hash), böylece yanıt
  süresi hesabın varlığını ele vermez. `change-password`'da yanlış mevcut parola da lockout sayacını artırır; kilitli
  hesapta parola değişmez.
- **E-posta onayı** zorunlu (dev'de link loglanır; link `Jwt:Authority`'den üretilir, Host başlığından değil),
  **TOTP 2FA** (authenticator app). Her authenticator kodu bir kez geçer (ASVS 6.5.1): 2FA'yı açan kod girişte
  kullanılamaz, aynı kodla yarışan iki istekten biri girer. 2FA adımı `/2fa/enable`'ın verdiği 10 kurtarma kodundan
  birini de kabul eder: her kod bir kez kullanılır, kilitli hesap kodla da giremez, kullanılan kod
  `SECURITY recovery_code_used` (Warning) olarak loglanır. Kurtarma kodları veritabanında düz metin değil, PBKDF2
  hash'i olarak durur (ASVS 6.5.2). Onay linki `resend-confirmation-email` ile yeniden istenir; yalnızca onaylanmamış
  hesaba gider, yanıt her adres için aynıdır.
- **Parola sıfırlama:** giriş sayfasındaki "Forgot your password?" linki bir e-posta formu açar. Onaylı hesaba
  `/account/reset-password` linki gider (Identity token'ı, 6 saat, tek kullanımlık: sıfırlama security stamp'i
  yeniler). Bilinmeyen ya da onaysız adres aynı sayfayı alır, e-posta gitmez. Yeni parola iki kez girilir; sıfırlama
  bütün refresh token'ları ve authorization'ları iptal eder, otomatik giriş yapmaz (OWASP Forgot Password).
- **Seed'siz kayıt:** `User` rolü yoksa (seed kapalı) hesap rolsüz oluşur; admin API'si rolü sonradan atar.
- **Oturumlar:** parola değişikliği ve "her yerden çıkış" bütün refresh token'ları ve authorization'ları iptal eder ve
  sağlayıcıdaki giriş oturumunu hemen bitirir. Verilmiş access token'lar süreleri dolana kadar (15 dk) geçerlidir.

### Yetkilendirme (authorization) — permission-based
- Roller (Identity) **+ ince taneli izinler**. İzinler `module:resource:action` (ör. `catalog:products:create`).
- **Varsayılan kapalı:** yetki meta verisi olmayan bir uç ve hiçbir uca eşleşmeyen istek kimlik doğrulama ister
  (`FallbackPolicy = RequireAuthenticatedUser`, MS'in "tercih edilen yol" dediği yöntem). `RequirePermission`'ı unutulan
  bir uç böylece herkese açılmaz (OWASP API5). Anonim olması gereken uç bunu `AllowAnonymous()` ile söyler: kimlik
  sağlayıcının `/connect/*` uçları, giriş ve parola sıfırlama sayfaları, kayıt ve e-posta onayı, health uçları,
  OpenAPI belgesi ve Scalar.
  Bir sonucu: tanımsız bir yola anonim istek 404 yerine 401 alır.
- `RequirePermissionAttribute` (`AuthorizeAttribute` + `IAuthorizationRequirementData`) kendi `PermissionRequirement`'ını
  taşır, `PermissionAuthorizationHandler` onu denetler. Özel policy provider ya da izin başına bir named policy yok.
  Kullanım: `endpoint.RequirePermission("catalog:products:create")` veya handler'da `[RequirePermission(...)]`.
  Anonim istek 401, izni olmayan istek 403 alır.
- İzinler **sunucu tarafında çözülür**, token'dan okunmaz. Bir `IClaimsTransformation` token'daki `permission`
  claim'lerini atar ve `IPermissionResolver`'ın döndürdüklerini ekler. Kimlik sağlayıcı eklentisi bunları kullanıcının
  rollerinden ve doğrudan izinlerinden, servis client'ları için `Identity:Clients`'tan okur. Sonuç HybridCache'te `sub` + security stamp anahtarıyla
  `Cache:PermissionExpiration` (varsayılan 5 dk) boyunca tutulur: bir rol değişikliği oturum açmış kullanıcıya en geç bu
  sürede yansır, yeni bir security stamp'li token cache'i atlar. Girdiler `sub`'a göre tag'lidir: izni değiştiren kod
  `HybridCache.RemovePermissionsAsync(sub)` ile onları hemen düşürür (bu sunucunun belleği ve dağıtık cache; başka
  sunucuların belleği süre dolunca). Kimlik eklentisinin admin API'si her rol değişikliğinde bunu yapar ve kullanıcının
  refresh token'larını iptal eder. Host'ta bir `IPermissionResolver` kayıtlı değilse
  uygulama açılmaz. Token'daki hiçbir rol izin kontrolünü atlatmaz. Kimlik eklentisinde `Administrator` rolünün üyesi,
modüllerin tanımladığı bütün izinleri sunucuda alır (seed o izni role yazmamış olsa da), 2FA'sı açıksa. Veritabanında doğrudan
rolünden çıkarılan bir admin de izinlerini önbellek süresi dolunca kaybeder. `Administrator` rolünü vermek ya da almak
`identity:users:manage`'e ek olarak `identity:roles:manage` ister (yalnızca admin'de var); son admin rolünü kaybedemez
(409).
- **Her modül kendi izinlerini** `IPermissionDefinitionProvider` ile tanımlar; seeder hepsini toplayıp
  rollere bağlar (`User` rolü = iş modüllerinin yalnızca `:read` izinleri — least privilege).

### Endpoint'ler
| Endpoint | Açıklama |
|---|---|
| `GET·POST /connect/authorize` | Authorization Code + PKCE (oturum yoksa `/account/login`'e yönlendirir) |
| `POST /connect/token` | Code, refresh token (rotation + reuse detection) ve client credentials grant'ları |
| `POST /connect/revoke` · `GET·POST /connect/endsession` | Token iptali / sağlayıcıdan çıkış (oturumlu tarayıcıya önce antiforgery'li bir onay formu sorulur: logout CSRF) |
| `POST /api/v1/auth/register` | Kayıt (anti-enumeration generic yanıt) + onay e-postası |
| `GET /api/v1/auth/confirm-email` | E-posta onayı |
| `POST /api/v1/auth/resend-confirmation-email` | Onay linkini yeniden gönder (yalnızca onaysız hesaba, generic yanıt) |
| `GET·POST /account/forgot-password` · `reset-password` | Parola sıfırlama sayfaları (generic yanıt, link e-postayla) |
| `POST /api/v1/auth/logout-all` | Tüm oturumları kapat |
| `GET /api/v1/auth/me` | Mevcut principal (id, email, roller, izinler) |
| `POST /api/v1/auth/change-password` | Parola değiştir (tüm oturumları iptal eder) |
| `POST /api/v1/auth/2fa/setup` · `enable` · `disable` | TOTP iki-faktör; üçü de mevcut parolayı ister (`enable`/`disable` ayrıca kodu). 2FA açıkken `setup` 409 döner (anahtar gizli kalır), `enable` yeni kurtarma kodları verir. Açma ve kapama bütün oturumları bitirir |
| `GET /api/v1/admin/users` · `roles` · `permissions` | Listeleme (permission korumalı) |
| `POST·DELETE /api/v1/admin/users/{id}/roles` | Rol ata/kaldır (`identity:users:manage`; `Administrator` için ayrıca `identity:roles:manage`, son admin korunur). İzinler hemen yeniden çözülür, kullanıcının oturumları biter |

### Ek sertleştirme
- **Reverse proxy arkasında:** `UseCoreWeb` pipeline'ın başında `UseForwardedHeaders`'ı çalıştırır. Yalnızca
  `ForwardedHeaders:KnownProxies`/`KnownIPNetworks`'te listelenen kaynakların `X-Forwarded-For` ve `X-Forwarded-Proto`
  başlıkları uygulanır; böylece rate limiter gerçek istemciyi ayırır, istek şeması (ve `Secure` cookie'ler) https olur.
  Liste boşsa hiç kimsenin başlığı uygulanmaz (framework varsayılanı, loopback dahil). `X-Forwarded-Host` işlenmez:
  public adres `Jwt:Authority`'den gelir.
- **Security headers** (her yanıtta): `X-Content-Type-Options`, `X-Frame-Options: DENY`,
  `Referrer-Policy`, `Content-Security-Policy: default-src 'none'`, `Server` başlığı kaldırılır; prod'da **HSTS**.
- **Audit logging:** giriş (başarılı/başarısız), lockout, 2FA, refresh, çıkış, parola değişimi ve sıfırlama, oturum
  iptali, rol atama; `SECURITY` önekli, e-posta maskeli, parola/kod/token loglanmaz. Refresh token tekrar kullanımını OpenIddict
  kendisi loglar.
- **Seed** (`Identity:Seed:SeedOnStartup`): `Administrator` rolü tüm beyan edilmiş izinleri, `User` rolü iş modüllerinin
  `:read` izinlerini alır; opsiyonel admin (parola user-secrets ile) ve `Identity:Clients`'taki client'lar.
- **Admin için 2FA zorunlu:** `Administrator` rolü yalnızca 2FA'sı açık hesapta sayılır (çözümleyicide; rolün kendi
  izinleri dahil). Seed admin parolayla girer, 2FA'yı API'den açar; açma ve kapama izin önbelleğini hemen düşürür.

> **Üretimde mutlaka:** `Jwt:Authority`'yi host'un public https adresine (ya da harici sağlayıcıya) ayarla, imza ve
> şifreleme sertifikalarını host'ta ekle, TLS/HTTPS'i edge'de zorla, gerçek bir `IEmailSender` (SMTP/SendGrid) tak,
> admin parolasını ve client secret'larını user-secrets/ortam değişkeniyle ver, bir **Data Protection anahtar deposu**
> seç (kalıcı volume'da `PersistKeysToFileSystem`, `PersistKeysToDbContext` ya da blob/Redis). Giriş cookie'leri,
> antiforgery token'ları ve onay linkleri bu anahtarlarla korunur; framework'ün varsayılan deposu container
> yenilenince kaybolur ve her örnekte farklıdır. `LoggingEmailSender` yalnızca Development'ta kayıtlıdır; diğer
> ortamlarda `IEmailSender`, imza anahtarları ya da anahtar deposu eksikse uygulama açılışta durur.

**Kaynaklar:** [OWASP Authentication CS](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html) ·
[OWASP Password Storage CS](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html) ·
[OWASP OAuth2 CS](https://cheatsheetseries.owasp.org/cheatsheets/OAuth2_Cheat_Sheet.html) ·
[RFC 9700 — OAuth 2.0 Security BCP](https://www.rfc-editor.org/rfc/rfc9700) ·
[OpenIddict documentation](https://documentation.openiddict.com/) ·
[MS Learn — Policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0) ·
[MS Learn — Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0)

---

## Konfigürasyon

`appsettings.json` (özet):

```jsonc
{
  "ConnectionStrings": { "Database": "Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;" },
  "Database":      { "ApplyMigrations": false },   // true: açılışta migration uygula
  "Jwt":           { "Authority": "", "Audience": "novacore.api" },   // Development: https://localhost:5443 (bu host)
  "Identity":      { "Seed": { "SeedOnStartup": false, "AdminEmail": "admin@novacore.local", "AdminPassword": "" },
                     "Clients": [] },                // { ClientId, DisplayName, ClientSecret, RedirectUris, PostLogoutRedirectUris, Permissions }
  "Cache":         { "DefaultExpiration": "00:05:00", "PermissionExpiration": "00:05:00" },
  "RateLimiting":  { "PermitLimit": 100, "WindowSeconds": 60 },   // giriş ve hesap uçları 1/10'unu kullanır ("strict")
  "Cors":          { "AllowedOrigins": [] },        // boş = Development'ta her origin (credentials olmadan), diğer ortamlarda hiçbiri
  "ForwardedHeaders": { "KnownProxies": [], "KnownIPNetworks": [] },  // reverse proxy'nin IP'leri / CIDR ağları; boş = başlıklar işlenmez
  "Seq":           { "ServerUrl": "" }              // dolu = Seq'e log gönder
}
```
Çevre değişkeni `OTEL_EXPORTER_OTLP_ENDPOINT` doluysa log, trace ve metrikler OTLP ile dışa aktarılır (ör. Aspire Dashboard, bir OTel collector).

> `Jwt:Authority` her ortamda zorunludur; Development dışında mutlak bir `https` URI olmalıdır.
> Gizli değerler (admin parolası, client secret'ları) için `dotnet user-secrets` ya da ortam değişkenleri kullanın.

Bu bölümler options pattern ile bağlanır ve **uygulama açılırken doğrulanır** (`ValidateOnStart`). Geçersiz bir değer
host'u, ayarın adını veren bir hatayla durdurur:

- `Jwt`: `Authority` her ortamda zorunlu mutlak bir URI (Development dışında `https`), `Audience` boş olamaz.
- `Identity:Clients`: `ClientId` tekil ve Guid değil; redirect URI'leri mutlak; redirect URI'si olmayan (servis)
  client'ın secret'ı olmalı; `Permissions` yalnızca servis client'larında.
- `RateLimiting`: `PermitLimit` ve `WindowSeconds` ≥ 1. `Cache:DefaultExpiration` ve `Cache:PermissionExpiration`
  pozitif birer süre.
- `Cors:AllowedOrigins`: her giriş çıplak bir origin olmalı (`https://app.example.com`): yol, sondaki `/` ve `*` yok.
- `ForwardedHeaders:KnownProxies`: IP adresleri (IPv4 dört bölümüyle, `10.0.0` gibi kısaltma yok);
  `ForwardedHeaders:KnownIPNetworks`: CIDR ağları (`10.0.0.0/8`, host bitleri sıfır).
- `Database:ApplyMigrations`: `true`/`false`.

Değerler kayıt anında değil, ilk kullanımda okunur. Bu yüzden `CreateBuilder`'dan sonra eklenen config (ör. testlerde
`ConfigureAppConfiguration`) da geçerlidir.

---

## Veritabanı & Migration

Her modül **kendi DbContext'ine + kendi SQL Server şemasına** sahiptir (`catalog` / `payments`,
`HasDefaultSchema`), migration geçmişi de şema bazında ayrılır (`__EFMigrationsHistory`). Kimlik sağlayıcının
`IdentityServerDbContext`'i (`identity` şeması) eklenti paketinden gelir; sağlayıcıyı ve migration'ları host seçer, bu
yüzden migration'ları Api projesindedir (`src/Api/NovaCore.Api/Identity/Migrations`). Sınırlar veritabanı düzeyinde
uygulanır. Her modül şemasında outbox tabloları da vardır: `outbox_messages` ve `outbox_consumers`.

```bash
# Yeni migration (modül = hem -p hem -s; design-time factory sayesinde DB gerekmez)
dotnet ef migrations add <Ad> --project src/Modules/Catalog/NovaCore.Modules.Catalog --context CatalogDbContext --startup-project src/Modules/Catalog/NovaCore.Modules.Catalog --output-dir Infrastructure/Migrations

# Uygula
dotnet ef database update --project src/Modules/Catalog/NovaCore.Modules.Catalog --context CatalogDbContext --startup-project src/Modules/Catalog/NovaCore.Modules.Catalog

# Kimlik sağlayıcının migration'ları Api'de
dotnet ef migrations add <Ad> --project src/Api/NovaCore.Api --context IdentityServerDbContext --startup-project src/Api/NovaCore.Api --output-dir Identity/Migrations
```
Alternatif: `Database:ApplyMigrations=true` ile uygulama açılışta tüm modül migration'larını otomatik uygular
(`DatabaseMigrationService`).

---

## Test

```bash
dotnet test
```
- **Unit:** `Result<T>`, `PaginationRequest`/`PagedList<T>` davranışları.
- **Integration:** gerçek host'u `WebApplicationFactory` ile ayağa kaldırır; her DbContext testte **in-memory
  SQLite**'a takılır. Token'ları bellek içi bir OpenID Connect issuer'ı (`TestIdentityProvider`) üretir.
- **Outbox:** event'in entity ile aynı transaction'da yazıldığı (iki yönlü rollback) ve bir mesajın her handler'a bir
  kez teslim edildiği test edilir. Dispatcher testleri sahte saatle (`FakeTimeProvider`) çalışır: yeniden deneme,
  10 denemede bırakma ve retrying execution strategy altında çalışma da bu testlerde.
- **Stok rezervasyonu:** Catalog ve Payments handler'ları doğrudan çağrılarak test edilir: rezervasyon, üç ret
  nedeni, tekrar teslimde değişiklik olmaması, ödemenin bir kez sonuçlanması. Bir test de akışın tamamını Api host'unda
  gerçek dispatcher'la sahte saati ilerleterek dener (POST'tan Completed'a).
- **Eşzamanlılık:** son birim için aynı anda gelen iki ödemeden biri Completed, biri Failed olur ve stok 0'da kalır.
  Bir test interceptor'ı başka bir yazarı taklit eder: okunduktan sonra değişen stok, ürün uçlarında 409 döndürür.
  Rezervasyonun teslimi ise düşer ve güncel stokla yeniden denenir.
- **Kimlik sağlayıcı:** eklenti, yalnızca çekirdek + eklentiden kurulan bir host'ta (`IdentityServer/`) uçtan uca test
  edilir: Authorization Code + PKCE, refresh **rotation** ve **reuse** (zincir iptali), client credentials, sertleştirmeler,
  2FA, oturum uçları, admin API'si. `IdentityServerWiringTests` aynı akışı Api host'unun kendisinde dener.
- **Architecture:** modül sınırlarını zorlar (modüller birbirine yalnızca `*.Contracts` üzerinden bağlı), çekirdek paketlerin
  yalnızca Microsoft paketlerine bağlı olduğunu ve paketler arası bağımlılık yönünü doğrular (`NovaCore.Abstractions`
  ASP.NET/EF içermez), building block'larda assembly taraması olmadığını kontrol eder. Modül sırası ve endpoint listesi
  de testle sabitlenmiştir. `WorkflowPinTests` her GitHub Action'ın bütün workflow'larda (şablonunki dahil) tek ve tam
  bir commit SHA'sına sabitlendiğini doğrular.
- **Şablon (tüketici smoke testi):** `bash eng/template-smoke.sh` ve CI'daki `template-smoke` job'ı bütün paketleri yerel
  bir feed'e pack eder, şablonu kendi hive'ına kurar, `--auth BuiltIn` ve `External` ile birer proje üretir; projeler
  paketleri o feed'den alarak (repo'nun projelerinden değil) uyarılar hata sayılarak derlenir, testleri ve
  `has-pending-model-changes` çalışır, içlerinde repo'ya özgü dosya olmadığı kontrol edilir.

---

## Çekirdeği Dağıtma

İki tamamlayıcı yol:

1. **`dotnet new` template (birincil):** `BMD.NovaCore.Templates` şablon paketi (`src/Templates/NovaCore.Templates`)
   iskeleti paketlere bağlı olarak üretir: Api host, örnek `Todos` modülü, test projesi, CI. Repo'nun kendi dosyaları
   (BuildingBlocks kaynağı, plan belgeleri, `.claude/`, `release.yml`, PublicAPI dosyaları) şablona girmez. Kurulum ve
   kullanım: [Yeni Proje (Tüketici Rehberi)](#yeni-proje-tüketici-rehberi).
   ```bash
   dotnet new install BMD.NovaCore.Templates::0.1.0-preview.1
   dotnet new novacore -n YeniProje --auth BuiltIn   # ya da External: yalnızca Jwt:Authority, kimlik sağlayıcı yok
   ```
2. **Lokal/özel NuGet paketi (BuildingBlocks için):** yalnızca stabil çekirdeği versiyonlu paketleyip
   bir bug fix'i tüm projelere yayın.
   ```bash
   dotnet pack NovaCore.slnx -c Release -o ./artifacts                               # 0.1.0
   dotnet pack NovaCore.slnx -c Release -o ./artifacts --version-suffix preview.1    # 0.1.0-preview.1
   # artifacts/ klasörünü lokal feed olarak ekleyin, sonra: dotnet add package BMD.NovaCore.AspNetCore (ve ihtiyaca göre diğerleri)
   ```
   - Yedi kütüphane paketi ve şablon paketi üretilir: çekirdek `BMD.NovaCore.Abstractions`, `BMD.NovaCore.AspNetCore`,
     `BMD.NovaCore.EntityFrameworkCore`; eklentiler `BMD.NovaCore.Serilog`, `BMD.NovaCore.OpenTelemetry`,
     `BMD.NovaCore.Scalar`, `BMD.NovaCore.Identity.OpenIddict`; şablon `BMD.NovaCore.Templates`. Modüller paketlenmez.
   - **Paket ID'leri `BMD.` önekini taşır, içlerindeki assembly ve namespace'ler `NovaCore.*`'dır** (`BMD.NovaCore.AspNetCore`
     → `using NovaCore.AspNetCore;`). nuget.org'da başka sahiplerin `NovaCore.*` paketleri var (`NovaCore.AspNetCore`,
     `NovaCore.Templates` dahil); paket ID'si her yerde benzersiz olmalı (DECISIONS D-041). Üretilen projenin
     `nuget.config`'i `BMD.NovaCore.*`'ı yalnızca GitHub feed'ine eşler (package source mapping).
   - Sürüm `Directory.Build.props`'taki `VersionPrefix`'ten gelir. API kararlı olana kadar 0.x kalır.
   - Her paket XML dokümanını, `src/PACKAGE.md`'den gelen README'yi, MIT lisans ifadesini ve repo linkini taşır. Yanında bir `.snupkg` sembol paketi üretilir.
   - Public API `PublicAPI.*.txt` dosyalarında izlenir. İzlenmeyen bir değişiklik build'i kırar.
   - Yayın `.github/workflows/release.yml` ile yapılır. `VersionPrefix` ile eşleşen bir `v*` tag'i (ör. `v0.1.0-preview.1`) GitHub Packages'a push eder.
     `build` job'u yalnızca okuma izniyle derler, test eder, pack eder ve kütüphane başına bir CycloneDX SBOM'u üretir.
     `publish` job'u `release` environment'ında sahibinin onayını bekler; paketlerin build provenance'ını imzalar
     (`actions/attest`) ve push eder (DECISIONS D-051 (6)).

---

## Paket / Lisans Tablosu

| Paket | İşlev | Lisans |
|---|---|---|
| Mapperly (Riok) | Object mapping (source-gen) | **Apache-2.0** |
| Scalar.AspNetCore | OpenAPI arayüzü (`NovaCore.Scalar` eklentisi) | **MIT** |
| Serilog.AspNetCore | Structured logging (`NovaCore.Serilog` eklentisi) | **Apache-2.0** |
| OpenTelemetry.* | Observability (`NovaCore.OpenTelemetry` eklentisi) | **Apache-2.0** |
| Microsoft.AspNetCore.OpenApi / Caching.Hybrid / JwtBearer / Http.Resilience | Microsoft-yerleşik (`NovaCore.AspNetCore`) | **MIT** |
| Microsoft.EntityFrameworkCore.Relational / Extensions.Hosting.Abstractions / Options.ConfigurationExtensions / Diagnostics.HealthChecks.EntityFrameworkCore | Microsoft-yerleşik (`NovaCore.EntityFrameworkCore`) | **MIT** |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Kullanıcı/rol deposu (`NovaCore.Identity.OpenIddict`) | **MIT** |
| OpenIddict.Server.AspNetCore / OpenIddict.EntityFrameworkCore | OpenID Connect sağlayıcı (`NovaCore.Identity.OpenIddict` eklentisi) | **Apache-2.0** |
| Microsoft.EntityFrameworkCore.SqlServer | EF Core sağlayıcı | **MIT** |
| Microsoft.CodeAnalysis.PublicApiAnalyzers | Public API takibi (yalnızca build, pakete girmez) | **MIT** |
| Polly.Core / Extensions / RateLimiting | Geçişli: `Microsoft.Extensions.Http.Resilience`'ın bağımlılığı | **BSD-3-Clause** (adlı istisna, D-052) |
| nuget-license, CycloneDX (dotnet tool) | Lisans kapısı, SBOM (yalnızca CI ve yayın, pakete girmez) | **Apache-2.0** |

CI'daki lisans kapısı (`eng/check-licenses.sh`) geçişliler dahil her paketi denetler: MIT ya da Apache-2.0 olmalı;
Microsoft paketleri (`Microsoft.*`, `System.*`) lisansa bakılmadan geçer (Y-06), getirdikleri paketler yine denetlenir.

> ⚠️ **MediatR (v13+) ve AutoMapper (v15+) artık ticari** (Lucky Penny). NovaCore bunları kullanmaz. Başka bir mediator
> ya da request-dispatch soyutlaması da eklenmez (DECISIONS Y-01): handler'lar bağımlılıklarını minimal API DI'dan alır.

---

## Kararı Değiştirecek Eşikler

- **Tam Clean Architecture'a geç** → yalnızca bir modül gerçekten karmaşık DDD/invariant içeriyorsa, sadece o modül için.
- **Redis L2 cache ekle** → bir `IDistributedCache` (Redis) kaydet; HybridCache otomatik benimser.
- **Harici bir IdP'ye geç (Keycloak, Entra)** → `Jwt:Authority`'yi ona çevir ve o IdP'nin kullanıcıları için bir
  `IPermissionResolver` kaydet; çekirdek değişmez.
- **Runtime .dll eklenti mimarisi** → üçüncü partiler senin derlemen olmadan modül yüklemeliyse (tek-geliştirici için neredeyse hiç gerekmez).

---

### Notlar
- .NET 10 LTS'tir (Kasım 2025 → ~Kasım 2028).
- HybridCache .NET 9'dan beri GA/production-ready. `Microsoft.Extensions.Caching.Hybrid` 10.0.0 artık deneysel
  uyarısı (EXTEXP0018) üretmiyor, bu yüzden bastırma da yok.
- Modüller açıkça kaydedilir (`AddModule<TModule>()`); assembly taranmaz.
