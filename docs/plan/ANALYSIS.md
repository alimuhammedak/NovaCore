> **Anlık görüntü (2026-09-24).** Bu dosya, yeniden yapılanma kararlarının dayandığı analizdir ve bir daha değiştirilmez.
> Yürütülebilir plan için [PLAN.md](PLAN.md), kararlar için [DECISIONS.md](DECISIONS.md), ilerleme için [PROGRESS.md](PROGRESS.md) dosyalarına bak.

# NovaCore: Derin mimari analiz ve yeniden yapılanma planı

## Context

**Hedef (kullanıcı):** Bütün projelerde kullanılabilecek, versiyonlanabilir, bağımlılığı minimum, best practice'lere ve design pattern'lere tam uyan bir .NET altyapısı.

**Soru:** Mevcut NovaCore bu hedefe ne kadar uyuyor, nerede yanlış yöne gidiyoruz, verilen kararlar bugün hâlâ doğru mu?

**Yöntem:**
- `src/` ve `tests/` altındaki kodun tamamı (~3.700 satır) okundu.
- `dotnet build` / `dotnet test` çalıştırıldı: 0 uyarı, 33/33 test geçti.
- `dotnet list package --vulnerable/--outdated` ve `dotnet restore --force` ile paket denetimi yapıldı.
- 13 kritik karar Microsoft Learn MCP ile birebir karşılaştırıldı.
- `engineering:architecture` ve `engineering:tech-debt` skill'leri uygulandı. Puanlama: **(Etki + Risk) × (6 − Efor)**.

**Kullanıcının verdiği kararlar (bu planın dayanağı):**
1. **Dağıtım:** NuGet paketleri + ince template.
2. **Kimlik:** Çekirdek yalnızca doğrulama yapar. Token'ı ayrı bir IdP üretir (OpenIddict eklentisi veya harici IdP).
3. **Bağımlılık:** Çekirdek yalnızca Microsoft/BCL. Üçüncü taraf paketler opsiyonel `NovaCore.*` eklenti paketlerine taşınır.

---

## 1. Özet hüküm

Makro mimari doğru: modüler monolit, vertical slice, şema başına DbContext, mediator yok, Result pattern. **Ama proje, hedefine göre üç temel eksende yanlış yönde ilerliyor:**

1. **Bir "altyapı kütüphanesi" değil, bir "uygulama şablonu".**
   - Birincil dağıtım `dotnet new` template'i, yani kopyala-yapıştır. Versiyonlama yok, git yok, CI yok.
   - Paketlenebilir projelerde XML dokümantasyonu kapalı, API uyumluluk denetimi yok.
   - Microsoft'un kütüphane rehberi *"DO distribute your library using a NuGet package"* diyor.
2. **Kimlik tasarımı Microsoft'un JWT rehberine aykırı.** HS256 simetrik anahtar kullanılıyor ve parola isteğinden doğrudan access token üretiliyor. Rehber bunun tersini söylüyor: *"Asymmetric keys should always be used"*, *"Never create an access token from a username/password request"* (Configure JWT bearer authentication, aspnetcore-10.0).
3. **"Bağımlılıksız" hedefinin tersine, çekirdek fazla şey getiriyor.** `Core.Web`'i referans alan her proje Serilog, Seq, OpenTelemetry, Scalar, FluentValidation ve Asp.Versioning'i zorunlu olarak alıyor. Bunların bir kısmı hiç kullanılmıyor, bir kısmının yerleşik .NET 10 karşılığı var.

Ek olarak testlerin ve "0 uyarı" çıtasının **gizlediği** üretim hataları var. Bunlar Bölüm 3'teki P0 maddeleri.

---

## 2. Karar denetimi: "Bugün hâlâ doğru mu?"

