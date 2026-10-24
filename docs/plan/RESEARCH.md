# Araştırma kayıtları

Planda olmayan bir durum çıktığında ya da bir karar kaynak gerektirdiğinde buraya bir R-kaydı eklenir. **Önce birincil kaynağa bakılır:** Microsoft Learn MCP'deki `microsoft_docs_search`, `microsoft_docs_fetch` ve `microsoft_code_sample_search`. Microsoft dışı bir paket söz konusuysa paketin resmi reposu veya dokümanı kullanılır. Kaynak bulunamadıysa bu açıkça yazılır; tahmin, bulgu gibi yazılmaz.

Biçim:
```
### R-NNN: <soru> (<tarih>, faz <id>)
- Bulgu: <1–3 cümle, doğrudan alıntı varsa tırnak içinde>
- Kaynak: <URL>
- Sonuç: <uygulandı / kullanıcıya soruldu (Q/D ID) / bilgi amaçlı>
```

---

### R-001: Minimal API için yerleşik validation (2026-09-24, analiz)
- Bulgu: .NET 10'da `builder.Services.AddValidation()` DataAnnotations ve `IValidatableObject` ile Minimal API validation sağlar. Top-level API stabil; alttaki resolver API'leri experimental. Source generator yalnızca `AddValidation`'ın çağrıldığı assembly'yi tarar, bu yüzden her modül assembly'si kendi `AddValidation` çağrısını yapmalı. Hatalar `IProblemDetailsService` ile özelleştirilebilir.
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/validation?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#minimal-apis
- Sonuç: D-003 → F2c.1

### R-002: Özel policy provider yerine IAuthorizationRequirementData (2026-09-24, analiz)
- Bulgu: .NET 8'den itibaren `IAuthorizationRequirementData` ile attribute gereksinim taşıyabiliyor, özel `IAuthorizationPolicyProvider` gerekmiyor. ASP.NET Core 8–10'da yalnızca Minimal API ve routed endpoint'lerde geçerli; MVC, SignalR ve Blazor desteği .NET 11 ile geliyor.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authorization/custom-authorization-policies-with-iauthorizationrequirementdata?view=aspnetcore-10.0
- Sonuç: F2c.2

### R-003: Migration uygulama stratejisi (2026-09-24, analiz)
- Bulgu: Otomatik dağıtım için migration bundle, DBA incelemesi için SQL script önerilir. Açılışta migration EF Core 9+ kilidiyle "kabul edilebilir" ama ödünleri var: uygulamanın yüksek yetki gerektirmesi, geri alma zorluğu, SQL'in incelenememesi. CI'da `dotnet ef migrations has-pending-model-changes` önerilir.
- Kaynak: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying
- Sonuç: Açılışta migration opt-in olarak kalır. F0.8'de CI kontrolü eklenir.

### R-004: Rate limiting bölümleme (2026-09-24, analiz)
- Bulgu: Bölümleme sağlanmazsa tek bir sayaç kullanılır. Bölümlemenin faydası: "One user can't consume the entire rate limit for everyone." IP bazlı örnek: `RateLimitPartition.GetFixedWindowLimiter(partitionKey: RemoteIpAddress...)`.
- Kaynak: https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0#rate-limiting-partitions
- Sonuç: F0.3

### R-005: Options pattern ve başlangıçta doğrulama (2026-09-24, analiz)
- Bulgu: `AddOptions<T>().Bind/BindConfiguration().ValidateDataAnnotations().ValidateOnStart()`. `[OptionsValidator]` source generator reflection kullanmaz ve AOT uyumludur. Kütüphane yazarları için `configSectionPath` parametreli extension metot önerilir.
- Kaynak: https://learn.microsoft.com/dotnet/core/extensions/options#options-validation · https://learn.microsoft.com/dotnet/core/extensions/options-validation-generator · https://learn.microsoft.com/dotnet/core/extensions/options-library-authors
- Sonuç: F2b.1

### R-006: Health endpoint güvenliği (2026-09-24, analiz)
- Bulgu: `RequireAuthorization()` ve `RequireHost()` ile kısıtlanabilir. Host header sahtelenebildiği için port kontrolü veya yetkilendirme önerilir. MS örnek ServiceDefaults kodu health uç noktalarını yalnızca Development'ta map'liyor.
- Kaynak: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0#use-health-checks-routing
- Sonuç: F2b.5 (yöntem kullanıcıya sorulacak)

### R-007: Passkey desteği (2026-09-24, analiz; bilgi amaçlı)
- Bulgu: .NET 10'da ASP.NET Core Identity passkey (WebAuthn) destekliyor. Kapsamı Identity kimlik doğrulama senaryolarıyla sınırlı, SignInManager ve cookie akışına bağlı. HTTPS zorunlu.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0
- Sonuç: Kapsamda değil. Faz 3b'de OpenIddict eklentisinde değerlendirilebilir (kullanıcı kararı).

