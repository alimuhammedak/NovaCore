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
- [Hızlı Başlangıç](#hızlı-başlangıç)
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

## Hızlı Başlangıç

Gereksinim: **.NET 10 SDK** (10.0.300+), **SQL Server** (yerel instance, Windows auth — bkz. `appsettings.json`).

```bash
# 1) Bağımlılıkları derle + test et (veritabanı GEREKMEZ — testler SQLite kullanır)
dotnet build
dotnet test

# 2) Migration'ları uygula (modül başına ayrı şema: catalog / payments / identity)
dotnet tool restore
dotnet ef database update --project src/Modules/Catalog/NovaCore.Modules.Catalog   --context CatalogDbContext   --startup-project src/Modules/Catalog/NovaCore.Modules.Catalog
dotnet ef database update --project src/Modules/Payments/NovaCore.Modules.Payments  --context PaymentsDbContext  --startup-project src/Modules/Payments/NovaCore.Modules.Payments
dotnet ef database update --project src/Api/NovaCore.Api  --context IdentityServerDbContext  --startup-project src/Api/NovaCore.Api

# 3) İlk admin + roller + izinler için seed (parolayı user-secrets ile ver — repoya YAZMA)
dotnet user-secrets --project src/Api/NovaCore.Api set "Identity:Seed:AdminPassword" "Admin!Passw0rd-2026"

# 4) Çalıştır (Development: Jwt:Authority boşken geliştirme anahtarı devreye girer; prod'da Jwt:Authority zorunlu)
Identity__Seed__SeedOnStartup=true dotnet run --project src/Api/NovaCore.Api
```

Açılınca tarayıcı **Scalar API arayüzünü** (`/scalar/v1`) açar. Bağlantı dizesi (varsayılan):
`Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;`

| Uç nokta | Açıklama |
|---|---|
| `GET /health` | Readiness: tüm health check'ler, her modülün DB bağlantısı dahil (yalnızca Development) |
| `GET /alive` | Liveness: yalnızca "live" etiketli (yalnızca Development) |
| `GET /openapi/v1.json` | OpenAPI 3.1 dokümanı |
| `/scalar/v1` | Scalar API arayüzü (yalnızca Development) |
| `POST /api/v1/auth/register` · `login` · `refresh` · `logout` · `me` | Kimlik doğrulama akışı (bkz. [Güvenlik](#güvenlik--yetkilendirme)) |
| `/api/v1/auth/2fa/*` | TOTP iki-faktör (setup/enable/disable) |
| `/api/v1/admin/*` | Kullanıcı/rol/izin yönetimi (permission korumalı) |
| `/api/v1/products` | Catalog modülü (CRUD, permission korumalı) |
| `/api/v1/payments` | Payments modülü (Catalog'a contract üzerinden bağlı) |

> SQL Server olmadan da uygulama açılır ve `/openapi`, validation/401 kapıları çalışır. `/health` bu durumda
> `Unhealthy` (503) döner, çünkü modül veritabanlarına ulaşamaz. DB'ye dokunan endpoint'ler düzgün bir
> **RFC 9457 ProblemDetails** ile yanıt verir.
>
> `/health` ve `/alive` yalnızca Development'ta map'lenir (DECISIONS D-022). Production'da probe gerekiyorsa host
> kendi `MapHealthChecks` kaydını yapar. `RequireHost` Host header'ına güvendiği için sahtelenebilir; port kontrolü
> `ConnectionInfo.LocalPort` ile yapılmalıdır.

### 5 dakikada yeni proje (template)
```bash
dotnet new install .                 # bu repoyu template olarak kur
dotnet new novacore -n SiparisServisi
```

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
| JWT bearer auth + permission-based authz altyapısı | Identity *modülü* (users/roles/tokens) |
| Auditing interceptor, domain-event dispatcher | |

---

## Proje Ağacı

```
NovaCore.slnx
├── Directory.Build.props          # tüm projeler: net10.0, nullable, analyzer'lar, TreatWarningsAsErrors, VersionPrefix
├── Directory.Build.targets        # IsPackable projeler: paket metadata'sı, README, PublicAPI dosyaları, doc/analyzer korumaları
├── Directory.Packages.props       # merkezî paket sürümleri (CPM)
├── docker-compose.yml             # opsiyonel SQL Server + Seq + Jaeger
├── .template.config/template.json # `dotnet new novacore`
│
├── src/
│   ├── BuildingBlocks/
│   │   ├── NovaCore.Abstractions/        # Result, PagedList, Entity, IDomainEvent/DomainEvent, ICurrentUser (yalnızca BCL)
│   │   ├── NovaCore.AspNetCore/          # IModule + AddModule<T>, AddCoreDefaults/AddCoreWeb, ProblemDetails, Result→HTTP, JWT, authz, OpenAPI, HybridCache
│   │   ├── NovaCore.EntityFrameworkCore/ # AddModuleDbContext, AddCorePersistence: auditing, startup migration, domain-event dispatcher
│   │   ├── NovaCore.Serilog/             # eklenti: AddCoreLogging (Console + opsiyonel Seq)
│   │   ├── NovaCore.OpenTelemetry/       # eklenti: AddCoreObservability (metrics + tracing, OTLP)
│   │   └── NovaCore.Scalar/              # eklenti: MapCoreScalar (Scalar UI, yalnızca Development)
│   │
│   ├── Modules/
│   │   ├── Catalog/
│   │   │   ├── NovaCore.Modules.Catalog.Contracts/  # IProductCatalog + ProductInfo (dışa açılan tek yüzey)
│   │   │   └── NovaCore.Modules.Catalog/            # CatalogModule, Domain, Features/, Infrastructure/ (schema: catalog)
│   │   └── Payments/
│   │       └── NovaCore.Modules.Payments/           # PaymentsModule (schema: payments) — Catalog'a Contracts ile bağlı
│   │
│   └── Api/
│       └── NovaCore.Api/           # Program.cs: composition root, her şeyi açıkça ekler
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
builder.AddCorePersistence();                      // audit interceptor, domain event'ler, opt-in startup migration

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

1. `dotnet new classlib -o src/Modules/Orders/NovaCore.Modules.Orders` (isim `NovaCore.Modules.` ile başlamalı;
   mimari testler modülleri bu önekle bulur)
2. `NovaCore.AspNetCore` ve `NovaCore.EntityFrameworkCore`'a referans ver. Validation için paket gerekmez, ASP.NET Core
   framework'ü getiriyor.
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

---

## Cross-Cutting Concern'ler

| Concern | Yaklaşım (.NET 10) | NovaCore'da |
|---|---|---|
| **Exception handling** | `IExceptionHandler` + `AddProblemDetails` (RFC 9457) | `GlobalExceptionHandler` |
| **Logging** | Serilog (structured) + OTel korelasyon | `AddCoreLogging` (`NovaCore.Serilog` eklentisi) — Console + opsiyonel Seq |
| **Tracing/Metrics** | OpenTelemetry | `AddCoreObservability` (`NovaCore.OpenTelemetry` eklentisi) — OTLP yalnızca endpoint konfigüre ise |
| **Caching** | HybridCache (L1 + opsiyonel L2) | `HybridCache` doğrudan enjekte edilir (zaten soyut bir sınıf, sarmalayıcı yok). `AddCoreCaching` varsayılan süreyi `Cache:DefaultExpiration`'dan alır |
| **Validation** | .NET 10 yerleşik validation (`AddValidation`, DataAnnotations) | Her modül `AddModule`'de `services.AddValidation()` çağırır; kurallar request record'unda |
| **Mapping** | Mapperly (source-gen) | `[Mapper]` statik partial sınıflar, EF-translatable `ProjectToDto` |
| **Auth** | ASP.NET Core Identity + JWT/refresh + permission-based | bkz. [Güvenlik & Yetkilendirme](#güvenlik--yetkilendirme) |
| **Auditing** | EF Core `SaveChangesInterceptor` | `AuditableEntityInterceptor` — CreatedBy/ModifiedAt merkezî |
| **Rate limiting** | Yerleşik `AddRateLimiter` | IP bazlı fixed-window + `"strict"` adlı policy |
| **Health** | Yerleşik Health Checks + `AddDbContextCheck` | `/health` (her modül DB'si, "ready" etiketi) + `/alive`, yalnızca Development |
| **API docs** | `Microsoft.AspNetCore.OpenApi` + Scalar | Bearer security scheme transformer ile; Scalar UI `NovaCore.Scalar` eklentisinde (`MapCoreScalar`) |
| **Modül kaydı** | Açık kayıt | `builder.AddModule<TModule>()` (assembly taraması yok) |
| **Result** | Result pattern | Sıfır-bağımlılık `Result<T>` + `ToHttpResult()` |

`Result<T>` **beklenen** hatalar (NotFound/Validation/Conflict) için; gerçek istisnai durumlar için
exception. Exception'ı akış kontrolü için kullanmaktan kaçınılır.

---

## Güvenlik & Yetkilendirme

ASP.NET Core Identity (SQL Server, `identity` şeması) + JWT access/refresh + permission-based authz.
Tasarım, OWASP cheat sheet'lerine ve Microsoft Learn'e göre sertleştirilmiştir (kaynaklar aşağıda).

### Kimlik doğrulama (authentication)
- **Parola saklama:** Identity PBKDF2-HMAC-SHA256, **600.000 iterasyon** (OWASP Password Storage).
- **Parola politikası:** uzunluk öncelikli — **min 12**, max 128, zorunlu kompozisyon kuralı yok
  (OWASP Authentication: "no composition rules").
- **Brute-force:** hesap kilidi (**5 başarısız → 15 dk**), `/auth` grubunda rate limiting (`strict`),
  **generic 401** (user enumeration önleme — "Invalid credentials").
- **Email confirmation** zorunlu (dev'de link loglanır), **TOTP 2FA** (authenticator app, iki adımlı login).

### Token mimarisi
- **Doğrulama (çekirdek):** API access token'ı bir OpenID Connect sağlayıcısına karşı doğrular: `Jwt:Authority`.
  Issuer ve imza anahtarları sağlayıcının metadata'sından ve JWKS'inden keşfedilir, yalnızca asimetrik algoritmalar
  (RS/PS/ES) kabul edilir. Simetrik `Jwt:SigningKey` (HS256) **yalnızca Development'ta** geçerlidir; başka ortamda host
  açılmaz (MS JWT rehberi: *"Asymmetric keys should always be used"*).
- **Identity modülünün kendi token'ları** (eski yol, F3b.3'te emekliye ayrılacak): kısa ömürlü **access JWT** (HS256,
  15 dk) — `sub`, `name`, `email`, `role*` claim'leri (izinler token'a girmez, aşağıya bakın). HS256 olduğu için
  yalnızca Development'ta ve testlerde doğrulanır.
- Uzun ömürlü **refresh token** (256-bit, **DB'de SHA-256 hash'li**, 7 gün).
- **Rotation:** her `/auth/refresh`'te yeni refresh, eskisi iptal.
- **Reuse detection + family revocation:** tüketilmiş bir refresh tekrar gelirse **tüm token ailesi iptal**
  + `ERR refresh_token_reuse_detected` audit log'u (hırsızlık tespiti — OWASP OAuth2).
- **JWT validation sertleştirme:** algoritma listesi sabit (asimetrik; HS256 yalnızca Development anahtarıyla),
  issuer/audience zorunlu, `ClockSkew = 30s`, `MapInboundClaims = false`, prod'da `RequireHttpsMetadata = true`.

### Yetkilendirme (authorization) — permission-based
- Roller (Identity) **+ ince taneli izinler**. İzinler `module:resource:action` (ör. `catalog:products:create`).
- `RequirePermissionAttribute` (`AuthorizeAttribute` + `IAuthorizationRequirementData`) kendi `PermissionRequirement`'ını
  taşır, `PermissionAuthorizationHandler` onu denetler. Özel policy provider ya da izin başına bir named policy yok.
  Kullanım: `endpoint.RequirePermission("catalog:products:create")` veya handler'da `[RequirePermission(...)]`.
  Anonim istek 401, izni olmayan istek 403 alır.
- İzinler **sunucu tarafında çözülür**, token'dan okunmaz. Bir `IClaimsTransformation` token'daki `permission`
  claim'lerini atar ve `IPermissionResolver`'ın döndürdüklerini ekler. Identity modülü bunları kullanıcının rollerinden
  ve doğrudan izinlerinden okur (`IdentityPermissionResolver`). Sonuç HybridCache'te `sub` + security stamp anahtarıyla
  `Cache:PermissionExpiration` (varsayılan 5 dk) boyunca tutulur: bir rol değişikliği oturum açmış kullanıcıya en geç bu
  sürede yansır, yeni bir security stamp'li token cache'i atlar. Host'ta bir `IPermissionResolver` kayıtlı değilse
  uygulama açılmaz. `Administrator` rolü (token'daki rol claim'i) super-admin'dir, tüm kontrolleri geçer.
- **Her modül kendi izinlerini** `IPermissionDefinitionProvider` ile tanımlar; seeder hepsini toplayıp
  rollere bağlar (`User` rolü = iş modüllerinin yalnızca `:read` izinleri — least privilege).

### Endpoint'ler
| Endpoint | Açıklama |
|---|---|
| `POST /api/v1/auth/register` | Kayıt (anti-enumeration generic yanıt) + onay e-postası |
| `GET /api/v1/auth/confirm-email` | E-posta onayı |
| `POST /api/v1/auth/login` · `login/2fa` | Giriş (gerekirse 2FA ile) |
| `POST /api/v1/auth/refresh` | Token yenileme (rotation + reuse detection) |
| `POST /api/v1/auth/logout` · `logout-all` | Oturum / tüm oturumları kapat |
| `GET /api/v1/auth/me` | Mevcut principal (id, email, roller, izinler) |
| `POST /api/v1/auth/change-password` | Parola değiştir (tüm oturumları iptal eder) |
| `POST /api/v1/auth/2fa/setup` · `enable` · `disable` | TOTP iki-faktör |
| `GET /api/v1/admin/users` · `roles` · `permissions` | Listeleme (permission korumalı) |
| `POST·DELETE /api/v1/admin/users/{id}/roles` | Rol ata/kaldır (`identity:users:manage`) |

### Ek sertleştirme
- **Security headers** (her yanıtta): `X-Content-Type-Options`, `X-Frame-Options: DENY`,
  `Referrer-Policy`, `Content-Security-Policy: default-src 'none'`, `Server` başlığı kaldırılır; prod'da **HSTS**.
- **Audit logging:** login (başarılı/başarısız), lockout, refresh, **reuse**, logout, parola değişimi, rol atama
  (`ISecurityAuditLogger`, e-posta maskeli, parola/token loglanmaz).
- **Seed:** roller (`Administrator`, `User`) + tüm modül izinleri + opsiyonel admin (parola user-secrets ile).

> **Üretimde mutlaka:** `Jwt:Authority`'yi token'ları imzalayan OpenID Connect sağlayıcısına (https) ayarla
> (`Jwt:SigningKey` Development dışında reddedilir), TLS/HTTPS'i edge'de zorla,
> gerçek bir `IEmailSender` (SMTP/SendGrid) tak, admin parolasını güçlü ver. `LoggingEmailSender` yalnızca
> Development'ta kayıtlıdır. Diğer ortamlarda `IEmailSender` kayıtlı değilse uygulama açılışta anlamlı bir hatayla durur.

**Kaynaklar:** [OWASP Authentication CS](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html) ·
[OWASP Password Storage CS](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html) ·
[OWASP OAuth2 CS](https://cheatsheetseries.owasp.org/cheatsheets/OAuth2_Cheat_Sheet.html) ·
[MS Learn — Policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0) ·
[MS Learn — Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0)

---

## Konfigürasyon

`appsettings.json` (özet):

```jsonc
{
  "ConnectionStrings": { "Database": "Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;" },
  "Database":      { "ApplyMigrations": false },   // true: açılışta migration uygula
  "Jwt":           { "Authority": "", "Issuer": "novacore", "Audience": "novacore.api", "SigningKey": "", "AccessTokenMinutes": 15, "RefreshTokenDays": 7 },
  "Identity":      { "Seed": { "SeedOnStartup": false, "AdminEmail": "admin@novacore.local", "AdminPassword": "" } },
  "Cache":         { "DefaultExpiration": "00:05:00", "PermissionExpiration": "00:05:00" },
  "RateLimiting":  { "PermitLimit": 100, "WindowSeconds": 60 },   // /auth grubu 1/10'unu kullanır ("strict")
  "Cors":          { "AllowedOrigins": [] },        // boş = her ortamda her origin'e izin (credentials olmadan)
  "Seq":           { "ServerUrl": "" }              // dolu = Seq'e log gönder
}
```
Çevre değişkeni `OTEL_EXPORTER_OTLP_ENDPOINT` doluysa traces/metrics OTLP ile dışa aktarılır (ör. Jaeger).

> **Üretim:** `Jwt:Authority` zorunludur (mutlak `https` URI), `Jwt:SigningKey` verilemez. Development'ta ikisi de
> boşsa sabit bir geliştirme anahtarı kullanılır.
> Gizli değerler için `dotnet user-secrets` kullanın.

Bu bölümler options pattern ile bağlanır ve **uygulama açılırken doğrulanır** (`ValidateOnStart`). Geçersiz bir değer
host'u, ayarın adını veren bir hatayla durdurur:

- `Jwt`: `Issuer`/`Audience` boş olamaz. `Authority` Development dışında zorunlu ve `https`, Development'ta verilirse
  mutlak bir URI. `SigningKey` yalnızca Development'ta ve en az 32 bayt,
  `AccessTokenMinutes` ≥ 1, `RefreshTokenDays` 1–36.500.
- `RateLimiting`: `PermitLimit` ve `WindowSeconds` ≥ 1. `Cache:DefaultExpiration` ve `Cache:PermissionExpiration`
  pozitif birer süre.
- `Cors:AllowedOrigins`: her giriş çıplak bir origin olmalı (`https://app.example.com`): yol, sondaki `/` ve `*` yok.
- `Database:ApplyMigrations`: `true`/`false`.

Değerler kayıt anında değil, ilk kullanımda okunur. Bu yüzden `CreateBuilder`'dan sonra eklenen config (ör. testlerde
`ConfigureAppConfiguration`) da geçerlidir.

---

## Veritabanı & Migration

Her modül **kendi DbContext'ine + kendi SQL Server şemasına** sahiptir (`catalog` / `payments` /
`identity`, `HasDefaultSchema`), migration geçmişi de şema bazında ayrılır (`__EFMigrationsHistory`).
Sınırlar veritabanı düzeyinde uygulanır.

```bash
# Yeni migration (modül = hem -p hem -s; design-time factory sayesinde DB gerekmez)
dotnet ef migrations add <Ad> --project src/Modules/Catalog/NovaCore.Modules.Catalog --context CatalogDbContext --startup-project src/Modules/Catalog/NovaCore.Modules.Catalog --output-dir Infrastructure/Migrations

# Uygula
dotnet ef database update --project src/Modules/Catalog/NovaCore.Modules.Catalog --context CatalogDbContext --startup-project src/Modules/Catalog/NovaCore.Modules.Catalog
```
Alternatif: `Database:ApplyMigrations=true` ile uygulama açılışta tüm modül migration'larını otomatik uygular
(`DatabaseMigrationService`).

---

## Test

```bash
dotnet test
```
- **Unit:** `Result<T>`, `PaginationRequest`/`PagedList<T>` davranışları.
- **Integration:** gerçek host'u `WebApplicationFactory` ile ayağa kaldırır. Identity store testte
  **in-memory SQLite**'a takılır; böylece tam güvenlik akışı **SQL Server gerektirmeden** test edilir:
  login, refresh **rotation**, **reuse detection** (+ family revoke), permission 200/403, security headers.
- **Architecture:** modül sınırlarını zorlar (Payments yalnızca Catalog.Contracts'a bağlı), çekirdek paketlerin
  yalnızca Microsoft paketlerine bağlı olduğunu ve paketler arası bağımlılık yönünü doğrular (`NovaCore.Abstractions`
  ASP.NET/EF içermez), building block'larda assembly taraması olmadığını kontrol eder. Modül sırası ve endpoint listesi
  de testle sabitlenmiştir.

---

## Çekirdeği Dağıtma

İki tamamlayıcı yol:

1. **`dotnet new` template (birincil):** tüm iskeleti saniyeler içinde üretir.
   ```bash
   dotnet new install .
   dotnet new novacore -n YeniProje
   ```
2. **Lokal/özel NuGet paketi (BuildingBlocks için):** yalnızca stabil çekirdeği versiyonlu paketleyip
   bir bug fix'i tüm projelere yayın.
   ```bash
   dotnet pack NovaCore.slnx -c Release -o ./artifacts                               # 0.1.0
   dotnet pack NovaCore.slnx -c Release -o ./artifacts --version-suffix preview.1    # 0.1.0-preview.1
   # artifacts/ klasörünü lokal feed olarak ekleyin, sonra: dotnet add package NovaCore.AspNetCore (ve ihtiyaca göre diğerleri)
   ```
   - Altı paket üretilir: çekirdek `NovaCore.Abstractions`, `NovaCore.AspNetCore`, `NovaCore.EntityFrameworkCore`;
     eklentiler `NovaCore.Serilog`, `NovaCore.OpenTelemetry`, `NovaCore.Scalar`. Modüller paketlenmez.
   - Sürüm `Directory.Build.props`'taki `VersionPrefix`'ten gelir. API kararlı olana kadar 0.x kalır.
   - Her paket XML dokümanını, `src/PACKAGE.md`'den gelen README'yi, MIT lisans ifadesini ve repo linkini taşır. Yanında bir `.snupkg` sembol paketi üretilir.
   - Public API `PublicAPI.*.txt` dosyalarında izlenir. İzlenmeyen bir değişiklik build'i kırar.
   - Yayın `.github/workflows/release.yml` ile yapılır. `VersionPrefix` ile eşleşen bir `v*` tag'i (ör. `v0.1.0-preview.1`) GitHub Packages'a push eder.

---

## Paket / Lisans Tablosu

| Paket | İşlev | Lisans |
|---|---|---|
| Mapperly (Riok) | Object mapping (source-gen) | **Apache-2.0** |
| Scalar.AspNetCore | OpenAPI arayüzü (`NovaCore.Scalar` eklentisi) | **MIT** |
| Serilog.AspNetCore | Structured logging (`NovaCore.Serilog` eklentisi) | **Apache-2.0** |
| OpenTelemetry.* | Observability (`NovaCore.OpenTelemetry` eklentisi) | **Apache-2.0** |
| Microsoft.AspNetCore.OpenApi / Caching.Hybrid / JwtBearer / Http.Resilience | Microsoft-yerleşik (`NovaCore.AspNetCore`) | **MIT** |
| Microsoft.EntityFrameworkCore / Extensions.Hosting.Abstractions / Configuration.Binder | Microsoft-yerleşik (`NovaCore.EntityFrameworkCore`) | **MIT** |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Kullanıcı/rol deposu | **MIT** |
| Microsoft.EntityFrameworkCore.SqlServer | EF Core sağlayıcı | **MIT** |
| Microsoft.CodeAnalysis.PublicApiAnalyzers | Public API takibi (yalnızca build, pakete girmez) | **MIT** |

> ⚠️ **MediatR (v13+) ve AutoMapper (v15+) artık ticari** (Lucky Penny). NovaCore bunları kullanmaz.
> Pipeline behavior gerçekten gerekirse MIT lisanslı `martinothamar/Mediator` eklenebilir.

---

## Kararı Değiştirecek Eşikler

- **Mediator kütüphanesi ekle** → yalnızca pipeline behavior ihtiyacı somutlaşırsa (`martinothamar/Mediator`, MIT).
- **Tam Clean Architecture'a geç** → yalnızca bir modül gerçekten karmaşık DDD/invariant içeriyorsa, sadece o modül için.
- **Redis L2 cache ekle** → bir `IDistributedCache` (Redis) kaydet; HybridCache otomatik benimser.
- **OpenIddict/Keycloak'a geç** → SSO/OIDC/multi-client/federation gerekirse.
- **Runtime .dll eklenti mimarisi** → üçüncü partiler senin derlemen olmadan modül yüklemeliyse (tek-geliştirici için neredeyse hiç gerekmez).

---

### Notlar
- .NET 10 LTS'tir (Kasım 2025 → ~Kasım 2028).
- HybridCache .NET 9'dan beri GA/production-ready. `Microsoft.Extensions.Caching.Hybrid` 10.0.0 artık deneysel
  uyarısı (EXTEXP0018) üretmiyor, bu yüzden bastırma da yok.
- Modüller açıkça kaydedilir (`AddModule<TModule>()`); assembly taranmaz.