| Karar | Hüküm | Gerekçe / MS Learn kaynağı |
|---|---|---|
| .NET 10 LTS | ✅ Doğru | LTS, Kasım 2028'e kadar destekli |
| Modüler monolit + vertical slice | ✅ Doğru | Tek geliştirici için doğru denge |
| Modül başına DbContext + şema + migration tablosu | ✅ Doğru | Sınır DB seviyesinde uygulanıyor |
| MediatR/AutoMapper yok, "sade CQRS" | ✅ Doğru | Lisans ve basitlik açısından doğru |
| Result pattern + ProblemDetails (RFC 9457) | ✅ Doğru, küçük eksikler | Map/Bind yok, tek `Error` taşıyor. `default(Error) != Error.None` tuzağı var |
| CPM (Directory.Packages.props) | ⚠️ Eksik | Transitive pinning kapalı, audit uyarıları görmezden geliniyor (bkz. P0-1) |
| **`dotnet new` template ile dağıtım** | ❌ Hedefle çelişiyor | Library guidance: NuGet + SemVer + Package Validation + SourceLink |
| **Kendi JWT'sini üretmek (HS256, parola→token)** | ❌ MS rehberine aykırı | Configure JWT bearer authentication → "Recommended approaches to create a JWT" |
| Permission claim'lerinin JWT'ye gömülmesi | ⚠️ | Token şişer, yetki değişikliği token süresi boyunca yansımaz. Harici IdP ile de uyumsuz |
| Özel `PermissionPolicyProvider` | ⚠️ Eskimiş | .NET 8+: `IAuthorizationRequirementData`, Minimal API'de 8–10 arasında destekli. Policy provider'a gerek yok |
| FluentValidation + özel `ValidationFilter` | ⚠️ Yerleşik karşılığı var | .NET 10 `builder.Services.AddValidation()` stabil. Modül assembly'si başına bir `AddValidation` çağrısı gerekir |
| `ICacheService` sarmalayıcısı | ⚠️ Gereksiz | `HybridCache` zaten soyut bir sınıf (abstraction). Bu, bir soyutlamanın üzerine bir soyutlama daha eklemek demek |
| Config değerlerinin eager okunması (`GetValue` ile kayıt anında) | ❌ | Options pattern: `AddOptions<T>().BindConfiguration().ValidateOnStart()`. Test ortamındaki env-var hack'ini de bu doğuruyor |
| `"strict"` rate limiter | ❌ Hata | `AddFixedWindowLimiter` bölünmemiş tek bir limiter oluşturur. Rehbere göre bölümleme olmazsa bir kullanıcı tüm limiti tüketir |
| Açılışta migration (opt-in) | ⚠️ Kabul edilebilir | EF Core 9+ migration kilidi var. Yine de "prefer a migration bundle" önerisi geçerli |
| Serilog'un çekirdekte olması | ⚠️ | Kütüphane `ILogger` soyutlamasını kullanmalı. Serilog bir host tercihi, eklenti paketine gitmeli |
| `/health` herkese açık, DB kontrolü yok | ⚠️ | MS örneği health uç noktalarını yalnızca Development'ta map'liyor ya da `RequireHost`/`RequireAuthorization` öneriyor |
| Reflection ile deploy klasörü taraması (ModuleLoader) | ⚠️ | AOT uyumsuz. Klasördeki `NovaCore.Modules.*.dll` adlı her dosya yükleniyor. Api zaten modüllere referans verdiği için açık kayıt daha basit ve güvenli |
| Asp.Versioning | ❌ Kullanılmıyor | Kayıtlı ama hiçbir uç nokta version set kullanmıyor |
| Scrutor | ❌ Kullanılmıyor | Referans var, kodda hiç kullanılmıyor. README "kullanılıyor" diyor |
| Output cache | ⚠️ Kullanılmıyor | Kayıtlı, hiçbir policy veya uç nokta yok |
| `DateTimeOffset.UtcNow` her yerde | ⚠️ | .NET 8+ `TimeProvider` (+ testte `FakeTimeProvider`) |
| `logger.LogX(...)` çağrıları | ⚠️ | Kütüphane rehberi `[LoggerMessage]` source generator öneriyor (CA1848) |
| EXTEXP0018 bastırması | ❓ Doğrulanacak | Güncel HybridCache dokümanları artık experimental uyarısından söz etmiyor. Muhtemelen eskimiş |

---

## 3. Bulgular: önceliklendirilmiş teknik borç

Puan = (Etki + Risk) × (6 − Efor). Etki, Risk ve Efor 1–5 arası.

### P0: Güvenlik ve doğruluk (hemen)

