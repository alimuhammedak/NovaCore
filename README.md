# NovaCore — .NET 10 Tek-Geliştirici Modüler Monolit Çekirdeği

Tek geliştirici + Web API + minimum karmaşıklık için tasarlanmış, **derleme-zamanı modüler monolit**
mimarisi. İnce bir paylaşılan **BuildingBlocks çekirdeği** + her modülde **Vertical Slice (dikey dilim)**
ile organize edilmiş iş modülleri.

> **Lisans felsefesi:** MediatR ve AutoMapper artık ticari (Lucky Penny). NovaCore bunları **kullanmaz**.
> Yerine: sade CQRS (mediator yok), **Mapperly** (Apache-2.0), **FluentValidation** (Apache-2.0),
> **Scrutor** (MIT), **Scalar** (MIT) ve Microsoft-yerleşik bileşenler. Çekirdekteki her paket MIT/Apache.

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
dotnet ef database update --project src/Modules/Identity/NovaCore.Modules.Identity  --context IdentityDbContext  --startup-project src/Modules/Identity/NovaCore.Modules.Identity

# 3) İlk admin + roller + izinler için seed (parolayı user-secrets ile ver — repoya YAZMA)
dotnet user-secrets --project src/Api/NovaCore.Api set "Identity:Seed:AdminPassword" "Admin!Passw0rd-2026"

