# NovaCore yeniden yapılanma planı (yürütülebilir)

> **Bu dosya korunur.** Değişiklikler yalnızca kullanıcı onayıyla yapılır; hook her düzenlemede onay ister.
> Gerekçeler: [ANALYSIS.md](ANALYSIS.md) · Kararlar ve yasaklar: [DECISIONS.md](DECISIONS.md) · Durum: [PROGRESS.md](PROGRESS.md) · Araştırma kayıtları: [RESEARCH.md](RESEARCH.md)
>
> Her faz yeni bir oturumda `/novacore-phase <faz-id>` ile başlatılır.

## Genel kurallar (her faz için geçerli)

- **Öncelik sırası:** DECISIONS.md > PLAN.md > PROGRESS.md > CLAUDE.md > konuşma geçmişi. Bir çelişki görürsen dur ve sor.
- **Branch:** Her faz `main`'den açılan `faz/<id>-<kısa-ad>` branch'inde yürür. `main`'e merge yalnızca kullanıcı onayıyla yapılır.
- **Commit:** Her adım ayrı bir commit'tir. Mesaj biçimi `faz<id>(<adım-id>): <özet>`, örneğin `faz0(F0.3): partition strict rate limiter by client IP`.
- **Her adımdan sonra:** `dotnet build NovaCore.slnx` ve `dotnet test NovaCore.slnx` çalıştırılır. Kırmızıyken bir sonraki adıma geçilmez.
- **Test önce gelir:** Bir hata düzeltmesinde önce hatayı gösteren test yazılır. Test kırmızı görülür, sonra düzeltme yapılır.
- **Kapsam:** Yalnızca o adımın "Dosyalar/Kapsam" satırındaki işler yapılır. Başka her şey sapma protokolüne (skill §4) tabidir.
- **Dokümanlar:** Bir faz bir konvansiyonu değiştirirse aynı fazda README.md, ilgili CLAUDE.md ve memory dosyaları da güncellenir.
- **Adım ID'leri sabittir.** Bir adım bölünecekse `F2b.1a` gibi alt ID'ler kullanılır. Adımlar yeniden numaralandırılmaz.

## Faz sırası ve bağımlılıklar

```
0 → 1 → 2a → 2b → 2c → 3a → 3b → 4a → 4b → 5
```
Bir faza başlamadan önce kendisinden önceki tüm fazlar PROGRESS.md'de "tamam" olarak işaretlenmiş olmalıdır.

---

## Faz 0: Stabilizasyon (güvenlik ve doğruluk)

