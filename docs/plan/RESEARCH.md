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