| # | Bulgu | Kanıt | Puan |
|---|---|---|---|
| P0-1 | **2 adet HIGH seviyeli açık + eski yamalar.** Microsoft.OpenApi 2.0.0 (GHSA-v5pm-xwqc-g5wc, tüm projelerde transitive) ve SQLitePCLRaw 2.1.11 (GHSA-2m69-gcr7-jv3q, testlerde). `NU1903` yalnızca restore'da görünüyor, artımlı derlemede görünmüyor, yani "0 uyarı" çıtası bunları gizliyor. EF Core 10.0.4, en son 10.0.12 (8 yama geride). **EF sürüm kayması:** Identity modülü EF Core 10.0.8'i SqlServer provider 10.0.4 ile birlikte çalıştırıyor | `dotnet list package --vulnerable`, `obj/project.assets.json` | 45 |
| P0-2 | **Auth rate limiter tüm dünya için tek kova.** `/api/v1/auth/*` altındaki her şey, `/me` ve `/refresh` dahil, **tüm kullanıcılar için toplamda** dakikada 10 isteğe izin veriyor. Tek bir saldırgan herkesin login olmasını engelleyebilir. Testler limiti 1.000.000'a çekip bunu gizliyor | [CoreWebExtensions.cs:133](src/BuildingBlocks/NovaCore.Core.Web/Web/CoreWebExtensions.cs:133), [IdentityModule.cs:94](src/Modules/Identity/NovaCore.Modules.Identity/IdentityModule.cs:94) | 45 |
| P0-3 | **Git yok, CI yok.** Versiyonlama, tag, geri dönüş, audit kapısı yapılamıyor. Hedefin ("versiyonlanabilir") ön koşulu bu | repo kökü | 40 |
| P0-4 | **`LoggingEmailSender` tüm ortamlarda kayıtlı.** Production'da e-posta onay token'ları loglara yazılıyor. README'deki "token loglanmaz" iddiasıyla çelişiyor | [IdentityModule.cs](src/Modules/Identity/NovaCore.Modules.Identity/IdentityModule.cs), `AddSingleton<IEmailSender, LoggingEmailSender>` | 40 |
| P0-5 | **`IDomainEvent.EventId` ve `OccurredOnUtc` her okunuşta farklı değer üretiyor.** Default interface member `=> Guid.CreateVersion7()` her erişimde yeniden hesaplanıyor | [IDomainEvent.cs:9](src/BuildingBlocks/NovaCore.Core.Abstractions/Messaging/IDomainEvent.cs:9) | 35 |
| P0-6 | Onay linki `Request.Host`'tan üretiliyor ve `AllowedHosts:"*"`. Host header ile link zehirlenebilir. Hatalı 2FA kodu lockout sayacını artırmıyor. Refresh rotation'da yarış durumu var: aynı token ile gelen iki istek iki geçerli token alabiliyor | [RegistrationEndpoints.cs:82](src/Modules/Identity/NovaCore.Modules.Identity/Features/RegistrationEndpoints.cs:82), [LoginEndpoints.cs:144](src/Modules/Identity/NovaCore.Modules.Identity/Features/LoginEndpoints.cs:144), [AuthTokenService.cs:54](src/Modules/Identity/NovaCore.Modules.Identity/Services/AuthTokenService.cs:54) | 24–30 |

> P0-6'daki Identity açıkları Faz 3'te modül değiştirilince ortadan kalkacak. Faz 3'e kadar Identity kullanılacaksa minimal düzeltme yapılır.

### P1: Mimari yön (hedefle çelişenler)

| # | Bulgu | Puan |
|---|---|---|
| P1-1 | **Dağıtım modeli: template → NuGet.** Kopyalanan kod güncellenemez. Paketlenebilir projelerde `GenerateDocumentationFile=false`, package validation yok, SemVer yok, LICENSE/CHANGELOG yok | 27 |
| P1-2 | **Eager config okuma → Options pattern.** `JwtOptions` ham singleton olarak kayıtlı. `RateLimiting`, `Jwt` ve `Database` kayıt anında okunuyor. Test altyapısındaki process-wide `Environment.SetEnvironmentVariable` hack'i ve CLAUDE.md'deki "gotcha" notu bu yüzden var | 28 |
| P1-3 | **Çekirdek bağımlılıkları.** `Core.Web` 15 paket getiriyor. `Core.Infrastructure` HTTP içeriyor (`GlobalExceptionHandler`, `ToHttpResult`). "Infrastructure" adı içerikle uyuşmuyor | 21 |
| P1-4 | **Kimlik: kendi token üretimi → OIDC/harici IdP.** Çekirdek yalnızca `Authority`/JWKS ile doğrulamalı. İzinler token'dan değil, sunucu tarafında `IClaimsTransformation` + HybridCache ile çözülmeli | 18 |
| P1-5 | **Modüller arası yazma tutarlılığı yok.** Payments stoğu kontrol ediyor ama düşmüyor (TOCTOU, sınırsız satış). Domain event'ler hiç dispatch edilmiyor (`Raise` hiç çağrılmıyor, dispatcher hiç tetiklenmiyor). Dispatcher singleton olduğu için scoped handler'da patlar. Outbox yok | 14–24 |
| P1-6 | **Mimari testler elle listelenmiş.** Identity ve yeni eklenecek modüller kapsam dışı. Genel kural ("hiçbir modül başka bir modülün implementasyonuna referans veremez") uygulanmıyor | 24 |
| P1-7 | **Analyzer yok.** `TreatWarningsAsErrors=false`, `AnalysisLevel` yok, `EnforceCodeStyleInBuild=false` | 30 |