**Amaç:** Bilinen güvenlik açıklarını ve canlı hataları kapatmak. Git ve CI ile kontrol noktalarını kurmak. Mimari yapıya dokunulmaz.

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F0.1 | **Git başlangıcı.** `git init`, `.gitignore` kontrol edilir (`.claude/hooks/.last-verified` eklenir). Kod değişikliği yapılmadan "baseline" commit'i atılır. LICENSE (türü Q-02'ye göre) ve CHANGELOG.md (Keep a Changelog biçimi) eklenir. | kök | `git log` baseline commit'ini gösterir, çalışma ağacı temizdir |
| F0.2 | **Paketler ve açıklar.** Microsoft paketleri en son 10.0.x yamasına çıkarılır. EF paketleri tek sürüme hizalanır. OTel ve Scalar minör güncellemeleri yapılır. Major güncellemeler (xUnit v3 → 4.x) **yapılmaz**, PROGRESS'teki "Sonraya bırakılanlar" listesine yazılır. Açıklar için önce top-level paket güncellenir. Açık kalırsa transitive pinning kullanılır; pack üzerindeki etkisi için R-014'e bak, pinning öncesinde kullanıcıya sorulur. `WarningsAsErrors` içine `NU1903;NU1904` eklenir. | `Directory.Packages.props`, `Directory.Build.props` | `dotnet list NovaCore.slnx package --vulnerable --include-transitive` temiz. `dotnet restore NovaCore.slnx --force` NU190x vermiyor. Build ve test yeşil |
| F0.3 | **Rate limiter.** `"strict"` policy'si IP'ye göre bölünmüş bir policy'ye çevrilir (`AddPolicy` + `RateLimitPartition.GetFixedWindowLimiter`, R-004). Test host'ta istemci IP'sini simüle eden bir yardımcı eklenir. | `CoreWebExtensions.cs`, testler | Yeni test: bir IP limiti aşınca 429 alır, farklı bir IP 200 alır |
| F0.4 | **IDomainEvent.** `EventId` ve `OccurredOnUtc` oluşturma anında sabitlenir (ör. `abstract record DomainEvent : IDomainEvent`). | `Core.Abstractions/Messaging/IDomainEvent.cs`, birim testi | Aynı event'ten iki okuma aynı `EventId`'yi döner |
| F0.5 | **E-posta gönderici.** `LoggingEmailSender` yalnızca Development'ta kaydedilir. Diğer ortamlarda `IEmailSender` kayıtlı değilse açılış anlamlı bir hata ile durur. | `IdentityModule.cs`, testler | Production ortamında `IEmailSender` eksikken host açılmaz. Development'ta açılır |
| F0.6 | **Ölü kod ve doküman düzeltmesi.** Scrutor ve Asp.Versioning (paket referansı + `AddApiVersioning`) kaldırılır. `AddOutputCache`/`UseOutputCache` kaldırılır. "Postgres" yorumu silinir. README paket tablosu ve CLAUDE.md'deki "0 warnings" notu güncellenir: artımlı derleme audit uyarılarını göstermez. | `Core.Web`, `Core.Infrastructure` csproj, `Directory.Packages.props`, README, CLAUDE.md, `.claude/hooks/package-policy.json` (allowlist'ten çıkarma, onaylı) | `grep` kaldırılanlara referans bulmaz. Build ve test yeşil |
| F0.7 | **Yasakları testlere çevirme (D-007).** DECISIONS "Yapılmayacaklar" listesindeki test edilebilir maddeler mimari testlere dönüştürülür:<br>• Y-01/Y-32/Y-33: yasaklı paket referansı yok (MediatR, AutoMapper, FluentAssertions, EF InMemory, EF Proxies).<br>• Y-08: `NovaCore.Modules.*` taranarak hiçbir modül başka bir modülün implementasyonuna (Contracts hariç) referans vermiyor.<br>• Y-15: `IRepository`/`IUnitOfWork`/`Repository<` adlı tip yok.<br>• Y-17: modül projelerinde yalnızca izin verilen sonekler (`NovaCore.Modules.<M>` ve `.Contracts`).<br>• Y-29: `Helper(s)`/`Util(s)`/`Utility`/`Manager`/`Common` adlı tip yok.<br>• Y-26: test host `ValidateScopes` + `ValidateOnBuild` ile ayağa kalkıyor.<br>• Y-31: endpoint imzalarında `*.Domain` tipi yok. Reflection ile uygulanabilirliği değerlendirilir; uygulanamazsa gerekçesi PROGRESS'e yazılır ve kullanıcıya bildirilir.<br>• Y-38 (opsiyonel): `src/` altında TODO/FIXME yok.<br>Mevcut kod 2026-09-25 itibarıyla Y-15/Y-17/Y-29/Y-38'i ihlal etmiyor. Bir test mevcut bir ihlali ortaya çıkarırsa sapma protokolü uygulanır. Bu adım daha önce Faz 5'teydi, erken koruma için buraya taşındı. | `tests/.../Architecture/*`, `NovaCoreApiFactory.cs` | Yeni testler yeşil. Her kural için kasıtlı bir ihlal eklendiğinde kırmızıya dönüyor (yerelde denenir, commit'lenmez) |
| F0.8 | **CI.** `.github/workflows/ci.yml` eklenir: restore (audit hata sayılır), build, test, `dotnet ef migrations has-pending-model-changes` (3 context), vulnerable check. Remote yoksa (Q-03) workflow yazılır ve aynı komutlar yerelde çalıştırılarak doğrulanır. | `.github/workflows/ci.yml` | Komutlar yerelde yeşil. Remote varsa ilk CI çalışması yeşil |
| F0.9 | **(Koşullu) Identity minimal düzeltmeleri.** Yalnızca kullanıcı "Faz 3'e kadar Identity kullanılacak" derse yapılır (fazın başında sorulur): onay linki `Request.Host` yerine config'teki bir taban URL ile üretilir; hatalı 2FA kodu `AccessFailedAsync` çağırır; refresh rotation koşullu güncelleme ile yarış durumuna karşı korunur. | `Identity/Features/*`, `AuthTokenService.cs` | Her biri için bir test |

