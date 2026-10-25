# İlerleme arşivi

> [PROGRESS.md](PROGRESS.md)'den taşınan kayıtlar (D-082): biten fazların adımları, eski devir notları, kapanmış "Sonraya bırakılanlar" maddeleri.
> İçerik değiştirilmeden taşındı. Her faz sonunda bir önceki fazın kayıtları buraya eklenir (son biten faz PROGRESS.md'de kalır); `/novacore-phase` bu dosyayı oturum başında okumaz.

## Adımlar

### Faz 0
- [x] F0.1 (849a30d, b87f5ea, 610163e): Baseline `849a30d` main'de, docker-compose.yml hariç (D-008). LICENSE (MIT) ve CHANGELOG eklendi. Compose SA parolasını `.env`'den okuyor, parola git geçmişinde yok. `.gitignore`'da değişiklik gerekmedi.
- [x] F0.2 (f865e89): ASP.NET Core ve EF paketleri 10.0.12'ye (dotnet-ef dahil), OTel 1.19.x'e, Scalar 2.17.9'a çıktı. İki yüksek açık (R-028) top-level güncellemeyle kapandı, transitive pinning gerekmedi. NU1903/NU1904 artık hata. `restore --force` temiz, build 0 uyarı, 33/33 test yeşil. Q-09 kapsamındaki paketlere dokunulmadı.
- [x] F0.3 (d732c15): "strict" artık `AddPolicy` + IP partition. `StrictRateLimitTests` gerçek `/auth/login`'e iki IP'den istek atıyor (D-009): IP-A 400 sonra 429 alıyor, IP-B 400 alıyor. Önce kırmızı görüldü. Düşük limit `CreateHost` host config'iyle veriliyor (R-026), bunun için `NovaCoreApiFactory`'den `sealed` kaldırıldı. Sürpriz: `TestServer.SendAsync` yolunda gövde algılanmıyor, test `IHttpRequestBodyDetectionFeature`'ı açıkça ayarlıyor. tests/CLAUDE.md ve CLAUDE.md'ye kalıp notu eklendi.
- [x] F0.4 (0fa6f9d): `IDomainEvent` üyeleri default gövdesiz property'ler oldu. Yeni `abstract record DomainEvent` değerleri `init` ile oluşturma anında sabitliyor. `DomainEventTests` önce kırmızı görüldü (iki okuma farklı Guid). BuildingBlocks/CLAUDE.md, README ve CHANGELOG'da (breaking notu) güncellendi.
- [x] F0.5 (bfcb282): `LoggingEmailSender` yalnızca `configuration[HostDefaults.EnvironmentKey] == Development` olduğunda kaydediliyor (Q-08, R-030). Yeni `EmailSenderStartupCheck` (IHostedService) Development dışında sender yoksa açılışı durduruyor. 3 test eklendi, Production/sender yok testi önce kırmızı görüldü. Development testi, factory'nin `UseEnvironment` çağrısının config'e erken ulaştığını doğruladı. `NovaCoreApiFactory`'ye `virtual EnvironmentName` eklendi. README, Identity/CLAUDE.md ve tests/CLAUDE.md güncellendi.
- [x] F0.6 (e826412): Scrutor, Asp.Versioning (`AddApiVersioning` dahil) ve `AddOutputCache`/`UseOutputCache` kaldırıldı. Paketler hook allowlist'inden de çıkarıldı. "Postgres" yorumu SQL Server oldu. README, CLAUDE.md ("0 warnings" + `restore --force` notu) ve Program.cs yorumları güncellendi, memory de güncellendi. Grep (docs/plan dışı) temiz, 38/38 yeşil.
- [x] F0.7 (d087b6c): 6 yeni test dosyası, 8 test: Y-01/32/33, Y-08, Y-17, Y-15, Y-29, Y-26, Y-31, Y-38. Hepsi yeşil. Her kural kasıtlı ve commit'lenmemiş bir ihlalle kırmızıya döndü, ayrıca captive singleton da denendi. `NovaCoreApiFactory.CreateHost` ValidateScopes ve ValidateOnBuild'i açıkça açıyor. Y-29 taraması `NovaCore.*` namespace'iyle sınırlı, çünkü `Microsoft.AspNetCore.OpenApi` generator'ü assembly'lerimize kendi file-local tiplerini gömüyor. Y-31 ApiExplorer ile uygulanabildi, kör noktası "Sonraya bırakılanlar"da. xUnit2029 uyarısı öneriye uyularak giderildi.
- [x] F0.8 (caf4271): `.github/workflows/ci.yml` (ubuntu, checkout@v7, setup-dotnet@v6) çalıştırıyor: restore (NU1903/4 hata), Release build, test, 3 context için `has-pending-model-changes`, JSON'dan açık kontrolü (R-027). Remote yok (Q-03). Komutlar yerelde bash ile yeşil, `jq` adımı PowerShell eşdeğeriyle doğrulandı.
- Faz sonu code review: 1 bulgu (test factory'lerinde `WithWebHostBuilder` ile parent factory dispose edilmiyordu, SQLite bağlantısı sızıyordu). Faz kapanış commit'inde düzeltildi.
- [~] F0.9 (koşullu) Identity minimal düzeltmeleri. **Atlandı** (Q-04: Identity kullanılmayacak)

### Faz 1
Başlangıçtaki deneme derlemesi (R-036): packable projelerde CS1591 132 (Abstractions 67, Infrastructure 22, Web 43, Catalog.Contracts 0), CA1848/CA1873 38, CA1707 42 (testler), CA1000 3, CA1305 3, CA1859 2, CA1711/CA1716/CA1725/CA1822/CA1816 1'er. IDE uyarısı yok. F1.2 bu yüzden alt adımlara bölündü (PLAN F1.2 izin veriyor). TreatWarningsAsErrors F1.2f'ye kaydı (Q-10).

- [x] F1.1 (e947640): `AnalysisLevel=latest-recommended` ve `EnforceCodeStyleInBuild=true` açıldı, TWAE F1.2f'ye kadar false (D-014). `WarningsNotAsErrors=NU1901;NU1902` (D-011).
  - **Sapma:** `GenerateDocumentationFile` targets'ta etkisiz (R-040) → 4 packable csproj'a taşındı (Q-18, D-014). `ContinuousIntegrationBuild` props'ta.
  - Yeni `Directory.Build.targets`: paket metadata'sı (MIT, `src/PACKAGE.md` → README.md, repo, snupkg, package validation) ve "packable ama doc yok" koruması. Koruma önce kırmızı görüldü.
  - `CS1591` artık hiçbir yerde bastırılmıyor (packable olmayanlar doc üretmediği için gerek yok).
  - Release build 225 uyarı, deneme envanteriyle birebir aynı (R-036). 46/46 yeşil.
  - OpenAPI belgesine XML açıklamaları giriyor (R-041, Q-19: kabul).
- [x] F1.2 Uyarıların düzeltilmesi (alt adımlar aşağıda): 225 uyarı → 0, TWAE açık.
  - [x] F1.2a (8905aea): Core.Abstractions 0 uyarı.
    - 67 XML doc eklendi. Doc'lardaki iddialar kodla karşılaştırıldı (HTTP eşlemesi, audit damgası, cache varsayılanları).
    - CA1000: `Result<T>.Success/Failure` internal oldu, `PagedList<T>.Empty` silindi (D-012).
    - CA1711/CA1716 gerekçeli `[SuppressMessage]` ile bastırıldı (D-011).
    - Toplam uyarı 225 → 153. 46/46 yeşil.
  - [x] F1.2b (0f85b4c): Core.Infrastructure 0 uyarı.
    - 22 XML doc eklendi, interface implementasyonlarında `<inheritdoc />`.
    - `.editorconfig`: CA1848/CA1873 = `suggestion`, "F2b.6'da sil" yorumuyla (D-011). Identity'deki 32 uyarıyı da kapsıyor.
    - Toplam uyarı 153 → 93. 46/46 yeşil.
  - [x] F1.2c (a264e70): Core.Web 0 uyarı.
    - 43 XML doc eklendi.
    - CA1305 (Serilog `Console`/`Seq`): planda `CultureInfo.CurrentCulture` yazıyordu, bunun yerine açık `formatProvider: null` kullanıldı.
      - Neden: `null` Serilog'un kendi varsayılanı ve kültürü render anında çözer. `CurrentCulture` ise kültürü açılış anında sabitlerdi, bu da ince bir davranış farkı olurdu.
      - CA1305 açık `null`'ı kabul ediyor (derlemeyle doğrulandı).
    - Toplam uyarı 93 → 48. 46/46 yeşil.
  - [x] F1.2d (772328f): Modüller 0 uyarı.
    - `IdentityDbContext.OnModelCreating` parametresi `modelBuilder` → `builder` (CA1725, base imzasına uyum).
    - `has-pending-model-changes` (Identity) değişiklik görmüyor.
    - Identity'deki CA1848/CA1873 F1.2b'deki `.editorconfig` ayarıyla kapandı.
    - Toplam uyarı 48 → 47. Kalanların hepsi testlerde. 46/46 yeşil.
  - [x] F1.2e (599cf2a): Testler 0 uyarı.
    - `.editorconfig` `[tests/**.cs]` için CA1707 = none (D-011).
    - CA1859: iki yardımcı somut tip döndürüyor.
    - CA1822: `LoginAsync` static oldu.
    - CA1816: `AuthFlowTests` sealed oldu, bu yetti.
    - CA1305: `ToString(CultureInfo.InvariantCulture)`.
    - Hiçbir test silinmedi, zayıflatılmadı ya da skip edilmedi. Debug build 0 uyarı, 46/46 yeşil.
  - [x] F1.2f (83073e1): `TreatWarningsAsErrors=true`.
    - `restore --force` temiz.
    - `--no-incremental` Release ve Debug derlemeleri 0 uyarı, 0 hata.
    - 46/46 yeşil.
- [x] F1.3 (9ae66b1): `VersionPrefix=0.1.0` (props). `dotnet pack` → `*.0.1.0.nupkg`, `--version-suffix preview.1` → `*.0.1.0-preview.1.nupkg` (4 paket + 4 snupkg). Ek paket yok.
  - Gözlem: çözüm genelinde pack, `NovaCore.Api` için kodsuz bir uyarı veriyor ("packaging has been disabled"). Web SDK `WarnOnPackingNonPackableProject=true` ayarlıyor, modüllerde ve testlerde bu değer false. Pack başarısız olmuyor. Bastırılmadı (Y-10), "Sonraya bırakılanlar"a yazıldı.
- [x] F1.4 (3e83278): `Microsoft.CodeAnalysis.PublicApiAnalyzers` 5.6.0 (CPM) eklendi, 4 packable csproj'da `PrivateAssets=all`.
  - Paket `PublicAPI.*.txt`'yi kendisi eklemiyor (R-042). `AdditionalFiles` item'ları `Directory.Build.targets`'ta.
  - Shipped dosyaları yalnızca `#nullable enable` içeriyor. Tüm API Unshipped'te, `dotnet format` + RS0016 code fix ile üretildi: Abstractions 149, Infrastructure 53, Web 122, Contracts 22 satır.
  - **Sapma:** RS0026 → `ToHttpResult` 4 overload'a bölündü (Q-20, D-015). Çağrılar aynen derleniyor.
  - Kabul denemesi: geçici bir public üye RS0016 hatası verdi, commit'lenmedi.
  - Build 0/0, 46/46 yeşil.
- [x] F1.5 (ae0a3f8): `dotnet pack NovaCore.slnx -c Release -o artifacts` exit 0. 4 `.nupkg` + 4 `.snupkg` üretildi. Package validation hatasız. `artifacts/` gitignore'da, ağaç temiz.

  | Paket | lib/net10.0 | README.md | Lisans | Repository | snupkg |
  |---|---|---|---|---|---|
  | NovaCore.Core.Abstractions 0.1.0 | dll + xml | ✓ | MIT (expression) | git, url + commit SHA | pdb ✓ |
  | NovaCore.Core.Infrastructure 0.1.0 | dll + xml | ✓ | MIT | ✓ | pdb ✓ |
  | NovaCore.Core.Web 0.1.0 | dll + xml | ✓ | MIT | ✓ | pdb ✓ |
  | NovaCore.Modules.Catalog.Contracts 0.1.0 | dll + xml | ✓ | MIT | ✓ | pdb ✓ |

  - Authors `Muhammed Ali AK`.
  - PublicApiAnalyzers bağımlılık olarak pakete girmiyor (`PrivateAssets=all`).
  - ProjectReference'lar `0.1.0` bağımlılığına dönüşüyor.
  - Gözlem: `NovaCore.Core.Web` paketi FluentValidation, Serilog, OpenTelemetry ve Scalar'a bağımlı. Bu D-003 ile çelişiyor ama zaten F2a.2 ve F2c'nin işi, dokunulmadı.
  - Api için kodsuz pack uyarısı devam ediyor (F1.3 notu).
- [x] F1.6 (6ea455b): `.github/workflows/release.yml` yazıldı, **tetiklenmedi** (tag yok, push yok).
  - Akış: `v*` tag → checkout@v7, setup-dotnet@v6 → tag/VersionPrefix kontrolü → restore → build (`-p:VersionSuffix`) → test → pack (`--no-build`) → upload-artifact@v7 (nupkg + snupkg) → yalnızca `.nupkg` GitHub Packages'a (`--no-symbols --skip-duplicate`) (D-013, R-043).
  - Doğrulama:
    - PyYAML ayrıştırması ve yapı kontrolü geçti (trigger, permissions, 9 adım).
    - Tag adımı bash ile denendi: `v0.1.0` → suffix boş, `v0.1.0-preview.1` → `preview.1`, `v0.2.0` → hata ve exit 1.
    - Build/test/pack zinciri boş ve `preview.1` suffix'le yerelde çalıştırıldı: 0/0, 46/46. Paket ve DLL informational version tutarlı (`0.1.0-preview.1+<sha>`).
    - Push çalıştırılmadı. actionlint indirilmedi. GitHub'ın kendi doğrulaması ilk push'ta görülecek.
- Dokümanlar (0948d14): root CLAUDE.md (komutlar, standartlar, gotcha'lar), src/BuildingBlocks/CLAUDE.md, tests/CLAUDE.md, README (proje ağacı, dağıtım, paket tablosu), CHANGELOG (Added/Changed/Removed) ve memory `novacore-stack` güncellendi.
- Faz doğrulaması (2026-09-25):
  - `restore --force` temiz.
  - `build -c Release --no-incremental`: 0 uyarı, 0 hata.
  - `test`: 46/46.
  - `pack -o artifacts`: exit 0, 8 dosya. Api için kodsuz uyarı var (F1.3 notu).
  - `list package --vulnerable`: 0.
  - 3 context'te `has-pending-model-changes`: değişiklik yok.
- Faz sonu code review (xhigh), 9 bulgu:
  - Düzeltildi (2aa4d60), hepsi `release.yml` ve `ResultHttpExtensions` içinde:
    - (1) Tag suffix'i `${{ }}` ile doğrudan script'e giriyordu (script injection). Artık env ile geçiyor.
    - (2) Suffix doğrulanmıyordu: `v0.1.0-` stable yayınlıyordu. Artık SemVer prerelease regex'i var. 8 tag durumu bash ile denendi, `$(…)` içeren tag reddediliyor.
    - (3) `VersionPrefix` okunurken tüm stdout yakalanıyordu. Artık `tail -n 1`.
    - (4) checkout `persist-credentials: false`.
    - (5) `ToHttpResult(Result<T>, Func)` bölünürken null tolerance kaybolmuştu. Parametre yeniden `Func<T, IResult>?`, `?? Results.Ok` davranışı birebir geri geldi.
  - CHANGELOG'a not düşüldü: `Result<T>.Failure(error)` artık `Result.Failure`'a bağlanıyor (kodda değişiklik yok, D-012'nin sonucu).
  - "Sonraya bırakılanlar"a yazıldı: Stop hook'un izlemediği dosyalar, PublicApiAnalyzers referansının targets'a taşınması, template'e giren paket kimliği.

### Faz 2a
Plan onayı (2026-09-25): kullanıcı kararları Q-21 (D-018), Q-22 (D-019), Q-23. Model kararları D-020, araştırmalar R-044…R-046. Başlangıçta main'deki `/openapi/v1.json` faz sonu karşılaştırması için dışarıya kaydedildi (commit'lenmedi). Taban: build 0 uyarı, 46/46.

- [x] F2b.6 (194b754, öne alındı, D-017): 6 sınıf `[LoggerMessage]`'a çevrildi, `.editorconfig`'teki geçici CA1848/CA1873 satırları silindi. Seviye ve mesaj şablonları aynı, EventId'ler açık ve assembly içinde benzersiz (R-044).
  - **Sürpriz:** CA1873 yalnızca `UserRegistered`'daki `Mask(email)` argümanını işaretledi (Information seviyesi; kural varsayılan olarak Information ve altını denetliyor). `IsEnabled` guard'ı instance ve static `[LoggerMessage]` metotlarında tanınmadı, çünkü analyzer guard ile log çağrısının aynı alıcıda (receiver) olmasını istiyor (R-047). Çözüm: audit log tanımları `this ILogger` extension biçiminde (`SecurityAuditLog`). Bastırma yok.
  - Kök CLAUDE.md'ye Standards → "Logging is source-generated" maddesi eklendi, Gotchas'taki CA1848 maddesi silindi. CHANGELOG'a "Changed" notu yazıldı. `--no-incremental` build 0/0, 46/46.
- [x] F2a.1 Çekirdek projeler (alt adımlar: 1a Abstractions, 1b EntityFrameworkCore, 1c AspNetCore, 1d Catalog.Contracts paketlenmez)
  - [x] F2a.1a (975e19c): `NovaCore.Core.Abstractions` → `NovaCore.Abstractions` (`git mv`). Namespace kökü, using'ler, proje referansları, slnx ve `release.yml`'deki VersionPrefix yolu güncellendi. PublicAPI satırlarında yalnızca namespace değişti, analyzer kabul etti. 46/46.
    - **Sürpriz:** yeniden adlandırmadan sonra test ve Api çıktı klasörlerinde eski `NovaCore.Core.Abstractions.dll` kalıyor, `Solution.ProductionAssemblies` bunu da yüklerdi. Her adlandırmada `bin/obj` silindi. CI temiz checkout kullandığı için etkilenmiyor.
  - [x] F2a.1b (8d5811a): Yeni `NovaCore.EntityFrameworkCore`: `AddModuleDbContext`, `AuditableEntityInterceptor`, `DatabaseMigrationService`, `DomainEventDispatcher` buraya taşındı. ASP.NET referansı yok. `Microsoft.Extensions.Hosting.Abstractions` ve `Microsoft.Extensions.Configuration.Binder` 10.0.12 CPM'e eklendi (R-045). Binder, bayrağın bugünkü gibi `GetValue<bool>` ile okunması için gerekli.
    - Yeni `builder.AddCorePersistence()` interceptor'ı, dispatcher'ı ve bayrağa bağlı migration servisini kaydediyor. `Program.cs` bunu modüllerden önce çağırıyor, böylece hosted service sırası (migration → seeder) korunuyor.
    - `dotnet format` RS0016 code fix'i yeni proje için PublicAPI satırı yazmadı ("workspace" uyarısı verdi). Tek yeni satır analyzer mesajındaki imzadan elle eklendi, taşınan 25 satırda yalnızca namespace değişti. 46/46.
  - [x] F2a.1c (4faa61c): `NovaCore.Core.Web` → `NovaCore.AspNetCore` (`git mv`). Core.Infrastructure'da kalanlar (Caching, DependencyInjection, Exceptions, Http) bunun içine taşındı, Core.Infrastructure silindi. AspNetCore artık doğrudan Abstractions'ı referans alıyor, EntityFrameworkCore'u hiç almıyor. İki PublicAPI listesi birleştirildi, yalnızca namespace değişti. 46/46.
  - [x] F2a.1d (08817be): `Catalog.Contracts` artık paketlenmiyor (D-020 (5)). PublicAPI dosyaları, analyzer referansı ve doc üretimi kaldırıldı. Contracts tipleri endpoint imzalarında olmadığı için OpenAPI belgesi değişmiyor.
  - F2a.1 kabul kriteri: build 0/0, 46/46, `has-pending-model-changes` üç context'te de "No changes".
- [x] F2a.2 Eklenti paketleri (alt adımlar: 2a Serilog, 2b OpenTelemetry, 2c Scalar, 2d FluentValidation köprüsü, D-019)
  - [x] F2a.2a (a546bc5): Yeni `NovaCore.Serilog` (`SerilogHostingExtensions.AddCoreLogging`). `AddCoreDefaults` artık logging kurmuyor. `UseCoreWeb` artık `UseSerilogRequestLogging` çağırmıyor, `Program.cs` onu `UseCoreWeb`'den hemen önce çağırıyor (security headers ile sırası yer değişti, gözlenebilir HTTP farkı yok). `formatProvider: null` korundu (D-020 (8)).
    - `Directory.Build.targets`'a `NovaCoreRequirePublicApiTracking` koruması eklendi. Serilog csproj'undan referans geçici olarak silinince build hata verdi (commit'lenmedi). 46/46.
  - [x] F2a.2b (af888ea): Yeni `NovaCore.OpenTelemetry` (`OpenTelemetryHostingExtensions.AddCoreObservability`). `AddCoreDefaults`'ta artık yalnızca health ve resilience var. `service.version` hâlâ NovaCore assembly sürümü (0.1.0.0), artık eklentiden okunuyor. 46/46.
  - [x] F2a.2c (3f4efcf): Yeni `NovaCore.Scalar` (`app.MapCoreScalar()`, yalnızca Development, başlık ve tema aynı). `UseCoreWeb` yalnızca OpenAPI belgesini map'liyor.
    - Yeni test `Scalar_ui_is_served_in_development`: taşımadan önce yeşildi. Scalar çekirdekten çıkıp host henüz bağlanmamışken kırmızıya döndü, bağlandıktan sonra yeniden yeşil. 47/47.
  - [x] F2a.2d (d308fc4): `ValidationFilter<T>` ve `ValidationExceptionHandler` paketlenmeyen `NovaCore.FluentValidation` projesine taşındı (D-019). Handler'ı host `AddValidationExceptionHandler()` ile kaydediyor; bu metot kaydı listenin başına ekliyor (R-046).
    - Yeni test `Validation_exception_handler_runs_before_the_global_handler`: taşımadan önce yeşildi, kayıt sona eklenecek şekilde değiştirilince kırmızıya döndü (commit'lenmedi). 48/48.
  - F2a.2 kabul kriteri: `NovaCore.AspNetCore`'un doğrudan paketleri artık yalnızca `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.Caching.Hybrid` (ve `PrivateAssets=all` PublicApiAnalyzers). Serilog, OpenTelemetry, Scalar ve FluentValidation çekirdekte yok.
- [x] F2a.3 (6f7828e): `ModuleLoader` ve `AddModules()` silindi, yerine `builder.AddModule<TModule>()` geldi. `IModule.IsEnabled` kaldırıldı (D-018). `Program.cs` modülleri eski sırayla ekliyor: Catalog, Identity, Payments.
  - Yeni `ModuleRegistrationTests` modül sırasını ve 24 modül endpoint'ini (HTTP metodu + route) sabitliyor. Liste, reflection sürümü çalışan host'tan çıkarıldı. Test o sürüme karşı yeşildi, geçişten sonra da yeşil. Sıra değiştirilince iki test de kırmızıya döndü (commit'lenmedi).
  - Modüllerin XML yorumlarındaki "otomatik keşfedilir" ifadeleri düzeltildi. `Program.cs` bölümlere ayrıldı (eklentiler / çekirdek / modüller). 50/50.
- [x] F2a.4 (c3d599e) Mimari testler
  - `PackageDependencyTests` (Y-05, D-003, ANALYSIS §4), 5 test:
    - Çekirdek csproj'larda doğrudan paketler yalnızca `Microsoft.*`/`System.*` (`PrivateAssets=all` hariç).
    - Çekirdek assembly'ler yalnızca Microsoft/BCL assembly'lerine ve Abstractions'a bağlanıyor. Bu, transitive olarak gelen bir üçüncü taraf tipinin (ör. Polly) kodda kullanılmasını yakalar.
    - Çekirdek projeler Abstractions dışında birinci taraf projeye referans vermiyor (Abstractions hiçbirine).
    - Abstractions ve EntityFrameworkCore'da `FrameworkReference` yok. Abstractions yalnızca `System.*`'a, EntityFrameworkCore hiçbir `Microsoft.AspNetCore*`'a bağlanmıyor.
    - Eklentiler (çekirdek dışındaki packable building block'lar, listeye eklemeye gerek yok) yalnızca AspNetCore ve Abstractions'ı referans alabiliyor.
  - `ExplicitRegistrationTests` (Y-20, çekirdek kısmı): `src/BuildingBlocks` kaynaklarında `Assembly.Load*`, `GetAssemblies()`, `GetAssemblyName`, `AssemblyLoadContext`, `GetTypes()`, `GetExportedTypes()` yok.
  - Her kural kasıtlı bir ihlalle kırmızıya döndürüldü (commit'lenmedi): AspNetCore'a Scalar paketi, AspNetCore → EF referansı, AspNetCore'da `typeof(Polly.ResiliencePipeline)`, `AppDomain.GetAssemblies()`, EF'e ASP.NET framework'ü, Scalar → EF referansı.
    - EF'e framework eklendiğinde restore NU1510 ile düştü: Binder ve Hosting.Abstractions o zaman framework'ten geleceği için "prune" edilir. Bu, ASP.NET'siz tasarımın (D-020 (1)) bu iki paketi neden açıkça istediğini de doğruluyor.
  - Tüm testler 56/56.
- [x] F2a.5 (13d6e64) Dokümanlar
  - README: mimari diyagramı, bağımlılık kuralları, çekirdek/proje tablosu, proje ağacı (`NovaCore.sln` yazıyordu, `.slnx` oldu), `Program.cs`, "Yeni modül ekleme" (artık `Program.cs`'e bir satır), cross-cutting tablosu, test, dağıtım (6 paket), paket/lisans tablosu, notlar.
  - Kök CLAUDE.md: Architecture bölümü paket haritasına göre yeniden yazıldı. Standards (Abstractions, Result, packable listesi, PublicApiAnalyzers koruması), Gotchas (erken okunan config artık iki metotta; modül öneki; yeniden adlandırmadan sonra eski DLL'ler) ve pack komutu güncellendi.
  - `src/BuildingBlocks/CLAUDE.md` (çekirdek, eklentiler, paket olmayan köprü), `src/Modules/CLAUDE.md` (modül ekleme adımları), `tests/CLAUDE.md` (yeni mimari ve host testleri), `src/PACKAGE.md` (paket tablosu), CHANGELOG.
  - Memory: `novacore-stack`, `novacore-project`, `novacore-security` ve `MEMORY.md` indeksi.
  - Kabul kriteri: `docs/plan` ve CHANGELOG'daki geçmiş kayıtlar dışında `Core.Web`, `Core.Infrastructure`, `Core.Abstractions`, `NovaCore.Core`, `ModuleLoader` ve `AddModules` geçmiyor (git grep, memory dahil).
- [x] F2a.6 (opsiyonel) İlk önizleme sürümü: yalnızca yerel pack (Q-23). Tag ve push yapılmadı.
  - `dotnet pack NovaCore.slnx -c Release -o artifacts --version-suffix preview.1`: 6 `.nupkg` + 6 `.snupkg`, exit 0. `artifacts/` gitignore'da.

    | Paket 0.1.0-preview.1 | lib/net10.0 | README | Lisans | Repository | nuspec bağımlılıkları | snupkg |
    |---|---|---|---|---|---|---|
    | NovaCore.Abstractions | dll + xml | ✓ | MIT | git + commit | yok | pdb ✓ |
    | NovaCore.AspNetCore | dll + xml | ✓ | MIT | ✓ | NovaCore.Abstractions, AspNetCore.Authentication.JwtBearer, AspNetCore.OpenApi, Extensions.Caching.Hybrid, Extensions.Http.Resilience | pdb ✓ |
    | NovaCore.EntityFrameworkCore | dll + xml | ✓ | MIT | ✓ | NovaCore.Abstractions, EntityFrameworkCore(.Relational), Extensions.Configuration.Binder, Extensions.Hosting.Abstractions | pdb ✓ |
    | NovaCore.Serilog | dll + xml | ✓ | MIT | ✓ | Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.Seq | pdb ✓ |
    | NovaCore.OpenTelemetry | dll + xml | ✓ | MIT | ✓ | OpenTelemetry.* (5) | pdb ✓ |
    | NovaCore.Scalar | dll + xml | ✓ | MIT | ✓ | Scalar.AspNetCore | pdb ✓ |

  - Çekirdek paketlerin nuspec'lerinde yalnızca Microsoft paketleri ve `NovaCore.Abstractions` var. Üçüncü taraf bağımlılıklar yalnızca eklentilerde. Modüller ve `NovaCore.FluentValidation` paketlenmedi. Api için bilinen kodsuz pack uyarısı devam ediyor (F1.3 notu).
- Faz doğrulaması (2026-09-25):
  - `restore --force`: temiz.
  - `build -c Release --no-incremental`: 0 uyarı, 0 hata.
  - `test`: 56/56 (code review düzeltmelerinden sonra 58/58).
  - `list package --vulnerable --include-transitive`: 13 projenin hepsi temiz.
  - `has-pending-model-changes` (3 context): değişiklik yok.
  - Çekirdek paketlerin tüm transitive kapanışında üçüncü taraf paket olarak yalnızca Polly var. `dotnet nuget why`'a göre tek yolu `Microsoft.Extensions.Http.Resilience` → `Microsoft.Extensions.Resilience` (D-020 (6)).
  - OpenAPI: faz başında main'den alınan `/openapi/v1.json` ile faz sonundaki belge **bayt bayt aynı** (29.460 bayt).
  - Smoke run (`dotnet run`, Development, DB'siz): `/alive`, `/health` 200; `/scalar/v1` 200; `/api/v1/products` 401 ve security header'lar yerinde. Konsolda her istek için Serilog `RequestLoggingMiddleware` satırı var.
- Faz sonu code review (xhigh), 9 bulgu:
  - Düzeltildi (review düzeltme commit'i):
    - (1) `AddModuleDbContext` artık `AuditableEntityInterceptor`'ı da `TryAddScoped` ile kaydediyor (`AddCorePersistence` de `TryAddScoped`). Böylece `AddCorePersistence` çağrılmayan ya da geç çağrılan bir host'ta modül DbContext'i ilk sorguda patlamıyor. F2a.1b'deki bölünmenin getirdiği yeni bir hata yolunu kapatıyor. Yeni test önce kırmızı görüldü ("No service for type AuditableEntityInterceptor").
    - (2) `AddModule<TModule>()` aynı modül ikinci kez eklenirse kayıt anında hata veriyor. Eski tarayıcı modülleri assembly adına göre tekilleştiriyordu. İkinci ekleme bütün endpoint'leri iki kez map'ler ve ilk istekte ambiguous route hatası verirdi. Yeni test önce kırmızı görüldü.
    - (3) `CoreWebExtensions` XML dokümanı hâlâ "OpenAPI + Scalar" ve "exception handlers" diyordu, düzeltildi.
  - Atlandı (aşağıda "Sonraya bırakılanlar"a yazıldı): namespace gölgeleme, Y-05 csproj testinin kör noktaları, test yardımcılarındaki tekrar, `LoginFailed`'da guard'sız maskeleme, DECISIONS'ta satır sırası.

### Faz 2b
Plan onayı (2026-09-25): kullanıcı kararı Q-24 (D-022, health uçları yalnızca Development). Model kararları D-021, araştırmalar R-048…R-051. F2b.1, F2b.1a (NovaCore.AspNetCore) ve F2b.1b (NovaCore.EntityFrameworkCore) olarak bölündü. Başlangıçta main'deki `/openapi/v1.json` faz sonu karşılaştırması için dışarıya kaydedildi (29.460 bayt, commit'lenmedi).

- [ ] F2b.1 Options pattern (alt adımlar: 1a AspNetCore, 1b EntityFrameworkCore)
  - [x] F2b.1a (a73cd47): `Jwt`, `RateLimiting`, `Cors` ve `Cache` artık `AddOptions<T>().BindConfiguration().ValidateOnStart()` ile kayıtlı. `JwtBearerOptions`, `RateLimiterOptions`, `CorsOptions` ve `HybridCacheOptions` bu options'lardan `Configure<IOptions<...>>` ile, ilk kullanıldıklarında yapılandırılıyor (R-048).
    - Doğrulama: `JwtOptions` ve `RateLimitingOptions` için `[OptionsValidator]` source-gen. Nitelikle ifade edilemeyen üç kural `.Validate` ile: UTF-8 anahtar uzunluğu, çıplak CORS origin'i, pozitif cache süresi. Development anahtarı `PostConfigure<IHostEnvironment>`'ta.
    - Public API (0.x, D-021): `AddCoreAuthentication`, `AddCoreInfrastructure` ve `AddCoreCaching` `IConfiguration` almıyor. `JwtTokenService` `IOptions<JwtOptions>` alıyor. Identity'deki `AuthTokenService` de öyle.
    - Yeni `ConfigurationValidationTests` önce kırmızı görüldü (12/12): geçersiz değerler host'u ayarın adını veren bir mesajla durduruyor, factory'nin sonradan eklediği config options'a ulaşıyor, Development'ta boş anahtar geliştirme anahtarına düşüyor.
    - **Zorunlu yan değişiklik:** `StrictRateLimitApiFactory` `ConfigureHostConfiguration` yerine `ConfigureAppConfiguration` kullanıyor. Tembel okumada `args` (host config) factory'nin kendi `ConfigureAppConfiguration` değerine yeniliyordu.
    - 70/70.
  - [x] F2b.1b (7d525a0): `DatabaseMigrationService` her zaman kayıtlı; `Database:ApplyMigrations`'ı `StartAsync`'te `IOptions<DatabaseOptions>`'tan okuyor. `ValidateOnStart` bool olmayan bir değeri açılış hatasına çeviriyor. Servis `internal` oldu (D-021 (2)).
    - EF paketinde `Microsoft.Extensions.Configuration.Binder` yerine `Microsoft.Extensions.Options.ConfigurationExtensions` 10.0.12 (CPM) var. Binder yalnızca `GetValue` için eklenmişti ve hâlâ transitive geliyor. `restore --force` temiz.
    - Yeni `DatabaseMigrationServiceTests`: bayrak `AddCorePersistence`'tan sonra eklenen config'ten de okunuyor. `true` vakası önce kırmızı görüldü. `yes` değeri host'u durduruyor.
    - 73/73.
  - F2b.1 kabul kriteri: `NovaCore.AspNetCore` ve `NovaCore.EntityFrameworkCore`'da `GetValue`, `Get<T>` ya da `GetSection` yok. Geriye yalnızca `IModule.AddModule`'ün `IConfiguration` parametresi kalıyor; o modüllere verilen bir sözleşme, okuma değil.
- [x] F2b.2 (3c3202f): `NovaCoreApiFactory`'deki iki `Environment.SetEnvironmentVariable` satırı silindi. Testler env var olmadan 73/73 yeşil. `git grep SetEnvironmentVariable -- tests src` boş.
  - `tests/CLAUDE.md`'deki "Config gotcha" bölümü "in-memory config is enough" olarak yeniden yazıldı. Farklı bir değer için subclass'ta `ConfigureWebHost` + `ConfigureAppConfiguration` kullanılıyor.
  - Kök `CLAUDE.md`: Gotchas'tan "Eager config reads" silindi. Standards'a config kuralı eklendi: doğrulanan options, kayıt anında okuma yok. Memory `novacore-security` güncellendi.
- [x] F2b.3 (9c3b0e5): `TimeProvider.System` tüketicinin kaydedildiği yerde `TryAddSingleton` ile kayıtlı: `AddCoreAuthentication`, `AddCorePersistence`, `AddModuleDbContext`. Audit interceptor, `JwtTokenService` ve Identity'deki token servisi, kayıt, seeder ve admin listesi saati buradan alıyor.
  - Public API (0.x, D-021 (5)): `AccessToken` `IssuedAtUtc` taşıyor. `ExpiresInSeconds` artık token'ın ömrü (`expires_in` 899 → 900). `JwtTokenService` ve `AuditableEntityInterceptor` `TimeProvider` alıyor.
  - `RefreshToken.IsExpired/IsActive` `now` parametreli metot oldu. `ApplicationUser` ve `RefreshToken` üzerindeki `CreatedAtUtc = UtcNow` varsayılanları kalktı, oluşturan kod set ediyor. `has-pending-model-changes` üç context'te "No changes".
  - Tek istisna: `DomainEvent.OccurredOnUtc` varsayılanı (D-021 (6)).
  - Testler (ilk ikisi önce kırmızı görüldü):
    - `SourceHygieneTests.Source_reads_the_time_from_TimeProvider`: 12 satır yakaladı. Mevcut TODO testiyle aynı dosya taraması paylaşılıyor.
    - `RefreshTokenExpiryTests`: `FakeTimeProvider` ile süre aşılınca 401 ve `auth.refresh_token_expired`.
    - `JwtTokenServiceTests` ve `AuditableEntityInterceptorTests`: önce derlenmedi, sonra yeşil.
  - Test paketi `Microsoft.Extensions.TimeProvider.Testing` 10.10.0 (MIT, yalnızca test projesi). 77/77.
- [x] F2b.4 (b2fe948): `GlobalExceptionHandler` `detail`'e yalnızca `AppException` mesajını yazıyor. `ArgumentException`, `FormatException`, `UnauthorizedAccessException` ve `KeyNotFoundException` 4xx durum kodunu koruyor ama mesaj yalnızca log'a gidiyor (Y-35). Status eşlemesi değişmedi.
  - ProblemDetails `instance` artık bir URI reference: `PathBase + Path` (`ToUriComponent`), query yok (R-051). Eskiden `"GET /path"` yazıyordu.
  - Yeni `GlobalExceptionHandlerTests` handler'ı `AddCoreInfrastructure`'ın ProblemDetails yapılandırmasıyla çalıştırıyor. 4xx'e eşlenen dört tip ve `instance` önce kırmızı görüldü; 500 ve AppException zaten doğruydu. 84/84.
- [x] F2b.5 (96bfe78): `AddModuleDbContext` her context için `AddDbContextCheck<TContext>` ekliyor ("ready" etiketi, ad = context tipi). Paket `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.12 (MIT, ASP.NET bağımlılığı yok, R-049).
  - `MapDefaultEndpoints` `/health` ve `/alive`'ı yalnızca Development'ta map'liyor (Q-24, D-022). Ortam `endpoints.ServiceProvider`'dan okunuyor, `MapModules` ile aynı dar altyapı noktası. Public imza aynı.
  - Test host üç modül context'ini de kendi in-memory SQLite'ına çeviriyor (`ReplaceWithSqlite<TContext>`, audit interceptor korunuyor, D-021 (8)). Mevcut `Health_endpoint_reports_healthy` testi değişmeden yeşil.
  - `HealthEndpointsTests`'e eklenenler (önce kırmızı görüldü): her modül context'i için "ready" kontrolü; Catalog var olmayan bir SQLite dosyasına bağlanınca `/health` 503 "Unhealthy"; Production'da `/health` ve `/alive` 404.
  - README, `tests/CLAUDE.md`, `src/BuildingBlocks/CLAUDE.md`, kök CLAUDE.md ve memory güncellendi. 88/88.
- [x] F2b.6 (194b754) LoggerMessage → Faz 2a'da yapıldı (D-017)
- Faz doğrulaması (2026-09-25):
  - `restore --force`: temiz, NU uyarısı yok.
  - `build -c Release --no-incremental`: 0 uyarı, 0 hata.
  - `test`: 88/88 (review düzeltmelerinden sonra 89/89).
  - `list package --vulnerable --include-transitive`: 13 projenin hepsi temiz.
  - `has-pending-model-changes` (3 context): değişiklik yok.
  - `git grep`: `src`'de `DateTime(Offset).UtcNow` yalnızca `IDomainEvent.cs:30` (D-021 (6)). `tests`/`src`'de `SetEnvironmentVariable` yok. Building block'larda `GetValue<`, `.Get<` ya da `GetSection(` yok.
  - OpenAPI: faz başında main'den alınan `/openapi/v1.json` ile faz sonundaki belge **bayt bayt aynı** (29.460 bayt). Handler'lara eklenen `TimeProvider` parametreleri servis olarak çıkarıldı.
  - Smoke run (Development, yerel SQL Server açık):
    - `/alive` 200, `/health` 200 Healthy (üç DB kontrolü gerçek SQL Server'a karşı), `/scalar/v1` 200, `/api/v1/products` 401.
    - Boş login gövdesi 400 ProblemDetails, `instance: "/api/v1/auth/login"`.
    - `RateLimiting__PermitLimit=0` ve `Cors__AllowedOrigins__0=https://app.example.com/` ile açılış iki mesajla durdu: `PermitLimit: The field RateLimitingOptions.PermitLimit must be between 1 and 2147483647.` ve `Cors:AllowedOrigins entries must be origins such as https://app.example.com...`.
- Faz sonu code review (xhigh), 10 bulgu:
  - Düzeltildi (8d59e7f):
    - (1) `Jwt:RefreshTokenDays` `int.MaxValue`'ya kadar kabul ediliyordu ama `now.AddDays` taşıyordu, doğrulama geçip ilk login patlıyordu. Artık üst sınır 36.500 ve buna bir test vakası var.
    - (2) Varsayılan kalkınca `CreatedAtUtc`'yi unutan bir oluşturma yeri sessizce 0001-01-01 yazardı. `ApplicationUser` ve `RefreshToken`'da artık `required`.
    - (3) Saat kuralındaki istisna artık dosyanın tamamını değil yalnızca o satırı kapsıyor. `TimeProvider.System.GetUtcNow()` da yakalanıyor. İstisna dosyasına eklenen geçici bir ihlal testi kırmızıya çevirdi (commit'lenmedi).
    - (4) Test factory'si EF internal provider'ını dispose ediyor ve artık okunmayan SQL Server connection string'ini set etmiyor.
    - (5) README'nin konfigürasyon bölümü doğrulama kurallarını anlatıyor. Boş `Cors:AllowedOrigins` "yalnızca dev'de" değil, her ortamda her origin'e izin veriyor.
  - Yanlış alarm: IDN origin'lerin reddedileceği iddiası. .NET 10'da `GetLeftPart` Unicode host'u koruyor (betikle denendi).
  - Atlandı ("Sonraya bırakılanlar"a yazıldı): Development'ta `BadHttpRequestException` → 500, DB health check'lerinde timeout yok, test yardımcılarındaki tekrar.

### Faz 2c
Plan onayı (2026-09-26): kullanıcı kararları Q-25 (boş Guid kuralı özel `ValidationAttribute` ile) ve Q-26 (`ExceptionHandlerOrderTests` silinir, Y-44 onayı). Model kararları D-023, araştırmalar R-052…R-054. Plan öncesinde repoya dokunmadan scratchpad'de doğrulananlar: modüller validation generator'ünü transitive framework referansıyla alıyor, yeni 400 yanıtının anahtarları ve alanları FluentValidation'ınkiyle aynı, EXTEXP0018 artık hiç üretilmiyor. main'in `/openapi/v1.json`'u faz sonu karşılaştırması için scratchpad'e kaydedildi (29.460 bayt, commit'lenmedi). F2c.1 alt adımlara bölündü: 1a test, 1b Catalog, 1c Payments, 1d Identity, 1e köprünün silinmesi.

- [x] F2c.1 Validation (alt adımlar: 1a karakterizasyon testi, 1b Catalog, 1c Payments, 1d Identity, 1e köprünün silinmesi). FluentValidation'ın yerini modül başına `AddValidation()` ve DataAnnotations aldı. 400 biçimi `ValidationTests` ile korunuyor.
  - [x] F2c.1a (e69a55c): Yeni `ValidationTests` (Theory, 13 satır). Kapsam: validate edilen her request şekli (Catalog 2, Payments 2, Identity 9).
    - Her satır geçersiz bir gövde gönderip 400 validation ProblemDetails'ı sabitliyor: content-type, `type`, `status`, dolu `title`, `instance` = yol, `traceId` ve `errors` anahtar kümesi birebir.
    - FluentValidation'a karşı yeşil. CreateProduct filtresi geçici kaldırılınca yalnızca o satır kırmızıya döndü (commit'lenmedi).
    - Yeni yardımcı `NovaCoreApiFactory.CreateClientWithClaims(...)`: token host'un kendi `ITokenService`'iyle imzalanıyor, kullanıcı seed'i gerekmiyor (D-023 (7)). tests/CLAUDE.md'ye yazıldı. 102/102.
  - [x] F2c.1b (2798a2b): `CreateProduct`/`UpdateProduct` kurallarını request record'larındaki DataAnnotations'ta tanımlıyor: `[Required]`, `[MaxLength]`, `[Range]`. Fiyat için `[Range]` `MinimumIsExclusive` ve anlaşılır bir `ErrorMessage` taşıyor.
    - `Validator` sınıfları ve `ValidationFilter` kalktı. `CatalogModule` kendisi `services.AddValidation()` çağırıyor (D-023 (1)). csproj'da FluentValidation ve köprü referansı yok.
    - Catalog satırları yeşil. `AddValidation()` geçici silinince tam o iki satır kırmızıya döndü (commit'lenmedi). 102/102.
  - [x] F2c.1c (f53fa72): `CreatePayment.Request`: `Quantity` için `[Range(1, int.MaxValue)]`, `ProductId` için private nested `NotEmpty` attribute'u (Q-25). Hata diğer alan hatalarıyla aynı yanıtta geliyor. `PaymentsModule` `services.AddValidation()` çağırıyor, FluentValidation ve köprü referansı yok.
    - İki Payments satırı yeşil. `AddValidation()` geçici silinince tam o iki satır kırmızıya döndü (commit'lenmedi). 102/102.
  - [x] F2c.1d (1743422): Identity'nin dokuz request record'u DataAnnotations taşıyor. `[Required]`, `[EmailAddress]`, `[MaxLength]` kullanılıyor; parola için `[Length(12, 128)]`, kodlar için `[Length(6, 8)]` (length-first politika yorumu record'un üstünde). Validator'lar ve filtreler kalktı, `IdentityModule` `services.AddValidation()` çağırıyor. Artık hiçbir modül FluentValidation'a referans vermiyor.
    - Dokuz Identity satırı ve `AuthGateTests` yeşil.
    - `AddValidation()` geçici silinince bunlar kırmızıya döndü. D-009 gereği login'in 400'üne dayandığı için `StrictRateLimitTests` de kırmızıya döndü (commit'lenmedi). 102/102.
  - [x] F2c.1e (03b9cae): `NovaCore.FluentValidation` silindi (D-019): `ValidationFilter<T>`, `ValidationExceptionHandler`, `AddValidationExceptionHandler`, slnx kaydı, Api referansı ve `Program.cs` çağrısı. `ExceptionHandlerOrderTests` kullanıcı onayıyla silindi (Q-26, Y-44). Yeniden adlandırma gotcha'sı gereği tüm `bin/obj` temizlendi.
    - `ExplicitRegistrationTests` (Y-20) artık tüm `src`'yi tarıyor. `*FromAssembly*` kayıt yardımcılarını da yakalıyor; EF'in `ApplyConfigurationsFromAssembly`'si hariç (D-023 (5)). İki kasıtlı ihlalle kırmızı görüldü (commit'lenmedi).
    - Dokümanlar: kök, BuildingBlocks, Modules, Identity ve tests CLAUDE.md, README, CHANGELOG (Unreleased net duruma göre düzeltildi), memory (`novacore-stack`, `novacore-project`). README lisans satırı F2c.4'e kaldı. 101/101.
- [x] F2c.2 (e0a4791): Yeni `RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData`. Attribute mevcut `PermissionRequirement`'ı taşıyor, handler değişmedi (D-023 (6), R-053).
  - `RequirePermission(...)` imzası aynı, artık bu attribute'u ekliyor. `AddCoreAuthorization` yalnızca handler'ı kaydediyor.
  - `PermissionPolicyProvider` (`perm:` policy adları) ve kullanılmayan `HasPermissionAttribute` silindi. PublicAPI güncellendi (0.x).
  - Yeni `PermissionAuthorizationTests` token'larla davranışı sabitliyor, önce policy provider'a karşı yeşil görüldü:
    - permission claim'i kendi ucunu açıyor (200), başka ucu açmıyor (403);
    - `Administrator` rolü her ucu açıyor;
    - izni olmayan kimliği doğrulanmış çağıran 403 alıyor.
  - Attribute requirement döndürmeyince korunan her uç düştü (commit'lenmedi).
  - Dokümanlar: README güvenlik bölümü, BuildingBlocks ve tests CLAUDE.md, CHANGELOG, memory `novacore-security`. 105/105.
- [x] F2c.3 (8712414): `ICacheService`, `CacheEntryOptions` (Abstractions) ve `HybridCacheService` (AspNetCore) silindi, tüketicisi yoktu. `AddCoreCaching` `HybridCache`'i kaydetmeye ve varsayılan süreyi `Cache:DefaultExpiration`'dan vermeye devam ediyor. Tüketiciler `HybridCache`'i doğrudan enjekte ediyor.
  - `EXTEXP0018` `NoWarn`'dan çıktı (R-054). Release ve Debug `--no-incremental` build: 0 uyarı, 0 hata.
  - `ConfigurationValidationTests`'e yeni vaka: `Cache:DefaultExpiration` `HybridCacheOptions.DefaultEntryOptions`'a ulaşıyor. Önce yeşil görüldü, bağlantı bozulunca kırmızıya döndü (commit'lenmedi).
  - PublicAPI (iki paket), README, BuildingBlocks ve kök CLAUDE.md (Gotchas), CHANGELOG ve memory güncellendi. 106/106.
- [x] F2c.4 (1dd53c4): `FluentValidation` ve `FluentValidation.DependencyInjectionExtensions` CPM'den ve `package-policy.json` allowlist'inden çıktı (hook onayıyla). README (giriş ve lisans tablosu) ve CHANGELOG güncellendi. `restore --force` temiz, `git grep -i fluentvalidation` kodda, props'ta ve policy'de boş. 106/106.
  - Doğrulama sırasında ~45 tam koşuda 2 kez aralıklı bir test hatası görüldü. Kök nedeni `Mvc.Testing`'deki bir framework yarışı, F2c'den önce de vardı (R-055, "Sonraya bırakılanlar").
- Faz doğrulaması (2026-09-26):
  - `restore --force`: temiz, NU uyarısı yok.
  - `build -c Release --no-incremental`: 0 uyarı, 0 hata.
  - `test`: 106/106.
  - `list package --vulnerable --include-transitive`: 12 projenin hepsi temiz (`NovaCore.FluentValidation` silindiği için 13 → 12).
  - `has-pending-model-changes` (3 context): değişiklik yok.
  - `git grep`: kodda, props'ta, slnx'te, workflow'larda ve policy'de `FluentValidation`, `ICacheService`, `HybridCacheService`, `PermissionPolicyProvider`, `HasPermissionAttribute`, `ValidationFilter`, `ValidationExceptionHandler`, `EXTEXP0018` yok. `services.AddValidation()` üç modülde de var.
  - OpenAPI: faz başında main'den alınan `/openapi/v1.json` ile faz sonundaki belge **bayt bayt aynı** (29.460 bayt). DataAnnotations kısıtları belgeye yansımıyor (R-052, "Sonraya bırakılanlar").
  - Smoke run (Development, yerel SQL Server açık):
    - `/alive` 200, `/health` 200 Healthy, `/scalar/v1` 200, `/api/v1/products` 401.
    - Boş login gövdesi 400 `application/problem+json`: RFC 9110 `type`, `instance: "/api/v1/auth/login"`, `traceId`, `errors` = `Email`, `Password`.
    - Geçersiz register (`not-an-email`, kısa parola) 400: `Email` ve `Password` hataları.
    - Ortam Development'a ayarlanmadan (yanlış `dotnet run` bayrağı) host `Jwt:SigningKey is required outside Development` mesajıyla açılmadı. Bu doğru davranış.
- Faz sonu code review (xhigh), 5 bulgu:
  - Düzeltildi (aff227e):
    - (1) `ExplicitRegistrationTests` (Y-20) `GetTypes()`/`GetExportedTypes()` metotlarını yakalıyordu, `DefinedTypes`/`ExportedTypes` property'lerini yakalamıyordu. F2c.1e testi tüm `src`'ye genişletmişti. Artık ikisi de yakalanıyor. Catalog'a eklenen geçici bir probe testi kırmızıya çevirdi (commit'lenmedi).
    - (2) `AddCoreWeb`'deki yorum hâlâ F2c.2'de silinen policy provider'dan söz ediyordu.
  - Atlandı ("Sonraya bırakılanlar"a yazıldı): iki farklı validation 400 başlığı, Catalog/Payments için geçerli gövde testi yok (F5.3). Kök CLAUDE.md'deki mediator cümlesi zaten listedeydi (Faz 0, F0.6).

### Faz 3a
Plan onayı (2026-09-27). Kullanıcı kararı: 2c önce main'e merge edildi (`--no-ff`, push yok). Model kararları D-024, araştırmalar R-056…R-058. Uygulama sırası F3a.1 → F3a.3 → F3a.2 (D-017 örneği, ID'ler sabit): F3a.1'in kabul testi RSA token istediği için `TestIdentityProvider` F3a.1'de doğuyor, F3a.3 mevcut testleri ona bağlıyor, F3a.2 testleri bir kez daha (sunucu tarafı izinlere) taşıyor. F3a.2 iki alt adım: 2a resolver + Identity implementasyonu, 2b claims transformation. Başlangıçta main'in `/openapi/v1.json`'u scratchpad'e kaydedildi (29.460 bayt, commit'lenmedi).

- [x] F3a.1 (9327877): `Jwt:Authority` eklendi. JwtBearer issuer'ı ve imza anahtarlarını OIDC discovery + JWKS'ten alıyor. Kabul edilen algoritmalar RS/PS/ES 256–512.
  - Simetrik `Jwt:SigningKey` yalnızca Development'ta geçerli (D-024 (1)): Development dışında `https` Authority zorunlu ve anahtar açılışı durduruyor. Development'ta ikisi de boşsa geliştirme anahtarı devreye giriyor. Anahtar varken HS256 ve `Jwt:Issuer` Authority'ninkilere ekleniyor; JwtBearer ikisini birleştiriyor (R-056).
  - `JwtTokenService` anahtar yokken `Jwt:SigningKey`'i adlandıran bir `InvalidOperationException` fırlatıyor. Eskiden IdentityModel'in `ArgumentException`'ı 400'e dönüşürdü.
  - Metot adı `AddCoreAuthentication` olarak kaldı (D-024 (2)). PublicAPI: `JwtOptions.Authority`.
  - Yeni `TestIdentityProvider` (bellek içi RSA, discovery + JWKS bir `HttpMessageHandler` üzerinden). Factory bunu `BackchannelHttpHandler` olarak bağlıyor ve `Jwt:Authority`'yi her ortamda ona ayarlıyor. `SigningKey` yalnızca Development'ta ayarlanıyor.
  - Yeni `JwtValidationTests` (7 vaka):
    - Authority'nin RS256 token'ı 200.
    - Yanlış issuer, yanlış audience, yabancı anahtar ve süresi dolmuş token 401.
    - Production'da https metadata ile RS256 kabul, geliştirme anahtarıyla imzalı HS256 red.
  - Kırmızıdan yeşile: pozitif ve Production testleri önce kırmızıydı. Ret testleri Authority yokken zaten yeşildi, bu yüzden issuer/audience/lifetime doğrulaması geçici olarak kapatıldı: üçü kırmızıya döndü (commit'lenmedi).
  - `ConfigurationValidationTests`: Production satırı artık `Jwt:Authority` bekliyor. Yeni satırlar: Production'da `http://` ve boş authority, Production'da anahtar, geçersiz URI. `JwtTokenServiceTests`'e anahtarsız issuance vakası eklendi.
  - README, appsettings, kök/BuildingBlocks/Identity CLAUDE.md ve CHANGELOG (Breaking configuration) güncellendi. 116/116.
  - Doğrulama sırasında bir kez `ConfigurationValidationTests`'in Cors satırı düştü. Bu R-055'teki açılış yarışı (aynı satır); ardından 10 tam koşu yeşil.
- [x] F3a.3 (af7e4a3): `CreateClientWithClaims` token'ı artık host'un HS256 `ITokenService`'i yerine factory'nin `TestIdentityProvider`'ıyla (RS256, `Jwt:Authority` + JWKS üzerinden doğrulanır) imzalıyor.
  - `PermissionAuthorizationTests` ve `ValidationTests` değişmeden yeşil. Issuer bağlantısı (`BackchannelHttpHandler`) geçici olarak kaldırılınca bu iki sınıfta 15 test kırmızıya döndü (commit'lenmedi); yani artık test IdP'sine dayanıyorlar.
  - `tests` altında `ITokenService` kullanan test kalmadı. Identity akış testleri (`AuthFlowTests`, `RefreshTokenExpiryTests`) modülün kendi HS256 token'larıyla Development'ta çalışıyor.
  - `tests/CLAUDE.md`'ye "Tokens: an in-memory OpenID Connect issuer" bölümü eklendi. 116/116.
- [x] F3a.2 İzin çözümleme (alt adımlar: 2a resolver, 2b claims transformation). İzinler sunucuda çözülüyor, token'dan okunmuyor; rol değişikliği cache TTL'i içinde yansıyor.
  - [x] F3a.2a (5063f54): Yeni public `IPermissionResolver` (`GetPermissionsAsync(ClaimsPrincipal, CancellationToken)`). Identity modülü `IdentityPermissionResolver`'ı scoped kaydediyor: `sub` → kullanıcı → rollerin `permission` claim'leri ∪ kullanıcının doğrudan izinleri. Guid olmayan, bilinmeyen ya da eksik `sub` boş döner (store Guid olmayan id'de exception fırlatıyor).
    - Yeni `IdentityPermissionResolverTests` (4 vaka), önce derlenmedi. Bu alt adımda resolver'ı henüz tüketen yok; token'a izin gömen eski kod 2b'de kalkıyor. 120/120.
  - [x] F3a.2b (7d5558d): `AddCoreAuthorization` internal `PermissionClaimsTransformation`'ı kaydediyor. Kimliği doğrulanmış principal klonlanıyor, token'daki tüm `permission` claim'leri atılıyor ve `IPermissionResolver`'ın döndürdükleri ekleniyor.
    - Önbellek: HybridCache, anahtar `novacore:permissions:{sub}:{stamp}` (kaçışlı), süre yeni `Cache:PermissionExpiration` (varsayılan 5 dk, pozitif olmalı, D-024 (4)). Caller token'ı `None` (D-024 (6)).
    - Public API: `NovaClaims.SecurityStamp` (`AspNet.Identity.SecurityStamp`, R-057).
    - Identity'nin token'ları artık `permission` claim'i taşımıyor. `AuthTokenService` yalnızca rolleri yazıyor, `RoleManager` bağımlılığı kalktı.
    - Testler (önce kırmızı görüldü, 6 hata):
      - Factory'ye sunucu tarafı izin deposu eklendi (`CreateClientWithPermissions`, `GrantPermissions`). Dekoratör resolver issuer'a bakıyor: test IdP'sinin konuları depodan (verilmemişse boş), Identity'nin token'ları host'un resolver'ından.
      - İlk denemede dekoratör konuya göre ayırıyordu. Bu yüzden rastgele `sub`'lar Identity DB'sine düşüp 500 verdi; issuer'a göre ayrım bunu çözdü.
      - `FakeClockApiFactory` `MemoryCacheOptions.Clock`'u da sahte saate bağlıyor (HybridCache L1 süresi `TimeProvider`'ı değil bunu kullanıyor, R-057).
      - `PermissionAuthorizationTests` sunucu tarafı izinlere geçti; yeni vaka: token'daki izin claim'i hiçbir şey açmıyor.
      - Yeni `PermissionResolutionTests`: iptal edilen izin TTL dolana kadar geçerli; yeni stamp cache'i atlıyor.
      - Rol değişikliği kabul testi: Identity kullanıcısı login → 403 → rol eklendi → aynı token TTL içinde 403 → saat ilerleyince 200. Code review'da ayrı sınıfa (`RoleChangeTests`) taşındı.
      - `ConfigurationValidationTests`: `Cache:PermissionExpiration` satırı.
    - Kasıtlı bozmalar (commit'lenmedi): claim silme kapatılınca token-claim testi, stamp anahtardan çıkarılınca stamp testi kırmızıya döndü.
    - README, kök/BuildingBlocks/Identity/tests CLAUDE.md, CHANGELOG (Breaking behaviour) ve memory `novacore-security` güncellendi. 125/125.
- Faz doğrulaması (2026-09-27):
  - `restore --force`: temiz, NU uyarısı yok.
  - `build -c Release --no-incremental`: 0 uyarı, 0 hata.
  - `test`: 125/125 (review düzeltmelerinden sonra 127/127, üç ardışık koşu).
  - `list package --vulnerable --include-transitive`: 12 projenin hepsi temiz.
  - `has-pending-model-changes` (3 context): değişiklik yok.
  - OpenAPI: faz başında main'den alınan `/openapi/v1.json` ile faz sonundaki belge **bayt bayt aynı** (29.460 bayt).
  - Smoke run (Development, yerel SQL Server açık, DB'ye yazılmadı):
    - `/alive` 200, `/health` 200, `/api/v1/products` anonim 401.
    - Geliştirme anahtarıyla imzalı Administrator token'ı `/products` 200 aldı. `/auth/me`: roller `[Administrator]`, izinler `[]`; bilinmeyen `sub` gerçek Identity store'undan boş çözüldü.
    - Yalnızca `permission: catalog:products:read` claim'i taşıyan token `/products` 403 aldı, `/me`'de izinler `[]`.
  - Production açılışı:
    - `Jwt:Authority` yokken host `Jwt:Authority must be the absolute URI of an OpenID Connect provider…` mesajıyla durdu.
    - `Jwt:SigningKey` verilince `Jwt:SigningKey (a symmetric HS256 key) is accepted in Development only…` ile durdu.
- Faz sonu code review (xhigh), 10 bulgu:
  - Düzeltildi (f0a5bf2):
    - (1) Rol değişikliği testi sıraya bağımlıydı. Identity token'ı fixture'ın paylaşılan sahte saatinden damgalıyor; başka bir test saati önce ilerletirse login token'ı "henüz geçerli değil" (401) oluyordu. Login'den önce saati ilerleterek yeniden üretildi. Test kendi host'u olan `RoleChangeTests`'e taşındı.
    - (2) `IPermissionResolver` kayıtlı değilse yalnızca Development'ta (ValidateOnBuild) açılış duruyordu; Production'da her kimlikli istek 500 verirdi. Yeni internal `PermissionResolverStartupCheck` (hosted service, `IServiceProviderIsService`) her ortamda açılışı durduruyor. Yeni `PermissionResolverStartupCheckTests` önce kırmızı görüldü. Böylece D-024 (7)'deki "açılış durur" sonucu her ortamda doğru; mekanizma DI doğrulaması değil bu kontrol.
    - (3) `IPermissionResolver` dokümanı artık sonucun `sub` + stamp başına cache'lendiğini ve token'a (scope, client, audience) göre değişmemesi gerektiğini söylüyor.
    - (4) Anahtarsız issuance mesajı, Development'ta Authority varken geliştirme anahtarının doldurulmadığını da söylüyor.
    - (5) `NovaClaims` sınıf özeti artık izinlerin token'dan geldiğini söylemiyor.
  - Atlandı ("Sonraya bırakılanlar"a yazıldı): super-admin bypass'ının token rolüne dayanması, Development'ta issuer ve anahtarın bağsız birleşmesi, cache değerinin her okumada deserialize edilmesi, factory'deki dispose sırası, test yardımcılarındaki tekrar.

### Faz 3b
Plan onayı (2026-09-27). Kullanıcı kararları: Q-28 (D-027, tam parite), Q-29 (D-028, Development simetrik anahtarı kalkar). Model kararları D-026, araştırmalar R-059…. Başlangıçta main'in `/openapi/v1.json`'u scratchpad'e kaydedildi (29.460 bayt, commit'lenmedi). Alt adımlar: F3b.1a iskelet + sunucu, 1b Authorization Code + PKCE + refresh, 1c client credentials; F3b.2a sertleştirme + audit, 2b kayıt/onay, 2c 2FA, 2d oturum uçları, 2e admin + rol seed'i; F3b.3a Api geçişi + modülün silinmesi, 3b çekirdek temizliği, 3c dokümanlar; F3b.5a OpenAPI/Scalar, 5b tarayıcı. Eklenti, eski modül Api host'unda dururken her commit'in yeşil kalması için bir tüketici test host'unda test ediliyor (D-026 (12)).

- [x] F3b.1 OpenIddict eklentisi (alt adımlar: 1a iskelet + sunucu, 1b Authorization Code + PKCE + refresh, 1c client credentials)
  - [x] F3b.1a (47e72ab): Yeni packable `NovaCore.Identity.OpenIddict` (yalnızca `NovaCore.AspNetCore`'u referans alıyor). Paketler: `OpenIddict.Server.AspNetCore` ve `OpenIddict.EntityFrameworkCore` 7.7.1 (CPM + allowlist, hook onaylı).
    - İçerik: `IdentityServerDbContext` (Identity + OpenIddict, şema `identity`, migration yok), `ApplicationUser`/`ApplicationRole`, `AddCoreIdentityServer`.
    - `AddCoreIdentityServer`: issuer = `Jwt:Authority`, PKCE zorunlu, access token şifrelemesi kapalı, 15 dk / 7 gün. Development'ta ephemeral anahtar. `OpenIddictServerOptions` `ValidateOnStart` ile bağlı.
    - Kullanıcı `IPermissionResolver`'ı taşındı. Api host'u henüz değişmedi.
    - Tüketici test host'u `IdentityServerTestHost` (core + eklenti, SQLite, TestServer `https://localhost` = Authority, JwtBearer backchannel'ı aynı sunucuya bağlı).
    - Testler (8):
      - discovery: issuer, uçlar, yalnızca S256;
      - issuer istek host'undan değil Authority'den geliyor;
      - Development'ta RSA anahtar;
      - Production'da anahtar yoksa açılış duruyor (OpenIddict ID0085/86 mesajı);
      - EC anahtarla ES256 yayımlanıyor;
      - Authority yoksa açılış duruyor;
      - resolver testleri (2).
    - **Sürpriz:** OpenIddict `plain` PKCE yöntemini de varsayılan olarak açık tutuyor. Discovery testi bunu yakaladı (kırmızı) ve `plain` listeden silindi (RFC 9700 §2.1.1, R-059).
    - Kasıtlı bozmalar (commit'lenmedi): `ValidateOnStart` kaldırılınca iki açılış testi, issuer ataması kaldırılınca issuer ve Authority testleri kırmızıya döndü.
    - 136/136.
  - [x] F3b.1b (d67697b): Authorization Code + PKCE ve refresh.
    - `MapCoreIdentityServer`:
      - `/connect/authorize`: passthrough; cookie yoksa login'e challenge; prompt=none/login ve max_age; implicit consent; kalıcı authorization.
      - `/connect/token`: code/refresh; kullanıcı hâlâ var mı, giriş yapabilir mi, kilitli mi kontrol edilir; roller yeniden yazılır.
      - `/connect/endsession`, OpenIddict'in `/connect/revoke`'u.
      - `/account/login`: sade HTML, antiforgery, yalnızca yerel ReturnUrl.
    - Cookie'ler: `AddIdentityCookies` ile, varsayılan şema Bearer kalıyor; 30 dk sliding, Secure.
    - `Identity:Clients` + `Identity:Seed:SeedOnStartup` → `IdentityServerSeeder` client'ları eşitliyor. Doğrulama: `[OptionsValidator]` ve `.Validate` (Guid olmayan, tekil ClientId, mutlak redirect URI).
    - Testler (13), tüketici host'unda `OidcFlow` ile:
      - uçtan uca akış; token çekirdekten geçiyor (RS256, `at+jwt`, `aud`=novacore.api, `permission` yok);
      - izin yoksa 403; anonim API çağrısı 401 (login redirect'i değil);
      - PKCE yok ya da `plain` → 400; prompt=none → `login_required`;
      - ReturnUrl başka siteye yönlendirmiyor; antiforgery'siz form 400;
      - refresh rotasyonu; leeway 0 iken reuse zinciri iptal ediyor; revoke; kilitli kullanıcı refresh yapamıyor;
      - endsession sağlayıcı oturumunu kapatıyor.
    - **Sürprizler (R-061):**
      - (1) Antiforgery middleware'i bu pipeline'da yok, `[FromForm]` 500 verdi. Form artık handler'da `IsRequestValidAsync` ile doğrulanıyor.
      - (2) Çekirdeğin `IClaimsTransformation`'ı OpenIddict şemasının `AuthenticateAsync`'inde de çalıştığı için refresh sonrası token'a `permission` claim'leri giriyordu. Test yakaladı; token endpoint'i bunları dışarıda bırakıyor.
      - (3) Yeni testlerin paralel yükü R-055 yarışını tam koşuların 4'te 3'üne çıkardı. Testler `DisableParallelization` koleksiyonuna alındı, host'lar sınıf başına paylaşıldı; ardından 13/13 tam koşu yeşil (R-055 ek gözlem).
    - Kasıtlı bozmalar (commit'lenmedi): antiforgery kontrolü, open redirect koruması, lockout kontrolü, endsession sign-out ve 0 leeway kaldırılınca ilgili testler kırmızıya döndü.
    - 149/149.
  - [x] F3b.1c (cf4f49c): Client credentials.
    - Redirect URI'si olmayan client bir servis sayılıyor: secret zorunlu, izinleri `Identity:Clients:n:Permissions`'tan geliyor.
    - Token endpoint'i client credentials grant'ını karşılıyor: `sub` = client id, `name` = görünen ad, `aud` = `Jwt:Audience`.
    - Resolver Guid olmayan `sub`'ı yapılandırılmış servis client'ı olarak çözüyor.
    - Seed descriptor'ı client türüne göre izin veriyor (servis: yalnızca token endpoint + client_credentials).
    - Yeni açılış kuralları: izin yalnızca servis client'ında; secret'sız servis yasak; redirect URI'leri mutlak.
    - Testler (8):
      - izinli servis 200, izinsiz servis 403;
      - yanlış secret 401 `invalid_client` (RFC 6749 §5.2);
      - kullanıcı client'ı client_credentials kullanamıyor;
      - dört yapılandırma kuralı;
      - EC anahtarlı Production host'unda ES256 token çekirdekten geçiyor (PLAN F3b.1 "RS256/ES256").
    - Kasıtlı bozma (commit'lenmedi): resolver client izinlerini döndürmeyince iki test kırmızıya döndü.
    - 157/157 (üç ardışık tam koşu).
  - F3b.1 kabul kriteri: uçtan uca token akışı çalışıyor (Authorization Code + PKCE, refresh, client credentials; RS256 ve ES256).
- [x] F3b.2 Sertleştirmelerin taşınması (alt adımlar: 2a sertleştirme + audit, 2b kayıt/onay, 2c 2FA, 2d oturum uçları, 2e admin + rol seed'i)
  - [x] F3b.2a (9d4b08a): Sertleştirmeler ve audit.
    - Identity seçenekleri modüldeki değerlerle aynı: PBKDF2 600.000, parola 12 karakter ve kompozisyon kuralı yok, tekil e-posta, onaylı e-posta, lockout 5 deneme / 15 dk (yeni hesaplar dahil), onay token'ı 6 saat.
    - Login sayfası bilinmeyen hesap, yanlış parola ve kilitli hesap için tek mesaj gösteriyor. Onaysız e-posta mesajı yalnızca doğru paroladan sonra geliyor (modüldeki gibi).
    - `SecurityAuditLogger` arayüzsüz, internal ve somut bir sınıf (Y-13). Bu adımda: login_succeeded/failed, account_locked_out, token_refreshed, signed_out.
      - Maskeleme artık iki çağrıda da `IsEnabled` guard'ının arkasında; modüldeki guard'sız `LoginFailed` notu burada kapanıyor.
      - Refresh token reuse'u OpenIddict kendi logluyor (Warning: "...were revoked to prevent a potential token replay attack", F3b.1b'de gözlendi).
    - Testler (8, `SignInHardeningTests`): hash'te 600.000 iterasyon (v3 biçimi çözülerek), length-first politika (2), tekil e-posta, 5 hatada kilit (+ audit), onaysız hesap, bilinmeyen hesap ile yanlış parolanın aynı görünmesi, maskeli audit ve logda parola yok.
    - Kırmızıdan yeşile: sertleştirme öncesi eklentiye karşı 7/8 kırmızı (stash ile denendi), sonra yeşil.
    - 165/165.
  - [x] F3b.2b (dbf4ef8): Kayıt ve e-posta onayı.
    - `/api/v1/auth/register` ve `/confirm-email` modüldeki yollarında (tag "Auth", "strict" rate limit). Kayıt, var olan adres için yeni kayıtla aynı yanıtı veriyor. Onay linki `Jwt:Authority`'den üretiliyor, Host header'dan değil; P0-6'daki host header zehirlenmesi kapandı.
    - Public `IEmailSender` (host implemente eder). Development'ta `LoggingEmailSender` (`TryAdd`), Production'da sender yoksa `EmailSenderStartupCheck` açılışı durduruyor. Eklenti kendi assembly'si için `services.AddValidation()` çağırıyor.
    - Varsayılan `User` rolüne atama F3b.2e'de, rol seed'iyle birlikte geliyor: rol yokken `AddToRoleAsync` hata veriyor.
    - **Sürpriz (R-062):** .NET 10'un validation generator'ü internal request record'unu kapsamadı, geçersiz gövde handler'a ulaştı (test yakaladı). Doğrulanan gövdeler artık public, üst düzey record'lar (`RegisterRequest`) ve PublicAPI'de izleniyor. Endpoint sınıfları internal kalıyor.
    - Testler (7, `RegistrationTests`):
      - kayıt → yakalanan e-posta → onay → giriş;
      - var olan adres aynı yanıtı alıyor ve ikinci e-posta gitmiyor;
      - sahte Host header linke girmiyor;
      - validation hataları (DisplayName, Email, Password);
      - bozuk token 400;
      - Production'da sender zorunlu;
      - Development'ta log sender.
    - EC testinin Production host'una bir sender eklendi (yeni açılış kuralı).
    - Kasıtlı bozmalar (commit'lenmedi): link Host'tan üretilince ve var olan adres 409 dönünce 3 test, açılış kontrolü kaldırılınca 1 test kırmızıya döndü.
    - 172/172.
  - [x] F3b.2c (fe206f6): 2FA.
    - Login sayfası `RequiresTwoFactor`'da `/account/login/2fa`'ya gidiyor: aynı sade form, antiforgery, yalnızca yerel ReturnUrl. Kod adımı iki faktör cookie'si olmadan açılmıyor, login'e dönüyor.
    - `TwoFactorAuthenticatorSignInAsync` hatalı kodu `AccessFailed` sayıyor, yani lockout'a gidiyor. F0.9'daki "hatalı 2FA kodu lockout sayacını artırmıyor" açığı kapandı.
    - `/api/v1/auth/2fa/setup|enable|disable` modüldeki gibi (bearer, `RequireAuthorization`). Doğrulanan gövde public `TwoFactorCodeRequest` (R-062).
    - Authenticator uygulamasındaki etiket artık sabit "NovaCore" değil, host'un `IHostEnvironment.ApplicationName`'i.
    - Audit: two_factor_challenged/enabled/disabled, bad-2fa-code.
    - Testler (6, `TwoFactorTests`):
      - etkinleştirme sonrası girişte kod adımı ve token;
      - 5 hatalı kod kilitliyor;
      - yanlış kod etkinleştirmiyor;
      - biçimsiz kod validation hatası;
      - devre dışı bırakınca adım kalkıyor;
      - kod adımı parola adımı olmadan açılmıyor.
    - TOTP testte hesaplanıyor (`AuthenticatorCode`). HMAC-SHA1 için CA5350, MS'in "When to suppress" iznine dayanan dar ve gerekçeli bir `[SuppressMessage]` ile bastırıldı (R-063, Y-44).
    - Kasıtlı bozmalar (commit'lenmedi): kod adımı atlanınca 2 test, hatalı kod sayılmayınca lockout testi kırmızıya döndü.
    - 178/178.
  - [x] F3b.2d (824b67f): Oturum uçları.
    - Yeni uçlar modüldeki yollarında (bearer):
      - `/api/v1/auth/me`: çekirdeğin çözdüğü izinleri de gösteriyor.
      - `/change-password`: public `ChangePasswordRequest`. Identity stamp'i yeniliyor ve bütün token'lar iptal ediliyor.
      - `/logout-all`: stamp yenileniyor; OpenIddict `RevokeBySubjectAsync` ile token'lar ve authorization'lar iptal ediliyor.
    - Tek oturum çıkışı standart `/connect/revoke` + `/connect/endsession`; modüldeki JSON `/logout` taşınmadı (D-027).
    - Sağlayıcı cookie'sinin `SecurityStampValidatorOptions.ValidationInterval`'ı sıfır (D-026 (6)). Böylece bu iki uç IdP oturumunu hemen bitiriyor.
    - Verilmiş access token'lar süreleri (15 dk) dolana kadar geçerli; modülde de öyleydi, CHANGELOG'a yazıldı.
    - Audit: password_changed, sessions_revoked.
    - Testler (6, `SessionTests`):
      - `/me` sunucuda çözülen izinlerle; anonim `/me` 401;
      - logout-all ve parola değişikliği sonrası refresh `invalid_grant` ve authorize login'e dönüyor;
      - yanlış mevcut parola hiçbir şeyi değiştirmiyor;
      - kısa yeni parola validation hatası.
    - Kasıtlı bozmalar (commit'lenmedi): varsayılan stamp aralığıyla 2 test, iptalsiz logout-all ile 1 test kırmızıya döndü.
    - 184/184. Tam koşu artık ~1 dk: seri IdentityServer testlerinde her kullanıcı ve giriş PBKDF2 600k hesaplıyor.
  - [x] F3b.2e (4d5fd89): Admin ve rol seed'i.
    - `/api/v1/admin/*` modüldeki yollarında, `identity:*` izinleriyle (`IdentityPermissionProvider`). Doğrulanan gövde public `AssignRoleRequest`. Audit: role_assigned/removed.
    - `IdentityServerSeeder`, `SeedOnStartup`'ta şu sırayla çalışıyor:
      - roller: Administrator her beyan edilmiş izni, User kimlik dışı `:read` izinlerini alıyor; eksik izin ekleniyor, verilmiş izin silinmiyor;
      - admin: `Identity:Seed:AdminEmail/AdminPassword`;
      - client'lar.
    - Kayıt yeni kullanıcıyı User rolüne ekliyor (modüldeki gibi).
    - Testler (8):
      - `AdminTests` (5): seed edilen rol izinleri, admin kullanıcı listeler, User rol atayamaz (403), rol atama/kaldırma + 404 + 400, yeni kayıt User rolünde;
      - `IdentityServerRoleChangeTests`: rol değişikliği TTL içinde yansıyor, sahte saatle;
      - `RefreshTokenLifetimeTests`: 7 gün + 1 sn sonra refresh `invalid_grant`. Bu, modüldeki `RefreshTokenExpiryTests`'in karşılığı.
      - Sahte saatli sınıflar `FakeClockIdentityServerFixture`'ı paylaşıyor ama her biri kendi host'unu açıyor.
    - Kasıtlı bozmalar (commit'lenmedi): User rolüne admin okuma izinleri verilince, atama ucu izinsiz bırakılınca ve refresh ömrü 14 gün olunca ilgili test kırmızıya döndü.
    - Faz içindeki ek model kararları tek bir kayıtta toplandı: D-029.
    - 191/191 (iki ardışık tam koşu).
  - F3b.2 kabul kriteri: modülün her güvenlik testinin eklentide bir karşılığı var ve yeşil.
    - `AuthFlowTests` → `AuthorizationCodeFlowTests` + `SignInHardeningTests` + `AdminTests`
    - `RefreshTokenExpiryTests` → `RefreshTokenLifetimeTests`
    - `RoleChangeTests` → `IdentityServerRoleChangeTests`
    - `IdentityPermissionResolverTests` → `IdentityServerPermissionResolverTests`
    - `EmailSenderRegistrationTests` → `RegistrationTests`
    - Host düzeyindeki `StrictRateLimitTests`, `ValidationTests` ve `AuthGateTests` satırları F3b.3a'da Api host'una taşınıyor.
- [x] F3b.3 Eski modülün emekliye ayrılması (alt adımlar: 3a Api geçişi + modülün silinmesi, 3b çekirdek temizliği, 3c dokümanlar)
  - [x] F3b.3a (9d35624): Api host'unun geçişi ve modülün silinmesi (kullanıcı onayıyla).
    - `Program.cs`:
      - modül kaydı yerine `AddCoreIdentityServer` ve `AddModuleDbContext<IdentityServerDbContext>` (SQL Server, `MigrationsAssembly` = Api, geçmiş tablosu `identity` şemasında, `EnableRetryOnFailure`; OpenIddict store'ları transaction'ları execution strategy içinde açıyor, R-064);
      - `MapCoreIdentityServer`, modüllerden önce.
    - Migration ve factory: Api'de `Identity/Migrations/…_Initial` ve `IdentityServerDbContextFactory`. `has-pending-model-changes` üç context'te "No changes".
    - `appsettings.Development.json`: `Jwt:Authority` = `https://localhost:5443`.
    - CI'daki model kontrolü Api'deki context'i de kapsıyor.
    - Migration'daki bileşik index dizileri CA1861 verdi. `.editorconfig`'te yalnızca `src/**/Migrations/*.cs` için kapatıldı (MS "When to suppress": tek seferlik çağrı, dizi saklanıyor; Y-44, R-064).
    - Silinenler:
      - `src/Modules/Identity/` (slnx'ten de);
      - karşılığı eklentide olan `AuthFlowTests`, `RefreshTokenExpiryTests`, `RoleChangeTests`, `IdentityPermissionResolverTests`, `EmailSenderRegistrationTests`;
      - Api/test `bin/obj` temizlendi.
      - `FakeClockApiFactory` ve `ProductionApiFactory` `DerivedApiFactories.cs`'e taşındı.
    - Güncellenen host testleri:
      - `ModuleRegistrationTests` (modüller Catalog/Payments; uç listesi eklentinin API uçlarıyla);
      - `ValidationTests` (login/2fa-login/refresh/logout satırları çıktı);
      - `AuthGateTests` (login satırı yerine grant'sız token isteği 400);
      - `StrictRateLimitTests` → `/connect/token` (D-009 anlamı: A 400, A 429, B 400);
      - `HealthEndpointsTests` (context adı), `OpenApiTests` (`/connect` ve `/account` belgede yok).
    - `NovaCoreApiFactory` Development dışında ephemeral anahtar ve e-posta gönderici veriyor (üretim host'unun getirmesi gerekenler). Böylece production testlerindeki modül sender sarmalayıcıları kalktı.
    - Development'ta Authority'siz geliştirme anahtarını sınayan test, "Development'ta da Authority gerekir" testine çevrildi. Eklenti Authority'yi zorunlu kılıyor; D-028 çekirdekten de kaldırıyor.
    - Yeni `IdentityServerWiringTests`: Api host'u kendi Authority'si. Authorization Code + PKCE ile giriş → `/api/v1/products` 200, `/api/v1/payments` 403 (izinler store'dan). `Program.cs`'teki map satırı kaldırılınca bu test ve kayıt testi kırmızıya döndü (commit'lenmedi).
    - Değişmezler `src/BuildingBlocks/NovaCore.Identity.OpenIddict/CLAUDE.md`'ye taşındı. Kök ve tests CLAUDE.md'deki modül atıfları, README'deki üç yer düzeltildi (geniş README güncellemesi F3b.3c'de).
    - Kabul: `git grep "NovaCore.Modules.Identity|IdentityModule"` (docs/plan ve CHANGELOG dışında) boş.
    - 173/173 (iki koşu).
  - [x] F3b.3b (16f6aae): Çekirdek temizliği (D-028).
    - `ITokenService`, `JwtTokenService` ve `AccessToken` silindi. `JwtOptions`'ta yalnızca `Authority` ([Required]) ve `Audience` kaldı.
    - `AddCoreAuthentication`:
      - geliştirme anahtarı, `PostConfigure` ve HS256 dalı yok;
      - issuer yalnızca metadata'dan geliyor, algoritmalar yalnızca asimetrik;
      - Authority her ortamda zorunlu, Development dışında https;
      - artık kullanmadığı için `TimeProvider` kaydı da kalktı; tüketicileri kendilerininkini `TryAdd` ediyor.
    - PublicAPI'den 30 satır silindi. `appsettings.json`'daki Jwt bölümü sadeleşti. D-024 (1) satırına "yerine D-028" notu eklendi (hook onayıyla).
    - Eklentinin kendi Authority kontrolü de kalktı: çekirdek artık aynı şeyi her ortamda doğruluyor ve önce o çalışıyor. Test çekirdeğin mesajını bekliyor.
    - Testler:
      - `JwtTokenServiceTests` silindi (silinen kodun testi, D-028).
      - `ConfigurationValidationTests`: SigningKey/Issuer/AccessTokenMinutes/RefreshTokenDays satırları çıktı. Development + boş Authority satırı çekirdeğin mesajını bekliyor ve eski çekirdeğe karşı kırmızıydı (stash ile denendi). `Jwt:Audience` boş satırı eklendi. Config katmanı testi `Audience` üzerinden.
      - `JwtValidationTests`: simetrik imzalı token Development ve Production'da 401 (theory); Production'da Authority kabulü ayrı test.
      - Factory artık anahtar ya da token ömrü vermiyor.
    - Kabul: `git grep "SigningKey|ITokenService|JwtTokenService|AccessTokenMinutes|RefreshTokenDays"` src/tests/.github'da yalnızca OpenIddict'in `AddEphemeralSigningKey`'i ve test IdP'sinin `SigningKey`'ini buluyor.
  - [x] F3b.3c (bfc4161): Dokümanlar.
    - README:
      - hızlı başlangıç (https profili, migration komutu), uç nokta tablosu, çekirdek/proje tablosu, proje ağacı (eklenti ve Api/Identity);
      - Güvenlik bölümü yeniden yazıldı (doğrulama / sağlayıcı / hesaplar), konfigürasyon (Jwt, Identity:Clients, doğrulama kuralları);
      - veritabanı (migration'lar Api'de), test, dağıtım (yedi paket), lisans tablosu (OpenIddict, Apache-2.0), eşikler.
    - Kök CLAUDE.md: paket listesi, host kompozisyonu, yedi paket / dört eklenti.
    - Memory: `novacore-security` yeniden yazıldı, `novacore-stack`, `novacore-project` ve `MEMORY.md` güncellendi.
    - F3b.3a ve 3b'de gelen belgelerle birlikte eski modülden söz eden doküman kalmadı.
    - Dokunulmayanlar ("Sonraya bırakılanlar"da kalıyor): README'deki mediator cümlesi, eskimiş Binder satırı, Y-10 atıfları.
  - F3b.3 kabul kriteri: `NovaCore.Modules.Identity`'ye referans yok (docs/plan ve CHANGELOG geçmişi dışında).
- [ ] F3b.3 Eski modülün emekliye ayrılması
- [x] F3b.4 (cecc0ee): docker-compose.
  - İmajlar pinlendi: SQL Server `2022-CU27-ubuntu-22.04`, Seq `2026.1.17182`, Jaeger v2 `jaegertracing/jaeger:2.21.0` (v1 all-in-one EOL; varsayılan yapılandırma OTLP'yi alıyor). `:latest` kalmadı.
  - Keycloak ayrı bir overlay'de: `docker-compose.keycloak.yml`. `quay.io/keycloak/keycloak:26.7.4`, `start-dev --import-realm`, `127.0.0.1:8180`, bootstrap admin parolası `.env`'den.
  - Neden ayrı dosya: compose, aktif olmayan profillerdeki `:?` değişkenlerini de yorumluyor (denendi, R-065). Profil kullanılsaydı Keycloak kullanmayanların compose'u bozulurdu.
  - Neden 8180: bu makinede 8080'i bir httpd tutuyordu.
  - Örnek realm `deploy/keycloak/novacore-realm.json`:
    - public PKCE (S256) client `novacore-scalar` (redirect'ler Scalar'ın iki adresi);
    - `novacore.api` audience mapper'ı, realm rolleri → `role` claim'i (Sonraya bırakılanlar'daki "Keycloak rolleri `realm_access.roles`'ta" notu bu mapper'la karşılandı);
    - Administrator/User rolleri; kullanıcı ya da secret yok.
  - `.env.example`: iki parolanın yer tutucusu. SA parolası D-008'den beri `.env`'de.
  - Kabul:
    - `docker compose config` (yerel `.env`), `docker compose --env-file .env.example config` ve iki dosyalı `config` geçerli;
    - `config --images` yalnızca pinli etiketler.
  - Ek smoke (kullanıcı onayıyla): realm import edildi, discovery ve admin API doğruladı. Container, volume ve ağ silindi (R-065).
  - README (proje ağacı, "Harici sağlayıcıyla deneme") ve CHANGELOG güncellendi.
- [x] F3b.5 Uçtan uca doğrulama (alt adımlar: 5a OpenAPI ve Scalar, 5b tarayıcıda akış)
  - [x] F3b.5a (23be3a9): OpenAPI ve Scalar.
    - Eklentinin internal `OAuthSecuritySchemeTransformer`'ı her OpenAPI belgesine (`ConfigureAll<OpenApiOptions>`) `OAuth2` şemasını ekliyor: authorizationCode akışı, URL'ler `Jwt:Authority` altında, scope'lar openid/profile/email/roles/offline_access. Şema anahtarı public `IdentityServerHostingExtensions.OpenApiSecuritySchemeId` sabiti.
    - `NovaCore.Scalar`'a `MapCoreScalar(WebApplication, Action<ScalarOptions>)` overload'u geldi; RS0026 gereği ikisi de düz overload.
    - `Program.cs` Scalar'da `OAuth2`'yi öne seçiyor: client `scalar`, PKCE SHA-256, scope'lar openid/profile/email/roles.
    - `appsettings.Development.json` `Identity:Clients`'a public `scalar` client'ını ekledi; redirect ve post-logout URI'si `https://localhost:5443/scalar/v1`.
    - Testler:
      - eklenti host'unda belge şemayı ve URL'leri içeriyor;
      - Api host'unda `OAuth2` ve Authority altındaki authorize URL'si var;
      - `/connect` ve `/account` belgede işlem (path) olarak yok. Eski metin kontrolü yolu da yakaladığı için path kontrolüne çevrildi.
    - Kasıtlı bozma (commit'lenmedi): transformer kaydı kaldırılınca iki test kırmızıya döndü.
    - 169/169 (iki koşu).
  - [x] F3b.5b (ef1d733): Tarayıcıda akış (kullanıcı onayıyla ayrı `NovaCore_F3b5` veritabanı; deneme sonunda silindi).
    - Uygulama https profilinde çalıştı:
      - açılışta migration'lar ve seed (admin + `scalar` client) çalıştı;
      - admin parolası yalnızca süreç ortamında tutuldu ve sonradan silindi.
    - Built-in browser'da Scalar:
      - OAuth2 şeması öne seçili; Auth/Token URL'leri, redirect `https://localhost:5443/scalar/v1`, client `scalar`, PKCE SHA-256 (ekran görüntüsü).
      - "Authorize" → `/connect/authorize` 302 → `/account/login` (ekran görüntüsü) → giriş (`login_succeeded`) → code ile `/scalar/v1`'e dönüş.
    - **Kısıt (R-066):** built-in browser Scalar'ın `window.open` popup'ını aynı sekmede açtı, bu yüzden Scalar'ın kendi code değişimi çalışmadı. Akış aynı sekmede aynı client ve PKCE ile sürdürüldü.
      - Token 200: RS256, `at+jwt`, doğru `iss`/`aud`, izin claim'i yok.
      - Scalar'ın API client'ından `GET /api/v1/products` → **200 OK** (ekran görüntüsü).
      - OAuth2 şeması token'sız seçiliyken Scalar 401 aldı; şema kaldırılınca 200.
    - Kod değişikliği yok. Popup tabanlı Scalar akışı otomatik testlerin karşılığıyla ve normal tarayıcıda denenmek üzere raporlandı.
- [x] Code review düzeltmeleri (dc7f101): `code-review` (xhigh, `main...faz/3b-idp`) 14 bulgu verdi.
  - Kapsam içi 3 gerçek hata düzeltildi (D-030, R-067). Üçünün testi de düzeltmeden önce kırmızıydı; testler 169 → 172.
    - Giriş sayfası: `SignInManager` onaysız hesapta parolaya bakmadan `NotAllowed` döndürüyor. "E-postanı onayla" mesajı bu yüzden her parolayla çıkıyor ve kayıtlı adresleri ele veriyordu. Artık parola sayfada kontrol ediliyor; yanlışsa genel mesaj gösteriliyor ve lockout sayacı artıyor.
    - `auth_time` ve `max_age`: cookie'nin `IssuedUtc`'si her yenilemede kayıyordu (sıfır aralıklı stamp doğrulaması her okumada cookie'yi yeniliyor). Oturum açma zamanı artık `OnSigningIn`'de cookie özelliklerine yazılıyor.
    - `/2fa/setup` 2FA açıkken anahtarı döndürüyordu: bir access token'la kod hesaplanıp 2FA kapatılabiliyordu. Artık 409.
  - "Eklenti `TimeProvider` kaydetmiyor" bulgusu gerçek değil: `AddAuthentication()` `TimeProvider.System`'ı kaydediyor (probe testiyle görüldü).
  - Kalan 10 bulgu kapsam dışı; "Sonraya bırakılanlar"da.
- Faz doğrulaması (2026-09-27):
  - `dotnet restore NovaCore.slnx --force`: NU uyarısı yok.
  - `dotnet build NovaCore.slnx -c Release --no-incremental`: 0 uyarı, 0 hata.
  - `dotnet test NovaCore.slnx`: code review'dan önce 169/169 (3 koşu), düzeltmelerden sonra 172/172 (3 koşu, ikisi Release).
  - `dotnet list NovaCore.slnx package --vulnerable --include-transitive`: açık yok.
  - `has-pending-model-changes`: Catalog, Payments ve Api'deki `IdentityServerDbContext` için değişiklik yok.
  - `docker compose config` (yerel `.env`, `.env.example` ve Keycloak overlay'iyle): geçerli.
  - OpenAPI farkı (main'e karşı), yalnızca beklenenler:
    - login, login/2fa, logout ve refresh yolları ve şemaları gitti;
    - `TwoFactorCodeRequest` ve `OAuth2` şeması geldi;
    - logout-all ve 2fa/setup özetleri ile tag sırası değişti.
  - Production smoke: Authority yoksa çekirdeğin mesajıyla, anahtar yoksa OpenIddict'in "At least one encryption key must be registered" mesajıyla açılış duruyor.
  - Tarayıcı raporu: F3b.5b.

### Faz 4a
Plan onayı 2026-09-28. Uygulama sırası F4a.1 → F4a.3 → F4a.2 (ID'ler sabit): F4a.2'nin dispatcher'ı handler'ları F4a.3'ün cache'li wrapper'ıyla çağırıyor.

- [x] F4a.1 (4678ea3): Event'lerin outbox'a yazılması.
  - Event tamponu jenerik olmayan `Entity` tabanına taşındı (`Entity<TId> : Entity`). Mevcut kod aynen derleniyor.
    - **Sapma:** planda kurucu `private protected`'tı. PublicApiAnalyzers RS0022 verdi (türeyen `Entity<TId>` korumalı `Raise`'i dışarı açıyor), kurucu `protected` oldu (D-031 (2)).
  - `OutboxInterceptor` (public) `SavingChanges`'te her `Entity`'nin event'lerini `outbox_messages`'a ekliyor ve tamponu temizliyor.
    - EF interceptor'ı `DetectChanges`'ten ve execution strategy'nin sardığı `StateManager.SaveChanges`'ten önce çağırdığı için satırlar aynı transaction'da (R-068).
    - Modelde outbox yokken event gelirse kayıt açıklayıcı bir `InvalidOperationException` ile duruyor.
  - `AddModuleDbContext` interceptor'ı her modül context'ine kendisi ekliyor. Catalog ve Payments `modelBuilder.AddOutbox()` çağırıyor. `Outbox` migration'ları: `bigint IDENTITY` anahtar, unique `EventId`.
  - `NovaCoreApiFactory.ReplaceWithSqlite` interceptor'ı da ekliyor.
  - Testler 172 → 178:
    - `OutboxInterceptorTests` (5):
      - yazım ve içeriğin geri dönüşü;
      - iki yönlü rollback: reddedilen outbox satırı entity'yi, reddedilen entity outbox satırını geri alıyor. Hangi insert önce çalışırsa çalışsın aynı transaction kanıtlanıyor;
      - outbox'sız context;
      - event'siz kayıt.
    - `ModuleRegistrationTests`: her modül context'i outbox'ı map'liyor.
    - Önce kırmızı: `AddOutbox` yokken derleme hatası.
  - `has-pending-model-changes` üç context'te temiz.
  - CHANGELOG, `src/Modules/CLAUDE.md` ve `src/BuildingBlocks/CLAUDE.md` güncellendi. R-068, R-069 ve D-031 yazıldı.
- [x] F4a.3 (fecd64c): Eski dispatcher düzeltmesi.
  - `IDomainEventDispatcher` artık scoped (`TryAddScoped`): handler'lar çağıranın scope'undan çözülüyor. `DomainEventDispatcher` internal oldu, çünkü kurucusu internal cache'e bağlı (D-031 (7)).
  - Reflection çağrısının yerine `DomainEventHandlerInvoker<TEvent>` geldi: handler'ları tipli çözüp tipli çağırıyor. Event tipi başına bir kez `MakeGenericType` ile kuruluyor ve `DomainEventHandlerInvokers` singleton'ında tutuluyor. F4a.2'nin dispatcher'ı da bunu kullanacak.
  - Testler 178 → 181 (`DomainEventDispatcherTests`: çağıranın scope'u, bir event'in bütün handler'ları ve yalnız onlar, handler'sız event).
    - Önce kırmızı: eski singleton'da 2 test `Cannot resolve scoped service ... from root provider` ile düştü.
  - Abstractions'ta `IDomainEventDispatcher` ve `DomainEvent` belgeleri güncellendi ("a future outbox" → "the outbox"). CHANGELOG ve `src/BuildingBlocks/CLAUDE.md` da güncellendi.
- [x] F4a.2 (04b21ec): Dispatcher.
  - `OutboxDispatcher : BackgroundService` (internal), `AddCorePersistence` migration servisinden sonra kaydediyor.
    - Açılışta bir tur, sonra `PeriodicTimer(5 sn, TimeProvider)`.
    - Outbox'ı map'leyen her context'te `Id` üzerinden 50'lik keyset parçaları: `ProcessedOnUtc IS NULL AND Attempts < 10`.
    - Mesaj başına handler'lar için ayrı DI scope. Bookkeeping context'i handler'larınkinden ayrı.
  - Processed tablosu `outbox_consumers`, anahtarı (EventId, Handler): biten handler kaydediliyor, yeniden denemede atlanıyor. Hepsi bitince `ProcessedOnUtc` (`ExecuteUpdate`).
  - Hata: change tracker temizleniyor, `Attempts + 1` ve `Error` (≤ 2000 karakter) yazılıyor, Warning log; 10. denemede Error log ve mesaj bırakılıyor.
  - Tip `Type.GetType` ile çözülüyor ve `IDomainEvent` olmalı. Tur düzeyindeki hata döngü sınırında yakalanıp loglanıyor. Açık transaction yok.
  - `OutboxDispatch` migration'ları: `Attempts`, `Error`, `ProcessedOnUtc`, (`ProcessedOnUtc`, `Id`) index'i ve `outbox_consumers`.
  - Test altyapısı (D-031 (8), R-069):
    - `NovaCoreApiFactory`'nin üç context'i adlandırılmış paylaşılabilir in-memory SQLite'a geçti (`Pooling=False`, factory başına benzersiz ad, açık tutulan bir bağlantı). Catalog ve Payments şeması ctor'da oluşturuluyor.
    - **Sürpriz:** dispatcher Api host'unda da çalıştığı için `HealthEndpointsTests`'in "eksik dosya" testi kırmızıya döndü. Dispatcher dosyayı `ReadWriteCreate` ile açıp oluşturuyor, health check de onu sağlıklı görüyordu. Test artık `Mode=ReadWrite` kullanıyor.
  - Testler 181 → 187 (`OutboxDispatcherTests`, 6):
    - bir kez işlenir (sonraki turlar ve yeniden başlatılan host tekrar teslim etmiyor);
    - başarısız handler yeniden deneniyor, başarılı olan tekrar çalışmıyor;
    - 10 denemede bırakılıyor;
    - handler'sız event işlenmiş sayılıyor;
    - `IDomainEvent` olmayan tip başarısız deneme sayılıyor;
    - retrying execution strategy altında, bookkeeping yazımına bir kez enjekte edilen geçici hatada handler bir kez çalışıyor.
  - Önce kırmızı: 6/6 `no such column: Attempts`.
  - **Sürpriz:** `FakeTimeProvider.Advance(1 dk)` 5 sn'lik periyodik zamanlayıcıyı 12 kez tetikliyor. İlk tetik bekleyeni serbest bırakıyor, sonrakiler birikiyor; tek advance iki tur çalıştırabiliyor. Poison testi bu yüzden "tam olarak k" yerine son duruma kadar ilerletiyor. Teknik tests/CLAUDE.md'de.
  - Kasıtlı bozma (commit'lenmedi): handler atlama kapatılınca yeniden deneme testi kırmızıya döndü.
  - 187/187 (iki tam koşu).
  - `has-pending-model-changes` üç context'te temiz.
  - Güncellenenler: README (Domain event'ler ve outbox, cross-cutting tablosu, Veritabanı, Test), kök `CLAUDE.md` (Persistence), `src/BuildingBlocks/CLAUDE.md`, `src/Modules/CLAUDE.md` (handler kaydı, idempotency, Contracts'ta event), `tests/CLAUDE.md`, CHANGELOG, `Program.cs` yorumu, memory `novacore-stack`.
- Faz doğrulaması (2026-09-28, code review'dan önce):
  - `restore --force`: NU uyarısı yok.
  - Release `--no-incremental`: 0 uyarı, 0 hata.
  - Test: 187/187 (Release ve Debug).
  - `list package --vulnerable`: 12 projede açık yok.
  - `has-pending-model-changes`: üç context'te temiz.
- [x] Code review düzeltmeleri (96ddf10): `code-review` (xhigh, `main...faz/4a-outbox`) 15 bulgu verdi.
  - Kapsam içi gerçek hatalar düzeltildi; yeni testlerin üçü de düzeltmeden önce kırmızıydı (R-070). Testler 187 → 190.
    - (#1) Kaydı tutulamayan handler her denemede yeniden çalışırdı. Örnek: kapalı generic bir handler'ın adı sürümlü tip argümanıyla 256 karakteri geçer ve SQL Server kaydı reddeder. Böyle bir handler artık hiç çalıştırılmıyor, deneme başarısız sayılıyor.
    - (#7) Aynı handler tipinin iki kaydı ilk teslimde iki kez çalışıp denemeyi düşürüyordu. Başarılı handler'ın adı teslim içinde `finished`'e ekleniyor.
    - (#4) Tur boyunca tek context'te biriken kayıtlar her `SaveChanges`'i yavaşlatıyordu (birikmiş kuyrukta O(n²)). Her kayıttan sonra `ChangeTracker.Clear()`.
    - (#6) Durdurma sırasında `OperationCanceledException` dışındaki hatalar dispatcher'ı faulted bırakıyordu. Filtreler artık yalnızca host'un kendi iptalini dışarı bırakıyor.
    - (#14) Test: ilk host başarısız bir beklemede açık kalıyordu. Artık `using` bloğunda.
  - Belge düzeltmeleri:
    - (#2) Handler'lar mesaj başına bir scope'u paylaşıyor; `IDomainEventHandler` belgesi ve `src/Modules/CLAUDE.md` "kendi scope'u" diyordu.
    - (#10) Sıra garantisi yok (README, CHANGELOG, XML doc).
    - (#15) `tests/CLAUDE.md`'deki "Unit: No host" ifadesi.
    - (#8) `OutboxMessage.Type`: generic tip argümanları sürüm taşır.
    - (#11) `AddCorePersistence` olmadan event'ler teslim edilmez.
    - (#3) Başarısız kayıttan sonra event'lerin durumu.
  - Gerekli değil (#13): interceptor'daki `DetectChanges`, navigation'la eklenen yeni entity'leri takibe alıyor. Kapatmak event kaybettirirdi (R-070). Neden'i koda yorum olarak yazıldı.
  - Kapsam dışı ve belgelenmiş bulgular "Sonraya bırakılanlar"da: #3, #5, #8, #9, #12.
  - D-kaydı gerekmedi: düzeltmeler uygulama ayrıntısı; D-031'le çelişen bir şey yok.
- Faz doğrulaması, düzeltmelerden sonra (2026-09-28):
  - Release `--no-incremental`: 0 uyarı, 0 hata.
  - Test: 190/190 (Release ve Debug).
  - `has-pending-model-changes`: üç context'te temiz.
  - Outbox testleri art arda 3 koşuda yeşil.

### Faz 4b
Plan onayı 2026-09-28. F4b.1'de kullanıcı event akışını seçti (Q-30, D-034). F4b.2 üç alt adıma bölündü (D-035 (8)).

- [x] F4b.1 (cf51371): Stok kararı.
  - Kullanıcı event akışını seçti (Q-30 → D-034). Diğer iki seçenek: senkron contract, senkron rezervasyon + event'le ödeme kaydı.
  - Uygulama kararları D-035'te, kaynaklar R-075'te. Kod değişmedi.
- [x] F4b.2a (f3602d8): Payments tarafı.
  - Yeni `NovaCore.Modules.Payments.Contracts` projesi (`PaymentCreated`), yalnızca `NovaCore.Abstractions`'a bağlı; slnx'e eklendi. `Catalog.Contracts`'a `StockReserved` ve `StockReservationFailed` eklendi, o da artık `NovaCore.Abstractions`'a bağlı.
  - `Payment` kurucusu `PaymentCreated`'ı raise ediyor. `Complete()`/`Fail()` yalnızca Pending'den çalışıyor, aynı sonuç tekrar gelirse değişiklik yok, diğer geçişler `InvalidOperationException`. `Refund()` ve `PaymentStatus.Refunded` silindi (şema değişmedi, durum string kolon).
  - `SettlePayment` (internal) iki handler taşıyor; `PaymentsModule` onları açıkça kaydediyor. Bilinmeyen ödeme `InvalidOperationException`.
  - Bu adımdan sonra `PaymentCreated`'ın handler'ı yok: dispatcher onu işlenmiş sayıyor, ödeme Pending kalıyor (F4b.2b'ye kadar bugünkü davranış).
  - Testler 190 → 200: `PaymentTests` (5) ve `PaymentSettlementTests` (5). Önce kırmızı: tipler olmadığı için derleme hatası.
- [x] F4b.2b (3deb3c8): Catalog tarafı; akış uçtan uca çalışıyor.
  - `Product.RemoveStock(int)` `Result` dönüyor: aktif olmayan ürün `product.inactive`, yetersiz stok `product.insufficient_stock` ile reddediliyor ve stok değişmiyor. `quantity <= 0` `ArgumentOutOfRangeException`.
  - `StockReservation`: anahtarı PaymentId, durumu Reserved ya da Refused, `Reason` ret kodu. `Reserve`/`Refuse` cevap event'ini raise ediyor. Tablo `stock_reservations`, `products`'a FK yok. Migration `StockReservations`.
  - `ReserveStockForPayment` (internal handler): rezervasyon zaten varsa çıkıyor, ürün yoksa `product.not_found`, her şey tek `SaveChanges`'te. `CatalogModule` handler'ı açıkça kaydediyor; Catalog artık Payments.Contracts'ı referans alıyor.
  - Testler 200 → 212: `ProductStockTests` (6) ve `StockReservationTests` (6: rezervasyon, üç ret nedeni, tekrar teslim, dispatcher'la POST'tan Completed'a). Önce kırmızı: tipler olmadığı için derleme hatası. Yeni test altyapısı: `OutboxDelivery.RunUntilAsync`.
  - **Sapma (plan):** uçtan uca test ödemenin sonucunu `GET /payments` yerine veritabanından okuyor. Liste `CreatedAtUtc`'ye (DateTimeOffset) göre sıralıyor ve SQLite bunu çeviremiyor. Hata kısa bir denemeyle doğrulandı: "SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses" (R-068). SQL Server'da sorun yok; madde "Sonraya bırakılanlar"da.
  - Test tekniği: handler'ı doğrudan çağıran testler ödemeyi outbox'a koymuyor (`ClearDomainEvents`). Aksi hâlde dispatcher'ın ilk turu aynı talebi aynı anda teslim edebilirdi.
  - `has-pending-model-changes` üç context'te de temiz.
  - Güncellenenler: README (proje ağacı, uç nokta tablosu, Modüller Arası İletişim, Domain event'ler, Test), `src/Modules/CLAUDE.md`, `tests/CLAUDE.md`, CHANGELOG.
- [x] F4b.2c (b5f43bc): Stok eşzamanlılık belirteci.
  - `ProductConfiguration`: `Stock` artık `IsConcurrencyToken()`. `has-pending-model-changes` fark görmedi (belirteç şemayı değiştirmiyor), bu yüzden migration eklenmedi.
  - `UpdateProduct` ve `DeleteProduct` `DbUpdateConcurrencyException`'ı 409 `product.concurrency_conflict`'e çeviriyor; iki uca `.ProducesProblem(409)` eklendi (OpenAPI belgesine yeni yanıt).
  - Test altyapısı: `NovaCoreApiFactory.CatalogConnectionString` (protected); `FakeClockApiFactory` artık sealed değil; yeni `StockRaceApiFactory` ve iç `CompetingStockWrite` interceptor'ı. Interceptor, ürünü değiştiren bir sonraki kayıttan önce stoğu başka bir bağlantıyla değiştiriyor.
  - Testler 212 → 214: `StockConcurrencyTests` (PUT ve DELETE, 409; eşzamanlı satışın stoğu korunuyor). Önce kırmızı: belirteç yokken ikisi de 204 aldı. Kasıtlı bozma (commit'lenmedi): belirteç kapatılınca ikisi de yine kırmızı.
  - **Sürpriz:** hiçbir alanı değiştirmeyen PUT UPDATE göndermiyor, dolayısıyla çakışmıyor (doğru davranış). Test gerçek bir yeniden stoklama yapıyor.
  - Güncellenenler: README (Modüller Arası İletişim), `src/Modules/CLAUDE.md` (belirteç kalıbı), `tests/CLAUDE.md`, CHANGELOG.
- [x] F4b.3 (b43bdd7): Eşzamanlılık testi.
  - `StockConcurrencyTests`'e gerçek dispatcher'la iki test eklendi:
    - Son birim için aynı anda iki ödeme: ikisi de 201 Pending, sonra biri Completed biri Failed, stok 0.
    - Rezervasyon stoğu okuduktan sonra başka bir satış son birimi alıyor: ilk teslim `DbUpdateConcurrencyException` ile düşüyor (`Attempts` = 1), yeniden deneme yeni stoğu okuyup reddediyor. Ödeme Failed, stok 0.
  - Testler 214 → 216. Adımın kapsamı yalnızca test olduğu ve davranış F4b.2b/F4b.2c'de geldiği için testler yazıldıkları anda yeşildi.
  - Kasıtlı bozma (commit'lenmedi): stok kontrolü kapatılınca ilk test (iki ödeme de Completed), belirteç kapatılınca ikinci test (bayat stokla Completed) kırmızıya döndü.
  - README Test bölümü güncellendi.
- [x] F5.3 (4bf9706): Test borcu (Faz 5'ten taşındı, D-033).
  - `CatalogEndpointsTests` (3): geçerli gövdeyle oluşturma (201 + Location), okuma, aramayla listeleme; güncelleme (204, yeni değerlerle okunuyor); silme (204, ardından 404 `product.not_found`).
  - "Ödeme yap" maddesini F4b.2b'deki `StockReservationTests` karşılıyor (POST'tan Completed'a, dispatcher'la).
  - Testcontainers eklenmedi (D-035 (7)).
  - Testler 216 → 219. Mevcut davranışı sabitledikleri için kırmızı aşamaları yok.
  - "Sonraya bırakılanlar"daki Faz 2c maddesi ve D-033 listesindeki satır kapandı.
- Faz doğrulaması (2026-09-28, code review'dan önce):
  - `restore --force`: NU uyarısı yok.
  - Release `--no-incremental`: 0 uyarı, 0 hata.
  - Test: 219/219 (Release ve Debug).
  - `list package --vulnerable`: 13 projede açık yok.
  - `has-pending-model-changes`: üç context'te temiz.
- [x] Code review düzeltmeleri (cd91b05): `code-review` (xhigh, `main...faz/4b-referans-moduller`) 13 bulgu verdi.
  - Düzeltilenler:
    - (#1) `Two_payments_for_the_last_unit_sell_it_once` kararsızdı. Açılıştaki tur ya da sınıfın önceki bir testinin saat ilerletmesinden kalan tur, ilk ödemenin stoğunu ikinci POST'un kontrolünden önce ayırabiliyordu; ikinci POST 409 alırdı. Ödemeler artık doğrudan, outbox'a yazılarak kaydediliyor. Kasıtlı bozma: stok kontrolü kapatılınca test yine kırmızı.
    - (#6) Stok belirteci modeli değiştirmişti ama snapshot güncellenmemişti; `has-pending-model-changes` belirteci görmüyor. Boş `StockConcurrencyToken` migration'ı snapshot'ı modelle eşitliyor, şema değişmiyor. Kural: `src/Modules/CLAUDE.md`, "After adding/altering an entity, generate a migration".
    - (#7) `ProductConfiguration` yorumu, yalnızca adı değiştiren bir güncellemenin çakışmayacağını ima ediyordu. Düzeltildi.
    - (#8) README ve CHANGELOG'daki "iki tur içinde" üst sınırı, başarısız teslimlerin eklediği turları söylemiyordu. Düzeltildi.
    - (#10, #11) Test verisi yardımcıları (`AddProductAsync`, `StockAsync`, `PaymentStatusAsync`, outbox sorgusu) üç sınıfta ayrı ayrı yazılmıştı; `ModuleTestData`'da toplandı. `PaymentSettlementTests`'in ödemeleri de artık outbox'a konmuyor.
    - (#12) `OutboxDelivery.RunUntilAsync` zaman aşımında yalın bir `TaskCanceledException` veriyordu; artık açıklayıcı bir `TimeoutException` veriyor.
  - Değişiklik gerekmedi: (#4) `PaymentStatus.Refunded`'ın silinmesi. `Refund()` hiç çağrılmadığı için bu değerle yazılmış satır olamaz.
  - Kapsam dışı ya da kabul edildi, "Sonraya bırakılanlar"da: #2 (zaten listedeydi), #3, #5, #9, #13.
  - `tests/CLAUDE.md` yeni yardımcılara ve "arka planda süren tur" tuzağına göre güncellendi. Testler 219/219.
- Faz doğrulaması, düzeltmelerden sonra (2026-09-28):
  - Release `--no-incremental`: 0 uyarı, 0 hata.
  - Test: 219/219 (Release).
  - `has-pending-model-changes`: üç context'te temiz.
  - Akış ve eşzamanlılık testleri art arda 3 koşuda yeşil (27/27).

### Faz 4c
Plan onayı 2026-10-20. Yeni test altyapısı `CoreWebTestHost` (yalnızca çekirdek web paketiyle kurulan tüketici host'u) F4c.1'de gelir, F4c.3 ve F4c.4 de kullanır. Plan onayı şu test değişikliklerini de kapsıyor (Y-44): `GlobalExceptionHandlerTests` ve `DomainEventDispatcherTests` silinen sınıflarla birlikte gider; super-admin testi ve framework exception durum kodları tasarım gereği değişir; health'in Production testi, `JwtValidationTests` ve `ValidationTests` aynı amaçla kurulum değiştirir.

- [x] F4c.1 (7a060b8): Güvenli varsayılanlar.
  - `AddCoreAuthorization` `FallbackPolicy = RequireAuthenticatedUser` kuruyor. `AllowAnonymous` alanlar: `/connect/authorize|token|endsession`, `/account` grubu, health, OpenAPI belgesi, Scalar. OpenIddict'in discovery/JWKS/revoke istekleri authentication middleware'inde yanıtlandığı için etkilenmiyor (R-076).
  - CORS: `Cors:AllowedOrigins` boşken Development her origin'e izin veriyor, diğer ortamlar hiçbirine.
  - Yeni test altyapısı `CoreWebTestHost`. Testler 219 → 227: `SecureDefaultsTests` (meta verisiz uç 401/200, beş açık uç anonim 200, Production'da CORS kapalı, Development'ta açık). Önce kırmızı: meta verisiz uç 200, Production CORS `*` döndü.
  - Kasıtlı bozma (commit'lenmedi): eklentideki `AllowAnonymous` işaretleri kaldırılınca kimlik akış testlerinden 11'i kırmızı.
  - Değişen test: health'in Production testi kimlikli client'la 404'ü doğruluyor; anonim istek artık 401 alıyor (tanımsız yola fallback, MS'in belgelediği davranış).
  - Kararlar D-036 (1)'de. Güncellenenler: README (Konfigürasyon, Yetkilendirme), kök/BuildingBlocks/eklenti/tests CLAUDE.md, CHANGELOG, memory `novacore-security`.
- [x] F4c.2 (c1f0075): Super-admin sunucu tarafında.
  - Çekirdekte super-admin kalmadı: `PermissionAuthorizationHandler` ve `HttpContextCurrentUser.HasPermission` yalnızca çözülen izinlere bakıyor, `SuperAdminRole` silindi (public API, 0.x).
  - Eklentinin resolver'ı store'daki `Administrator` üyeliğini modüllerin tanımladığı bütün izinlere çözüyor, seed'in o izni role yazıp yazmadığından bağımsız. `IPermissionResolver` değişmedi.
  - Testler 227 → 229. Önce kırmızı: token rolüyle 200, seed'in yazmadığı izin çözülmedi, rolden çıkarılan admin önbellek süresinden sonra da 200 aldı. Yeni: `AdministratorRemovalTests` (sahte saat; admin ucu ve `/probe` 403), resolver testi. Değişenler: super-admin testi 403 bekliyor; `JwtValidationTests` sunucuda izin verilmiş bir okuyucu, `ValidationTests` tanımlı bütün izinler kullanıyor.
  - Kararlar D-036 (2), R-077. Güncellenenler: README (Yetkilendirme), kök ve eklenti CLAUDE.md, CHANGELOG, memory `novacore-security`.
- [x] F4c.3 (1c6751c): Forwarded headers.
  - `UseCoreWeb` `UseForwardedHeaders()`'ı en başta çalıştırıyor. `ForwardedHeaders:KnownProxies` (IP) ve `KnownIPNetworks` (CIDR) yeni internal `TrustedProxiesOptions`'a bağlanıyor ve açılışta doğrulanıyor; dolu listede yalnızca For + Proto uygulanıyor, framework'ün loopback varsayılanlarının yerine bu liste geçiyor. Boş listede options'a dokunulmuyor.
  - Plan "ForwardedHeadersSettings" adını anıyordu; diğer options tipleriyle aynı biçimde `TrustedProxiesOptions` oldu.
  - Testler 229 → 238: `ForwardedHeadersTests` (`CoreWebTestHost`: güvenilen proxy ve ağ istemciyi ve şemayı belirler, başka kaynak ve dolu listede loopback yok sayılır, liste yokken hiçbir şey uygulanmaz, "strict" limiter forwarded istemciye göre bölünür, dört geçersiz giriş açılışı durdurur). Önce kırmızı: 6 test.
  - **Sürpriz:** `IPAddress.TryParse("10.0.0")` başarılı ve `10.0.0.0` dönüyor; IPv4 için dört bölüm şartı eklendi (R-078).
  - Api `appsettings.json`'a boş `ForwardedHeaders` bölümü eklendi. "Sonraya bırakılanlar"da 3 madde kapandı, 1'i kısmen.
  - Kararlar D-036 (3), R-078. Güncellenenler: README (Konfigürasyon, Ek sertleştirme), BuildingBlocks CLAUDE.md, CHANGELOG.
- [x] F4c.4 (f28d973): Projenin hata eşlemesi.
  - `GlobalExceptionHandler` silindi (public API). Yerine internal `ExceptionMapping`: `ExceptionHandlerOptions.StatusCodeSelector`, `CustomizeProblemDetails` içinde başlık/`type`/`detail` ve `SuppressDiagnosticsCallback`. `ErrorType` → durum/başlık eşlemesi artık `ResultHttpExtensions` ile ortak (`internal`).
  - Davranış: `AppException` durumu ve mesajı aynı; `BadHttpRequestException` kendi durumu (bozuk JSON 400); diğer her şey, sunucunun kendi `OperationCanceledException`'ı dahil, mesajsız 500. 500'leri framework'ün `UnhandledException` logu yazıyor; istemci hataları ve projenin handler'larının işledikleri loglanmıyor.
  - Testler 238 → 244: `ExceptionMappingTests` (13) geldi, `GlobalExceptionHandlerTests` (7) sınıfla birlikte gitti (plan onayı, Y-44). Önce kırmızı: 7 test (sonradan kaydedilen handler çalışmıyordu, framework exception'ları 4xx/499, bozuk JSON 500). Kasıtlı bozma: bastırma kapatılınca log testi kırmızı (commit'lenmedi).
  - Kararlar D-036 (4), R-079. "Sonraya bırakılanlar"da `BadHttpRequestException` maddeleri kapandı; review #9'un koşul notu güncellendi (kanca artık var, kod değişmedi).
  - Güncellenenler: README (Cross-cutting tablosu), BuildingBlocks CLAUDE.md, CHANGELOG.
- [x] F4c.5 (2cc1743): Tipli yanıt sözleşmesi.
  - `ToHttpResult` union dönüyor: `Results<NoContent, ProblemHttpResult>`, `Results<Ok<T>, ProblemHttpResult>`, `Results<TSuccess, ProblemHttpResult>`. `Problem(Error)` → `ProblemHttpResult`. `(Result, int)` overload'u silindi (çağıranı yoktu). Public API değişikliği (0.x).
  - Modül handler'ları `Task<Results<…>>` dönüyor; başarı için yazılan `.Produces<T>()`/`.Produces(204)` satırları kalktı, `.ProducesProblem(...)` kaldı (`ProblemHttpResult` meta veri üretmiyor, R-080).
  - Kimlik eklentisinin `/api/v1/auth/*` ve `/api/v1/admin/*` uçları tipli sonuçlara geçti. Anonim gövdeler aynı JSON'u üreten internal record'lar oldu: `MessageResponse`, `RecoveryCodesResponse`, `RoleSummary`, `PermissionGroup`. `/connect/*` ve `/account/*` `IResult` kaldı.
  - OpenAPI farkı (adım öncesi ve sonrası, geçici bir snapshot testiyle; commit'lenmedi):
    - 7 modül ucunun yanıtları bayt bayt aynı.
    - 13 kimlik ucu şemasız `200`'den tipli yanıtlara geçti: gövde şemaları, `204`'ler, 400 validation, 404/409 mesajları. `logout-all`, `change-password`, `2fa/disable` ve admin rol uçları artık gerçekteki `204`'ü bildiriyor.
    - Hiçbir şema silinmedi, 9 şema eklendi, `responses` dışındaki kısım aynı.
  - Testler 244 → 245: `EndpointContractTests.Described_endpoints_return_typed_results`. Önce kırmızı: kimlik ve modül uçlarındaki düz `IResult` handler'lar. Yanıt gövdelerini mevcut uç testleri koruyor.
  - Kararlar D-036 (5), R-080. "Sonraya bırakılanlar"da Y-31 kör noktası kapandı. Güncellenenler: README (Result, slice anatomisi), kök/Modules/BuildingBlocks/eklenti CLAUDE.md, CHANGELOG.
- [x] F4c.6 (7365696): Data Protection anahtar deposu koruması.
  - Eklenti `KeyManagementOptions`'ı `Validate<IHostEnvironment>` + `ValidateOnStart` ile doğruluyor: Development dışında `XmlRepository` boşsa (hiçbir `PersistKeysTo*` çağrılmamışsa) host açılmıyor; hata mesajı seçenekleri sayıyor. Depo paketi eklenmedi; depoyu host seçiyor.
  - Test altyapısı: `InMemoryKeyStore` (`IXmlRepository`); `NovaCoreApiFactory` Development dışında onu da ekliyor.
  - Testler 245 → 247: depo yokken Production host'u açılmıyor, depo varken açılıyor; Development'ta framework varsayılanı yeterli. Önce kırmızı: depo olmadan açılıyordu.
  - İmza anahtarı eksikliğini ve e-posta göndericisi eksikliğini bekleyen iki mevcut test, kendi hatalarını görmeye devam etsin diye depoyla kuruluyor (plan).
  - Kararlar D-036 (6), R-081. Güncellenenler: README (Üretimde mutlaka), eklenti ve tests CLAUDE.md, CHANGELOG.
- [x] F4c.7 (cd269cb): Outbox şeması ve bakımı.
  - `outbox_consumers`: anahtar (`MessageId`, Handler); `MessageId` → `outbox_messages.Id`, cascade delete'li FK. Dispatcher kayıtları mesajın `Id`'siyle okuyup yazıyor.
  - Saklama: her turun sonunda `ProcessedRetention`'dan (7 gün) eski işlenmiş mesajlar siliniyor. Süre, `Id` sırasıyla okunan 50'lik parçalarda bellekte karşılaştırılıyor (SQLite `DateTimeOffset`'i SQL'de karşılaştıramıyor). İncelemeye bırakılan mesaj kalıyor.
  - `IDomainEventDispatcher` (Abstractions, public) ve `DomainEventDispatcher` silindi; `DomainEventHandlerInvokers` dispatcher için kaldı.
  - Migration `OutboxConsumersByMessage` (Catalog, Payments): EF'in ürettiği adımlar mevcut kayıtları kaybettirirdi, elle yeniden yazıldı. Kayıtlar `EventId` üzerinden mesajın `Id`'sine dönüşüyor, sahipsiz kayıt siliniyor; Down tersini yapıyor.
  - **Sürpriz 1:** `migrations script` migration'ı tek batch üretiyor; yeni kolonu kullanan ifade `EXEC(N'…')` içine alındı. Yerel SQL Server'da geçici bir veritabanında denendi: script ile Up, çalışma zamanında Down ve Up, Payments'ın bütün zinciri. Veritabanı silindi (R-082).
  - **Sürpriz 2:** `FakeTimeProvider` 7 günlük ilerletmede 5 sn'lik zamanlayıcıyı ~120 bin kez tetikliyor (test başına ~55 sn). Saklama testleri satırları zaman damgasıyla yaşlandırıyor (tests/CLAUDE.md'ye not).
  - Testler 247 → 246: `OutboxDispatcherTests`'e saklama süresi geçen mesajın kayıtlarıyla silinmesi ve incelemeye bırakılan mesajın kalması (+2), tüketici sorgusu `MessageId` üzerinden. `DomainEventDispatcherTests` (3) sınıfla birlikte gitti (plan onayı, Y-44). Önce kırmızı: 4 test (kolon yok, temizlik yok). Kasıtlı bozma: temizlik çağrısı kapatılınca saklama testi kırmızı (commit'lenmedi).
  - `has-pending-model-changes` üç context'te temiz.
  - Kararlar D-036 (7) (D-031 (5) ve (7) revizyon kuralıyla işaretlendi, D-033 (7)), R-082. "Sonraya bırakılanlar"da 4 madde kapandı.
  - Güncellenenler: README (outbox, proje ağacı), kök/BuildingBlocks/tests CLAUDE.md, CHANGELOG.

- [x] Pkcs açığı (df35563): faz doğrulamasında `restore --force` NU1903 verdi. 13 Ekim'de yayımlanan `System.Security.Cryptography.Pkcs` açıkları (CVE-2026-84008, CVE-2026-70162) Microsoft.Data.SqlClient üzerinden geliyordu ve en yeni EF/SqlClient da açıklı sürüme bağlı (R-083). Kullanıcıya seçenekler soruldu; soruyu reddedip "devam et" dedi, önerilen seçenek uygulandı: SQL Server sağlayıcısını kullanan projelere doğrudan Pkcs 10.0.13 (D-036 (8)).
- Faz doğrulaması (2026-10-20, code review'dan önce):
  - `restore --force`: Pkcs düzeltmesinden sonra NU uyarısı yok (önce NU1903).
  - Release `--no-incremental`: 0 uyarı, 0 hata.
  - Test: 246/246 Debug. Release'te bir koşuda `ConfigurationValidationTests`'in Production/`http` Authority satırı kırmızı oldu; mesajı yakalanamadı, sonraki 3 sınıf koşusu ve 2 tam Release koşusu yeşil. R-055'in (açılış hatası bekleyen testlerde aralıklı `ObjectDisposedException`) etkilediği test ailesinden; kanıtsız olduğu için kesin neden yazılmadı.
  - `list package --vulnerable`: 13 projede açık yok.
  - `has-pending-model-changes`: üç context'te temiz.
- [x] Code review düzeltmeleri (fed25c4): `code-review` (high, `main...faz/4c-tuketici-hazirligi`) 5 bulgu verdi.
  - Düzeltilenler:
    - (#1) Exception eşlemesi `CustomizeProblemDetails` içindeydi ve exception taşıyan her ProblemDetails yazımını eziyordu; projenin handler'ının yazdığı başlık ve detay da gidiyordu. Eşleme `ExceptionHandlerOptions.ExceptionHandler` delegate'ine taşındı; middleware onu yalnızca hiçbir handler işlemediğinde çağırıyor. Kırmızı test: `The_problem_details_a_project_handler_writes_are_its_own`.
    - (#2) Saklama taraması, hâlâ saklanan bir mesaj içeren ilk parçada duruyordu; geç işlenmiş düşük Id'li bir mesaj turu 49 silmeyle sınırlıyordu. Artık süresi dolmuş mesaj içermeyen ilk parçada duruyor. Kırmızı test: `A_message_processed_late_does_not_hold_back_the_expired_ones_after_it` (eski kuralla zaman aşımı).
    - (#3) Api host'u SQL Server sağlayıcısını kendisi referans alıyor; Pkcs düzeltmesini yalnızca modüller üzerinden alıyordu. Api'ye de doğrudan referans eklendi.
  - "Sonraya bırakılanlar"a yazılanlar: #4 (her turda tarama sorgusu), #5 (iç içe `MessageResponse`).
  - Testler 246 → 248.
- Faz doğrulaması, düzeltmelerden sonra (2026-10-20):
  - `restore --force`: NU uyarısı yok.
  - `list package --vulnerable`: 13 projede açık yok.
  - Release `--no-incremental`: 0 uyarı, 0 hata.
  - Test: 248/248, Release ve Debug.
  - `has-pending-model-changes`: üç context'te temiz.

### Faz 4d (koşullu)
Plan onayı 2026-10-20. Çalışma ağacındaki izlenmeyen `AGENTS.md`, `.agents/` ve `.codex/` dosyalarına dokunulmuyor (kullanıcı kararı); commit'ler dosya dosya stage'leniyor. Plan onayı şu test değişikliğini de kapsıyor (Y-44): `End_session_signs_the_browser_out_of_the_provider` endsession'ın yeni onay adımını geçiyor.

- [x] F4d.1 (88ed38d): IdP modeli (a) kendi host'unda eklenti (Q-31, D-037). Faz yürüyor.
- [x] F4d.2 (02f8ccf): Parola sıfırlama, onay e-postasını yeniden gönderme, `User` rolü yokken kayıt.
  - Sıfırlama sağlayıcının HTML sayfalarıyla: giriş sayfasında "Forgot your password?" → `/account/forgot-password` → e-postadaki link → `/account/reset-password`. Yanıt her adres için aynı, link yalnızca onaylı hesaba gidiyor. Yeni parola iki kez giriliyor. Sıfırlama bütün oturumları iptal ediyor, otomatik giriş yapmıyor.
  - Yeniden gönderme JSON uç: `POST /api/v1/auth/resend-confirmation-email`, yalnızca onaysız hesaba e-posta, yanıt aynı. Yeni public gövde `ResendConfirmationEmailRequest`.
  - Kayıtta `User` rolü yoksa hesap rolsüz oluşuyor (bugün 500).
  - Ortak parçalar: `HtmlPage` (LoginPage'in sayfa/form kodu taşındı), `EmailLinks` (kayıt ve sıfırlama linkleri, token Base64Url). `/account` grubu `MapCoreIdentityServer`'da tek yerde kuruluyor. Audit: `password_reset_requested`, `password_reset`.
  - Testler 248 → 264: yeni `PasswordResetTests` (11: uçtan uca sıfırlama ve oturumların bitmesi, tek kullanım, bilinmeyen/onaysız adres aynı yanıt ve e-posta yok, iki parola, politika, bozuk token ile bilinmeyen kullanıcı aynı yanıt, Host başlığı, giriş sayfasındaki link, üç uçta "strict"); `RegistrationTests` +3 (yeniden gönderme yalnızca onaysız hesaba, yeniden gönderilen link onaylıyor, seed'siz kayıt); `ValidationTests`, `SecureDefaultsTests` ve `ModuleRegistrationTests` listelerine yeni uçlar. Önce kırmızı: 16 test (biri yanlışlıkla yeşildi, kayıt e-postasını okuyordu; ikinci e-postayı şart koşacak şekilde düzeltildi).
  - Kararlar D-038 (1)–(4), R-084. Güncellenenler: README (uçlar, kimlik sağlayıcı, hesaplar, yetkilendirme, audit), eklenti CLAUDE.md, CHANGELOG.
- [x] F4d.3 (f428ff3): Kurtarma koduyla giriş.
  - 2FA adımı tek alan: altı haneli giriş authenticator koduna, gerisi kurtarma koduna gidiyor (`XXXXX-XXXXX`, büyük harfe çevriliyor). Kod tek kullanımlık (`RedeemTwoFactorRecoveryCodeAsync`).
  - **Sürpriz (R-085):** `SignInManager.TwoFactorRecoveryCodeSignInAsync` lockout'a bakmıyor ve hatalı kodu saymıyor. Eklenti ikisini kendisi yapıyor: kilitli hesap kodla giremiyor, hatalı kod başarısız deneme sayılıyor.
  - Audit: `recovery_code_used` (Warning, EventId 27); hatalı kodun nedeni `bad-recovery-code`.
  - Testler 264 → 267: `TwoFactorTests` +3 (kod bir kez giriş yapar ve küçük harfle de kabul edilir, hatalı kod sayılır, kilitli hesap kodla giremez). `EnableAsync` yardımcısı kurtarma kodlarını da döndürüyor. Önce kırmızı: 3 test.
  - Kararlar D-038 (5), R-085. Güncellenenler: README (Hesaplar), eklenti CLAUDE.md (Hardening), CHANGELOG.
- [x] F4d.4 (e91fcac): Güvenlik düzeltmeleri.
  - Logout CSRF: `/connect/endsession` oturumlu tarayıcıya önce bir soru gösteriyor. Formu isteğin parametrelerini antiforgery token'ıyla aynı uca POST ediyor ve yalnızca bu POST çıkış yapıyor (OpenIddict'in Velusia örneği). Oturumsuz tarayıcı doğrudan yönlendiriliyor.
  - Zamanlama: bilinmeyen e-postada parola, host'un hasher'ıyla bir kez üretilen sahte hash'e karşı doğrulanıyor (`DummyPasswordHash`). `SignInManager` bilinmeyen kullanıcıda hiç hash'lemiyordu.
  - `change-password`: kilitli hesapta parola değişmiyor, yanlış mevcut parola `AccessFailedAsync` çağırıyor, başarı sayacı sıfırlıyor. Hash tek kez.
  - Testler 267 → 273:
    - `AuthorizationCodeFlowTests` +3: GET soru gösteriyor ve oturum sürüyor; antiforgery'siz POST oturumu kapatmıyor; oturumsuz tarayıcı soru görmüyor. Sonuncusu mevcut davranışı sabitliyor, önceden de yeşildi.
    - `SignInHardeningTests` +1: sayan bir hasher'la, bilinmeyen e-posta bilinen kadar hash doğruluyor.
    - `SessionTests` +2: 5 yanlış mevcut parola kilitliyor; başarılı değişim sayacı sıfırlıyor.
    - Değişen test (plan onayı, Y-44): `End_session_signs_the_browser_out_of_the_provider` onay adımını geçiyor.
    - Önce kırmızı: 6 test.
  - Kararlar D-038 (6)–(8), R-086, R-087. Güncellenenler: README (Hesaplar, Endpoint'ler), eklenti CLAUDE.md, CHANGELOG (Security).
- [x] F4d.5 (6d42236): Token temizliği.
  - Yeni `ExpiredTokenPruner` (`BackgroundService`): ilk tur açılışta, sonra saatte bir. Her tur önce token'lar, sonra authorization'lar için OpenIddict'in `PruneAsync`'ini 14 günlük eşikle çağırıyor; değerler OpenIddict.Quartz'ın varsayılanları (R-088). Yeni paket yok. Tur hatası loglanıyor, host durmuyor.
  - **Sürpriz:** OpenIddict'in store'u süreyi veritabanının saatiyle (`DateTime.UtcNow` sorgunun içinde) karşılaştırıyor, eşik ise `TimeProvider`'dan geliyor. Test süre tarihlerini gerçek saate göre veriyor.
  - Test altyapısı: `IdentityServerTestHost` tek bir `SqliteConnection` paylaşıyordu. Arka plandaki tur istekle aynı anda o bağlantıyı kullanacağı için host, Api factory'si gibi adlı paylaşımlı in-memory veritabanına ve context başına bağlantıya geçti (R-069). Değişiklikten sonra eklentinin 95 testi temizlik servisi eklenmeden yeşildi.
  - Testler 273 → 274: `TokenPruningTests` (sahte saat bir saat ilerliyor; süresi dolmuş token ve iptal edilmiş authorization siliniyor, geçerli kalıcı authorization ve yeni token kalıyor). Önce kırmızı. Kasıtlı bozma (commit'lenmedi): saat ilerletilmeyince test kırmızı, yani silmeyi saatlik tur yapıyor. Api host'unun testlerinde kimlik şeması çoğunlukla yok; açılış turu hata loglayıp devam ediyor, host durmuyor.
  - Kararlar D-038 (9), R-088. Güncellenenler: README (Kimlik sağlayıcı), eklenti ve tests CLAUDE.md, CHANGELOG.

- Faz doğrulaması (2026-10-20, code review'dan önce):
  - `restore --force`: NU uyarısı yok.
  - Release `--no-incremental`: 0 uyarı, 0 hata.
  - Test: 274/274, Debug ve Release.
  - `list package --vulnerable`: 13 projede açık yok.
  - `has-pending-model-changes`: üç context'te temiz.
- [x] Code review düzeltmeleri (2e81b2e): `code-review` (high, `main...faz/4d-kimlik-uretim`) 7 bulgu verdi.
  - Düzeltilenler:
    - (#2) `change-password`'daki yanlış parolalar hesabı kilitleyince audit olayı yazılmıyordu (giriş sayfası `account_locked_out` yazıyor). Artık yazılıyor. Kırmızı test: `A_lockout_from_wrong_current_passwords_is_audited`.
    - (#4) Altı hane olmayan her giriş kurtarma kodu sayılıyordu; yanlış uzunlukta bir authenticator kodu "Invalid recovery code." alıyordu. Artık yalnızca rakam olan giriş authenticator kodu (kurtarma kodları her zaman tire içeriyor). Kırmızı test: `A_code_of_digits_only_is_an_authenticator_code_whatever_its_length`.
    - (#3) Temizlik turunda token adımı hata verince authorization adımı atlanıyordu. Artık iki adımın hata sınırı ayrı (OpenIddict.Quartz da devam ediyor). Hata üretmek zor olduğu için testi yok.
    - (#6) `Register` aynı `ValidationProblem` biçimini elle kuruyordu; `PasswordProblem`'i kullanıyor.
  - "Sonraya bırakılanlar"a yazılanlar: #1 (yeniden gönderme ucunda zamanlama farkı, forgot-password maddesine eklendi), #5 (çıkış sorusunda vazgeçme yolu yok), #7 (128 karakter sınırı iki yerde).
  - Testler 274 → 276.
- Faz doğrulaması, düzeltmelerden sonra (2026-10-20):
  - `restore --force`: NU uyarısı yok.
  - Release `--no-incremental`: 0 uyarı, 0 hata.
  - Test: 276/276, Debug ve Release.
  - `list package --vulnerable`: 13 projede açık yok.
  - `has-pending-model-changes`: üç context'te temiz.

### Faz 5
Plan onayı 2026-10-20. Uygulama sırası F5.4 → F5.1a → F5.1b → F5.1c → F5.2 → F5.5 (ID'ler sabit). Kanal ve ilk sürüm plan aşamasında soruldu (Q-32, Q-33). Çalışma ağacındaki izlenmeyen `AGENTS.md`, `.agents/` ve `.codex/` dosyalarına dokunulmuyor (4d'deki kullanıcı kararı); commit'ler dosya dosya stage'leniyor.

- [x] F5.1a (d3c612b): Tek log hattı OpenTelemetry (R-089, D-040 (1)–(2)).
  - **Sürpriz:** `UseOtlpExporter`, paketin kendi dokümanına göre log'ları zaten açıyordu ("automatically enables logging, metrics, and tracing"). Yani endpoint tanımlıyken log'lar OTLP'ye gidiyordu, ama biçimlenmiş mesaj ve scope'lar olmadan. Adım bu yüzden plandakinden dar: `AddCoreObservability` log sinyalini `WithLogging` ile açıkça kuruyor, `IncludeFormattedMessage` ve `IncludeScopes` açık (MS'in OTLP örneği ve Aspire ServiceDefaults gibi).
  - Repo'nun Api host'u Serilog'da kalıyor; Serilog diğer provider'lara yazmadığı için davranış aynı.
  - Testler 276 → 277: `ObservabilityTests` (OTel log provider'ı kayıtlı, iki seçenek açık). Önce kırmızı.
  - Güncellenenler: CHANGELOG (Changed), `src/PACKAGE.md`, README (OTel satırları), eklentinin csproj açıklaması.
- [x] F5.1b (216741f): Template package `BMD.NovaCore.Templates` (`src/Templates/NovaCore.Templates`), D-040 (3)–(7), D-041, R-090, R-091.
  - MS templatepack düzeni; kökteki `.template.config` silindi. Derlenen kodu olmadığı için `Directory.Build.targets`'taki sembol, package validation, XML doc ve PublicAPI kuralları şablon paketine uygulanmıyor (`NovaCoreLibraryPackage`). NU5128 `SuppressDependenciesWhenPacking` ile.
  - Üretilen projenin NovaCore sürümü pack anında yazılıyor (`TargetsForTfmSpecificContentInPackage`); `--version-suffix` da geçiyor.
  - İçerik: Api host (`Program.cs`, kontrol listeli `appsettings`, Development dışı probe örneği, üretilen portlar), `Todos` örnek modülü (4 slice, izinler, validation, `Result`→ProblemDetails, `todos` şeması, `Initial` migration, outbox), test projesi (`ApiFactory`, `TestIdentityProvider`, 7 test + BuiltIn'de discovery testi), `nuget.config` (source mapping), CPM, TWAE. `--auth BuiltIn|External`.
  - **Sürpriz 1 (R-090):** `ContentTargetFolders=content` içeriği `content/content/...` altına koydu; `PackagePath="content"` ile düzeldi.
  - **Sürpriz 2 (R-091, Q-34):** nuget.org'da başka sahiplerin `NovaCore.Templates`, `NovaCore.AspNetCore`, `NovaCore.EntityFrameworkCore` paketleri var; `dotnet new`'in güncelleme kontrolü yabancı şablonu önerdi. Kullanıcı kararıyla bütün paket ID'leri `BMD.NovaCore.*` (D-041); assembly ve namespace'ler aynı.
  - Doğrulama (`bash eng/template-smoke.sh`, yalıtılmış hive ve NuGet önbelleği): BuiltIn 8/8, External 7/7 test; TWAE altında 0 uyarı; `has-pending-model-changes` iki seçenekte temiz; repo'ya özgü dosya, yazar adı ya da çözülmemiş token yok.
  - Hook: şablonun `Directory.Packages.props`'u ilk kez Bash'le yazılmıştı; paket denetiminden geçmesi için Write ile yeniden yazıldı.
  - Şablon `src/` altında olduğu için mimari testler onun `.cs` dosyalarını da tarıyor: `SourceHygieneTests` şablonun test yardımcısındaki `TimeProvider.System.GetUtcNow()`'ı yakaladı. Test gevşetilmedi; `TestIdentityProvider` saati constructor'dan alıyor.
  - Repo: 0 uyarı, 277/277.
  - Güncellenenler: README (template satırları, proje ağacı, paket ID'leri), kök CLAUDE.md (komutlar), `src/PACKAGE.md`, CHANGELOG.
- [x] F5.1c (a1bfe90): Üretilen projenin CI'ı (`content/novacore/.github/workflows/ci.yml`), D-040 (8), R-092.
  - Adımlar: restore (NuGet audit), Release build, test, context başına `has-pending-model-changes` ve migration bundle (artifact), açık paket kontrolü. Feed token'ı `NOVACORE_PACKAGES_TOKEN` secret'ından, yalnızca restore, bundle ve açık kontrolü adımlarına.
  - **Sürpriz:** self-contained bundle context başına ~180 MB, framework-dependent ~82 MB. Bundle'lar framework-dependent ve yalnızca varsayılan dala push'ta üretilip 7 gün saklanıyor; private repo'nun artifact kotası her push'ta dolmasın.
  - Doğrulama: adımlar üretilen BuiltIn projede yerelde çalıştı (0 uyarı, 8/8, iki context temiz, iki bundle, açık yok; `jq` yerine Python eşdeğeri). YAML iki seçenekte de ayrıştırılıyor; External'da kimlik context'i yok.
- [x] F5.2 (d1fb612): Tüketici smoke testi CI'da: `.github/workflows/ci.yml`'de `build-and-test`'e paralel `template-smoke` job'ı, `bash eng/template-smoke.sh "$RUNNER_TEMP/template-smoke"` (D-040 (9)).
  - Script F5.1b'de kabul kriterinin aracı olarak commit'lenmişti; bu adım onu CI'a bağlıyor.
  - Doğrulama: script yerelde yeşil (BuiltIn 8/8, External 7/7, migration'lar temiz, üretilen `ci.yml` iki seçenekte doğru). Workflow YAML olarak ayrıştırılıyor. Remote CI koşusu push'tan sonra görülecek (push kullanıcı onayıyla).
  - Güncellenenler: README (Test), CHANGELOG.
- [~] F5.3 → Faz 4b'ye taşındı (D-033)
- [x] F5.4 (a7f6bdf): Kanal GitHub Packages (D-013 kalır), ilk sürüm `0.1.0-preview.1` (Q-32, Q-33 → D-039). Kod değişmiyor; `release.yml` şablon paketini de aynı sürümle yayımlayacak (F5.1b'den sonra doğrulanır). Tag ve push faz sonunda ayrıca sorulacak.
- [x] F5.5 (4cc3195): Final dokümanlar.
  - README: yeni "Yeni Proje (Tüketici Rehberi)" bölümü: kurulum (feed, token, şablon, `--auth`), şablonun ürettikleri, sorumluluk modeli, yeni proje kontrol listesi, ezilebilir ayarlar tablosu (kod okunarak; `JwtBearerOptions` için `Configure` şart, R-056), nesne düzeyi yetki rehberi (resource-based authorization, OWASP API1), BFF önerisi, migration dağıtımı (bundle / idempotent script), güncelleme. Eski "Hızlı Başlangıç" artık "Bu Repoda Çalışmak". Paket/lisans tablosu ve mediator satırları (Y-01) düzeltildi.
  - CHANGELOG: `[0.1.0-preview.1] - 2026-10-20` başlığı (tag onayı faz sonunda; onay gelmezse başlık `Unreleased`'a döner).
  - CLAUDE.md'ler: kök (paket ID'leri, şablon, mediator → Y-01, Y-10 → Y-44), `tests/CLAUDE.md` ve `Directory.Build.props` yorumu (Y-44), `src/BuildingBlocks/CLAUDE.md` (OTel log'ları, paket ID'leri), yeni `src/Templates/CLAUDE.md`. `src/PACKAGE.md` (outbox). Memory: `novacore-project`, `novacore-stack`, index.
  - "Sonraya bırakılanlar": bu fazda kapanan 7 madde işaretlendi, Faz 5'ten 7 yeni madde.
  - D-032 kullanıcıya bırakıldı: `docs/plan/DECISIONS.md` `.claude/hooks/package-policy.json` `protectedPaths`'e geri eklenecek (hook dosyasını model düzenleyemiyor).

- Faz doğrulaması (2026-10-20, code review'dan önce): `restore --force` NU uyarısı yok; Release `--no-incremental` 0 uyarı, 0 hata; test 277/277; `list package --vulnerable` 14 projede açık yok; `has-pending-model-changes` üç context'te temiz; `bash eng/template-smoke.sh` yeşil (BuiltIn 8/8, External 7/7).
- [x] Code review düzeltmeleri (3423e71): `code-review` (high, `main...faz/5-sablon-yayin`) 7 bulgu verdi.
  - Düzeltilenler:
    - (#1) Sürüm damgalama hedefi dosyayı ters bölülü bir yolla `File.ReadAllText`'e veriyordu; Linux'taki pack (release.yml, `template-smoke`) dosyayı bulamayabilirdi. Yollar ileri bölü. Linux'ta ilk doğrulama push'tan sonra CI'da (Docker bu makinede kapalı).
    - (#2) Port yer tutucuları çıplak sayılardı (`5443`, `5080`); template engine her geçişi değiştirdiği için ileride üretilecek bir migration'ın zaman damgası bozulabilirdi. Yer tutucular `NOVACORE_HTTPS_PORT` / `NOVACORE_HTTP_PORT`; smoke testi çözülmemiş yer tutucuyu da arıyor.
    - (#3) Şablonun test factory'si izinleri issuer'a göre seçiyordu; BuiltIn'de sağlayıcının issuer'ı da test issuer'ı olduğu için sağlayıcının token'ları host'un resolver'ına hiç gitmiyordu. Artık subject'e göre: yalnızca `CreateClientWithPermissions`'ın yarattığı subject'ler verilen izinleri alır.
    - (#7) Script, varsayılan `mktemp -d` çalışma klasörünü çıkışta siliyor.
  - "Sonraya bırakılanlar"a yazılanlar: #4 (örnek `CompleteTodo`'da eşzamanlı iki tamamlama ikisi de 204), #5 (fork ve Dependabot PR'larında secret olmadığı için üretilen CI'ın restore'u 401), #6 (`template-smoke` job'ı çözümü bir kez daha derliyor).

### Faz 6a
Kaynak: [proposals/guvenlik-kalite-kararlari.md](proposals/guvenlik-kalite-kararlari.md) (`oneri/guvenlik-kalite`, `afbb879`; faz branch'ine `--no-ff` merge edildi). Repo 2026-10-21'de public oldu (API'de `public`). Faz başında secret scanning, push protection ve Dependabot security updates kapalıydı; ruleset ve environment yoktu, CodeQL default setup kurulmamıştı. Faz branch'i faz sırasında push ediliyor (Q-38).

- [x] F6a.1 (d164b33): Öneri branch'i merge edildi (`d1ddee9`).
  - DECISIONS: D-042…D-049 (K1–K8) ve D-050 (D-039 (1)'in yerine: paketler public, token gerekliliği sürüyor). D-035 (7) ve D-039 (1) revizyon kuralıyla işaretlendi.
  - PLAN: faz sırası `… → 5 → 6a → 6b → 6c → 6d → 6e`; Faz 6a–6e adım tabloları belgenin §3'ünden.
  - PROGRESS: Q-35…Q-38, D-032 notu 6e sonuna kaydı. RESEARCH: R-093 (paket görünürlüğü ve token), R-094 (lock dosyası ve `rollForward`).
  - Başlangıç durumu: build 0 uyarı, 277/277 yeşil.
- [x] F6a.2 (b0b158d): Workflow sertleştirme (D-051 (1)–(3), R-095).
  - `ci.yml`, `release.yml` ve şablonun `ci.yml`'i: action'lar SHA'ya sabit (`# vX.Y.Z`), `persist-credentials: false`, workflow düzeyinde `permissions: {}` ve job düzeyinde izinler, `timeout-minutes`, `concurrency`. `release.yml`'deki `${{ }}` interpolasyonu `env:`'e taşındı.
  - Yeni `eng/lint-workflows.sh`: actionlint 1.7.12 + zizmor 1.31.1, digest'e sabit imajlarla. `ci.yml`'de `workflow-lint` job'u; şablonun workflow'u `template-smoke` job'unda, üretilen projelerde lint'leniyor.
  - Önce: zizmor 7 `unpinned-uses` (high), 2 `artipacked`, 1 `use-trusted-publishing` buldu. Sonra: repo ve üretilen iki proje temiz (online denetimler dahil). Tek istisna push adımındaki satır içi `use-trusted-publishing` ignore'u (GitHub Packages'ta trusted publishing yok).
  - Sürprizler: zizmor `.` girdisinde şablonun ham workflow'unu da toplayıp ayrıştıramıyor (girdi `.github` oldu). actionlint `.github/workflows`'u yalnızca git repo'sunda buluyor (dosyalar açıkça veriliyor). zizmor-action SARIF modunda bulguda başarısız olmuyor (kullanılmadı).
  - Kod değişmedi; `bash eng/template-smoke.sh` yeşil.
- [x] F6a.3 (f6ac014): Lock dosyaları ve SDK sabitleme (R-094, D-048 (2)).
  - `Directory.Build.targets`: `IsPackable != true` olan projelerde `RestorePackagesWithLockFile`. Altı lock dosyası: Api, Catalog, Catalog.Contracts, Payments, Payments.Contracts, test projesi. Packable paketlerde ve şablon projesinde yok.
  - `global.json` `rollForward: disable` (SDK 10.0.300). Şablonun `global.json`'ı `latestFeature` kaldı.
  - CI ve release restore'u `--locked-mode`.
  - Deneme (commit'lenmedi): CPM'de `Riok.Mapperly` 4.3.1 → 4.3.0 yapılınca `restore --locked-mode` üç projede NU1004 ile kırıldı; geri alındı.
  - Build 0 uyarı, 277/277 yeşil.
- [x] F6a.4 (ebded6e): Lisans kapısı (D-052, R-096, Q-39).
  - `dotnet-tools.json`'a `nuget-license` 4.0.18 (Apache-2.0); yeni `eng/check-licenses.sh`; CI'da restore'dan sonra "Package licenses" adımı.
  - İlk koşu: tek ihlal Polly.Core/Extensions/RateLimiting 8.4.2 (BSD-3-Clause), zincir NovaCore.AspNetCore → Microsoft.Extensions.Http.Resilience → Microsoft.Extensions.Resilience → Polly. Kullanıcı adlı istisnayı seçti (Q-39).
  - Microsoft muafiyeti olmadan iki Microsoft paketi de düşüyordu (lisansı dosya olan `Microsoft.Data.SqlClient.SNI.runtime`, `Microsoft.Identity.Client.NativeInterop`); Y-06 gereği önekle geçiyorlar.
  - Deneme (commit'lenmedi): izin listesi yalnızca `MIT` olunca kapı 39 paketle (OpenIddict, OpenTelemetry, Serilog, Mapperly, SQLitePCLRaw, xunit) exit 39 verdi. Temizken exit 0.
  - Exit kodu belgede yoktu; ölçüldü: ihlalde sıfırdan farklı (ihlal sayısı), temizken 0.
- [x] F6a.5 (17a1370): Secret taraması (D-051 (4), R-097).
  - `ci.yml`'de `secret-scan` job'u: `fetch-depth: 0`, Betterleaks 1.9.0 linux arşivi SHA-256 ile doğrulanıyor, `betterleaks git . --redact`. Opt-in `.githooks/pre-commit` (`--pre-commit --staged`).
  - Tam geçmiş taraması 31 bulgu verdi; hepsi `tests/NovaCore.Api.Tests` altında, 14 farklı test değeri (`"a long passphrase for tests"`, `token=bm90LWEtdG9rZW4` = "not-a-token" ...), tek tek incelendi.
  - **Plandan sapma:** Parmak izi listesi (`.betterleaksignore`) yerine `.betterleaks.toml`'da değer listesi: değer tam eşleşmeli VE `tests/` altında olmalı. Parmak izi commit'e bağlı; testlere her yeni değer, commit'ten sonra ikinci bir commit isterdi. SARIF de yüklenmiyor (D-051 (2) ile aynı gerekçe).
  - Denemeler: liste ile geçmiş temiz. Ayrı bir klasörde aynı değer `src/` altında yakalandı, `tests/` altındaki yeni bir değer de yakalandı. Hook, staged sahte bir `ghp_` token'ıyla commit'i durdurdu (exit 1, commit oluşmadı); temiz bir staged değişiklik geçti.
- [x] F6a.6 (92fac01): CodeQL ve Scorecard (D-051 (5)).
  - `codeql.yml` (csharp, manual build, locked restore) ve `scorecard.yml` yazıldı, lint temiz.
  - CodeQL faz branch'indeki ilk koşuda yeşil (CodeQL 2.27.2, 52 kural). 4 uyarı buldu, hepsi kimlik eklentisinde; `src/` 6a'nın kapsamı dışı, aşağıda "Sonraya bırakılanlar"a (6b girdisi) yazıldı.
  - Scorecard yalnızca main'de çalışıyor; merge sonrası doğrulanacak.
- [x] F6a.7 (26c7d73): Release tedarik zinciri (D-051 (6), R-098).
  - `release.yml` iki job oldu. `build` (yalnızca `contents: read`): sürüm kontrolü, locked restore, build, test, pack, kütüphane başına CycloneDX SBOM. `publish` (`environment: release`, `packages`/`id-token`/`attestations: write`): yalnızca `global.json`'ı checkout eder, artifact'i indirir, `actions/attest` ile paket + SBOM provenance'ı imzalar, push eder.
  - `dotnet-tools.json`'a CycloneDX 6.3.1 (Apache-2.0).
  - Ölçüm: SBOM döngüsü yerelde yedi paketin hepsinde çalıştı (1–36 bileşen). `--include-project-references` sahte bileşen ürettiği için kullanılmadı.
  - Yan etki: yayın ayrı job'a taşınınca zizmor'un `use-trusted-publishing` bulgusu kalktı, F6a.2'deki satır içi ignore silindi; workflow'larda zizmor muafiyeti kalmadı.
  - Gerçek koşu bir sonraki `v*` tag'iyle olacak (environment F6a.11'de kurulur).
- [x] F6a.8 (3e22a09): docker-compose. Bütün portlar `127.0.0.1`'e bağlandı (SQL Server 1433, Seq 5341/8081, Jaeger 16686/4317/4318; Keycloak zaten öyleydi). İmajlar `tag@sha256:<digest>` ile sabitlendi: digest'ler `docker buildx imagetools inspect` ile okundu (Seq, Jaeger, Keycloak çok mimarili index; SQL Server tek manifest). `docker compose config` iki dosyayla geçerli.
- [x] F6a.9 (5c9a684): Dependabot (D-051 (7)).
  - `.github/dependabot.yml`: `nuget` (şablon içeriği hariç, gruplar), `github-actions`, `dotnet-sdk`, `docker-compose`; haftalık, 7 gün cooldown. zizmor dosyayı denetliyor; cooldown 2 güne inince 4 `dependabot-cooldown` bulgusu verdi (deneme).
  - Şablona `.github/dependabot.yml` (`github-actions`); smoke testi ve üretilen projelerin lint'i yeşil.
  - Yeni `WorkflowPinTests` (Architecture): her action bütün workflow'larda, şablonun ham `ci.yml`'i dahil, tek ve tam bir SHA'ya sabit. Önce kırmızı görüldü (şablonda `checkout@v7`, commit'lenmedi), sonra yeşil.
  - Build 0 uyarı. İlk tam koşuda 1 test kırmızı oldu (277/278; testin adı yakalanamadı); iki tekrar koşu 278/278 yeşil. Aralıklı; R-055'teki bilinen başlangıç yarışıyla aynı sınıfta olabilir.
  - Merge sonrası doğrulanacak: Dependabot'un `exclude-paths`'i NuGet'te uygulayıp uygulamadığı, lock dosyalarını güncelleyip güncellemediği, `docker-compose.keycloak.yml`'yi görüp görmediği (belgede yazmıyor).
- [x] F6a.10 (e0ced40): Claude hook genişletmesi (D-051 (8), D-048 (3)).
  - `package-guard.js`: SHA'ya sabitlenmemiş workflow `uses:`'u ve sır gibi görünen değerler için `ask`.
  - `verify-on-stop.js`: `Directory.Build.targets` ve `*.txt` izleniyor.
  - Taslak scratchpad'de yazılıp 16 senaryoyla denendi, sonra korumalı dosyalara kullanıcı onayıyla uygulandı; uygulanan dosyalar taslakla aynı, senaryolar yeniden yeşil. Senaryolar: tag'li/SHA'lı/yerel/digest'li/tag'li Docker `uses:`, şablon workflow'u, workflow olmayan YAML, GitHub token, özel anahtar, AWS anahtarı, bağlantı dizesi parolası ve yer tutucusu, C# test parolası, sıradan kod, korunan yol, yasaklı paket.
  - İzlenen bütün repo dosyaları sır desenlerine karşı tarandı: eşleşme yok, yani mevcut dosyaları düzenlemek yanlış alarm vermiyor.
- F6a.5 düzeltmesi: CI'daki `secret-scan` `5c9a684`'te kırmızıydı (1 bulgu). Bulgu yukarıdaki F6a.5 notundaki `token=` alıntısıydı (`docs/plan/PROGRESS.md`, commit `17a1370`), liste ise yalnızca `tests/` altını kapsıyordu. Geçmiş değiştirilemeyeceği için `.betterleaks.toml`'a aynı değer için yalnızca bu dosyayla sınırlı ikinci bir liste girdi. Yerel tam tarama temiz. Ders: plan notlarına test değerleri `anahtar=değer` biçiminde yazılmaz.
- [x] F6a.11: GitHub ayarları (Q-36, Q-37; her biri kullanıcı onayıyla, `gh api`, 2026-10-21). Hepsi API'den geri okundu:
  - `security_and_analysis`: `secret_scanning` ve `secret_scanning_push_protection` açık, `dependabot_security_updates` açık; Dependabot uyarıları açık (`vulnerability-alerts` 204).
  - Ruleset `main` (id 25565183): `~DEFAULT_BRANCH`, `deletion` + `non_fast_forward`, bypass yok. Plandaki "admin bypass"tan sapma, kullanıcı onayıyla: koruma kazalara karşı, gerekirse ruleset geçici kapatılır.
  - Ruleset `release tags` (id 25565185): `refs/tags/v*`, `creation` + `update` + `deletion`, bypass yalnızca repository admin (`always`).
  - Environment `release`: required reviewer `alimuhammedak`, `prevent_self_review: false`, `can_admins_bypass: false`; deployment policy yalnızca `v*` tag'leri.
  - Kullanıcıda kalanlar: 8 paketin görünürlüğü (D-050; paket sayfası → Package settings → Change visibility, geri alınamaz) ve nuget.org'da `BMD.*` önek rezervasyonu (D-048 (1)).
- [x] F6a.12 (5327a4a): Dokümanlar.
  - README: tüketici kurulumunda token notu (D-050), şablonun `dependabot.yml`'i, tam SDK sürümü, yeni "CI, tedarik zinciri ve güvenlik taramaları" bölümü, `WorkflowPinTests`, yayın job'ları ve `gh attestation verify`, lisans tablosunda Polly ve araçlar.
  - Kök CLAUDE.md: komutlar (lock, lisans, lint, pre-commit), hook notu, "action'lar SHA'ya sabit" ve lock dosyası standartları. tests/CLAUDE.md ve src/Templates/CLAUDE.md da güncellendi.
  - CHANGELOG `[Unreleased]`: Security ve Changed. Memory `novacore-stack`.
- Faz sonu code review (high), 7 bulgu (`133c604`'te düzeltildi, biri bırakıldı):
  - CI concurrency'si, kuyruktaki push koşusunu daha yenisi gelince iptal ediyordu (`ebded6e`'de görüldü). Push'larda grup artık commit SHA'sı, yalnızca PR'lar birbirini iptal ediyor (ci.yml, codeql.yml, şablon).
  - `Polly.*` muafiyeti doğrudan bir Polly referansını da geçiriyordu. Lisans kapısı artık önce doğrudan referansları muafiyetsiz denetliyor. Deneme (commit'lenmedi): Catalog'a doğrudan Polly.Core eklenince kapı exit 1 verdi.
  - `WorkflowPinTests` `.yaml`'ı, tırnaklı `uses:`'u ve `docker://…@sha256`'yı ele almıyordu; düzeltildi.
  - `lint-workflows.sh` workflow'u olmayan klasörde yanıltıcı bir actionlint hatası veriyordu; artık açık bir hata veriyor.
  - Betterleaks `curl` satırı tek satıra düşmüştü; düzeltildi. `check-licenses.sh` tool restore çıktısını gizliyordu; artık gösteriyor.
  - Bırakılan: CodeQL repo içi PR branch'lerinde iki kez koşar (push + PR). Bu repo PR akışı kullanmıyor; faz branch'lerinin taranması için push tetikleyicisi gerekli.
- Faz doğrulaması (2026-10-21, `133c604` üzerinde): `restore --force` uyarısız, lock dosyaları değişmedi; `restore --locked-mode` geçti; Release build 0 uyarı 0 hata; test 278/278; açık paket 0; lisans kapısı geçti; Betterleaks tam geçmiş temiz; actionlint + zizmor temiz; `docker compose config` geçerli; şablon smoke testi ve üretilen projelerin lint'i yeşil. GitHub: CI (build-and-test, template-smoke, secret-scan, workflow-lint) ve CodeQL yeşil.

### Faz 6b
Plan onayı (2026-10-21). Kullanıcı kararları: Q-40 (izlenmeyen `.agents/`, `.codex/`, `AGENTS.md`'ye dokunulmaz), Q-41 (User rolü: `catalog:products:read`, `payments:read`, `payments:create`), Q-42 (blocklist kaynağı SecLists xato 1M), Q-43 (faz branch'i faz boyunca push edilir). Model kararları D-053, araştırmalar R-099…R-104. Uygulama sırası F6b.1a → 1b → 1c → F6b.2 → F6b.1d → F6b.3 → F6b.4 → F6b.5 → F6b.6 → F6b.7 (D-053 (1)). Alt adımlar: F6b.1a TOTP tek kullanım, 1b kurtarma kodu hash'i, 1c yeniden kimlik doğrulama, 1d Administrator 2FA; F6b.3a açık rol izinleri, 3b ödeme sahibi; F6b.4a `at+jwt`, 4b CORS, 4c `http` redirect, 4d client secret, 4e mutlak refresh ömrü; F6b.6a zamanlama, 6b cookie bayrakları, 6c revoke rate limit, 6d CodeQL uyarıları.

- [x] F6b.1 MFA (alt adımlar 1a–1d)
  - 2026-10-22, yeni oturum: önceki oturumdan kalan commit'lenmemiş F6b.1a testleri kullanıcının isteğiyle atıldı ("At, baştan yaz"), testler yeniden yazıldı.
  - [x] F6b.1a (cf96566): TOTP tek kullanım (D-053 (2), R-099). `SingleUseAuthenticatorTokenProvider` Identity'nin `Authenticator` sağlayıcısının yerine geçer: Identity'nin ±2 adımlık kontrolünden sonra son beş adımda kabul edilmiş kodları reddeder, yeni kodu `[NovaCore]/UsedAuthenticatorCodes` kullanıcı token'ına yazar; kayıt `ConcurrencyStamp`'la yapıldığı için yarışan iki istekten biri girer. Kodlar sayı olarak karşılaştırılır (Identity `int.TryParse` ile karşılaştırıyor; `0` önekli kod aynı koddur, testi var). Etkilenen iki mevcut test (2FA'yı açan kodla giriş, aynı kodla kapatma) bir sonraki adımın koduna geçti. 4 yeni test; 282/282 yeşil.
  - [x] F6b.1b (3591766): Kurtarma kodu hash'i (D-053 (3), R-099). `HashedRecoveryCodesUserStore` (Identity'nin EF user store'u, `AddEntityFrameworkStores`'tan önce kaydedilir; o yalnızca `TryAdd` yapıyor) kodları `PasswordHasher` varsayılanıyla (V3, PBKDF2-HMAC-SHA512, 100.000) `[NovaCore]/RecoveryCodeHashes` token'ında tutar; `Replace`/`Redeem`/`Count` ezilir. Identity'nin düz metin yuvası (`[AspNetUserStore]/RecoveryCodes`) okunmaz: eski kodlar çalışmaz, CHANGELOG silme komutunu veriyor. Eski kodlar için uyumluluk kodu yazılmadı (Y-45; 0.x önizleme). README'nin 2FA maddesi 1a ve 1b için güncellendi. 2 yeni test; 284/284 yeşil.
  - [x] F6b.1c (7a0c78f): Yeniden kimlik doğrulama (D-053 (4), ASVS 7.5.1). `/2fa/setup` `{ password }`, `/2fa/enable` ve `/2fa/disable` `{ password, code }` alır (kırıcı değişiklik, CHANGELOG). Kilitli hesap reddedilir; yanlış parola ya da kod lockout'a sayılır, sayaç yalnızca tam başarıda sıfırlanır (giriş sayfasıyla aynı kural; önceden API'deki yanlış kod sayılmıyordu). `/2fa/enable` 2FA açıkken yeni kurtarma kodları verir (`recovery_codes_renewed` audit olayı, EventId 28). 2FA'yı açmak ya da kapamak kullanıcının refresh token'larını ve authorization'larını iptal eder. `ValidationTests`'e `/2fa/setup` eklendi, enable/disable anahtarlarına `Password` girdi. 7 yeni test; 291/291 yeşil.
  - [x] F6b.1d: Administrator için 2FA (D-053 (5); F6b.2'den sonra, D-053 (1)). `IdentityPermissionResolver` 2FA'sı kapalı hesapta `Administrator` rolünü hiç saymaz (ne "bütün izinler" ne rolün claim'leri); diğer roller ve doğrudan izinler kalır. `/2fa/enable` (açarken) ve `/2fa/disable` kullanıcının izin önbelleğini F6b.2'nin yoluyla siler, yani değişiklik aynı access token'a hemen yansır. Seed admin parolayla girer, 2FA'yı API'den açar (README, Api ve şablon `appsettings.json` yorumu, CHANGELOG "Upgrade"). Testler: seed admin'le çalışan testler `AdminTests.Fixture.AdminAccessTokenAsync()`'i paylaşır (parolayla giriş, sonra store'da 2FA açılır ve önbellek düşürülür: 2FA'lı ikinci giriş kod isterdi). `AdministratorRemovalTests` aynı sırayla kurulur. 2 yeni test; 300/300 yeşil. Şablonun `appsettings.json` yorumu değişti; smoke testi faz sonunda.
- [x] F6b.2 (cb4bf1e): Yetki yükseltme (D-053 (6), R-100).
  - Çekirdek: izin önbelleği girdileri `novacore:permissions:{sub}` tag'iyle yazılır; yeni public `HybridCache.RemovePermissionsAsync(subject)` (`PermissionCacheExtensions`) `RemoveByTagAsync` çağırır. Anahtar security stamp'i de içerdiği için silen taraf anahtarı bilemez; eklentinin token'larında stamp yok, yani anahtar fiilen `sub` başına.
  - Eklenti: admin API'sinde `Administrator` rolünü vermek ya da almak ayrıca `identity:roles:manage` ister (403); rol adı normalize edilerek karşılaştırılır (`administrator` aynı rol, testi var). Son üye rolü kaybedemez (409). Her rol değişikliği kullanıcının önbelleğini siler ve refresh token'larını + authorization'larını iptal eder.
  - Testler: `IdentityServerRoleChangeTests` admin API yerine store'da rol değiştirir (API artık önbelleği hemen sildiği için "önbellek süresi içinde" yolunu yalnız store değişikliği gösteriyor). 7 yeni test; 298/298 yeşil.
  - Bilinen sınır: iki admin'in eşzamanlı kaldırılması son admin'i bırakmayabilir → Sonraya bırakılanlar.
- [x] F6b.3 Seeding ve Payments (alt adımlar 3a, 3b)
  - [x] F6b.3a (e4ebd92): Açık rol izinleri (D-047, D-053 (7), Q-41). Seeder'ın "`:read` ile biten her izin `User`'a" kuralı kalktı; `User` rolü `Identity:Seed:UserPermissions` listesini alır (eksikleri ekler, hiçbirini silmez). Açılışta iki kontrol: listedeki her izin bir modülce tanımlı olmalı (`Validate<IEnumerable<IPermissionDefinitionProvider>>`), `identity:*` izni olamaz (her kayıtlı hesap admin API'sine erişirdi). Api `appsettings.json`: `catalog:products:read`, `payments:read`, `payments:create` (Q-41); şablon: `todos:items:read` (bugünkü davranış). Test altyapısı kararı: `IdentityServerTestHost` artık JSON yapılandırma kaynaklarını düşürüyor; test çıktısındaki Api `appsettings` dosyalarını yanlışlıkla okuyordu (Api'nin listesi bu host'ta tanımsız izinler içerdiği için açılmıyordu; Development'taki `scalar` client'ının `DisplayName`'i de sızıyordu). Seçenekler varsayılanlarla aynı, davranış değişmedi. 3 yeni test; 302/302 yeşil.
  - [x] F6b.3b: Ödeme sahibi (D-047, D-053 (7)). `Payment.OwnerId` (zorunlu, 256) çağıranın `sub`'ıdır (kullanıcı ya da servis client'ı); `CreatePayment` `ICurrentUser.UserId`'yi verir. `GetPayments.VisibleTo` çağıranın ödemelerine filtreler, yeni `payments:read-all` hepsini gösterir; `(OwnerId, CreatedAtUtc)` index'i listeyi taşır. Migration `PaymentOwner`; `has-pending-model-changes` temiz. Eski ödemeler `OwnerId = ""` alır (yalnız `read-all` görür; `CreatedBy` kullanıcı adıdır, `sub` değil, modül sınırı yüzünden geri doldurulamaz). Test notu: liste `DateTimeOffset`'e göre sıralandığı için SQLite'ta uçtan uca koşamıyor (R-068); filtre sorgu düzeyinde, modülün SQLite veritabanında test edildi. Uçtan uca test 6c'ye (Sonraya bırakılanlar). 7 yeni test; 308/308 yeşil.
- [x] F6b.4 Token/protokol (alt adımlar 4a–4e)
  - [x] F6b.4a (5e8a437): `at+jwt` (D-053 (8), R-101). Eklenti JwtBearer'ın `ValidTypes`'ını `["at+jwt"]` yapar (`PostConfigure`: çekirdeğin `Configure`'u parametre nesnesini kayıt sırasından bağımsız olarak yeniden yazıyor). Çekirdek değişmedi (Entra `typ: JWT`). Test: sağlayıcının kendi anahtarıyla imzalı, yalnız `typ`'ı farklı iki token (`at+jwt` 200, `JWT` 401). Sürpriz: Api host'u eklentiyi çalıştırdığı için factory'nin bellek içi issuer'ının `typ: JWT` token'ları 31 testi kırdı; repo'nun ve şablonun `TestIdentityProvider`'ı artık `at+jwt` üretir (RFC 9068 sağlayıcısının yerine geçiyorlar). 2 yeni test; 310/310 yeşil.
  - Ara düzeltme (1f018a0, F6b.1c'ye ait): push'tan sonra CI'ın `secret-scan` job'u 3 bulguyla kırmızıydı: 2FA testlerindeki `password = "not the password"` değeri generic-password kuralına takılıyor. Değer `.betterleaks.toml`'daki incelenmiş test değerleri listesine eklendi (D-051 (4)). Betterleaks yerelde kurulu değil; doğrulama CI'da. Şablon smoke testi (`bash eng/template-smoke.sh`) bu noktada geçti: BuiltIn 8/8, External 7/7 (şablonda F6b.1d yorumu, F6b.3a listesi, F6b.4a `at+jwt`).
  - [x] F6b.4b (eb8ecb3): CORS (R-102, RFC 9700 §2.6). `/connect/authorize`'a ve `/account` grubuna `DisableCorsAttribute` meta verisi; preflight 204 alır ama izin başlığı almaz. `/connect/token` ve API CORS'ta kalır (testte kontrol olarak). Test host'u yapılandırılmış bir origin'le (çekirdeğin policy'si orada `AllowCredentials`). 5 yeni test (teori); 315/315 yeşil.
  - [x] F6b.4c (82cb4ff): `http` redirect (D-053 (9), RFC 8252 §7.3/§8.3). Development dışında redirect ve post-logout URI'leri https olmalı; düz `http` yalnız loopback IP'de (`127.0.0.1`, `[::1]`) kabul edilir, `localhost` reddedilir (başka bir arayüze çözülebilir). Kural `IdentityServerOptions` doğrulamasıdır (`Validate<IHostEnvironment>`, `ValidateOnStart`); göreli URI'yi mutlak-URI kuralı raporlar. 6 yeni test; 321/321 yeşil.
  - [x] F6b.4d (87128f2): Client secret gücü (D-053 (9), RFC 6749 §10.10). Confidential client secret'ı en az 32 karakter (`IdentityClientOptions.MinimumSecretLength`), yoksa açılış durur; servis client'ı da kullanıcı adına giriş yapan confidential client da. Testlerdeki iki servis client secret'ı 32 karakterin üstüne çıktı ve `.betterleaks.toml` listesine eklendi (CI taramasında bulgu olmaması için önden). 2 yeni test; 323/323 yeşil.
  - [x] F6b.4e (439d139): Mutlak refresh ömrü (R-103). Ölçüm (`FakeTimeProvider`, `RefreshTokenAbsoluteLifetimeTests`): girişten 6 gün sonra yenilenen token 7 gün + 1 sn'de hâlâ 200 aldı, yani OpenIddict'in varsayılanı (sliding) her yenilemede yeni 7 gün veriyordu: açık. `DisableSlidingRefreshTokenExpiration()` ile yeni refresh token ilk token'ın bitişini alır; aynı test 400 `invalid_grant`. Rolling refresh token'lar ve 30 sn leeway değişmedi. 1 yeni test; 324/324 yeşil.
- [x] F6b.5 Şifre politikası
  - 2026-10-22: Blocklist dosyasını indirme onayı soruldu; kullanıcı soru aracını reddetti ve "continue" dedi. İndirme sohbette açık onay istediği için F6b.5 F6b.6 ve F6b.7'den sonraya alındı (adımlar birbirine bağlı değil, ID'ler sabit). Onay sohbette geldi ("Evet, xato-net-10-million-passwords-1000000.txt dosyasını scratchpad'e indir").
  - İndirme: SecLists commit `c205c36a445bff37f8e58a9ec829105cd4975c58`, `Passwords/Common-Credentials/xato-net-10-million-passwords-1000000.txt`, 8.557.632 bayt, 1.000.000 satır, SHA-256 `424a3e03a17df0a2bc2b3ca749d81b04e79d59cb7aeec8876a5a3f308d0caf51`; LICENSE MIT (Copyright (c) 2018 Daniel Miessler). Scratchpad'deki ayrı bir klasöre indirildi, `python -I` ile veri olarak okundu.
  - Uygulama (NIST SP 800-63B-4): `RequiredLength = 15`, kayıt ve parola değiştirme isteklerinde `Length(15, 128)`. `CommonPasswordValidator` (singleton `IPasswordValidator`, `TryAddEnumerable`; Identity'nin `AddPasswordValidator`'ı scoped kaydedip listeyi her istekte okurdu) gömülü `CommonPasswords.txt`'e büyük/küçük harf duyarsız bakar, hata kodu `CommonPassword`. Dosyanın başlığında kaynak, commit, SHA-256 ve SecLists'in MIT bildirimi var (paketle dağıtıldığı için). Kayıt, parola değiştirme ve sıfırlama Identity'nin doğrulayıcılarından geçtiği için üçü de kapsanıyor. Test değerleri `.betterleaks.toml` listesine eklendi. `PasswordResetTests`'in "en az 12 karakter" beklentisi 15'e çekildi (davranış değişikliği). 6 yeni test; 354/354 yeşil.
  - Süzme (Q-42): 15–128 karakterlik 10.908 satırın ilki 3000'i, büyük/küçük harf duyarsız tekrarsız, hepsi ASCII, kontrol karakteri yok; kaynakta 790.431. satıra kadar uzanıyor. `#` ile başlayan parola yok (başlık satırları `#` ile atlanıyor).
- [x] F6b.6 Doğrulanacaklar (alt adımlar 6a–6d)
  - [x] F6b.6a (10559ca): Zamanlama (R-105, D-054 (13)). Geçici bir testle (commit'lenmedi), her yol için 12 örneğin medyanı, `IdentityServerTestHost`: anlık gönderici → register var olan 428 / yeni 418 ms, forgot-password bilinmeyen 1 / onaylı 2 ms, resend bilinmeyen 1 / onaysız 1 ms (fark yok). 200 ms'lik gönderici (SMTP gecikmesi) → register 388 / 602 ms, forgot-password 1 / 209 ms, resend 1 / 204 ms. Yani yanıt e-posta gönderimini bekliyor ve gerçek bir göndericiyle hesabın varlığı süreden okunuyor: **açık**. Araştırma (R-105): Microsoft'un kendi Identity kodu ve Keycloak aynı deseni kullanıyor (kullanıcı yoksa hemen dön, varsa e-postayı bekle); Microsoft'un e-posta zamanlaması için önerisi yok. Kapatma gerekçesi OWASP Forgot Password ("asynchronous calls" ya da "same logic") ve ASVS 6.3.8 (L3); K5 kararı açık çıkarsa kapatmayı istiyor. Kullanıcı seçenekleri gördükten sonra asenkron gönderimi seçti ve Y-18'in kapsamına girmediğine karar verdi (SMTP süreç dışı). Uygulama: `EmailQueue` (sınırlı `Channel`, 1000, `Wait`, tek okuyucu `BackgroundService`, e-posta başına DI scope'u, hata loglanır, adres ve gövde loglanmaz); kayıt, sıfırlama formu ve yeniden gönderme kuyruğa koyup hemen döner. Testler: yanıt bekletilen göndericiyle zaman aşımına uğramadan döner (üç uç), hata veren gönderici isteği bozmaz ve sonraki e-posta gider (önce dördü de kırmızıydı). E-postayı istekten hemen sonra okuyan `RegistrationTests` ve `PasswordResetTests` `CapturingEmailSender.WaitForAsync` ile teslimatı bekler; "gitmedi" iddiaları sonraki bir e-postayı bekleyerek kuyruk sırasına dayanır. Kullanıcının soru aracını bir kez reddettiği sırada taslak test çalışma ağacında kırmızı build bırakmıştı; Stop hook'u durdurdu, taslak scratchpad'e alınıp onaydan sonra geri kondu. 4 yeni test; 358/358 yeşil.
  - [x] F6b.6b (9f79ef4): Cookie bayrakları (R-104). Ölçüm: 2FA cookie'leri `SameAsRequest`, antiforgery `None` (https'te bile Secure değil). Düzeltme: `TwoFactorUserId` ve `TwoFactorRememberMe` cookie'leri `Always` (uygulama cookie'si gibi); antiforgery Development dışında `Always` (MS önerisi), Development'ta `SameAsRequest`: `Always` düz http sayfasında 500 veriyor (antiforgery Secure cookie'yi http'de kuramayınca hata atıyor; `SecureDefaultsTests` http üzerinden yakaladı). 2 yeni test; 326/326 yeşil.
  - [x] F6b.6c (d17c761): `/connect/revoke` hız sınırı (R-103). Ölçüm: revocation'ı OpenIddict authentication handler'ında yanıtlıyor, endpoint'i yok; bu yüzden `strict` politikası takılamıyor, ama limiter authentication'dan önce koştuğu için IP başına global pencere uygulanıyor (`PermitLimit=3` ile 4. istek 429). **Açık değil:** RFC 7009 §2.2 gereği bilinmeyen token'a da 200 dönülüyor (token oracle'ı yok); confidential client secret tahmini global pencereyle sınırlı ve secret'lar artık en az 32 karakter (F6b.4d). `strict`'i takmak için hiç çalışmayan sahte bir endpoint gerekirdi; eklenmedi. Global limit bir testle sabitlendi. 1 yeni test; 327/327 yeşil.
  - [x] F6b.6d (c1f93b7): CodeQL'in 4 uyarısı (D-044 triage kuralı). (1) `cs/exposure-of-sensitive-information` ×2 (`SecurityAuditLogger.cs:14`, `:25`): **gerçek**, Y-35 ile de çelişiyor (maskeli e-posta da kişisel veri). Kırmızı testle düzeltildi: `user_registered` ve `login_failed` artık yalnız hesap id'si taşır, bilinmeyen adres için hiçbir şey; maskeleme kodu ve CA1873 guard'ları kalktı. (2) `cs/user-controlled-bypass` ×2 (`LoginPage.cs:53`, `:125`): **false positive**. Kullanıcının kontrol ettiği `HasFormContentType` yalnız reddetmeye yarıyor; başlıkla gelen geçerli bir antiforgery token'ıyla JSON gönderen istek de 400 alıyor (yeni test, iki sayfa için; koşul olmasa `ReadFormAsync` 500 verirdi). Uyarılar kullanıcı onayıyla (`gh api`, gerekçe ve test adıyla) GitHub'da "false positive" olarak kapatıldı ve API'den geri okundu (#3, #4 `dismissed`). 4 yeni test; 330/330 yeşil.
- [x] F6b.7 (368d677): Eksik negatif protokol testleri. Yeni `NegativeProtocolTests` (ikinci bir public client'lı fixture): kayıtlı olmayan redirect URI'nin yedi varyantı (sondaki `/`, ek sorgu, büyük/küçük harf, port, `http`, başka host, başka client'ın URI'si) 400 ve yönlendirme yok; kodun ikinci kullanımı `invalid_grant` ve ilk kullanımın refresh token'ını da iptal ediyor; başka client'la kod `invalid_grant`; verifier'sız ya da yanlış verifier'lı kod reddediliyor; `response_type=token` 400, `grant_type=password` `unsupported_grant_type`; yetkilendirme yanıtında RFC 9207 `iss` parametresi var ve keşif belgesi bunu ilan ediyor; JWKS'te özel anahtar parçası (`d`, `p`, `q`, `dp`, `dq`, `qi`, `k`) yok; `/account/*` sayfaları `X-Frame-Options: DENY` ve `frame-ancestors 'none'` taşıyor. Mevcut kapsamdakiler tekrar yazılmadı: S256'sız authorize (`AuthorizationCodeFlowTests`), `/connect/revoke` ile iptal edilen refresh token. 18 test, ilk koşuda yeşil: açık bulunmadı. 348/348 yeşil.
- Faz doğrulaması (2026-10-22, `10559ca`): `restore --force` ve `--locked-mode` exit 0; `build -c Release` 0 uyarı / 0 hata; `dotnet test` 358/358; açık paket yok (`list package --vulnerable --include-transitive`); lisans kapısı exit 0; `has-pending-model-changes` Catalog, Payments, IdentityServerDbContext temiz; branch'te CI (build-and-test, template-smoke, secret-scan, workflow-lint) ve CodeQL yeşil, branch'te açık CodeQL uyarısı yok.
- Code review (`code-review` skill'i, branch diff'i, 12 bulgu):
  - Düzeltilen (`15520c1`, kırmızı testle, D-055): #1 dolu e-posta kuyruğu yazanı bekletip zamanlama farkını geri getiriyordu → `TryWrite`, sığmayan düşer ve loglanır; #3 eski düz metin kurtarma kodları hiç silinmiyordu → kod yenilenince Identity'nin yuvası silinir; #4 son admin koruması 2FA şartını saymıyordu → 2FA'lı başka üye kalmadıkça 409; #7 paylaşılan statik `IdentityError` (Y-24) → her hata için yeni örnek; #9 testte `DateTime.UtcNow` → `TimeProvider.System`. `SendConfirmationAsync`, `Register` ve `ResendConfirmationEmail`'deki artık kullanılmayan `CancellationToken` parametreleri kalktı.
  - Ertelenen (Sonraya bırakılanlar): #2 ilk TOTP kaydında INSERT yarışı, #5 eşzamanlı son-admin kaldırma (zaten kayıtlı), #6 `/connect/endsession`'ın CORS'u, #8 yarışı zorlamayan test.
  - Değişiklik yapılmayan: #10 `ChangePassword`'ün sayma kodu yeni yardımcının kopyası (önceden var olan kod, Y-09), #11 her istekte tag dizisi ayrılması (ihmal edilebilir; HybridCache tag'leri çağrıda istiyor), #12 iki sınıfta aynı `[NovaCore]` sabiti (önemsiz).

### Faz 6c
Plan onayı (2026-10-22). Başlangıç: `main` @ `37c8086`, 361 test. Kullanıcı kararları: Q-44 (faz branch'i her adımdan sonra push edilir), Q-45 (şablon da MTP + xUnit 4'e geçer, F6c.1b), Q-46 (önceki fazlardan devredilen üç madde bu fazda: R-055 → F6c.1c, ilk TOTP kaydındaki INSERT yarışı → F6c.2c, yarışı zorlamayan 2FA testi → F6c.2d). Q-40 sürüyor (izlenmeyen `.agents/`, `.codex/`, `AGENTS.md`'ye dokunulmaz). Sıra PLAN'daki gibi; alt adımlar: F6c.1a repo, 1b şablon, 1c R-055; F6c.2a altyapı ve gerçek migration'lar, 2b SQLite'ın gizledikleri, 2c TOTP INSERT yarışı, 2d deterministik 2FA yarış testi. Faz kararları D-056'ya, araştırmalar R-108'den başlayarak yazılır.

- [x] F6c.1a (ac227a4): MTP + xUnit 4 (R-108). `global.json` `test.runner: Microsoft.Testing.Platform`; `xunit.v3` 3.2.2 → 4.0.2 (MTP v2 2.5.1); `Microsoft.NET.Test.Sdk` ve `xunit.runner.visualstudio` kalktı. CI ve release `dotnet test --solution NovaCore.slnx`. Stop hook'u (kullanıcı onayıyla) `--solution` kullanıyor ve `-nologo`'yu bırakıyor: ölçümde eski komut exit 5 verdi, çünkü `-nologo` MTP modunda test uygulamasına geçiyor. **Plandan sapma:** PLAN'ın `dotnet test NovaCore.slnx` genel kuralı değişmedi; konumsal `.slnx` 10.0.300'de çalışıyor (exit 0). Filtre örnekleri `-- --filter-method "*.Class.Method"` / `--filter-class` oldu (CLAUDE.md, tests/CLAUDE.md, README); üçü de denendi. Kırmızı bir deneme testi exit 2 verdi (hook bunu kırmızı sayar; deneme commit'lenmedi). 361/361 yeşil (~4 dk yerelde).
- [x] F6c.1b (f32b0ec): Şablon MTP + xUnit 4 (Q-45). Şablonun `global.json`'ına `test.runner`, `xunit.v3` 4.0.2, iki VSTest paketi kalktı. Şablonun CI'ındaki `dotnet test -c Release --no-build` proje klasöründe tek çözüm olduğu için MTP modunda aynen çalışıyor (smoke testi aynı komutu koşuyor). `bash eng/template-smoke.sh` yeşil: BuiltIn 8/8, External 7/7, MTP modunda.
- [x] F6c.1c (ee56153): R-055 (Q-46, R-109). Kaynak: `DeferredHost.StartAsync` `release/10.0` ve `main`'de hâlâ yarışlı; dotnet/aspnetcore#69684 ve düzeltme PR'ı #69685 açık. Ölçüm 1 tam Release koşusunda durduruldu (isabet yok; koşu ~4,5 dk, eski sıklık ~2/45, kaynak kök nedeni zaten doğruluyor). Etkilenen tek test `ConfigurationValidationTests`'in açılış teorisiydi: 12 vaka artık `CoreWebTestHost` + `AddCorePersistence` üzerinde `StartAsync`'in kendi exception'ını bekliyor (ayarlar çekirdek paketlerin). Aynı host geçerli yapılandırmayla Development ve Production'da açıldı (deneme, commit'lenmedi). tests/CLAUDE.md: açılış hatası `factory.Server` üzerinde değil, doğrudan başlatılan host'ta beklenir. **Plandan sapma:** ölçüm 20 yerine 1 koşu; karar ölçüme değil upstream kaynağa dayandı.
- [x] F6c.2 SQL Server katmanı (alt adımlar 2a–2d)
  - [x] F6c.2a (c7fb3aa): Altyapı ve gerçek migration'lar (D-043, D-056 (3)–(5), R-110). `Testcontainers.MsSql` 4.16.0 (kullanıcı hook'ta onayladı; lisans kapısı geçti). `TestSqlServer`: koşu başına tek container, `docker-compose.yml`'deki imaj (`SqlServerImageTests` eşitliği koruyor), statik `Lazy` (xUnit fixture'ları birbirine bağımlı olamıyor), Ryuk siliyor. `ApiFactory` taban sınıfı ayrıldı (issuer, izinler, ortam, scope doğrulaması, üretim ekleri); `NovaCoreApiFactory` SQLite'ı, yeni `SqlServerApiFactory` Program'ın SQL Server kayıtlarını ve `Database:ApplyMigrations=true`'yu kullanıyor (üretimdeki `DatabaseMigrationService`). `ModuleTestData` yardımcıları taban sınıfı alıyor (şemasız ham SQL kullanan `OutboxMessagesAsync` hariç). `MigrationTests`: Api host'u açılışta üç context'in bütün migration'larını uyguluyor; `OutboxConsumersByMessage` (Catalog ve Payments) kayıtları mesaj anahtarına taşıyor, sahipsiz kaydı siliyor, Down'da event id'ye geri taşıyor, FK cascade çalışıyor (EXEC'li SQL ilk kez koştu); `PaymentOwner` eski ödemeye `""` veriyor. Docker yoksa skip; CI ve release `-- --fail-skips on`. Docker'sız yol yerelde taklit edilemedi (`DOCKER_HOST` override'ı Testcontainers'ın named pipe'a geçmesini engellemedi). **Plandan sapma:** Respawn veri sıfırlamaya ihtiyaç duyan ilk adıma (F6c.2b) kaldı. Yerel: SQL Server testleri sıcak ~40 sn; tam takım 366/366 (~5,5 dk).
  - [x] F6c.2b (a40ab1e): SQLite'ın gizledikleri (R-111, D-057, Q-47). `Respawn` 7.0.0 (kullanıcı hook'ta onayladı): `SqlServerApiFactory.ResetAsync` her testten önce `catalog` ve `payments` şemalarını boşaltıyor (migration geçmişi kalıyor). `SqlServerQueryTests`: `GET /api/v1/payments` uçtan uca (sahip filtresi, `CreatedAtUtc` azalan sıra, sayfalama; `payments:read-all` hepsini gösteriyor; 6b'den devredilen madde); üç context'in tabloları gerçek veritabanında yalnız kendi şemasında, dbo boş; ürün araması varsayılan collation'da büyük/küçük harf duyarsız, aksan duyarlı. **Bulgu:** SQL Server `decimal(18,2)` fazla ondalığı sessizce yuvarlıyordu (0.001 → 0.00, 10.005 → 10.01; API 201 dönüyordu). Kullanıcı reddetmeyi seçti (Q-47): önce `ValidationTests`'e üç kırmızı satır (0.001, 10.005, 17 tam basamak), sonra `DecimalColumnAttribute(18, 2)` `CreateProduct`/`UpdateProduct`'ın `Price`'ında; hassasiyet `ProductConfiguration` sabitlerinden (model değişmedi, `has-pending-model-changes` temiz). Sütunun sınır değerleri (0.01, 12345678.99, 9999999999999999.99) SQL Server'da birebir dönüyor. CI'daki R-055 kırmızısı (`f32b0ec`) R-109'a kanıt olarak eklendi.
  - [x] F6c.2c (5bd8dfe): İlk TOTP kaydında INSERT yarışı (Q-46, D-058; 6b code review #2). Yarış aracı `UsedCodeRace` (test interceptor'ı, `ConfigureDbContext` ile host'un store context'lerine takılı): kullanılmış kod token'ını yazan bir sonraki kayıttan hemen önce rakip isteği sonuna kadar koşturur, yani iki istek de yazmadan önce okur. Kırmızı test (`TwoFactorRaceTests.Two_requests_enabling_two_factor_with_one_code_let_one_in`): aynı kodla iki `/2fa/enable`'dan kaybeden 500 aldı. Düzeltme: `HashedRecoveryCodesUserStore.UpdateAsync` kaydedilemeyen eklenmiş token'ın anahtarı veritabanında varsa Identity'nin `ConcurrencyFailure`'ını döndürür ve o token'ı context'ten çıkarır (aynı istekteki lockout kaydı onu yeniden eklemesin). Kaybeden 400 alıyor, aynı kod tekrar 400, 2FA bir kez açık. Test SQLite'ta koşuyor: kontrol sağlayıcıdan bağımsız, iki sağlayıcı da aynı `DbUpdateException` yolundan geçiyor. Kasıtlı bozmada (tek kullanım kontrolü her zaman `true`) 3/3 kırmızı (commit'lenmedi).
  - [x] F6c.2d (9f69ca9): Deterministik 2FA yarış testi (Q-46; 6b code review #8). `Two_sign_ins_racing_with_one_code_let_one_in` `TwoFactorTests`'ten `TwoFactorRaceTests`'e taşındı ve `Task.WhenAll` yerine `UsedCodeRace`'le koşuyor: ilk tarayıcı kodu kaydetmeden hemen önce ikincisi kod adımını sonuna kadar geçirir. İkincisi yönlendirilir, ilki kullanıcının concurrency stamp'ine takılıp kod formunu yeniden alır. Kasıtlı bozmada (tek kullanım kontrolü her zaman `true`) 3/3 kırmızı (commit'lenmedi).
- [x] F6c.3 (938f258, şablon ayrı commit'te): Outbox saklama karşılaştırması (D-059, R-112). **Karar:** karşılaştırma SQL'de. `outbox_messages`'ta `(ProcessedOnUtc, Id)` index'i zaten var, yani `ProcessedOnUtc < cutoff` index üzerinde bir aralık; `Id` sıralı parçalar ve bellekte karşılaştırma yalnızca SQLite içindi. `DeleteExpiredAsync` artık 50'lik `ExecuteDelete` parçalarıyla siliyor (25 satır → 10). SQLite test bağlamları `DateTimeOffset`'i UTC tick saklıyor (`UtcTicksModelCustomizer`: `NovaCoreApiFactory`'nin EF servis sağlayıcısında ve `OutboxDispatcherTests`'in host'unda `ReplaceService` ile); ham SQL tick yazıyor. Testler: yeni `OutboxRetentionTests` (SQL Server'da Api host'unun dispatcher'ı süresi dolanı kayıtlarıyla siliyor, dolmayanı bırakıyor); mevcut SQLite saklama testleri yeşil. **Plandan sapma:** plan "`Id` sıralı parçalar kalır, yeni index gerekmesin" diyordu; index zaten olduğu için parçalar da kalktı. Model ve migration değişmedi. 378/378. Şablon: üretilen projenin testleri de SQLite'ta dispatcher'ı koşturuyor ve yeni sorgu orada çevrilemezdi (her turda hata logu); şablonun `ApiFactory`'sine aynı `UtcTicksModelCustomizer` eklendi, smoke testi yeşil (BuiltIn 8/8, External 7/7). CHANGELOG'da tüketiciye "Upgrade" notu.
- [x] F6c.4 (94806b5): Coverage ve ratchet (D-060, R-113). `coverlet.MTP` 10.1.0 (kullanıcı hook'ta onayladı), `dotnet-reportgenerator-globaltool` 5.5.11 (`dotnet-tools.json`). `eng/check-coverage.sh [Configuration] [--update]`: testleri coverage'la koşar (`--fail-skips on`, SQL Server dahil), ReportGenerator özetini `eng/coverage-baseline.txt` ile karşılaştırır (tolerans 0,3 puan), CI'da özeti job summary'ye yazar. CI: "Test with coverage" + mimari testler ayrı adımda (coverlet DLL'leri yerinde enstrümante ediyor, `PackageDependencyTests` enstrümante edilmiş `netstandard` referansını görüp kırmızı oluyordu; test gevşetilmedi). Ölçüm: Release'te iki koşu birebir aynı, satır %95,5 (2088/2186), dal %82,0 (548/668) = baseline. Kapı denemesi: baseline %99'la exit 1 (commit'lenmedi). Sürprizler: Directory.Packages.props'taki XML yorumunda `--` (XML'de yasak) bütün CPM'i bozdu (NU1015), düzeltildi; Git Bash yolu ReportGenerator'a geçmiyor, betik göreli yol kullanıyor; bu makinede `jq` yok, betik `sed` kullanıyor; `template-smoke.sh` paketleri kendi önbelleğine restore ettiği için sonrasında `dotnet test --no-build` projeyi "VSTest" sandı, `dotnet build` (restore) düzeltti.
- [x] F6c.5 (9671e1c): API sözleşmesi (D-061, R-114). **Plandan sapma:** belge build'de değil test host'unda üretiliyor. Ölçüm: `Microsoft.Extensions.ApiDescription.Server` 10.0.12 ile build GetDocument adımında düştü (Program Production'da açıldı, Data Protection deposu yok); .NET 10'da ortam seçilemiyor (`OpenApiGenerationEnvironment` .NET 11). Deneme geri alındı. `OpenApiContractTests`: `/openapi/v1.json` = commit'li `docs/openapi/v1.json` (OpenAPI 3.1.1); farklıysa dosyayı yazar ve kırmızı olur (iki koşu deterministik). `eng/check-openapi.sh`: main'den ayrılma noktasındaki belgeyle (main'de `HEAD^1`) oasdiff v1.33.0 (digest'e sabit imaj) `breaking --fail-on ERR`, kabul dosyası `eng/openapi-breaking-accepted.txt`. CI'da yeni `api-contract` job'u (`fetch-depth: 0`). 3.1 ölçümü: işlem silme, yanıttan zorunlu alan silme, yeni zorunlu istek alanı, tip değişikliği yakalandı; opsiyonel alan ve aynı belge geçti; kabul satırı çalışıyor.
- [x] F6c.6 (9c64c9f): CsCheck property testleri (D-062, R-115). `CsCheck` 4.9.1 (kullanıcı hook'ta onayladı). Yeni: `ResultPropertyTests` (başarı/başarısızlık, `ToHttpResult` her `ErrorType` için durum + `detail` + `code`), `PaginationPropertyTests` (her `int` sayfa/boyut geçerli istek ve doğru ofset; `PagedList` sayıları toplamla tutarlı), `DomainPropertyTests` (`Payment` bir kez Pending'den sonuçlanır, aynı sonuç tekrar bir şey değiştirmez, tutar = birim fiyat × adet; `Product` stoğu modelle aynı, asla eksi değil), `StockReservationPropertyTests` (tekrarlı ve karışık teslimlerde her ödeme ilk teslimiyle bir kez karara bağlanır, stok tam ayrılan miktar kadar düşer). **Bulgu:** `PaginationRequest.Skip` büyük sayfada taşıyordu (kırmızı, seed `e_6U6vth8ZA3`); `long`'da hesaplanıp `int.MaxValue`'da kesiliyor. 388/388.
- [x] F6c.7 (fa2da44): Stryker.NET haftalık (D-063, R-116). `dotnet-stryker` 5.0.0 (`dotnet-tools.json`). Ölçüm: Api test projesine karşı test projesi ve kaynak projesi kipleri "No project found" (Buildalyzer VS 18 Insiders'ın MSBuild'iyle Api'yi çözümleyemedi, Abstractions geçişli); çözüm kipi koştu ama MTP runner'ının test başına coverage yakalaması 388 testte saatler sürecekti ve host açan testleri "Dubious" yaptı. **Plandan sapma (yapısal):** saf testler (`Result`, sayfalama, `Payment`, `Product`, domain event'leri ve property testleri; 8 dosya) yeni `tests/NovaCore.Domain.Tests` projesine taşındı. `eng/mutation.sh` üç hedef (Abstractions, Catalog `Domain/`, Payments `Domain/`) ~2 dk 20 sn; `mutation.yml` haftalık + `workflow_dispatch`, rapor artifact ve job özeti. İlk rapor: %69,77 / %61,11 / %70,00. Rapor gerçek boşluklar gösterdi: `PagedList` sayfa sınırları ve `pageSize = 1` (property üreteçleri sınırlara düşmüyordu) ve `Result.Failure(Error.None)` değişmezi; üreteçlere sınırlar ve bir test eklenince Abstractions %79,07. Coverage betiği iki projeyi ölçüyor (`[*.Tests]*` hariç); `--update` ilk kez kullanıldı: baseline %95,8 / %82,9. CI'daki mimari adımı `--project` (çözüm genelindeki filtre Domain.Tests'te exit 8 verdi). Tam takım 389. **Açık:** `workflow_dispatch` ancak workflow varsayılan dalda olunca tetiklenebiliyor; branch'te koşu merge'den sonraya kaldı.
- Faz doğrulaması (2026-10-23, `fa2da44`): `restore --force` ve `--locked-mode` exit 0; `build -c Release` 0 uyarı / 0 hata; `dotnet test --solution NovaCore.slnx -c Release -- --fail-skips on` 389/389 (Docker'la, SQL Server dahil, atlanan yok); açık paket yok; lisans kapısı exit 0; `has-pending-model-changes` Catalog, Payments, IdentityServerDbContext temiz; `eng/check-coverage.sh` satır %95,8 / dal %82,9 (= baseline); `eng/check-openapi.sh` (main'de belge yok: karşılaştırma yok; `HEAD`'e karşı değişiklik yok); `eng/mutation.sh` üç rapor (%79,07 / %61,11 / %70,00); `eng/lint-workflows.sh` temiz; `bash eng/template-smoke.sh` BuiltIn 8/8, External 7/7. GitHub: CI (build-and-test, api-contract, template-smoke, secret-scan, workflow-lint) ve CodeQL yeşil, branch'te açık CodeQL uyarısı yok; CI'daki coverage yereldekiyle aynı.
- Code review (`code-review` skill'i, high, branch diff'i, 7 bulgu): düzeltilen (`85d532a`) #5 kabul edilen kırılımlar listesi hiç temizlenmiyordu → dosya başlığı satırın ne zaman silineceğini söylüyor. Ertelenen (Sonraya bırakılanlar): #1 `Payment.Amount` taşması (API kararı, kullanıcıya soruluyor), #2 SQL Server host'unda R-055 maskelemesi (upstream), #3 `TestSqlServer`'ın geçici başlatma hatasını saklaması, #4 kayıp token kontrolünün asıl hatayı gölgelemesi. Değişiklik yapılmayan: #6 sözleşme testinin dosyayı yazması (tasarım, D-061), #7 `DecimalColumnAttribute`'taki `Aggregate` (önemsiz).

### Faz 6d
Başlangıç 2026-10-23, `main` @ `1841d6f`, 389 test. Kullanıcı "6d izinsiz devam et" dedi: plan onayı beklenmeden bu satırlara yazıldı; faz branch'i önceki fazlardaki gibi (Q-38, Q-43, Q-44) her adımdan sonra push edilir, main'e dokunulmaz. Kullanıcı kararları: Q-48 (örtük `Result<T>` operatörleri kalkar), Q-49 (SonarQube Cloud ve CodeRabbit kurulumunu kullanıcı faz içinde yapar). Q-40 sürüyor (izlenmeyen `.agents/`, `.codex/`, `AGENTS.md`'ye dokunulmaz). Faz kararları D-064'e, araştırmalar R-117'den başlayarak yazılır.

**Ölçüm (adımlardan önce, `TreatWarningsAsErrors=false`):** PLAN'daki `AnalysisMode<Kategori>=All` bu repoda etkisiz: `AnalysisLevel=latest-recommended` bileşik değer olduğu için SDK kategorinin modunu bu son ekten alıyor (`Microsoft.CodeAnalysis.NetAnalyzers.targets:531`; MS: "AnalysisLevel takes precedence over AnalysisMode"). Doğru özellik `AnalysisLevel<Kategori>=latest-all`. Onunla: Security 0; Reliability 498 (CA2007 480, CA2000 17, CA2025 1); Performance 62 (CA1812 59, CA1849 3); Usage 83 (CA2234 79, CA2225 2, CA2213 2). CA2000/2025/1849/2234/2213'ün hepsi testlerde. IDE0051/52/59/60 0, IDE0005 (paketlerde) 1, CA1502/1505 0, CA1506 5. `dotnet format --verify-no-changes`: 181 (160 satır sonu, 12 BOM'lu migration, 9 using sırası); Linux CI'da LF checkout `end_of_line = crlf` yüzünden her dosyayı kırardı.

**Adımlar ve kendi kararlarım (D-016):**
- F6d.1a Security `latest-all` (bulgu yok). F6d.1b Reliability: CA2007 uygulama kodunda (Api, modüller, testler) ve yalnız ASP.NET Core'da koşan paketlerde (`NovaCore.AspNetCore`, `NovaCore.Identity.OpenIddict`) `none`: MS'in "When to suppress"ı uygulama kodu ve SynchronizationContext'siz ortam için izin veriyor; testlerde xUnit1030 `ConfigureAwait(false)`'u zaten yasaklıyor. `NovaCore.EntityFrameworkCore` ASP.NET Core'a bağlı değil, 15 `await`'i `ConfigureAwait(false)` alır. CA2000/CA2025 testlerde düzeltilir. F6d.1c Performance: CA1812 `none` (59/59 DI, options, EF yapılandırması, JSON ya da test DI'ı tarafından oluşturuluyor; MS bu durumlar için bastırmayı öneriyor; ölü tipleri F6d.3c'deki Roslynator raporu yakalar), CA1849 testlerde düzeltilir. F6d.1d Usage: CA2234 testlerde `Uri`'ye geçer (bastırma izni yok: parametreler gerçekten URI), CA2213 düzeltilir, CA2225 için operatörler kalkar (Q-48).
- F6d.2a IDE0051/52/59/60 her yerde, IDE0005 yalnız `src/BuildingBlocks/**`'te warning (IDE0005 build'de `GenerateDocumentationFile` istiyor; diğer projelerde açmak CS1591 bastırması gerektirirdi). F6d.2b CA1502/1505/1506 warning, MS'in varsayılan eşikleriyle; CA1506'nın 5 bulgusu (composition root'lar, endpoint map'leri, test host'u) MS'in "bakımı kolaysa bastırılabilir" iznine dayanarak gerekçeli `[SuppressMessage]` alır. `CodeMetricsConfig.txt` yalnız eşik değişince gerekir; eşik değişmediği için eklenmez (plandan sapma). F6d.2c `dotnet format --verify-no-changes` CI'da: `.editorconfig`'ten `end_of_line` kalkar (index zaten LF; şablonun `.editorconfig`'i de taşımıyor), `.gitattributes` `* text=auto` ile normalizasyonu makineden bağımsız yapar, migration'lar EF'in yazdığı gibi `utf-8-bom`. F6d.2d Şablon: üretilen proje repo'nun standartlarını taşır (D-040 (5)), MS'in yerleşik ayarları şablona da işlenir, smoke testiyle doğrulanır.
- F6d.3a BannedApiAnalyzers 5.6.0 (`src/` projeleri): statik saatler, `Console`, `Environment.Exit`, `BuildServiceProvider`, `new HttpClient`. D-021 istisnası yüzünden saat girdileri `NovaCore.Abstractions`'a verilmez; orayı `SourceHygieneTests`'in regex'i korumaya devam eder (Y-44: `#pragma` yerine bastırmasız kapsam). F6d.3b Meziantou.Analyzer 3.0.303 (7 günlük cooldown'u geçmiş en yeni sürüm), varsayılan önem dereceleriyle: warning olanlar düzeltilir, bilgi düzeyindekiler build'i kırmaz (kademeli). F6d.3c Roslynator CLI 1.0.1 `find-symbol --unused` haftalık rapor.
- F6d.4 SonarQube Cloud workflow'u (scanner CI'da, NuGet paketi yok), Qodana Community deneme koşusu (yerel Docker), CodeRabbit yapılandırması; hesap/uygulama kurulumları kullanıcıda (Q-49).
- F6d.5 atlanır: ilk kararlı sürüm yok (D-039 (2)).
- F6d.6 csharp-lsp ve Ponytail'dan 2–3 kural CLAUDE.md'ye.

**Bu fazda yapılmayacaklar:** 6e'nin işleri (DAST, ajan koşuları); `SonarAnalyzer.CSharp` NuGet paketi (Y-06, D-045); AwesomeAssertions (D-045 (2)); Y-44 istisnası olmayan bastırma; Microsoft'un "When to suppress" izni olmayan kuralda kapsam daraltarak susturma.

- [x] F6d.1 Kategori bazlı analiz (alt adımlar 1a–1d)
  - [x] F6d.1a (4a479b7): `AnalysisLevelSecurity=latest-all` (R-117: `AnalysisMode<Kategori>` bileşik `AnalysisLevel`'ın yanında yok sayılıyor). Bulgu yok. 389/389.
  - [x] F6d.1b (adc7e09): `AnalysisLevelReliability=latest-all`. CA2007: `.editorconfig`'te Api, modüller, testler ve `NovaCore.AspNetCore`/`NovaCore.Identity.OpenIddict` için `none` (MS "When to suppress": uygulama kodu ve SynchronizationContext'siz ASP.NET Core); `NovaCore.EntityFrameworkCore`'un 15 `await`'i `ConfigureAwait(false)` (`await using` için `scope.ConfigureAwait(false)`). Kapsam deneme dosyasıyla doğrulandı: EF Core'da CA2007 hata, AspNetCore'da değil (commit'lenmedi). CA2000: testlerde `FormUrlEncodedContent`/`StringContent`/`HttpRequestMessage`/`LogCapture` `using` aldı, `OidcFlow`'un cookie handler'ı bir alanda tutulup `Dispose`'ta bırakılıyor. CA2025: `EmailQueueTests`'teki kayıt isteği host kapanmadan bekleniyor. 389/389.
  - [x] F6d.1c (3b8e42e): `AnalysisLevelPerformance=latest-all`. CA1812 `.editorconfig`'te `none` (59/59 DI, options, EF yapılandırması, JSON ya da test DI'ı; MS'in önerdiği bastırma durumları). CA1849: üç test bağlantısı `OpenAsync`. 389/389.
  - [x] F6d.1d (244e447): `AnalysisLevelUsage=latest-all`. CA2225 → Q-48: `Result<T>`'nin iki örtük operatörü kalktı (PublicAPI, CreatePayment, GetProductById, şablonun GetTodoById'si, iki operatör testi silindi, property testleri `Result.Success`/`Failure` kullanıyor; D-012'ye revizyon işareti). CA2234: 79 test çağrısı `Uri`'ye geçti (58'i betikle literal sarmalama, `OidcFlow.AuthorizeUri` ve `EndSessionUri` `Uri` döndürüyor, 9'u elle; `PermissionResolutionTests`'te tekrar eden yol tek alana alındı). CA2213: `JwtValidationTests` ve `PermissionAuthorizationTests` fixture'ı alana kopyalamak yerine primary constructor parametresini kullanıyor. D-064, CHANGELOG (Changed + Removed). 389 → 387 (iki operatör testi). CI yeşil (template-smoke ve coverage kapısı dahil).
- [x] F6d.2 IDE kuralları, metrikler, format, şablon (alt adımlar 2a–2c; IDE kuralları ile metrikler aynı `.editorconfig` bölümünü değiştirdiği için tek alt adım oldu)
  - [x] F6d.2a (446a412): IDE0051/52/59/60 her yerde, IDE0005 `src/BuildingBlocks/**`'te warning; tek bulgu (`PersistenceExtensions`'ta kullanılmayan using) silindi. CA1502/1505/1506 MS'in varsayılan eşikleriyle warning; CA1506'nın 5 bulgusu (`IdentityServerHostingExtensions` tipi ve `AddCoreIdentityServer`, `AccountEndpoints.Map`, `ConnectEndpoints.Authorize`, `IdentityServerTestHost.StartAsync`) MS'in "bakımı kolaysa" iznine dayanan gerekçeli `[SuppressMessage]` aldı. `CodeMetricsConfig.txt` eklenmedi: eşik değişmiyor (plandan sapma). Deneme dosyalarıyla doğrulandı: pakette IDE0005/0051/0059/0060 hata veriyor, modülde IDE0005 raporlanmıyor ve build bozulmuyor (commit'lenmedi). 387/387.
  - [x] F6d.2b (dfee976, 1b472c2, f58159f): CI'da `dotnet format NovaCore.slnx --verify-no-changes --no-restore` (Build'den sonra). `.editorconfig`'ten `end_of_line = crlf` kalktı (index LF; Linux checkout'u her dosyada kırardı), migration'lar `charset = utf-8-bom` (EF böyle yazıyor), 10 dosyada using sırası düzeldi. **Ölçüm:** `core.autocrlf=false` ile LF checkout'lu bir worktree'de (Windows) format yine kırmızıydı: `end_of_line` yokken formatter dokunduğu satır sonlarını platformun satır sonuyla yazıyor. `.gitattributes` `* text=auto` (`core.eol` varsayılan olarak native) her makinede çalışma ağacını platformla eşleştiriyor; aynı worktree'de format temiz. Index'te değişiklik yok (353 dosya zaten LF). İlk plandaki `.gitattributes`'ı ölçümden önce gereksiz saymıştım; ölçüm somut sorunu gösterdi. **Sürpriz:** `dotnet format` "Duplicate source file PublicAPI.*.txt" uyarısı veriyor: PublicApiAnalyzers 5.6.0 bu dosyaları kendisi ekliyor, `Directory.Build.targets` de (R-037 eski sürüme dayanıyordu); ayrı commit'te düzeltildi (R-118). 387/387. Linux CI'da `Formatting` adımı yeşil.
  - [x] F6d.2c (79024e9): Şablon (D-040 (5)): `Directory.Build.props`'a dört `AnalysisLevel<Kategori>=latest-all`; `.editorconfig`'te CA2007 ve CA1812 tüm projede `none` (bütün çözüm ASP.NET Core uygulama kodu), IDE0051/52/59/60 ve CA1502/1505/1506 warning, migration'lar `utf-8-bom`. Smoke testi ilk koşuda yalnız host'un `Program.cs`'inde CA1506'yı yakaladı (44 > 40; composition root, her modülle büyüyor, top-level statements attribute almıyor) → yalnız o dosya için `none`. Şablon testlerinde 11 string URI `Uri`'ye geçti (`TodosUri` alanı). **Plandan sapma:** şablonun CI'ına format adımı ve `.gitattributes` eklenmedi: üretilen iki projede `dotnet format --verify-no-changes` 9'ar `IMPORTS` hatası verdi (using sırası proje adına bağlı), adım tüketicinin ilk CI koşusunu kırardı (Sonraya bırakılanlar'daki Faz 5 maddesi güncellendi). `bash eng/template-smoke.sh` yeşil: BuiltIn 8/8, External 7/7. D-065 (F6d.2'nin kararları). CI yeşil.
- [x] F6d.3 Ek analizörler (alt adımlar 3a–3c)
  - [x] F6d.3a (2b30668): Microsoft.CodeAnalysis.BannedApiAnalyzers 5.6.0 (PublicApiAnalyzers ile aynı sürüm; Microsoft öneki, hook sormadı), `Directory.Build.targets`'tan `src/` altındaki derlenen projelere (şablon paketi hariç). `src/BannedSymbols.txt`: `Console`, `Environment.Exit`, üç `BuildServiceProvider` overload'u (Y-24), üç `HttpClient` kurucusu (Y-30). `src/BannedSymbols.Clock.txt`: `DateTime.Now/UtcNow/Today`, `DateTimeOffset.Now/UtcNow`; `NovaCore.Abstractions` hariç (D-021 (6) istisnası orada; `SourceHygieneTests`'in regex'i bütün `src/`'i taramaya devam ediyor, `#pragma` gerekmedi). Her overload ayrı satır (BannedApi'nin ID biçimi parametresiz girdiyi yalnız parametresiz overload'a uyguluyor). Mevcut kodda bulgu yok. Deneme dosyalarıyla doğrulandı: modülde `Console` ve `DateTimeOffset.UtcNow` RS0030, Abstractions'ta saat serbest, `Environment.Exit` RS0030 (commit'lenmedi). Beş lock dosyası `--force-evaluate` ile yenilendi, `--locked-mode` geçti. 387/387. CI yeşil.
  - [x] F6d.3b (aa2cc16): Meziantou.Analyzer 3.0.303, `Directory.Packages.props`'ta `GlobalPackageReference` (README'nin CPM önerisi; development dependency, hiçbir paket ona bağlanmıyor). Varsayılan modda 635 uyarı → Q-50 (kullanıcı: varsayılan + 3 dar kapatma): MA0048 her yerde, MA0004 CA2007'nin kapalı olduğu yerlerde, MA0051 migration'larda `none`. Düzeltilenler: MA0002/MA0006 Meziantou'nun code fixer'ıyla (`dotnet format analyzers`, 144 yer `Ordinal`; mevcut eşitlik davranışının aynısı; fixer'ın bozduğu üç satır kırılımı elle düzeltildi), `AdminEndpoints`'teki tek `OrderBy` artık kültürden bağımsız `Ordinal`; MA0074 `StartsWithSegments(..., OrdinalIgnoreCase)` (PathString'in varsayılanı); MA0009 10 regex'e 1 sn timeout, MA0023 için `ExplicitCapture`; MA0015/0016/0054/0084 testlerde (MA0084, F6d.1d'deki `_factory` değişikliğimin gölgelediği yereli gösterdi: `production` oldu); MA0008 `PaginationRequest`'e `[StructLayout(LayoutKind.Auto)]`. MA0051: `AddCoreIdentityServer` kayıt sırası korunarak beş özel metoda (hesap deposu, e-posta, OpenIddict sunucusu, oturum cookie'leri, client ayarları), `DeliverAsync`'in handler döngüsü `RunHandlersAsync`'e, test host'unun varsayılanları `UseTestDefaults`'a bölündü; ardından `AddCoreIdentityServer`'ın ve test host'unun CA1506 bastırmaları gereksizleşti ve kalktı (5 → 3). **Hook boşluğu:** `package-guard.js` `GlobalPackageReference`'ı yakalamadı, onay istemedi (paket D-045/D-049'da onaylı) → Sonraya bırakılanlar (hook dosyası korumalı). Lock dosyaları yenilendi, `--locked-mode` ve format temiz. 387/387; paketlerin nuspec'inde analizör bağımlılığı yok (`BMD.NovaCore.AspNetCore` pack edilip bakıldı).
  - [x] F6d.3c (e222a7d): Roslynator CLI 1.0.1 (`dotnet-tools.json`), `eng/find-unused.sh` ve haftalık + elle `unused-code.yml` (rapor, kapı değil; job özeti). **Ölçüm:** bütün türlerle 101 yanlış pozitif ağırlıklı sembol (DI kurucuları, JSON DTO'ları, minimal API handler'ları); rapor tip, alan, sabit, enum üyesi ve olaylarla sınırlandı: bugün 2 beklenen girdi (reflection'la uygulanan `StockReservationConfiguration`, `PaymentConfiguration`). Deneme sınıfıyla doğrulandı (commit'lenmedi). actionlint + zizmor temiz.
- [x] F6d.4 SaaS analizi (alt adımlar 4a–4c; Sonar'ın ilk gerçek koşusu kullanıcının kurulumunu bekliyor)
  - [x] F6d.4a (93950d5): SonarQube Cloud `ci.yml`'de: `build-and-test` Cobertura coverage'ını 1 günlük artifact olarak yükler; `sonar-token` job'u token'ın varlığını söyler (fork, Dependabot ve kurulmamış repo atlanır; yalnız push); `sonar` job'u (`needs: [build-and-test, sonar-token]`) locked restore, `dotnet-sonarscanner` 12.0.0'ı job'da kurar (NuGet referansı yok), `begin` (Cobertura, `**/Migrations/**` hariç) → Release build → `end`. actionlint + zizmor temiz (auditor personasındaki `secrets-outside-env` R-120'de). **İlk koşu kullanıcının kurulumunu bekliyor (Q-49):** `SONAR_TOKEN` henüz yok; CI'da (`93950d5`) `sonar-token` koştu, `sonar` atlandı.
  - [x] F6d.4b (d7ff172): Qodana Community for .NET deneme koşusu (yerel Docker, 2026.2.699, token'sız). **.NET 10 desteği ölçüldü:** `qodana.yaml`'daki `bootstrap` SDK 10.0.300'ü kurdu, `.slnx` analiz edildi, 12,3 dk. 220 sonuç (89 warning, 131 note), çoğu yanlış pozitif (JSON DTO'ları, configuration options, EF kurucuları); birkaç küçük gerçek bulgu Sonraya bırakılanlar'da. CI'a eklenmedi (R-120). `qodana.yaml` repoya girmedi (geçici worktree'de).
  - [x] F6d.4c (d7ff172): CodeRabbit repoda yapılandırma istemiyor; GitHub uygulamasını kullanıcı kurar (Q-49). Yalnız PR'ları inceler; bu repoda bugün Dependabot PR'ları.
  - [x] F6d.4d (merge sonrası, `faz/6d-coderabbit`): 4c'nin "yapılandırma gerekmiyor" sonucu CodeRabbit'i Dependabot PR'larına bırakıyordu; kullanıcı tamamını istedi (Q-51, D-067, R-121). Kökte şemaya karşı doğrulanmış `.coderabbit.yaml` (tr, assertive, DECISIONS code guideline, dört özel pre-merge check, üretilmiş dosyalar dışarıda); faz skill'i: adım commit'inden önce `coderabbit review --agent`, faz sonunda PR + CodeRabbit + `gh pr merge --merge`. CLI 0.9.0 ve Claude Code eklentisi kuruldu; CLI oturumu kullanıcıda. Bu değişikliğin kendisi PR ile gider; CodeRabbit'in ilk gerçek incelemesi o PR'da.
    - **İlk gerçek inceleme (PR #10, 2026-10-23, `2715e09`, ASSERTIVE, Advanced; saatte 10 incelemeden 1'i):** 4 yorum, dördü de doğru, hepsi düzeltildi: (1) `.coderabbit.yaml`'da şablonun kaynak adı `NovaCore` yazılmıştı, D-040 (4)'e göre `MyService`; (2) Y-44 check'i bastırmayı tek bir D kaydıyla kabul ediyordu: artık ya MS'in "When to suppress" izni + kalıcı + gerekçe + R/D kaynağı ya da kullanıcı onayını kaydeden D/Q kaydı; test zayıflatma yalnız kullanıcı onayıyla; (3) skill: CLI bulgusu düzeltilince build/test commit'ten önce yeniden koşar; (4) skill: faz sonu düzeltmeleri ve PROGRESS push'tan önce commit'lenir, CodeRabbit sonuçları da merge'ten önce. Yanlış pozitif yok; markdownlint/languagetool Türkçe belgelerde gürültü yapmadı.
    - **İkinci inceleme (`c282eec`):** 1 yorum, yanlış pozitif: Y-44 check'inden kullanıcı onayı yolunun kaldırılmasını istedi. Değiştirilmedi; Yapılmayacaklar girişi her Y maddesi için kullanıcıya sorma yolunu tanıyor, CLAUDE.md "user approval, unless Microsoft allows" diyor, D-011 ve Q-50 bu yolla onaylanmış bastırmalar. Yorum gerekçesiyle yanıtlandı ve çözüldü.
    - CodeRabbit aynı bulguyu PROGRESS satırında bir kez daha açtı ve yine geri çekti; kök neden Y-44'ün metniydi → Q-52, Y-46.
    - **Merge (2026-10-23):** kullanıcı onayı "PR #10'u gh pr merge 10 --merge ile main'e merge et ve yerel main'i güncelle"; merge commit `0e42a3f`. Son kod dışı commit `714efad`'in CI'ı merge anında sürüyordu; aynı koddaki `59f61ce`'nin CI'ı ve CodeQL'i yeşildi, `714efad` yalnız belge, skill ve `.coderabbit.yaml` değiştiriyordu, yerel build temizdi. PR akışıyla (D-067 (1)) yapılan ilk merge.
    - **CLI oturumu (2026-10-23):** kullanıcı `coderabbit auth login` ile girdi; `auth status`: GitHub, bölge US, plan **Advanced (trial)**, seat atanmış; `usage`: repo başına saatte 10 inceleme, kullanım faturası kapalı. *Dikkat:* plan deneme sürümü; deneme bitince plan düşerse özel pre-merge check'ler ve simplify (Team ve üstü) çalışmayabilir, D-067 (5) o zaman yeniden ölçülür. Dependabot için seat ataması hâlâ kullanıcıda.
- [x] F6d.5 atlandı: `PackageValidationBaselineVersion` ilk kararlı sürümden sonra (D-039 (2)); yayımlanan tek sürüm `0.1.0-preview.1`.
- [x] F6d.6: Ponytail'dan üç kural kök CLAUDE.md'ye ("Working habits": düzenlemenin ulaşması gereken her yeri, şablonun derlenmeyen kopyaları dahil, önce listelemek; hata düzeltmesine çağıranlardan başlayıp kök nedeni ortak kodda bir kez düzeltmek; raporu atlanan ya da kontrol edilmeyenle bitirmek). `shortcut:` yorumu alınmadı (Y-38, Y-45). csharp-lsp: `csharp-ls` 0.28.0 global dotnet aracı olarak kuruldu (`.slnx` ve .NET 10 destekli, sürüm notları). **Engellendi:** eklentiyi `.claude/settings.json`'da etkinleştirme ve CLAUDE.md'ye eklenti notu auto-mode sınıflandırıcısınca reddedildi (Self-Modification); atlatılmadı. İnsanlar için kurulum adımı README'de ("Analizörler"); eklentiyi kullanıcı `/plugin` ile etkinleştirir, CLAUDE.md notunu isterse kendisi ekler. Dokümanlar: README (CI tablosu, Analizörler, komutlar, paket/lisans tablosu), kök CLAUDE.md (lint paragrafı, saat kuralı, bastırma listesi), tests/CLAUDE.md, src/Templates/CLAUDE.md, src/BuildingBlocks/CLAUDE.md (F6d.1d). Memory `novacore-stack` güncellendi. Commit `0857887`.
- Faz doğrulaması (2026-10-23, `0857887` + review düzeltmeleri; PLAN 6d'de ayrı liste yok, 6c'ninki kullanıldı): `restore --force` ve `--locked-mode` exit 0; `build -c Release` 0 uyarı / 0 hata; `dotnet format --verify-no-changes` temiz; `eng/check-coverage.sh Release` 365/365 (Docker'la, SQL Server dahil, atlanan yok) satır %95,7 / dal %82,8 (baseline %95,8 / %82,9, tolerans içinde; operatör testleri ve bölünen metotlar); mimari testler 22/22 (toplam 387); lisans kapısı exit 0; açık paket yok; `has-pending-model-changes` üç context'te temiz; `eng/check-openapi.sh` kırılım yok; `eng/lint-workflows.sh` temiz; `eng/find-unused.sh` 2 beklenen girdi; `eng/mutation.sh` %79,07 / %61,11 / %70,00 (değişmedi; ilk denemede Payments hedefi kilitli bir DLL yüzünden düştü, build sunucusu kapatılıp yeniden koşuldu); `bash eng/template-smoke.sh` BuiltIn 8/8, External 7/7. GitHub: her push'ta CI (build-and-test, api-contract, template-smoke, secret-scan, workflow-lint, sonar-token) ve CodeQL yeşil; `sonar` token olmadığı için atlandı.
- Code review (`code-review` skill'i, high, `main...faz/6d-analizor-sikilastirma`, 6 bulgu): düzeltilen: #2 D-065 (2)'deki 5 CA1506 bastırması 3'e indiği halde işaretsizdi (revizyon işareti), #3 README ve `.editorconfig` `NovaCore.Abstractions`'ın `ConfigureAwait(false)` çağırdığını söylüyordu (await'i yok), #4 kök CLAUDE.md komut listesinde `eng/find-unused.sh` ve `dotnet format` yoktu. Değişiklik yapılmayan: #1 `Directory.Build.targets`'taki yol öneki koşulu (iki değer de aynı proje yolundan türüyor), #5 `OidcFlow` kurucusunda sabit bir `Uri` hatası olursa handler sızıntısı (yalnız test, ihmal edilebilir), #6 Sonar job'unun ikinci Release build'i (tarayıcının begin/end sarmalaması gerektiriyor).

### Faz 7a
Başlangıç 2026-10-23, `main` @ `af5ce0b` (plan commit'i henüz push edilmemişti, branch'le gider), 387 test. Plan onaylandı (plan modu). Faz branch'i her adımdan sonra push edilir (Q-59), main'e dokunulmaz; faz sonunda PR (D-067). Q-40 sürüyor. Kararlar D-072'den, araştırmalar R-125'ten başlayarak yazılır.

**Plan özeti ve kendi kararlarım (D-016):**
- F7a.1 Lisans kapısı Y-47'yi uygular: adlı sağlayıcı istisnaları tam paket adıyla (desen değil), iki geçişte; şablonun ürettiği projeye kendi `eng/check-licenses.sh`'ı ve CI adımı.
- F7a.2 Sağlayıcıyı host seçer: `AddModuleDatabase` (host, bir kez; sonraki çağrı yerini alır; yoksa açılış durur) ve `AddModuleDbContext<T>(schema, configure)` (migration geçmişi tablosunu çekirdek şemaya koyar). Sağlayıcıya özgü her şey (migration'lar, design-time factory'ler, sağlayıcı paketi, Pkcs pini) modüllerden host'a taşınır (D-026 (2)'nin kalıbı, MS "separate migrations assembly"); migration ID'leri ve operasyonları aynı kalır. Design-time factory'ler kalır, çünkü `--auth External` projesi resolver'ı yazılana kadar Development'ta `Build` edemiyor (D-024 (7)).
- F7a.3 PostgreSQL test katmanı: PG migration setleri test assembly'sinde (yeni proje yok, Y-17), aynı senaryolar soyut taban sınıflarla iki sağlayıcıda, sağlayıcıya özgü testler ayrı.
- F7a.4 Şablon `--db SqlServer|PostgreSql`; compose eklenmez (D-040 (4), PLAN'daki "compose servisi"nden sapma).
- F7a.5 Belgeler.

**Bu fazda yapılmayacaklar:** çok kiracılılık (7b); kurum başına veritabanı; MySQL/Oracle testleri (yalnız belgelenir); RLS; `.claude/hooks/` düzenlemesi (kullanıcıda); repo'nun `NovaCoreApiFactory`'sindeki strip yaklaşımını sadeleştirmek (Y-09); token pruning'in gerçek veritabanında testi.

- [x] F7a.1 (23f89f9): Lisans kapısı adlı sağlayıcı istisnalarını tam paket adıyla taşır (`Npgsql`, `Npgsql.EntityFrameworkCore.PostgreSQL`, iki geçişte; `Polly.*` yalnız geçişli). **Ölçüm (R-125, scratchpad deneme projesi):** istisnasız kırmızı (exit 3), iki ad yazılınca yalnız `Npgsql.NodaTime` kırmızı (exit 1), yalnız `Npgsql` yazılınca EF sağlayıcısı da kırmızı (exit 2: tam ad önek gibi davranmıyor), üçü yazılınca yeşil. Repo kapısı yeşil. Şablon: üretilen projeye `eng/check-licenses.sh` (`nuget-license` 4.0.18 araç manifestinde) ve CI'da "Package licenses" adımı; `bash eng/template-smoke.sh` her iki `--auth`'ta kapıyı koşturdu, yeşil (BuiltIn ve External testleri geçti). Y-06 → Y-47: `ci.yml`, `eng/check-licenses.sh`, README; hook mesajı (`.claude/hooks/package-guard.js:99`) kullanıcıda. D-072, CHANGELOG. 387/387. CodeRabbit CLI: 10 bulgu; 1'i kapsam içi (R-125'teki belirsiz parantez) düzeltildi, 9'u izlenmeyen `.agents/`, `.codex/` dosyalarında (Q-40, dokunulmadı). *Not:* üretilen projenin CI'ı GitHub'da koşmuyor; `dotnet tool restore` adımı migration adımındaki gibi feed token'ı almıyor (package source mapping'e güveniyor).
- [x] F7a.2 (9793988): Sağlayıcıyı host seçer. `AddModuleDatabase(configure)` (options'ta; sonraki çağrı yerini alır, yoksa `ValidateOnStart` açılışı durdurur) ve `AddModuleDbContext<T>(schema, configure)` (host'un veritabanı → geçmiş tablosu şemada, `RelationalOptionsExtension` ile → modülün ayarı → outbox interceptor'ı). Catalog ve Payments'ın migration'ları ve design-time factory'leri `src/Api/NovaCore.Api/{Catalog,Payments}/`'a (`git mv`, namespace klasörü izler); ortak `DesignTimeDatabase`. Modüller SqlServer, Pkcs ve Design'ı bırakır; lock dosyaları `--force-evaluate`, `--locked-mode` temiz. **Ölçüm:** taşımadan önce ve sonra üç context'in `migrations list` ve `script --idempotent` çıktıları birebir aynı; `has-pending-model-changes` üçünde temiz; SQLite şemalı geçmiş tablosunu şemasız açıyor (`DatabaseMigrationServiceTests` yeşil). Testler önce kırmızı: `ModuleProviderTests` (Catalog, Payments, şablonun Todos'u sağlayıcıya bağlıydı), `ModuleDbContextRegistrationTests` (yeni API, derlenmiyordu). Yeni: 3 birim, 2 mimari, 1 SQL Server testi (design-time factory'ler çalışma zamanıyla aynı sağlayıcı, migration assembly'si, migration listesi ve geçmiş tablosu; `MigrationTests` context'leri factory'lerden alır). Şablon: Todos'un migration'ları ve factory'si Api'de, `Program.cs` `AddModuleDatabase`, CI ve smoke `has-pending-model-changes`/bundle'ı Api üzerinden; `bash eng/template-smoke.sh` iki `--auth`'ta yeşil. 393/393, format temiz. D-073 (D-036 (8)'e revizyon işareti), R-126, CHANGELOG. CodeRabbit CLI: 16 bulgu; 10'u Q-40 dosyalarında; `ModuleProviderTests`'in repo modüllerinin csproj'larını da taraması kabul edildi (kullanılmayan paket assembly referansı bırakmaz); R-126 başlığından sonra boş satır (dosyanın kendi biçimi böyle değil) ve D-036 işaretinin yeni kayıt istemesi (revizyon kuralının emsali uygulandı, yeni kayıt D-073) reddedildi; yüklenemeyen bir şablon paketinin tanınmaması kabul edilen sınır (testin yorumunda).
- [x] F7a.3 (a848635): PostgreSQL test katmanı. Paketler `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 ve `Testcontainers.PostgreSql` 4.16.0 (hook onay istemedi; ikisi D-068/PLAN'da adıyla geçiyor), test projesine `Microsoft.EntityFrameworkCore.Design`. **Lisans kapısı ölçümü:** Npgsql'le yeşil; istisna kaldırılınca `Npgsql` ve `Npgsql.EntityFrameworkCore.PostgreSQL` kırmızı (exit 2). İmaj `postgres:18.4-trixie` + digest (7 günlük bekleme süresini geçen en yeni 18.x; 18.6 iki günlük), compose'da opsiyonel `postgres` servisi (parola `.env`'den, diğer servisler için zorunlu değil; `docker compose config` temiz), `SqlServerImageTests` → `DatabaseImageTests`. PG migration'ları test projesinde `PostgreSql/<Modül>/Migrations`, `dotnet ef migrations add Initial` ile üretildi (EF snapshot'ları `--namespace`'ten türeyen bir klasöre yazdı, elle yanlarına taşındı); `.editorconfig`'in migration bölümü yalnız doğrudan `Migrations/` altını eşlediği için düzen host'unkiyle aynı yapıldı ve bölüm `{src,tests}/**/Migrations/*.cs` oldu. `PostgreSqlModelTests` (Docker'sız) PG setinin modelle eşleştiğini denetler. Ortak altyapı `Databases/` (`DatabaseApiFactory`, `TestDatabase`, `PortableSql`) ve dört soyut senaryo sınıfı; SQL Server tarafı bunlara geçti (`OutboxRetentionTests` → `SqlServerOutboxTests`, `MigrationTests` ve `SqlServerQueryTests` tabandan türer), `IdentityServerWiringTests`'in hesap hazırlığı `HostAccounts`'a çıktı. **İlk koşuda yeşil:** PostgreSQL 14/14, SQL Server 15/15 (Npgsql'in migration kilidi tablo bırakmıyor, `public` boş). Sağlayıcı farkı ölçüldü: arama PG'de büyük/küçük harfe duyarlı (`PostgreSqlQueryTests`). 410/410, format ve `--locked-mode` temiz. D-074, R-127, CHANGELOG, README paket tablosu. CodeRabbit CLI: 4 bulgu, hepsi Q-40 dosyalarında.
- [x] F7a.4 (50fa8fd): Şablonda `--db SqlServer|PostgreSql` (D-075, R-128). Seçim yalnız Api'ye dokunur (paket, Pkcs pini yalnız SQL Server'da, `AddModuleDatabase`, `DesignTimeDatabase`, bağlantı dizesi, lisans istisnası, migration'lar). PG migration'ları paketlenmiş şablondan üretilen `MyService` projesinde üretildi, şablonda `MigrationsPostgreSql`'de; sabit sembolün `fileRename`'i `Migrations` yapar. Şablonun test fabrikası `AddModuleDatabase`'i SQLite ile değiştirir. Compose eklenmedi (D-040 (4), PLAN'dan sapma). `.coderabbit.yaml` migration globları `Migrations*`. **Ölçüm:** `bash eng/template-smoke.sh` dört bileşimde yeşil (build, test, lisans kapısı, `has-pending-model-changes`, yalnız seçilen veritabanı); üretilen PG projesinde kapı istisnasız kırmızı (exit 2). İlk koşu Windows MAX_PATH'e takıldı (uzun proje adı + derin scratchpad yolu, `e_sqlite3`); adlar kısaltıldı, smoke varsayılan kısa geçici dizinde koşuyor. CodeRabbit CLI: 11 bulgu, hepsi Q-40 dosyalarında. 410/410.
- [x] F7a.5 (111b304): README (şablon, `--db`, sorumluluk tablosu, migration komutları, "Sağlayıcılar" tablosu: MySQL/MariaDB'de şema = veritabanı, ölçülmedi; SQLite şemayı yok sayar, ölçüldü; collation farkı; sağlayıcı değiştirme), kök CLAUDE.md, Modules/BuildingBlocks/Templates/tests CLAUDE.md, memory (`novacore-project`, `novacore-stack`).
- [x] F7a.1 ek (454147d): `.claude/hooks/package-guard.js:99` mesajında Y-06 → Y-47. Korumalı dosya; değişikliği kullanıcı yaptı, faz sonunda kullanıcının seçimiyle faz branch'ine commit'lendi.
- Faz sonu (2026-10-24, yeni oturum): Docker Desktop kapalıydı, açıldı. Doğrulama baştan koşturuldu: `restore --locked-mode` temiz; `build -c Release` 0 uyarı; `eng/check-coverage.sh Release --update` 385/385 (SQL Server ve PostgreSQL dahil, atlanan yok), satır %96,6 / dal %82,7 → baseline satır 96,6, dal 82,9 kaldı (69958af); mimari testler 25/25; `dotnet format --verify-no-changes` temiz; lisans kapısı yeşil; `has-pending-model-changes` üç context'te temiz; `eng/check-openapi.sh` değişiklik yok; `eng/lint-workflows.sh` temiz; `eng/template-smoke.sh` dört bileşimde yeşil. **Code review** (`code-review` skill'i, high, 6 bulgu): (1) outbox saklama senaryosu kayıtları mesajla JOIN üzerinden sayıyordu, mesajsız kalan kayıt "silindi" görünürdü → `MessageId` ile ayrı sayılıyor; düzeltme bir yarışı da gösterdi: dispatcher'ın silmesi süresi geçmiş mesajı iki INSERT arasında silebiliyordu, eski `INSERT ... SELECT` bu durumda kayıt yazmayıp testi boşa geçiriyordu; mesaj ve kayıt tek transaction'da, execution strategy üzerinden (8d58413); (2) `NovaCoreApiFactory`'nin yorumları SQL Server'ı modüllere atfediyordu → `AddModuleDatabase` (8d58413); (3) `ModuleProviderTests`'in yalnız `FileNotFoundException` yakalaması: önce genişletildi, CodeRabbit CLI "yüklenemeyen sağlayıcıyı sessizce kabul etme" dedi, geri alındı (yüksek sesle düşmesi mimari koruma için doğru); (4) `GetPayments` sıralamasında eşitlik bozucu yok, (5) compose'daki `postgres` parolasız `up`'ta düşüyor → Sonraya bırakılanlar; (6) `PostgreSqlDesignTime` ile `DesignTimeDatabase` benzerliği: farklı assembly'ler, host'unki internal; değişiklik yok. 410/410 (Debug), format temiz.
- PR #11 (2026-10-24): CI'ın bütün job'ları, CodeQL, Sonar ve Greptile yeşil (Greptile bulgu vermedi). **CodeRabbit:** 2 satır yorumu, 3 pre-merge uyarısı. (1) `docker-compose.yml:32` `POSTGRES_DB` yok, örnek bağlantının `NovaCore` veritabanı oluşmuyor → yanıtlandı: EF'in migrator'ı eksik veritabanını kendisi oluşturuyor (PG katmanı her sınıfta var olmayan bir veritabanı adıyla tam buna dayanıyor; compose'daki SQL Server servisi de `NovaCore`'u önceden oluşturmuyor). (2) `NovaCoreApiFactory` `AddModuleDatabase`'e geçsin → kapsam dışı (Y-09, faz notu), Sonraya bırakılanlar, yanıtlandı. (3) Başlık İngilizce ve emir kipinde olmalı (`.coderabbit.yaml`, D-067 (5); PR #10 da böyleydi) → başlık `faz7a: let each project choose its database` oldu. (4) Y-46 kontrolü PG migration'larının `*.Designer.cs`/`*ModelSnapshot.cs`'indeki `#pragma warning disable 612, 618`'i bastırma saydı → yanlış pozitif: EF bu satırı ürettiği her designer ve snapshot dosyasına yazıyor (host'un SQL Server setinde de var), path filter'lar bu dosyaları zaten dışarıda bırakıyor; kontrolün metnine araçların ürettiği dosyalar istisnası eklendi. (5) Belgeler: `CLAUDE.md:70` ve `README.md:310`'daki lisans kapısı komut açıklamaları yalnız Polly'yi sayıyordu → Npgsql istisnası eklendi. Biome'un şablonun `appsettings.Development.json`'ını (şablon koşullu JSONC) ayrıştıramaması bir araç hatası, bulgu değil. İkinci inceleme (`ceda48d`, diff dışı 1 bulgu): `.coderabbit.yaml`'ın Y-47 kontrolü sağlayıcı istisnasını yalnız "projenin sağlayıcısı" diye anıyordu → D-072'nin iki tam paket adı ve ailenin diğer paketlerinin istisna olmadığı yazıldı.
- Faz doğrulaması (kısmi, 2026-10-24, önceki oturum): `build -c Release` 0 uyarı; `dotnet test` 410/410 (SQLite, SQL Server, PostgreSQL, Docker'la, atlanan yok); `eng/check-coverage.sh Release` satır %96,6 / dal %82,7 (baseline %95,8 / %82,9, tolerans içinde; baseline `--update` henüz yapılmadı); `has-pending-model-changes` üç context'te temiz + `PostgreSqlModelTests`; lisans kapısı yeşil; `eng/check-openapi.sh` değişiklik yok; `eng/lint-workflows.sh` temiz; `dotnet format` temiz; smoke dört bileşimde yeşil. CI: F7a.3'e kadar her push yeşil (PG katmanı Linux'ta).

## Eski devir notları

**2026-10-25, Faz 7b: Çok kiracılılık (⏸ duraklatıldı, F7b.6'dan sonra).** Branch `faz/7b-cok-kiracililik`, yerel, push edilmedi (Q-60). Bu oturum: F7b.5'in çalışma ağacındaki yarısını bitirdi (`67a2c28`, PROGRESS `b2e4eb7`: eksik D-081, CHANGELOG ve kabul senaryosu eklendi) ve F7b.6'yı yaptı (`a12b291`). Önceki notun "F7b.5 yarım" kısmı artık geçersiz. Biten: F7b.1, F7b.7, F7b.3, F7b.2, F7b.4, F7b.5, F7b.6. Oturum iki adımda durdu: F7b.8 büyük (şablon + sekiz smoke bileşimi), D-082.

- **Sıradaki adım F7b.8 (şablon `--multitenancy`, D-076 (12): varsayılan kapalı).** Bilinmesi gerekenler: eklenti artık `NovaCore.EntityFrameworkCore`'a da bağlı (şablonun `Directory.Packages.props`'una `BMD.NovaCore.MultiTenancy`); seçenek açıkken Api `AddCoreMultiTenancy` + `AddModuleDbContext<MultiTenancyDbContext>` ve iki sağlayıcıda store migration'ları ister (PG seti D-075 (2)'deki gibi paketlenmiş şablondan üretilir), Todos'ta kurum kapsamlı varlık modelin değişmesi demek (iki sağlayıcıda Todos migration'ı). Smoke bileşimleri `--auth` × `--db` × `--multitenancy` (8). F7b.4 şablonun `ApiFactory`'sine dokundu ve şablon smoke'u bu fazda **hiç koşmadı**: F7b.8'in ilk işi mevcut smoke'u koşturmak olmalı.
- **Sonra F7b.9 (belgeler):** README çok kiracılılık rehberi (kurum çözümü, izolasyon, sistem işi, outbox'ta kurum ve trace, yaşam döngüsü ve silme, sağlayıcı başına RLS örneği), "Ezilemeyenler" paragrafı, Y-14'ün canlı atıfları → Y-48, memory. Ardından faz sonu (§5): doğrulama komutları, code review, CodeRabbit CLI, PR.
- **Sürprizler:** `TenancyFixture` artık `AddCorePersistence` ile dağıtıcıyı gerçek saatte çalıştırıyor (handler testleri ≤5 sn bekler); trace testi `ActivityListener`'ı yalnız kendi trace kimliğine örnekliyor, çünkü dinleyiciler süreç genelinde. Ham SQL metni bir değişkene atanmadan `SqlQueryRaw`'a giderse EF1002 build'i kırıyor. Model sırası entity adına göre: `Reminder` bilerek `Note`'tan sonra sıralanıyor, bağımlı-önce silmeyi sınamak için.
- **Doğrulama (bu oturum):** `dotnet build` 0 uyarı, `dotnet test` 524/524 (SQLite, SQL Server, PostgreSQL; Docker açıktı), `dotnet format --verify-no-changes` ve `restore --locked-mode` temiz, Catalog ve Payments'ta `has-pending-model-changes` temiz (F7b.5), `PostgreSqlModelTests` yeşil.
- **Doğrulanmayan:** şablon smoke'u (fazda hiç koşmadı); faz branch'inde CI (push yok, Q-60); coverage ve OpenAPI kapıları (faz sonunda).
- **Bekleyen soru:** yok. Q-40 sürüyor (izlenmeyen `.agents/`, `.codex/`, `AGENTS.md`).
- Devam: `/clear`, ardından `/novacore-phase 7b`.

**2026-10-24 akşam, Faz 7b: Çok kiracılılık (⏸ duraklatıldı, F7b.5 ortasında).** Bu oturum faz dışıydı: PR #12'yi (D-082) merge etti, `main`'i faz branch'ine aldı (`778dece`; PR #12'nin kararı burada D-076 ile çakıştı, D-082 oldu), Stop hook'unu düzeltti (`ff65bcc`: kırmızı ağaçta her tur build + test'i yeniden çalıştırıyordu). Biten: F7b.1, F7b.7, F7b.3, F7b.2, **F7b.4** (`df447eb`, başka bir oturumda commit'lendi, PROGRESS satırı bu oturumda yazıldı). Aşağıdaki öğlen notunun stash ve "F7b.4'te kalanlar" kısmı geçersiz: stash yok, F7b.4 bitti.

- **F7b.5 yarım, commit'lenmedi (çalışma ağacında):** `OutboxMessage`'a `TenantId` ve `TraceParent` kolonları; `OutboxInterceptor` kaydeden kapsamın kurumunu ve `Activity.Current`'ın `traceparent`'ını yazıyor; `OutboxDispatcher` handler kapsamında `TenantContext.Use(tenant)` yapıyor ve handler'ları kendi `CONSUMER` "process" span'inde çalıştırıyor (`ActivitySourceName` = `NovaCore.EntityFrameworkCore`, ebeveyni ve bağlantısı kaydedilen `traceparent`); `AddCoreObservability` o kaynağı `AddSource` ile alıyor. Migration'lar `OutboxTenantAndTrace` dört sette (Catalog ve Payments; SQL Server host'ta, PostgreSQL testlerde). Birim testleri `OutboxInterceptorTests`, `OutboxDispatcherTests`, `ObservabilityTests`'te. Araştırma R-131 RESEARCH'te.
- **F7b.5'te kalanlar (ağaçtan çıkarıldı, önceki oturumun planı kayıtlı değil, kontrol et):** D-081 kaydı DECISIONS'ta yok (R-131 ona atıf yapıyor); CHANGELOG yok; PLAN'ın kabul testi (handler, mesajı yazan kurumun verisini görür, başkasınınkini görmez) için `Databases/` altında entegrasyon senaryosu yok (`OutboxScenarios` değişmedi). Outbox'ı olan context'ler yalnız Catalog ve Payments, migration seti tam.
- **Doğrulama (bu oturum):** `df447eb` + F7b.5'in yarım hâliyle `dotnet build` ve `dotnet test` yeşil (Stop hook'u üzerinden, 342 sn; Docker açıktı, Testcontainers SQL Server ve PostgreSQL'i kaldırdı). Test sayısı kaydedilmedi: hook yalnız kırmızıda çıktı veriyor. Format, `--locked-mode` ve smoke koşmadı.
- **Paralel build uyarısı:** aynı klasörde iki `dotnet build` (ör. bir arka plan koşusu ve Stop hook'u, ya da Codex) `obj`'deki DLL'i kilitleyip CS2012 ile düşürüyor; kod hatası değil, tek build ile yeniden koş.
- **Oturum bütçesi (D-082):** oturumda en fazla 3 adım; devam `/clear`, ardından `/novacore-phase 7b`. CodeRabbit CLI adımlarda değil, faz sonunda bir kez.
- **Bekleyen soru:** yok. Q-40 sürüyor.
- **Doğrulanmayan:** şablon smoke'u bu fazda hiç koşmadı; F7b.4 şablonun `ApiFactory`'sine dokundu, faz sonunda mutlaka koşmalı. Faz branch'inde CI koşmadı.

**2026-10-24, Faz 7b: Çok kiracılılık (⏸ duraklatıldı, F7b.4 ortasında; kullanıcı oturumu kesti).** Branch `faz/7b-cok-kiracililik`, yerel, push edilmedi (Q-60: yalnız faz sonunda). Plan onaylı, sıra F7b.1 → F7b.7 → F7b.3 → F7b.2 → **F7b.4** → F7b.5 → F7b.6 → F7b.8 → F7b.9 (D-076 (13)). Biten: F7b.1 (`e4ab92d`), F7b.7 (`99080c1`), F7b.3 (`b90da7e`), F7b.2 (`9eef5d3`); son yeşil koşu 479/479 (SQLite, SQL Server, PostgreSQL; Docker gerekli).

- **Devam ederken önce:** `git stash list` → `stash@{0}` "faz7b F7b.4 yarim: …" → `git stash pop`. İçeriği yukarıdaki F7b.4 satırında. Stash'le testler kırmızı olur: repo'nun `ApiFactory`'si `services.Single(d => d.ServiceType == typeof(IPermissionResolver))` yapıyor, Api host'unda artık iki resolver var (kimlik + üyelik).
- **F7b.4'te kalanlar:**
  1. Test fabrikaları birleşime geçer: `ApiFactory`'nin `GrantingPermissionResolver`'ı sarmalamak yerine ek resolver olur (test issuer'ının subject'lerine izin, diğerlerine boş). Kimlik resolver'ı test token'larının Guid subject'leri için store'a gidecek, bu yüzden `NovaCoreApiFactory` kimlik şemasını da kurucuda `EnsureCreated` etmeli. Şablonun `ApiFactory`'si de aynı; External'daki "registered last" yorumu değişir. Şablon değişikliği smoke ister (`bash eng/template-smoke.sh`).
  2. Testler (önce kırmızı): `TenancyFixture`'a bir test izin sağlayıcısı (`notes:notes:write`), `MultiTenancy:Roles:Writer`/`Reader` yapılandırması, POST ucuna `RequirePermission`; `AddTenantAsync` üyeleri `Writer` rolüyle eklesin (F7b.2 testleri yazıyor). `TenancyScenarios`'a: aynı `sub` iki kurumda farklı izin (önce A sonra B, önbellek sızmaz), rol değişikliği ve üyeliğin bitişi sonraki istekte etkili, bilinmeyen rol `tenant.unknown_role`, `TenantsOfAsync` yalnız aktif kurumlar ve rolleri (PG'de `text[]`, SQL Server'da JSON sorguyu sınar). Administrator kuralı: `IdentityServer/` altında kimlik host'u + eklenti (`IdentityServerFixture.Configure`'da `AddCoreMultiTenancy`, store context'i, tablolar `CreateTables`), dönüşüm bir kapsamda `IHttpContextAccessor.HttpContext` + `tenant` route değeriyle çağrılır: 2FA'sız admin + `Writer` üyeliği yalnız Writer'ın izinlerini, 2FA'lı admin bütün tanımlı izinleri alır.
  3. Belgeler: CHANGELOG (resolver birleşimi, roller), D-079 (uygulama kararları: birleşim, internal yapıcı + fabrika, rol doğrulaması), PROGRESS satırı.
- **Bu fazda bilinmesi gerekenler (sürprizler):** EF'in singleton interceptor'ları (`IQueryExpressionInterceptor`) iç servis sağlayıcısının anahtarında: örnek başına sağlayıcı kuruluyor, test sürecinde 20'yi geçince `ManyServiceProvidersCreatedWarning` → tek paylaşılan örnek (R-130, regresyon testi). Framework'ün `NoopClaimsTransformation`'ı `AddAuthentication`'da kaydediliyor, dönüşüm kontrolü onu saymıyor. Eklentide CA2007 bastırması yok, `ConfigureAwait(false)` (D-078 (4)). `TenantStatus` şu an `Active`/`Suspended`; `Closed` ve veri silme F7b.6'da (eklenti o adımda `NovaCore.EntityFrameworkCore`'a da bağlanır, `PackageDependencyTests`'in eklenti kuralı değişir, D-076 (2)).
- **Bekleyen soru:** yok. Q-40 sürüyor (izlenmeyen `.agents/`, `.codex/`, `AGENTS.md`'ye dokunulmuyor).
- **Doğrulanmayan:** şablon smoke'u bu fazda hiç koşmadı (şablon içeriği henüz değişmedi, ama paketler değişti); faz branch'inde CI koşmadı.
- Devam: yeni oturumda `/novacore-phase 7b`.

**2026-10-24, Faz 7a: Veritabanı özgürlüğü (✅ tamam, PR #11 ile `main`'e merge edildi).** Kullanıcı onayı: "PR #11'i merge commit'le main'e al"; merge `cf895d8` (`gh pr merge 11 --merge`), yerel `main` `--ff-only` ile güncellendi. Merge anında `1816cd2`'de `build-and-test` (iki koşu) ve CodeRabbit hâlâ koşuyordu, diğer bütün kontroller (template-smoke, CodeQL, api-contract, secret-scan, workflow-lint) yeşildi; `f8c637f`'ten sonraki commit'ler yalnız belge ve yapılandırma (`CLAUDE.md`, `README.md`, `.coderabbit.yaml`, PROGRESS) ve `f8c637f`'te `build-and-test` yeşildi. main'deki CI sonucu henüz kontrol edilmedi. Branch `faz/7a-veritabani-ozgurlugu`, her adımdan sonra push edildi (Q-59). F7a.1–F7a.5 önceki oturumda; bu oturumda faz sonu: hook mesajı commit'i (kullanıcının değişikliği, onun seçimiyle), doğrulama baştan (Docker'la, 410/410, smoke dört bileşimde), coverage baseline satır %96,6, code review (2 düzeltme, 1 geri alınan, 2 Sonraya bırakılanlar'a) ve PR. Ayrıntı: Faz 7a adımlarının "Faz sonu" satırı.

- **Yapılanlar:** lisans kapısı adlı sağlayıcı istisnasını tam paket adıyla tanıyor, şablonun ürettiği projede de bir kapı var (F7a.1); sağlayıcıyı host seçiyor (`AddModuleDatabase`), modüller sağlayıcıya bağlanmıyor, migration'lar ve design-time factory'ler host'ta (F7a.2); paketler PostgreSQL'de de test ediliyor, aynı senaryolar iki sağlayıcıda (F7a.3); şablonda `--db SqlServer|PostgreSql` (F7a.4); belgeler (F7a.5).
- **Kararlar:** D-072 (lisans istisnası tam adla), D-073 (`AddModuleDatabase` + `AddModuleDbContext(schema, ...)`, migration'lar ve design-time factory'ler host'ta), D-074 (PG test katmanı, ortak senaryolar), D-075 (şablon `--db`); R-125…R-128; Q-59. Faz sonunda yeni D/R kaydı yok.
- **Plandan sapmalar:** şablona compose servisi eklenmedi (D-040 (4), D-075 (4)); repo'nun `NovaCoreApiFactory`'si strip yaklaşımında kaldı (Y-09).
- **Sürprizler:** EF `--namespace` ile snapshot'ı ayrı klasöre yazdı; `.editorconfig` `Migrations/*.cs` alt klasörü eşlemiyor; şablon smoke'u Windows'ta MAX_PATH'e takıldı; PG'de arama büyük/küçük harfe duyarlı; code review düzeltmesi, outbox saklama testinin dispatcher'la yarışta boşa geçebildiğini gösterdi.
- **Kullanıcıya:** (1) PR'ın merge onayı; (2) üretilen projenin CI'ı GitHub'da hiç koşmadı (smoke yerel komutları koşturuyor; Sonraya bırakılanlar'daki Faz 5 maddesi).
- **Sonraki faz (7b) bilmeli:** izolasyon testleri `Databases/` senaryo kalıbıyla iki sağlayıcıda yazılır; model değişikliği hem host'un SQL Server setine hem `tests/NovaCore.Api.Tests/PostgreSql/<Modül>/Migrations`'a migration ister.

**2026-10-23, faz dışı: CRM-Nominal geçişi değerlendirmesi ve plan değişikliği (Faz 7a ve 7b eklendi).** Kod değişmedi. Kullanıcı, CRM-Nominal'i (çok kiracılı eğitim kurumu CRM'i, PostgreSQL) NovaCore üzerinde yeniden kurmak istiyor. İlk analizin (Haiku) NovaCore'dan istekleri Microsoft'un belgeleri ve iki reponun koduyla karşılaştırıldı; kullanıcıya iki tur soru soruldu.

- **Kararlar:**
  - D-068: veritabanı özgürlüğü; Y-06'nın yerine Y-47 (sağlayıcı, projede adlı istisna).
  - D-069: çok kiracılılık NovaCore'a isteğe bağlı bir paketle girer; Y-14'ün yerine Y-48.
  - D-070: CRM NovaCore üzerinde yeniden kurulur (kod değil iş mantığı taşınır); kimlik modeli tek hesap + uygulamada üyelik; `UseCoreWeb`'e kimlik doğrulama ile yetkilendirme arasında middleware yeri eklenmez.
  - D-071: Faz 7a ve 7b; sıra `6d → 7a → 7b → 6e`.
  - Sorular Q-53…Q-58, araştırma R-122…R-124, belge [proposals/crm-cok-kiracililik.md](proposals/crm-cok-kiracililik.md).
- **Bulgular:** İlk analizin üç öncülü tutmadı: middleware kancası gerekmiyor, kurumun yalnız token'dan çözülmesi şart değil, Microsoft kurum başına hesap yerine kişi başına tek hesap öneriyor. Kaçırdığı iki engel var: Npgsql'in lisansı ve ASP.NET Core'un tek bir `IClaimsTransformation` çalıştırması (R-122, R-123).
- **Sonraki faz 7a (`/novacore-phase 7a`) bilmeli:**
  - Şablonda ve repo'nun modüllerinde sağlayıcıyı her modül kendisi seçiyor (`UseSqlServer`).
  - Şablonun ürettiği projede lisans kapısı yok.
  - Y-06'ya canlı atıflar F7a.1'de Y-47'ye taşınır: `ci.yml`, `eng/check-licenses.sh`, README; hook mesajı korumalı. `.coderabbit.yaml`'ın Y-list check'i bu oturumda Y-47'ye çevrildi.
  - Npgsql ve `Testcontainers.PostgreSql` yeni paketler: hook onay ister.
- **Değişmeyen:** Aşağıdaki 6d notundaki "6e bilmeli" maddeleri 6e için geçerli; 6e artık 7b'den sonra.
- **Yapılmayan:** commit.

**2026-10-23, Faz 6d: Analizör ve platform sıkılaştırma (tamam, `main`'e merge ve push edildi).** Kullanıcı onayı: "faz/6d-analizor-sikilastirma'yı main'e --no-ff merge et ve push et"; merge `231b05f` (ağaç faz branch'inin ucu `13b4eed` ile aynı). Branch `faz/6d-analizor-sikilastirma`, her adımdan sonra push edildi; kullanıcı "izinsiz devam et" dedi, plan onayı beklenmeden PROGRESS'e yazıldı. 17 commit. 387/387 yeşil; CI ve CodeQL yeşil.

- **Yapılanlar:** F6d.1a–1d (Security/Reliability/Performance/Usage `latest-all`; CA2007 uygulama kodunda ve ASP.NET Core paketlerinde kapalı, EF Core paketi `ConfigureAwait(false)`; CA1812 kapalı; testlerde CA2000/CA2025/CA1849/CA2234/CA2213 düzeltmeleri; `Result<T>`'nin örtük operatörleri kalktı), F6d.2a–2c (IDE0051/52/59/60, IDE0005 paketlerde, CA1502/1505/1506; CI'da `dotnet format --verify-no-changes`, `end_of_line` kalktı, `.gitattributes` `* text=auto`; şablona yerleşik analizör ayarları), F6d.3a–3c (BannedApiAnalyzers, Meziantou.Analyzer, Roslynator haftalık rapor), F6d.4a–4c (Sonar CI job'u token'la, Qodana deneme koşusu, CodeRabbit kurulum notu), F6d.5 atlandı (kararlı sürüm yok), F6d.6 (Ponytail'dan üç kural). Kararlar D-064…D-066, araştırma R-117…R-120, kullanıcı kararları Q-48…Q-50.
- **Bulgular ve sürprizler:** PLAN'daki `AnalysisMode<Kategori>` bileşik `AnalysisLevel`'ın yanında etkisizdi (R-117); CA2225 ile CA1000 generic `Result<T>`'de çelişiyor (Q-48); Linux checkout'u `end_of_line = crlf` yüzünden her dosyada format hatası verirdi, Windows'ta `autocrlf=false` de (R-118); PublicApiAnalyzers 5.6.0 PublicAPI dosyalarını kendisi ekliyor (R-037 eskimiş); şablonun ürettiği projeler using sırası yüzünden format-temiz değil (format adımı şablona eklenmedi); Meziantou'nun code fixer'ı üç satır kırılımını bozdu; `package-guard.js` `GlobalPackageReference`'ı görmüyor; Qodana .NET 10'u `bootstrap` ile analiz ediyor ama gürültülü (CI'a eklenmedi).
- **Plandan sapmalar:** `AnalysisLevel<Kategori>` (`AnalysisMode` yerine); `CodeMetricsConfig.txt` eklenmedi (eşik değişmiyor); şablonun CI'ına format adımı eklenmedi; csharp-lsp eklentisi etkinleştirilemedi (auto-mode sınıflandırıcısı `.claude/settings.json` ve CLAUDE.md'deki eklenti notu düzenlemesini reddetti).
- **Kullanıcıya:** (1) ~~merge onayı~~ verildi; (2) ~~SonarQube Cloud kurulumu~~ yapıldı: `SONAR_TOKEN` eklendi, ilk koşu yeşil, uyarıları Sonraya bırakılanlar'da; (3) ~~CodeRabbit kurulumu~~ yapıldı, ama ilk koşu (PR #9) Dependabot'un review seat'i olmadığı için inceleme yapmadı (Sonraya bırakılanlar); tamamı F6d.4d'de (D-067): `coderabbit auth login` ve `dependabot[bot]`'a seat ataması kullanıcıda; (4) csharp-lsp: `csharp-ls` kurulu, eklentiyi `/plugin` ile etkinleştir (istersen CLAUDE.md'ye not ekle); (5) `package-guard.js`'e `GlobalPackageReference` kontrolü; (6) Qodana imajı yerel diskte (`docker rmi jetbrains/qodana-cdnet:2026.2` ile silinebilir); (7) 6a'dan kalanlar hâlâ açık (Dependabot PR'ları, paketleri public yapmak, `BMD.*` önek rezervasyonu), 6c'den `Payment.Amount` kararı ve `mutation.yml`'in elle tetiklenmesi.
- **Merge sonrası doğrulandı (2026-10-23, `ee7bcd5`):**
  - main'de CI (build-and-test, api-contract, template-smoke, secret-scan, workflow-lint, sonar-token; `sonar` token olmadığı için atlandı), CodeQL ve Scorecard yeşil.
  - Coverage main'de de satır %95,7 / dal %82,8 (baseline %95,8 / %82,9, tolerans içinde). `api-contract` kırılım bulmadı.
  - main'de açık CodeQL uyarısı yok. Açık 6 code scanning uyarısı Scorecard'ın 6a'dan beri bilinen bulguları.
  - `unused-code.yml` ve `mutation.yml` artık varsayılan dalda; `workflow_dispatch` ile elle tetiklenebilirler (kullanıcıda).
- **Sonraki faz (6e) bilmeli:** yeni analizörler her build'de (Meziantou varsayılan önemleriyle); test kodunda `Uri`, `Ordinal`, regex timeout zorunlu (tests/CLAUDE.md). Sonar bulguları token gelince CI'da görünecek. DECISIONS hâlâ hook korumasında değil (D-032, 6e sonu).

**2026-10-23, Faz 6c: Test altyapısı (tamam, `main`'e merge ve push edildi).** Kullanıcı onayı: "faz/6c-test-altyapisi'yi main'e --no-ff merge et ve push et"; merge `64b3ab6`. Branch `faz/6c-test-altyapisi`, her adımdan sonra push edildi (Q-44); 16 commit. Son kod commit'i `fa2da44`, kapanış `85d532a` sonrası. 389/389 yeşil; CI ve CodeQL yeşil.

- **Yapılanlar:** F6c.1a–1c (MTP + xUnit 4.0.2, şablon da; R-055'in teorisi doğrudan başlatılan host'ta), F6c.2a–2d (Testcontainers SQL Server katmanı, gerçek migration'lar ve EXEC'li veri adımları, `GET /payments`, şema, collation, `decimal(18,2)`; ilk TOTP INSERT yarışı düzeltildi; yarış testleri deterministik), F6c.3 (outbox saklama silmesi SQL'de; SQLite test bağlamları `DateTimeOffset`'i tick saklıyor; şablon da), F6c.4 (coverage ratchet), F6c.5 (commit'li OpenAPI belgesi + oasdiff), F6c.6 (CsCheck property testleri), F6c.7 (Stryker haftalık, `tests/NovaCore.Domain.Tests`). Kararlar D-056…D-063, araştırma R-108…R-116. Testler 361 → 389.
- **Bulgular (testlerin bulduğu gerçek hatalar):** SQL Server fiyatı sessizce yuvarlıyordu (0.001 → 0.00; Q-47 ile 400); ilk TOTP kaydında yarışı kaybeden istek 500 alıyordu (D-058); `PaginationRequest.Skip` büyük sayfada taşıyordu (D-062); mutation raporu property üreteçlerinin sınırları kaçırdığını gösterdi.
- **Plandan sapmalar:** PLAN.md'nin `dotnet test` kuralı değişmedi (konumsal `.slnx` çalışıyor); R-055 ölçümü 1 koşuda durduruldu (upstream kaynağı yeterli); OpenAPI belgesi build'de değil test host'unda üretiliyor (.NET 10'un build zamanı üretimi Program'ı Production'da çalıştırıyor, D-061); outbox'ta `Id` parçaları da kalktı (index zaten varmış); saf testler yeni bir test projesine taşındı (Stryker, D-063).
- **Sürprizler:** `-nologo` MTP modunda exit 5; coverlet DLL'leri yerinde enstrümante ediyor (mimari testler ayrı adımda); XML yorumundaki `--` bütün CPM'i bozdu; `template-smoke.sh`'tan sonra `--no-build` test, `obj` başka önbelleği gösterdiği için "VSTest" diyor (önce `dotnet build`); Docker Desktop yeniden başlayınca Ryuk bir kez düştü; Buildalyzer bu makinede VS 18 Insiders'ın MSBuild'ini kullanıyor.
- **Kullanıcıya:** (1) merge onayı; (2) `Payment.Amount` taşması için karar (Sonraya bırakılanlar, code review #1); (3) merge'den sonra `mutation.yml` bir kez elle tetiklenmeli (`workflow_dispatch` varsayılan dal ister); (4) 6a'dan kalanlar hâlâ açık (Dependabot PR'ları, paketleri public yapmak, `BMD.*` önek rezervasyonu).
- **Merge sonrası doğrulandı (2026-10-23, `36902b4`):**
  - main'de CI (build-and-test, api-contract, template-smoke, secret-scan, workflow-lint), CodeQL ve Scorecard yeşil.
  - Coverage main'de de satır %95,8 / dal %82,9 (= baseline). `api-contract` main'de ilk kez gerçekten karşılaştırdı (`64b3ab6`'ya karşı): değişiklik yok.
  - main'de açık CodeQL uyarısı yok. Açık 6 code scanning uyarısı Scorecard'ın 6a'dan beri bilinen bulguları (CII-Best-Practices, Code-Review, Maintained, Fuzzing, Security-Policy, Branch-Protection).
  - `mutation.yml` henüz tetiklenmedi (kullanıcıda).
- **Sonraki faz (6d) bilmeli:** coverage kapısı ve mutation raporu hazır (D-048'in "ölçüm araçları devrede" ön koşulu). Coverage baseline %95,8 / %82,9; analizör sıkılaştırması kod değiştirirse `eng/check-coverage.sh --update`. API belgesi değişirse `OpenApiContractTests` dosyayı yazar. DECISIONS hâlâ hook korumasında değil (D-032, 6e sonu).

**2026-10-22, Faz 6b: Kimlik güvenlik sınırı (tamam, `main`'e merge ve push edildi).** Kullanıcı onayı: "faz/6b-kimlik-guvenlik-siniri'yi main'e --no-ff merge et ve push et"; merge `4909fe4`. Branch `faz/6b-kimlik-guvenlik-siniri`, her adımdan sonra push edildi (Q-43). Son kod commit'i `15520c1`; 361/361 yeşil; branch'te CI (build-and-test, template-smoke, secret-scan, workflow-lint) ve CodeQL yeşil, branch'te açık CodeQL uyarısı yok.

- **Yapılanlar:** F6b.1a–1d (tek kullanımlık TOTP, hash'li kurtarma kodları, 2FA uçlarında parola, Administrator için 2FA), F6b.2 (Administrator'ı yalnız `identity:roles:manage`, son 2FA'lı admin korunur, rol değişikliği önbelleği hemen düşürür), F6b.3a–3b (`Identity:Seed:UserPermissions`, ödeme sahibi + `payments:read-all`), F6b.4a–4e (`at+jwt`, CORS dışı authorize/`/account`, `http` redirect, 32 karakterlik secret, mutlak refresh ömrü), F6b.5 (15 karakter + SecLists blocklist), F6b.6a–6d (e-posta kuyruğu, Secure cookie'ler, revoke limiti ölçüldü, CodeQL), F6b.7 (18 negatif protokol testi). Kararlar D-053 (plan), D-054 (uygulama), D-055 (code review); araştırma R-099…R-107. Testler 278 → 361.
- **Sürprizler:**
  - Api host'u eklentiyi çalıştırdığı için `at+jwt` zorunluluğu test issuer'ının `typ: JWT` token'larıyla 31 testi kırdı; repo ve şablon test issuer'ları `at+jwt` üretiyor.
  - `IdentityServerTestHost` test çıktısındaki Api `appsettings` dosyalarını okuyordu; JSON kaynakları düşürüldü.
  - Antiforgery `Always` düz http sayfasında 500 veriyor (R-107); Development'ta `SameAsRequest`.
  - OpenIddict'in refresh ömrü varsayılan olarak kayan (R-106); mutlak yapıldı.
  - Zamanlama açığı ölçüldü (R-105); Microsoft'un kendi Identity kodu ve Keycloak aynı açığı taşıyor, gerekçe OWASP/ASVS L3. Kullanıcı kuyruğu seçti, Y-18'in kapsamına girmediğine karar verdi.
  - CI secret taraması bir test parolasını yakaladı (`1f018a0`).
- **Kullanıcıyla alınan kararlar:** önceki oturumun commit'lenmemiş F6b.1a testleri atıldı ("At, baştan yaz"); blocklist indirmesi onaylandı; F6b.6a için asenkron gönderim; CodeQL #3 ve #4 GitHub'da false positive olarak kapatıldı.
- **Kullanıcıya:**
  - Kırıcı değişiklikler CHANGELOG'da (2FA uçlarının gövdesi, 15 karakter, eski kurtarma kodları, `User` rol listesi, admin için 2FA). Seed admin'in izinleri 2FA açılınca gelir.
  - 6a'dan kalanlar hâlâ açık: Dependabot PR'ları, paketleri public yapmak, `BMD.*` önek rezervasyonu.
- **Merge sonrası doğrulandı (2026-10-22, `4909fe4` ve `0c651bc`):**
  - main'de CI (build-and-test, template-smoke, secret-scan, workflow-lint) ve CodeQL yeşil.
  - Scorecard iki push'ta da GitHub'ın GraphQL API'sindeki bir iç hatayla düştü (`ListCommits ... Something went wrong while executing your query`, kodla ilgisiz); `0c651bc`'nin koşusu yeniden başlatıldı ve yeşil.
  - CodeQL: #1 ve #2 (`cs/exposure-of-sensitive-information`) main taramasında `fixed`, #3 ve #4 `dismissed` (false positive). main'de açık CodeQL uyarısı yok.
- **Sonraki faz (6c) bilmeli:** Sonraya bırakılanlar'da 6c'ye işaretli üç madde var: `GET /api/v1/payments`'in uçtan uca testi (SQLite sıralaması), ilk TOTP kaydındaki INSERT yarışı, yarışı zorlamayan 2FA testi. `PaymentOwner` migration'ı gerçek SQL Server'da sınanmalı. DECISIONS hâlâ hook korumasında değil (D-032, 6e sonu).

**2026-10-21, Faz 6a: Public geçiş ve CI/tedarik zinciri sertleştirme (tamam, `main`'e merge ve push edildi).** Branch `faz/6a-ci-sertlestirme`; kullanıcı onayıyla `--no-ff` merge (`49d341d`) ve push.

- **Yapılanlar:**
  - F6a.1: K1–K8 kararları D-042…D-050 olarak, Faz 6a–6e PLAN'a. Paketler public olacak, token gerekliliği sürüyor (D-050).
  - F6a.2–F6a.7: action'lar SHA'ya sabit, en az izin, actionlint + zizmor, lock dosyaları + SDK sabit, lisans kapısı, Betterleaks, CodeQL, Scorecard, release'in build/publish ayrımı + SBOM + provenance.
  - F6a.8–F6a.12: compose `127.0.0.1` + digest, Dependabot + `WorkflowPinTests`, hook genişletmesi, GitHub ayarları (secret scanning, push protection, Dependabot, ruleset'ler, `release` environment), dokümanlar.
  - Kararlar: D-051 (1)–(8), D-052 (Q-39). Araştırma: R-093…R-098. Testler 277 → 278.
- **Sürprizler:**
  - Lisans kapısı Polly'yi (BSD-3-Clause, Http.Resilience üzerinden) buldu → Q-39.
  - CodeQL ilk koşuda kimlik eklentisinde 4 uyarı verdi → Sonraya bırakılanlar (6b girdisi).
  - Plan notuna yazılan bir test değeri CI'ın secret taramasını kırdı.
  - R-055'teki aralıklı test hatası CI'da da görüldü.
  - zizmor-action bulguda başarısız olmuyor; doğrudan imaj kullanıldı.
- **Kullanıcıya:**
  - Merge ve push yapıldı (kullanıcı onayı: "faz/6a-ci-sertlestirme'yi main'e --no-ff merge et ve push et"). `main` artık ruleset'li (force push ve silme kapalı); `--no-ff` merge + push akışı değişmedi.
  - 8 paketi public yap (D-050): her paketin sayfası → Package settings → Change visibility. Geri alınamaz.
  - nuget.org'da `BMD.*` önek rezervasyonu (D-048 (1)).
  - İstersen bir `v0.1.0-preview.2` tag'i: yeni release akışı (onaylı `publish`, provenance, SBOM) ilk kez gerçekten koşar.
- **Merge sonrası doğrulandı (2026-10-21, `3e0dd8b`):**
  - main'de CI, CodeQL ve Scorecard yeşil.
  - Scorecard'ın açık uyarıları: CII-Best-Practices 0, Code-Review 0 (PR'sız akış), Maintained 0 (repo 90 günden yeni), Fuzzing 0, Security-Policy 0 (SECURITY.md yok), Branch-Protection 3. Pinned-Dependencies ve Token-Permissions uyarı vermedi.
  - Dependabot'un ilk koşusu dört ekosistemde çalıştı.
    - `docker-compose` iki compose dosyasını da okudu (#1 Keycloak 26.8.1, #2 Jaeger 2.22.0). mcr.microsoft.com ve quay.io yayın tarihi vermediği için bu imajlarda cooldown uygulanmıyor (log: "skipping cooldown").
    - `dotnet-sdk`: #3 10.0.300 → 10.0.402. Merge edilirse herkes 10.0.402'yi kurmalı (`rollForward: disable`).
    - `nuget`: #4 Microsoft grubu, 16 güncelleme. `Directory.Packages.props` ve dört lock dosyasını birlikte güncelledi; şablon içeriğine dokunmadı (`exclude-paths` NuGet'te çalışıyor).
    - `github-actions`: PR yok (pin'ler güncel).
  - PR'lar kullanıcının kararını bekliyor; hiçbiri merge edilmedi.
- **Sonraki faz (6b) bilmeli:** CodeQL'in 4 uyarısı 6b'nin girdisi. Plan notlarına test değerlerini `anahtar=değer` biçiminde yazma. DECISIONS hâlâ hook korumasında değil (D-032, artık 6e sonu).

## Kapanmış "Sonraya bırakılanlar" maddeleri

- ~~Forwarded headers yok (Faz 0) ve proxy arkasında cookie'lerin `Secure` bayrağı (Faz 3b) → F4c.3.~~ → F4c.3'te yapıldı.
- ~~Y-31 testinin `IResult` kör noktası (Faz 0, F0.7) → F4c.5.~~ → F4c.5'te yapıldı.
- ~~`BadHttpRequestException`'ın 500 dönmesi (Faz 2b) → F4c.4.~~ → F4c.4'te yapıldı.
- ~~Super-admin bypass'ının token rolüne dayanması (Faz 3a) → F4c.2.~~ → F4c.2'de yapıldı.
- ~~`outbox_consumers` anahtarı (Faz 4a, #12) ve işlenmiş mesajların hiç silinmemesi (Faz 4a) → F4c.7.~~ → F4c.7'de yapıldı.
- ~~Parola sıfırlama, onay e-postasını yeniden gönderme, `User` rolü yokken kayıtta 500 (Faz 3b) → F4d.2.~~ → F4d.2'de yapıldı.
- ~~Kurtarma kodları (Faz 3b) → F4d.3.~~ → F4d.3'te yapıldı.
- ~~Logout CSRF, giriş zamanlama farkı, `change-password` lockout'u (Faz 3b) → F4d.4.~~ → F4d.4'te yapıldı.
- ~~OpenIddict kayıtlarının temizlenmemesi (Faz 3b) → F4d.5.~~ → F4d.5'te yapıldı.
- ~~Template'in paket kimliğini taşıması (Faz 1, F5.1) → F5.1.~~ → F5.1b'de yapıldı: şablon ayrı bir template package, repo'ya özgü dosya taşımıyor.
- ~~Catalog/Payments uçtan uca testleri (Faz 2c) → F5.3 (4b'de).~~ → F5.3'te yapıldı (Faz 4b).
- ~~`UseCoreWeb`'e kimlik doğrulama ile yetkilendirme arasında bir middleware yeri. *Koşul:* bir proje yetkilendirmeden önce kimliğe bağlı bir middleware'e (ör. tenant çözümü) ihtiyaç duyar. Bugün proje kendi middleware'ini `UseCoreWeb`'den sonra ekleyebiliyor.~~ → Koşul 2026-10-23'te ilk tüketiciyle (CRM-Nominal) incelendi ve **tutmadı**: izin çözümlemesi kimlik doğrulamanın içinde çalışıyor (`IClaimsTransformation`), uygulamanın middleware'i `UseCoreWeb`'den sonra endpoint'ten önce çalışıyor, askıdaki kurumun kontrolü kurum çözümüne giriyor. Eklenmeyecek (D-070 (5), R-122).
- ~~(Faz 0) Global ve "strict" rate limiter partition anahtarı olarak `RemoteIpAddress` kullanıyor. Bir reverse proxy arkasında bu proxy'nin IP'si olur. `ForwardedHeaders` yapılandırılmamış.~~ → F4c.3'te çözüldü: güvenilen proxy'nin `X-Forwarded-For`'u `RemoteIpAddress`'i istemcinin adresi yapıyor.
- ~~(Faz 0, F0.7) Y-31 testi ApiExplorer'a dayanıyor. `Task<IResult>` dönen ve `Produces<T>` bildirmeyen handler'ların yanıt payload'ını göremez (Identity uç noktalarının çoğu böyle).~~ → F4c.5'te çözüldü: belgede görünen bütün handler'lar tipli sonuç dönüyor ve `EndpointContractTests` bunu doğruluyor. ApiExplorer dışı uçlar (health, `ExcludeFromDescription`) hâlâ kapsam dışı.
- ~~(Faz 0, F0.6)~~ → F5.5'te düzeltildi (kök CLAUDE.md ve README Y-01'e yönlendiriyor). Kök `CLAUDE.md` Standards bölümünde "If a mediator is ever truly needed, use MIT `martinothamar/Mediator`" yazıyor. Bu, DECISIONS Y-01 ile çelişiyor ("herhangi bir mediator veya request-dispatch soyutlaması" yasak). Öncelik DECISIONS'ta, ama not gelecekteki oturumları yanıltabilir. Cümle kaldırılmalı veya Y-01'e yönlendirilmeli.
- ~~(Faz 0) "strict" policy'nin tek tüketicisi Identity'nin `/api/v1/auth` grubu.~~ → Faz 3b'de değerlendirildi: policy'yi artık eklentinin `/connect/token`, `/account/*` ve `/api/v1/auth/*` uçları kullanıyor, `StrictRateLimitTests` `/connect/token`'ı sınıyor (D-009'un anlamı korundu).
- ~~(Faz 1, F2a.2) **Yeni packable projeler PublicAPI takibini sessizce atlayabilir.**~~ → F2a.2a'da çözüldü: `Directory.Build.targets`'taki `NovaCoreRequirePublicApiTracking` hedefi, packable bir projede referans yoksa build'i durduruyor (D-020 (7)). Eski not: `AdditionalFiles` ve doc koruması `Directory.Build.targets`'ta, ama `Microsoft.CodeAnalysis.PublicApiAnalyzers` referansı her csproj'da ayrı (PLAN F1.4 böyle istiyordu). F2a.2'deki 3 yeni pakete referans eklenmeli. Alternatif: referansı targets'a taşımak ya da bir koruma eklemek; bu PLAN'dan farklı bir yaklaşım olduğu için sorulmalı (code review bulgusu).
- ~~(Faz 1) **Stop hook değişiklik algılaması eksik:** `verify-on-stop.js` kök `Directory.Build.targets`'ı ve `*.txt` dosyalarını (PublicAPI) izlemiyor. Yalnızca bunlar değişirse build/test doğrulaması çalışmaz. Dosya korunan bir yolda, düzeltmek kullanıcı onayı ister (code review bulgusu).~~ → F6a.10'da düzeltildi.
- ~~(Faz 1, F5.1)~~ → F5.1b'de çözüldü. **Template paket kimliğini taşıyor:** `dotnet new novacore`, `Directory.Build.targets`'ı (Authors = Muhammed Ali AK), `src/PACKAGE.md`'yi, `release.yml`'i ve PublicAPI dosyalarını kopyalıyor. `sourceName` değişimi yüzünden RepositoryUrl `github.com/alimuhammedak/<YeniAd>` oluyor (code review bulgusu).
- ~~(Faz 1, F2a.2) Serilog log kültürü:~~ → F2a.2a'da karar verildi: `null` korunuyor, 2a'da davranış değişmiyor (D-020 (8)). Eski not: `formatProvider: null` Serilog'un varsayılanı. Logların culture-invariant olup olmayacağına NovaCore.Serilog'da karar verilmeli.
- ~~(Faz 2a, F2a.5)~~ → F5.5'te düzeltildi. Kök CLAUDE.md Standards'taki "0 warnings" maddesi bastırma için hâlâ "DECISIONS Y-10" diyor. Y-10'un yerini Y-44 aldı (D-016), ifade de değişti: yalnızca MS'in "When to suppress" iznine dayanan kalıcı bastırma. Tek kelimelik ama bir kuralı değiştiriyor, 2a kapsamı dışı.
- ~~(Faz 2a, code review)~~ → Faz 3b'de çözüldü: eklentinin logger'ında `LoginFailed` da guard'lı. Eski not: `SecurityAuditLogger.LoginFailed` maskeyi guard'sız hesaplıyor, `UserRegistered` ise guard'lı. CA1873 varsayılan olarak yalnızca Information ve altına bakıyor (R-047). Davranış F2b.6'dan önceki gibi. Identity F3b.3'te emekliye ayrılacak.
- ~~(Faz 1) `NovaCore.Core.Web` paketinin nuspec bağımlılıklarında FluentValidation, Serilog, OpenTelemetry ve Scalar var (D-003 ihlali). F2a.2 ve F2c'nin işi.~~ → F2a.2'de çözüldü (Serilog/OTel/Scalar eklentilere, FluentValidation `NovaCore.FluentValidation`'a taşındı), F2a.4 testleri koruyor.
- ~~(Faz 2a, F2a.4 → F2c.1) **Y-20'nin modül kısmı henüz testli değil.**~~ → F2c.1e'de çözüldü: `ExplicitRegistrationTests` artık tüm `src`'yi tarıyor ve `*FromAssembly*` kayıt yardımcılarını da yakalıyor. EF'in `ApplyConfigurationsFromAssembly`'si model yapılandırması olduğu için hariç (D-023 (5)). Eski not: Modüller validator'ları `AddValidatorsFromAssembly` ile (assembly taraması) kaydediyor. Bu FluentValidation ile birlikte F2c.1'de gidiyor; o zaman `ExplicitRegistrationTests`'in taraması `src/Modules`'ü de kapsamalı. `ApplyConfigurationsFromAssembly` (EF model yapılandırması, DI kaydı değil) Y-20 kapsamında mı, o gün karar verilmeli.
- ~~(Faz 2b, code review) `GlobalExceptionHandler.Map`'te `BadHttpRequestException` için bir durum yok.~~ → F4c.4'te çözüldü: kendi durum kodunu dönüyor. Development'ta minimal API'ler bozuk gövdede bu exception'ı fırlatıyor (`ThrowOnBadRequest`), yanıt 400 yerine 500 oluyor ve Error seviyesinde log düşüyor. Bu 2b'den önce de böyleydi; status eşlemesini değiştirmek F2b.4'ün kapsamı dışında. Çözüm: `BadHttpRequestException.StatusCode`'u kullanmak.
- ~~(Faz 2c)~~ → F5.5'te düzeltildi. README "Paket / Lisans Tablosu"ndaki `Microsoft.EntityFrameworkCore / Extensions.Hosting.Abstractions / Configuration.Binder` satırı eskimiş. EF paketi F2b'den beri Binder yerine `Options.ConfigurationExtensions` ve `Diagnostics.HealthChecks.EntityFrameworkCore` kullanıyor. F2c.4 aynı tabloda yalnızca FluentValidation satırını sildi.
- ~~(Faz 2c, code review → F5.3)~~ → Faz 4b'de çözüldü: `CatalogEndpointsTests` (ürün oluştur, oku, listele, güncelle, sil) ve `StockReservationTests` (POST'tan Completed'a ödeme). Eski not: Catalog ve Payments için geçerli gövdeyle uçtan uca test yok. `ValidationTests` yalnızca reddedilen gövdeleri sabitliyor, `PermissionAuthorizationTests`'teki POST 403'te duruyor. Geçerli girdiyi yanlışlıkla reddeden bir attribute değişikliği yakalanmaz. F5.3'ün (Catalog/Payments entegrasyon testleri) kapsamına giriyor.
- ~~(Faz 2c)~~ → F5.5'te düzeltildi. Y-10'un yerini Y-44 aldı, ama iki yer hâlâ Y-10 diyor: `tests/CLAUDE.md` ("never weaken one to pass (Y-10)") ve `Directory.Build.props`'taki `TreatWarningsAsErrors` yorumu. Kök CLAUDE.md'deki aynı not yukarıda (Faz 2a, F2a.5).
- ~~(Faz 3a → F3b.3)~~ → F3b.3b'de çözüldü (D-028): üretim kodu ve Development simetrik modu kaldırıldı. Eski not: Çekirdekteki `ITokenService`/`JwtTokenService`/`AccessToken` ve `JwtOptions`'ın issuance alanlarının (`AccessTokenMinutes`, `RefreshTokenDays`) tek tüketicisi Identity modülü (D-024 (3)). Modülle birlikte kaldırılmalı. O gün HS256 üreten bir issuer kalmayacak; Development'taki simetrik doğrulamanın (`Jwt:SigningKey`) kalıp kalmayacağına karar verilmeli (ör. `dotnet user-jwts` için).
- ~~(Faz 3a, code review → 3b; **3b'de yapılmadı**, plan kapsamı dışında bırakıldı) **Super-admin bypass'ı token'daki rol claim'ine dayanıyor.**~~ → F4c.2'de çözüldü (D-036 (2)). `Administrator` rolünden çıkarılan bir kullanıcı, token'ın ömrü boyunca (Identity'de 15 dk) her izni geçmeye devam ediyor. İzinler ise cache TTL'iyle yansıyor. IdP 3b'de gelirken super-admin de sunucu tarafında çözülmeli (resolver'dan dönen bir işaret ya da rol).
- ~~(Faz 3a, code review)~~ → F3b.3b'de çözüldü: `Jwt:SigningKey` yok (D-028). Eski not: Development'ta hem `Jwt:Authority` hem `Jwt:SigningKey` varsa JwtBearer issuer'ları ve anahtarları bağsız birleştiriyor (R-056): Authority'nin issuer'ını taşıyan ama HS256 geliştirme anahtarıyla imzalanmış bir token kabul ediliyor. Yalnızca Development. Simetrik mod 3b'de yeniden değerlendirilince (yukarıdaki madde) birlikte ele alınmalı.
- ~~(Faz 3a → 3b) Keycloak rolleri~~ → F3b.4'te örnek realm'deki mapper rolleri `role` claim'ine yazıyor, F3b.5a'da OpenAPI belgesi OAuth2 akışını yayımlıyor. Kalan kısım (Keycloak için permission resolver) aşağıda, Faz 3b maddelerinde. Eski not: Keycloak rolleri düz `role` claim'i olarak değil `realm_access.roles` içinde geliyor. Super-admin kontrolü ve bir Keycloak resolver'ı bunları eşlemeli. OpenAPI belgesi hâlâ düz Bearer şeması yayımlıyor; Scalar için OAuth2/OIDC akışı F3b.5'in işi.
- ~~(Faz 3b, plan kapsamı dışı) Parola sıfırlama yok (eski modülde de yoktu). Onaylanmamış hesaba yeniden onay e-postası gönderme ucu da yok.~~ → F4d.2'de yapıldı.
- ~~(Faz 3b, plan kapsamı dışı) OpenIddict token ve authorization kayıtları hiç temizlenmiyor (Quartz entegrasyonu ya da `PruneAsync` çağıran bir hosted service). Tablolar zamanla büyür.~~ → F4d.5'te çözüldü (`ExpiredTokenPruner`).
- ~~(Faz 3b, code review) Giriş sayfasında zamanlama farkı: bilinmeyen e-posta PBKDF2'siz hemen dönüyor, bilinen e-posta 600k iterasyon harcıyor. Aynı mesaja rağmen yanıt süresi hesabın varlığını ele veriyor (eski modülde de vardı). Çözüm: bilinmeyen kullanıcıda sahte bir hash doğrulamak. Kayıt ucunda da benzer bir fark var (yeni hesapta hash + e-posta).~~ → F4d.4'te giriş sayfası için çözüldü (sahte hash). Kayıt ucundaki ve parola sıfırlama formundaki fark kalıyor (aşağıdaki F4d.2 maddesi).
- ~~(Faz 3b, code review) Kurtarma kodları kullanılamıyor: `/2fa/enable` 10 kod veriyor ama giriş sayfasının kod adımı yalnızca authenticator kodunu kabul ediyor (`TwoFactorRecoveryCodeSignInAsync` çağrılmıyor). Eski modülde de böyleydi.~~ → F4d.3'te çözüldü.
- ~~(Faz 3b, code review) `change-password`'da yanlış mevcut parola lockout sayacını artırmıyor. Çalınmış bir access token'la yalnızca IP rate limit'iyle sınırlı bir parola denemesi yapılabiliyor (eski modülde de vardı).~~ → F4d.4'te çözüldü.
- ~~(Faz 3b, code review) `register`, `User` rolü yoksa (seed kapalıyken, Development varsayılanı) kullanıcıyı oluşturduktan sonra 500 veriyor. Onay e-postası gitmiyor, tekrar kayıt genel 200'le dönüyor; adres takılı kalıyor (eski modülde de vardı).~~ → F4d.2'de çözüldü: rol yoksa hesap rolsüz oluşuyor; takılan adres için yeniden gönderme ucu var.
- ~~(Faz 3b, code review) `/connect/endsession`, `id_token_hint` ya da onay adımı olmadan GET'le oturumu kapatıyor (logout CSRF). OpenIddict örnekleri bir onay formu gösteriyor.~~ → F4d.4'te çözüldü.
- ~~(Faz 3b, code review; **F4c.3'te kısmen çözüldü**: güvenilen proxy yapılandırılınca istek şeması https olur ve `SameAsRequest` cookie'ler `Secure` alır) `SecurePolicy = Always` yalnızca uygulama cookie'sinde. 2FA kullanıcı cookie'si ve antiforgery cookie'si varsayılanda. Forwarded headers'sız bir TLS proxy'si arkasında `Secure` bayrağı olmadan çıkıyorlar (yukarıdaki Faz 0 `ForwardedHeaders` maddesiyle ilgili).~~ → F6b.6b'de çözüldü: 2FA cookie'leri `Always`, antiforgery Development dışında `Always`.
- ~~(Faz 4a, plan kapsamı dışı) Outbox ve `outbox_consumers` satırları hiç silinmiyor.~~ → F4c.7'de çözüldü: işlenmiş mesajlar 7 gün sonra kayıtlarıyla siliniyor (OpenIddict kayıtları F4d.5'te). MS Idempotent Consumer tekrar teslim penceresinden uzun bir saklama süresi öneriyor. İşlenmiş mesajları ve kayıtlarını belli bir süre sonra silen bir temizlik gerekecek (OpenIddict kayıtları için olan maddeyle birlikte ele alınabilir).
- ~~(Faz 4a) `IDomainEventDispatcher`'ın `src`'de çağıranı yok.~~ → F4c.7'de kaldırıldı. PLAN F4a.3 gereği düzeltildi ve korunuyor, ama entity event'leri outbox'tan dağıtılıyor. Faz 5'te (ya da kullanıcı kararıyla) kaldırılması değerlendirilebilir.
- ~~(Faz 4a, code review #12) `outbox_consumers`'ın clustered anahtarı (uniqueidentifier, nvarchar(256)) geniş ve SQL Server sırasında rastgele.~~ → F4c.7'de çözüldü (D-036 (7)). Outbox tablosu için R-068'de kullanılan gerekçenin aynısı geçerli. Mesaj satırının `bigint Id`'si + Handler (FK ve cascade delete ile, temizlik için de uygun) daha iyi olur. D-031 (5)'i değiştirir; tablo boşken, ilk kullanımdan önce yapmak ucuz.
- ~~(Faz 6b, F6b.3b → Faz 6c) `GET /api/v1/payments` uçtan uca test edilmedi: `CreatedAtUtc`'ye göre sıralama SQLite'ta çevrilemiyor (R-068). Filtre `GetPayments.VisibleTo` ile sorgu düzeyinde test edildi. *Koşul:* 6c'nin SQL Server katmanı geldiğinde sahiplik filtresi ve sıralama HTTP üzerinden sınanır.~~ → F6c.2b'de yapıldı (`SqlServerQueryTests`).
- ~~(Faz 6b code review #2 → Faz 6c) İlk TOTP doğrulamasında `[NovaCore]/UsedAuthenticatorCodes` satırı INSERT edilir. Aynı kodla yarışan iki ilk doğrulama (2FA'yı açan iki eşzamanlı istek ya da 0.1.0-preview.1'de 2FA açmış bir kullanıcının ilk iki eşzamanlı girişi) aynı anahtarı ekleyebilir; EF komut sırası (`AspNetUserTokens` < `AspNetUsers`) INSERT'ü önce koşarsa kaybeden istek `DbUpdateException` ile 500 alır. Güvenlik etkisi yok (ikinci kullanım yine geçmez). *Koşul:* 6c'nin SQL Server katmanı; düzeltme satırı `/2fa/setup`'ta önceden oluşturmak ya da store'da benzersizlik ihlalini `ConcurrencyFailure`'a çevirmek, testi gerçek SQL Server'da.~~ → F6c.2c'de çözüldü (D-058).
- ~~(Faz 6b code review #8 → Faz 6c) `Two_sign_ins_racing_with_one_code_let_one_in` iki isteği `Task.WhenAll` ile gönderiyor ama okuma/yazma sırasını zorlamıyor; `ConcurrencyStamp` kontrolü bozulsa bile çoğu koşuda sıralı yol çalışıp test geçer. *Koşul:* 6c'de kayıttan hemen önce araya giren bir interceptor (Catalog'daki `StockRaceApiFactory` gibi) ile deterministik hale getirilir.~~ → F6c.2d'de yapıldı (`UsedCodeRace`).
- (Faz 1) ~~F2b.6'da yapılacak: `.editorconfig`'teki geçici CA1848/CA1873 satırları silinecek.~~ → F2b.6 Faz 2a'nın ilk adımı oldu (D-017), Faz 2a adım listesinde.
- ~~(Faz 7a, PR #11 CodeRabbit) Repo'nun `NovaCoreApiFactory`'si context kayıtlarını silip SQLite'la yeniden ekliyor (`ReplaceWithSqlite`); şablonun fabrikası gibi `AddModuleDatabase` ile değiştirmek varsayılan entegrasyon testlerini de `AddModuleDbContext`'in yolundan geçirirdi (geçmiş tablosu şemada, outbox interceptor'ı). Bugün bu yol birim testlerde (`ModuleDbContextRegistrationTests`) ve iki gerçek veritabanı katmanında sınanıyor. Faz 7a'da Y-09 gereği dokunulmadı. *Koşul:* fabrikaya dokunan bir sonraki iş (ör. 7b'nin kurum testleri); `ReplaceWithSqlite`'ı kullanan alt sınıflar (`HealthEndpointsTests`) için yardımcı kalır.~~ → F7b.3'te yapıldı (D-077 (5)): fabrika `AddModuleDatabase`'le, alt sınıflar `UseDatabase`'i override ediyor, `ReplaceWithSqlite` kalktı.
