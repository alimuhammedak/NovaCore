# NovaCore yeniden yapılanma planı (yürütülebilir)

> **Bu dosya korunur.** Değişiklikler yalnızca kullanıcı onayıyla yapılır; hook her düzenlemede onay ister.
> Gerekçeler: [ANALYSIS.md](ANALYSIS.md) · Kararlar ve yasaklar: [DECISIONS.md](DECISIONS.md) · Durum: [PROGRESS.md](PROGRESS.md) · Araştırma kayıtları: [RESEARCH.md](RESEARCH.md)
>
> Her faz yeni bir oturumda `/novacore-phase <faz-id>` ile başlatılır.

## Genel kurallar (her faz için geçerli)

- **Öncelik sırası:** DECISIONS.md > PLAN.md > PROGRESS.md > CLAUDE.md > konuşma geçmişi. Bir çelişki görürsen dur ve sor.
- **Branch:** Her faz `main`'den açılan `faz/<id>-<kısa-ad>` branch'inde yürür. `main`'e merge yalnızca kullanıcı onayıyla yapılır.
- **Commit:** Her adım ayrı bir commit'tir. Mesaj biçimi `faz<id>(<adım-id>): <özet>`, örneğin `faz0(F0.3): partition strict rate limiter by client IP`.
- **Her adımdan sonra (D-088):** Yazan oturum (`/novacore-phase`) `dotnet build NovaCore.slnx`'i çalıştırır ve adımın yeni ya da değişen test sınıflarını koşar. Kırmızıyken bir sonraki adıma geçilmez. Tam paket (`dotnet test --solution NovaCore.slnx`) her yazan oturumdan sonra ayrı bir oturumda `/novacore-verify <faz-id>` ile çalışır. Açık bir doğrulama bulgusu ya da önceki bir oturumdan kalan doğrulanmamış bir kod commit'i varken yeni adıma başlanmaz.
- **Test önce gelir:** Bir hata düzeltmesinde önce hatayı gösteren test yazılır. Test kırmızı görülür, sonra düzeltme yapılır. Kırmızı, adımın kendi test sınıfında görülür.
- **Kapsam:** Yalnızca o adımın "Dosyalar/Kapsam" satırındaki işler yapılır. Başka her şey sapma protokolüne (skill §4) tabidir.
- **Dokümanlar:** Bir faz bir konvansiyonu değiştirirse aynı fazda README.md, ilgili CLAUDE.md ve memory dosyaları da güncellenir.
- **Adım ID'leri sabittir.** Bir adım bölünecekse `F2b.1a` gibi alt ID'ler kullanılır. Adımlar yeniden numaralandırılmaz.

## Faz sırası ve bağımlılıklar

```
0 → 1 → 2a → 2b → 2c → 3a → 3b → 4a → 4b → 4c → 4d → 5 → 6a → 6b → 6c → 6d → 7a → 7b → 6e
```
Bir faza başlamadan önce kendisinden önceki tüm fazlar PROGRESS.md'de "tamam" olarak işaretlenmiş olmalıdır. 4d koşulludur: F4d.1'deki karar fazı gereksiz kılarsa faz `✅ tamam (atlandı)` olarak işaretlenir. 4c ve 4d, 2026-09-28'de tüketici hazırlığı araştırmasıyla eklendi (D-033, [proposals/tuketici-hazirligi.md](proposals/tuketici-hazirligi.md)). 6a–6e, 2026-10-21'de güvenlik ve kalite araştırmasıyla eklendi (D-042…D-049, [proposals/guvenlik-kalite-kararlari.md](proposals/guvenlik-kalite-kararlari.md)). 7a ve 7b, 2026-10-23'te CRM-Nominal geçişi değerlendirmesiyle eklendi (D-068…D-071, [proposals/crm-cok-kiracililik.md](proposals/crm-cok-kiracililik.md)). 6e'den önce yürürler: 6e'nin denetimleri son kod yüzeyini görmeli (D-071 (3)).

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

## Faz 4b: Referans modüller (doğruluk ve event akışı)

**Amaç:** Catalog ve Payments, NovaCore'un repo içindeki referans uygulaması ve paketlerin gerçek kullanım testidir. Bu faz iki açığı kapatır:
- Ödeme stoğu kontrol ediyor ama düşmüyor, yani sınırsız satış mümkün (ANALYSIS P1-5).
- 4a'nın outbox'ını hiçbir modül kullanmıyor.