**Bu fazda yapılmayacaklar:** proje bölme veya yeniden adlandırma (2a), Options pattern dönüşümü (2b), FluentValidation'ın kaldırılması (2c), Identity'nin yeniden tasarımı (3), major paket güncellemeleri.

**Faz doğrulaması:**
```
dotnet restore NovaCore.slnx --force
dotnet build NovaCore.slnx -c Release
dotnet test NovaCore.slnx
dotnet list NovaCore.slnx package --vulnerable --include-transitive
git log --oneline
```

---

## Faz 1: Kütüphane altyapısı (versiyonlanabilirlik)

**Amaç:** Paketlenebilir projeleri Microsoft kütüphane rehberine (R-009, R-011) uygun hale getirmek. **Bu fazda paket yayınlanmaz.** Paket isimleri Faz 2a'da değişeceği için ilk yayın Faz 2a sonunda yapılır.

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F1.1 | **Build ayarları.** Tüm projeler için `AnalysisLevel=latest-recommended`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`. `IsPackable=true` olanlar için koşullu PropertyGroup: `GenerateDocumentationFile`, `PackageLicenseExpression`, `PackageReadmeFile`, `RepositoryUrl`, `Authors`, `IncludeSymbols` + `snupkg`, CI'da `ContinuousIntegrationBuild`, `EnablePackageValidation`. Packable projelerde CS1591 artık bastırılmaz. | `Directory.Build.props` | Ayarlar aktif. Açığa çıkan uyarılar F1.2'nin girdisidir |
| F1.2 | **Uyarıların düzeltilmesi.** Analyzer ve XML doc uyarıları düzeltilir. Uyarı bastırmak (`NoWarn`, `#pragma`, `.editorconfig` severity düşürme) **yalnızca kullanıcı onayıyla** yapılır (Y-10). Uyarı sayısı çok fazlaysa proje bazında alt adımlara (F1.2a, F1.2b...) bölünür. | Kaynak kod | `dotnet build -c Release` 0 uyarı ve 0 hata |
| F1.3 | **Versiyon.** `Directory.Build.props` içinde `VersionPrefix=0.1.0` tanımlanır. API kararlı olmadığı için sürümler 0.x'tir, 1.0.0 kararı Faz 5'te verilir. Prerelease için CI `VersionSuffix` ekler. Ek paket kullanılmaz. | `Directory.Build.props` | `dotnet pack` çıktısında sürüm doğru |
| F1.4 | **Public API takibi.** `Microsoft.CodeAnalysis.PublicApiAnalyzers` eklenir (`PrivateAssets=all`). Her packable projeye `PublicAPI.Shipped.txt` ve `PublicAPI.Unshipped.txt` eklenir. | packable csproj'lar | Public API değişikliği analyzer uyarısı üretir |
| F1.5 | **Pack doğrulaması.** `dotnet pack -c Release -o artifacts` çalıştırılır. `.nupkg` içinde XML doc, README ve lisans, yanında `.snupkg` olmalı. | — | İçerik kontrolü raporlanır. `artifacts/` commit'lenmez |
| F1.6 | **Yayın akışı.** `.github/workflows/release.yml` hazırlanır: `v*` tag'inde pack yapılır ve GitHub Packages'a push edilir. Feed bilgisi Q-03'e bağlı. Workflow yazılır, **tetiklenmez**. | `.github/workflows/release.yml` | Workflow sözdizimi geçerli |

**Bu fazda yapılmayacaklar:** paket yayınlamak, proje yapısını değiştirmek, davranış değişikliği yapmak.

