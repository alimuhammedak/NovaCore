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