### R-008: HybridCache (2026-09-24, analiz)
- Bulgu: `HybridCache` soyut bir sınıftır ve varsayılan bir implementasyonu vardır. L1+L2, stampede koruması ve tag ile invalidation sunar. Güncel dokümanlarda experimental uyarısından söz edilmiyor.
- Kaynak: https://learn.microsoft.com/aspnet/core/performance/caching/hybrid?view=aspnetcore-10.0 · https://learn.microsoft.com/dotnet/core/extensions/caching#hybrid-caching
- Sonuç: F2c.3 (`ICacheService` kaldırılır; EXTEXP0018'in hâlâ gerekip gerekmediği derlemeyle doğrulanır)

### R-009: .NET kütüphane rehberi: dağıtım, versiyon, bağımlılık (2026-09-24, analiz)
- Bulgu: "DO distribute your library using a NuGet package." "CONSIDER using SemVer 2.0.0." "DO review your .NET library for unnecessary dependencies." Tam sürüm veya üst sınır gerektiren paket referanslarından kaçınılmalı.
- Kaynak: https://learn.microsoft.com/dotnet/standard/library-guidance/ · https://learn.microsoft.com/dotnet/standard/library-guidance/versioning · https://learn.microsoft.com/dotnet/standard/library-guidance/dependencies
- Sonuç: D-001, D-003

### R-010: JWT oluşturma önerileri (2026-09-24, analiz)
- Bulgu: "Asymmetric keys should **always** be used when creating access tokens." "You should **NOT** create an access token from a username/password request." OIDC/OAuth standartları kullanılmalı. Tarayıcıda token saklamak yerine backend ve HttpOnly cookie önerilir.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0#recommended-approaches-to-create-a-jwt · https://learn.microsoft.com/aspnet/core/security/how-to-choose-identity-solution?view=aspnetcore-10.0
- Sonuç: D-002 → Faz 3a/3b

### R-011: Package validation (2026-09-24, analiz)
- Bulgu: `EnablePackageValidation=true` + `PackageValidationBaselineVersion` sürümler arası ikili kırılmaları pack sırasında yakalar. Kasıtlı kırılmalar `CompatibilitySuppressions.xml` ile belgelenir.
- Kaynak: https://learn.microsoft.com/dotnet/fundamentals/apicompat/package-validation/overview · https://learn.microsoft.com/dotnet/fundamentals/apicompat/package-validation/baseline-version-validator
- Sonuç: F1.1, F5.4

### R-012: TimeProvider (2026-09-24, analiz)
- Bulgu: .NET 8+ `TimeProvider` zaman için bir soyutlamadır. Testte `FakeTimeProvider` (Microsoft.Extensions.TimeProvider.Testing) kullanılır. Öneri: "Always inject TimeProvider… rather than using DateTime or DateTimeOffset directly."
- Kaynak: https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview · https://learn.microsoft.com/dotnet/core/extensions/timeprovider-testing
- Sonuç: F2b.3

### R-013: Source-generated logging (2026-09-24, analiz)
- Bulgu: Kütüphane yazarlarına `[LoggerMessage]` önerilir (boxing ve allocation yok, şablon derleme zamanında ayrıştırılır). CA1848 bunu zorlar.
- Kaynak: https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1848
- Sonuç: F2b.6

### R-014: NuGet audit ve transitive açıklar (2026-09-24, analiz)
- Bulgu: .NET 10 hedefleyen projelerde `NuGetAuditMode` varsayılanı `all`, yani transitive paketler de denetlenir. Uyarılar restore sırasında çıkar. Transitive açıkta önerilen sıra: önce top-level paketi güncelle, sonra yakındaki ara paketi, en son doğrudan referans veya CPM transitive pinning. **Dikkat:** transitive pinning, pack edilen pakette pinlenen paketleri açık bağımlılığa dönüştürür. Bu yüzden kütüphane projelerinde dikkatle kullanılmalı.
- Kaynak: https://learn.microsoft.com/nuget/concepts/auditing-packages · https://learn.microsoft.com/dotnet/core/compatibility/sdk/10.0/nugetaudit-transitive-packages · https://learn.microsoft.com/nuget/consume-packages/central-package-management#transitive-pinning
- Sonuç: F0.2

---

## "Yapılmayacaklar" araştırması (2026-09-25; öneri: [proposals/yapilmayacaklar-onerisi.md](proposals/yapilmayacaklar-onerisi.md))

### R-015: Framework Design Guidelines: soyutlama, static sınıf, exception (2026-09-25)
- Bulgu: *"DO NOT provide abstractions unless they are tested by developing several concrete implementations and APIs consuming the abstractions."* Çok sayıda soyutlama kullanılabilirliği düşürür. Static sınıflar için *"DO NOT treat static classes as a miscellaneous bucket."* Exception'lar için *"DO NOT use exceptions for the normal flow of control, if possible."* Base class'lar yalnızca kullanıcıya belirgin değer katıyorsa eklenmeli.
- Kaynak: https://learn.microsoft.com/dotnet/standard/design-guidelines/abstractions-abstract-types-and-interfaces · https://learn.microsoft.com/dotnet/standard/design-guidelines/static-class · https://learn.microsoft.com/dotnet/standard/design-guidelines/exception-throwing · https://learn.microsoft.com/dotnet/standard/design-guidelines/base-classes-for-implementing-abstractions
- Sonuç: Ö-01, Ö-16, Ö-17

### R-016: EF Core test stratejisi (2026-09-25)
- Bulgu: Önerilen yol gerçek veritabanı sistemine karşı test. InMemory provider *"highly discouraged"*. Sorgu için `DbSet` mock'lamaktan kaçınılmalı. Repository katmanı güvenilir bir test double sağlar ama *"significant cost in terms of implementation and maintenance"* ve testlenebilir sorguların `IEnumerable` döndüren metotlara sarılmasını gerektirir.
- Kaynak: https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy · https://learn.microsoft.com/ef/core/testing/
- Sonuç: Ö-03, Ö-20

### R-017: CQRS, Event Sourcing, DDD: ne zaman kullanılmamalı (2026-09-25)
- Bulgu: CQRS *"might not be suitable when: the domain or the business rules are simple; a simple CRUD-style user interface… sufficient."* Event Sourcing düz CRUD, MVP ve kısa ömürlü sistemler için uygun değil. eShop: *"forcing the same pattern everywhere leads to failure."* DDD küçük veya CRUD ağırlıklı uygulamalarda yatırımı karşılamaz; basit CRUD'da anemik model anti-pattern sayılmaz.
- Kaynak: https://learn.microsoft.com/azure/architecture/patterns/cqrs · https://learn.microsoft.com/azure/architecture/patterns/event-sourcing · https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/eshoponcontainers-cqrs-ddd-microservice · https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps#domain-driven-design--should-you-apply-it
- Sonuç: Ö-04, Ö-05

### R-018: DI yönergeleri ve anti-pattern'ler (2026-09-25)
- Bulgu: Service locator'dan kaçınılmalı; buna çalışma zamanında bağımlılık çözen factory enjekte etmek de dahil. Static servis erişimi kullanılmamalı. Servis kaydı sırasında `BuildServiceProvider` çağrılmamalı. Captive dependency (singleton → scoped) bir anti-pattern'dir ve scope doğrulaması açılmalıdır. Stateful static sınıflardan kaçınılmalı.
- Kaynak: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/guidelines
- Sonuç: Ö-12, Ö-13, Ö-14

### R-019: ASP.NET Core Best Practices (2026-09-25)
- Bulgu: `Task.Wait`/`.Result` ile bloklanmamalı. `Task.Run` çağırıp hemen await edilmemeli. *"async void is ALWAYS a bad practice in ASP.NET Core apps."* HttpContext arka plan thread'ine taşınmamalı. Exception'lar normal program akışını kontrol etmek için kullanılmamalı.
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0
- Sonuç: Ö-15, Ö-16

### R-020: Over-posting / mass assignment (2026-09-25)
- Bulgu: Entity'nin tamamını açmak yerine DTO kullanılmalı. Amaçlar: over-posting'i önlemek, gizli alanları saklamak, payload'u küçültmek.
- Kaynak: https://learn.microsoft.com/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0#prevent-over-posting · https://learn.microsoft.com/azure/security/develop/threat-modeling-tool-input-validation
- Sonuç: Ö-19

### R-021: EF Core verimli sorgulama (2026-09-25)
- Bulgu: Lazy loading N+1'e çok kolay yol açtığı için *"it is recommended to avoid it."* Sonuç kümesi sınırlandırılmalı (test verisi küçük olduğu için sorun geç fark edilir). Yalnızca gereken alanlar projekte edilmeli.
- Kaynak: https://learn.microsoft.com/ef/core/performance/efficient-querying · https://learn.microsoft.com/ef/core/querying/pagination
- Sonuç: Ö-21, Ö-22

### R-022: LLM'lerde mode collapse ve tipiklik yanlılığı (2026-09-25)
- Bulgu: Zhang ve ark., *Verbalized Sampling: How to Mitigate Mode Collapse and Unlock LLM Diversity* (arXiv 2510.01171, Ekim 2025). Mode collapse'ın temel nedenlerinden biri olarak tercih verisindeki tipiklik yanlılığını gösteriyor. Önerilen çare, modelden olasılıklarıyla birlikte birden çok cevap istemek. Yaratıcı görevlerde çeşitlilik 1,6–2,1 kat artıyor.
- Kaynak: https://arxiv.org/abs/2510.01171
- Sonuç: Ö-28, Ö-29 (bilimsel bir kaynak; Microsoft dokümanı değil)

### R-023: YAGNI (2026-09-25)
- Bulgu: Öngörüyle eklenen bir yeteneğin dört maliyeti var: inşa, gecikme, taşıma, onarım. YAGNI, yazılımı değiştirmeyi kolaylaştıran çabalara (refactoring, test) uygulanmaz.
- Kaynak: https://martinfowler.com/bliki/Yagni.html
- Sonuç: Bölüm A'nın genel ilkesi; Ö-02, Ö-07

### R-024: Önce monolit (2026-09-25)
- Bulgu: Fowler: *"you shouldn't start a new project with microservices, even if you're sure your application will be big enough."* MS: süreçleri ayırmak ek yük getirir (asenkron iletişim, event bus, retry, eventual consistency). Bağımsız özellik dilimleri çıkarılamıyorsa ayırmak yalnızca karmaşıklık ekler.
- Kaynak: https://martinfowler.com/bliki/MonolithFirst.html · https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures
- Sonuç: Ö-06, Y-02

### R-025: HttpClient kullanımı (2026-09-25)
- Bulgu: İstek başına HttpClient oluşturulursa socket tükenir. Factory ile oluşturulan client'lar kısa ömürlüdür; typed client singleton'a enjekte edilmemeli.
- Kaynak: https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines · https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory-troubleshooting
- Sonuç: Ö-18

---

## Faz 0 araştırması (2026-09-25)

### R-026: Test host'ta erken okunan config'i ezmek (2026-09-25, faz 0)
- Bulgu: `WebApplicationFactory.CreateHost(IHostBuilder)` override'ında `ConfigureHostConfiguration` ile eklenen değerler *"will be enumerated before the entry point is called, and the configurations will be passed to the entry point via `args`"*. Command-line config en yüksek önceliğe sahip olduğu için env var'ı da ezer. `ConfigureAppConfiguration` ile eklenenler ise yalnızca `Build` sonrasında görünür. Bu, `AddCoreWeb`'in eager okumalarının (tests/CLAUDE.md) nedenini de açıklıyor. `Program.cs` `CreateBuilder(args)` çağırdığı için yöntem burada çalışır.
- Kaynak: https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0#customize-the-webapplicationfactory-with-test-configurations · https://learn.microsoft.com/dotnet/core/extensions/configuration#alternative-hosting-approach
- Sonuç: F0.3 (düşük limitli test host'u, env var yarışı olmadan), F0.5 (Production ortamlı test host'u).

### R-027: `dotnet list package --vulnerable` çıkış kodu (2026-09-25, faz 0)
- Bulgu: Yerelde gözlendi: komut iki yüksek açık listelediği hâlde exit 0 döndü. `--format json` çıktısında her paket için `vulnerabilities` dizisi var (`projects[].frameworks[].topLevelPackages[]/transitivePackages[]`). CI'da açık varken kırmızı olması için çıktı ayrıştırılmalı. Microsoft dokümanında çıkış koduna dair bir ifade bulamadım. Bu bir gözlemdir.
- Kaynak: yerel çalıştırma, SDK 10.0.300 (2026-09-25)
- Sonuç: F0.8 (`--format json` + `jq -e`).

### R-028: Faz 0 başındaki açıklar (2026-09-25, faz 0)
- Bulgu: GHSA-v5pm-xwqc-g5wc: `Microsoft.OpenApi` `>= 2.0.0-preview.11, <= 2.7.4` sürümleri etkileniyor, 2.7.5'te yamalı. Kaynağı `Microsoft.AspNetCore.OpenApi` 10.0.8. GHSA-2m69-gcr7-jv3q: `SQLitePCLRaw.lib.e_sqlite3` `<= 2.1.11` etkileniyor. Kaynağı `Microsoft.EntityFrameworkCore.Sqlite` 10.0.4. nuspec'lere göre `Microsoft.AspNetCore.OpenApi` 10.0.12 `Microsoft.OpenApi [2.12.0, 3.0.0)`, `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 `SQLitePCLRaw.bundle_e_sqlite3 2.1.12` istiyor. Top-level güncelleme ikisini de kapatmalı, transitive pinning gerekmemeli (R-014).
- Kaynak: https://api.github.com/advisories/GHSA-v5pm-xwqc-g5wc · https://api.github.com/advisories/GHSA-2m69-gcr7-jv3q · https://api.nuget.org/v3-flatcontainer/ (nuspec)
- Sonuç: F0.2

### R-029: ValidateScopes / ValidateOnBuild varsayılanları (2026-09-25, faz 0)
- Bulgu: *"ValidateScopes and ValidateOnBuild are enabled by default in the Development environment but disabled in other environments."* .NET 9'dan beri HostBuilder için de geçerli. Varsayılana güvenmek yerine açıkça ayarlamak için `UseDefaultServiceProvider` kullanılır.
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0#validatescopes-and-validateonbuild · https://learn.microsoft.com/aspnet/core/breaking-changes/9/hostbuilder-validation
- Sonuç: F0.7 (Y-26)

### R-030: Ortam adı IConfiguration'da (2026-09-25, faz 0)
- Bulgu: `HostDefaults.EnvironmentKey` (`"environment"`) *"The configuration key used to set EnvironmentName"*. WebApplicationBuilder'ın config'i command-line, `DOTNET_` ve `ASPNETCORE_` host kaynaklarını içerir, bu yüzden `ASPNETCORE_ENVIRONMENT` ve `--environment` bu anahtara düşer. Ayarlanmamışsa değer null olur ve ortam varsayılan olarak Production kabul edilir.
- Kaynak: https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.hostdefaults.environmentkey · https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#default-host-configuration-sources
- Sonuç: F0.5 (Q-08). `IModule.AddModule` ortam almadığı için Identity modülü ortamı buradan okur. Testle doğrulanır.

### R-031: GitHub Actions sürümleri (2026-09-25, faz 0)
- Bulgu: GitHub releases API'ye göre `actions/checkout` en son v7.0.1 (2026-07-20), `actions/setup-dotnet` en son v6.0.0 (2026-07-16).
- Kaynak: https://api.github.com/repos/actions/checkout/releases/latest · https://api.github.com/repos/actions/setup-dotnet/releases/latest
- Sonuç: F0.8

---

## Faz 1 araştırması (2026-09-25)

### R-032: IsPackable'a bağlı ayarlar props'ta mı, targets'ta mı? (2026-09-25, faz 1)
- Bulgu: *"Directory.Build.props is imported early in Microsoft.Common.props, and properties defined later are unavailable to it. So, avoid referring to properties that aren't yet defined (and will evaluate to empty)."* Ayrıca *"Set dependent properties in .targets files, because they pick up customizations from individual projects."* NovaCore'da `IsPackable` her csproj'da tanımlı. Bu yüzden `Condition="'$(IsPackable)' == 'true'"` props'ta hep boş değer görür.
- Kaynak: https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory#directorybuildprops-and-directorybuildtargets · https://learn.microsoft.com/visualstudio/msbuild/customize-your-build#choose-between-adding-properties-to-a-props-or-targets-file
- Sonuç: Q-14 → D-010. Koşullu grup `Directory.Build.targets`'ta. Doc üretimi için yetmediği ortaya çıktı (R-040 → D-014).

### R-033: NuGet audit ve TreatWarningsAsErrors (2026-09-25, faz 1)
- Bulgu: NuGet `TreatWarningsAsErrors`, `WarningsAsErrors`, `NoWarn` ve `WarningsNotAsErrors` özelliklerine uyar (*"TreatWarningsAsErrors: Treats all NuGet warnings as errors"*). Audit dokümanı: TWAE kullanılıyorsa *"you can use `<WarningsNotAsErrors>$(WarningsNotAsErrors);NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>` to prevent vulnerabilities discovered in the future from breaking your build."*
- Kaynak: https://learn.microsoft.com/nuget/reference/errors-and-warnings · https://learn.microsoft.com/nuget/concepts/auditing-packages#running-a-security-audit-with-restore · https://learn.microsoft.com/nuget/reference/errors-and-warnings/nu1901-nu1904
- Sonuç: Q-16 → D-011. `WarningsNotAsErrors=NU1901;NU1902`. NU1903/NU1904 F0.2'deki gibi hata kalır.

### R-034: CA1000, CA1711, CA1716 ne zaman bastırılabilir? (2026-09-25, faz 1)
- Bulgu:
  - CA1000: *"Do not suppress a warning from this rule."* Düzeltme: static üyeyi kaldırmak ya da instance üyeye çevirmek. Varsayılan olarak yalnızca dışarıdan görünen tiplere bakar.
  - CA1711: *"Do not suppress a warning from this rule unless the suffix has an unambiguous meaning in the application domain."*
  - CA1716: *"You can suppress a warning from this rule if you're sure that the identifier won't confuse users of the API, and that the library is usable in all available languages in .NET."* `Error`, VB'deki `Error` anahtar kelimesi yüzünden işaretleniyor. VB'de `[Error]` ile kullanılabilir.
  - Üçü de "Enabled by default in .NET 10: No". `latest-recommended` ile açılıyorlar.
- Kaynak: https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1000 · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1711 · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1716
- Sonuç: Q-13 → D-012. CA1000 düzeltildi. CA1711 ("domain event handler" alan dilinde net) ve CA1716 (NovaCore C#-only) gerekçeli bastırıldı.

### R-035: Test adlarında alt çizgi (2026-09-25, faz 1)
- Bulgu: Microsoft'un "Unit testing best practices" dokümanı, test adının üç parçadan oluşmasını öneriyor (metot, senaryo, beklenen davranış). Örnekler alt çizgiyle yazılmış: `Add_SingleNumber_ReturnsSameNumber`.
- Kaynak: https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices#follow-test-naming-standards
- Sonuç: Q-12 → D-011. CA1707 yalnızca `tests/**.cs` için kapalı.

### R-036: Faz 1 deneme derlemesi uyarı envanteri (2026-09-25, faz 1; gözlem)
- Bulgu: `dotnet build -c Release` + `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, tüm projelerde `GenerateDocumentationFile=true`, `NoWarn`'dan CS1591 çıkarılmış hâlde (ayrı bir `--artifacts-path`'e) çalıştırıldı. Tekilleştirilmiş uyarılar:
  - Packable projelerde CS1591 132 (Abstractions 67, Infrastructure 22, Web 43, Catalog.Contracts 0).
  - CA1848 24 + CA1873 14 (Core.Infrastructure 6, Identity 32).
  - CA1707 42 (yalnızca testler).
  - CA1000 3, CA1305 3, CA1859 2. CA1711, CA1716, CA1725, CA1822, CA1816 1'er.
  - IDE (code style) uyarısı yok: `.editorconfig`'teki kurallar `suggestion` seviyesinde.
- Kaynak: yerel çalıştırma, SDK 10.0.300.
- Sonuç: F1.2 alt adımlara bölündü (F1.2a–f). Q-10…Q-13.

### R-037: Microsoft.CodeAnalysis.PublicApiAnalyzers (2026-09-25, faz 1)
- Bulgu:
  - NuGet flat-container'a göre son stabil sürüm **5.6.0**. Sonraki `5.11.0-1.26425.128` prerelease.
  - Dokümana göre 3.3.x ve sonrasında *"Text files `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` will be added to each project in scope, if they do not already exist."*
  - Nullable takibi için dosyaların başına `#nullable enable` yazılır.
  - Mevcut API'yi toplu belgelemek için code fix proje veya çözüm genelinde uygulanabilir.
  - Mevcut bir projede Shipped/Unshipped ayrımı için açık bir yönerge yok.
- Kaynak: https://api.nuget.org/v3-flatcontainer/microsoft.codeanalysis.publicapianalyzers/index.json · https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md
- Sonuç: F1.4. Hiçbir sürüm yayımlanmadığı için tüm API Unshipped'e girer, Shipped'e taşıma F5.4'te. Dosyaların `AdditionalFiles` olarak otomatik eklenip eklenmediği paketin build dosyasından doğrulanacak.

### R-038: Paket metadata'sı: README ve ContinuousIntegrationBuild (2026-09-25, faz 1)
- Bulgu:
  - `PackageReadmeFile` pakete göre yol verir. Dosyanın ayrıca `<None Include="…" Pack="true" PackagePath="…"/>` ile pakete konması gerekir. Yalnızca Markdown desteklenir.
  - `ContinuousIntegrationBuild` yalnızca CI'da açılmalı, çünkü yerelde normalize yollar debugger'ın kaynak bulmasını engeller. GitHub Actions için örnek: `<PropertyGroup Condition="'$(GITHUB_ACTIONS)' == 'true'">`.
  - Library guidance: Source Link, sembol yayını ve deterministic build *"CONSIDER"* seviyesinde öneriliyor.
- Kaynak: https://learn.microsoft.com/nuget/reference/msbuild-targets#packagereadmefile · https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#continuousintegrationbuild · https://learn.microsoft.com/dotnet/standard/library-guidance/sourcelink
- Sonuç: F1.1 (Q-15 → D-013).

### R-039: GitHub Packages NuGet feed'i (2026-09-25, faz 1)
- Bulgu:
  - Feed adresi `https://nuget.pkg.github.com/NAMESPACE/index.json`. Actions içinde `GITHUB_TOKEN` kullanılabilir.
  - Özel bir repodan paket kurmak için tüketici en az `read:packages` kapsamlı PAT (classic) kullanır.
  - İlk yayında paketin görünürlüğü varsayılan olarak private.
  - `.snupkg`: resmi dokümanda geçmiyor. GitHub topluluk tartışmasına göre (resmi değil) GitHub Packages sembol paketini desteklemiyor ve ikinci yüklemeyi "version conflict" ile reddediyor.
- Kaynak: https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry · https://github.com/orgs/community/discussions/38678 (topluluk kaynağı)
- Sonuç: Q-17 → D-013. release.yml yalnızca `.nupkg` push eder (`--no-symbols`), `.snupkg` workflow artifact'i olarak saklanır.

### R-040: Directory.Build.targets bazı özellikler için çok geç (2026-09-25, faz 1; gözlem + SDK kaynağı)
- Bulgu: F1.1'de `GenerateDocumentationFile=true` targets'ta ayarlandı ve `-getProperty` true gösterdi, ama `DocumentationFile` boş kaldı, CS1591 de hiç çıkmadı.
  - Nedeni: SDK `DocumentationFile`'ı `Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.BeforeCommon.targets:301-313`'te `GenerateDocumentationFile`'a göre hesaplıyor. Bu dosya `-pp` çıktısında Directory.Build.targets'tan (satır ~11729) çok önce import ediliyor (satır ~2830).
  - Aynı sorun `ContinuousIntegrationBuild` için de var: Roslyn `Microsoft.Managed.Core.targets:215` `DeterministicSourcePaths`'i ondan türetiyor ve bu dosya da önce geliyor (satır ~5204). `GITHUB_ACTIONS=true` ile değerlendirmede `DeterministicSourcePaths` boş çıktı.
  - Paket metadata'sı (`PackageLicenseExpression`, `PackageReadmeFile`, `RepositoryUrl`, `IncludeSymbols`…) pack target'ları çalışırken okunuyor, targets'ta sorunsuz.
  - `EnablePackageValidation` target koşulunda okunuyor, sorun yok. Ancak baseline `PackageDownload` item'ı `Microsoft.NET.ApiCompat.targets:18`'de değerlendirme anında okunuyor (import satır ~17801, targets'tan sonra), F5.4'te dikkat.
  - SDK'da `BeforeMicrosoftNETSdkTargets` diye bir kanca var (`Sdk/Sdk.targets:13`); csproj gövdesinden sonra, BeforeCommon'dan önce import ediliyor.
- Kaynak: yerel SDK 10.0.300 dosyaları ve `dotnet msbuild -pp`. R-032'deki MS Learn ifadesi ("imported after importing .targets files from NuGet packages") targets'ın SDK'nın kendi değerlendirme anı kararlarından sonra geldiğini de açıklıyor.
- Sonuç: Q-18 → D-014 (D-010'un yerine). `GenerateDocumentationFile` packable csproj'larda, `ContinuousIntegrationBuild` props'ta. Targets'ta paket metadata'sı ve "packable ama doc yok" koruması kalıyor.

### R-041: XML doc üretimi OpenAPI belgesini değiştiriyor (2026-09-25, faz 1)
- Bulgu:
  - .NET 10: *"At build-time, the OpenAPI package will leverage a source generator to discover XML comments in the current application assembly and any project references and emit source code to insert them into the document via an OpenAPI document transformer."*
  - Kaynaklar: uygulama assembly'si ve *"Any projects referenced via `ProjectReferences` that have the `GenerateDocumentationFile` property set."*
  - Kapatmak için generator Analyzers'tan çıkarılır.
  - Gözlem: main ile F1.1 branch'inin `/openapi/v1.json` çıktıları karşılaştırıldı (geçici, commit'lenmemiş bir dump testiyle). Tek fark, iki `PagedList` şemasına Abstractions'taki `<summary>`'den gelen `"description"` alanı. Endpoint'ler, şema yapısı ve durum kodları aynı.
- Kaynak: https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#openapi · https://learn.microsoft.com/aspnet/core/fundamentals/openapi/openapi-comments?view=aspnetcore-10.0
- Sonuç: Q-19. Kullanıcı kabul etti: yalnızca açıklama metni eklenir, API sözleşmesi değişmez. CHANGELOG'a "Changed" notu yazılacak.

### R-042: PublicApiAnalyzers kurulumu: AdditionalFiles ve RS0026 (2026-09-25, faz 1)
- Bulgu:
  - 5.6.0 paketinin `buildTransitive/*.props|targets` dosyaları `PublicAPI.*.txt`'yi `AdditionalFiles` olarak **eklemiyor**. Yalnızca global analyzer config ve `CodeAnalysisTreatWarningsAsErrors` ayarını yapıyorlar. R-037'deki "files will be added" ifadesi dosyaları oluşturan IDE code fix'ini anlatıyor. Bu yüzden item'lar `Directory.Build.targets`'a eklendi.
  - Unshipped listesi `dotnet format analyzers <proj> --diagnostics RS0016 --severity warn` ile, analyzer'ın kendi code fix'iyle üretildi. Çözüm genelinde tek çağrı yalnızca ilk projeyi doldurdu, diğerleri proje proje çalıştırıldı. Record'ların derleyicinin ürettiği `Equals(object)`/`ToString()` üyeleri `~` (oblivious) ile yazıldı.
  - RS0026 (ApiDesign): aynı isimli iki public overload'un ikisi de opsiyonel parametre taşıyınca hata veriyor. Roslyn kuralı: *"Only one overload may have optional parameters"*, bu overload en çok parametreye sahip olmalı. Gerekçe, sonradan parametre eklemenin kaynak ve ikili uyumluluğu bozması.
- Kaynak: `~/.nuget/packages/microsoft.codeanalysis.publicapianalyzers/5.6.0/buildTransitive/` · https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md
- Sonuç: F1.4. Q-20 → D-015: `ToHttpResult` overload'lara bölündü.

### R-043: GitHub Actions'tan GitHub Packages'a push (2026-09-25, faz 1)
- Bulgu:
  - GitHub'ın .NET rehberindeki örnek: `permissions: packages: write`, `setup-dotnet` içinde `source-url: https://nuget.pkg.github.com/<owner>/index.json` ve `NUGET_AUTH_TOKEN: ${{secrets.GITHUB_TOKEN}}`, sonra `dotnet nuget push`.
  - NuGet registry sayfasında push için `--api-key` + `--source` biçimi de var.
  - `source-url` feed'i setup-dotnet'in yazdığı nuget.config'e ekler, yani restore'da ikinci bir kaynak olur. NovaCore CPM kullanıyor ve package source mapping yok. Bu durumda NuGet'in NU1507 uyarısını vermesi beklenir ve TWAE bunu restore hatasına çevirir (R-033).
  - **Bu bir çıkarımdır, CI'da denenmedi.**
  - Güncel sürümler (GitHub releases API): `actions/upload-artifact` v7.0.1, `actions/checkout` v7.0.1, `actions/setup-dotnet` v6.0.0.
- Kaynak: https://docs.github.com/en/actions/tutorials/build-and-test-code/net · https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry · https://api.github.com/repos/actions/upload-artifact/releases/latest
- Sonuç: F1.6. `release.yml` feed'i yalnızca push adımında `--source` + `--api-key GITHUB_TOKEN` ile veriyor, restore kaynaklarına dokunmuyor. Feed sahibi `${{ github.repository_owner }}` (bu repo için `alimuhammedak`, D-013).

---

## Faz 2a araştırması (2026-09-25)

### R-044: `[LoggerMessage]` kullanımı: primary constructor, EventId, CA1873 (2026-09-25, faz 2a)
- Bulgu:
  - .NET 9'dan beri instance bir logging metodu logger'ı sınıftaki bir `ILogger` primary constructor parametresinden alabiliyor: *"Starting with .NET 9, the logging method can additionally get the logger from an `ILogger` primary constructor parameter in the containing class."* Aynı sınıfta bir `ILogger` alanı da varsa alan kullanılıyor.
  - Metotlar `partial` olmalı ve `void` dönmeli. En fazla 6 şablon parametresi alabiliyorlar (SYSLIB1023).
  - SYSLIB1006: *"Event ID values must be unique within the scope of each assembly."* Bu yüzden EventId'ler açıkça ve assembly içinde benzersiz verilir.
  - CA1873'e göre argümanlar, log seviyesi kapalı olsa bile değerlendirilir. Düzeltme yolları: `IsEnabled` guard'ı, source-generated logging ya da argümanda pahalı işlem yapmamak.
- Kaynak: https://learn.microsoft.com/dotnet/core/extensions/logging/source-generation#basic-usage · https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1006 · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873
- Sonuç: F2b.6. Primary constructor'lı partial sınıflarda instance metotlar kullanılır. Seviye ve mesaj şablonları aynı kalır, EventId'ler açık yazılır.

### R-045: `IHostApplicationBuilder` ve `IHostedService` hangi pakette? (2026-09-25, faz 2a)
- Bulgu: İkisi de `Microsoft.Extensions.Hosting.Abstractions.dll` assembly'sinde ve aynı isimli pakette, `Microsoft.Extensions.Hosting` namespace'inde. ASP.NET Core shared framework'ü gerekmiyor. Yerel NuGet önbelleğinde 10.0.12 sürümü var; bu, EF Core 10.0.12'nin bağlandığı Microsoft.Extensions.* sürümüyle aynı.
- Kaynak: https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.ihostapplicationbuilder · https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.ihostedservice
- Sonuç: D-020 (1). `NovaCore.EntityFrameworkCore` `FrameworkReference Microsoft.AspNetCore.App` yerine bu paketi kullanır.

### R-046: `IExceptionHandler` sırası (2026-09-25, faz 2a)
- Bulgu: *"Multiple implementations can be added, and they're called in the order registered."* Middleware handler'ları sırayla dener ve ilk `true` dönende durur. `AddExceptionHandler<T>` handler'ı singleton olarak kaydediyor.
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0#iexceptionhandler · https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.exceptionhandlerservicecollectionextensions.addexceptionhandler
- Sonuç: D-019. `ValidationExceptionHandler` ayrı projeye taşınınca `GlobalExceptionHandler`'dan önce kalmalı. Kaydını servis listesinin başına eklemek, host'taki çağrı sırasından bağımsız olarak bunu sağlar.

### R-047: CA1873 hangi `IsEnabled` guard'ını kabul ediyor? (2026-09-25, faz 2a; analyzer kaynağı + gözlem)
- Bulgu:
  - F2b.6'da `SecurityAuditLogger.UserRegistered` içindeki `Mask(email)` argümanı (Information seviyesi, metot çağrısı) CA1873 verdi. `IsEnabled` guard'ı eklemek, hem instance hem de static `[LoggerMessage]` metodunda uyarıyı kaldırmadı.
  - Nedeni analyzer kaynağında (`AvoidPotentiallyExpensiveCallWhenLogging.cs`, `IsGuardedByIsEnabled`): guard ancak `AreInvocationsOnSameInstance` doğruysa geçerli sayılıyor, yani `IsEnabled` ile log çağrısının alıcısı (receiver) aynı sembol olmalı. Instance bir `[LoggerMessage]` metodunun alıcısı `this`, static bir metodun ise yok. Extension metotta alıcı ilk argüman oluyor, yani `logger`.
  - Aynı kaynağa göre kural varsayılan olarak yalnızca Information ve altını denetliyor (`max_log_level` seçeneği). Bu yüzden Warning seviyesindeki `LoginFailed`/`Mask` ve Error seviyesindeki `string.Join` çağrıları işaretlenmedi. Yerel SDK 10.0.300 da böyle davrandı.
  - `this ILogger` extension biçimindeki `static partial class` ile kural guard'ı tanıdı ve build 0 uyarı verdi. Bu biçim MS'in kendi source-generation örneklerinde de var.
- Kaynak: https://github.com/dotnet/sdk/blob/main/src/Microsoft.CodeAnalysis.NetAnalyzers/src/Microsoft.CodeAnalysis.NetAnalyzers/Microsoft.NetCore.Analyzers/Performance/AvoidPotentiallyExpensiveCallWhenLogging.cs · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873 · https://learn.microsoft.com/dotnet/core/extensions/logging/source-generation#basic-usage · yerel build (SDK 10.0.300)
- Sonuç: F2b.6. `SecurityAuditLogger`'ın log tanımları `SecurityAuditLog` (internal static partial, `this ILogger` extension) sınıfında, `UserRegistered` guard'lı. Bastırma yapılmadı (Y-44). Diğer sınıflar R-044'teki instance biçimini kullanıyor.

## Faz 2b araştırması (2026-09-25)

### R-048: Options pattern: source-generated doğrulama, ValidateOnStart, bağımlı Configure (2026-09-25, faz 2b)
- Bulgu:
  - `[OptionsValidator]` işaretli boş bir `partial class : IValidateOptions<T>` için generator DataAnnotations niteliklerinden reflection'sız bir validator üretir. Generator *"automatically enabled by default when your project references Microsoft.Extensions.Options version 8 or later, or when building an ASP.NET application"*. Yerelde doğrulandı: `Microsoft.AspNetCore.App.Ref/10.0.8/analyzers/dotnet/cs/Microsoft.Extensions.Options.SourceGeneration.dll` var, yani `NovaCore.AspNetCore` paket eklemeden kullanabiliyor. Validator'ı ayrıca `AddSingleton<IValidateOptions<T>, TValidator>()` ile kaydetmek gerekiyor.
  - `ValidateOnStart()` doğrulamayı ilk kullanımdan host açılışına taşıyor. Bağlama (`BindConfiguration`) da options ilk oluşturulurken yapıldığı için, bağlanamayan bir değer de (ör. bool'a `"yes"`) açılışta hata veriyor.
  - `AddOptions<T>(name).Configure<TDep1, TDep2>(...)` kaydı bir `ConfigureNamedOptions<T, TDep1, TDep2>`'dir, yani `IConfigureNamedOptions<T>`. Başka bir options'a ya da servise bağlı yapılandırma kayıt anında değil, options ilk istendiğinde çalışır.
  - .NET 8'den beri rate limiter middleware'i `AddRateLimiter` kaydını zorunlu tutuyor: *"If AddRateLimiter is not called on app startup, ASP.NET Core throws an informative error"*. Bu yüzden `AddRateLimiter` çağrısı kalır, limitlere bağlı kısım `AddOptions<RateLimiterOptions>().Configure<IOptions<...>>` ile eklenir. MS rate-limit örneği ise options'ı kayıt anında `Bind` ile okuyor. Bu, P1-2'deki sorunun ta kendisi olduğu için örnek izlenmedi.
- Kaynak: https://learn.microsoft.com/dotnet/core/extensions/options-validation-generator · https://learn.microsoft.com/dotnet/core/extensions/options#options-validation · https://learn.microsoft.com/aspnet/core/breaking-changes/8/addratelimiter-requirement · https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0 · yerel targeting pack
- Sonuç: F2b.1a/1b, D-021.

### R-049: DbContext health check ve health uçlarının korunması (2026-09-25, faz 2b)
- Bulgu:
  - `AddDbContextCheck<TContext>(name, failureStatus, tags, customTestQuery)` EF Core'un `CanConnectAsync`'ini çağırır. Kontrolün adı varsayılan olarak `TContext` tipinin adıdır. Paket `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`; 10.0.12 nuspec'ine göre MIT lisanslı, yalnızca `Microsoft.Extensions.Diagnostics.HealthChecks(.Abstractions)` ve `Microsoft.EntityFrameworkCore.Relational`'a bağlı (ASP.NET yok).
  - `CanConnectAsync`: *"Any exceptions thrown when attempting to connect are caught and not propagated to the application, except for the ones indicating cancellation."*
  - `RequireHost` için MS uyarısı: *"API that relies on the Host header, such as HttpRequest.Host and RequireHost, are subject to potential spoofing by clients."* Önerilen yollar `ConnectionInfo.LocalPort` kontrolü, host filtering ya da `RequireAuthorization`. Readiness için MS örneği `Predicate = healthCheck => healthCheck.Tags.Contains("ready")` kullanıyor.
- Kaynak: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0#entity-framework-core-dbcontext-probe · https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0#use-health-checks-routing · https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.storage.relationaldatabasecreator.canconnectasync?view=efcore-10.0 · NuGet önbelleğindeki nuspec
- Sonuç: F2b.5. Koruma yöntemi kullanıcıya soruldu: Q-24 → D-022 (yalnızca Development).

### R-050: TimeProvider ve FakeTimeProvider (2026-09-25, faz 2b)
- Bulgu:
  - *"Always inject TimeProvider as a dependency rather than using DateTime or DateTimeOffset directly."* `FakeTimeProvider` `Microsoft.Extensions.TimeProvider.Testing` paketinde. Varsayılan başlangıç zamanı 2000-01-01 UTC, `new FakeTimeProvider(start)` ile değiştirilebiliyor. `Advance` saati ileri alıyor.
  - ASP.NET Core 8'den beri kimlik doğrulama ve Identity bileşenleri `ISystemClock` yerine `TimeProvider` kullanıyor: *"It can be set directly or by registering a provider in the dependency injection container."* Yani DI'a kaydedilen bir `TimeProvider` Identity'nin kendi zaman hesaplarını da etkileyebilir. Testte sahte saat gerçek "şimdi" ile başlatılır ki JWT doğrulaması (gerçek saat) ile çelişmesin.
- Kaynak: https://learn.microsoft.com/dotnet/core/extensions/timeprovider-testing · https://learn.microsoft.com/aspnet/core/breaking-changes/8/isystemclock-obsolete?view=aspnetcore-10.0
- Sonuç: F2b.3, D-021.

### R-051: ProblemDetails `instance` alanı (2026-09-25, faz 2b)
- Bulgu: `ProblemDetails.Instance`: *"A URI reference that identifies the specific occurrence of the problem."* NovaCore bugün `"{Method} {Path}"` yazıyor (`InfrastructureServiceCollectionExtensions.cs:25`, ör. `"GET /api/v1/products/42"`). Bu değer bir URI reference değil: HTTP metodu ve boşluk içeriyor. İsteğin yolu (`PathBase + Path`, `PathString.ToUriComponent()` ile kodlanmış) göreli bir URI reference'tır. Query string dahil edilmez, çünkü istemcinin gönderdiği secret'ları yanıta geri yansıtabilir (Y-35).
- Kaynak: https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.problemdetails.instance?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0#customizeproblemdetails-operation
- Sonuç: F2b.4.

## Faz 2c araştırması (2026-09-26)

### R-052: Modül assembly'lerinde .NET 10 yerleşik validation (2026-09-26, faz 2c)
- Bulgu:
  - *"The source generator creates metadata only for the assembly where AddValidation is called."* Metadata eksikse *"No automatic validation runs. Invalid input reaches the endpoint handler"*, üstelik hata ya da log da olmuyor. Bu yüzden her modül kendi assembly'sinde `AddValidation()` çağırmalı ve her modülün validation'ı testle korunmalı.
  - MS dokümanı iki adım öneriyor: *"If the assembly is a plain class library (it isn't based on the Microsoft.NET.Sdk.Web or Microsoft.NET.Sdk.Razor SDKs), add a package reference ... Microsoft.Extensions.Validation"* ve her assembly'de `AddValidation`'ı çağıran bir extension metot. Paket adımı, ASP.NET framework referansı olmayan kütüphaneler için.
  - NovaCore modülleri framework'ü `NovaCore.AspNetCore`'dan transitive alıyor. Bu durumda framework şunları zaten getiriyor:
    - Transitive framework referansları `@(FrameworkReference)`'a ekleniyor (`Microsoft.NET.Sdk.FrameworkReferenceResolution.targets:255-260`).
    - Hedef paket `Microsoft.Extensions.Validation.dll` referansını ve `Microsoft.Extensions.Validation.ValidationsGenerator` analyzer'ını getiriyor. Catalog modülünde `dotnet msbuild -t:ResolveTargetingPackAssets -getItem:Analyzer` ile görüldü.
    - Generator'ün interceptor namespace'i SDK tarafından net10+ için koşulsuz ekleniyor (`…targets:486`, `InterceptorsPreviewNamespaces`).
    - Paket, `Microsoft.AspNetCore.App.Ref/10.0.8/data/PackageOverrides.txt`'de yer alıyor. Açıkça referans verilirse budanır (NU1510, bkz. F2a.4 notu).
  - Scratch deneyi: transitive framework'lü bir `Microsoft.NET.Sdk` kütüphanesi, TWAE + `latest-recommended`, `services.AddValidation()` kütüphane içinde çağrıldı.
    - Build 0 uyarı verdi. Üretilen kod `#pragma warning disable ASP0029` içeriyor. Method-group handler'ların parametre tipleri de bulunuyor.
    - 400 yanıtı `application/problem+json`, `type` RFC 9110 §15.5.1, `status` 400. `instance` ve `traceId` host'un `CustomizeProblemDetails`'ından geliyor. Hata anahtarları PascalCase özellik adları.
    - Bunlar main'deki FluentValidation yanıtıyla aynı (gerçek API'den alındı). Farklar yalnızca üç:
      - `title`: "Validation failed" yerine framework varsayılanı "One or more validation errors occurred.". Handler'ların kendi `Results.ValidationProblem(...)` yanıtları bu başlığı zaten kullanıyor.
      - Mesaj metinleri değişiyor.
      - `[Required]` başarısız olursa aynı alandaki diğer attribute'lar çalışmıyor.
  - Model sırası: *"If property validation produces an error, the remaining steps are skipped"*. Yani `IValidatableObject` yalnızca property hataları yoksa çalışıyor, bu da denemede görüldü. MS önerisi: tek bir değeri denetleyen kural için özel `ValidationAttribute`, birden çok özelliği kapsayan kural için `IValidatableObject`. Private nested özel bir attribute record parametresinde çalıştı; generator attribute'ları reflection ile okuyor.
  - `[ValidatableType]` ve `[SkipValidation]` .NET 10'da deneysel (ASP0029). Minimal API parametre tipleri handler imzasından bulunduğu için bunlara gerek yok. Record ctor parametresindeki kısıtlar OpenAPI şemasına yansımadı.
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/validation?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#validation-support-in-minimal-apis · https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0#customize-validation-error-responses-using-iproblemdetailsservice · yerel SDK 10.0.300 ve targeting pack 10.0.8 dosyaları · scratchpad deneyi ve main'deki API'nin yanıtı (2026-09-26)
- Sonuç: F2c.1, D-023. Ek paket yok, her modül `AddModule` içinde `services.AddValidation()` çağırıyor. Boş Guid kuralı özel attribute ile yazılıyor (Q-25).

### R-053: `IAuthorizationRequirementData` ile permission attribute'u (2026-09-26, faz 2c)
- Bulgu:
  - MS örneği: `class MinimumAgeAuthorizeAttribute(int age) : AuthorizeAttribute, IAuthorizationRequirement, IAuthorizationRequirementData`. `GetRequirements()` requirement'ı döndürüyor, handler `AuthorizationHandler<…>` olarak singleton kaydediliyor, endpoint'te `.RequireAuthorization(new MinimumAgeAuthorizeAttribute(21))` kullanılıyor. Requirement karşılanmazsa sonuç 403.
  - `AuthorizeAttribute`'tan türeyen ve policy adı taşımayan attribute, varsayılan policy'yi (kimliği doğrulanmış kullanıcı) getiriyor. Yani anonim istek bugünkü gibi 401 alıyor.
  - .NET 8–10'da bu yalnızca Minimal API ve routed endpoint'lerde uygulanıyor. MVC, SignalR ve Blazor desteği .NET 11'le geliyor. NovaCore yalnızca Minimal API kullanıyor.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authorization/custom-authorization-policies-with-iauthorizationrequirementdata?view=aspnetcore-10.0
- Sonuç: F2c.2, D-023 (6). Attribute mevcut `PermissionRequirement`'ı döndürüyor, handler değişmiyor, `PermissionPolicyProvider` siliniyor.

### R-054: EXTEXP0018 bastırması hâlâ gerekli mi? (2026-09-26, faz 2c; gözlem)
- Bulgu:
  - `Microsoft.Extensions.Caching.Hybrid` 10.0.0 ile `Microsoft.Extensions.Caching.Abstractions` 10.0.12'nin `lib/net10.0` assembly'lerinde `EXTEXP` dizgesi yok. Abstract `HybridCache` sınıfı bu ikincisinde.
  - Çözüm, `NoWarn`'da `EXTEXP0018` olmadan (global property ile `NoWarn=RMG020;xUnit1051`, ayrı `--artifacts-path`) `--no-incremental` derlendi: 0 uyarı, 0 hata.
  - Override'ın gerçekten uygulandığını görmek için aynı yolla RMG020 de kaldırıldı: Catalog'da 28 RMG020 hatası çıktı.
  - MS dokümanında EXTEXP0018'e dair bir ifade bulunamadı. R-008'e göre güncel HybridCache dokümanları deneysel bir uyarıdan söz etmiyor.
- Kaynak: NuGet önbelleğindeki assembly'ler, yerel build (SDK 10.0.300) · https://learn.microsoft.com/aspnet/core/performance/caching/hybrid?view=aspnetcore-10.0
- Sonuç: F2c.3. `EXTEXP0018` `NoWarn`'dan çıkarılıyor. PLAN'daki "uyarı gelirse sor" koşulu oluşmadı.

### R-055: Açılışta hata bekleyen testler neden ara sıra `ObjectDisposedException` alıyor? (2026-09-26, faz 2c; kaynak + gözlem)
- Bulgu:
  - F2c.4 doğrulamasında `restore --force` → build → test sırasıyla ~45 tam koşuda 2 kez aynı hata görüldü: `ConfigurationValidationTests.Invalid_configuration_stops_the_host_at_startup` (Cors satırı), `factory.Server`'dan beklenen validation mesajı yerine *"Cannot access a disposed object. Object name: 'IServiceProvider'"* aldı. Yalnızca ilgili sınıflar koşarken (paralel yük yokken) yeniden üretilemedi.
  - Kök neden `Microsoft.AspNetCore.Mvc.Testing`'deki `DeferredHostBuilder.DeferredHost.StartAsync`: sonucu `_hostStartedTcs`'ten beklemeden önce koşulsuz olarak `_host.Services.GetRequiredService<IHostApplicationLifetime>()` çağırıyor. Minimal hosting'de host'u uygulamanın kendi `app.Run()`'ı başlatıyor. Açılış hata verirse `RunAsync`'in `finally`'si host'u dispose ediyor, gerçek exception da `EntryPointCompleted(exception)` ile `_hostStartedTcs`'e yazılıyor. Test thread'i `StartAsync`'e bundan sonra girerse gerçek hata `ObjectDisposedException` ile örtülüyor.
  - Etkilenebilecek testler: açılışın düşmesini ve mesajı bekleyenler. `ConfigurationValidationTests` (F2b.1a), `EmailSenderRegistrationTests`'in Production/sender yok vakası (F0.5; `ObjectDisposedException` `InvalidOperationException`'ın alt tipi, ama `Assert.Throws` tam tip istiyor) ve `DatabaseMigrationServiceTests`'in `yes` vakası.
  - Hata F2c öncesinden beri var ve F2c koduyla ilgili değil. Uygulamanın açılış yolunda F2c yalnızca endpoint filtrelerini azalttı.
  - Bizim kodumuzda temiz bir düzeltme yok: gerçek exception yalnızca private bir TCS'te. `IStartupValidator` sarmalayıcısı `ValidateOnStart` hatalarını yakalayabilir, ama hosted service hatalarını (`EmailSenderStartupCheck`) yakalayamaz.
- Kaynak: https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Mvc/Mvc.Testing/src/DeferredHostBuilder.cs · https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.hostingabstractionshostextensions.runasync · yerel koşular (SDK 10.0.300)
- Sonuç: Kapsam dışı (F2c'nin işi değil, test altyapısındaki framework yarışı), PROGRESS → "Sonraya bırakılanlar". Faz raporunda kullanıcıya bildirilir.
- Ek gözlem (2026-09-27, F3b.1b): Eklentinin testleri (çok sayıda host açılışı ve PBKDF2) diğer testlerle paralel koşunca yarış tam koşuların çoğunda (4'te 3) görüldü; yeni testler hariç tutulunca 4/4 yeşildi. Bu testler `DisableParallelization` koleksiyonuna alındı ve host'lar sınıf başına paylaşıldı. Ardından 13/13 tam koşu yeşil. Kök neden değişmedi.

## Faz 3a araştırması (2026-09-27)

### R-056: Authority/JWKS ile JWT doğrulama, simetrik anahtarın yeri ve ağsız test (2026-09-27, faz 3a)
- Bulgu:
  - MS'in temel örneği yalnızca `Authority` ve `Audience` ayarlıyor: *"A basic implementation of the AddJwtBearer can validate just the audience and the issuer. The signature must be validated…"* Metadata ve imza anahtarları `Authority`'den türetilen `.well-known/openid-configuration` belgesinden keşfediliyor.
  - Aynı sayfa: *"Asymmetric keys should **always** be used when creating access tokens."* *"Don't create access tokens in production apps… Limit creating access tokens to test scenarios."* Test için: *"It's essential that these features are only disabled in the test environment."* Bu, simetrik anahtarın ortamla (Development) sınırlanmasının birincil gerekçesi.
  - `JwtBearerPostConfigureOptions.PostConfigure`: `Authority` varsa `MetadataAddress = Authority + "/.well-known/openid-configuration"`. `RequireHttpsMetadata` açıkken `https` olmayan adres `InvalidOperationException` verir (ilk istekte). Backchannel `options.BackchannelHttpHandler ?? new HttpClientHandler()` ile ve yalnızca `options.Backchannel` boşsa kuruluyor. Yani testte handler `Configure` aşamasında verilirse Authority → metadata → JWKS yolu ağsız çalışıyor.
  - `JwtBearerHandler.SetupTokenValidationParametersAsync`: `ConfigurationManager` bir `BaseConfigurationManager` ise IdentityModel'e veriliyor. Değilse discovery'nin issuer'ı ve anahtarları `ValidIssuers`/`IssuerSigningKeys`'e **ekleniyor** (`Concat`). Statik bir anahtar ve issuer, Authority ile birlikte kullanılabiliyor.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0 · https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Security/Authentication/JwtBearer/src/JwtBearerPostConfigureOptions.cs · https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Security/Authentication/JwtBearer/src/JwtBearerHandler.cs
- Sonuç: F3a.1, D-024 (1)–(3). `Jwt:Authority` eklendi. Simetrik `Jwt:SigningKey` yalnızca Development'ta kabul ediliyor. Test IdP'si discovery ve JWKS'yi `BackchannelHttpHandler` üzerinden sunuyor.

### R-057: `IClaimsTransformation`, HybridCache'in süre hesabı ve security stamp claim'i (2026-09-27, faz 3a)
- Bulgu:
  - `IClaimsTransformation.TransformAsync`: *"this will be run on each AuthenticateAsync call, so its safer to return a new ClaimsPrincipal if your transformation is not idempotent."* Kavram sayfası: *"This method might be called multiple times."* MS örneği transformation'ı DI'a kaydediyor (`AddTransient<IClaimsTransformation, …>`).
  - HybridCache (Microsoft.Extensions.Caching.Hybrid 10.0) L1 girişini `IMemoryCache.CreateEntry` ile yazıyor ve `AbsoluteExpirationRelativeToNow` ayarlıyor (`DefaultHybridCache.SetL1`). Süreyi `MemoryCache` kendi saatiyle hesaplıyor: `MemoryCacheOptions.Clock` (`Microsoft.Extensions.Internal.ISystemClock`, *"Abstracts the system clock to facilitate testing"*). HybridCache'in DI'dan aldığı `TimeProvider` yalnızca tag invalidation zaman damgası için kullanılıyor. Sonuç: TTL testi için sahte saat `MemoryCacheOptions.Clock`'a bağlanmalı.
  - `ClaimsIdentityOptions.SecurityStampClaimType` varsayılanı: *"Defaults to "AspNet.Identity.SecurityStamp"."* Security stamp kullanıcının kimlik bilgileri değişince değişen rastgele bir değer. ASP.NET Identity rol değişikliğinde stamp'i yenilemiyor, yani rol değişikliği TTL ile yansıyor (PLAN F3a.2 kabul kriteri).
- Kaynak: https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authentication.iclaimstransformation.transformasync?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/security/authentication/claims?view=aspnetcore-10.0 · https://github.com/dotnet/extensions/blob/release/10.0/src/Libraries/Microsoft.Extensions.Caching.Hybrid/Internal/DefaultHybridCache.L2.cs · https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.memory.memorycacheoptions.clock · https://learn.microsoft.com/dotnet/api/microsoft.extensions.internal.isystemclock · https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.identity.claimsidentityoptions.securitystampclaimtype?view=aspnetcore-10.0
- Sonuç: F3a.2, D-024 (4)–(7).

### R-058: JWT lifetime doğrulaması `TimeProvider`'a bağlanabilir mi? (2026-09-27, faz 3a)
- Bulgu: `TokenValidationParameters`'ta (IdentityModel 8.19) bir `TimeProvider` yok. Sürenin dışarıdan kontrol edilebilmesinin tek yolu `LifetimeValidator` delegesi. Doküman: *"If set, this delegate will be called to validate the lifetime of the token, instead of default processing. This means that no default lifetime validation will occur."* `TimeProvider` yalnızca deneysel `ValidationParameters` API'sinde var.
- Kaynak: https://learn.microsoft.com/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters.lifetimevalidator?view=msal-web-dotnet-latest · https://learn.microsoft.com/dotnet/api/microsoft.identitymodel.tokens.validators.validatelifetime?view=msal-web-dotnet-latest
- Sonuç: R-050'deki hizalama yapılmıyor (D-024 (8)). Varsayılan doğrulamayı elle yazılmış bir delegeyle değiştirmenin riski, testte "şimdi"den başlayan sahte saatin maliyetinden büyük.

## Faz 3b araştırması (2026-09-27)

### R-059: OpenIddict 7.7.1: paketler, access token biçimi, anahtarlar, refresh token rotasyonu (2026-09-27, faz 3b)
- Bulgu:
  - Güncel kararlı sürüm 7.7.1 (2026-09-17). Lisans Apache-2.0, `net10.0` hedefi var. `OpenIddict.EntityFrameworkCore` 7.7.1'in bağımlılıkları: `OpenIddict.Core`, `OpenIddict.EntityFrameworkCore.Models`, `Microsoft.EntityFrameworkCore.Relational [10.0.11, )`. `OpenIddict.AspNetCore` meta paketi client ve validation paketlerini de getiriyor; sunucu için `OpenIddict.Server.AspNetCore` yetiyor.
  - Access token şifrelemesi varsayılan olarak açık (JWE). *"the OpenIddict server enforces encryption for all the token types it supports"*. `DisableAccessTokenEncryption()` *"when integration with third-party APIs/resource servers is desired"* için var. Kaynakta: *"SHOULD only be done when issuing tokens to third-party resource servers/APIs you don't control and don't fully trust."*
  - `OpenIddictServerConfiguration` bir `IPostConfigureOptions<OpenIddictServerOptions>`. Asimetrik imza anahtarı yoksa ID0086, şifreleme anahtarı yoksa ID0085 ile `InvalidOperationException` fırlatıyor. Şifreleme anahtarı, access token şifrelemesi kapalıyken de zorunlu (code ve refresh token için).
  - `AddEphemeralSigningKey(algorithm)` RSA ve ECDSA (ES256/384/512) algoritmalarını destekliyor. `AddSigningCertificate(X509Certificate2)` anahtar türünü kısıtlamıyor, yalnızca `DigitalSignature` kullanımını kontrol ediyor. Ephemeral anahtarlar *"should only be used during development"*.
  - `OpenIddictServerOptions` varsayılanları:
    - `RefreshTokenReuseLeeway` 30 sn; rolling refresh token'lar açık (`DisableRollingRefreshTokens` false).
    - `RequireProofKeyForCodeExchange` false; ömürler: access 1 saat, refresh 14 gün.
    - `TimeProvider` DI'dan alınıyor, yoksa `TimeProvider.System`.
  - `CodeChallengeMethods` varsayılan olarak `plain` ve `S256`'yı içeriyor (F3b.1a testi discovery'de `plain`'i yakaladı). RFC 9700 §2.1.1 istemcilerin `S256` kullanmasını istiyor; `plain`'de challenge, verifier'ın kendisi. Eklenti `plain`'i listeden siliyor.
  - `ValidateTokenEntry`: zaten "redeemed" işaretli bir token gelirse *"revoke the entire chain of tokens associated with the authorization"* (`RevokeByAuthorizationIdAsync`). Refresh token'lar için leeway süresince tekrar kullanıma izin veriliyor. Authorization'ın kendisi iptal edilmiyor.
  - EF entegrasyonu: `modelBuilder.UseOpenIddict()` (varsayılan `string` anahtar) OnModelCreating içinde çağrılabiliyor. Store kaydı: `AddCore(o => o.UseEntityFrameworkCore().UseDbContext<T>())`.
  - Minimal API passthrough: örnek `Zirku.Server`, `connect/authorize`'ı `app.MapMethods(...)` ile map'liyor ve `Results.SignIn(principal, null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)` döndürüyor. `Velusia.Server`, Identity ile authorization code akışını gösteriyor:
    - cookie yoksa Challenge;
    - implicit consent'te kalıcı authorization oluşturuluyor;
    - token endpoint'inde kullanıcı yeniden doğrulanıyor (`CanSignInAsync`);
    - security stamp claim'i hiçbir token'a yazılmıyor.
- Kaynak: https://api.nuget.org/v3/catalog0/data/2026.09.17.20.00.23/openiddict.aspnetcore.7.7.1.json · https://api.nuget.org/v3/catalog0/data/2026.09.17.20.01.01/openiddict.entityframeworkcore.7.7.1.json · https://documentation.openiddict.com/configuration/token-formats · https://documentation.openiddict.com/configuration/encryption-and-signing-credentials · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Server/OpenIddictServerOptions.cs · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Server/OpenIddictServerConfiguration.cs · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Server/OpenIddictServerBuilder.cs · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Server/OpenIddictServerHandlers.Protection.cs · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.EntityFrameworkCore/OpenIddictEntityFrameworkCoreHelpers.cs · https://github.com/openiddict/openiddict-samples/tree/dev/samples/Zirku · https://github.com/openiddict/openiddict-samples/tree/dev/samples/Velusia
- Sonuç: D-026 (1), (3), (5), (7), (9). Çekirdek JwtBearer IdP'den bağımsız standart bir resource server olduğu için access token şifrelemesi kapatılıyor. Anahtar eksikliği `ValidateOnStart` ile açılışta yakalanıyor.

### R-060: DbContext bir pakette, migration'lar uygulamada (2026-09-27, faz 3b)
- Bulgu: EF Core, migration'ları DbContext'ten farklı bir assembly'de tutmayı destekliyor. *"You can store migrations in a different project from the one containing your DbContext."* Önerilen düzen:
  - veri projesi DbContext'i ve entity'leri taşır;
  - migration projesi migration'ları, snapshot'ı ve `IDesignTimeDbContextFactory<TContext>`'i taşır;
  - çalışma anında `x.MigrationsAssembly("…")` ayarlanır.
  
  Uygulama migration'ları çalışma anında uyguluyorsa migration assembly'sini referans almalı.
- Kaynak: https://learn.microsoft.com/ef/core/managing-schemas/migrations/projects
- Sonuç: D-026 (2). `NovaCore.Identity.OpenIddict` `IdentityServerDbContext`'i taşır. Api host'u SQL Server migration'larını ve design-time factory'yi taşır (F3b.3a). Paket sağlayıcıya bağlanmaz.

### R-061: Sağlayıcı cookie'si, antiforgery ve `IClaimsTransformation` (2026-09-27, faz 3b; kaynak + gözlem)
- Bulgu:
  - *"For the following AddAuthentication call without a default authentication scheme specified, two authentication handlers are added to the app using their default authentication scheme names"*: parametresiz `AddAuthentication()` varsayılan şemayı değiştirmiyor. `AddIdentityCookies` yalnızca şemaları ekliyor (Identity UI'ın `AddIdentity`'si ise varsayılanları cookie'ye çeviriyor). Testte API anonim çağrıya 302 değil 401 veriyor.
  - Antiforgery: .NET 8 notu *"WebApplicationBuilder automatically adds the middleware when the antiforgery services have been registered"* diyor. Antiforgery sayfası ise *"Blazor and Minimal APIs require an explicit call to UseAntiforgery"* diyor. Gözlem bu pipeline'da ikincisini doğruluyor: `UseCoreWeb` `UseAuthentication`/`UseAuthorization`'ı açıkça çağırıyor, `[FromForm]` parametreli login ucu "contains anti-forgery metadata, but a middleware was not found" ile 500 verdi. Aynı sayfa açık doğrulamayı da anlatıyor: *"resolve IAntiforgery from DI and call ValidateRequestAsync or IsRequestValidAsync explicitly"*.
  - `IClaimsTransformation` her `AuthenticateAsync` çağrısında, hangi şema olursa olsun çalışıyor (R-057). Token endpoint'indeki `AuthenticateAsync(OpenIddict şeması)` da çekirdeğin `PermissionClaimsTransformation`'ından geçiyor. OpenIddict örneğindeki "principal'ın bütün claim'lerini kopyala" kalıbı bu yüzden çözülmüş `permission` claim'lerini refresh sonrası access token'a taşıdı (F3b.1b testi yakaladı).
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authorization/authorize-with-a-specific-scheme?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-8.0?view=aspnetcore-10.0#minimal-apis · https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0#antiforgery-with-minimal-apis · yerel koşu
- Sonuç: D-026 (6):
  - Login formu `ReadFormAsync` + `IAntiforgery.IsRequestValidAsync` ile açıkça doğrulanır. Çekirdeğe `UseAntiforgery` eklenmez; yalnızca bir eklentinin ihtiyacı için çekirdek pipeline'ı değişmezdi.
  - Token endpoint'i principal'ı kopyalarken `permission` claim'lerini dışarıda bırakır.

### R-062: .NET 10'un validation generator'ü internal request tiplerini kapsıyor mu? (2026-09-27, faz 3b; gözlem + doküman)
- Bulgu:
  - Eklentideki `internal sealed record RegisterRequest` (internal bir static sınıf içinde) için generator metadata üretmedi. Geçersiz gövde handler'a ulaştı, yanıtta Identity'nin `password` hatası vardı.
  - `EmitCompilerGeneratedFiles` çıktısında `ValidatableInfoResolver.g.cs` handler'ın servis parametrelerini (`IOptions<JwtOptions>`, `UserManager<…>`) kapsıyordu, request tipini kapsamıyordu. Aynı handler'ın bağlandığı bir yerde:
    - record `public` olunca tip resolver'a girdi;
    - yalnızca kapsayan sınıf `public` olunca girmedi.
  - MS'in "internal da olur" diyen diyagnostikleri (ASP0034 *"Make the parameter type and each of its containing types public or internal"*) aspnetcore-11.0 dokümanında. .NET 10 dokümanı yalnızca *"Validated types and properties are accessible to generated code"* diyor.
  - Sonuç: bu SDK'da (10.0.300, targeting pack 10.0.8) generator yalnızca public request tiplerini kapsıyor. Eksik metadata hata ya da log üretmiyor (R-052).
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/validation?view=aspnetcore-10.0#troubleshoot-generated-validation-metadata · https://learn.microsoft.com/aspnet/core/diagnostics/asp0034?view=aspnetcore-11.0 · yerel derleme (`-p:EmitCompilerGeneratedFiles=true`)
- Sonuç: D-026'ya ek: eklentinin doğrulanan request gövdeleri (`RegisterRequest` ve sonrakiler) public, üst düzey record'lardır ve PublicAPI'de izlenir. Y-23 gereksiz public API'yi yasaklıyor; burada public olmaları doğrulamanın çalışmasının koşulu. Endpoint sınıfları internal kalıyor. Api host'unun `ValidationTests`'i bu uçları da kapsayacak (F3b.3a).

### R-063: Testteki TOTP hesabı için CA5350 bastırması (2026-09-27, faz 3b)
- Bulgu:
  - 2FA testleri bir authenticator uygulamasının yerine kodu kendisi hesaplıyor. RFC 6238 ve ASP.NET Core Identity'nin doğrulayıcısı HMAC-SHA1 kullanıyor. Identity'nin `AuthenticatorTokenProvider.GenerateAsync`'i boş döndüğü için (kod uygulamada üretilir) sunucudan alınamıyor.
  - `HMACSHA1` CA5350'yi tetikliyor. Kuralın dokümanı: *"Suppress a warning from this rule when the level of protection needed for the data does not require a security guarantee."* Testteki kod bir authenticator uygulamasını taklit ediyor ve güvenlik garantisi gerektirmiyor.
  - Başka bir API ile (`IncrementalHash` + `HashAlgorithmName.SHA1`) analyzer'ı atlatmak Y-11'e aykırı olurdu.
- Kaynak: https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca5350 · https://datatracker.ietf.org/doc/html/rfc6238
- Sonuç: Y-44'ün izin verdiği kalıcı ve dar bastırma: yalnızca `tests/.../IdentityServer/AuthenticatorCode.For` üzerinde gerekçeli `[SuppressMessage]`.

### R-064: Api host'unda identity store'u: retry stratejisi ve migration analyzer'ı (2026-09-27, faz 3b; kaynak)
- Bulgu:
  - `OpenIddictEntityFrameworkCoreAuthorizationStore` açık transaction'larını (`DeleteAsync`, `PruneAsync`) `context.Database.CreateExecutionStrategy().ExecuteAsync(...)` içinde açıyor. Toplu iptaller (`RevokeBySubjectAsync`) `ExecuteUpdateAsync` kullanıyor. Yani `EnableRetryOnFailure` (SQL Server retrying strategy) ile uyumlu; modüllerle aynı ayar kullanılabilir.
  - Üretilen ilk migration bileşik index'ler için `columns: new[] { ... }` geçiyor, bu da CA1861'i tetikliyor. CA1861'in "When to suppress" bölümü: *"The invocation only runs once."* ve *"The array could be mutated within the invoked method, or you aren't sure if it would mutated."* Migration'ın `Up`/`Down`'u veritabanı başına bir kez çalışıyor; `MigrationBuilder` diziyi operasyonda saklıyor.
- Kaynak: https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.EntityFrameworkCore/Stores/OpenIddictEntityFrameworkCoreAuthorizationStore.cs · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1861
- Sonuç:
  - Api host'u context'i `EnableRetryOnFailure` ile kaydediyor (F3b.3a).
  - CA1861 `.editorconfig`'te yalnızca `src/**/Migrations/*.cs` için kapalı (Y-44'ün izin verdiği kalıcı bastırma). Kök CLAUDE.md'nin bastırma listesine eklendi.

### R-065: docker-compose imaj sürümleri, Keycloak realm import'u ve compose'un zorunlu değişkenleri (2026-09-27, faz 3b; kaynak + gözlem)
- Bulgu:
  - Güncel etiketler (2026-09-27, registry API'leri):
    - Keycloak `26.7.4` (quay.io, 2026-09-16);
    - SQL Server 2022 için en yüksek CU `2022-CU27-ubuntu-22.04` (mcr.microsoft.com);
    - Seq `2026.1.17182` (2026-09-24);
    - Jaeger v2 `jaegertracing/jaeger:2.21.0` (2026-09-15). Jaeger v1'in `all-in-one` imajı yerine v2 imajının varsayılan all-in-one yapılandırması OTLP 4317/4318 ve UI 16686'yı sunuyor.
  - Keycloak dokümanı:
    - *"For the Keycloak containers, the import directory is `/opt/keycloak/data/import`"*, `--import-realm` ile. *"If a realm already exists in the server, the import operation is skipped."*
    - İlk admin: `KC_BOOTSTRAP_ADMIN_USERNAME` / `KC_BOOTSTRAP_ADMIN_PASSWORD`. Geliştirme örneği `start-dev` kullanıyor ve portu `127.0.0.1`'e bağlıyor.
  - Gözlem: `docker compose config`, profili aktif olmayan servislerdeki `${VAR:?}` zorunlu değişkenlerini de yorumlayıp hata veriyor (scratch denemesi). Keycloak ana dosyada bir profile konsaydı, `.env`'inde `KEYCLOAK_ADMIN_PASSWORD` olmayan herkesin compose komutları bozulurdu.
  - Bu makinede host'un 8080 portunu bir `httpd` kullanıyordu.
  - Smoke denemesi (kullanıcı onayıyla, 2026-09-27):
    - `Realm 'novacore' imported`; discovery issuer `http://localhost:8180/realms/novacore`;
    - admin API'ye göre client public, standard flow, PKCE `S256`, audience mapper'ı `novacore.api`, rol mapper'ı `role`, roller Administrator/User;
    - container, volume ve ağ silindi, imaj yerelde kaldı.
- Kaynak: https://quay.io/repository/keycloak/keycloak?tab=tags · https://mcr.microsoft.com/v2/mssql/server/tags/list · https://hub.docker.com/r/datalust/seq/tags · https://hub.docker.com/r/jaegertracing/jaeger/tags · https://www.keycloak.org/server/importExport · https://www.keycloak.org/server/containers · yerel `docker compose` 29.2.1 denemeleri
- Sonuç: F3b.4.
  - Keycloak ayrı bir overlay dosyasında (`docker-compose.keycloak.yml`), host portu `127.0.0.1:8180`.
  - İmajlar pinli; Jaeger v2'ye geçildi (v1 EOL).
  - `.env.example` iki parolanın yer tutucusunu içeriyor.

### R-066: Built-in browser'da Scalar'ın OAuth popup'ı (2026-09-27, faz 3b; gözlem)
- Bulgu (F3b.5b, uygulama https profilinde, ayrı `NovaCore_F3b5` veritabanıyla):
  - Scalar "Authorize" butonu authorize URL'sini `window.open` ile açıyor. Built-in browser bunu yeni bir pencere yerine aynı sekmede açtı: `/connect/authorize` 302 → `/account/login`.
  - Giriş başarılı oldu: `login_succeeded`, ardından authorize code verdi ve `/scalar/v1?code=…&state=…&iss=…`'e döndü. Ancak code'u token'a çevirecek asıl Scalar penceresi (opener) artık yoktu, bu yüzden Scalar token isteği göndermedi.
  - Aynı sekmede, aynı public `scalar` client'ı ve PKCE S256 ile akış elle sürdürüldü:
    - IdP oturumu açık olduğu için authorize sormadan code verdi;
    - `/connect/token` 200 döndü: RS256, `typ: at+jwt`, `iss` `https://localhost:5443/`, `aud` `novacore.api`, `client_id` `scalar`, rol Administrator, `permission` claim'i yok, `expires_in` 899, id_token var.
  - Scalar'ın API client'ından `GET /api/v1/products`:
    - OAuth2 şeması seçili ama token'sız kalınca, elle eklenen `Authorization` başlığına rağmen 401 döndü. Muhtemelen Scalar seçili şemanın boş başlığını gönderiyor.
    - Şema kaldırılınca aynı token'la **200 OK** (sayfalı liste).
    - Aynı token'la sayfadan doğrudan `fetch` de 200.
- Kaynak: yerel çalıştırma logları (`SECURITY login_succeeded`, `HTTP GET /connect/authorize 302`, `HTTP POST /connect/token 200`, `HTTP GET /api/v1/products 200`) ve built-in browser ekran görüntüleri.
- Sonuç:
  - Sağlayıcı, çekirdek doğrulama ve Scalar'ın istek arayüzü tarayıcıda uçtan uca çalışıyor.
  - Scalar'ın popup tabanlı code değişimi bu tarayıcı panelinde denenemedi; normal bir tarayıcıda popup açılır. Aynı adımlar otomatik testlerde de koşuyor (`AuthorizationCodeFlowTests`, `IdentityServerWiringTests`).
  - Scalar'ın token'sız OAuth2 seçimiyle başlığı ezmesi "Sonraya bırakılanlar"a yazıldı.

### R-067: Faz 3b code review — `SignInManager`'ın NotAllowed sırası ve cookie yenilemesinin `IssuedUtc`'si (2026-09-27, faz 3b; kaynak + kırmızı test)
- Soru: Giriş sayfasındaki "e-postanı onayla" mesajı gerçekten yalnızca doğru paroladan sonra mı çıkıyor? `auth_time` ve `max_age` için cookie'nin `IssuedUtc`'si oturum açma zamanı mı?
- Bulgu:
  - `SignInManager.CheckPasswordSignInCoreAsync` önce `PreSignInCheck`'i çalıştırıyor: `CanSignInAsync` false ise (onaysız e-posta) parolaya bakmadan `SignInResult.NotAllowed` dönüyor, lockout sayacına da dokunmuyor. Yani mesaj yanlış parolayla da çıkıyordu ve kayıtlı ama onaysız adresleri ele veriyordu (hesap tespiti). Eski modülde de aynıydı. Kırmızı test: yanlış parolayla "Confirm your email address" döndü.
  - `CookieAuthenticationHandler`:
    - `HandleAuthenticateAsync`, `ValidatePrincipal` sonrası `context.ShouldRenew` true ise `RequestRefresh`'i çağırıyor. `RequestRefresh` yarı ömür kontrolü yapmadan yeniliyor, `_refreshIssuedUtc = TimeProvider.GetUtcNow()`. `SecurityStampValidator` doğrulamadan sonra `ShouldRenew = true` yapıyor; `ValidationInterval = 0` (D-029 (5)) ile bu her okumada oluyor. Kayan süre de yarı ömürden sonra aynı şeyi yapıyor.
    - `FinishResponseAsync` yenilenen bilete `IssuedUtc = _refreshIssuedUtc` yazıyor. `CloneTicket` ise `Properties.Items`'ı olduğu gibi kopyalıyor.
    - `HandleSignInAsync` `IssuedUtc`'yi `Events.SigningIn`'den önce atıyor; `SigningIn` yalnızca oturum açılırken çalışıyor, yenilemede değil.
  - Sonuç olarak `IssuedUtc` son yenileme zamanı. Kırmızı test (sahte saat): 16. dakikada bir authorize isteği, 21. dakikadaki id_token'ın `auth_time`'ını 960 sn ileri kaydırdı; `max_age` hiç tetiklenmezdi. OpenIddict örnekleri de `IssuedUtc` kullanıyor, ama onlarda stamp doğrulaması 30 dakikada bir.
- Kaynak: https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Identity/Core/src/SignInManager.cs · https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Security/Authentication/Cookies/src/CookieAuthenticationHandler.cs · OpenID Connect Core 1.0 §2 (`auth_time`), §3.1.2.1 (`max_age`)
- Sonuç (D-030):
  - Giriş sayfası `NotAllowed`'da parolayı kendisi kontrol ediyor (lockout'taysa kontrol etmiyor). Doğruysa mesaj "e-postanı onayla", yanlışsa genel mesaj ve `AccessFailedAsync`.
  - Oturum açma zamanı cookie'nin `AuthenticationProperties.Items`'ına `OnSigningIn`'de yazılıyor, `auth_time` ve `max_age` oradan okunuyor. Kaydı olmayan oturum `max_age` isteğinde yeniden giriş istiyor.
  - Ayrıca: `AddAuthentication()` `TimeProvider.System`'ı `TryAddSingleton` ile kaydediyor (probe testiyle görüldü), yani eklenti zaten kendi kaydıyla `TimeProvider` getiriyor; code review'daki "TimeProvider kaydı yok" bulgusu gerçek bir hata değil.

## Faz 4a araştırması (2026-09-28)

### R-068: Outbox'ın yazılması ve dağıtılması: SaveChanges sırası, execution strategy, idempotent tüketici, anahtar, zamanlayıcı (2026-09-28, faz 4a; kaynak)
- Soru: Bir interceptor'ın eklediği outbox satırları entity ile aynı transaction'a giriyor mu? Dispatcher `EnableRetryOnFailure` ile nasıl uyumlu kalır? "Processed tablosu" neye göre anahtarlanmalı? Outbox'ın anahtarı ve dağıtım sırası ne olmalı? Arka plan döngüsü neyle beklemeli?
- Bulgu:
  - EF Core `DbContext.SaveChanges(Async)` sırası: `SavingChanges` event'i → `UpdateLogger.SaveChangesStarting(Async)` (interceptor'lar) → `TryDetectChanges()` → `StateManager.SaveChanges(Async)`. `StateManager.SaveChanges`, `AutoTransactionBehavior.Never` değilse işi `Dependencies.ExecutionStrategy.Execute(...)` içinde yapar ve kaydedilecek girişleri (`GetEntriesToSave`) orada toplar. Yani `SavingChanges` içinde eklenen satırlar aynı kayda ve aynı transaction'a girer, interceptor bir kez çalışır, retrying strategy yalnızca izlenen girişlerin yazımını yeniden oynatır. MS: SaveChanges çoğu sağlayıcıda transaction'lıdır, "all the operations either succeed or fail".
  - Connection resiliency: "When retries are enabled, each operation you perform via EF Core becomes its own retriable operation. That is, each query and each call to SaveChangesAsync() will be retried as a unit". `BeginTransaction` ile açılan transaction retrying strategy'de `InvalidOperationException` verir. `ExecutionStrategy.OnFirstExecution` açık transaction varsa ve `RetriesOnFailure` true ise bu hatayı atar. `CallOnWrappedException` `DbUpdateException`'ın iç hatasına bakarak `ShouldRetryOn`'u çağırır.
  - Idempotent Consumer (Azure Architecture Center): tüketici işlediği anahtarları kaydeder ve tekrarları atlar. Aynı depoyu birden çok tüketici paylaşıyorsa "key the records on a composite of the consumer identity and the message identity. A store keyed on the message identity alone would let the first consumer suppress processing for all the others." İşaret ile yan etki ayrı adımlarda yazılırsa aradaki çökmede mesaj yeniden işlenir; bunu kapatan atomik varyant Inbox pattern'idir. Eşzamanlı tüketicilere karşı uniqueness constraint önerilir. Ayrıca "Prefer naturally idempotent operations".
  - SQL Server index rehberi: clustered anahtar dar, benzersiz, sürekli artan ve değişmez olmalı. `IDENTITY` ile doldurulan tek `bigint` hepsini sağlar; tek `uniqueidentifier` "doesn't satisfy the ever-increasing property unless the values are generated sequentially". `SqlGuid.CompareTo` SQL Server davranışıyla "the last six bytes of a value are most significant"; Guid v7'nin zaman damgası ilk baytlarda olduğu için SQL Server sırasında artan değildir.
  - EF Core SQLite sınırı: `DateTimeOffset` için eşitlik sorgusu çalışır, karşılaştırma ve sıralama çevrilemez (istemcide değerlendirilmesi gerekir).
  - `PeriodicTimer(TimeSpan, TimeProvider)` .NET 8'den beri var. `WaitForNextTickAsync` bir auto-reset event gibi davranır: iki çağrı arasındaki tick'ler tek tick'te birleşir, kaybolmaz.
  - .NET 10: `BackgroundService` `ExecuteAsync`'in tamamını arka plan task'ında çalıştırır (önceden ilk `await`'e kadarki kısım açılışta çalışıyordu). Hosted service'te scoped servis için scope açılır. `ExecuteAsync`'ten kaçan hata varsayılan olarak host'u durdurur.
- Kaynak: https://github.com/dotnet/efcore/blob/release/10.0/src/EFCore/DbContext.cs · https://github.com/dotnet/efcore/blob/release/10.0/src/EFCore/ChangeTracking/Internal/StateManager.cs · https://github.com/dotnet/efcore/blob/release/10.0/src/EFCore/Storage/ExecutionStrategy.cs · https://learn.microsoft.com/ef/core/saving/basic · https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency · https://learn.microsoft.com/azure/architecture/patterns/idempotent-consumer · https://learn.microsoft.com/sql/relational-databases/sql-server-index-design-guide#clustered-index-design-guidelines · https://learn.microsoft.com/sql/connect/ado-net/sql/compare-guid-uniqueidentifier-values · https://learn.microsoft.com/ef/core/providers/sqlite/limitations · https://learn.microsoft.com/dotnet/api/system.threading.periodictimer.waitfornexttickasync · https://learn.microsoft.com/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task · https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services#consuming-a-scoped-service-in-a-background-task
- Sonuç (D-031):
  - Outbox satırlarını `SavingChanges`'teki interceptor yazar.
  - Anahtar `bigint identity`, `EventId` unique index. Dağıtım sırası ve keyset `Id` üzerinden.
  - Processed tablosu (`outbox_consumers`) anahtarı (EventId, handler). Garanti at-least-once, handler'lar idempotent yazılır.
  - Dispatcher açık transaction kullanmaz; beklemesi `PeriodicTimer(TimeProvider)`.

### R-069: Arka planda DB'ye dokunan bir servisle SQLite test veritabanını paylaşmak (2026-09-28, faz 4a; kaynak)
- Soru: `NovaCoreApiFactory` her context'e tek bir açık `SqliteConnection` nesnesi veriyor. Host'ta arka planda çalışan outbox dispatcher'ı istek işlemeyle aynı anda bu bağlantıyı kullanabilir mi?
- Bulgu:
  - MS: "the .NET APIs objects are not thread-safe. This means that SqliteConnection, SqliteCommand and SqliteDataReader cannot be shared and used concurrently from multiple threads… simply create and open a new instance of SqliteConnection whenever you need to access the database". Busy/locked hatalarında Microsoft.Data.Sqlite komut zaman aşımına (varsayılan 30 sn) kadar kendiliğinden yeniden dener.
  - Paylaşılabilir in-memory veritabanı: `Data Source=<ad>;Mode=Memory;Cache=Shared`. Aynı adı kullanan bağlantılar aynı veritabanını görür. Veritabanı en az bir bağlantı açık kaldıkça yaşar.
- Kaynak: https://learn.microsoft.com/dotnet/standard/data/sqlite/database-errors · https://learn.microsoft.com/dotnet/standard/data/sqlite/in-memory-databases · https://learn.microsoft.com/dotnet/standard/data/sqlite/connection-strings
- Sonuç (D-031):
  - Test factory'sinin context'leri her işlemde kendi bağlantısını açar, factory başına benzersiz adlı paylaşılabilir in-memory veritabanları kullanılır ve bir bağlantı açık tutulur.
  - Dispatcher testleri de aynı yolu kullanır.

### R-070: Faz 4a code review: outbox'ta `DetectChanges`, durdurmada hata filtresi, kaydı tutulamayan handler (2026-09-28, faz 4a; kaynak + kırmızı test)
- Soru: Review'daki bulgular gerçek mi? Düzeltmeleri güvenli mi?
- Bulgu:
  - `ChangeTracker.Entries()` ve `Entries<TEntity>()` bilerek `DetectChanges` çağırıyor. MS: DetectChanges'ten önce bir navigation koleksiyonuna eklenen yeni entity "does not appear as a tracked entity", sonra "is tracked as Added". Interceptor bunu kapatırsa (review #13), navigation'la eklenen bir entity'nin event'leri outbox'a yazılmadan SaveChanges'in kendi DetectChanges'iyle kaydedilir. Yani ek DetectChanges gerekli. Düzeltilmedi, koda "neden" yorumu eklendi.
  - `BackgroundService.ExecuteTask`, `ExecuteAsync`'in task'ı. Durdurmada `OperationCanceledException` dışında bir hata `ExecuteAsync`'ten kaçarsa task faulted olur ve host bunu hata olarak loglar. Eski filtre `when (!token.IsCancellationRequested)`, durdurma başladıktan sonra her hatayı dışarı bırakıyordu. Kırmızı test: iptalde kendi hatasını atan bir handler dispatcher'ı faulted bıraktı.
  - SQL Server'da `nvarchar(256)` kolona daha uzun bir değer yazmak hata veriyor. Kapalı generic bir handler'ın `FullName`'i tip argümanının assembly-qualified adını (sürüm dahil) taşıdığı için 256'yı kolayca geçiyor. Handler yan etkilerini commit ettikten sonra kaydı yazılamazsa her denemede yeniden çalışırdı. SQLite uzunluk uygulamadığı için test, adın uzunluğunu dispatcher'ın kendi kontrolüyle sınıyor.
- Kaynak: https://learn.microsoft.com/ef/core/change-tracking/change-detection · https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker.entries · https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services#backgroundservice-base-class · https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
- Sonuç:
  - Kaydı tutulamayacak (256 karakterden uzun adlı) handler çalıştırılmıyor, deneme başarısız sayılıyor.
  - Her kayıttan sonra change tracker boşaltılıyor ve ad `finished`'e ekleniyor. Aynı handler tipinin ikinci kaydı atlanıyor.
  - İki catch filtresi yalnızca host'un kendi iptalini dışarı bırakıyor.
  - Belgelerde handler scope'u (mesaj başına, paylaşılan) ve sıra garantisi (yok) düzeltildi.

### R-071: Ortak altyapının dağıtım modeli ve tüketiciye açılan ayarlar (2026-09-28, faz dışı; kaynak)
- Soru: Microsoft ortak host varsayılanlarını nasıl dağıtıyor? Paketle dağıtılan bir kütüphane tüketiciye nasıl ayar noktası sunmalı?
- Bulgu:
  - Aspire ServiceDefaults bir NuGet paketi değil, çözüme eklenen bir proje. İçinde OpenTelemetry (metrik, trace ve log), health, service discovery ve HTTP resilience var. Sayfa kodun "intended as a starting point" olduğunu ve serbestçe değiştirilebileceğini söylüyor.
  - Aspire'ın `MapDefaultEndpoints`'i health uçlarını yalnızca Development'ta açıyor. Gerekçe: bu uçları başka ortamlarda açmanın "security implications" var. D-022 bununla aynı.
  - Kütüphane yazarları için Options rehberi, tüketicinin ayar verebilmesi için `Action<TOptions>` ya da `IConfiguration` alan extension metot overload'ları gösteriyor.
- Kaynak: https://aspire.dev/get-started/csharp-service-defaults/ · https://learn.microsoft.com/dotnet/core/extensions/options-library-authors · https://learn.microsoft.com/dotnet/fundamentals/networking/telemetry/metrics#reuse-service-defaults-project-without-aspire-orchestration
- Sonuç: Bilgi amaçlı. Sorumluluk modelinin gerekçesi: [proposals/tuketici-hazirligi.md](proposals/tuketici-hazirligi.md) §2.

### R-072: Üretimde barındırma: forwarded headers, Data Protection anahtarları, migration dağıtımı (2026-09-28, faz dışı; kaynak)
- Soru: NovaCore ile kurulan bir proje gerçek bir sunucuya çıktığında framework'ün beklediği ama NovaCore'un yapmadığı ne var?
- Bulgu:
  - **Forwarded headers:** IIS out-of-process dışında middleware varsayılan olarak kapalı. Açılınca da `ForwardedHeaders.None` ile başlıyor. Diğer middleware'lerden önce, en geç `UseHsts`'ten önce çalışmalı. Başlıkları yalnızca `KnownProxies`/`KnownNetworks`'teki proxy'lerden kabul ediyor.
  - **Data Protection:** Docker'da anahtarlar kalıcı bir volume'a ya da harici bir sağlayıcıya yazılmalı. Web farm'da varsayılan yapılandırma "isn't generally appropriate". `PersistKeysToDbContext`, Microsoft'un `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` paketinde.
  - **Migration dağıtımı:** EF, otomatik dağıtım için migration bundle, SQL'in gözden geçirilmesi gerekiyorsa idempotent script öneriyor. Runtime migration EF 9'dan beri kilitli ve ödünleşimleriyle kabul edilebilir. Ama gözden geçirme, en az yetki ya da yüksek erişilebilirlik gerekiyorsa ayrı bir dağıtım adımı tercih ediliyor. Şemayı değiştiren hesap uygulamanın çalışma zamanı hesabından ayrı olmalı.
- Kaynak: https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/default-settings?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/host-and-deploy/web-farm?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0 · https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying
- Sonuç: [proposals/tuketici-hazirligi.md](proposals/tuketici-hazirligi.md) E4, E5, E8. Karar bekliyor (K4).

### R-073: Güvenli varsayılanlar ve API sözleşmesi (2026-09-28, faz dışı; kaynak)
- Soru: Kaynaklar, yeni bir projenin varsayılan olarak hangi davranışla başlamasını öneriyor?
- Bulgu:
  - **Fallback policy:** Yetkilendirme meta verisi olmayan bir endpoint FallbackPolicy'ye düşer. FallbackPolicy varsayılan olarak `null`, yani yetki istenmez. MS, varsayılan olarak kimlik doğrulama istemenin tercih edilen yolunun fallback policy olduğunu söylüyor.
  - **Exception handler'lar:** Kayıt sırasıyla çalışır, ilk `true` zinciri keser. `UseExceptionHandler`'ın yedek yolu, yani ProblemDetails yanıtı ya da `StatusCodeSelector`, yalnızca her `TryHandleAsync` `false` dönerse kullanılır.
  - **Yanıt tipleri:** `Produces` endpoint'in davranışını kısıtlamaz. `Results<T1,…>` meta veriyi kendisi taşır ve bildirilmeyen bir yanıt tipini derleme hatasına çevirir.
  - **Exception'lar:** "Do not use throwing or catching exceptions as a means of normal program flow."
  - **Zaman aşımı:** ASP.NET Core varsayılan olarak istekleri zaman aşımına uğratmıyor. Request timeouts middleware'i açıkça eklenmeli.
  - **Nesne düzeyi yetki:** Resource-based authorization `IAuthorizationService.AuthorizeAsync(user, resource, requirement)` ile yapılır.
  - **Entegrasyon testleri:** MS'in önerdiği yol `ConfigureTestServices` içinde bir `TestAuthHandler` ile kimlik doğrulamayı taklit etmek.
  - **Tarayıcı istemcileri:** Public OIDC/OAuth istemcileri web uygulamaları için artık önerilmiyor. Tarayıcı uygulamaları için BFF öneriliyor: token'lar sunucuda kalır, tarayıcıda yalnızca HttpOnly bir cookie olur.
  - **OWASP API Security Top 10 2023:** API1 Broken Object Level Authorization 2019'dan beri birinci sırada. API4 Unrestricted Resource Consumption ve API5 Broken Function Level Authorization da listede.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authorization/policies?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0#iexceptionhandler · https://learn.microsoft.com/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0#minimize-exceptions · https://learn.microsoft.com/aspnet/core/performance/timeouts?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0#mock-authentication · https://learn.microsoft.com/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0#backend-for-frontend-bff-security-architecture · https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0#recommended-approaches-to-create-a-jwt · https://owasp.org/API-Security/editions/2023/en/0x00-toc/
- Sonuç: [proposals/tuketici-hazirligi.md](proposals/tuketici-hazirligi.md) E2, E3, E6, E10, E11, E12, F3, K3. Karar bekliyor (K4).

### R-074: Şablon ve paket dağıtımı (2026-09-28, faz dışı; kaynak)
- Soru: NovaCore bir projeye nasıl ulaştırılmalı?
- Bulgu:
  - .NET şablonları bir template package olarak dağıtılır: `PackageType=Template` ayarlı, şablonların `content` klasöründe durduğu bir paketleme projesi (`dotnet new templatepack`). NovaCore'un bugünkü `template.json`'u bir kaynak klasörü vermediği için repo kökünü kopyalıyor.
  - GitHub Packages'ın NuGet registry'si, paket herkese açık olsa bile indirmek için `read:packages` yetkili bir token ya da `GITHUB_TOKEN` istiyor.
- Kaynak: https://learn.microsoft.com/dotnet/core/tools/templates#template-packages · https://learn.microsoft.com/dotnet/core/tutorials/cli-templates-create-template-package · https://docs.github.com/en/enterprise-cloud@latest/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry
- Sonuç: [proposals/tuketici-hazirligi.md](proposals/tuketici-hazirligi.md) E1, F1, K1. Karar kullanıcının.

## Faz 4b araştırması (2026-09-28)

### R-075: Modüller arası stok düşümü: tek transaction mı, event mi? Eşzamanlı iki satış stoğu nasıl aşmaz? (2026-09-28, faz 4b; kaynak)
- Soru: Payments stoğu kontrol ediyor ama düşmüyor (P1-5). Stok Catalog'un, ödeme Payments'ın DbContext'inde. İki yazım nasıl tutarlı tutulur? Aynı ürünü aynı anda alan iki istek stoğu nasıl aşmaz?
- Bulgu:
  - MS "Domain events": aggregate'ler arası tutarlılık için iki yol var, tek transaction ve nihai tutarlılık. *"both approaches (single atomic transaction and eventual consistency) can be right"*. İkincisi *"requires more complex code in order to detect possible inconsistencies across aggregates and the need to implement compensatory actions"*. Event'leri orijinal transaction'ın parçası olan ek tablolarda saklamak bu telafiyi mümkün kılıyor (outbox).
  - EF Core "Transactions": context'ler arası transaction yalnızca ilişkisel sağlayıcıda kurulabiliyor ve context'lerin aynı `DbConnection` ve `DbTransaction`'ı paylaşmasını gerektiriyor. Retrying execution strategy'de kullanıcının açtığı transaction `CreateExecutionStrategy().ExecuteAsync` içine alınmalı.
  - Azure "Compensating Transaction": telafinin kendisi de başarısız olabilir. Adımlar idempotent olmalı, ilerleme güvenilir biçimde kaydedilmeli.
  - EF Core "Handling Concurrency Conflicts":
    - Optimistic concurrency: belirteç `UPDATE`/`DELETE`'in `WHERE` koşuluna girer, çakışmada `DbUpdateConcurrencyException` atılır.
    - `rowversion` SQL Server'a özgü; bazı veritabanları bunu *"don't support these at all (e.g. SQLite)"*. Uygulama yönetimli belirteç her sağlayıcıda çalışıyor.
    - Otomatik bir güncelleme çakışmada *"can simply loop and retry immediately, after re-querying the data"*. Kullanıcıya dönen yolda seçenek: *"inform the user that the update failed due to conflicting changes"*.
  - Microsoft'un eShop örneğinde Catalog, siparişin stok doğrulama isteğine `OrderStockConfirmedIntegrationEvent` ya da `OrderStockRejectedIntegrationEvent` ile cevap veriyor.
- Kaynak: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation · https://learn.microsoft.com/ef/core/saving/transactions#cross-context-transaction · https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions · https://learn.microsoft.com/azure/architecture/patterns/compensating-transaction · https://learn.microsoft.com/ef/core/saving/concurrency · https://github.com/dotnet/eShop/blob/main/src/Catalog.API/IntegrationEvents/EventHandling/OrderStatusChangedToAwaitingValidationIntegrationEventHandler.cs
- Sonuç: Q-30'da kullanıcı event akışını seçti (D-034).
  - Ortak bağlantı ve transaction seçenek yapılmadı: modülleri tek veritabanına bağlar ve Catalog.Contracts'a EF sızdırır (`IProductCatalog.cs:4-5`).
  - Senkron contract iki commit arasında stok kaybı bırakırdı ve outbox'ı yine hiçbir modül kullanmazdı.
  - Uygulama kararları D-035'te: `Stock` uygulama yönetimli concurrency token, handler'daki çakışma dispatcher'ın yeniden denemesine bırakılıyor, uç noktalarda 409.

## Faz 4c araştırması (2026-10-20)

### R-076: Fallback policy hangi istekleri kapsar? Anonim kalması gereken uçlar hangileri? (2026-10-20, faz 4c; kaynak + kırmızı test)
- Soru: `FallbackPolicy = RequireAuthenticatedUser` hangi isteklere uygulanır? Kimlik eklentisinin OpenIddict uçları ve Development uçları (health, OpenAPI, Scalar) nasıl etkilenir? Boş bir CORS policy'si geçerli mi?
- Bulgu:
  - MS: fallback policy, yetki meta verisi olmayan ve yetkilendirme middleware'inden geçen isteklere uygulanır; *"A request that doesn't match an endpoint uses the fallback policy if the authorization middleware runs for the request."* Açık uçlar `AllowAnonymous` alır; MS bunu varsayılan olarak kimlik doğrulama istemenin tercih edilen yolu sayıyor.
  - OpenIddict'in ASP.NET Core sunucusu bir `IAuthenticationRequestHandler`. Discovery, JWKS ve revocation isteklerini authentication middleware'inde kendisi yanıtlar, yetkilendirmeye hiç ulaşmazlar. Passthrough uçları (`/connect/authorize`, `/token`, `/endsession`) ise isteği doğruladıktan sonra endpoint'e bırakır. Bu yüzden onlar `AllowAnonymous` ister.
  - Kırmızı test: `AllowAnonymous` kaldırılınca kimlik akış testlerinden 11'i kırmızıya döndü (commit'lenmedi).
  - `CorsPolicyBuilder.Build()` origin'siz bir policy'yi kabul eder; o policy hiçbir origin'e `Access-Control-Allow-Origin` vermez (test: Production'da basit istek ve preflight).
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authorization/policies?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/security/authorization/secure-data?view=aspnetcore-10.0#secure-user-data · https://learn.microsoft.com/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0#static-file-authorization
- Sonuç (D-036 (1)):
  - Fallback policy `AddCoreAuthorization`'da kurulur.
  - `AllowAnonymous` alan uçlar: `/connect/*` passthrough uçları, `/account` grubu, health, OpenAPI belgesi, Scalar.
  - Tanımsız bir yola anonim istek artık 404 yerine 401 alır. Health'in Production testi bu yüzden kimlikli bir client'la 404'ü doğruluyor.

### R-077: Super-admin'i sunucu tarafına taşımanın yolu (2026-10-20, faz 4c; kod + kırmızı test)
- Soru: `PermissionAuthorizationHandler` `Administrator` rolünü token'daki `role` claim'inden okuyor; rolünden çıkarılan bir admin token'ın ömrü boyunca her kontrolü geçiyor. Bypass sunucuda çözülen bilgiye nasıl dayanır, `IPermissionResolver` değişmeli mi?
- Bulgu:
  - Kod: seed (`IdentityServerSeeder.SeedRolesAsync`) `Administrator` rolüne o anda tanımlı bütün izinleri claim olarak yazıyor ve yalnızca `SeedOnStartup` açıkken çalışıyor. Resolver rolün claim'lerini zaten okuyor, yani seed'den sonra admin izinlerini token rolü olmadan da alıyor. Bypass yalnızca seed'den sonra tanımlanan izinler ve token rolü için fark yaratıyor.
  - MS policy rehberi bir handler'ın birden çok koşulla `Succeed` edebileceğini gösteriyor, ama super-admin için bir kalıp önermiyor. Konu bir tasarım seçimi.
  - Seçenekler:
    - (a) Bypass'ı silip yalnızca seed'e güvenmek: en basit yol, ama seed kapalıyken yeni bir modülün izinleri admin'e ulaşmaz.
    - (b) Resolver'ın `Administrator` üyeliğini tanımlı bütün izinlere (`IPermissionDefinitionProvider`) çözmesi: çekirdekte rol kavramı kalmaz, `IPermissionResolver` aynı kalır.
    - (c) Çekirdekte `"*"` gibi bir işaret izni: tanımsız izinleri de kapsar, ama `ICurrentUser.Permissions`'a ve `/me` yanıtına sihirli bir değer sokar ve public API'ye sabit ekler.
  - Kırmızı testler: token'da `role: Administrator` olan ama sunucuda izni olmayan çağrı 200 aldı; seed'in role yazmadığı bir izin admin'e çözülmedi; rolden çıkarılan admin önbellek süresinden sonra da 200 aldı.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/authorization/policies?view=aspnetcore-10.0 · kod: `IdentityServerSeeder.cs`, `PermissionAuthorization.cs`
- Sonuç (D-036 (2)): (b). Çekirdekte super-admin yok, `HttpContextCurrentUser.SuperAdminRole` kalkar. Tanımlanmamış bir izin admin'e de verilmez.

### R-078: .NET 10'da forwarded headers'ın varsayılanları ve config'ten yapılandırılması (2026-10-20, faz 4c; kaynak + kırmızı test)
- Soru: `UseForwardedHeaders` neyi varsayılan olarak işler, hangi proxy'lere güvenir? Güvenilen liste config'ten nasıl verilir, boş liste ne yapmalı?
- Bulgu:
  - MS:
    - `ForwardedHeaders` varsayılanı `None`'dır; işlenecek başlıklar açıkça seçilmelidir.
    - Middleware başlıkları yalnızca `KnownProxies` ve `KnownIPNetworks`'teki kaynaklardan kabul eder. Varsayılan liste loopback'tir.
    - `ForwardLimit` 1 kalmalıdır.
    - Middleware, şemaya, host'a ya da istemci IP'sine bakan her şeyden önce çalışmalıdır. Edge'de açılırsa istemci kimliğini taklit edebilir.
  - .NET 10: `KnownNetworks` ve `Microsoft.AspNetCore.HttpOverrides.IPNetwork` obsolete (ASPDEPR005); yerine `KnownIPNetworks` ve `System.Net.IPNetwork`.
  - `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` middleware'i kendisi ekler ve listeleri temizler (her kaynağa güvenir). Boş config'te options'a dokunmamak bu yolu bozmaz.
  - `IPAddress.TryParse("10.0.0")` başarılı olur ve `10.0.0.0` döner (eski kısaltma biçimi). `System.Net.IPNetwork.TryParse` önek olmadan ya da host bitleri set edilmiş bir tabanla başarısız olur.
  - Kırmızı testler: güvenilen proxy'nin başlıkları ve limiter bölümü uygulanmıyordu; geçersiz girişler açılışı durdurmuyordu. Dolu listede loopback'ın güvenilmediği de testte.
- Kaynak: https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/security-considerations?view=aspnetcore-10.0#security-middleware · https://learn.microsoft.com/aspnet/core/breaking-changes/10/ipnetwork-knownnetworks-obsolete?view=aspnetcore-10.0 · https://learn.microsoft.com/dotnet/api/system.net.ipaddress.tryparse
- Sonuç (D-036 (3)):
  - Bölüm `ForwardedHeaders`, anahtarlar `KnownProxies` ve `KnownIPNetworks`.
  - Yalnızca For ve Proto uygulanır. Liste doluysa framework'ün loopback varsayılanları temizlenir; boşsa options'a dokunulmaz.
  - IPv4 adresi dört bölümüyle yazılmalıdır.

### R-079: Exception handler middleware'in sırası, yedek yolu ve diagnostics'i (2026-10-20, faz 4c; kaynak + kırmızı test)
- Soru: Çekirdeğin eşlemesi `IExceptionHandler` olmadan nasıl çalışır? Projenin handler'ı nasıl her zaman önce çalışır? Log ve iptal davranışı ne olur?
- Bulgu:
  - Kaynak (`ExceptionHandlerMiddlewareImpl.HandleException`, release/10.0), işlem sırası:
    1. `OperationCanceledException` ya da `IOException` gelmişse ve `RequestAborted` iptal edilmişse, middleware `RequestAbortedException` logu yazar, durumu 499 yapar ve **handler'lar çalışmadan döner**. Yani eski `GlobalExceptionHandler`'ın 499 eşlemesine yalnızca sunucunun kendi iptalleri ulaşıyordu.
    2. `Response.StatusCode = StatusCodeSelector?.Invoke(exception) ?? 500`.
    3. Kayıtlı `IExceptionHandler`'lar sırayla denenir; `true` dönen ilk handler zinciri keser.
    4. Hiçbiri işlemezse `IProblemDetailsService.TryWriteAsync` çağrılır. Context'te `Exception` ve `ProblemDetails.Status = Response.StatusCode` bulunur, yani `CustomizeProblemDetails` exception'ı görür.
  - Diagnostics: `SuppressDiagnosticsCallback` null ise yalnızca `ExceptionHandledBy == ExceptionHandlerService` bastırılır. Callback yalnızca middleware'in işlediği exception'larda çağrılır. Bastırılanlar: `UnhandledException` Error logu, `HandledException` EventSource olayı, `error.type` metrik etiketi.
  - `BadHttpRequestException` bir `IOException`'dır, ama istek iptal edilmediyse 1. adıma takılmaz.
  - `ProblemHttpResult` da yanıtını `IProblemDetailsService` üzerinden yazar; o yolda `Exception` null'dır.
  - Kırmızı testler (`ExceptionMappingTests`): `AddCoreWeb`'den sonra kaydedilen handler çalışmıyordu, dört framework exception'ı ve sunucu iptali 4xx/499 alıyordu, bozuk JSON gövdesi 500 dönüyordu. Kasıtlı bozma: bastırma kapatılınca log testi kırmızı (commit'lenmedi).
- Kaynak: https://raw.githubusercontent.com/dotnet/aspnetcore/release/10.0/src/Middleware/Diagnostics/src/ExceptionHandler/ExceptionHandlerMiddlewareImpl.cs · https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0#iexceptionhandler · https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.exceptionhandleroptions.suppressdiagnosticscallback?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/breaking-changes/10/exception-handler-diagnostics-suppressed?view=aspnetcore-10.0
- Sonuç (D-036 (4)):
  - Eşleme `StatusCodeSelector`, `CustomizeProblemDetails` ve `SuppressDiagnosticsCallback` ile yapılır. `GlobalExceptionHandler` silinir.
  - `OperationCanceledException` özel bir durum almaz.
  - 500'leri framework loglar. İstemci hataları loglanmaz. Projenin handler'larının işlediği exception'lar, framework varsayılanı gibi bastırılır.

### R-080: Tipli yanıtlar ve OpenAPI meta verisi (2026-10-20, faz 4c; kaynak + ölçüm)
- Soru: `Result`'tan HTTP'ye dönüşüm başarı tipini handler imzasında nasıl taşır? Hata yanıtları için meta veri nereden gelir? OpenAPI belgesi değişir mi?
- Bulgu:
  - MS:
    - Yalnızca `IEndpointMetadataProvider` uygulayan dönüş tipleri belgede bir `responses` girişi üretir.
    - `Results<T1,…>` union'ı, tip argümanlarından meta veri sağlayanların meta verisini ekler. Implicit dönüşümleri sayesinde bildirilmeyen bir sonuç derleme hatası verir.
    - `Produces` yalnızca meta veridir, davranışı kısıtlamaz.
  - Kaynak (`ProblemHttpResult`, release/10.0): `PopulateMetadata` yalnızca `DisableCookieRedirectMetadata` ekler, yanıt tipi eklemez. `ExecuteAsync` yanıtı `IProblemDetailsService` üzerinden yazar. Yani `.ProducesProblem(...)` gerekir ve `traceId`/`instance` özelleştirmesi aynen uygulanır.
  - `TypedResults.ValidationProblem`, `Ok<T>`, `NoContent`, `Created<T>`, `NotFound<T>`, `Conflict<T>`, `BadRequest<T>`, `UnauthorizedHttpResult` meta veri sağlar. `Results.X` aynı tipleri `IResult` olarak döndürür, bu yüzden gövdeler değişmez.
  - Ölçüm: belge adımdan önce ve sonra alındı.
    - 7 modül ucunun `responses` girişleri bayt bayt aynı. Silinen `.Produces<T>()` satırlarının yerini union tuttu.
    - 13 kimlik ucu şemasız `200`'den tipli yanıtlara geçti; örneğin `logout-all` artık gerçekteki `204`'ü bildiriyor.
    - Hiçbir şema silinmedi; 9 şema eklendi. Belgenin `responses` dışındaki kısımları aynı.
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0 · https://raw.githubusercontent.com/dotnet/aspnetcore/release/10.0/src/Http/Http.Results/src/ProblemHttpResult.cs
- Sonuç (D-036 (5)): `ToHttpResult` union döner, `(Result, int)` overload'u silinir. Eklentinin JSON uçları tipli sonuç döner; protokol ve HTML uçları `IResult` kalır. `EndpointContractTests` belgede görünen handler'ların düz `IResult` dönmediğini doğrular.

### R-081: Data Protection anahtar deposu açıkça seçilmiş mi, nasıl anlaşılır? (2026-10-20, faz 4c; kaynak + kırmızı test)
- Soru: Kimlik eklentisi, host'un bir anahtar deposu seçip seçmediğini nasıl anlar? Kontrol hangi mekanizmayla yapılmalı?
- Bulgu:
  - MS:
    - Uygulama ortamını tanımaya çalışır: Azure App Service'te `%HOME%\ASP.NET\DataProtection-Keys`, kullanıcı profili varsa `%LOCALAPPDATA%` ya da `$HOME/.aspnet`, IIS'te registry kullanılır. Hiçbiri yoksa anahtarlar süreç bitince kaybolur.
    - Docker'da anahtarlar kalıcı bir volume'a ya da harici bir sağlayıcıya yazılmalıdır. Web farm'da varsayılan yapılandırma uygun değildir.
  - MS (extensibility): uygulama genelindeki depo `KeyManagementOptions.XmlRepository` ile değiştirilir. `PersistKeysToFileSystem`, `PersistKeysToDbContext`, Azure Blob ve Redis bu özelliği ayarlar. Varsayılan sezgisel seçim ise options'a yazılmaz, `XmlKeyManager` onu kendi içinde yapar. Yani `XmlRepository == null` "host bir depo seçmedi" demektir.
  - Kırmızı test: Production'da anahtarlar ve e-posta göndericisi varken, depo olmadan host açılıyordu.
  - Yan etki: imza anahtarı eksikliğini bekleyen mevcut test artık önce depo hatasını görüyordu. Teste depo eklendi; amacı aynı kaldı.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/default-settings?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/security/data-protection/extensibility/key-management?view=aspnetcore-10.0#ixmlrepository · https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/host-and-deploy/web-farm?view=aspnetcore-10.0
- Sonuç (D-036 (6)):
  - Eklenti `KeyManagementOptions`'ı `Validate<IHostEnvironment>` + `ValidateOnStart` ile doğrular: Development ya da `XmlRepository != null`.
  - Azure App Service'in otomatik `%HOME%` deposu da açık sayılmaz; host onu tek satırla (`PersistKeysToFileSystem`) belirtir.
  - Depo paketi eklenmez (`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`); depoyu host seçer.

### R-082: Outbox tüketici anahtarı, saklama süresi ve migration'ın veri dönüşümü (2026-10-20, faz 4c; kaynak + ölçüm)
- Soru: `outbox_consumers` mesaj satırına nasıl bağlanır? İşlenmiş mesajlar ne zaman ve nasıl silinir (SQLite testleri dahil)? Migration mevcut satırları güvenle nasıl dönüştürür?
- Bulgu:
  - Anahtar: R-068'deki SQL Server index rehberi (dar, benzersiz, sürekli artan clustered anahtar) tüketici tablosu için de geçerli. (`bigint` mesaj `Id`'si, Handler) bunu sağlıyor; (`uniqueidentifier`, `nvarchar(256)`) sağlamıyordu. FK'nin cascade delete'i, toplu silmede kayıtları veritabanının silmesini sağlar.
  - Saklama:
    - MS Idempotent Consumer, işlenmiş kayıtların tekrar teslim penceresinden uzun tutulmasını öneriyor (R-068).
    - Burada bir mesaj yalnızca outbox'ta bekledikçe yeniden teslim edilir (10 deneme × 5 sn ≈ 50 sn); süre inceleme içindir. 7 gün seçildi.
    - SQLite `DateTimeOffset` karşılaştırmasını SQL'e çeviremediği için (R-068) süre bellekte karşılaştırılır. En eski işlenmiş mesajlar `Id` sırasıyla 50'şer okunur, süresi dolanlar `ExecuteDelete` ile silinir.
  - Ölçüm (FakeTimeProvider): saati 7 gün ilerletmek, 5 sn periyotlu `PeriodicTimer`'ı her periyot için bir kez tetikledi; iki test yaklaşık 55'er sn sürdü. Testler artık satırları zaman damgasıyla yaşlandırıyor ve 50–110 ms sürüyor.
  - Migration:
    - EF'in ürettiği adımlar (`DropColumn EventId` + `AddColumn MessageId default 0`) mevcut kayıtları kaybettirirdi.
    - Elle yazılan adımlar: nullable kolon ekle → `UPDATE … JOIN` ile doldur → sahipsiz kaydı sil → PK ve `EventId`'yi kaldır → `NOT NULL` → yeni PK ve FK.
    - `dotnet ef migrations script` migration'ı tek batch olarak üretiyor. Yeni kolonu kullanan ifade aynı batch'te derlenirse SQL Server "Invalid column name" verir; bu yüzden ifadeler `EXEC(N'…')` içindedir.
    - Yerel SQL Server'da geçici bir veritabanıyla denendi:
      - Script ile Up: iki kayıt dönüştü, sahipsiz kayıt silindi, mesajın silinmesi kaydını cascade ile götürdü.
      - Çalışma zamanında Down `EventId`'yi geri getirdi, tekrar Up başarılı oldu.
      - Payments'ın bütün zinciri boş şemada uygulandı. Veritabanı sonra silindi.
- Kaynak: https://learn.microsoft.com/sql/relational-databases/sql-server-index-design-guide#clustered-index-design-guidelines · https://learn.microsoft.com/azure/architecture/patterns/idempotent-consumer · https://learn.microsoft.com/ef/core/providers/sqlite/limitations · https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing#customize-migration-code · https://learn.microsoft.com/sql/t-sql/language-elements/execute-transact-sql
- Sonuç (D-036 (7)).

### R-083: SqlClient'ın getirdiği Pkcs açığı (2026-10-20, faz 4c doğrulaması; kaynak + ölçüm)
- Soru: Faz doğrulamasında `restore --force` NU1903 verdi. Açık nereden geliyor, nasıl kapatılır?
- Bulgu:
  - GitHub Advisory: GHSA-846f-f764-76mx (CVE-2026-84008) ve GHSA-2fmv-rv2c-59x7 (CVE-2026-70162), 2026-10-13, .NET DoS. `System.Security.Cryptography.Pkcs` 9.0.0–9.0.20 ve 10.0.0–10.0.12 açıklı; düzeltmeler 9.0.21 ve 10.0.13.
  - `dotnet nuget why`: EF Core SqlServer 10.0.12 → Microsoft.Data.SqlClient 6.1.6 → Pkcs 9.0.11. EF Core 10.0.13 SqlClient 6.1.7'ye bağlı, o da hâlâ Pkcs 9.0.11'e. SqlClient 7.1.1 net9.0 için Pkcs 9.0.18'e bağlı. Yani üst paketi güncellemek açığı kapatmıyor.
  - Hiçbir packable projenin grafiğinde Pkcs yok; yalnızca Catalog, Payments, Api ve testler.
  - NuGet (R-014): üst paket açığı kapatmıyorsa sıradaki yol doğrudan referans ya da CPM transitive pinning. Transitive pinning bütün projelerde açılır ve packable paketlerin nuspec bağımlılıklarını değiştirebilir.
- Kaynak: https://github.com/advisories/GHSA-846f-f764-76mx · https://github.com/advisories/GHSA-2fmv-rv2c-59x7 · https://learn.microsoft.com/nuget/concepts/auditing-packages · https://learn.microsoft.com/nuget/consume-packages/central-package-management#transitive-pinning
- Sonuç (D-036 (8)): Catalog ve Payments, Pkcs 10.0.13'e doğrudan referans verir (CPM'de sürüm). Api ve testler onu proje referansından en yakın sürüm olarak alır. `list package --vulnerable` 13 projede temiz.

### R-084: Parola sıfırlama ve onay e-postasını yeniden gönderme: hesabın varlığını ele vermeden (2026-10-20, faz 4d; kaynak + kırmızı test)
- Soru: Sıfırlama ve yeniden gönderme uçları nasıl çalışmalı ki yanıt hesabın varlığını ele vermesin? Sıfırlamadan sonra ne olmalı? Link mi, kod mu?
- Bulgu:
  - ASP.NET Core `MapIdentityApi` (release/10.0, `IdentityApiEndpointRouteBuilderExtensions.cs`):
    - `/forgotPassword` yalnızca kullanıcı var ve e-postası onaylıysa token üretip gönderir, her durumda `Ok` döner. Kod yorumu: "Don't reveal that the user does not exist or is not confirmed".
    - `/resetPassword` bilinmeyen ya da onaysız kullanıcıya bozuk token'la aynı `InvalidToken` hatasını verir. Token Base64Url'dir. Lockout'a dokunmaz.
    - `/resendConfirmationEmail` bilinmeyen adrese `Ok` döner, bulunan her kullanıcıya (onaylı olsa da) link gönderir.
  - `UserManager.ResetPasswordAsync` token'ı (stamp dahil) doğrular, sonra parola politikasını uygular ve security stamp'i yeniler. Aynı token ikinci kez doğrulanmaz.
  - MS Learn (Account confirmation and password recovery): ForgotPassword "fails silently if the user email has not been confirmed"; parola sıfırlama e-postadaki linkle açılan bir sayfada yapılır. Blazor WASM rehberinde onay e-postasını yeniden gönderme linki istemcinin kayıt ekranında.
  - OWASP Forgot Password Cheat Sheet:
    - Var olan ve olmayan hesap için aynı mesaj ve tutarlı yanıt süresi.
    - Token rastgele, tek kullanımlık ve süreli.
    - Başarılı sıfırlamadan sonra otomatik giriş yok; oturumlar iptal edilebilir.
    - Yeni parola iki kez girilir.
    - Sayfada `Referrer-Policy: no-referrer` (çekirdek zaten her yanıtta gönderiyor).
    - Token gösterilmeden hesapta değişiklik yapılmaz (ör. kilitleme).
  - Kırmızı test: seed kapalıyken (`User` rolü yok) `register` hesabı oluşturup `AddToRoleAsync`'te 500 veriyordu, onay e-postası da gitmiyordu.
- Kaynak: https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs · https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Identity/Extensions.Core/src/UserManager.cs · https://learn.microsoft.com/aspnet/core/security/authentication/accconfirm · https://learn.microsoft.com/aspnet/core/blazor/security/webassembly/standalone-with-identity/account-confirmation-and-password-recovery · https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html
- Sonuç (D-038 (1)–(4)):
  - Sıfırlama sağlayıcının HTML sayfalarıyla yapılır: kullanıcı parolasını giriş sayfasında unutur ve link bir sayfa açmalıdır.
  - Yeniden gönderme kayıt gibi bir JSON uçtur ve yalnızca onaysız hesaba e-posta yollar.
  - Sıfırlama oturumları iptal eder, otomatik giriş yapmaz.
  - Kalan fark: bilinen hesapta e-posta gönderimi yanıtı uzatabilir (PROGRESS → Sonraya bırakılanlar).

### R-085: Kurtarma koduyla giriş ve lockout (2026-10-20, faz 4d; kaynak + kırmızı test)
- Soru: Giriş sayfasının 2FA adımı kurtarma kodunu nasıl kabul eder? Kod tek kullanımlık mı? Hatalı kod ve kilitli hesap nasıl ele alınır?
- Bulgu:
  - `SignInManager.TwoFactorRecoveryCodeSignInAsync` (release/10.0) iki adım yapıyor: bekleyen 2FA kullanıcısını cookie'den alıyor ve `RedeemTwoFactorRecoveryCodeAsync`'i çağırıyor.
    - `PreSignInCheck` ya da `IsLockedOut` kontrolü yok.
    - Hatalı kodda `AccessFailedAsync` çağrılmıyor. Kaynaktaki yorum: "We don't protect against brute force attacks since codes are expected to be random."
    - Başarıda lockout sayacı sıfırlanıyor.
  - `TwoFactorAuthenticatorSignInAsync` ise önce `PreSignInCheck` yapıyor (lockout), hatalı kodda `AccessFailedAsync` çağırıyor.
  - `UserStoreBase.RedeemCodeAsync` (MS API dokümanı): "recovery codes are only valid once, and will be invalid after use."
  - `UserManager.CreateTwoFactorRecoveryCode`: `XXXXX-XXXXX`, alfabe `23456789BCDFGHJKMNPQRTVWXY` (yalnızca büyük harf ve rakam, ~47 bit). Altı haneli bir authenticator koduyla karışmaz. `RedeemTwoFactorRecoveryCodeAsync` büyük/küçük harfi normalize etmiyor.
  - Sonuç olarak, olduğu gibi kullanılırsa kilitli bir hesap kurtarma koduyla girebilirdi ve eklentinin "hatalı 2FA kodu sayılır" kuralı kurtarma kodunda geçerli olmazdı.
- Kaynak: https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Identity/Core/src/SignInManager.cs · https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Identity/Extensions.Core/src/UserManager.cs · https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.identity.userstorebase-5.redeemcodeasync · https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.identity.signinmanager-1.twofactorrecoverycodesigninasync
- Sonuç (D-038 (5)): 2FA adımı tek alan. Yalnızca rakam authenticator koduna gider (code review'dan sonra; önce "tam altı hane" idi ve yanlış uzunluktaki kod "kurtarma kodu geçersiz" mesajı alıyordu), gerisi kurtarma koduna. Kurtarma kodundan önce `IsLockedOutAsync`, hatalı kodda `AccessFailedAsync` eklenti tarafında yapılır. Kod büyük harfe çevrilir. Kullanılan kod `recovery_code_used` (Warning) olarak loglanır.

### R-086: Sağlayıcıdan çıkışta onay (logout CSRF) (2026-10-20, faz 4d; kaynak + kırmızı test)
- Soru: `/connect/endsession` herhangi bir GET'le oturumu kapatıyordu. Spec ne istiyor, OpenIddict nasıl öneriyor?
- Bulgu:
  - OpenID Connect RP-Initiated Logout 1.0 §2:
    - OP kullanıcıya OP'den de çıkmak isteyip istemediğini "SHOULD" sormalı.
    - `id_token_hint` yoksa (ya da OP oturumuna ait değilse) bu soru "MUST".
    - OP logout ucunda GET ve POST'u desteklemeli.
  - §4: Oturum açık değilken çıkış istemek hata değildir.
  - §6: `id_token_hint`'siz istekler bir hizmet reddi yoludur.
  - OpenIddict'in Velusia örneği:
    - GET bir onay sayfası gösterir.
    - Sayfanın formu isteğin bütün parametrelerini (form ya da query) hidden input olarak aynı uca POST eder.
    - POST `[ValidateAntiForgeryToken]` ile korunur, `SignInManager.SignOutAsync` ve OpenIddict şemasıyla `SignOut` yapar.
    - OpenIddict, client'ın kayıtlı `post_logout_redirect_uri`'sine yönlendirir.
  - `IAntiforgery.IsRequestValidAsync` güvenli metotlarda (GET dahil) true döner; POST ayrıca kontrol edilmelidir.
  - Kırmızı test: oturumlu tarayıcının GET'i ve antiforgery'siz POST'u oturumu kapatıyordu.
- Kaynak: https://openid.net/specs/openid-connect-rpinitiated-1_0.html · https://github.com/openiddict/openiddict-samples/blob/dev/samples/Velusia/Velusia.Server/Controllers/AuthorizationController.cs · https://github.com/openiddict/openiddict-samples/blob/dev/samples/Velusia/Velusia.Server/Views/Authorization/Logout.cshtml · https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.antiforgery.iantiforgery.isrequestvalidasync
- Sonuç (D-038 (6)):
  - Oturumlu tarayıcıya her durumda soru sorulur. `id_token_hint` doğrulanmaz; spec'e uyuyor ve daha az kod.
  - Yalnızca geçerli antiforgery token'lı POST çıkış yapar.
  - Oturumsuz tarayıcı doğrudan yönlendirilir.

### R-087: Giriş sayfasında yanıt süresi ve `change-password`'da lockout (2026-10-20, faz 4d; kaynak + kırmızı test)
- Soru:
  - Bilinmeyen e-posta neden daha hızlı dönüyor ve nasıl eşitlenir?
  - `change-password` lockout'a neden bakmıyor, nasıl bağlanır?
- Bulgu:
  - Zamanlama:
    - `SignInManager.PasswordSignInAsync(string, ...)` bilinmeyen kullanıcıda hemen `Failed` döner, hash hesaplamaz. `UserManager.CheckPasswordAsync` de null kullanıcıda hash'lemez.
    - Bilinen kullanıcıda PBKDF2 600.000 iterasyon harcanır.
    - OWASP Authentication Cheat Sheet "quick exit" örneğini yanlış gösteriyor ve kullanıcı ne olursa olsun aynı işlemin yapılmasını öneriyor.
  - Hasher kaydı: `IPasswordHasher<TUser>` `AddIdentityCore`'da `TryAddScoped`'tır. Host kendi hasher'ını önce kaydederse o kullanılır, bu yüzden sahte hash de onunla üretilmeli. Singleton tutucu hasher'ı her çağrıda alır (Y-26). Static alan kullanılmaz (Y-24).
  - Lockout:
    - `UserManager.ChangePasswordAsync` eşleşmeyen parolada `PasswordMismatch` döner, `AccessFailedAsync` çağırmaz.
    - `SignInManager.CheckPasswordSignInAsync` lockout'u kontrol edip sayardı ama parola iki kez hash'lenirdi: bir kez kontrolde, bir kez `ChangePasswordAsync`'te.
    - OWASP: parola değişimi mevcut parolayı doğrular; brute-force koruması bütün kimlik doğrulama yollarında geçerli.
  - Kırmızı test:
    - Bilinmeyen e-posta hasher'ı hiç çağırmıyordu.
    - 5 yanlış mevcut parola hesabı kilitlemiyordu, ardından doğru parola parolayı değiştiriyordu.
- Kaynak: https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Identity/Core/src/SignInManager.cs · https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Identity/Extensions.Core/src/UserManager.cs · https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Identity/Extensions.Core/src/IdentityServiceCollectionExtensions.cs · https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html
- Sonuç (D-038 (7), (8)):
  - `DummyPasswordHash` singleton'ı, host'un hasher'ıyla bir kez üretilen hash'i tutar. Bilinmeyen e-postada parola ona karşı doğrulanır.
  - `change-password`, giriş sayfasının `CheckUnconfirmedAsync` kalıbını izler:
    - kilitliyse reddeder;
    - `PasswordMismatch`'te `AccessFailedAsync` çağırır;
    - başarıda sayacı sıfırlar.
    Hash tek kez hesaplanır.

### R-088: OpenIddict kayıtlarının temizlenmesi: `PruneAsync`'in anlamı ve OpenIddict.Quartz'ın varsayılanları (2026-10-20, faz 4d; kaynak + kırmızı test)
- Soru: Token ve authorization tabloları neyle, ne sıklıkla ve hangi eşikle temizlenir? Yeni paket olmadan nasıl çalışır?
- Bulgu:
  - `IOpenIddictTokenManager.PruneAsync(threshold)`, EF Core store'unda (7.7.1) şunları siler: `CreationDate < threshold` olan ve durumu `Valid`/`Inactive` dışında olan ya da authorization'ı artık geçerli olmayan ya da süresi dolmuş (`ExpirationDate < DateTime.UtcNow`) token'lar.
    - Süre karşılaştırması sorgunun içinde, yani veritabanının saatiyle yapılıyor. Eşik çağıranın saatinden geliyor.
    - 1.000'lik parçalar halinde `ExecuteDeleteAsync` ile siliyor. Hataları toplayıp sonunda `AggregateException` atıyor.
  - `IOpenIddictAuthorizationManager.PruneAsync(threshold)`: `CreationDate < threshold`, durumu `Valid` değil ya da tipi ad hoc, ve token'ı kalmamış authorization'lar.
    - Geçerli kalıcı authorization hiç silinmiyor. Eklenti kullanıcı ve client başına bir tane yeniden kullanıyor, sayı sınırlı.
    - Dokümanı token'ların önce temizlenmesini söylüyor.
  - OpenIddict.Quartz (7.7.1):
    - `MinimumTokenLifespan` ve `MinimumAuthorizationLifespan` = 14 gün. Eşik `TimeProvider.GetUtcNow() − lifespan`.
    - Önce token'lar, sonra authorization'lar.
    - Saatte bir tetikleniyor, ilk çalışma açılıştan 1–9 dk sonra. Yorumdaki gerekçe: uygulama bir saat dolmadan yeniden başlarsa da bir kez çalışsın.
  - OpenIddict `TimeProvider`'ı DI'dan alıyor (`OpenIddictCoreOptions.TimeProvider`).
  - Kırmızı test: süresi dolmuş token ve iptal edilmiş authorization bir saat sonra da duruyordu.
  - Test altyapısı: `IdentityServerTestHost` bütün context'lere tek bir `SqliteConnection` veriyordu. Arka plandaki tur istekle aynı anda o bağlantıyı kullanacağı için host, Api factory'si gibi paylaşımlı adlı in-memory veritabanına ve context başına bağlantıya geçti (R-069).
- Kaynak: https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.EntityFrameworkCore/Stores/OpenIddictEntityFrameworkCoreTokenStore.cs · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.EntityFrameworkCore/Stores/OpenIddictEntityFrameworkCoreAuthorizationStore.cs · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Quartz/OpenIddictQuartzJob.cs · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Quartz/OpenIddictQuartzOptions.cs · https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Quartz/OpenIddictQuartzExtensions.cs
- Sonuç (D-038 (9)):
  - Eklentide `ExpiredTokenPruner` (`BackgroundService`). Zamanlayıcı `PeriodicTimer(1 saat, TimeProvider)`, eşik 14 gün, önce token'lar.
  - İlk tur açılışta, `OutboxDispatcher` gibi. Quartz'ın 1–9 dakikalık rastgele gecikmesi birden çok örneği yaymak içindir; burada veritabanı başına tek örnek varsayılıyor (D-031) ve aynı satırları iki kez silmek zararsız.
  - Tur hatası loglanır, host durmaz.
  - Yeni paket yok.


## Faz 5 araştırması (2026-10-20)

### R-089: Şablonun tek log hattı: Serilog mu, OpenTelemetry mi? (2026-10-20, faz 5; kaynak + kırmızı test)
- Soru: PLAN F5.1 "log hattı tektir; hangisi olacağı fazda bir R-kaydıyla seçilir" diyor. Bugün log'lar Serilog'a (console/Seq), metrik ve trace OTLP'ye gidiyor (proposals/tuketici-hazirligi.md F2). Şablon hangisini açmalı ve eklentide ne eksik?
- Bulgu:
  - MS'in .NET observability belgesi ve OTLP örneği log'ları `ILogger` üzerinden OpenTelemetry'ye alıyor (`builder.Logging.AddOpenTelemetry`, `IncludeFormattedMessage` ve `IncludeScopes` açık) ve üç sinyali tek OTLP exporter'la gönderiyor. Aspire ServiceDefaults'un `Extensions.cs`'i de aynısını yapıyor.
  - OpenTelemetry.Exporter.OpenTelemetryProtocol 1.19.1, `UseOtlpExporter` dokümanı: *"Calling this method automatically enables logging, metrics, and tracing."* Yani `NovaCore.OpenTelemetry` endpoint tanımlıyken log'ları zaten OTLP'ye gönderiyordu, ama `OpenTelemetryLoggerOptions` varsayılanlarıyla: biçimlenmiş mesaj ve scope'lar (isteğin scope'u dahil) kayda girmiyordu.
  - OpenTelemetry 1.19.1: `OpenTelemetryBuilder.WithLogging(configureBuilder, configureOptions)` `OpenTelemetry` adlı `ILoggerProvider`'ı kaydediyor ve seçenekleri bağlıyor.
  - Serilog'un `AddSerilog`'u (`writeToProviders: false`) logger factory'yi değiştirdiği için diğer `ILoggerProvider`'lara yazmıyor; Serilog'u kullanan bir host'ta OTel'in log provider'ı boşta kalır, davranış değişmez.
  - Kırmızı test: `AddCoreObservability` sonrası host'ta `OpenTelemetryLoggerProvider` yoktu (endpoint tanımsızken).
- Kaynak: https://learn.microsoft.com/dotnet/core/diagnostics/observability-otlp-example · https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel · https://github.com/microsoft/aspire/blob/main/src/Aspire.ProjectTemplates/templates/aspire-servicedefaults/Extensions.cs · NuGet önbelleğindeki `OpenTelemetry.Exporter.OpenTelemetryProtocol.xml` ve `OpenTelemetry.Extensions.Hosting.xml` (1.19.1)
- Sonuç (D-040 (2)): Şablonun hattı OpenTelemetry; şablon Serilog eklentisini kullanmaz. `AddCoreObservability` log sinyalini açıkça kurar (`WithLogging`, biçimlenmiş mesaj ve scope'lar açık), exporter koşulu aynı. Repo'nun Api host'u Serilog'da kalıyor (eklentinin tek tüketicisi). Daha basit alternatif, şablonun `Program.cs`'inin bu ayarı kendisi yapması, aynı satırları her projeye kopyalatırdı.

### R-090: Template package: düzen, sürümün içeriğe yazılması, koşullu içerik ve portlar (2026-10-20, faz 5; kaynak + ölçüm)
- Soru: NovaCore'un `dotnet new` şablonu nasıl paketlenir ki üretilen proje repo'yu değil paketleri kullansın, iki kimlik seçeneği olsun ve sürümü şablon paketininkiyle aynı kalsın?
- Bulgu:
  - MS'in template package eğitimi (`dotnet new templatepack`): `PackageType=Template`, `IncludeContentInPack`, `IncludeBuildOutput=false`, `ContentTargetFolders=content`, `NoDefaultExcludes` (nokta ile başlayan dosyalar için), `<Compile Remove="**\*" />`, şablonlar `content/<şablon>/.template.config/template.json` altında. Örnek `NoWarn NU5128` kullanıyor; NU5128 sayfasının "lib/ref dosyası olmayan paket" çözümü `SuppressDependenciesWhenPacking=true` (bastırma gerekmez). `Microsoft.TemplateEngine.Tasks` yalnızca opsiyonel doğrulama ve yerelleştirme getiriyor.
  - Ölçüm: `Content Include="content\**\*"` + `ContentTargetFolders=content` dosyaları `content/content/...` altına koydu; `PackagePath="content\"` ile `content/novacore/...` oldu (NuGet: sonu ayırıcıyla biten PackagePath'e glob'un göreli yolu eklenir).
  - Sürüm: NuGet pack'in belgelenmiş kancası `TargetsForTfmSpecificContentInPackage`; hedef `TfmSpecificPackageFile` item'ı (`PackagePath` ile) üretir. Şablonun `Directory.Packages.props`'u `NOVACORE_PACKAGE_VERSION` token'ını taşır, hedef `$(PackageVersion)`'ı yazar (`ReadAllText().Replace()` + `WriteLinesToFile`, içerik `[MSBuild]::Escape` ile; yeni paket yok). Ölçüm: `--version-suffix smoke.1` ile paketteki dosyada `0.1.0-smoke.1`.
  - Template engine (dotnet/templating wiki): choice parametresi (`datatype: choice`, `choices`, `defaultValue`), `computed` sembol (`"(auth == \"BuiltIn\")"`), `sources.modifiers` içinde `condition` + `exclude` (yollar şablon köküne göre), `port` generator'ü (`low`/`high`/`fallback`, `replaces` ile metin değişimi). Koşullu sözdizimi: `.cs` `#if`, MSBuild ve `nuget.config` `<!--#if -->`, `.json` `//#if`; `.slnx` listede yok. `sourceName`'in beş biçimi değişir (kimlik, namespace, sınıf adı ve küçük harfli ikisi).
  - Ölçüm: `--debug:custom-hive` ile yalıtılmış kurulum; üretilen BuiltIn ve External projeler TWAE altında derlendi, testleri geçti, `has-pending-model-changes` temiz; portlar üretildi (ör. 7166/5154), JSON yorumları korunuyor.
- Kaynak: https://learn.microsoft.com/dotnet/core/tutorials/cli-templates-create-template-package · https://learn.microsoft.com/dotnet/core/tools/templates · https://learn.microsoft.com/nuget/reference/errors-and-warnings/nu5128 · https://learn.microsoft.com/nuget/reference/msbuild-targets#targetsfortfmspecificcontentinpackage · https://github.com/dotnet/templating/wiki/Reference-for-template.json · https://github.com/dotnet/templating/wiki/Available-Symbols-Generators · https://github.com/dotnet/templating/wiki/Conditional-processing-and-comment-syntax · https://github.com/dotnet/templating/wiki/Naming-and-default-value-forms
- Sonuç: D-040 (3)–(6). `src/Templates/NovaCore.Templates`, `eng/template-smoke.sh`.

### R-091: nuget.org'daki `NovaCore.*` paketleri ve paket ID'si (2026-10-20, faz 5; kaynak + ölçüm)
- Soru: Şablonun ilk kurulumunda `dotnet new` güncelleme kontrolü "NovaCore.Templates 0.3.0-preview.1" önerdi. Bizim paket ID'lerimiz başka yerde var mı, sorun ne?
- Bulgu:
  - nuget.org'da başka bir sahibin `NovaCore.Templates`, `NovaCore.AspNetCore` ve `NovaCore.EntityFrameworkCore` paketleri var (0.3.0-preview.1, "Native ASP.NET Core modular monolith templates with DDD layering"; net8.0, `NovaCore.BuildingBlocks`/`Shared`/`Localization`'a bağlı). `NovaCore.*` önekinde başka sahiplerin paketleri de var (`NovaCore.Agents.*`, `NovaCore.Contracts`); önek kimseye ayrılmamış.
  - Etkisi: sürümsüz `dotnet new install NovaCore.Templates` yabancı şablonu kurar, güncelleme kontrolü onu önerir. nuget.org'u da kaynak olarak gören ve source mapping'i olmayan bir restore aynı ID'nin daha yüksek sürümünü oradan alabilir (bağımlılık karışıklığı).
  - NuGet "Package authoring best practices": *"DO choose a package ID that is unique and clearly differentiated from existing packages on NuGet.org."* MSBuild ile paket oluşturma sayfası: kimlik nuget.org dahil paketin bulunduğu her yerde benzersiz olmalı, şirket adı ilk parça olabilir; ID'nin namespace'le eşleşmesi "helpful". Package source mapping, bir paketin yalnızca eşlenen kaynaktan gelmesini sağlar.
  - Kullanıcı (Q-34): "hiçbir sorun yaşamayacak şekilde, benim ismimi koymadan", önek "BMD ile başlasın". nuget.org'da `BMD.` önekli paket yok (`BMDRM.LibSql.Core` dışında), `BMD.NovaCore.*` ID'leri boş.
  - Ölçüm: `PackageId=BMD.$(MSBuildProjectName)` ile yedi paket + şablon `BMD.NovaCore.*` oldu; paketler arası bağımlılıklar da (`BMD.NovaCore.AspNetCore` → `BMD.NovaCore.Abstractions`), DLL'ler `NovaCore.*` kaldı.
- Kaynak: https://learn.microsoft.com/nuget/create-packages/package-authoring-best-practices#package-id · https://learn.microsoft.com/nuget/create-packages/creating-a-package-msbuild · https://learn.microsoft.com/nuget/consume-packages/package-source-mapping · nuget.org flat container ve arama API'si (2026-10-20)
- Sonuç: Q-34 → D-041. Paket ID'leri `BMD.NovaCore.*`; assembly ve namespace adları değişmez. Üretilen projenin `nuget.config`'i `BMD.NovaCore.*`'ı yalnızca GitHub feed'ine eşler.

### R-092: Üretilen projenin CI'ında migration bundle'ları (2026-10-20, faz 5; kaynak + ölçüm)
- Soru: Şablonun CI'ı migration'ları üretime nasıl taşısın (proposals/tuketici-hazirligi.md E8)?
- Bulgu:
  - EF Core "Applying Migrations": otomatik dağıtım için migration bundle, SQL'in gözden geçirilmesi gerekiyorsa script. Bundle CI'da üretilir, dağıtımda tek seferlik bir iş olarak, veritabanı hazır olduktan sonra çalıştırılır; uygulama imajında SDK ya da `dotnet ef` olmaz, her replika migration çalıştırmaz. Üretirken ve çalıştırırken ortam açıkça verilir (`ASPNETCORE_ENVIRONMENT=Production`), bağlantı dizesi bundle'a gömülmez, `--connection` ile verilir. Şemayı değiştiren hesap uygulamanın hesabından ayrıdır. `--self-contained` .NET runtime'ını da paketler; `--target-runtime` dağıtım hedefinin RID'idir.
  - Ölçüm (üretilen BuiltIn proje, linux-x64): self-contained bundle context başına ~180 MB, framework-dependent ~82 MB (startup projesinin bütün bağımlılıkları tek dosyada). Private bir repo'da her push'ta 2 × 180 MB artifact, GitHub Actions depolama kotasını hızla doldurur.
  - Üretilen workflow'un adımları üretilen projede yerelde çalıştırıldı: restore, build (0 uyarı), test (8/8), iki context için `has-pending-model-changes`, iki bundle, `dotnet list package --vulnerable` (açık yok; `jq` adımı Python eşdeğeriyle). YAML iki kimlik seçeneğinde de ayrıştırılıyor.
- Kaynak: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#bundles · https://learn.microsoft.com/ef/core/cli/dotnet#dotnet-ef-migrations-bundle
- Sonuç (D-040 (8)): bundle'lar context başına, framework-dependent (`--target-runtime linux-x64`), yalnızca varsayılan dala push'ta üretilir ve 7 gün saklanan bir artifact olarak yüklenir. Feed'e erişen adımlar (restore, bundle, açık kontrolü) `NuGetPackageSourceCredentials_novacore`'u secret'tan alır, diğerleri almaz.

## Faz 6a araştırması (2026-10-21)

### R-093: Public repo'da GitHub Packages paketlerinin görünürlüğü ve token gerekliliği (2026-10-21, faz 6a; kaynak + ölçüm)
- Soru: Repo public olunca (D-042) paketler de public mi olur, tüketen projelerin token taşıması gerekir mi (D-039 (1))?
- Bulgu:
  - NuGet kaydı sayfası: "You need an access token to publish, install, and delete private, internal, and public packages." Kimlik doğrulama yalnızca classic PAT ile ("GitHub Packages only supports authentication using a personal access token (classic)").
  - Aynı sayfa: "This registry supports granular permissions"; paketin izinleri repodan bağımsız ayarlanabilir. İlk yayında varsayılan görünürlük private.
  - Erişim ve görünürlük sayfası: bağlı repodan "the access permissions (but not the visibility)" miras alınır. "Once you make a package public, you cannot make it private again."
  - Ölçüm: `https://nuget.pkg.github.com/alimuhammedak/index.json` ve flat container anonim istekte 401. `gh` token'ında `read:packages` olmadığı için paket listesi okunamadı.
- Kaynak: https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry · https://docs.github.com/en/packages/learn-github-packages/configuring-a-packages-access-control-and-visibility
- Sonuç: Repo görünürlüğü paketleri değiştirmez, paket public olsa da tüketici token taşır. Kullanıcı paketleri public yapmayı seçti (Q-35) → D-050. Şablonun `nuget.config`'i ve CI'ı değişmez.

### R-094: Lock dosyaları ve `global.json` `rollForward` (2026-10-21, faz 6a; kaynak)
- Soru: Lock dosyası hangi projelere, locked mode SDK değişince neden kırılır (D-048 (2))?
- Bulgu (MS Learn, "PackageReference in project files" → Locking dependencies):
  - `RestorePackagesWithLockFile=true` proje kökünde `packages.lock.json` üretir; `dotnet restore --locked-mode` (`RestoreLockedMode`) lock dosyasıyla uyuşmayan restore'u durdurur. Lock dosyası bir kez varsa özellik kapalı olsa da kullanılır.
  - "If you are building an application ... do check in the lock file". Paylaşılan kütüphane için "you **should not** check in the lock file": bağımlı projenin restore'u kütüphanenin kilidini kullanmaz.
  - Not: SDK bazı projelere örtük `PackageReference` ekler; başka bir SDK sürümü bu paketlerin sürümünü değiştirip locked mode'u kırar. Önerilen azaltma `global.json` `rollForward: disable`; bütün geliştiriciler ve CI aynı SDK'yı kullanır.
  - .NET 10'da package pruning varsayılan açık; lock dosyası pruning'i hesaba katıyor.
- Kaynak: https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#locking-dependencies · https://learn.microsoft.com/dotnet/core/tools/global-json#rollforward
- Sonuç: Lock dosyası yalnızca packable olmayan projelerde (Api, modüller, Contracts, test); packable kütüphanelerde yok. `IsPackable` csproj'da tanımlı olduğu için ayar `Directory.Build.targets`'tadır (Q-14). Repo'nun `global.json`'ı `disable`; şablonun `global.json`'ı `latestFeature` kalır, çünkü `disable` tam 10.0.300 SDK'sı olmayan her tüketicide şablonu kırardı.

### R-095: Workflow sertleştirme araçları, sürümleri ve pin'leri (2026-10-21, faz 6a; kaynak + ölçüm)
- Soru: Action'lar neye sabitlenir, workflow'lar hangi araçla ve nasıl denetlenir (F6a.2)?
- Bulgu:
  - SHA'lar `gh api repos/<o>/<r>/git/ref/tags/<tag>` ile çözüldü (etiket nesnesiyse `git/tags/<sha>` ile commit'e inildi): `actions/checkout` v7.0.1 `3d3c42e5…`, `actions/setup-dotnet` v6.0.0 `a98b5685…`, `actions/upload-artifact` v7.0.2 `cf430e03…`. Mevcut `@v7`/`@v6`/`@v7` etiketleri aynı commit'leri gösteriyordu, davranış değişmedi.
  - zizmor 1.31.1 (MIT): `unpinned-uses` varsayılan olarak her action'ın hash'e sabitlenmesini ister; `artipacked` `persist-credentials` eksikliğini yakalar. `--strict-collection` sözdizimi ve şema hatasında uyarı yerine hata verir. Girdi `.` verilince şablonun `content/novacore/.github/workflows/ci.yml`'ini de topluyor ve `#if` satırları yüzünden ayrıştıramıyor; girdi `.github` olunca yalnızca repo'nun dosyaları.
  - zizmor-action (MIT, v0.7.1) varsayılan olarak SARIF yükler ve "the action will **not** fail when `zizmor` produces findings"; imajı etiketle çeker. Doğrudan imaj, digest'e sabitlenebiliyor ve bulguda sıfırdan farklı kodla çıkıyor.
  - actionlint 1.7.12 (MIT): belgelenen indirme betiği `main`'i izliyor ve checksum doğrulamıyor; Docker imajı digest'e sabitlenebiliyor (shellcheck dahil).
  - Ölçüm: digest'ler `docker buildx imagetools inspect` ile (index): actionlint `sha256:b1934ee5…`, zizmor `sha256:42d409db…`. Mevcut workflow'larda zizmor 7 `unpinned-uses` (high), 2 `artipacked`, 1 `use-trusted-publishing` (info) buldu; actionlint temizdi.
  - `use-trusted-publishing`: NuGet'in trusted publishing'i nuget.org'a ait (OIDC token → kısa ömürlü API anahtarı). GitHub Packages'a push job'un kendi kısa ömürlü `GITHUB_TOKEN`'ıyla yapılıyor, uzun ömürlü anahtar yok.
- Kaynak: https://docs.zizmor.sh/audits/ · https://docs.zizmor.sh/usage/ · https://github.com/zizmorcore/zizmor-action · https://github.com/rhysd/actionlint/blob/main/docs/usage.md · https://learn.microsoft.com/nuget/nuget-org/trusted-publishing · https://docs.github.com/en/actions/reference/security/secure-use
- Sonuç (D-051 (1)–(3)): SHA + `# vX.Y.Z` yorumu; `eng/lint-workflows.sh` iki aracı digest'e sabit imajlarla çalıştırır, bulguda kırmızıdır; repo'da `workflow-lint` job'u, şablonun workflow'u smoke testinin ürettiği projelerde lint'lenir. `use-trusted-publishing` F6a.2'de push adımında satır içi `zizmor: ignore` ile kapatıldı; F6a.7'de push `id-token: write`'lı ayrı bir job'a taşınınca bulgu üretilmez oldu (ignore'suz kopyayla ölçüldü) ve ignore silindi.

### R-096: Lisans kapısı: nuget-license'ın davranışı ve bugünkü grafik (2026-10-21, faz 6a; kaynak + ölçüm)
- Soru: Y-06 (yalnızca MIT/Apache ya da Microsoft paketleri) CI'da nasıl zorlanır, bugünkü bağımlılık grafiği geçer mi (D-048, F6a.4)?
- Bulgu:
  - nuget-license 4.0.18 (Apache-2.0, net8–net11 tool): `--input` bir proje ya da çözüm alır, `.slnx` çalışıyor (ölçüldü). `--include-transitive` bütün ağacı izler. `--ignored-packages` joker kabul eder; yok sayılan paketin bağımlılıkları yine denetlenir. Restore yapmaz, son restore'un `project.assets.json`'ını okur.
  - Exit kodu belgede yok. Ölçüm: temizken 0, ihlalde ihlal sayısı (3 ve 39 görüldü).
  - Ölçüm (`MIT;Apache-2.0`, Microsoft muafiyetsiz): Polly.Core, Polly.Extensions, Polly.RateLimiting 8.4.2 (BSD-3-Clause; `dotnet nuget why`: NovaCore.AspNetCore → Microsoft.Extensions.Http.Resilience 10.0.0 → Microsoft.Extensions.Resilience → Polly). Ayrıca lisansı dosya olan `Microsoft.Data.SqlClient.SNI.runtime` 6.0.2 ve `Microsoft.Identity.Client.NativeInterop` 0.20.6.
  - nuget.org'da `Microsoft.` ve `System.` önekleri Microsoft'a ayrılmış (ID prefix reservation).
- Kaynak: https://github.com/sensslen/nuget-license · https://learn.microsoft.com/nuget/nuget-org/id-prefix-reservation · `dotnet nuget why NovaCore.slnx Polly.Core`
- Sonuç: Q-39 → D-052. Kapı geçişlileri de denetler, Microsoft önekleri ve `Polly.*` geçer.

### R-097: Betterleaks 1.9.0: tarama komutları, muafiyet ve yapılandırma (2026-10-21, faz 6a; kaynak + ölçüm)
- Soru: Repo'nun geçmişi ve yeni commit'ler sır için nasıl taranır, test kimlik bilgileri nasıl muaf tutulur (D-048, F6a.5)?
- Bulgu:
  - Betterleaks (MIT, gitleaks'in yazarından). Son stabil v1.9.0 (2026-09-29); v2.0.0 RC'de ve SARIF raporunu, yer bazlı ignore'u kaldırıyor. Doğrulama (sağlayıcıya istek) varsayılan olarak kapalı.
  - v1.9.0 komutları: bütün geçmiş `betterleaks git .`, staged değişiklik `betterleaks git . --pre-commit --staged`. Bulguda exit 1. `--redact` değeri loglamaz.
  - Yapılandırma sırası (`--help`): `--config`, ortam değişkenleri, sonra hedef klasördeki `.betterleaks.toml`/`.gitleaks.toml`. Format gitleaks uyumlu: `[extend] useDefault = true` ve `[[allowlists]]` (`condition = "AND"`, `paths`, `regexTarget = "secret"`, `regexes`).
  - Release arşivleri `checksums.txt` ile yayımlanıyor (cosign imzalı): linux_x64 `f8b185a3…6de7`, windows_x64 `b765daec…d3da`. İndirilen dosya bu değerlerle eşleşti.
  - Ölçüm: tam geçmiş 31 bulgu, hepsi `tests/NovaCore.Api.Tests` altında (`generic-password` 28, `generic-api-key` 3), 14 farklı değer; hepsi test sabiti. `.betterleaksignore` parmak izi `commit:path:rule:line` biçiminde, yani commit'e bağlı.
  - Ölçüm: değer listesiyle (`^…$`, `tests/` ile AND) geçmiş temiz. Aynı değer `src/` altında ve yeni bir test değeri yakalanıyor. Staged sahte `ghp_` token'ı hook'ta durdu.
- Kaynak: https://github.com/betterleaks/betterleaks/tree/v1.9.0 (README, docs/scanning.md, docs/config.md, .pre-commit-hooks.yaml) · https://github.com/betterleaks/betterleaks/releases/tag/v1.9.0 · `betterleaks git --help`
- Sonuç: D-051 (4). `.betterleaks.toml` (değer listesi), `.githooks/pre-commit`, `ci.yml` `secret-scan` job'u.

### R-098: Yayın: SBOM ve build provenance (2026-10-21, faz 6a; kaynak + ölçüm)
- Soru: Release paketleri için SBOM ve imzalı köken bilgisi nasıl üretilir (D-048, F6a.7)?
- Bulgu:
  - GitHub "Using artifact attestations": `actions/attest@v4`, `subject-path` ile (glob, en fazla 1024 subject); izinler `id-token: write`, `contents: read`, `attestations: write`. `predicate` ya da `sbom-path` verilmezse build provenance üretir (v4.2.2 `action.yml`). Doğrulama `gh attestation verify <dosya> --repo <owner>/<repo>`. Public repo'da ücretsiz (D-042).
  - Environments: required reviewers ve deployment tag kuralları public repo'larda Free planda var.
  - CycloneDX .NET 6.3.1 (Apache-2.0, `dotnet-CycloneDX`, net8–net10 tool): proje dosyası alır, `--disable-package-restore` ile mevcut restore'u kullanır, `--output-format Json`, `--set-type/--set-name/--set-version`. Ölçüm (`NovaCore.AspNetCore`): `--include-project-references` ile `BMD.NovaCore.Abstractions@0.1.0` (sürüm eki olmadan) ve sahte `NovaCore.Abstractions@1.0.0` bileşenleri çıktı; o seçenek olmadan 25 bileşen, hepsinde purl, `PublicApiAnalyzers` `scope: excluded`. `--recursive` bileşen sayısını değiştirmedi (assets dosyası geçişlileri zaten içeriyor).
- Kaynak: https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations · https://github.com/actions/attest/blob/v4.2.2/action.yml · https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments · https://github.com/CycloneDX/cyclonedx-dotnet
- Sonuç: D-051 (6). Gerçek koşu bir sonraki `v*` tag'iyle; o zamana kadar yalnızca lint ve yerel SBOM denemesiyle doğrulandı.

### R-099: TOTP tek kullanımı ve kurtarma kodlarının saklanması (2026-10-21, faz 6b; kaynak)
- Soru: ASP.NET Core Identity TOTP kodunu tekrar kabul ediyor mu, kurtarma kodlarını nasıl saklıyor; ASVS 6.5.1/6.5.2'ye nasıl uyulur (F6b.1a, F6b.1b)?
- Bulgu:
  - `AuthenticatorTokenProvider<TUser>.ValidateAsync` (v10.0.12) `DateTimeOffset.UtcNow`'dan adımı hesaplar, −2…+2 adımdaki kodları kabul eder ve kabul edileni hiçbir yere yazmaz. Yöntem `virtual`; TOTP hesabı `Rfc6238AuthenticationService` (internal).
  - `UserManager.CreateTwoFactorRecoveryCode` (protected virtual): 10 karakter, alfabe `23456789BCDFGHJKMNPQRTVWXY` (26) → ~47 bit. `UserStoreBase.ReplaceCodesAsync` kodları `;` ile birleştirip düz metin olarak kullanıcı token'ına yazar; `RedeemCodeAsync` düz karşılaştırır. İkisi de `virtual`.
  - `UserManager.SetTwoFactorEnabledAsync` security stamp'i yeniler. `UserStore.UpdateAsync` `ConcurrencyStamp`'ı concurrency token olarak kullanır, çakışmada `ConcurrencyFailure` döner.
  - NIST SP 800-63B: 112 bitten az entropili look-up secret tuzlu ve onaylı bir KDF ile saklanır; ASVS 5.0 6.5.2 aynı.
  - CA5350 "When to suppress": "the level of protection needed for the data does not require a security guarantee" → üretimdeki TOTP için geçerli değil.
- Kaynak: https://github.com/dotnet/aspnetcore/blob/v10.0.12/src/Identity/Extensions.Core/src/AuthenticatorTokenProvider.cs · …/Extensions.Core/src/UserManager.cs · …/Extensions.Stores/src/UserStoreBase.cs · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca5350 · https://pages.nist.gov/800-63-4/sp800-63b/authenticators/ · ASVS 5.0 V6
- Sonuç: D-053 (2), (3).

### R-100: HybridCache tag'leri ile izin önbelleğinin silinmesi (2026-10-21, faz 6b; kaynak)
- Soru: Rol değişikliğinde bir kullanıcının önbellekteki izinleri, security stamp'i bilinmeden nasıl silinir (F6b.2)?
- Bulgu: `GetOrCreateAsync` tag listesi alır; `RemoveByTagAsync(tag)` o tag'li girdileri "bu andan önce oluşturulanları yok say" kuralıyla mantıksal olarak geçersiz kılar, girdiler süreleri dolunca gider. Silme yerel ve dağıtık önbellekte geçerlidir, diğer sunucuların bellek içi önbelleğinde değil. `*` tag'i ayrılmış.
- Kaynak: https://learn.microsoft.com/aspnet/core/performance/caching/hybrid?view=aspnetcore-10.0#remove-cache-entries-by-tag · https://learn.microsoft.com/aspnet/core/performance/caching/hybrid?view=aspnetcore-10.0#cache-storage
- Sonuç: D-053 (6). Çok örnekli dağıtımda diğer örnekler `Cache:PermissionExpiration` içinde güncellenir (belgelenir).

### R-101: Access token türü (`typ`) ve harici sağlayıcılar (2026-10-21, faz 6b; kaynak)
- Soru: `ValidTypes = ["at+jwt"]` çekirdeğe konabilir mi (F6b.4a)?
- Bulgu: OpenIddict 7.7.1 access token'larını `typ: at+jwt` ile imzalar (`JsonWebTokenTypes.AccessToken`). Microsoft Entra'nın access token'ında da ID token'ında da başlıktaki `typ` "always JWT". Çekirdek `Jwt:Authority` ile herhangi bir OIDC sağlayıcısını doğruluyor (D-002, şablonun `--auth External`'ı); RFC 9068 §4 `at+jwt` kontrolünü RFC 9068 token'ları için istiyor.
- Kaynak: https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Abstractions/OpenIddictConstants.cs · https://learn.microsoft.com/entra/identity-platform/access-token-claims-reference · https://www.rfc-editor.org/rfc/rfc9068#section-4
- Sonuç: D-053 (8). Kontrol eklentide; çekirdek harici sağlayıcılarda audience ile ayırmaya devam eder.

### R-102: CORS'un uç bazında kapatılması (2026-10-21, faz 6b; kaynak)
- Soru: `UseCors(policyName)` bütün uçlara uygulanırken `/connect/authorize` ve `/account/*` nasıl hariç tutulur (F6b.4b, RFC 9700 §2.6)?
- Bulgu: `CorsMiddleware` (v10.0.12) önce uç meta verisine bakar: `IDisableCorsAttribute` varsa CORS çalışmaz, preflight 204 alır ama izin başlıkları yoktur. MS'in 2.2→3.0 geçiş belgesi `UseCors("default")` + `[DisableCors]` örneğini verir. `[DisableCors]` yalnızca `RequireCors` ile açılan CORS'u kapatmaz.
- Kaynak: https://github.com/dotnet/aspnetcore/blob/v10.0.12/src/Middleware/CORS/src/Infrastructure/CorsMiddleware.cs · https://learn.microsoft.com/aspnet/core/migration/22-to-30?view=aspnetcore-10.0#cors · https://learn.microsoft.com/aspnet/core/security/cors?view=aspnetcore-10.0
- Sonuç: Uçlara `DisableCorsAttribute` meta verisi.

### R-103: OpenIddict 7.7.1: saat, refresh token süresi ve revocation (2026-10-21, faz 6b; kaynak)
- Soru: Mutlak refresh ömrü nasıl ölçülür ve uygulanır, `/connect/revoke`'a uç meta verisi verilebilir mi (F6b.4e, F6b.6c)?
- Bulgu: `OpenIddictServerOptions.TimeProvider` boşsa DI'daki `TimeProvider`, o da yoksa `TimeProvider.System` kullanılır. `DisableSlidingRefreshTokenExpiration()` yeni refresh token'ın ilk token'ın bitişini almasını sağlar; `DisableRollingRefreshTokens()` ayrı bir seçenek. Passthrough yalnızca authorization, end-session, end-user verification, error, token ve userinfo uçlarında var; revocation OpenIddict'in authentication handler'ında tamamen yanıtlanıyor.
- Kaynak: https://github.com/openiddict/openiddict-core/blob/7.7.1/src/OpenIddict.Server/OpenIddictServerOptions.cs · …/OpenIddictServerBuilder.cs · …/OpenIddictServerConfiguration.cs · …/OpenIddict.Server.AspNetCore/OpenIddictServerAspNetCoreBuilder.cs
- Sonuç: F6b.4e ve F6b.6c'nin ölçümü buna göre kurulur.

### R-104: Antiforgery ve 2FA cookie'lerinin `Secure` varsayılanı (2026-10-21, faz 6b; kaynak)
- Soru: Eklentinin 2FA ve antiforgery cookie'leri hangi `SecurePolicy` ile çıkıyor (F6b.6b)?
- Bulgu: MS: ASP.NET Core antiforgery cookie'sinin `SecurePolicy`'sini bilerek `CookieSecurePolicy.None` yapar; Development dışında `o.Cookie.SecurePolicy = CookieSecurePolicy.Always` önerilir. Kimlik doğrulamayla ilgili cookie'ler daha sıkı bir politika kullanır. Eklentide yalnızca uygulama cookie'si `Always`.
- Kaynak: https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0#configure-antiforgery-with-antiforgeryoptions
- Sonuç: F6b.6b'nin ölçümü ve olası düzeltmesi buna göre.

### R-105: Kayıt, parola sıfırlama ve onay yeniden gönderiminde zamanlama ile hesap numaralandırma (2026-10-22, faz 6b; ölçüm + kaynak)
- Soru: Register, forgot-password ve resend yanıt süresinden hesabın varlığı okunabiliyor mu; okunuyorsa kaynaklar nasıl kapatılmasını söylüyor (F6b.6a)?
- Ölçüm (geçici test, commit'lenmedi; `IdentityServerTestHost`, yol başına 12 örneğin medyanı): anlık gönderici → register var olan 428 / yeni 418 ms, forgot-password bilinmeyen 1 / onaylı 2 ms, resend bilinmeyen 1 / onaysız 1 ms. 200 ms'lik gönderici → register 388 / 602 ms, forgot-password 1 / 209 ms, resend 1 / 204 ms. Yanıt, e-posta gönderimini bekliyor; e-posta yalnız hesap varken (ya da yeni kayıtta) gidiyor.
- Bulgu (kaynaklar):
  - Microsoft: ASP.NET Core Identity'nin kendi kodu aynı deseni kullanıyor. `MapIdentityApi` `/forgotPassword` ve Identity UI `ForgotPassword` (v10.0.12) kullanıcı yoksa hemen döner, varsa `IEmailSender`'ı `await` eder. Belgeler yalnız yanıt metninin hesabı ele vermemesini istiyor ("Don't reveal that the user does not exist or is not confirmed"); zamanlamadan söz etmiyor. `dotnet/aspnetcore`'da bu konuda issue yok. Genel kurallar: "ASP.NET Core Best Practices" uzun işleri istek içinde beklememeyi, arka plan servislerini önerir (performans gerekçesiyle); "Background tasks with hosted services" kuyruklu arka plan işinin kalıbını verir (sınırlı `Channel<T>`, `BoundedChannelFullMode.Wait`, `BackgroundService`). Microsoft'un e-posta için ya da numaralandırmaya karşı bir önerisi yok.
  - Keycloak (`ResetCredentialEmail.authenticate`): kullanıcı yoksa hemen başarı mesajı, varsa SMTP ile senkron gönderim. Aynı desen.
  - OWASP Forgot Password Cheat Sheet: yanıtlar tutarlı sürede dönmeli; yollar "asynchronous calls" ya da "the same logic is followed, instead of using a quick exit method". Kuyruk ya da arka plan işi adıyla geçmiyor.
  - OWASP Authentication Cheat Sheet: "quick exit" yerine her yolda aynı işi yapmak (giriş için sahte parola hash'i); kayıt da kapsamda.
  - ASVS 5.0.0 6.3.8 (**L3**): geçerli kullanıcılar hata mesajlarından, HTTP kodlarından ya da yanıt sürelerinden çıkarılamamalı; kayıt ve parola sıfırlama da kapsamda.
  - Proje: araştırma hedefi ASVS L1/L2 koymuş, bu kalemi L3/P3 saymıştı (proposals/guvenlik-kalite-arastirmasi.md #29). Kullanıcının K5 kararı ise bunu "ölçülüp açık çıkarsa aynı fazda kapanır" listesine koydu (proposals/guvenlik-kalite-kararlari.md, D-046, PLAN F6b.6).
- Kaynak: https://github.com/dotnet/aspnetcore/blob/v10.0.12/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs · …/Identity/UI/src/Areas/Identity/Pages/V5/Account/ForgotPassword.cshtml.cs · https://learn.microsoft.com/aspnet/core/security/authentication/accconfirm?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0#complete-long-running-tasks-outside-of-http-requests · https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0#queued-background-tasks · https://github.com/keycloak/keycloak/blob/main/services/src/main/java/org/keycloak/authentication/authenticators/resetcred/ResetCredentialEmail.java · https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html · https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html · https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x15-V6-Authentication.md
- Sonuç: Açık var (ölçüldü). Microsoft ve Keycloak bunu kapatmıyor; kapatma gerekçesi OWASP ve ASVS L3. Kapatma yolu kullanıcıya soruldu (asenkron gönderim / her yolda e-posta / belgelenmiş sapma).

### R-106: Refresh token'ın mutlak ömrü (2026-10-22, faz 6b; ölçüm)
- Soru: OpenIddict 7.7.1'in rolling refresh token'ları girişten 7 gün sonra bitiyor mu, yoksa her yenileme yeni 7 gün mü veriyor (F6b.4e)?
- Ölçüm (`FakeTimeProvider`, `RefreshTokenAbsoluteLifetimeTests`): girişten 6 gün sonra yenilenen token, girişten 7 gün + 1 sn sonra 200 aldı. Yani varsayılan "sliding": her yeni refresh token tam ömür alıyor, sürekli yenilenen bir oturum (çalınmış bir token dahil) hiç bitmiyor.
- Kaynak: R-103 (`DisableSlidingRefreshTokenExpiration()`), ölçüm testi.
- Sonuç: `server.DisableSlidingRefreshTokenExpiration()`; aynı test 400 `invalid_grant`. Rolling ve 30 sn leeway değişmedi.

### R-107: Antiforgery cookie'sinin `Always` politikası ve düz http (2026-10-22, faz 6b; ölçüm)
- Soru: Antiforgery cookie'si her ortamda `CookieSecurePolicy.Always` yapılabilir mi (F6b.6b)?
- Ölçüm: `Always` ile düz http üzerinden açılan `/account/login` 500 döndü (Api host'unun `SecureDefaultsTests`'i http üzerinden yakaladı). Antiforgery, istek SSL değilken `Always` politikasında token üretmeyi reddediyor. Varsayılan `None` ise https'te bile Secure bayrağı koymuyor.
- Kaynak: R-104 (MS: Development dışında `Always`), ölçüm.
- Sonuç: Development dışında `Always`, Development'ta `SameAsRequest` (https'te Secure, http'de sayfa çalışır). 2FA cookie'leri uygulama cookie'si gibi her ortamda `Always`: cookie işleyicisi http'de hata vermez, yalnız bayrağı koyar.

## Faz 6c araştırması (2026-10-22)

### R-108: `dotnet test`'in MTP modu ve xUnit v3 4.x (2026-10-22, faz 6c; kaynak + ölçüm)
- Soru: .NET 10 SDK'da `dotnet test` Microsoft.Testing.Platform (MTP) modunda nasıl çalışıyor, xUnit 4.0.x hangi paketleri istiyor, repo'nun komutlarından hangileri kırılıyor (F6c.1a)?
- Bulgu (kaynak):
  - MTP modu `global.json`'daki `"test": { "runner": "Microsoft.Testing.Platform" }` ile açılır. Bu modda bütün test projeleri MTP olmalı. MS'in geçiş adımları çözümü `--solution`'la vermeyi söylüyor; test uygulamasının seçenekleri `--`'dan sonra gelir. `--nologo` MTP modunda ancak .NET 11 Preview 7'de tanınıyor.
  - xunit.v3 4.0.2 (2026-10-09) MTP v2 (2.5.1) kullanıyor; MTP v1 desteği 4.0.0'da kalktı. MTP için `xunit.v3` yeterli. `Microsoft.NET.Test.Sdk` ve `xunit.runner.visualstudio` yalnız VSTest için gerekli (xUnit: "you can remove the package references"); Test Explorer MTP projelerini kendi MTP deneyimiyle çalıştırıyor.
  - 4.0'ın kırıcı değişiklikleri (`CollectionBehavior`'ın paralellik özellikleri, `ITestCaseOrderer`, `Xunit.Runners`) repo'da kullanılmıyor (grep).
  - Filtreler: `--filter-class`, `--filter-method`, `--filter-namespace`, `--filter-trait` (ve `-not-` biçimleri), `--filter-query`; `*` yalnız başta ya da sonda. 4.0.0 VSTest biçimli `--filter`'ı da geri getirdi.
- Ölçüm (SDK 10.0.300, xunit.v3 4.0.2):
  - 361/361 yeşil, yerelde ~4 dk.
  - Stop hook'unun eski komutu `dotnet test NovaCore.slnx --no-build -nologo` exit 5 verdi: `-nologo` test uygulamasına geçiyor ve tanınmıyor. Konumsal `NovaCore.slnx` ise 10.0.300'de hâlâ çalışıyor (exit 0); SDK'nın "should be via '--solution'" mesajı bu durumda çıkmadı.
  - `-- --filter-method "*.AuthorizationCodeFlowTests.Refresh_rotates_the_refresh_token"` 1 test, `-- --filter-class "*.ResultTests"` 6 test, `-- --filter "FullyQualifiedName~…"` 1 test koşturdu.
- Kaynak: https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test · https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp · https://xunit.net/releases/v3/4.0.0 · https://xunit.net/releases/v3/4.0.2 · https://xunit.net/docs/getting-started/v3/microsoft-testing-platform · https://xunit.net/docs/query-filter-language
- Sonuç: `global.json`'a `test.runner`; `xunit.v3` 4.0.2; iki VSTest paketi kalkar. Hook, CI ve release `dotnet test --solution NovaCore.slnx` kullanır (MS'in önerdiği biçim). Hook'tan `-nologo` kalkar. PLAN'daki `dotnet test NovaCore.slnx` genel kuralı çalıştığı için değişmedi (planlanan PLAN düzenlemesi gereksiz çıktı).

### R-109: R-055'in açılış yarışı MTP + xUnit 4 altında ve upstream durumu (2026-10-22, faz 6c; kaynak + ölçüm)
- Soru: Açılışın düşmesini bekleyen testlerin ara sıra gerçek hata yerine `ObjectDisposedException` alması (R-055) MTP + xUnit 4 altında sürüyor mu, framework'te düzeltildi mi (F6c.1c, Q-46)?
- Bulgu (kaynak): `DeferredHostBuilder.DeferredHost.StartAsync` hem `release/10.0`'da hem `main`'de hâlâ `_hostStartedTcs`'i beklemeden önce `_host.Services.GetRequiredService<IHostApplicationLifetime>()` çağırıyor. Hata dotnet/aspnetcore#69684 olarak açık ("WebApplicationFactory sometimes throws ObjectDisposedException instead of the app's startup exception", 2026-10-06); düzeltme PR'ı #69685 açık, merge edilmemiş. Yani kök neden bizim tarafta değişmedi.
- Ölçüm: MTP + xUnit 4.0.2, Release, 1 tam koşu (361/361, isabet yok); 10 koşuluk ölçüm, koşu başına ~4,5 dk sürdüğü ve kök neden kaynakta doğrulandığı için durduruldu. Eski sıklık ~2/45 koşu (R-055) ve 6a'da CI; birkaç koşu bir şey kanıtlamazdı.
- CI kanıtı: F6c.1c'den önceki `f32b0ec` push'unda (MTP + xUnit 4.0.2) `ConfigurationValidationTests.Invalid_configuration_stops_the_host_at_startup(Production, Jwt:Authority, http://idp.example.com)` beklenen mesaj yerine "Cannot access a disposed object. Object name: 'IServiceProvider'" aldı; taşımadan sonraki koşular yeşil.
- Repo'da etkilenen tek test: `ConfigurationValidationTests.Invalid_configuration_stops_the_host_at_startup` (12 vaka). Diğer açılış hatası testleri zaten kendi host'larında (`CoreWebTestHost`, `IdentityServerTestHost`) `StartAsync` ile koşuyor.
- Kaynak: https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Mvc/Mvc.Testing/src/DeferredHostBuilder.cs · https://github.com/dotnet/aspnetcore/issues/69684 · https://github.com/dotnet/aspnetcore/pull/69685
- Sonuç: Teori `CoreWebTestHost` + `AddCorePersistence` üzerinde, `StartAsync`'in kendi exception'ıyla koşuyor: ayarlar çekirdek paketlerin, Program'ın değil. 12 vakanın hepsi beklenen mesajla düştü; aynı host geçerli yapılandırmayla Development ve Production'da açıldı (deneme, commit'lenmedi). "Sonradan eklenen yapılandırma" davranışını gösteren iki test Api factory'sinde kaldı (host açılıyor, yarış yok). #69685 yayımlanınca bir şey yapmak gerekmez.

### R-110: SQL Server test katmanı: Testcontainers 4.16, xUnit v3 fixture'ları ve gerçek migration'lar (2026-10-22, faz 6c; kaynak + ölçüm)
- Soru: Gerçek SQL Server'da test için container nasıl açılır, test sınıfları nasıl paylaşır, Docker yokken ne olur, migration'lar nasıl sınanır (F6c.2a, D-043)?
- Bulgu (kaynak):
  - `Testcontainers.MsSql` 4.16.0 (MIT): `new MsSqlBuilder(image).Build()` + `StartAsync()`, `GetConnectionString()`. Geçişliler (Docker.DotNet.Enhanced*, SSH.NET 2026.0.0, SharpZipLib, BouncyCastle.Cryptography) MIT; lisans kapısı geçti. Ryuk (resource reaper, `testcontainers/ryuk:0.14.0`) container'ı test süreci bitince siler; Testcontainers onu kapatmamayı öneriyor.
  - `DockerUnavailableException` (`DotNet.Testcontainers.Builders`): "thrown when a connection to the Docker endpoint cannot be established".
  - xUnit v3: fixture'lar birbirine bağımlı olamaz ("fixtures cannot take dependencies on other fixtures"); assembly fixture'ları her testten önce, hevesle oluşturuluyor. `Assert.Skip/SkipWhen/SkipUnless` testi çalışma anında atlatıyor; MTP'de `--fail-skips on` atlanan testi başarısız sayıyor.
  - Respawn 7.0.0 bağımlılıksız (Apache-2.0); veriyi sıfırlamak gereken ilk test F6c.2b'de geldiği için paket orada eklenir.
- Ölçüm (yerel, Docker Desktop 29.2.1):
  - İlk koşu imaj indirmesiyle ~2 dk, sonrakiler ~40 sn (4 test: container, Api host'unun açılıştaki migration'ı ve üç ayrı veritabanı).
  - `DOCKER_HOST=tcp://127.0.0.1:1` Docker'sız durumu taklit etmedi: Testcontainers sıradaki uç noktaya (Docker Desktop'ın named pipe'ı) geçip testleri koşturdu. Docker'sız skip yolu yerelde denenemedi; Docker gerçekten yoksa `DockerUnavailableException` beklenir, başka bir exception gelirse testler atlanmaz, kırmızı olur (sessiz değil).
  - Tam takım 366/366 (`--fail-skips on`), ~5,5 dk.
- Kaynak: https://dotnet.testcontainers.org/modules/mssql/ · https://dotnet.testcontainers.org/api/resource_reaper/ · https://xunit.net/docs/shared-context · https://xunit.net/docs/getting-started/v3/whats-new · https://www.nuget.org/packages/Testcontainers.MsSql/4.16.0 · Testcontainers 4.16.0 XML belgeleri (NuGet paketi)
- Sonuç: Statik `Lazy` ile koşu başına tek container (`TestSqlServer`), sınıf başına veritabanı, `SqlServerApiFactory` üretimdeki `DatabaseMigrationService` yolunu kullanır. Docker yoksa yerelde skip; CI ve release `--fail-skips on`. D-056.

### R-111: SQL Server'da `LIKE` collation'ı ve `decimal(18,2)` (2026-10-22, faz 6c; ölçüm)
- Soru: SQLite'ın göstermediği iki SQL Server davranışı: ürün aramasının `EF.Functions.Like`'ı hangi collation'la eşleşiyor ve `decimal(18,2)` sütunu fazla basamağı ne yapıyor (F6c.2b)?
- Ölçüm (`mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04`, EF Core 10.0.12, varsayılan sunucu collation'ı, `SqlServerQueryTests`):
  - Arama büyük/küçük harf duyarsız (`blue` → "Blue Widget" ve "BLUE gadget", `CAFÉ` → "Café crème"), aksan duyarlı (`cafe` hiçbir şey bulmuyor). Liste `Name`'e göre de bu collation'la sıralanıyor ("BLUE gadget" < "Blue Widget").
  - `POST /api/v1/products` 201 döndü ama fiyat yuvarlanarak kaydedildi: 0.001 → 0.00, 10.004 → 10.00, 10.005 → 10.01, 10.999 → 11.00. Yani "0'dan büyük" kuralını geçen 0.001 bedava bir ürün olarak kaydediliyordu ve API'nin kabul ettiği değer okunan değerden farklıydı. SQLite `decimal`'ı metin olarak birebir sakladığı için bunu hiç göstermedi. 16'dan fazla tam basamak `decimal(18,2)`'ye sığmıyor.
- Kaynak: ölçüm; https://learn.microsoft.com/ef/core/providers/sqlite/limitations (SQLite'ta `decimal` karşılaştırması ve saklanması)
- Sonuç: Kullanıcı reddetmeyi seçti (Q-47). Ürün isteklerinin `Price`'ı `DecimalColumnAttribute(18, 2)` taşıyor: en fazla 2 ondalık ve 16 tam basamak, yoksa 400. Hassasiyet `ProductConfiguration`'daki sabitlerden geliyor (sütun ve kural tek yerden). Sütunun sınır değerleri (0.01, 9999999999999999.99) SQL Server'da birebir dönüyor. Collation davranışı testle sabitlendi; `LIKE` aramasında `%`, `_` ve `[` joker olarak yorumlanıyor (kaçırılmıyor) → Sonraya bırakılanlar.

### R-112: Outbox saklama silmesi SQL'de ve SQLite test katmanında `DateTimeOffset` (2026-10-23, faz 6c; kaynak + ölçüm)
- Soru: `OutboxDispatcher.DeleteExpiredAsync`'teki "SQLite karşılaştıramıyor" diye bellekte yapılan yaş karşılaştırması (`OutboxDispatcher.cs:178`) üretim kodunu test aracına göre şekillendiriyor mu, doğrusu ne (F6c.3)?
- Bulgu (kaynak + kod):
  - MS: SQLite `DateTimeOffset`'i yerel olarak desteklemiyor; eşitlik dışındaki karşılaştırma ve sıralama istemcide yapılmak zorunda. Değer dönüştürücü (value conversion) sağlayıcı tipini değiştirmenin belgelenmiş yolu.
  - `AddOutbox()` `outbox_messages`'a `(ProcessedOnUtc, Id)` index'ini zaten koyuyor (`OutboxDispatch` migration'ı). Yani `ProcessedOnUtc < cutoff` index üzerinde bir aralık araması; `Id` sıralı parçalar ve "geç işlenmiş mesaj ilk parçayı tutmasın" mantığı yalnızca SQLite yüzünden vardı.
- Ölçüm:
  - Yeni sorgu (`Where(ProcessedOnUtc < cutoff).OrderBy(ProcessedOnUtc).Take(50).ExecuteDeleteAsync()`, kısa parçaya kadar döngü) SQL Server'da çalıştı: Api host'unun kendi dispatcher'ı süresi dolan mesajı kayıtlarıyla (cascade) sildi, dolmayanı bıraktı (`OutboxRetentionTests`).
  - SQLite test bağlamları `DateTimeOffset`'i UTC tick (`long`) olarak saklayınca (`UtcTicksModelCustomizer`, yalnız test kodu) aynı sorgu SQLite'ta da çevrildi; `OutboxDispatcherTests`'in saklama testleri (geç işlenmiş mesaj dahil) yeşil. Ham SQL bu sütunlara tick yazıp okuyor.
  - Ek gözlem: Docker Desktop yeniden başladıktan sonraki ilk koşuda Testcontainers'ın Ryuk container'ı hazırlık kontrolünde kayboldu ("Could not find resource 'DockerContainer'"), fixture düştü; ikinci koşu yeşil. Ortamdan kaynaklı, kalıcı değil.
- Kaynak: https://learn.microsoft.com/ef/core/providers/sqlite/limitations · https://learn.microsoft.com/ef/core/modeling/value-conversions · `OutboxModelBuilderExtensions.cs` (index)
- Sonuç: Karşılaştırma SQL'de, mevcut index üzerinde; SQLite katmanı kendini uyarlar (D-059). Modelde ve migration'larda değişiklik yok.

### R-113: Coverage: coverlet.MTP, ReportGenerator ve ratchet (2026-10-23, faz 6c; kaynak + ölçüm)
- Soru: MTP'de coverage nasıl toplanır, main'e göre düşüşü engelleyen kapı nasıl kurulur (F6c.4)?
- Bulgu (kaynak):
  - coverlet.MTP 10.1.0 (MIT) MTP ≥ 2.4.1 ister (repo 2.5.1); seçenekler test uygulamasına `--` sonrası gider: `--coverlet`, `--coverlet-output-format cobertura`, `--coverlet-include/exclude "[Assembly]Type"`, `--coverlet-exclude-by-file`. Kendi eşiği (`--coverlet-threshold`) belgede "henüz desteklenmiyor" diye de geçtiği için kullanılmadı.
  - ReportGenerator 5.5.11 (Apache-2.0, dotnet tool) Cobertura'dan `TextSummary`, `JsonSummary`, `MarkdownSummaryGithub` üretir.
- Ölçüm:
  - Coverlet derlenmiş DLL'leri yerinde enstrümante ediyor; enstrümante edilmiş `NovaCore.Abstractions` bir `netstandard` referansı kazandı ve `PackageDependencyTests` ("yalnız BCL") kırmızı oldu. Mimari testler bu yüzden coverage'sız, ayrı bir `dotnet test` adımında (`--filter-namespace`) koşuyor; coverage koşusu onları `--filter-not-namespace` ile dışarıda bırakıyor.
  - Çıktı dosyası `coverage.cobertura.<zaman damgası>.xml`; desen `coverage.cobertura.*.xml`. Git Bash'te `-reports:/c/...` Windows yoluna çevrilmiyor, betik göreli yol kullanıyor. Bu makinede `jq` yok; betik ReportGenerator'ın metin özetini `sed` ile okuyor.
  - Release'te iki tam koşu (mimari testler hariç 356 test, SQL Server dahil, ~4,5 dk) birebir aynı sonucu verdi: satır %95,5 (2088/2186), dal %82,0 (548/668). Debug'ta (mimari testler dahil) %95,2 / %81,6; baseline Release (CI) değeri.
  - Kapı denemesi: baseline %99 iken betik exit 1 verdi (karşılaştırma ve `--update`'in yalnız yukarı yazan aritmetiği ayrıca denendi).
- Kaynak: https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/Coverlet.MTP.Integration.md · https://github.com/danielpalme/ReportGenerator · https://www.nuget.org/packages/coverlet.MTP/10.1.0 · ölçüm
- Sonuç: `eng/check-coverage.sh`: testleri coverage'la koşar, satır ve dal kapsamı `eng/coverage-baseline.txt`'in 0,3 puandan fazla altına düşerse kırmızı; `--update` baseline'ı yalnız yukarı yazar. CI'da "Test with coverage" + "Architecture tests" adımları. D-060.

### R-114: API sözleşmesi: build sırasında belge üretimi ve oasdiff'in OpenAPI 3.1 kapsamı (2026-10-23, faz 6c; kaynak + ölçüm)
- Soru: OpenAPI belgesi build'de üretilip commit'lenebilir mi (PLAN F6c.5), oasdiff .NET 10'un 3.1 belgesindeki kırıcı değişiklikleri yakalıyor mu?
- Bulgu (kaynak):
  - MS: `Microsoft.Extensions.ApiDescription.Server` belgeyi build'de, uygulamanın giriş noktasını sahte bir sunucuyla çalıştırarak üretir; "any logic in the apps startup is invoked". Ortamı seçen `OpenApiGenerationEnvironment` ".NET 11'den itibaren"; .NET 10'da tek yol, giriş assembly'sine (`GetDocument.Insider`) bakan bir dal.
  - oasdiff: OpenAPI 3.1 desteği v1.15.0'dan beri genel kullanımda (`$dynamicRef` ve bileşenlerdeki path item'lar hariç). `--err-ignore` dosyası oasdiff'in mesaj satırlarıyla eşleşir. GitHub Action'ın `review` girdisi varsayılan olarak karşılaştırmayı oasdiff.com'a yükler; action kullanılmadı.
- Ölçüm:
  - `Microsoft.Extensions.ApiDescription.Server` 10.0.12 ile Api build'i GetDocument adımında düştü: Program Production'da açıldı ve `No Data Protection key store is configured. Outside Development ...` (D-036 (6)) ile durdu. Deneme geri alındı.
  - Test host'unun (Development) `/openapi/v1.json`'u iki koşuda birebir aynı (OpenAPI 3.1.1, 1637 satır).
  - oasdiff v1.33.0 (`tufin/oasdiff@sha256:6263a96d…`, 7 günlük cooldown'a uyan en yeni; v1.34.1'in güvenlik düzeltmesi yalnız `--open` ve action için) bu belgenin kopyalarında: işlem silme (`api-removed-without-deprecation`), yanıttan zorunlu alan silme (`response-required-property-removed`), istekte yeni zorunlu alan (`new-required-request-property`) ve yanıt alanı tip değişikliği (`response-property-type-changed`, 3.1'in `["integer","string"]` birleşimi dahil) exit 1; yeni opsiyonel yanıt alanı ve aynı belge exit 0. Kabul dosyasındaki bir satır ilgili kırılımı susturdu; `#` yorum satırı zararsız.
  - Yan gözlem: `CreateProduct.Request` ve `CreatePayment.Request` belgede `Request`/`Request2` gibi sıralı adlar alıyor; kayıt sırası değişirse şema adları kayar (oasdiff `$ref`'i çözdüğü için kırıcı sayılmaz).
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0#generate-openapi-documents-at-build-time · https://github.com/oasdiff/oasdiff/blob/main/docs/OPENAPI-31.md · https://github.com/oasdiff/oasdiff/blob/main/docs/BREAKING-CHANGES.md · https://github.com/oasdiff/oasdiff-action · ölçüm
- Sonuç: Belge build'de değil test host'unda üretilir ve commit'lenir (`docs/openapi/v1.json`, `OpenApiContractTests`); CI'ın `api-contract` job'u `eng/check-openapi.sh` ile main'e göre kırıcı değişikliği yakalar. D-061.

### R-115: CsCheck property testleri (2026-10-23, faz 6c; kaynak + ölçüm)
- Soru: Property-based testler için CsCheck nasıl kullanılır, hangi kuralları sınamaya değer (F6c.6)?
- Bulgu (kaynak): CsCheck (Apache-2.0, bağımlılıksız) test çatısından bağımsızdır, başarısızlıkta exception atar ve yeniden üretmek için seed basar ("Set seed: ... to reproduce"); varsayılan 100 iterasyon, işlemci sayısı kadar iş parçacığı. Üreteçler: `Gen.Int[a, b]`, `Gen.Long[a, b]`, `Gen.String[Gen.Char.AlphaNumeric, min, max]`, `Gen.Enum<T>()`, `gen.Array[min, max]`, `Gen.Select(...)`; `Sample` / `SampleAsync(..., iter, threads)`. Model tabanlı denetim (`SampleModelBased`) var; birkaç işlem için işlem listesi üretip küçük bir modelle karşılaştırmak yeterli. Belgeler 5.x'i anlatıyor; kullanılan API 4.9.1'de derlendi.
- Ölçüm:
  - `PaginationRequest.Skip` her `int` sayfa için denendiğinde taştı: `(Page - 1) * PageSize` büyük sayfada `int` sarıyordu (ör. `page=2147483647` başka bir sayfanın ofsetini veriyordu). Shrink'ten sonra seed `e_6U6vth8ZA3`.
  - `Result<T>`, `ToHttpResult` (her `ErrorType` için durum, `detail`, `code`), `PagedList` sayıları, `Payment` durum geçişleri, `Product` stok dizileri ve Catalog rezervasyon handler'ının tekrar teslimi (25 örnek, tek iş parçacığı, paylaşılan SQLite host'u) yeşil. Tam takım 388/388, ~5 dk.
- Kaynak: https://github.com/AnthonyLloyd/CsCheck · https://www.nuget.org/packages/CsCheck/4.9.1 · ölçüm
- Sonuç: Property testleri `Unit/*PropertyTests` ve `Integration/StockReservationPropertyTests`. `Skip` `long`'da hesaplanıp `int.MaxValue`'da kesiliyor (D-062).

### R-116: Stryker.NET 5.0 ve Microsoft.Testing.Platform runner'ı (2026-10-23, faz 6c; kaynak + ölçüm)
- Soru: Stryker.NET xUnit v3 4.x + MTP ile koşuyor mu, Abstractions ve modüllerin domain kodu haftalık nasıl mutasyona uğratılır (F6c.7)?
- Bulgu (kaynak): dotnet-stryker 5.0.0 (Apache-2.0) .NET 10 runtime'ında koşar; `--test-runner mtp` önizlemedir ("may have limitations"); xUnit v3'ün MTP runner'ında `test-case-filter` yok. Çalışma kipleri: test projesinden (`--project` doğrudan referanslardan biri), kaynak projeden (`--test-project`) ya da çözümden (`--solution`). `--mutate` glob alır, `--break-at` 0 iken hiçbir skor koşuyu kırmaz.
- Ölçüm (yerel, SDK 10.0.300):
  - Test projesi kipi (`tests/NovaCore.Api.Tests`, `--project NovaCore.Abstractions.csproj`) ve kaynak projesi kipi "No project found" ile durdu: Buildalyzer bu makinede Visual Studio 18 Insiders'ın MSBuild'ini kullanıyor, test projesinin ve Api'nin "simulated build"i düşüyor, Abstractions ise yalnızca geçişli referans.
  - Çözüm kipi (`--solution NovaCore.slnx`) çalıştı: 388 test bulundu, ilk koşu ~5 dk, Abstractions'ta 46 mutant. Ama test başına coverage yakalaması her testi tek tek koşturuyor ve host açan testler zaman aşımıyla "Dubious" oluyor (6 dakikada 26 test, bitmedi). Coverage'sız ya da toplu coverage'da her mutant tam takımı (~5 dk) koşturur: yalnız Abstractions için ~1 saat.
  - Saf testler `tests/NovaCore.Domain.Tests`'e taşınınca (37 test, 2–4 sn) test projesi kipi çalıştı: Abstractions 46 mutant, Catalog `Domain/` 14, Payments `Domain/` 10 test edildi; üç koşu toplam ~2 dk 20 sn. İlk skorlar %69,77 / %61,11 / %70,00.
  - Hayatta kalanlar: `PagedList.HasPreviousPage`/`HasNextPage` ve `pageSize = 1` sınırları (rastgele üreteçler sınırlara nadiren düşüyordu), sınanmayan `Result.Failure(Error.None)` değişmezi, hata mesajı metinleri, eşdeğer mutantlar (`page < 1` → `page <= 1`). Üreteçlere sınır değerleri ve değişmeze bir test eklenince Abstractions %79,07.
- Kaynak: https://stryker-mutator.io/docs/stryker-net/configuration/ · https://stryker-mutator.io/docs/stryker-net/operating-modes/ · https://github.com/stryker-mutator/stryker-net/releases · ölçüm
- Sonuç: `eng/mutation.sh` üç hedefi `tests/NovaCore.Domain.Tests`'e karşı koşturur (`--break-at 0`, html/markdown/json); `mutation.yml` haftalık + `workflow_dispatch`. D-063.

## Faz 6d araştırması (2026-10-23)

### R-117: Kategori bazlı analiz modu ve ilk ölçüm (2026-10-23, faz 6d; kaynak + ölçüm)
- Soru: PLAN F6d.1'deki `AnalysisMode<Kategori>=All` bu repoda hangi kuralları açar, kaç bulgu çıkar?
- Bulgu (kaynak): `Directory.Build.props` `AnalysisLevel=latest-recommended` (bileşik değer) kullanıyor. SDK, kategori ayarı yoksa `AnalysisLevel<Kategori>`'yi `AnalysisLevel`'dan alır ve modu onun son ekinden okur; `AnalysisMode<Kategori>`'ye yalnız son ek boşsa bakar (`sdk/10.0.300/Sdks/Microsoft.NET.Sdk/analyzers/build/Microsoft.CodeAnalysis.NetAnalyzers.targets:502`, `:531`–`:532`). MS: "if you specify both properties, `AnalysisLevel` takes precedence over `AnalysisMode`". Yani `AnalysisModeSecurity=All` burada hiçbir kural açmaz (ölçüldü: 0 uyarı). Doğrusu `AnalysisLevel<Kategori>=latest-all`; Security için `analysislevelsecurity_10_all.globalconfig` CA2100, CA3001, CA3075, CA5394, CA5404'ü warning yapıyor. Metrik kuralları (CA1501/1502/1505/1506/1509) `All`'da da kapalı, tek tek açılır.
- Ölçüm (`dotnet build NovaCore.slnx --no-incremental -p:TreatWarningsAsErrors=false -p:AnalysisLevel<Kategori>=latest-all`, her kategori ayrı, tekil konum):
  - Security: 0.
  - Reliability: CA2007 480 (Identity.OpenIddict 161, EntityFrameworkCore 15, AspNetCore 3, modüller 19, testler 282), CA2000 17 ve CA2025 1 (hepsi testlerde).
  - Performance: CA1812 59 (paketler 29, modüller 7, testler 23), CA1849 3 (testlerde `SqliteConnection.Open()`).
  - Usage: CA2234 79 (hepsi testlerde, `HttpClient` string URI), CA2225 2 (`Result<T>`'nin örtük operatörleri), CA2213 2 (testlerde dispose edilmeyen factory alanları).
- Kural sayfaları ("When to suppress"):
  - CA2007: "generally appropriate to suppress the warning entirely for projects that represent application code"; ASP.NET Core'da SynchronizationContext yok, `ConfigureAwait` davranışı değiştirmez. "Passing `false` is a good option for app-independent libraries."
  - CA1812: "safe to suppress"; önerilen durumlar: IoC container'a kayıtlı, reflection'la ya da ASP.NET/runtime tarafından oluşturulan sınıflar.
  - CA2234: yalnızca parametre URI değilse bastırılabilir.
  - CA2225: "Do not suppress a warning from this rule if you're implementing a shared library."
  - CA1000: "Do not suppress a warning from this rule." CA2225'in analizörü karşılığı yalnızca operatörü tanımlayan tipte, public ve `To<Hedef>`/`From<Kaynak>` adıyla arıyor (dotnet/sdk `OperatorOverloadsHaveNamedAlternates.cs`); generic `Result<T>`'de bu public static bir üye demek, yani CA1000 ile çakışıyor → Q-48.
  - CA1506: "You can suppress this warning when the type or method is considered maintainable despite its large number of dependencies on other types." Varsayılan eşik tip 95, diğerleri 40; `CodeMetricsConfig.txt` yalnızca eşiği değiştirmek için.
- Kaynak: https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#analysislevelcategory · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview · https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2007 · …/ca1812 · …/ca2234 · …/ca2225 · …/ca1000 · …/ca1506 · https://github.com/dotnet/sdk/blob/release/10.0.3xx/src/Microsoft.CodeAnalysis.NetAnalyzers/src/Microsoft.CodeAnalysis.NetAnalyzers/Microsoft.CodeQuality.Analyzers/ApiDesignGuidelines/OperatorOverloadsHaveNamedAlternates.cs · ölçüm
- Sonuç: F6d.1 `AnalysisLevel<Kategori>=latest-all` kullanır (plandaki özellik adından sapma); bastırma kararları D-064.

### R-118: `dotnet format --verify-no-changes`, satır sonları ve PublicAPI dosyaları (2026-10-23, faz 6d; ölçüm + kaynak)
- Soru: CI'daki format kontrolü `.editorconfig`'in `end_of_line = crlf`'i ve Linux checkout'u ile nasıl çalışır (F6d.2)?
- Ölçüm (SDK 10.0.300, Windows):
  - CRLF checkout'ta (`core.autocrlf=true`): 181 bulgu. 160 `ENDOFLINE` (çalışma ağacında LF kalmış 9 dosya), 12 `CHARSET` (EF'in BOM'la yazdığı migration'lar; 33 migration dosyasının hepsi BOM'lu), 9 `IMPORTS`.
  - `end_of_line` kaldırılınca `ENDOFLINE` kontrolü hiç yapılmıyor. Linux checkout'u LF'tir; `end_of_line = crlf` orada her dosyayı kırardı.
  - `end_of_line` yokken, LF checkout'lu bir Windows worktree'sinde (`core.autocrlf=false`) format yine kırmızı: `WHITESPACE: Replace 13 characters with '\r\n…'`. Formatter dokunduğu satır kırılımlarını platformun satır sonuyla yazıyor (seçenek `end_of_line`'dan, yoksa `Environment.NewLine`).
  - `.gitattributes` `* text=auto` ile aynı worktree CRLF çıkıyor (`text` dosyaları `core.eol`'a göre yazılır, varsayılanı native) ve format temiz. Index'te değişiklik yok: 353 dosyanın hepsi zaten LF (`git add --renormalize` bir şey değiştirmedi).
  - `dotnet format` workspace yüklerken "Duplicate source file PublicAPI.*.txt" uyarısı veriyor: Microsoft.CodeAnalysis.PublicApiAnalyzers 5.6.0'ın `buildTransitive/Microsoft.CodeAnalysis.PublicApiAnalyzers.targets:96`–`:100` dosyalar proje klasöründe varsa onları `AdditionalFiles`'a kendisi ekliyor; `Directory.Build.targets` de ekliyordu (R-037 eski bir sürümü okumuştu).
  - Şablonun ürettiği projeler (SmokeBuiltIn, SmokeExternal) `dotnet format --verify-no-changes`'ta 9'ar dosyada `IMPORTS` hatası veriyor: şablon `using MyService…`'i `NovaCore…`'dan önce yazıyor ve ad değişince sıra bozuluyor.
- Kaynak: https://learn.microsoft.com/dotnet/core/tools/dotnet-format · https://git-scm.com/docs/gitattributes#_text · https://git-scm.com/docs/git-config#Documentation/git-config.txt-coreeol · paketin kendi targets dosyası · ölçüm
- Sonuç: `.editorconfig` `end_of_line` taşımaz, `.gitattributes` `* text=auto`, migration'lar `charset = utf-8-bom`; CI `dotnet format NovaCore.slnx --verify-no-changes --no-restore`. `Directory.Build.targets` PublicAPI dosyalarını artık eklemiyor. Şablon format adımını taşımaz: smoke testinin ürettiği iki projede `dotnet format --verify-no-changes` 9'ar `IMPORTS` hatası verdi (using sırası proje adına göre değişiyor). D-065.

### R-119: BannedApiAnalyzers, Meziantou.Analyzer ve Roslynator CLI (2026-10-23, faz 6d; kaynak + ölçüm)
- Soru: F6d.3'ün üç aracı bu repoda nasıl bağlanır, ne bulur?
- BannedApiAnalyzers 5.6.0 (MIT, Microsoft; PublicApiAnalyzers ile aynı sürüm): `BannedSymbols.txt` ya da `BannedSymbols.*.txt` adlı `AdditionalFiles` okur; girdi biçimi documentation comment ID'si, parametresiz `M:T.Method` yalnız parametresiz overload'u kapsar, her overload ayrı satır (dotnet/roslyn `src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md`). Üretilen kodu da analiz eder. Bugünkü kodda bulgu yok; deneme dosyalarıyla RS0030 doğrulandı.
- Meziantou.Analyzer (MIT): README CPM altında `GlobalPackageReference` öneriyor; `MeziantouAnalysisMode` `Default` (her kuralın kendi önemi), `None`, `all-suggestions`, `all-warnings`, `all-errors`. README'deki tabloda 131 kural warning, 101 kural suggestion. 7 günlük cooldown'u (D-051 (7)) geçen en yeni sürüm 3.0.303 (2026-10-13; 3.0.304…3.0.316 13–21 Ekim arası). Varsayılan modda ölçüm (`TreatWarningsAsErrors=false`): 635 uyarı. MA0004 408 (`ConfigureAwait`, CA2007'nin aynısı; varsayılan `MA0004.report = DetectContext` kütüphaneleri ve testleri raporluyor), MA0002 96, MA0048 69 (dosya başına bir tip: options + validator, exception ailesi, entity + enum, zaman damgalı migration dosyaları; `MA0048.mode = Prefix` çoğunu çözmüyor), MA0006 41, MA0009 10 (regex timeout; belge yalnız timeout'u uyumlu sayıyor), MA0051 4 (biri 361 satırlık migration, biri 220 satırlık `AddCoreIdentityServer`), diğerleri 7. Timeout eklenince MA0023 (gereksiz yakalama grubu) çıktı; `ExplicitCapture` çözdü. Meziantou'nun code fixer'ı (`dotnet format analyzers --diagnostics MA0002 MA0006`) 144 yeri `Ordinal` yaptı; üç yerde satır kırılımını bozdu (elle düzeltildi). Tek sıralama yeri `AdminEndpoints`'teki `OrderBy` (eskiden kültüre duyarlıydı). `PathString.StartsWithSegments`'in varsayılanı `OrdinalIgnoreCase`; MA0074 düzeltmesi bunu açıkça yazar.
- `package-guard.js` hook'u `GlobalPackageReference`'ı yakalamadı (Meziantou eklenirken onay istemedi).
- Roslynator CLI 1.0.1 (`roslynator.dotnet.cli`, Apache-2.0): `find-symbol --unused --visibility internal private --ignore-generated-code` `.slnx`'i yüklüyor, ~30 sn. Bütün türlerle 101 sembol, neredeyse hepsi yanlış pozitif: DI'ın çağırdığı kurucular, EF'in özel kurucuları, JSON'un doldurduğu DTO özellikleri, method group olarak verilen minimal API handler'ları (IDE0051 bunları kullanılıyor sayıyor). `--symbol-kind type field const enum-field event` ile 2 sembol: reflection'la uygulanan iki EF yapılandırma sınıfı. CA1812'nin raporladığı 59 sınıfın hiçbiri yok (DI kaydı tipe referans). Deneme sınıfı (kullanılmayan internal sınıf) raporda göründü.
- Kaynak: https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md · https://github.com/meziantou/Meziantou.Analyzer (README, docs/Rules/MA0002, MA0004, MA0009, MA0048, MA0051) · https://www.nuget.org/packages/Meziantou.Analyzer · https://github.com/dotnet/roslynator (`src/CommandLine/Options/FindSymbolCommandLineOptions.cs`, `roslynator find-symbol --help`) · ölçüm
- Sonuç: BannedApi `src/` projelerinde (saat girdileri Abstractions hariç), Meziantou varsayılan önemleriyle ve Q-50'nin üç kapatmasıyla, Roslynator haftalık rapor (`eng/find-unused.sh`, `unused-code.yml`). D-066.

### R-120: SonarQube Cloud, Qodana Community for .NET ve CodeRabbit (2026-10-23, faz 6d; kaynak + ölçüm)
- Soru: F6d.4'ün SaaS analizleri bu repoya nasıl bağlanır (K4, D-045; Q-49)?
- SonarQube Cloud (kaynak): .NET için otomatik analiz coverage almıyor ve CI tabanlı analizle birlikte açık kalırsa CI analizi düşüyor; projede kapatılmalı (Administration → Analysis Method). CI'da SonarScanner for .NET (`dotnet-sonarscanner`, 12.0.0 2026-10-15, 7 günlük cooldown'u geçmiş; repo lisansı LGPL-3.0, indirdiği analizörler SSALv1): `begin /k /o /d:sonar.token` → build → `end`; checkout `fetch-depth: 0`. Coverage: Coverlet'in Cobertura'sı `sonar.cs.cobertura.reportsPaths`. `begin` adımı build'in uyarılarını hata saymayı kapatıyor, bu yüzden ayrı job. Bölge EU varsayılan (`sonarcloud.io`); public repo ücretsiz (OSS planı). Proje ve organizasyon anahtarı içe aktarmada belirleniyor; workflow `alimuhammedak_NovaCore` / `alimuhammedak` varsayıyor (kullanıcı doğrulayacak).
- zizmor'un `auditor` personası `secrets-outside-env` uyarısı veriyor (token'ın bir environment'ta olması önerisi); repo'nun varsayılan personası göstermiyor, şablonun CI'ı da repo secret'ı kullanıyor. Değiştirilmedi.
- Qodana Community for .NET (kaynak): proje token'ı Community linter'ı için opsiyonel (yalnız Qodana Cloud'da rapor görmek için); "currently in Early Access"; çözüm `dotnet: solution:` ya da `--solution`; varsayılan profil `qodana.starter`; farklı SDK için belgelenen `bootstrap` (dotnet-install `--jsonfile global.json`).
- Qodana ölçümü (yerel Docker, `jetbrains/qodana-cdnet:2026.2`, 2026.2.699, token'sız, geçici bir worktree'de `qodana.yaml` ile; repoya girmedi): `bootstrap` SDK 10.0.300'ü kurdu, `.slnx` ve .NET 10 analiz edildi, 12,3 dk, exit 0. 220 sonuç: 89 warning, 131 note. Warning'lerin çoğu yanlış pozitif: `NotAccessedPositionalProperty` (JSON ile serileşen DTO'lar, ~45), `CollectionNeverUpdated`/`UnusedAutoPropertyAccessor` (configuration'a bağlanan options), `UnusedMember.Local` (EF'in özel kurucuları), `InconsistentNaming` (`KnownIPNetworks`, framework'ün adı). Gerçek ve küçük olanlar: testlerde 8 `RedundantUsingDirective` (IDE0005 testlerde çalışmıyor), testlerde 2 `InvalidXmlDocComment` (belirsiz ve çözülemeyen `cref`), `LoginPage.cs:183` gereksiz `!`, `ProductCatalog`'un namespace'i klasörüyle uyuşmuyor, 4 `UsingStatementResourceInitialization` (testler). Note'ların çoğu stil önerisi.
- CodeRabbit: GitHub uygulaması olarak kurulur ve yalnız pull request'leri inceler; repoda yapılandırma gerekmiyor (varsayılanlar). Bu repoda faz merge'leri yerel olduğu için bugün yalnız Dependabot PR'larını görür.
- Kaynak: https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/ci-based-analysis/github-actions-for-sonarcloud/ · https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/ci-based-analysis/sonarscanner-for-dotnet/introduction/ · https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/automatic-analysis/ · https://docs.sonarsource.com/sonarqube-cloud/analyzing-source-code/test-coverage/dotnet-test-coverage/ · https://github.com/SonarSource/sonar-scanner-msbuild (lisans) · https://www.jetbrains.com/help/qodana/qodana-cdnet.html · https://www.jetbrains.com/help/qodana/pricing.html · https://www.jetbrains.com/help/qodana/docker-images.html · ölçüm
- Sonuç: Sonar CI'da (token varken), Qodana yalnız deneme koşusu: CI'a eklenmedi (yanlış pozitif oranı yüksek, mevcut analizörlerle örtüşüyor, Early Access, koşu ~12 dk); gerçek bulguları Sonraya bırakılanlar'da. CodeRabbit kullanıcının uygulama kurulumuyla.

### R-121: CodeRabbit'in tam kullanımı (2026-10-23, faz 6d; kaynak + ölçüm)
- Soru: R-120 CodeRabbit'i "uygulama kurulumu yeter" diye bıraktı; ilk koşu (PR #9) Dependabot'un seat'i olmadığı için inceleme yapmadı ve faz merge'leri yerel olduğu için faz kodu hiç incelenmiyordu. CodeRabbit bu repoda bütünüyle nasıl kullanılır?
- Yapılandırma: kökteki `.coderabbit.yaml`, PR'ın head (feature) branch'indeki sürümüyle okunur. Şema `https://coderabbit.ai/integrations/schema.v2.json`; dosya şemaya karşı doğrulandı (0 hata; dil kodu `tr`, `tr-TR` şemada yok). Önemli anahtarlar: `reviews.profile` (`chill`/`assertive`), `request_changes_workflow`, `auto_review` (`drafts`, `base_branches` regex, `ignore_usernames`), `path_filters`, `path_instructions`, `pre_merge_checks` (`docstrings`, `title`, `description`, `issue_assessment`, en çok 50 `custom_checks`; mod `off`/`warning`/`error`), `finishing_touches`, `tools.*`, `knowledge_base.code_guidelines.filePatterns`, `knowledge_base.learnings.scope`.
- Code guidelines: `**/CLAUDE.md`, `**/AGENTS.md` ve benzerleri varsayılan olarak okunur, her dosya kendi dizini ve altı için geçerli; eklenen desenler varsayılanların üstüne gelir, `{files, applyTo}` biçimi de var. CodeRabbit CLAUDE.md'nin `path_instructions`'a yazılmamasını söylüyor (dosyayı değişen kod gibi inceler). İzlenmeyen kök `AGENTS.md` (Q-40) commit'li olmadığı için okunmaz.
- `error` modu yalnız `request_changes_workflow: true` iken "changes requested" incelemesiyle merge'ü engelliyor; ayrı adı belgelenmiş bir status check yok.
- Araçlar: neredeyse hepsi varsayılan açık ve yalnız tanıdıkları dosyalar varsa koşuyor; bu repoya değenler actionlint, zizmor, shellcheck, yamllint, markdownlint, gitleaks (Betterleaks), trufflehog, osvScanner, oasdiff, checkov, trivy, semgrep/opengrep, ast-grep, languagetool. Listede C#/Roslyn aracı yok.
- Bot PR'ları: bot hesaplarına seat otomatik atanmıyor; public repolar için fiyat sayfası "ücretsiz inceleme" diyor, ama PR #9 seat isteyerek atlandı. Dependabot PR'larının incelenmesi panelde `dependabot[bot]`'a seat atanmasına bağlı (kullanıcı).
- Plan sınırları: özel pre-merge check'ler ve unit test/simplify finishing touch'ları Team ve üstü (kullanıcının planı Advanced).
- CLI (ölçüm): Windows x64'te yerel çalışıyor, `irm https://cli.coderabbit.ai/install.ps1 | iex` 0.9.0'ı `%LOCALAPPDATA%\Programs\coderabbit`'e kurdu (`coderabbit`, `cr`). `coderabbit review --help`: `--agent` (ajanlar için yapılandırılmış çıktı), `--uncommitted`, `--committed`, `--include-untracked`, `--base`, `--deep` (tam PR politikası), `-c` (ek talimat dosyası), `--usage`. Oturum `coderabbit auth login` ile kullanıcıda. Claude Code eklentisi `claude plugin install coderabbit` ile kuruldu (`coderabbit@claude-plugins-official`, kullanıcı kapsamı).
- Kaynak: https://docs.coderabbit.ai/reference/configuration · https://docs.coderabbit.ai/getting-started/yaml-configuration · https://docs.coderabbit.ai/knowledge-base/code-guidelines · https://docs.coderabbit.ai/tools · https://docs.coderabbit.ai/pr-reviews/pre-merge-checks · https://docs.coderabbit.ai/management/seat-assignment · https://docs.coderabbit.ai/finishing-touches · https://docs.coderabbit.ai/cli · https://docs.coderabbit.ai/cli/windows · https://docs.coderabbit.ai/cli/claude-code-integration · https://www.coderabbit.ai/pricing · ölçüm
- Sonuç: D-067. R-120'nin CodeRabbit sonucu bu kayıtla düzeltildi.

## CRM-Nominal geçişi araştırması (2026-10-23, faz dışı)

Bağlam: CRM-Nominal (çok kiracılı eğitim kurumu CRM'i, PostgreSQL) NovaCore üzerinde yeniden kurulacak. İlk analiz (Haiku) CRM'in NovaCore'dan isteklerini listeledi; bu kayıtlar onu Microsoft'un belgeleri ve iki reponun koduyla karşılaştırır. Ayrıntı ve seçenekler: [proposals/crm-cok-kiracililik.md](proposals/crm-cok-kiracililik.md). İncelenen: NovaCore `8a75754`, CRM-Nominal `master` @ `8f9802b`.

### R-122: Middleware sırası, claims transformation ve ilk analizin NovaCore'dan istekleri (2026-10-23; kaynak + kod)
- Soru: CRM'in kurum çözümü ve askı kontrolü için `UseCoreWeb`'e kimlik doğrulama ile yetkilendirme arasında bir middleware yeri gerekiyor mu? İlk analizin diğer istekleri doğru mu?
- Middleware sırası (MS): Authorization "Immediately after the authentication middleware". `WebApplication` `UseRouting`'i kullanıcının middleware'inden önce, `UseEndpoints`'i sona ekler; kullanıcının middleware'i ve endpoint'leri ikisinin arasına girer. Yani `UseCoreWeb`'den sonra eklenen bir middleware her endpoint'ten önce çalışır.
- `IClaimsTransformation.TransformAsync` (MS API): "this will be run on each AuthenticateAsync call". NovaCore'un izin çözümlemesi (`PermissionClaimsTransformation`) bu yüzden kimlik doğrulamanın içinde çalışıyor; araya konan bir middleware ona hiç yetişmez.
- `AuthenticationService`'in kurucusu (MS API) tek bir `IClaimsTransformation` alıyor. NovaCore kendininkini `AddScoped` ile kaydediyor (`AuthorizationExtensions.cs:25`): bir proje sonradan kendi dönüşümünü kaydederse NovaCore'unki çalışmaz, izinler çözülmez (her korumalı uç 403) ve token'daki `permission` claim'leri artık silinmez. Bugün bunu yakalayan bir açılış kontrolü yok; resolver eksikse açılışı durduran `PermissionResolverStartupCheck` var.
- Kurumun istekten bulunması (MS, Azure Architecture Center "Map requests to tenants"): alan adı, URL yolu, sorgu dizesi, başlık ve token claim'i seçenek olarak sayılıyor. Şart olan "Request validation": isteğin o kurum için yetkili olduğu her istekte doğrulanır. Yani "kurum yalnız token claim'inden" tek güvenli yol değil.
- Identity (MS): `RequireUniqueEmail`'in varsayılanı `false`; varsayılan modelde `NormalizedUserName` unique index'li (`UserNameIndex`), `NormalizedEmail` index'i unique değil (`EmailIndex`). Kimlik eklentisi `RequireUniqueEmail = true` ve `UserName = Email` kullanıyor (`IdentityServerHostingExtensions.cs:102`, `AccountEndpoints.cs:97`): e-posta veritabanında da global tekil.
- Kod: `OutboxDispatcher` her tur için (`:76`) ve her mesaj için (`:163`) boş bir DI scope açıyor, bağlam geri yüklenmiyor. `AuditableEntityInterceptor` `CreatedBy`'a `UserName ?? UserId` yazıyor; kimlik eklentisinde `name` claim'i e-posta. `AddCoreInfrastructure` `CustomizeProblemDetails`'i atıyor (`InfrastructureServiceCollectionExtensions.cs:27`); bir projenin aynı atamayı yapması onu ezer.
- KVKK okuma denetimi için yer (MS, Minimal API filters): filtrelerin kullanım alanları arasında "Log information about the request and response"; grup düzeyinde eklenir. Mediator'ün pipeline behavior'ının karşılığı endpoint filter.
- Kaynak: https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0 (Middleware order, Built-in middleware, Middleware added automatically by `WebApplication`) · https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authentication.iclaimstransformation.transformasync?view=aspnetcore-10.0 · https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authentication.authenticationservice.-ctor?view=aspnetcore-10.0 · https://learn.microsoft.com/azure/architecture/guide/multitenant/considerations/map-requests · https://learn.microsoft.com/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0 · https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-10.0 · kod
- Sonuç: Middleware yeri eklenmez, askı kontrolü kurum çözümüne girer (D-070 (5)). Tek `IClaimsTransformation` koruması ve `CreatedBy` = `sub` Faz 7b'de (F7b.7, D-071 (2)). Outbox bağlamı F7b.5.

### R-123: Veritabanı sağlayıcıları ve lisansları (2026-10-23; kaynak + NuGet + kod)
- Soru: NovaCore bir projeyi bir veritabanına bağlıyor mu? PostgreSQL'i bugün ne engelliyor?
- EF Core sağlayıcı listesi (MS): sağlayıcılar farklı kaynaklardan; "When considering a provider, be sure to evaluate quality, licensing, support, etc. to ensure they meet your requirements." İlişkisel sağlayıcılar `Microsoft.EntityFrameworkCore.Relational` üzerine kurulu; MS, `Relational`'ın yama sürümünün uygulamaya doğrudan eklenmesini öneriyor. Çok sağlayıcılı projede her sağlayıcı için ayrı migration seti (EF 9 breaking changes sayfasının önerisi, "Migrations with multiple providers").
- Lisanslar (NuGet nuspec, 2026-10-23): `Microsoft.EntityFrameworkCore.SqlServer` MIT; `Npgsql` 10.0.3 ve `Npgsql.EntityFrameworkCore.PostgreSQL` `PostgreSQL`; `Pomelo.EntityFrameworkCore.MySql` 9.0.0 MIT; `MySql.EntityFrameworkCore` 10.0.12 `GPL-2.0-only WITH Universal-FOSS-exception-1.0`; `Oracle.EntityFrameworkCore`, `MongoDB.EntityFrameworkCore` ve `FirebirdSql.EntityFrameworkCore.Firebird` kendi lisans dosyasıyla. CRM'in test paketi Shouldly `BSD-3-Clause`.
- NovaCore'un durumu (kod): `NovaCore.EntityFrameworkCore` yalnız `Microsoft.EntityFrameworkCore` ve `.Relational`'a bağlı, outbox tabloları adla eşleniyor; kimlik eklentisinin context'ini host kaydediyor (D-026 (2)). Kilit şablonda: sağlayıcıyı her modül kendisi seçiyor (şablonun `TodosModule.cs:29` `UseSqlServer`, `EnableRetryOnFailure`, şemalı migration geçmişi), ayrıca design-time factory'ler, host'un kimlik context'i, SqlClient'ın Pkcs pini (D-036 (8)) ve compose. Repo'nun modülleri de aynı (`CatalogModule.cs:32`). Testler SQLite ve SQL Server; şablonun ürettiği projede lisans kapısı yok.
- Mimari varsayım: her modül kendi şemasında (`HasDefaultSchema`). SQL Server, PostgreSQL ve Oracle'da şema var; MySQL/MariaDB'de şema bir veritabanı, SQLite şemayı yok sayıyor (MySQL davranışı ölçülmedi).
- Kaynak: https://learn.microsoft.com/ef/core/providers/ · https://learn.microsoft.com/ef/core/what-is-new/ef-core-9.0/breaking-changes · https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers · https://api.nuget.org (nuspec'ler) · kod
- Sonuç: D-068 (Y-47), Faz 7a.

### R-124: Çok kiracılılık: Microsoft'un rehberi ve kimlik modelleri (2026-10-23; kaynak + kod)
- Soru: Çok kiracılılığın hangi parçaları genel, hangileri CRM'e özgü? Kişi başına tek hesap mı, kurum başına hesap mı?
- EF Core (MS): tek veritabanında kurum kolonu için EF'in desteği global query filter; kurum kimliği context'e bir servisle verilir. Sayfa kendini "working practices" olarak tanımlıyor, "best practices" değil. Context havuzu kullanılırsa kurum her istekte ayrıca verilmeli (Advanced Performance Topics). EF 10'da adlı filtreler: `IgnoreQueryFilters(["SoftDeletionFilter"])` yalnız o filtreyi kapatır.
- Kimlik (MS, Azure Architecture Center "Identity considerations"): "Use a single identity for each person when possible"; kişi başına birden çok kimlik iş ve kişisel ayrımı ya da düzenleyici zorunluluk için; "Avoid storing credentials multiple times if you use per-tenant identities". Yetki verisi IdP'de ya da uygulama katmanında, kurum bilgisinin yanında tutulabilir.
- Kimlik yaklaşımları (MS, "Identity approaches"): (a) IdP token'a kurum claim'i ekler; bir kullanıcı birden çok kurumda ise girişte kurum seçilir, kurum değiştirmek yeni token ister; (b) "Application-based authorization": kimlik sistemi kurumdan habersizdir, token'da kurum yok, ayrı bir liste ya da veritabanı kurum erişimini tutar, uygulama doğrular. Anti-pattern'ler: "Self-administered identity systems" (kendi IdP'ni çalıştırmak; yönetilen IdP önerilir), "User and tenant conflation".
- Veri (MS, "Storage and data approaches", "Multitenancy and Azure SQL Database", "Azure Database for PostgreSQL"): satır düzeyi güvenlik SQL Database'te ve PostgreSQL'de var; "Many multitenant solutions don't use row-level security because of those complexities". Paylaşılan veritabanında kurum kimliği her istekte veritabanına taşınır ve havuzdan gelen bağlantıda yeniden kurulup doğrulanır. PostgreSQL'de bağlam oturum değişkeniyle. SQL Server örneğinde uygulama bağlantıyı açtıktan sonra `SESSION_CONTEXT`'i ayarlıyor.
- Yaşam döngüsü (MS, "Tenant life cycle"): devre dışı bırakma geçici bir durumdur (yeniden etkinleştirilebilir); ayrılmada saklama süresi, yeniden katılım ve silme.
- Kod: NovaCore'da her modülün kendi DbContext'i ve şeması var; modüller arası altyapı BuildingBlocks'ta (outbox interceptor'ı her modüle `AddModuleDbContext` ekliyor, "a module that forgot it would lose its events without an error", `PersistenceExtensions.cs:71`). CRM bugün Finbuckle 10.1.4 (`MultiTenantIdentityDbContext`, claim + route stratejileri), `"Sube"` adlı filtre, `KapsamInterceptor` (ADR-001…005) ile çalışıyor; RLS kabul edildi, uygulanmadı (ADR-004). Finbuckle'ın davranışları CRM'in ADR'lerinden alındı, burada denenmedi.
- Kaynak: https://learn.microsoft.com/ef/core/miscellaneous/multitenancy · https://learn.microsoft.com/ef/core/querying/filters · https://learn.microsoft.com/ef/core/performance/advanced-performance-topics · https://learn.microsoft.com/azure/architecture/guide/multitenant/considerations/identity · https://learn.microsoft.com/azure/architecture/guide/multitenant/approaches/identity · https://learn.microsoft.com/azure/architecture/guide/multitenant/approaches/storage-data · https://learn.microsoft.com/azure/architecture/guide/multitenant/service/sql-database · https://learn.microsoft.com/azure/architecture/guide/multitenant/service/postgresql · https://learn.microsoft.com/sql/relational-databases/security/row-level-security · https://learn.microsoft.com/azure/architecture/guide/multitenant/considerations/tenant-life-cycle · kod (iki repo)
- Sonuç: Genel parçalar NovaCore'a isteğe bağlı bir paketle girer (D-069, Y-48); CRM'in kimlik modeli tek hesap + uygulamada üyelik (D-070 (3)). Faz 7b.

### R-125: Lisans kapısında adlı sağlayıcı istisnası (2026-10-23, faz 7a, F7a.1; ölçüm)
- Soru: Y-47'nin "projede adıyla kaydedilmiş sağlayıcı istisnası" `nuget-license` 4.0.18 ile nasıl yazılır; bir ad yalnız kendisini mi geçirir?
- Ölçüm (scratchpad'de `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 ve `Npgsql.NodaTime` 10.0.3'e referans veren deneme projesi, `--include-transitive --allowed-license-types "MIT;Apache-2.0"`): istisnasız üç paket kırmızı (`PostgreSQL` lisansı, exit 3); `--ignored-packages "...;Npgsql;Npgsql.EntityFrameworkCore.PostgreSQL"` ile yalnız `Npgsql.NodaTime` kırmızı (exit 1); yalnız `Npgsql` adıyla EF sağlayıcısı da kırmızı kalıyor (exit 2), yani tam ad önek gibi davranmıyor; üçü adlandırılınca yeşil (exit 0; `Npgsql.NodaTime`'un getirdiği `NodaTime` paketi Apache-2.0). Lisanslar nuspec'te ifade olarak geliyor (`License Information Origin: Expression`), ağ gerekmiyor.
- Npgsql 10.0.3 ve `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (2026-07-10) `PostgreSQL` lisanslı; sağlayıcı `Microsoft.EntityFrameworkCore(.Relational)` `[10.0.4, 11.0.0)`'a bağlı (NuGet nuspec).
- Kaynak: https://github.com/sensslen/nuget-license (README, `--ignored-packages`) · https://api.nuget.org (nuspec'ler) · ölçüm
- Sonuç: D-072. İstisna tam paket adlarıyla; `Npgsql*` deseni ailenin başka lisanslı bir eklentisini de geçirirdi.

### R-126: Sağlayıcıyı host'ta seçmek; migration'ların ve design-time factory'lerin yeri (2026-10-23, faz 7a, F7a.2; kaynak + ölçüm)
- Soru: Modüller sağlayıcı seçmeden her modül context'i host'un sağlayıcısını nasıl alır; modülün migration geçmişi tablosunu (`MigrationsHistoryTable`, sağlayıcının builder'ında) kim, nasıl ayarlar; sağlayıcıya özgü migration'lar nerede durur?
- EF Core (MS, "DbContext Lifetime, Configuration, and Initialization", EF 9+): `ConfigureDbContext` ile `AddDbContext` yapılandırmaları sırayla birleşir, çakışan seçeneklerde sonuncu kazanır; "Configuring a different provider will not remove the previous provider configuration": sağlayıcıyı değiştirmek için kaydın kendisi değiştirilmeli. Sağlayıcıya özgü seçenekler bağlantı dizesi olmadan da verilebilir (`UseSqlServer(sql => ...)`).
- `RelationalOptionsExtension` (MS API, `Microsoft.EntityFrameworkCore.Infrastructure`): her ilişkisel sağlayıcının seçenekleri; `WithMigrationsHistoryTableSchema` değiştirilmiş bir kopya döndürür ("It is unusual to call this method directly. Instead use DbContextOptionsBuilder"). EF Core kaynağı (`DbContextOptions<TContext>.WithExtension`, dotnet/dotnet VMR): uzantılar `extension.GetType()` anahtarıyla saklanır, yani taban tipten yapılan `AddOrUpdateExtension` sağlayıcının kendi uzantısının yerini alır. Birim testiyle doğrulandı: SQLite context'inin `MigrationsHistoryTableSchema`'sı modülün şeması, tek ilişkisel uzantı.
- Migration'lar (MS, "Migrations with Multiple Providers"): EF araçları yalnız etkin sağlayıcının migration'ını üretir; tek context tipiyle birden çok sağlayıcı "requires moving the migrations into a separate assembly". Design-time (MS, "Design-time DbContext Creation"): factory "the recommended pattern for a separate migrations project"; "Configure the same provider, model options, and migrations assembly at design time that the application uses at run time". EF araçları ortam ayarlı değilse Development kullanır (MS, "Applying Migrations").
- Kod: `--auth External` ile üretilen proje kendi `IPermissionResolver`'ını yazana kadar Development'ta `Build` edemiyor (ValidateOnBuild, D-024 (7)); Program'ı design-time'da çalıştırmak bu projelerde `dotnet ef`'i kırardı. Migration'lar modülde kalsaydı modül assembly'si SQL Server sağlayıcısına bağlı kalırdı (snapshot `SqlServerModelBuilderExtensions` çağırıyor) ve kimlik context'inin `MigrationsAssembly`'si (host) diğerlerinden (modül) farklı olurdu.
- Ölçüm: taşımadan önce ve sonra üç context'in `dotnet ef migrations list` ve `migrations script --idempotent` çıktıları birebir aynı; `has-pending-model-changes` temiz. SQLite migration geçmişinin şemasını yok sayıyor (`DatabaseMigrationServiceTests`: şemalı geçmiş tablosu `sqlite_master`'da şemasız).
- Kaynak: https://learn.microsoft.com/ef/core/dbcontext-configuration/#configuredbcontext-for-configuration-composition · https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.infrastructure.relationaloptionsextension · https://github.com/dotnet/dotnet/blob/main/src/efcore/src/EFCore/DbContextOptions%60.cs · https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers · https://learn.microsoft.com/ef/core/cli/dbcontext-creation · https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying
- Sonuç: D-073.

### R-127: NovaCore'un paketleri PostgreSQL'de (2026-10-24, faz 7a, F7a.3; ölçüm)
- Soru: Outbox (yazma, dağıtım, saklama silmesi), migration servisi ve kimlik eklentisi PostgreSQL'de, SQL Server'daki senaryolarla aynı davranıyor mu; sağlayıcıya göre neler değişiyor?
- Sürümler: `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (EF Core `[10.0.4, 11.0.0)`), `Npgsql` 10.0.3, `Testcontainers.PostgreSql` 4.16.0 (MsSql ile aynı), imaj `postgres:18.4-trixie` (7 günlük bekleme süresini geçen en yeni 18.x; 18.6 2026-10-21'de çıktı, Dependabot önerir). PostgreSQL 18 imajı veriyi `/var/lib/postgresql/18` altında tutar, volume üst dizindir (imajın 18 notu).
- Ölçüm (yerel Docker, ilk koşu): üç context'in PostgreSQL migration'ları (`dotnet ef migrations add Initial`, test projesindeki design-time factory'lerle) host'un `DatabaseMigrationService`'iyle uygulandı; her context tabloları ve `__EFMigrationsHistory` kendi şemasında, `public`'te tablo yok (Npgsql'in migration kilidi tablo bırakmıyor). Ödeme POST'u → outbox → Catalog'un stok ayırması → ödeme Completed, gerçek saatte; saklama silmesi SQL'de; ödeme listesinin `timestamp with time zone` sırası; `numeric(18,2)` sınır değerleri; host'ta OIDC ile giriş (Identity ve OpenIddict tabloları). 14/14 yeşil, SQL Server'daki aynı senaryolar da yeşil. Npgsql `DateTimeOffset` yazarken ofset 0 istiyor; kodun bütün zamanları `TimeProvider.GetUtcNow()` ya da `DateTimeOffset.UtcNow`'dan geliyor, sorun çıkmadı.
- Sağlayıcı farkı (modülün değil veritabanının): `EF.Functions.Like` kolonun collation'ını izler. SQL Server'ın varsayılanı büyük/küçük harfe duyarsız (aksana duyarlı), PostgreSQL'in varsayılanı deterministik: Catalog araması PostgreSQL'de büyük/küçük harfi ayırır (`PostgreSqlQueryTests`).
- Ham SQL: SQL Server'ın `[şema].[tablo]` ve `N'...'` biçimi PostgreSQL'de çalışmaz; tırnaksız ad PostgreSQL'de küçük harfe katlanır. Testler adları sağlayıcının `ISqlGenerationHelper`'ıyla tırnaklıyor, değerleri parametre geçiyor; `COUNT(*)` SQL Server'da `int`, PostgreSQL'de `bigint` (her ikisinde `CAST(... AS INTEGER)`).
- `.editorconfig`: `src/**/Migrations/*.cs` bir klasörün doğrudan altındaki dosyaları eşliyor; `Migrations/Catalog/` gibi bir alt klasör eşleşmiyordu. PG migration'ları host'un düzeninde (`PostgreSql/<Modül>/Migrations`), bölüm `{src,tests}/**/Migrations/*.cs`.
- Kaynak: ölçüm · https://hub.docker.com/_/postgres (18 notu, tag'ler) · https://www.npgsql.org/efcore/ · https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers
- Sonuç: D-074.

### R-128: Şablon motorunda sağlayıcı başına dosya seçimi (2026-10-24, faz 7a, F7a.4; kaynak + ölçüm)
- Soru: `--db` seçimiyle iki migration setinden yalnız seçileni, üretilen projede `Migrations` klasörüne nasıl koyarız (iki setin snapshot dosyasının adı aynı); `.sh` dosyasında `#if` çalışır mı?
- Kaynak (dotnet/templating wiki, "Reference for template.json"): `sources[].modifiers[]` yalnız `condition`, `include`, `exclude`, `copyOnly` alır; `rename` kaynak düzeyindedir ve "Each key is a path to a file". Sembollerin `fileRename`'i "The portion of template filenames to be replaced by the symbol value"; üretilmiş sembollerde 5.0.200'den beri, `join` örneği bir klasörü yeniden adlandırıyor. `constant` üreticisi `parameters.value` alır. MS Learn ("Templates for authors") `fileRename`'i parametreler için belgeliyor. Koşullu yorum sözdizimi belgesi `.sh`'ı "Single hash line comments" grubunda sayıyor: `#if (...)` varsayılan olarak işlenir.
- Ölçüm: paketlenmiş şablondan `--db PostgreSql` ile üretilen projede `eng/check-licenses.sh`'ın `#if` blokları işlendi; SQL Server migration klasörleri `exclude` ile dışarıda kaldı.
- Kaynak: https://github.com/dotnet/templating/wiki/Reference-for-template.json · https://github.com/dotnet/templating/wiki/Available-Symbols-Generators · https://github.com/dotnet/templating/blob/main/docs/Conditional-processing-and-comment-syntax.md · https://learn.microsoft.com/dotnet/core/tools/templates
- Sonuç: D-075 (2). PostgreSQL seti şablonda `MigrationsPostgreSql` klasörlerinde, sabit sembolün `fileRename`'iyle `Migrations` olur.