**Faz doğrulaması:** `dotnet build NovaCore.slnx -c Release` (0 uyarı), `dotnet test NovaCore.slnx`, `dotnet pack NovaCore.slnx -c Release -o artifacts`.

---

## Faz 2a: Paket yapısı (saf yapısal taşıma)

**Amaç:** Hedef paket haritasına geçmek (ANALYSIS §4). **Davranış değişmez.** Mevcut tüm testler, test kodunda yalnızca namespace ve using güncellemesi yapılarak aynen geçmelidir.

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F2a.1 | **Çekirdek projeler.** `NovaCore.Abstractions` (eski Core.Abstractions), `NovaCore.AspNetCore` (Core.Web + Core.Infrastructure'daki HTTP parçaları; HybridCacheService geçici olarak burada), `NovaCore.EntityFrameworkCore` (Persistence + Messaging). Namespace ve slnx güncellenir. Bağımlılık yönü: `Abstractions ← AspNetCore`, `Abstractions ← EntityFrameworkCore`. EntityFrameworkCore ile AspNetCore birbirine referans vermez. | `src/BuildingBlocks/**`, modül csproj'ları, slnx | Build ve test yeşil |
| F2a.2 | **Eklenti paketleri.** `NovaCore.Serilog`, `NovaCore.OpenTelemetry` ve `NovaCore.Scalar` oluşturulur. `AddCoreDefaults`/`AddCoreWeb` içindeki ilgili kod bu paketlere taşınır. Api host'u bunları açıkça ekler. | yeni projeler, `Program.cs` | Çekirdek paketlerde Serilog, OTel ve Scalar referansı yok |
| F2a.3 | **Açık modül kaydı.** Reflection taraması kaldırılır. Yerine `builder.AddModule<TModule>()` gelir ve `MapModules` modül listesini DI'dan okur. `IModule.IsEnabled` için kullanıcıya sorulur: config'e bağlansın mı, kaldırılsın mı? | `Modules/*`, `Program.cs` | Test: kayıt sırası ve endpoint'ler aynı |
| F2a.4 | **Mimari testler.** Çekirdek paketlerde Microsoft/BCL dışı referans olmadığı (Y-05) ve bağımlılık yönü test edilir. | `tests/.../Architecture/*` | Yeşil |
| F2a.5 | **Dokümanlar.** README (mimari ve proje ağacı), CLAUDE.md dosyaları (yeni isimler), memory `novacore-stack` güncellenir. | dokümanlar | Eski proje isimleri dokümanlarda geçmiyor |
| F2a.6 | **İlk önizleme sürümü (opsiyonel, kullanıcı onayıyla).** `0.1.0-preview.1` pack edilir. Feed hazırsa push edilir. | — | Kullanıcı onayı |

**Bu fazda yapılmayacaklar:** Options pattern, validation, yetkilendirme veya kimlik değişikliği. Kısaca: hiçbir davranış değişikliği.

---

## Faz 2b: Konfigürasyon ve çalışma zamanı doğruluğu

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F2b.1 | **Options pattern (R-005).** `Jwt`, `RateLimiting`, `Database`, `Cors` ve `Cache` için options sınıfları, `AddOptions<T>().BindConfiguration()`, `[OptionsValidator]` source-gen ve `ValidateOnStart()`. `JwtBearerOptions` `IConfigureNamedOptions` ile, `RateLimiterOptions` `IOptions<T>` üzerinden yapılandırılır. `DatabaseMigrationService` her zaman kayıtlı olur ve bayrağı `StartAsync` içinde okur. Kayıt anında `configuration.GetValue` kalmaz. | `NovaCore.AspNetCore`, `NovaCore.EntityFrameworkCore` | Geçersiz config açılışta anlamlı hata verir (test ile) |
| F2b.2 | **Test host sadeleştirme.** `Environment.SetEnvironmentVariable` hack'i kaldırılır. `ConfigureAppConfiguration` yeterli olur. CLAUDE.md dosyalarındaki "eager config" gotcha notları güncellenir. | `NovaCoreApiFactory.cs`, CLAUDE.md'ler | Testler env-var olmadan yeşil |
| F2b.3 | **TimeProvider (R-012).** `TimeProvider.System` kaydedilir. Audit interceptor, token servisi ve refresh token süre kontrolü bunu kullanır. Testlerde `FakeTimeProvider` kullanılır. | ilgili sınıflar, testler | Kodda `DateTimeOffset.UtcNow` kalmaz (entity varsayılan değerleri dahil; istisna varsa gerekçesi yazılır) |
| F2b.4 | **Exception handling.** Yalnızca `AppException` mesajları istemciye yansıtılır. `ArgumentException`, `UnauthorizedAccessException` ve benzerlerinde mesaj gizlenir. ProblemDetails'teki `Instance` alanı geçerli bir URI olur. | `GlobalExceptionHandler.cs`, testler | Test: iç mesaj yanıtta yok |
| F2b.5 | **Health (R-006).** Modül kaydında `AddDbContextCheck<T>()` ("ready" etiketiyle) eklenir. `/health` için production koruma yöntemi kullanıcıya sorulur: yalnızca dev, `RequireHost`/port ya da yetkilendirme. | host uzantıları | Test: DB kontrolü health raporunda yer alıyor |
| F2b.6 | **`[LoggerMessage]` (R-013).** SecurityAuditLogger, GlobalExceptionHandler, DatabaseMigrationService ve seeder bu yapıya çevrilir. | ilgili sınıflar | CA1848 uyarısı yok |

---

## Faz 2c: Yerleşik karşılıklara geçiş

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F2c.1 | **Validation (R-001).** FluentValidation yerine `AddValidation()` + DataAnnotations/`IValidatableObject` kullanılır. Her modüle kendi assembly'sinde bir `AddXxxValidation()` eklenir. `ValidationFilter` ve `ValidationExceptionHandler` kaldırılır. 400 ProblemDetails biçimi testlerle korunur. DataAnnotations ile ifade edilemeyen bir kural varsa **önce kullanıcıya sorulur**. | modül Features, AspNetCore | Mevcut validation testleri yeşil |
| F2c.2 | **Yetkilendirme (R-002).** `PermissionPolicyProvider` yerine `RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData` ve handler gelir. `RequirePermission(...)` extension imzası korunur. | `Authorization/*` | 200/403 testleri yeşil |
| F2c.3 | **Cache (R-008).** `ICacheService` ve `HybridCacheService` kaldırılır. `AddHybridCache` doğrudan kullanılır. EXTEXP0018 `NoWarn`'dan çıkarılmayı dener; uyarı gelirse kaynağıyla birlikte kullanıcıya sorulur. | Abstractions, AspNetCore | Build 0 uyarı |
| F2c.4 | **Paket temizliği.** FluentValidation paketleri CPM'den ve `package-policy.json` allowlist'inden çıkarılır (allowlist değişikliği hook onayı gerektirir). | props, policy | Referans yok |

---

## Faz 3a: Kimlik doğrulama çekirdeği (standart JWT doğrulama)

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F3a.1 | **Doğrulama.** `AddNovaAuthentication()`: JwtBearer `Authority` + `Audience` ile çalışır, JWKS keşfi ve asimetrik anahtar kullanılır (R-010). Simetrik anahtar yalnızca açık bir test/dev bayrağıyla kullanılabilir. | `NovaCore.AspNetCore/Auth/*` | Test: RSA ile imzalı token kabul edilir, yanlış issuer/audience/imza reddedilir |
| F3a.2 | **İzin çözümleme.** Bir `IClaimsTransformation`, kullanıcının izinlerini sunucu tarafında çözer: `IPermissionResolver` soyutlaması, HybridCache, anahtar `sub` + security stamp. JWT'deki `permission` claim'lerine bağımlılık kaldırılır. | Authorization | Test: rol değişikliği cache TTL içinde yansır |
| F3a.3 | **Test yardımcısı.** Bellek içi RSA anahtarıyla token üreten bir test IdP yardımcısı yazılır. | tests | Mevcut auth testleri bununla yeşil |

**Faz sonunda kullanıcıya sorulacak:** IdP seçimi (OpenIddict eklentisi, Keycloak ya da ikisi birden).

## Faz 3b: IdP

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F3b.1 | **`NovaCore.Identity.OpenIddict` eklentisi.** OpenIddict paketleri eklenir (hook onayı gerekir). ASP.NET Core Identity + OpenIddict server: Authorization Code + PKCE, refresh token rotation, client credentials. RS256/ES256 anahtarları: dev'de ephemeral, prod'da sertifika. | yeni proje | Uçtan uca token akışı çalışır |
| F3b.2 | **Sertleştirmelerin taşınması.** PBKDF2 600k, lockout, e-posta onayı, audit logger, admin/rol uç noktaları. | yeni proje | Güvenlik testleri taşınmış ve yeşil |
| F3b.3 | **Eski modülün emekliye ayrılması.** `NovaCore.Modules.Identity` kaldırılır. Silme işlemi kullanıcı onayıyla yapılır. | — | Referans yok |
| F3b.4 | **docker-compose.** Opsiyonel Keycloak servisi ve örnek realm eklenir. SA parolası `.env`'e taşınır. İmaj sürümleri pinlenir (`:latest` kalmaz). | `docker-compose.yml`, `.env.example` | `docker compose config` geçerli |
| F3b.5 | **Uçtan uca doğrulama.** Akış built-in browser ile Scalar üzerinden denenir. | — | Rapor ve ekran görüntüsü |

---

## Faz 4a: Outbox ve domain event dağıtımı

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F4a.1 | **Event'lerin outbox'a yazılması.** Bir SaveChanges interceptor, event'leri aynı transaction içinde modülün `outbox_messages` tablosuna yazar (her modül kendi şemasında; migration'lar eklenir). | `NovaCore.EntityFrameworkCore` | Test: event ile entity aynı transaction'da yazılır |
| F4a.2 | **Dispatcher.** `OutboxDispatcher : BackgroundService` scope başına çalışır. `EventId` ile idempotent (processed tablosu), retry yapar ve execution strategy ile uyumludur. | aynı | Test: bir mesaj yalnızca bir kez işlenir |
| F4a.3 | **Eski dispatcher düzeltmesi.** Singleton dispatcher'ın scope sorunu giderilir. Reflection çağrısı cache'lenmiş generic wrapper'a çevrilir. | Messaging | Scoped handler ile test |

