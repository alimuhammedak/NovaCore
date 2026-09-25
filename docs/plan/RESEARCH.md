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