### P2: Modernizasyon ve temizlik

- **Kullanılmayan kod:** Asp.Versioning, Scrutor, OutputCache (policy yok), `ICacheService`, `IModule.IsEnabled` (config'e bağlı değil), `HasPermissionAttribute`, [CoreWebExtensions.cs:75](src/BuildingBlocks/NovaCore.Core.Web/Web/CoreWebExtensions.cs:75) "Postgres" yorumu. *(20)*
- **Yerleşik karşılığa geçiş:** `IAuthorizationRequirementData` (policy provider'ı siler), .NET 10 `AddValidation` (FluentValidation'ı siler), `TimeProvider`, `[LoggerMessage]`. *(8–16)*
- **Domain invariant'ları entity'de değil validator'da.** `Product.Update` fiyat/stok kontrolü yapmıyor. `Payment` durum geçişleri serbest (Refunded → Complete mümkün). Entity eşitliği (Id bazlı) tanımlı değil. `IAuditableEntity` setter'ları public. *(20)*
- **Health:** DB check yok (`AddDbContextCheck`), `/health` herkese açık. *(20)*
- **GlobalExceptionHandler** `ArgumentException` ve `UnauthorizedAccessException` mesajlarını istemciye yansıtıyor (iç detay sızıntısı). `Instance` alanı URI değil. *(12)*
- **Dağınık config:** `appsettings` içindeki `Logging` bölümü Serilog tarafından okunmuyor (ölü config). docker-compose'ta SA parolası repoya commit'li ve imajlar `:latest` etiketli. *(15)*
- **Eskimiş dokümanlar:** README'deki Scrutor iddiası, "mimari test modülleri zorlar" iddiası, CLAUDE.md'deki "0 uyarı çıtası" (audit'i gizliyor).

---

## 4. Hedef mimari

### Paket haritası (çekirdek = yalnızca BCL + Microsoft)

```
NovaCore.Abstractions            (BCL only)   Result/Error, Paging, Entity/AggregateRoot, IDomainEvent (düzeltilmiş), ICurrentUser
NovaCore.AspNetCore              (MS only)    ProblemDetails+exception handlers, Result→HTTP, security headers, partitioned rate limit,
                                              CORS, JWT *doğrulama* (Authority/JWKS), permission authz (IAuthorizationRequirementData),
                                              validation (AddValidation), OpenAPI (Microsoft.AspNetCore.OpenApi), health, IModule (açık kayıt)
NovaCore.EntityFrameworkCore     (MS only)    AddModuleDbContext, audit + domain-event interceptor'ları (TimeProvider), Outbox, DB health check
── opsiyonel eklentiler (üçüncü taraf, her proje seçer; template varsayılan olarak ekler) ──
NovaCore.OpenTelemetry           OTel traces/metrics/logs + OTLP
NovaCore.Serilog                 Serilog host entegrasyonu (+Seq)
NovaCore.Scalar                  Scalar UI
NovaCore.Identity.OpenIddict     (Faz 3) OIDC sunucusu: ASP.NET Core Identity + OpenIddict (Apache-2.0)
```

**Bağımlılık yönü:** `Abstractions ← EntityFrameworkCore`, `Abstractions ← AspNetCore`. EF ve AspNetCore birbirine bağlı değil. Eklentiler yalnızca `AspNetCore`'a bağlı. Mapperly çekirdekte değil: source generator olduğu için modül seviyesinde bir tercih olarak önerilir.

### Design pattern karşılaştırması

| Pattern | Şu an | Hedef |
|---|---|---|
| Options | ❌ eager `GetValue`, ham singleton | `AddOptions<T>().BindConfiguration().ValidateOnStart()` + `[OptionsValidator]` source-gen |
| Result | ✅ | + `Map/Bind/Ensure`, validation için hata listesi |
| Plugin/Composite (`IModule`) | ⚠️ reflection taraması | Açık kayıt: `builder.AddModule<CatalogModule>()` |
| Adapter (`ICacheService`) | ❌ gereksiz sarmalayıcı | `HybridCache` doğrudan kullanılır |
| Strategy (policy provider) | ⚠️ | `IAuthorizationRequirementData` attribute + handler |
| Domain Events + **Outbox** | ❌ bozuk / yok | `SaveChanges` interceptor → modül şemasında outbox tablosu → `BackgroundService` dispatcher (scope başına, idempotent `EventId`) |
| Anti-corruption layer (Contracts) | ✅ yalnızca okuma | Yazma işlemleri için event'ler (stok rezervasyonu) |
| Clock | ❌ `UtcNow` | `TimeProvider` |
| Aggregate invariant'ları | ❌ validator'da | Entity metotlarında guard'lar + durum makinesi (`Payment`) |

---

## 5. Aşamalı yol haritası

### Faz 0: Stabilizasyon (P0)
1. `git init` + `.gitignore` kontrolü, ilk commit. `LICENSE` (MIT) ve `CHANGELOG.md` eklenir.
2. `Directory.Packages.props` güncellenir:
   - Tüm Microsoft paketleri 10.0.12 / 10.10.x'e çıkar.
   - OTel 1.19, Scalar 2.17, xUnit v3 4.x (breaking değişiklikler kontrol edilir).
   - EF paketlerinin sürümleri hizalanır.
   - Microsoft.OpenApi açığı güncel `Microsoft.AspNetCore.OpenApi` ile kapanmıyorsa `CentralPackageTransitivePinningEnabled=true` + açık `PackageVersion` eklenir.
   - `WarningsAsErrors` içine `NU1903;NU1904` eklenir.
3. `"strict"` limiter IP'ye göre bölünmüş bir policy'ye çevrilir (`AddPolicy` + `RateLimitPartition.GetFixedWindowLimiter`).
4. `IDomainEvent`: `EventId` ve `OccurredOnUtc` oluşturma anında sabitlenir (abstract record `DomainEvent` base).
5. `LoggingEmailSender` yalnızca Development'ta kaydedilir. Diğer ortamlarda `IEmailSender` kayıtlı değilse açılış hata verir.
6. Kullanılmayan kod silinir: Scrutor, Asp.Versioning, `AddOutputCache`, eski yorumlar.
7. **CI (GitHub Actions):** `restore` (audit hata sayılır), `build -warnaserror`, `test`, `dotnet ef migrations has-pending-model-changes` (her context için).

### Faz 1: Kütüphane altyapısı (versiyonlanabilirlik)
- **`Directory.Build.props`**, `IsPackable=true` olan projeler için:
  - `GenerateDocumentationFile=true`, `AnalysisLevel=latest-recommended`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`
  - `EnablePackageValidation=true` (ilk sürümden sonra `PackageValidationBaselineVersion`)
  - `ContinuousIntegrationBuild` (CI'da), `IncludeSymbols`/`snupkg`, `PackageLicenseExpression`, `PackageReadmeFile`, `RepositoryUrl`
  - SourceLink .NET 8+ SDK'da yerleşik.
- **Versiyon:** `VersionPrefix` `Directory.Build.props` içinde tutulur, CI prerelease için `VersionSuffix` ekler (ek paket yok).
- **Feed:** GitHub Packages (private). Tag'lenen sürüm (`v1.2.0`) CI'da `dotnet pack` + `dotnet nuget push` ile yayınlanır.
- **Public API takibi:** `Microsoft.CodeAnalysis.PublicApiAnalyzers` (yalnızca build-time, `PrivateAssets=all`) ile `PublicAPI.Shipped/Unshipped.txt`.

### Faz 2: Çekirdeği yeniden kesme (Microsoft-only)
- Projeler yeniden adlandırılıp bölünür (Bölüm 4). HTTP kodu `Core.Infrastructure`'dan `NovaCore.AspNetCore`'a taşınır.
- Options pattern: `JwtBearerOptions` `IConfigureNamedOptions` ile, `RateLimiterOptions` `AddOptions<RateLimiterOptions>().Configure<IOptions<NovaRateLimitOptions>>` ile yapılandırılır. `DatabaseMigrationService` her zaman kayıtlı olur, bayrağı `StartAsync` içinde okur.
- `ValidationFilter` + FluentValidation → `AddValidation()` (modül başına bir `AddXxxValidation()` extension'ı). Request DTO'larına DataAnnotations eklenir, karmaşık kurallar için `IValidatableObject` kullanılır.
- `PermissionPolicyProvider` → `RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData`. `RequirePermission(...)` endpoint extension'ı korunur, yalnızca içi değişir.
- `ICacheService` silinir, `HybridCache` doğrudan kullanılır. EXTEXP0018 gerekmiyorsa kaldırılır.
- `TimeProvider.System` kaydedilir. Interceptor, token ve refresh kodu bunu kullanır.
- `ModuleLoader` reflection taraması → açık `AddModule<T>()` / `MapModules()` (modül listesi DI'da tutulur).
- Health: `AddDbContextCheck<T>()` modül kaydında eklenir, "ready" etiketi verilir. `/health` ve `/alive` production'da `RequireHost` veya config ile korunur.
- `GlobalExceptionHandler`: yalnızca `AppException` mesajları istemciye yansıtılır. `ArgumentException` ve benzerleri için 500/400 döner, mesaj gizlenir.
- `ISecurityAuditLogger` ve diğer loglama noktaları → `[LoggerMessage]`.

### Faz 3: Kimlik (standartlara uyum)
- `NovaCore.AspNetCore`: `AddNovaAuthentication()` → JwtBearer `Authority` + `Audience` (JWKS otomatik keşfedilir). Simetrik anahtar desteği yalnızca test için kalır.
- İzinler: token'daki `permission` claim'leri yerine `IClaimsTransformation`. Kullanıcı rollerinden izinler çözülür ve `HybridCache` ile önbelleğe alınır (anahtar: `sub` + security stamp). Böylece harici IdP ile de çalışır.
- `NovaCore.Identity.OpenIddict` eklentisi: ASP.NET Core Identity (mevcut sertleştirmeler korunur: 600k PBKDF2, lockout, e-posta onayı) + OpenIddict (Authorization Code + PKCE, Refresh Token rotation yerleşik, RS256/ES256 + JWKS).
- Mevcut `NovaCore.Modules.Identity` (HS256, parola→token) emekliye ayrılır. docker-compose'a opsiyonel Keycloak servisi eklenir (harici IdP senaryosu için).

### Faz 4: Modüller arası tutarlılık (örnek modüller referans implementasyon olur)
- `EntityFrameworkCore` paketi:
  - `DomainEventsInterceptor`, `SavingChanges` sırasında event'leri modülün `outbox_messages` tablosuna aynı transaction içinde yazar.
  - `OutboxDispatcher : BackgroundService`, mesajları scope başına dispatch eder. `EventId` ile idempotent çalışır ve `EnableRetryOnFailure` ile uyumlu olması için execution strategy kullanır.
- Payments/Catalog akışı:
  - `PaymentCreated` event'i Catalog'da stok rezervasyonunu tetikler.
  - Başarılı olursa `StockReserved` ile payment `Completed` olur, başarısız olursa `Failed`.
  - Alternatif: stok rezervasyonu için senkron `IProductCatalog.ReserveStockAsync` contract'ı. Tek DB olduğu için daha basit. Faz 4 başında bu iki seçenekten biri seçilecek.
- Entity'lerde invariant guard'ları ve `Payment` durum makinesi eklenir.

### Faz 5: Template ve testler
- Template yalnızca `Api` host'u + 1 örnek modül üretir, NovaCore paketlerine **PackageReference** ile bağlanır.
- **Mimari testler:** `NovaCore.Modules.*` assembly'leri taranır. Hiçbiri başka bir modülün (Contracts hariç) referansını taşıyamaz. Çekirdek paketlerde Microsoft/BCL dışı referans olmadığı da test edilir.
- **Test host:** env-var hack'i kaldırılır (Options pattern sayesinde `ConfigureAppConfiguration` yeterli olur). Catalog ve Payments için entegrasyon testleri eklenir. Rate limiter bölümlemesi için test yazılır (iki farklı IP bağımsız sayılmalı). `FakeTimeProvider` ile token süresi testleri yazılır.
- **Paket tüketici smoke testi:** CI'da `dotnet pack` edilen paketlerle template'ten yeni bir proje üretilir ve build + test çalıştırılır.

---

## 6. Kritik dosyalar

- **Build/paket:** `Directory.Build.props`, `Directory.Packages.props`, `.template.config/template.json`, `NovaCore.slnx`, (yeni) `.github/workflows/ci.yml`, `LICENSE`, `CHANGELOG.md`
- **Çekirdek:** `src/BuildingBlocks/NovaCore.Core.Web/Web/CoreWebExtensions.cs` (en çok değişecek dosya), `Auth/AuthenticationExtensions.cs`, `Authorization/PermissionAuthorization.cs`, `Modules/ModuleLoader.cs`, `Modules/ModuleExtensions.cs`, `Validation/*`
- **Altyapı:** `src/BuildingBlocks/NovaCore.Core.Infrastructure/` (HTTP parçaları taşınır; `Persistence/*` EF paketine gider), `Messaging/DomainEventDispatcher.cs`, `Caching/*` (silinir)
- **Abstractions:** `src/BuildingBlocks/NovaCore.Core.Abstractions/Messaging/IDomainEvent.cs`, `Entities/Entity.cs`, `Results/*`
- **Modüller:** `*/…Module.cs` kayıtları, `Features/*` (validation → DataAnnotations), `Payments/Features/CreatePayment.cs`, `Identity/*` (Faz 3'te değiştirilir)
- **Testler:** `tests/NovaCore.Api.Tests/NovaCoreApiFactory.cs`, `Architecture/ModularityTests.cs`
- **Dokümanlar:** `README.md`, `CLAUDE.md`, `src/**/CLAUDE.md`, memory dosyaları (`novacore-stack`, `novacore-security`)

## 7. Doğrulama

- **Her faz sonunda:**
  - `dotnet restore --force` → NU190x uyarısı yok
  - `dotnet build -c Release -warnaserror` → 0 uyarı
  - `dotnet test` → yeşil
  - `dotnet list package --vulnerable --include-transitive` → temiz
- **Faz 0:**
  - Yeni rate limit testi: bir IP limiti aştığında diğer IP'nin 200 aldığı doğrulanır.
  - `IDomainEvent` birim testi: `EventId` iki okumada aynı kalmalı.
  - Production ortamında `IEmailSender` eksikse açılış hata vermeli.
- **Faz 1:** `dotnet pack -c Release` → `.nupkg` + `.snupkg` üretilir. İçinde XML dokümanları var, package validation geçiyor. Bir public API bilerek kırıldığında `pack` hata vermeli (baseline sonrası).
- **Faz 2:** Uygulama çalıştırılıp built-in browser'da Scalar açılır. Hatalı DTO ile 400 ValidationProblem döndüğü, izinsiz istekte 403 döndüğü ve `/health`'in DB kontrolünü içerdiği görülür. Çekirdek paketlerin bağımlılık grafiği `dotnet nuget why` ile doğrulanır (üçüncü taraf paket yok).
- **Faz 3:** OpenIddict (veya Keycloak) ile Authorization Code + PKCE akışında token alınır. API'nin JWKS üzerinden doğruladığı, izinlerin sunucu tarafında çözüldüğü ve rol değişikliğinin cache TTL içinde yansıdığı test edilir.
- **Faz 4:** Entegrasyon testi, eşzamanlı iki ödeme isteğinde stoğun eksiye düşmediğini doğrular. Outbox mesajının bir kez işlendiği de test edilir.
- **Faz 5:** Paket feed'inden `dotnet new novacore` ile üretilen yeni projede `dotnet build && dotnet test` geçer.

## 8. Bilerek yapılmayacaklar (tek geliştirici için aşırı mühendislik)
- Mikroservislere bölmek, mesaj kuyruğu (outbox in-process dispatch ile yeterli).
- MediatR veya başka bir mediator; tam Clean/DDD katmanlaması (yalnızca karmaşık bir modül gerçekten gerektirirse).
- Aspire'ı zorunlu tutmak (opsiyonel kalabilir). NativeAOT hedefi (şimdilik yok, ama açık modül kaydı bu yolu açık tutar).