Şablon (F5.1) bu modülleri kopyalamaz (D-033).

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F4b.1 | **Karar (kullanıcı).** Stok rezervasyonu senkron bir contract (`ReserveStockAsync`) ile mi yapılacak, event ile mi? Karar DECISIONS'a yazılır. | — | D-kaydı |
| F4b.2 | **Uygulama.** Seçilen yöntem uygulanır. Entity'ye yalnızca bu akışın gerektirdiği kurallar girer: stok eksiye düşmez; akış `Payment`'ın durumunu değiştiriyorsa yalnızca geçerli geçişlere izin verilir. Akışın kullanmadığı durum metotları silinir (bugün hiçbiri çağrılmıyor). | Catalog, Payments (event seçilirse `Payments.Contracts`) | Birim testleri |
| F4b.3 | **Eşzamanlılık testi.** Aynı ürün için iki eşzamanlı ödemede stok eksiye düşmez. | tests | Yeşil |
| F5.3 | **Test borcu (Faz 5'ten taşındı, ID korunur, D-033).** Catalog ve Payments için geçerli gövdeyle uçtan uca entegrasyon testleri: ürünü oluştur, oku, güncelle, sil; ödeme yap. Testcontainers opsiyoneldir ve paket hook onayı gerektirir. | tests | Yeşil |

**Bu fazda yapılmayacaklar:**
- Abstractions'ta Id bazlı entity eşitliği: kodda entity karşılaştıran yok (Y-14).
- Akışın gerektirmediği genel `Product` invariant'ları: basit CRUD'da anemik entity kabul edilir (Y-16).
- Çekirdek paketlerde değişiklik: 4c'nin işi. 4b bir çekirdek sorunu bulursa sapma protokolü uygulanır.

---

## Faz 4c: Tüketici hazırlığı (çekirdek)

**Amaç:** Paketleri, sıfırdan açılan bir projenin üretimde güvenle kullanabileceği hale getirmek:
- güvenli varsayılanlar,
- proxy arkasında doğru davranış,
- projenin kendi hata eşlemesini ekleyebilmesi,
- tipli API sözleşmesi,
- tek event yolu.

Public API değişiklikleri ilk yayından önce burada toplanır. Gerekçe ve kaynaklar: [proposals/tuketici-hazirligi.md](proposals/tuketici-hazirligi.md), R-071…R-074.

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F4c.1 | **Güvenli varsayılanlar (R-073).** `AddCoreAuthorization` `FallbackPolicy = RequireAuthenticatedUser` kurar. Kasıtlı olarak açık uçlar `AllowAnonymous` alır: kimlik eklentisinin giriş, kayıt ve onay sayfaları ile `/connect/*` uçları, health, OpenAPI belgesi ve Scalar. `Cors:AllowedOrigins` boşsa Development dışında hiçbir origin'e izin verilmez; bugün her origin'e izin veriliyor. | `NovaCore.AspNetCore` (Authorization, Hosting, Web), `NovaCore.Identity.OpenIddict`, `NovaCore.Scalar` | Test: yetki meta verisi olmayan bir uç anonim çağrıya 401 döner; listelenen açık uçlar anonim çalışır; Production'da boş CORS listesiyle `Access-Control-Allow-Origin` dönmez |
| F4c.2 | **Super-admin sunucu tarafında.** `Administrator` bypass'ı token'daki rol claim'ine değil, sunucuda çözülen bilgiye dayanır (yöntem fazda araştırılır, `IPermissionResolver` sözleşmesi değişebilir). Rolünden çıkarılan kullanıcı `Cache:PermissionExpiration` içinde yetkisini kaybeder. | `Authorization/*`, `Identity/HttpContextCurrentUser.cs`, eklentinin resolver'ı | Test: rolü kaldırılan admin, önbellek süresi dolunca 403 alır |
| F4c.3 | **Proxy arkasında doğru istemci (R-072).** `UseCoreWeb`, `UseForwardedHeaders`'ı pipeline'ın başına koyar. Güvenilen proxy'ler ve ağlar config'ten okunur ve açılışta doğrulanır. Liste boşsa başlıklar işlenmez (framework varsayılanı). | `Web/CoreWebExtensions.cs`, yeni options | Test: güvenilen proxy'den gelen `X-Forwarded-For` rate limiter'ın istemciyi ayırdığı IP'yi belirler, güvenilmeyen kaynaktan gelen yok sayılır; `X-Forwarded-Proto: https` isteğin şemasını https yapar |
| F4c.4 | **Projenin hata eşlemesi (R-073).** Exception → durum kodu eşlemesi `ExceptionHandlerOptions.StatusCodeSelector` ve ProblemDetails özelleştirmesine taşınır. Böylece projenin `IExceptionHandler`'ları kayıt sırasından bağımsız olarak önce çalışır. `ArgumentException`, `FormatException`, `KeyNotFoundException` ve `UnauthorizedAccessException` artık 4xx'e eşlenmez: bunlar kod hatasıdır ve 500 döner. `BadHttpRequestException` kendi durum kodunu döner. `AppException` ailesi kalır. | `Exceptions/*`, `DependencyInjection/*`, `Web/CoreWebExtensions.cs` | Test: `AddCoreWeb`'den sonra kaydedilen bir handler çalışır; `ArgumentException` mesajsız 500 döner; bozuk JSON gövdesi 400 döner |
| F4c.5 | **Tipli yanıt sözleşmesi (R-073).** `Result`/`Result<T>`'nin HTTP'ye dönüşümü, başarı tipini handler'ın imzasında taşıyan `Results<…>` union'larına geçer. Modül ve eklenti handler'ları geçirilir; başarı için elle yazılan `.Produces<T>()` kalkar. | `Http/ResultHttpExtensions.cs`, modül `Features/*`, eklenti uçları, `EndpointContractTests` | OpenAPI belgesindeki yanıt şemaları aynı kalır ya da zenginleşir (fark raporlanır); Y-31 testi `IResult` kör noktası olmadan çalışır |
| F4c.6 | **Data Protection anahtar deposu (R-072).** Kimlik eklentisi, Development dışında açıkça yapılandırılmış bir anahtar deposu yoksa açılışı anlamlı bir hatayla durdurur (F0.5'teki `IEmailSender` kalıbı). Depoyu host seçer. | `NovaCore.Identity.OpenIddict` | Test: Production'da depo yokken host açılmaz, depo varken açılır; Development etkilenmez |
| F4c.7 | **Outbox şeması ve bakımı.** `outbox_consumers`'ın anahtarı (EventId, Handler) → (mesaj `Id`, Handler) olur ve cascade delete'li bir FK eklenir (4a code review #12; D-031 (5)'i değiştirir). İşlenmiş mesajlar sabit bir saklama süresinden sonra silinir (R-068). Kullanılmayan `IDomainEventDispatcher`/`DomainEventDispatcher` kaldırılır: üretim kodunda çağıranı yok, outbox dispatcher invoker'ları doğrudan kullanıyor (Y-13). Modül migration'ları yenilenir. | `NovaCore.EntityFrameworkCore/Messaging`, `NovaCore.Abstractions/Messaging`, modül migration'ları, testler | Test: işlenmiş mesaj saklama süresinden sonra silinir, tüketici kayıtları da onunla gider; `has-pending-model-changes` temiz |

**Bu fazda yapılmayacaklar:** Aşağıdakilerin bugün bir tüketicisi yok (Y-14). Hepsi PROGRESS → "Sonraya bırakılanlar"da, ne zaman gerekeceğini söyleyen koşuluyla duruyor.
- `UseCoreWeb`'e kimlik doğrulama ile yetkilendirme arasında bir middleware yeri. Bugün proje kendi middleware'ini `UseCoreWeb`'den sonra ekleyebiliyor.
- İzin önbelleğinin anahtarına `iss` ya da ek bir bölümleme eklemek (tek Authority).
- Request timeouts.
- Outbox'ın birden çok örnekte çalışması için kilit ve trace bağlamı taşıma.
- `ApplicationUser` genişletme ve token'a claim ekleme kancası.
- BFF paketi.

---

## Faz 4d: Kimlik eklentisi üretim eksikleri (koşullu)

**Amaç:** `NovaCore.Identity.OpenIddict`'i, kullanıcıları olan bir ürünün IdP'si olabilecek hale getirmek. Yalnızca ilk proje yerleşik IdP'yi kullanacaksa yapılır (F0.9 gibi).

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F4d.1 | **Karar (kullanıcı).** İlk proje hangi IdP modelini kullanacak: (a) kendi host'unda eklenti, (b) projeler arası tek bir merkezi IdP (eklenti ayrı bir host'ta), (c) harici IdP (Entra, Keycloak). (a) ya da (b) seçilirse faz yürür, (c) seçilirse atlanır. | — | D-kaydı |
| F4d.2 | **Parola sıfırlama ve onay e-postasının yeniden gönderilmesi.** Yanıtlar hesabın varlığını ele vermez, uçlar "strict" rate limit altındadır. `User` rolü yoksa kayıt 500 vermez. | eklenti, testler | Uçtan uca test: sıfırlama linkiyle yeni parola belirlenir; bilinmeyen e-postaya aynı yanıt döner |
| F4d.3 | **Kurtarma koduyla giriş.** Giriş sayfasının 2FA adımı kurtarma kodunu da kabul eder. Kod tek kullanımlıktır. | eklenti | Test |
| F4d.4 | **Güvenlik düzeltmeleri.** `/connect/endsession` bir onay formu gösterir (logout CSRF). Bilinmeyen kullanıcıda sahte bir hash doğrulaması yapılır, böylece yanıt süresi hesabın varlığını ele vermez. `change-password`'da yanlış parola lockout sayacını artırır. | eklenti | Her biri için test |
| F4d.5 | **Token temizliği.** Süresi dolmuş OpenIddict token ve authorization kayıtları periyodik olarak temizlenir (`PruneAsync`, yeni paket yok). | eklenti | Test: süresi dolmuş kayıt temizlenir |

**Bu fazda yapılmayacaklar:** userinfo, Razor/Identity UI, reference token (D-026); `ApplicationUser` genişletme (Y-14).

---

## Faz 5: Şablon, dağıtım, yayın hazırlığı

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F5.1 | **Template (R-074).** Ayrı bir template package projesi (`PackageType=Template`, şablonlar `content/` klasöründe). NovaCore paketlerine PackageReference ile bağlanır. Şablonun ürettikleri:<br>• Api host ve 1 örnek modül.<br>• Test projesi: factory ve test IdP.<br>• CI: restore, build, test, `has-pending-model-changes` ve context başına migration bundle.<br>• Üretim kontrol listesini içeren `appsettings`.<br>• Development dışı için bir health probe örneği (R-049).<br>Kimlik seçeneği MS şablonlarındaki `--auth` gibi bir parametredir: yerleşik IdP (eklenti) ya da yalnızca `Jwt:Authority`. Log hattı tektir; hangisi olacağı fazda bir R-kaydıyla seçilir. Repo'ya özgü dosyalar şablona girmez: BuildingBlocks kaynağı, `docs/plan`, `.claude/`, `release.yml`, PublicAPI dosyaları, yazar kimliği. | yeni template package projesi, `.template.config` taşınır | `dotnet new novacore` iki kimlik seçeneğiyle de build ve test geçer; üretilen projede repo'ya özgü dosya yok |
| F5.2 | **Tüketici smoke testi (CI).** Paketler pack edilir ve yerel feed'e konur. Template package kurulur, her kimlik seçeneğiyle bir proje üretilir, build ve test çalıştırılır. | CI | Yeşil |
| F5.3 | **Faz 4b'ye taşındı (D-033).** | — | — |
| F5.4 | **Yayın kararı (kullanıcı).** (1) Kanal: GitHub Packages (D-013; tüketen her proje ve CI bir token taşır) ya da nuget.org (D-013 değişir). (2) İlk sürüm: önerilen `0.x-preview`. 1.0.0 (`PublicAPI.Shipped` ve package validation baseline) ilk gerçek proje bu sürümle çalıştıktan sonra ayrı bir kararla verilir. (3) Tag ve yayın kullanıcı onayıyla yapılır. | props, gerekirse `release.yml` | Karar D-kaydı |
| F5.5 | **Final dokümanlar.** README tüketici odaklı olur ve şunları içerir:<br>• Sorumluluk modeli ve yeni proje kontrol listesi ([proposals/tuketici-hazirligi.md](proposals/tuketici-hazirligi.md) §2).<br>• Projenin `PostConfigure` ile ezebileceği ayarlar tablosu.<br>• Tarayıcı uygulamaları için BFF önerisi.<br>• Nesne düzeyi yetki (resource-based authorization) rehberi.<br>• Migration dağıtımı (bundle ya da script).<br>CHANGELOG, CLAUDE.md dosyaları ve memory güncellenir. Kullanıcı DECISIONS.md'yi hook korumasına geri ekler (D-032). | dokümanlar | — |

**Bu fazda yapılmayacaklar:** BFF şablonu ya da paketi (Y-14), yeni çekirdek özelliği.

---

## Faz 6: Güvenlik ve kod kalitesi (6a–6e)

Kaynak: [proposals/guvenlik-kalite-kararlari.md](proposals/guvenlik-kalite-kararlari.md) (kullanıcı kararları K1–K8 → D-042…D-049) ve [proposals/guvenlik-kalite-arastirmasi.md](proposals/guvenlik-kalite-arastirmasi.md) (bulgular, konumlar, birincil kaynaklar). Sıranın gerekçesi (belge §3):
- 6a ucuzdur ve CodeQL ile secret scanning'i açar; 6b'nin değişiklikleri de taranır.
- 6b, public kodda görünen doğrulanmış açıkları kapatır.
- 6c gerçek SQL Server'ı getirir; 6b'deki migration'ı ve eski kör noktaları sınar.
- 6d analizörleri, 6c'nin ölçüm araçları devredeyken sıkılaştırır.
- 6e en pahalı ve en gürültülü katmanı en son ekler.

## Faz 6a: Public geçiş ve CI/tedarik zinciri sertleştirme

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F6a.1 | **Plan kayıtları.** K1–K8 DECISIONS'a (D-042…D-050), Faz 6a–6e PLAN'a, durum PROGRESS'e işlenir. D-035 (7) ve D-039 (1) revizyon kuralıyla işaretlenir. | `docs/plan/*` | Belgeler tutarlı |
| F6a.2 | **Workflow sertleştirme.** Action'lar tam commit SHA'sına sabitlenir (`# vX.Y.Z` yorumuyla). `persist-credentials: false`. `run:` içindeki `${{ }}` `env:`'e taşınır. İzinler job düzeyindedir. `timeout-minutes` ve `concurrency` eklenir. actionlint + zizmor CI job'u. | `ci.yml`, `release.yml`, şablonun `ci.yml`'i | actionlint ve zizmor temiz; CI yeşil; şablon smoke testi yeşil |
| F6a.3 | **Lock dosyaları ve SDK.** Packable olmayan projelerde `RestorePackagesWithLockFile`. CI ve release'te `--locked-mode`. `global.json` `rollForward: disable`. | `Directory.Build.targets`, `global.json`, `packages.lock.json`, workflow'lar | `restore --locked-mode` geçer; lock'suz bir sürüm değişikliği locked restore'u kırar (yerelde denenir) |
| F6a.4 | **Lisans kapısı.** `nuget-license`; izin listesi MIT/Apache-2.0, Microsoft önekleri Y-06 gereği serbest. | `dotnet-tools.json`, `ci.yml` | Kapı bugünkü grafikte yeşil, izin dışı lisansta kırmızı (yerelde denenir) |
| F6a.5 | **Secret taraması.** Betterleaks: CI'da tam geçmiş taraması (SARIF), opt-in pre-commit (`core.hooksPath`), incelenmiş parmak izleriyle `.betterleaksignore`. | `ci.yml`, `.githooks/`, `.betterleaksignore` | Tam geçmiş temiz; staged sahte token commit'i durdurur |
| F6a.6 | **CodeQL ve Scorecard.** CodeQL `csharp`, `build-mode: manual` (source generator'lar yüzünden `none` değil). Scorecard action. | `codeql.yml`, `scorecard.yml` | CodeQL yeşil; Scorecard main'de ilk koşuda yeşil |
| F6a.7 | **Release tedarik zinciri.** Build ve yayın job'ları ayrılır. Yayın job'u `environment: release`'tedir; `id-token`/`attestations`/`packages: write` yalnızca orada. CycloneDX SBOM + `actions/attest` build provenance. | `release.yml`, `dotnet-tools.json` | actionlint ve zizmor temiz; gerçek koşu sonraki `v*` tag'iyle |
| F6a.8 | **docker-compose.** Portlar `127.0.0.1`'e bağlanır, image'lar digest ile sabitlenir. | `docker-compose*.yml` | `docker compose config` geçerli |
| F6a.9 | **Dependabot.** `nuget`, `github-actions`, `dotnet-sdk`, `docker-compose` ekosistemleri; cooldown 7 gün; şablon CPM dosyası hariç. | `.github/dependabot.yml`, şablon | Lint temiz; ilk koşu merge'den sonra doğrulanır |
| F6a.10 | **Claude hook genişletmesi (K7).** SHA'ya sabitlenmemiş `uses:` ve sır desenleri için onay. | `.claude/hooks/` (korumalı) | Her kural için tetikleyen/tetiklemeyen girdi denenir |
| F6a.11 | **GitHub ayarları.** Secret scanning + push protection, Dependabot alerts ve security updates, `main` ve `v*` ruleset'leri, `release` environment'ı. Paket görünürlüğü (D-050) kullanıcıda. | repo ayarları (kullanıcı onayıyla) | Ayarlar API'de görünür |
| F6a.12 | **Dokümanlar.** README (CI/güvenlik, lock dosyası, pre-commit, attestation doğrulama), CLAUDE.md'ler, CHANGELOG, memory. | dokümanlar | — |

**Bu fazda yapılmayacaklar:** 6b–6e'nin işleri; `src/` altındaki uygulama kodu; yeni NuGet `PackageReference`.

**Faz doğrulaması:** `dotnet restore NovaCore.slnx --force`, `dotnet restore NovaCore.slnx --locked-mode`, `dotnet build NovaCore.slnx -c Release`, `dotnet test NovaCore.slnx`, `dotnet list NovaCore.slnx package --vulnerable --include-transitive`, lisans kapısı, Betterleaks tam geçmiş taraması, actionlint + zizmor, `docker compose config`, `bash eng/template-smoke.sh`, faz branch'inde CI.

## Faz 6b: Kimlik güvenlik sınırı (K5, K6)

**Kural:** Her madde önce kırmızı testle başlar.

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F6b.1 | **MFA.** TOTP tekrar kullanımı engellenir: son kabul edilen zaman adımı saklanır (ASVS 6.5.1). Kurtarma kodları hash'lenir (ASVS 6.5.2). `/2fa/setup`, `/enable`, `/disable` ve kurtarma kodu yenileme yeniden kimlik doğrulama ister (ASVS 7.5.1). Administrator için 2FA zorunludur. | `NovaCore.Identity.OpenIddict` | Her biri için önce kırmızı, sonra yeşil test |
| F6b.2 | **Yetki yükseltme.** Administrator'ı yalnızca Administrator verebilir (`identity:roles:manage`). Son admin korunur. Rol değişikliği izin önbelleğini siler ve refresh token'ları iptal eder. | eklenti, `NovaCore.AspNetCore` Authorization | Test |
| F6b.3 | **Seeding ve Payments (K6).** Rol izinleri açık listeye döner. `Payment.OwnerId` + migration. `GetPayments` çağıranın kendi ödemelerine filtrelenir; bütün liste ayrı bir izinle. | eklenti seeding, Payments modülü | Test; `has-pending-model-changes` temiz |
| F6b.4 | **Token/protokol.** `ValidTypes = ["at+jwt"]`. `/connect/authorize` ve `/account/*` CORS dışında (RFC 9700 §2.6). Development dışında `http` redirect URI reddi. Client secret minimum gücü. Mutlak refresh ömrü: önce `FakeTimeProvider` testiyle ölçülür; açık çıkarsa `DisableSlidingRefreshTokenExpiration`. | `NovaCore.AspNetCore` Authentication, eklenti | Test |
| F6b.5 | **Şifre politikası.** En az 15 karakter ve yerel blocklist; dış çağrı yok (NIST SP 800-63B-4). | eklenti | Test |
| F6b.6 | **Doğrulanacaklar.** register/forgot-password/resend zamanlama farkı, 2FA ve antiforgery cookie bayrakları, `/connect/revoke` üzerinde strict rate limit. Ölçülür; açık çıkarsa aynı fazda kapanır. | eklenti | Ölçüm raporu; açık varsa test |
| F6b.7 | **Eksik negatif protokol testleri.** Kapsam: `redirect_uri` varyantları, kod yeniden kullanımı, başka client'la kod kullanımı, PKCE düşürme, `response_type=token` / `grant_type=password`, `/connect/revoke`, `iss` parametresi, JWKS'te özel parça olmaması, `/account/*` clickjacking başlıkları. | tests | Yeşil |

## Faz 6c: Test altyapısı (K2)

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F6c.1 | **MTP ve xUnit 4.** `global.json` `test.runner`. `--filter "FullyQualifiedName~"` örnekleri xUnit bayraklarına güncellenir. | test projesi, `global.json`, CLAUDE.md, tests/CLAUDE.md | Bütün testler yeşil |
| F6c.2 | **SQL Server katmanı (D-043).** Testcontainers + Respawn. Gerçek migration'lar `MigrateAsync` ile uygulanır (EXEC'li `*_OutboxConsumersByMessage` dahil). Kapsam: `GET /payments`, şema izolasyonu, `EF.Functions.Like` collation, `decimal(18,2)`. | tests | Yeşil; SQLite katmanı hızlı varsayılan olarak kalır |
| F6c.3 | **Outbox karşılaştırması.** `OutboxDispatcher.cs:178`'deki SQLite yüzünden bellekte yapılan karşılaştırma gözden geçirilir: test aracı üretim kodunu şekillendirmemeli. | `NovaCore.EntityFrameworkCore/Messaging` | Karar ve test |
| F6c.4 | **Coverage.** coverlet.MTP + ReportGenerator. main'e göre düşüşü engelleyen ratchet. | CI | CI kapısı çalışır |
| F6c.5 | **API sözleşmesi.** OpenAPI belgesi build'de üretilip commit'lenir. `oasdiff breaking` (`review: false`). OpenAPI 3.1 kapsamı fazda ölçülür. | Api, CI | Kırıcı değişiklik CI'da yakalanır |
| F6c.6 | **Property testleri (CsCheck).** `Result`, sayfalama, Payment/Product durum kuralları, outbox handler idempotency. | tests | Yeşil |
| F6c.7 | **Mutation testi.** Stryker.NET haftalık; Abstractions + modül domain kodu; ilk aşamada `break-at: 0` (yalnızca rapor). | CI | Rapor üretilir |

## Faz 6d: Analizör ve platform sıkılaştırma (K4)

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F6d.1 | **Kategori bazlı `AnalysisMode<Kategori>=All`.** Sıra: Security (CA3001–CA3012, CA2100, CA5394, CA5404 vb.), sonra Reliability, Performance, Usage. Her kategori ayrı commit; önce çıkan hata sayısı ölçülür. | `Directory.Build.props`, kaynak | Build 0 uyarı; bastırma yalnızca Y-44 istisnasıyla |
| F6d.2 | **IDE kuralları ve metrikler.** IDE0051/52/59/60 ve IDE0005 warning'e çekilir (IDE0005 için bastırmasız kapsam seçimi). CA1502/CA1505/CA1506 + `CodeMetricsConfig.txt`. CI'da `dotnet format --verify-no-changes`. | `.editorconfig`, CI | Build ve format kontrolü yeşil |
| F6d.3 | **Ek analizörler.** BannedApiAnalyzers (saat, `Console`, `Environment.Exit` vb.). Meziantou.Analyzer kademeli. Roslynator CLI `find-symbol --unused` periyodik. | props, CI | Yeşil |
| F6d.4 | **SaaS analizi.** SonarQube Cloud (scanner CI'da, NuGet paketi yok: Y-06), Qodana Community deneme koşusu (.NET 10 desteği ölçülür), CodeRabbit PR incelemesi. | CI, repo ayarları | İlk koşular raporlanır |
| F6d.5 | **Package validation baseline.** `PackageValidationBaselineVersion` ilk kararlı sürümden sonra (D-039 (2)). | `Directory.Build.targets` | Koşul gerçekleşmediyse atlanır |
| F6d.6 | **Claude Code araçları.** csharp-lsp; Ponytail'dan 2–3 kural CLAUDE.md'ye (D-048 (4)). | CLAUDE.md | — |

## Faz 6e: Dinamik ve ajan tabanlı denetim (K3)

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F6e.1 | **DAST test client'ı.** İzinleri sınırlı bir test OIDC client'ı yalnızca DAST ortamı yapılandırmasında bulunur. | config, tests | Test: üretim yapılandırmasında client yok |
| F6e.2 | **DAST job'u.** Zamanlanmış + `workflow_dispatch`. Atılabilir instance + SQL Server container. Schemathesis (birincil), ZAP API scan (ikincil). `/connect/*` ve `/account/*` spec dışında olduğu için ayrı hedeflenir. | CI | İlk koşu raporlanır |
| F6e.3 | **OpenID conformance suite.** Yerel Docker, sürüm öncesi. | — | Rapor |
| F6e.4 | **Ajan koşuları.** Cloudflare `security-audit-skill` + Claude Security plugin: önce auth çekirdeği, sonra bütün repo, her kapsam en az iki kez. Girdi: Faz 6b listesi ve araştırmanın OIDC kontrol listesi. | — | Bulgu listesi |
| F6e.5 | **Triage kuralı.** Ajan bulgusu ancak başarısız bir xUnit testine dönüşebiliyorsa düzeltilir; test regresyon testi olarak kalır. Dönüşmeyen bulgu `needs_validation` olarak PROGRESS'e yazılır. | tests, PROGRESS | Her bulgu ya testli düzeltme ya `needs_validation` |

---

## Faz 7: İlk tüketici hazırlığı (7a, 7b)

Kaynak: [proposals/crm-cok-kiracililik.md](proposals/crm-cok-kiracililik.md) (kullanıcı kararları → D-068…D-071) ve RESEARCH R-122…R-124. CRM-Nominal, NovaCore üzerinde yeniden kurulacak (D-070). Önce NovaCore'un iki eksiği tamamlanır, CRM'in geçişi ondan sonra ayrıca planlanır. Sıra `6d → 7a → 7b → 6e` (D-071 (3)).

## Faz 7a: Veritabanı özgürlüğü (D-068)

**Amaç:** NovaCore hiçbir projeyi bir veritabanına bağlamaz. Paketler bugün de sağlayıcıdan bağımsız. Bu faz kilidi şablondan ve testlerden kaldırır, Y-47'yi lisans kapısına uygular ve bağımsızlığı ikinci bir sağlayıcıda kanıtlar.

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F7a.1 | **Lisans kuralı (Y-47).** Lisans kapısı, projede adıyla kaydedilmiş sağlayıcı istisnasını tanır (D-052 kalıbı). Repo'nun istisnası, PostgreSQL test katmanı için Npgsql'dir. Şablonun ürettiği projeye de bir lisans kapısı girer (bugün yok); seçilen sağlayıcı orada adlı istisnadır. Y-06'ya canlı atıflar Y-47'ye taşınır (`ci.yml`, `eng/check-licenses.sh`, README; `.claude/hooks/` mesajı korumalı, kullanıcı onayıyla). | `eng/check-licenses.sh`, `ci.yml`, şablonun CI'ı, README | Adlı istisnası olmayan `PostgreSQL` lisanslı bir paket kapıda kırmızı, istisnalı olan yeşil (yerelde denenir); üretilen projenin kapısı yeşil |
| F7a.2 | **Sağlayıcı tek yerden.** Modüller sağlayıcı seçmez. Host seçer, `AddModuleDbContext` seçimi her modül context'ine uygular; modül yalnız kendi ayarını (şema, migration geçmişi tablosu) ekler. Kesin API fazda R-kaydıyla seçilir; 0.x'te public API değişikliğidir, CHANGELOG'a girer. Repo'nun modülleri, Api host'u ve şablonun `Todos` modülü geçirilir. | `NovaCore.EntityFrameworkCore/Persistence`, `src/Modules/*`, `src/Api`, şablon | Hiçbir modül sağlayıcı seçmiyor (mimari test); davranış ve migration'lar aynı, `has-pending-model-changes` temiz |
| F7a.3 | **İkinci sağlayıcı: PostgreSQL test katmanı.** `Testcontainers.PostgreSql` (MIT) ve Npgsql EF sağlayıcısı (F7a.1'in adlı istisnası; yeni paketler hook onayıyla). Outbox (yazma, dağıtım, saklama silmesi), migration servisi ve kimlik eklentisi PostgreSQL'de koşar. Sağlayıcı başına ayrı migration seti (MS). SQL Server katmanının kalıbı (D-056 (3)–(5)): Docker yoksa yerelde atlanır, CI'da `--fail-skips on`. | tests, `docker-compose.yml` | PostgreSQL katmanı CI'da yeşil; aynı senaryolar iki sağlayıcıda |
| F7a.4 | **Şablon `--db SqlServer\|PostgreSql`.** `--auth` gibi bir şablon parametresi: seçilen sağlayıcının paketi, bağlantı dizesi, compose servisi, migration'ları ve CI'ı. SqlClient'ın Pkcs pini (D-036 (8)) yalnız SQL Server seçiminde. | şablon, `eng/template-smoke.sh` | Smoke testi dört bileşimde (iki `--auth` × iki `--db`) build, test ve migration kontrolünden geçer |
| F7a.5 | **Belgeler.** Desteklenen sağlayıcılar ve "modül başına şema" varsayımının sınırı (MySQL/MariaDB'de şema bir veritabanıdır, SQLite şemayı yok sayar; ölçülür ya da belgelenir). Kök CLAUDE.md'deki "SQL Server, not Postgres" notu, README, CHANGELOG, memory. | dokümanlar | — |

**Bu fazda yapılmayacaklar:** çok kiracılılık (7b); kurum başına veritabanı; MySQL, Oracle ve diğer sağlayıcıların test edilmesi (yalnız belgelenir); RLS.

## Faz 7b: Çok kiracılılık (D-069, D-070)

**Amaç:** Birçok kuruma satılan ürünlerin ortak altyapısını isteğe bağlı bir paket olarak sunmak. Tasarım Microsoft'un çok kiracılı rehberinden çıkar (R-124). İlk tüketici CRM-Nominal, ama CRM'e özgü parçalar (şube kapsamı, roller, KVKK kuralları) girmez.

**Kural:** Her güvenlik kuralı önce kırmızı testle başlar (6b gibi). İzolasyon testleri iki sağlayıcıda koşar (7a).

| ID | Adım | Dosyalar / Kapsam | Kabul kriteri |
|---|---|---|---|
| F7b.1 | **Tasarım kararları.** (1) Finbuckle tabanlı bir eklenti mi (Apache-2.0; yeni paket hook onayıyla), Microsoft'un yapı taşları üzerine kendi uygulamamız mı: maliyet hesabıyla (D-016). (2) Paket sınırı: hangi parça isteğe bağlı pakete, hangisi çekirdeğe girer (D-003: çekirdek yalnız Microsoft). (3) Kurum çözüm yöntemleri (yol, alan adı, başlık, claim) ve şablonun varsayılanı. | — | R ve D kaydı |
| F7b.2 | **Kurum bağlamı ve çözümü.** İstek başına kurum bağlamı. Kurum yalnız çağıranın o kurumda aktif üyeliği varsa ve kurum aktifse çözülür (fail-closed). Askıdaki kurum çözülmez; middleware gerekmez (D-070 (5)). | yeni paket, `NovaCore.AspNetCore` | Test: üyesi olunmayan kuruma istek erişemez; askıdaki kurum hiçbir veri görmez; geçerli token'la yola başka bir kurum yazmak erişim vermez |
| F7b.3 | **Veri izolasyonu.** `TenantId` taşıyan her varlığa adlı bir okuma filtresi ve yazma koruması (boşsa doldurur, başka kurumunsa reddeder), `AddModuleDbContext`'te otomatik (outbox interceptor'ı gibi). Açık ve dar bir sistem işi bağlamı. Filtresiz bir kurum varlığını yakalayan konvansiyon testi. | `NovaCore.EntityFrameworkCore`, yeni paket | Test (iki sağlayıcıda): başka kurumun verisi okunmaz, ona yazma reddedilir; başka bir adlı filtreyi kapatmak (`IgnoreQueryFilters([...])`) kurum filtresini yerinde bırakır; filtresiz bir kurum varlığı konvansiyon testini kırmızı yapar |
| F7b.4 | **Üyelik ve kuruma göre izin.** Kullanıcı ↔ kurum ↔ rol üyeliği. İzinler birden çok kaynaktan çözülür: kimlik eklentisinin platform izinleri ve üyelik (bugün DI'daki son `IPermissionResolver` kaydı kazanıyor). Önbellek anahtarı kuruma göre bölümlenir; `RemovePermissionsAsync(sub)` bütün bölümleri düşürür. "Sonraya bırakılanlar"daki izin önbelleği bölümlemesi maddesi buradadır. | `NovaCore.AspNetCore/Authorization`, yeni paket, kimlik eklentisi | Test: aynı `sub` iki kurumda farklı izin alır; önbellek kurumlar arasında sızmaz; Administrator kuralı (2FA, D-053 (5)) üyelik kaynağıyla birlikte de çalışır |
| F7b.5 | **Arka plan bağlamı.** Outbox mesajı kurum bağlamını ve trace bağlamını taşır, dağıtıcı onu handler'ın scope'unda geri yükler. Kurumsuz yazılan mesaj kurumsuz kalır. "Sonraya bırakılanlar"daki trace bağlamı maddesi buradadır. | `NovaCore.EntityFrameworkCore/Messaging`, migration'lar | Test: handler, mesajı yazan kurumun verisini görür, başka kurumunkini görmez; handler'ın span'i isteğin trace'ine bağlı |
| F7b.6 | **Kurum yaşam döngüsü.** Aktif, askıda, kapalı (MS: devre dışı bırakma geçicidir). Kapanan kurumun verisinin silinmesi için bir yol (saklama süresi, yeniden katılım); kapsamı fazda ölçülür. | yeni paket | Test: askıya alınan kurum, fazda belirlenen süre içinde bütün erişimini kaybeder; yeniden etkinleşince erişim döner |
| F7b.7 | **Çekirdek düzeltmeleri (R-122).** Bir projenin kendi `IClaimsTransformation`'ı NovaCore'un izin çözümlemesinin yerini aldıysa host açılmaz (`PermissionResolverStartupCheck` kalıbı). `AuditableEntityInterceptor` `CreatedBy`/`ModifiedBy`'a `sub` yazar: değişmez ve kişisel veri değil (D-054 (11)); 0.x davranış değişikliği, CHANGELOG'a girer. | `NovaCore.AspNetCore`, `NovaCore.EntityFrameworkCore` | Test: ikinci bir `IClaimsTransformation` kaydı açılışı durdurur; damga `sub`'dır |
| F7b.8 | **Şablon.** İsteğe bağlı çok kiracılılık seçeneği (şablon parametresi); örnek modülde kurum kapsamlı bir varlık. | şablon, `eng/template-smoke.sh` | Smoke testi seçenek açıkken ve kapalıyken yeşil |
| F7b.9 | **Belgeler.** README'de çok kiracılılık rehberi (kurum çözümü, izolasyon, sistem işi, arka plan, sağlayıcı başına RLS örneği). README'deki "Ezilemeyenler" paragrafı güncellenir (middleware yeri D-070 (5), izin önbelleği F7b.4). Y-14'e canlı atıflar Y-48'e taşınır. CLAUDE.md'ler, CHANGELOG, memory. | dokümanlar | — |

**Bu fazda yapılmayacaklar:** şube kapsamı ve CRM'in rolleri (CRM'in işi); kurum başına veritabanı; RLS'in paketlere girmesi; token'da kurum claim'i (D-070 (3)); kurum başına istek sınırı; CRM'in geçişi (ayrı plan, D-070 (4)).

**Faz doğrulaması (7a ve 7b):** `dotnet build NovaCore.slnx -c Release`, `dotnet test NovaCore.slnx` (SQLite, SQL Server ve PostgreSQL katmanları), `bash eng/template-smoke.sh`, her context için `has-pending-model-changes`, lisans kapısı, coverage ve OpenAPI sözleşmesi kapıları, faz branch'inde CI.