# 4) Çalıştır (seed'i aç: Jwt:SigningKey'i prod'da mutlaka ayarla)
Identity__Seed__SeedOnStartup=true dotnet run --project src/Api/NovaCore.Api
```

Açılınca tarayıcı **Scalar API arayüzünü** (`/scalar/v1`) açar. Bağlantı dizesi (varsayılan):
`Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;`

| Uç nokta | Açıklama |
|---|---|
| `GET /health` | Readiness (tüm health check'ler) |
| `GET /alive` | Liveness (yalnızca "live" etiketli) |
| `GET /openapi/v1.json` | OpenAPI 3.1 dokümanı |
| `/scalar/v1` | Scalar API arayüzü (yalnızca Development) |
| `POST /api/v1/auth/register` · `login` · `refresh` · `logout` · `me` | Kimlik doğrulama akışı (bkz. [Güvenlik](#güvenlik--yetkilendirme)) |
| `/api/v1/auth/2fa/*` | TOTP iki-faktör (setup/enable/disable) |
| `/api/v1/admin/*` | Kullanıcı/rol/izin yönetimi (permission korumalı) |
| `/api/v1/products` | Catalog modülü (CRUD, permission korumalı) |
| `/api/v1/payments` | Payments modülü (Catalog'a contract üzerinden bağlı) |

> SQL Server olmadan da uygulama açılır ve `/health`, `/openapi`, validation/401 kapıları çalışır.
> DB'ye dokunan endpoint'ler düzgün bir **RFC 9457 ProblemDetails** ile yanıt verir.

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
Api (ince host / composition root)
        │  referans
        ▼
Modules.*  ──(yalnızca Contracts üzerinden)──►  diğer Modules.*
        │
        ▼
BuildingBlocks (Core.Web → Core.Infrastructure → Core.Abstractions)
        │
        ▼
.NET 10 BCL / ASP.NET Core / EF Core
```

**Bağımlılık kuralları**
- `Core.Abstractions` hiçbir framework'e bağlı değildir (yalnızca BCL). Bu kural bir **mimari test**
  ile zorlanır (`ModularityTests`).
- Modüller **birbirine doğrudan referans vermez**; yalnızca bir modülün `*.Contracts` projesine bağlanır.
  Bu da bir mimari test ile zorlanır.
- `BuildingBlocks` hiçbir modüle bağlı değildir.

### Çekirdek (her projede ortak) vs Proje (her projeye özel)
| Çekirdek (BuildingBlocks) | Projeye özel (Modules / Api) |
|---|---|
| `Result<T>`, `Error`, `PagedList<T>` | Domain entity'leri (Product, Payment...) |
| Global exception handling + ProblemDetails | İş modülleri ve endpoint'leri |
| `IModule` keşif/kayıt altyapısı | DbContext'ler + migration'lar |
| HybridCache sarmalayıcı (`ICacheService`) | İş kuralları, validator'lar |
| Serilog + OpenTelemetry + Health (`AddCoreDefaults`) | Modüle özel konfigürasyon |
| OpenAPI + Scalar, rate limiting, output cache, CORS | |
| JWT bearer auth + permission-based authz altyapısı | Identity *modülü* (users/roles/tokens) |
| Auditing interceptor, domain-event dispatcher | |

---

## Proje Ağacı

```
NovaCore.sln
├── Directory.Build.props          # tüm projeler: net10.0, nullable, NoWarn
├── Directory.Packages.props       # merkezî paket sürümleri (CPM)
├── docker-compose.yml             # opsiyonel SQL Server + Seq + Jaeger
├── .template.config/template.json # `dotnet new novacore`
│
├── src/
│   ├── BuildingBlocks/
│   │   ├── NovaCore.Core.Abstractions/   # Result, PagedList, Entity, IDomainEvent/DomainEvent, ICacheService, ICurrentUser
│   │   ├── NovaCore.Core.Infrastructure/ # GlobalExceptionHandler, Result→HTTP, HybridCache, auditing, migration
│   │   └── NovaCore.Core.Web/            # IModule, AddCoreDefaults/AddCoreWeb, JWT, OpenAPI+Scalar, validation
│   │
│   ├── Modules/
│   │   ├── Catalog/
│   │   │   ├── NovaCore.Modules.Catalog.Contracts/  # IProductCatalog + ProductInfo (dışa açılan tek yüzey)
│   │   │   └── NovaCore.Modules.Catalog/            # CatalogModule, Domain, Features/, Infrastructure/ (schema: catalog)
│   │   ├── Payments/
│   │   │   └── NovaCore.Modules.Payments/           # PaymentsModule (schema: payments) — Catalog'a Contracts ile bağlı
│   │   └── Identity/
│   │       └── NovaCore.Modules.Identity/           # IdentityModule (schema: identity) — Identity+JWT/refresh+2FA+admin
│   │
│   └── Api/
│       └── NovaCore.Api/           # ~15 satırlık Program.cs (composition root)
│
└── tests/
    └── NovaCore.Api.Tests/         # unit + integration (WebApplicationFactory) + architecture testleri
```

### `Program.cs` — tüm host
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCoreDefaults();   // Serilog + OpenTelemetry + health + resilient HttpClients
builder.AddCoreWeb();        // ProblemDetails, OpenAPI+Scalar, JWT auth, rate limit, output cache, CORS
builder.AddModules();        // tüm NovaCore.Modules.* IModule'lerini bulur ve kaydeder

var app = builder.Build();

app.UseCoreWeb();            // exception handling, auth, rate limiter, Scalar (Development)
app.MapModules();            // tüm modüllerin endpoint'lerini bağlar
app.MapDefaultEndpoints();   // /health, /alive

app.Run();
```

---

## Yeni Modül Ekleme

"Ödeme modülü ekleme" senaryosu tam olarak şöyle: **yeni proje + `IModule` + Api referansı.** Başka hiçbir
merkezî dosya değişmez — host modülleri `NovaCore.Modules.*` derlemelerinden **otomatik keşfeder**.

1. `dotnet new classlib -o src/Modules/Orders/NovaCore.Modules.Orders` (isim `NovaCore.Modules.` ile başlamalı)
2. `NovaCore.Core.Web`'e referans ver.
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
        services.AddValidatorsFromAssembly(typeof(OrdersModule).Assembly, includeInternalTypes: true);
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
4. `NovaCore.Api`'ye proje referansı ekle. Bitti.

### Bir özelliğin (feature) anatomisi — Vertical Slice
Her özellik tek dosya: request + response + validator + route + handler bir arada. Mediator yok;
handler bağımlılıklarını doğrudan minimal-API DI'dan alır. Örnek: [`CreateProduct.cs`](src/Modules/Catalog/NovaCore.Modules.Catalog/Features/CreateProduct.cs).

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
| **Exception handling** | `IExceptionHandler` + `AddProblemDetails` (RFC 9457) | `GlobalExceptionHandler` + `ValidationExceptionHandler` (validation önce çalışır) |
| **Logging** | Serilog (structured) + OTel korelasyon | `AddCoreLogging` — Console + opsiyonel Seq |
| **Tracing/Metrics** | OpenTelemetry | `AddCoreObservability` — OTLP yalnızca endpoint konfigüre ise |
| **Caching** | HybridCache (L1 + opsiyonel L2) | `ICacheService` → `HybridCacheService` |
| **Validation** | FluentValidation | `ValidationFilter<T>` endpoint filtresi |
| **Mapping** | Mapperly (source-gen) | `[Mapper]` statik partial sınıflar, EF-translatable `ProjectToDto` |
| **Auth** | ASP.NET Core Identity + JWT/refresh + permission-based | bkz. [Güvenlik & Yetkilendirme](#güvenlik--yetkilendirme) |
| **Auditing** | EF Core `SaveChangesInterceptor` | `AuditableEntityInterceptor` — CreatedBy/ModifiedAt merkezî |
| **Rate limiting** | Yerleşik `AddRateLimiter` | IP bazlı fixed-window + `"strict"` adlı policy |
| **Output cache** | Yerleşik `AddOutputCache` | `AddCoreWeb` içinde |
| **Health** | Yerleşik Health Checks | `/health` + `/alive` |
| **API docs** | `Microsoft.AspNetCore.OpenApi` + Scalar | Bearer security scheme transformer ile |
| **DI keşfi** | Scrutor / reflection | `ModuleLoader` (assembly tarama) |
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
- Kısa ömürlü **access JWT** (HS256, 15 dk) — `sub`, `name`, `email`, `role*`, `permission*` claim'leri.
- Uzun ömürlü **refresh token** (256-bit, **DB'de SHA-256 hash'li**, 7 gün).
- **Rotation:** her `/auth/refresh`'te yeni refresh, eskisi iptal.
- **Reuse detection + family revocation:** tüketilmiş bir refresh tekrar gelirse **tüm token ailesi iptal**
  + `ERR refresh_token_reuse_detected` audit log'u (hırsızlık tespiti — OWASP OAuth2).
- **JWT validation sertleştirme:** algoritma pin'i (`HS256`), issuer/audience zorunlu, `ClockSkew = 30s`,
  `MapInboundClaims = false`, prod'da `RequireHttpsMetadata = true`.

### Yetkilendirme (authorization) — permission-based
- Roller (Identity) **+ ince taneli izinler**. İzinler `module:resource:action` (ör. `catalog:products:create`).
- `PermissionPolicyProvider` (dinamik policy) + `PermissionRequirement` + handler. Kullanım:
  `endpoint.RequirePermission("catalog:products:create")` veya `[HasPermission(...)]`.
- İzinler login'de **JWT claim** olarak gömülür; `Administrator` rolü super-admin (tüm kontrolleri geçer).
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

> **Üretimde mutlaka:** `Jwt:SigningKey` ayarla (≥32 bayt, user-secrets/env), TLS/HTTPS'i edge'de zorla,
> gerçek bir `IEmailSender` (SMTP/SendGrid) tak, admin parolasını güçlü ver.

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
  "Jwt":           { "Issuer": "novacore", "Audience": "novacore.api", "SigningKey": "", "AccessTokenMinutes": 15, "RefreshTokenDays": 7 },
  "Identity":      { "Seed": { "SeedOnStartup": false, "AdminEmail": "admin@novacore.local", "AdminPassword": "" } },
  "Cache":         { "DefaultExpiration": "00:05:00" },
  "RateLimiting":  { "PermitLimit": 100, "WindowSeconds": 60 },   // /auth grubu 1/10'unu kullanır ("strict")
  "Cors":          { "AllowedOrigins": [] },        // boş = dev'de hepsine izin
  "Seq":           { "ServerUrl": "" }              // dolu = Seq'e log gönder
}
```
Çevre değişkeni `OTEL_EXPORTER_OTLP_ENDPOINT` doluysa traces/metrics OTLP ile dışa aktarılır (ör. Jaeger).

> **Üretim:** `Jwt:SigningKey` zorunludur (≥32 bayt). Development'ta yoksa otomatik dev anahtarı üretilir.
> Gizli değerler için `dotnet user-secrets` kullanın.

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
- **Architecture:** modül sınırlarını zorlar (Payments yalnızca Catalog.Contracts'a bağlı; Core.Abstractions
  ASP.NET/EF içermez).

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
   dotnet pack src/BuildingBlocks/NovaCore.Core.Web -c Release -o ./artifacts
   # artifacts/ klasörünü lokal feed olarak ekleyin, sonra: dotnet add package NovaCore.Core.Web
   ```

---

## Paket / Lisans Tablosu

| Paket | İşlev | Lisans |
|---|---|---|
| Mapperly (Riok) | Object mapping (source-gen) | **Apache-2.0** |
| FluentValidation | Validation | **Apache-2.0** |
| Scrutor | DI assembly scanning | **MIT** |
| Scalar.AspNetCore | OpenAPI arayüzü | **MIT** |
| Serilog.AspNetCore | Structured logging | **Apache-2.0** |
| OpenTelemetry.* | Observability | **Apache-2.0** |
| Asp.Versioning.Http | API versioning | **MIT** |
| Microsoft.AspNetCore.OpenApi / Caching.Hybrid / JwtBearer | Microsoft-yerleşik | **MIT** |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Kullanıcı/rol deposu | **MIT** |
| Microsoft.EntityFrameworkCore.SqlServer | EF Core sağlayıcı | **MIT** |

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
- HybridCache .NET 9'dan beri GA/production-ready; .NET 10'da API'leri hâlâ `[Experimental]` (EXTEXP0018)
  işaretli olduğundan `NoWarn` ile bilinçli olarak açıldı.
- Reflection tabanlı modül taraması NativeAOT ile uyumsuzdur; AOT hedeflenirse source-generator tabanlı kayda geçilir.