## Faz 4b: Örnek modüller (referans implementasyon)

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F4b.1 | **Karar (kullanıcı).** Stok rezervasyonu senkron bir contract (`ReserveStockAsync`) ile mi yapılacak, event ile mi? Karar DECISIONS'a yazılır. | — | D-kaydı |
| F4b.2 | **Uygulama.** Seçilen yöntem uygulanır. Ayrıca `Payment` durum makinesi, `Product` invariant'ları ve Id bazlı entity eşitliği eklenir. | Catalog, Payments, Abstractions | Birim testleri |
| F4b.3 | **Eşzamanlılık testi.** Aynı ürün için iki eşzamanlı ödemede stok eksiye düşmez. | tests | Yeşil |

---

## Faz 5: Template, tüketici testi, 1.0 hazırlığı

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F5.1 | **Template.** Yalnızca Api host + 1 örnek modül üretir ve NovaCore paketlerine PackageReference ile bağlanır. | `.template.config`, template içeriği | `dotnet new novacore` ile build alınır |
| F5.2 | **Tüketici smoke testi (CI).** Paketler pack edilir, yerel feed'e konur, `dotnet new` ile proje üretilir, build ve test çalıştırılır. | CI | Yeşil |
| F5.3 | **Test borcu.** Catalog ve Payments için entegrasyon testleri yazılır. Testcontainers opsiyoneldir ve paket hook onayı gerektirir. | tests | Yeşil |
| F5.4 | **1.0.0 kararı (kullanıcı).** Onaylanırsa `PublicAPI.Shipped` güncellenir ve package validation baseline ayarlanır. | props | Karar D-kaydı |
| F5.5 | **Final dokümanlar.** README (tüketici odaklı), CHANGELOG ve memory güncellenir. | dokümanlar | — |
