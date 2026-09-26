# İlerleme

> Tek doğru durum kaynağı. `/novacore-phase` her başlangıçta bu dosyayı okur ve her adımdan sonra günceller.
> Adım tanımları [PLAN.md](PLAN.md)'dedir. Burada yalnızca durum, commit hash'i ve notlar tutulur.

## Fazlar

| Faz | Durum | Branch | Başlangıç | Bitiş | Merge |
|---|---|---|---|---|---|
| 0 | ✅ tamam | `faz/0-stabilizasyon` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 1 | ✅ tamam | `faz/1-kutuphane-altyapisi` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 2a | ✅ tamam | `faz/2a-paket-yapisi` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 2b | ✅ tamam | `faz/2b-konfigurasyon` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 2c | 🔄 devam ediyor | `faz/2c-yerlesik-karsiliklar` | 2026-09-26 | | |
| 3a | ⏳ başlamadı | | | | |
| 3b | ⏳ başlamadı | | | | |
| 4a | ⏳ başlamadı | | | | |
| 4b | ⏳ başlamadı | | | | |
| 5 | ⏳ başlamadı | | | | |

Durum değerleri: `⏳ başlamadı` · `🔄 devam ediyor` · `⏸ duraklatıldı` (devir notuna bak) · `✅ tamam`

## Adımlar

Biçim: `- [x] F0.3 (abc1234): not`

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

- [ ] F2c.1 Validation (alt adımlar: 1a karakterizasyon testi, 1b Catalog, 1c Payments, 1d Identity, 1e köprünün silinmesi)
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
  - [ ] F2c.1e Köprünün silinmesi (devam ediyor)
- [ ] F2c.2 Yetkilendirme
- [ ] F2c.3 Cache
- [ ] F2c.4 Paket temizliği

### Faz 3a
- [ ] F3a.1 JWT doğrulama (Authority/JWKS)
- [ ] F3a.2 İzin çözümleme
- [ ] F3a.3 Test IdP yardımcısı

### Faz 3b
- [ ] F3b.1 OpenIddict eklentisi
- [ ] F3b.2 Sertleştirmelerin taşınması
- [ ] F3b.3 Eski modülün emekliye ayrılması
- [ ] F3b.4 docker-compose
- [ ] F3b.5 Uçtan uca doğrulama

### Faz 4a
- [ ] F4a.1 Event'lerin outbox'a yazılması
- [ ] F4a.2 Dispatcher
- [ ] F4a.3 Eski dispatcher düzeltmesi

### Faz 4b
- [ ] F4b.1 Stok kararı
- [ ] F4b.2 Uygulama
- [ ] F4b.3 Eşzamanlılık testi

### Faz 5
- [ ] F5.1 Template
- [ ] F5.2 Tüketici smoke testi
- [ ] F5.3 Test borcu
- [ ] F5.4 1.0.0 kararı
- [ ] F5.5 Final dokümanlar

## Açık sorular (Q)

Soru cevaplanınca buraya cevap yazılır. Kalıcı bir karar ortaya çıkarsa DECISIONS'a D-kaydı olarak eklenir.

| ID | Soru | Ne zaman gerekli | Cevap |
|---|---|---|---|
| Q-01 | Önceki oturumdan gelen "yapılmasın" listesi (DECISIONS → Y-13+) | Faz 0'dan önce | Kullanıcı araştırma istedi (2026-09-25). Öneri: [proposals/yapilmayacaklar-onerisi.md](proposals/yapilmayacaklar-onerisi.md). **Onaylandı** → DECISIONS Y-13…Y-43 (D-007). Hook'un yasaklı listesine EF InMemory ve Proxies eklendi, tam sürümlü paket referansı engelleniyor. F0.7 kapsamı genişletildi. |
| Q-02 | LICENSE türü (öneri: MIT) | F0.1 | **MIT** (2026-09-25, kullanıcı) → D-006 |
| Q-03 | GitHub remote/repo var mı? CI ve paket feed'i için | F0.8, F1.6 | ~~Henüz yok~~ → **Var** (2026-09-25, Faz 0 merge'ünden sonra, kullanıcı isteğiyle): `https://github.com/alimuhammedak/NovaCore`, **private**, yalnızca `main` push'landı. İlk CI koşusu (run 36098557732) yeşil. F1.6: remote artık var, paket feed'i ve yayın tetiklemesi F1.6'da ayrıca sorulmalı → **Q-17** (GitHub Packages, `v*` tag). |
| Q-04 | Faz 3'e kadar Identity modülü aktif kullanılacak mı? (F0.9 yapılsın mı?) | Faz 0 başı | **Hayır.** Kullanıcı, plan bitene kadar Identity'yi hiç kullanmayacak (2026-09-25). **F0.9 atlanır.** F0.5'in (yalnızca Identity'ye ait) kaderi Q-05'te. |
| Q-05 | F0.5 (LoggingEmailSender yalnızca dev) hâlâ yapılsın mı? Identity kullanılmayacak ve Faz 3b'de emekliye ayrılacak | Faz 0 başı | **Evet, yapılsın** (2026-09-25, kullanıcı). F0.5 planda olduğu gibi kalır. |
| Q-06 | `docker-compose.yml:16`'da düz metin SA parolası var (Y-35). Baseline'a nasıl girsin? | F0.1 | **`.env`'e taşı** (2026-09-25, kullanıcı) → D-008. Baseline'a bu dosya girmez. |
| Q-07 | F0.3 kriteri "farklı IP 200 alır", ama /auth uç noktaları DB olmadan 200 dönmüyor. Test neye karşı yazılsın? | F0.3 | **Gerçek `/api/v1/auth/login`** (2026-09-25, kullanıcı) → D-009: "farklı IP 429 almaz". |
| Q-08 | F0.5: `IModule.AddModule` ortam almıyor. Identity "Development mı?" sorusunu nasıl cevaplasın? | F0.5 | **`configuration[HostDefaults.EnvironmentKey]`** (2026-09-25, kullanıcı, R-030). Public API değişmez. |
| Q-09 | F0.2: 10.0.x şemasına uymayan Microsoft paketleri (Caching.Hybrid, Http.Resilience 10.0.0 → 10.10.0; Test.Sdk 18.6.0 → 18.10.1) güncellensin mi? | F0.2 | **Dokunma** (2026-09-25, kullanıcı). "Sonraya bırakılanlar"a yazıldı. |
| Q-10 | F1.1 TreatWarningsAsErrors=true ile tek başına commit'lenirse build ~225 hatayla kırmızı olur (Y-36). Sıralama? | F1.1 | **TWAE en sona** (2026-09-25, kullanıcı): F1.1 TWAE hariç her şeyi açar, F1.2f TWAE'yi açar → D-010 (yerine D-014) |
| Q-11 | CA1848/CA1873 38 yerde (F2b.6'nın işi). F1.2 0 uyarı istiyor. | F1.2 | **`.editorconfig`'te geçici `suggestion`**, F2b.6'da kaldırılır (2026-09-25, kullanıcı) → D-011 |
| Q-12 | CA1707: 42 test adı alt çizgili. | F1.2 | **`tests/**.cs` için CA1707 = none** (2026-09-25, kullanıcı, R-035) → D-011 |
| Q-13 | Çekirdek API: CA1000 ×3, CA1711, CA1716. | F1.2 | **CA1000 düzeltilir** (`Result<T>.Success/Failure` internal, `PagedList<T>.Empty` silinir). **CA1711/CA1716 gerekçeli `[SuppressMessage]`** (2026-09-25, kullanıcı, R-034) → D-011, D-012 |
| Q-14 | IsPackable csproj'da tanımlı, props'taki koşul onu göremez. | F1.1 | **Yeni `Directory.Build.targets`** (2026-09-25, kullanıcı, R-032) → D-010 (yerine D-014, bkz. Q-18) |
| Q-15 | PackageReadmeFile için hangi README? | F1.1 | **Tek ortak kısa README** (`src/PACKAGE.md`) (2026-09-25, kullanıcı) → D-013 |
| Q-16 | TWAE ile NU1901/NU1902 de hataya dönüyor. | F1.1 | **F0.2 politikası korunur**: `WarningsNotAsErrors=NU1901;NU1902` (2026-09-25, kullanıcı, R-033) → D-011 |
| Q-18 | F1.1 uygulanırken: targets'taki `GenerateDocumentationFile` etkisiz (R-040). Doc üretimi nasıl açılsın? | F1.1 | **Packable csproj'lara** eklenir. `ContinuousIntegrationBuild` props'a taşınır, targets'a "packable ama doc yok" koruması eklenir (2026-09-25, kullanıcı) → D-014 (D-010'un yerine) |
| Q-19 | F1.1: doc üretimi OpenAPI belgesine XML açıklamaları ekliyor (R-041). Faz "davranış değişikliği yok" diyor. | F1.1 | **Kabul, kaydet** (2026-09-25, kullanıcı): yalnızca açıklama metni, API sözleşmesi aynı. CHANGELOG'a "Changed" notu. |
| Q-20 | F1.4: RS0026, `ToHttpResult`'un iki overload'u da opsiyonel parametreli. | F1.4 | **Overload'lara böl** (2026-09-25, kullanıcı, R-042) → D-015 |
| Q-17 | Sürüm tag'den nasıl türetilsin, feed ne? | F1.3, F1.6 | **Tag + props kontrolü.** GitHub Packages (`alimuhammedak`), yalnızca `.nupkg`. `.snupkg` artifact olarak saklanır (2026-09-25, kullanıcı, R-039) → D-013 |
| Q-21 | F2a.3: açık kayıtta `IModule.IsEnabled` ne olsun? | F2a.3 | **Kaldır** (2026-09-25, kullanıcı; Y-19) → D-018 |
| Q-22 | F2a.4'teki Y-05 testi, FluentValidation F2c'ye kadar çekirdekte kaldığı için kırmızı kalıyor. Nasıl çözülsün? | F2a.4 | **Ayrı, paketlenmeyen `NovaCore.FluentValidation` projesi**, F2c.1'de silinir (2026-09-25, kullanıcı) → D-019 |
| Q-23 | F2a.6: `0.1.0-preview.1` yayımlansın mı? | F2a.6 | **Yalnızca yerel pack ve içerik kontrolü.** Tag ve push yok, yayın kararı sonraya kalır (2026-09-25, kullanıcı) |
| Q-24 | F2b.5: `/health` ve `/alive` production'da nasıl korunsun? (yalnızca dev / yönetim portu (LocalPort) / yetkilendirme) | F2b.5 | **Yalnızca Development** (2026-09-25, kullanıcı; R-049) → D-022 |
| Q-25 | F2c.1: `CreatePayment.ProductId`'nin boş Guid kuralı (`NotEmpty`) hazır bir DataAnnotations attribute'uyla ifade edilemiyor (`[Required]` Guid.Empty'yi reddetmez). Nasıl ifade edilsin? | F2c.1 | **Özel `ValidationAttribute`** (2026-09-26, kullanıcı; R-052): `CreatePayment` içinde private nested. MS'in tek değerli kural yolu, hata diğer alan hatalarıyla aynı yanıtta gelir → D-023 |
| Q-26 | F2c.1: `ExceptionHandlerOrderTests` silinecek `ValidationExceptionHandler`'ın sırasını test ediyor. Silinsin mi? (Y-44) | F2c.1e | **Sil** (2026-09-26, kullanıcı). Köprü D-019 gereği gidiyor, validation 400 yolunu `ValidationTests` koruyor. |

## Sonraya bırakılanlar

Plan dışında fark edilen, ama kapsam dışı olduğu için dokunulmayan konular. Her faz sonunda kullanıcı bunları gözden geçirir.

- xUnit v3 → 4.x ve xunit.runner.visualstudio 4.x major güncellemesi (F0.2 kapsamı dışında). Kaynak: `dotnet list package --outdated`, 2026-09-24.
- (Faz 0, Q-09) `Microsoft.Extensions.Caching.Hybrid` ve `Microsoft.Extensions.Http.Resilience` 10.0.0 → 10.10.0, `Microsoft.NET.Test.Sdk` 18.6.0 → 18.10.1. Açıkları yok. Hybrid için F2c.3, Resilience için F2a.2 (eklenti paketi) uygun yer olabilir.
- (Faz 0) Global ve "strict" rate limiter partition anahtarı olarak `RemoteIpAddress` kullanıyor. Bir reverse proxy arkasında bu proxy'nin IP'si olur. `ForwardedHeaders` yapılandırılmamış.
- (Faz 0, F0.2) Bu makinede kurulu shared framework 10.0.8, paketler artık 10.0.12. Runtime'ı 10.0.12'ye güncellemek ortam işi, repo işi değil.
- (Faz 0, F0.8) GitHub Actions uyarısı (ilk CI koşusu, 2026-09-25): `ubuntu-latest` etiketi 19 Ekim 2026'dan itibaren Ubuntu 26'ya geçecek (actions/runner-images#14748). Etki beklenmiyor. İlk koşularda kırmızı olursa runner'ı `ubuntu-24.04`'e sabitlemek bir seçenek.
- (Faz 0, F0.7) Y-31 testi ApiExplorer'a dayanıyor. `Task<IResult>` dönen ve `Produces<T>` bildirmeyen handler'ların yanıt payload'ını göremez (Identity uç noktalarının çoğu böyle). ApiExplorer dışı endpoint'ler de kapsam dışı (health, `ExcludeFromDescription`). Handler'lar typed result'a (`Results<Ok<T>, …>`) geçerse kapsam kendiliğinden genişler.
- (Faz 0, F0.6) Kök `CLAUDE.md` Standards bölümünde "If a mediator is ever truly needed, use MIT `martinothamar/Mediator`" yazıyor. Bu, DECISIONS Y-01 ile çelişiyor ("herhangi bir mediator veya request-dispatch soyutlaması" yasak). Öncelik DECISIONS'ta, ama not gelecekteki oturumları yanıltabilir. Cümle kaldırılmalı veya Y-01'e yönlendirilmeli.
- (Faz 0) "strict" policy'nin tek tüketicisi Identity'nin `/api/v1/auth` grubu. Identity F3b.3'te emekliye ayrılınca policy'nin ve `StrictRateLimitTests`'in yeri yeniden değerlendirilmeli.
- (Faz 1) ~~F2b.6'da yapılacak: `.editorconfig`'teki geçici CA1848/CA1873 satırları silinecek.~~ → F2b.6 Faz 2a'nın ilk adımı oldu (D-017), Faz 2a adım listesinde.
- (Faz 1, F2a) `NovaCore.Modules.Catalog.Contracts` packable. ANALYSIS §4'teki paket haritasında yok. Örnek modülün sözleşmesi paket olmalı mı? 2a'da karar verilmeli. Packable kalırsa XML doc ve PublicAPI dosyaları da onunla gider.
- ~~(Faz 1, F2a.2) **Yeni packable projeler PublicAPI takibini sessizce atlayabilir.**~~ → F2a.2a'da çözüldü: `Directory.Build.targets`'taki `NovaCoreRequirePublicApiTracking` hedefi, packable bir projede referans yoksa build'i durduruyor (D-020 (7)). Eski not: `AdditionalFiles` ve doc koruması `Directory.Build.targets`'ta, ama `Microsoft.CodeAnalysis.PublicApiAnalyzers` referansı her csproj'da ayrı (PLAN F1.4 böyle istiyordu). F2a.2'deki 3 yeni pakete referans eklenmeli. Alternatif: referansı targets'a taşımak ya da bir koruma eklemek; bu PLAN'dan farklı bir yaklaşım olduğu için sorulmalı (code review bulgusu).
- (Faz 1) **Stop hook değişiklik algılaması eksik:** `verify-on-stop.js` kök `Directory.Build.targets`'ı ve `*.txt` dosyalarını (PublicAPI) izlemiyor. Yalnızca bunlar değişirse build/test doğrulaması çalışmaz. Dosya korunan bir yolda, düzeltmek kullanıcı onayı ister (code review bulgusu).
- (Faz 1, F5.1) **Template paket kimliğini taşıyor:** `dotnet new novacore`, `Directory.Build.targets`'ı (Authors = Muhammed Ali AK), `src/PACKAGE.md`'yi, `release.yml`'i ve PublicAPI dosyalarını kopyalıyor. `sourceName` değişimi yüzünden RepositoryUrl `github.com/alimuhammedak/<YeniAd>` oluyor (code review bulgusu).
- (Faz 1, F2a) CA1711 (`IDomainEventHandler<T>`) ve CA1716 (`Error`) gerekçeli bastırıldı. 2a'da namespace'ler değişirken bu isimler de yeniden düşünülebilir.
- ~~(Faz 1, F2a.2) Serilog log kültürü:~~ → F2a.2a'da karar verildi: `null` korunuyor, 2a'da davranış değişmiyor (D-020 (8)). Eski not: `formatProvider: null` Serilog'un varsayılanı. Logların culture-invariant olup olmayacağına NovaCore.Serilog'da karar verilmeli.
- (Faz 1) Çözüm genelinde `dotnet pack`, `NovaCore.Api` için kodsuz bir uyarı veriyor: Web SDK `WarnOnPackingNonPackableProject=true` ayarlıyor. Pack başarısız olmuyor, uyarı bastırılmadı (Y-10). Kapatmak için Api csproj'da bu özellik `false` yapılabilir (kullanıcı onayıyla).
- (Faz 2a, F2a.5) Kök CLAUDE.md Standards'taki "0 warnings" maddesi bastırma için hâlâ "DECISIONS Y-10" diyor. Y-10'un yerini Y-44 aldı (D-016), ifade de değişti: yalnızca MS'in "When to suppress" iznine dayanan kalıcı bastırma. Tek kelimelik ama bir kuralı değiştiriyor, 2a kapsamı dışı.
- (Faz 2a, code review) `NovaCore.FluentValidation`, `NovaCore.Serilog`, `NovaCore.OpenTelemetry` ve `NovaCore.Scalar` namespace'leri, `NovaCore.*` altındaki kodda üçüncü taraf kök namespace'leri gölgeliyor. Örneğin bir modülde kısmen nitelenmiş `FluentValidation.Results.X` artık `NovaCore.FluentValidation`'a bağlanıp derlenmez, `global::` gerekir. Bugünkü kod dosya başındaki using'lerle çalışıyor. Namespace adları D-020 (4)'te seçildi; değiştirmek public API kararı.
- (Faz 2a, code review) `PackageDependencyTests`'in csproj kontrolü `Directory.Build.*` ya da `GlobalPackageReference` ile eklenen paketleri görmüyor. `Include` yerine `Update` kullanan bir `PackageReference`'ta NRE veriyor. `bin/obj` altındaki csproj'ları da dışlamıyor. Assembly düzeyindeki test kısmen telafi ediyor.
- (Faz 2a, code review) `ExplicitRegistrationTests` ile `SourceHygieneTests` aynı "src altındaki .cs dosyaları, bin/obj hariç" kodunu tekrar ediyor. `Solution`'a bir yardımcı taşınabilir. Bu mevcut bir testi değiştireceği için 2a'da yapılmadı.
- (Faz 2a, code review) `SecurityAuditLogger.LoginFailed` maskeyi guard'sız hesaplıyor, `UserRegistered` ise guard'lı. CA1873 varsayılan olarak yalnızca Information ve altına bakıyor (R-047). Davranış F2b.6'dan önceki gibi. Identity F3b.3'te emekliye ayrılacak.
- (Faz 2a, code review) DECISIONS'ta D-018…D-020 satırları D-017'nin üstüne eklendi, tablo ID sırasında değil. Düzeltmek korunan dosyada yalnızca biçimsel bir düzenleme.
- (Faz 2a) Kodsuz Api pack uyarısı, Y-10 → Y-44 notu ve mediator cümlesi hâlâ açık (yukarıdaki maddeler).
- (Faz 1) `.snupkg` GitHub Packages'a gitmiyor (R-039), yalnızca workflow artifact'i. İleride nuget.org seçilirse sembol sunucusu kullanılabilir.
- (Faz 1, F5.4) Package validation baseline'ı (`PackageValidationBaselineVersion`) `Microsoft.NET.ApiCompat.targets:18`'de değerlendirme anında okunuyor, import'u Directory.Build.targets'tan sonra geliyor. F5.4'te nereye yazılacağı doğrulanmalı (R-040).
- ~~(Faz 1) `NovaCore.Core.Web` paketinin nuspec bağımlılıklarında FluentValidation, Serilog, OpenTelemetry ve Scalar var (D-003 ihlali). F2a.2 ve F2c'nin işi.~~ → F2a.2'de çözüldü (Serilog/OTel/Scalar eklentilere, FluentValidation `NovaCore.FluentValidation`'a taşındı), F2a.4 testleri koruyor.
- ~~(Faz 2a, F2a.4 → F2c.1) **Y-20'nin modül kısmı henüz testli değil.**~~ → F2c.1e'de çözüldü: `ExplicitRegistrationTests` artık tüm `src`'yi tarıyor ve `*FromAssembly*` kayıt yardımcılarını da yakalıyor. EF'in `ApplyConfigurationsFromAssembly`'si model yapılandırması olduğu için hariç (D-023 (5)). Eski not: Modüller validator'ları `AddValidatorsFromAssembly` ile (assembly taraması) kaydediyor. Bu FluentValidation ile birlikte F2c.1'de gidiyor; o zaman `ExplicitRegistrationTests`'in taraması `src/Modules`'ü de kapsamalı. `ApplyConfigurationsFromAssembly` (EF model yapılandırması, DI kaydı değil) Y-20 kapsamında mı, o gün karar verilmeli.
- (Faz 2a, F2a.2c) `SecurityHeadersMiddleware` hâlâ `/scalar` yolunu CSP'den muaf tutuyor. Scalar artık bir eklenti; çekirdeğin eklentinin yolunu bilmesi küçük bir bağlılık. Davranış değişikliği olacağı için 2a'da dokunulmadı.
- (Faz 2a, F2a.2b) OpenTelemetry `service.version` olarak uygulamanın değil NovaCore eklentisinin assembly sürümünü (0.1.0.0) raporluyor. Davranış 2a'da korundu. Doğrusu muhtemelen giriş assembly'sinin sürümü.
- (Faz 2b, code review) `GlobalExceptionHandler.Map`'te `BadHttpRequestException` için bir durum yok. Development'ta minimal API'ler bozuk gövdede bu exception'ı fırlatıyor (`ThrowOnBadRequest`), yanıt 400 yerine 500 oluyor ve Error seviyesinde log düşüyor. Bu 2b'den önce de böyleydi; status eşlemesini değiştirmek F2b.4'ün kapsamı dışında. Çözüm: `BadHttpRequestException.StatusCode`'u kullanmak.
- (Faz 2b, code review) DbContext health check'lerinde timeout yok. SQL Server'a erişilemezse `/health` bağlantı timeout'u kadar bekleyebilir. Uçlar yalnızca Development'ta olduğu için şimdi etkisi düşük. Production'da probe gerekince (D-022) `HealthCheckRegistration.Timeout` ile birlikte ele alınmalı.
- (Faz 2b, code review) Test yardımcıları tekrar ediyor: `NoOpEmailSender` iki dosyada (`EmailSenderRegistrationTests`, `ConfigurationValidationTests`), kullanıcı seed'i iki dosyada (`AuthFlowTests`, `RefreshTokenExpiryTests`). Paylaşılan bir test yardımcısına taşınabilir; mevcut testleri değiştirmek gerektiği için yapılmadı.
- (Faz 2b) `NovaCore.OpenTelemetry`, `OTEL_EXPORTER_OTLP_ENDPOINT`'i kayıt anında okuyor (`OpenTelemetryHostingExtensions.cs:35`). F2b.1'in kapsamı iki çekirdek paketti. Bu okuma `ConfigureAppConfiguration` ile ezilemiyor, ama MS ServiceDefaults da aynı kalıbı kullanıyor.
- (Faz 2b → Faz 3a) Token üretimi `TimeProvider`'dan okuyor, JwtBearer'ın lifetime doğrulaması (IdentityModel) ise gerçek saatten. Sahte saatli testler bu yüzden gerçek "şimdi"den başlıyor (R-050). F3a.1'de doğrulama yeniden yazılırken `TimeProvider` ile hizalanabilir.

## Son devir notu

> Her oturumun sonunda (faz bitmese bile) üzerine yazılır. Bir sonraki oturumun bilmesi gerekenler: nerede kalındığı, neyin yarım kaldığı, sürprizler ve bekleyen sorular.

**2026-09-25, Faz 2b bitti** ve kullanıcı onayıyla `faz/2b-konfigurasyon` main'e merge edildi (`--no-ff`), `main` push'landı.

- **Yapılanlar:**
  - Config (F2b.1a/1b): `Jwt`, `RateLimiting`, `Cors`, `Cache` ve `Database` artık `BindConfiguration` + `ValidateOnStart` ile bağlanan doğrulanmış options. `[OptionsValidator]` source-gen kullanılıyor, nitelikle ifade edilemeyen kurallar `.Validate` ile. Framework options'ları (`JwtBearerOptions`, `RateLimiterOptions`, `CorsOptions`, `HybridCacheOptions`) `Configure<IOptions<T>>` ile ilk kullanımda yapılandırılıyor. `DatabaseMigrationService` her zaman kayıtlı ve internal.
  - Test host (F2b.2): env-var hack'i kalktı. `ConfigureAppConfiguration` yeterli.
  - Saat (F2b.3): tüm zaman okumaları enjekte edilen `TimeProvider`'dan. Tek istisna `DomainEvent.OccurredOnUtc` varsayılanı. `AccessToken` `IssuedAtUtc` taşıyor. `SourceHygieneTests` kuralı koruyor.
  - Hata yanıtları (F2b.4): istemciye yalnızca `AppException` mesajları gidiyor. `instance` bir URI reference (path, query yok).
  - Health (F2b.5): her modül DbContext'i için "ready" etiketli DB kontrolü. `/health` ve `/alive` yalnızca Development'ta (D-022). Test host üç context'i de SQLite'a çeviriyor.
  - Testler 58 → 89. OpenAPI belgesi main ile bayt bayt aynı.
  - Kararlar: D-021 (model), D-022 (kullanıcı, Q-24). Araştırmalar: R-048…R-051.
- **Sürprizler:**
  - Tembel okumaya geçince `ConfigureHostConfiguration` (`args`) artık factory'nin `ConfigureAppConfiguration` değerini ezemiyor. Sonradan eklenen kaynak kazanıyor, bu yüzden `StrictRateLimitApiFactory` F2b.1a'da `ConfigureAppConfiguration`'a geçti.
  - `ValidateOnStart` birden fazla hatayı `AggregateException` içinde topluyor. Testler iç mesajları düzleştirerek kontrol ediyor.
  - `FakeTimeProvider` DI'a konunca ASP.NET Identity de onu kullanıyor (R-050). JWT doğrulaması ise gerçek saatte kalıyor.
  - Bash'te inline Python heredoc'ları regex kaçışlarını ve tırnakları bozabiliyor. Uzun betikler dosyaya yazılıp çalıştırılmalı; regex içeren C# dosyaları Write/Edit ile yazılmalı.
- **Sonraki fazın (2c) bilmesi gerekenler:**
  - F2c.1: `NovaCore.FluentValidation` silinecek. `ConfigurationValidationTests` ve `HealthEndpointsTests` host'u bu köprü olmadan da açabilmeli. `ValidationExceptionHandler`'a bağımlı test: `ExceptionHandlerOrderTests`.
  - F2c.3: `ICacheService`/`HybridCacheService` kalkacak. `CacheOptions` (`Cache:DefaultExpiration`) ve `HybridCacheOptions` yapılandırması `AddCoreCaching`'te duruyor; `AddHybridCache` doğrudan kullanılınca da korunmalı.
  - Yeni config eklenecekse kök CLAUDE.md Standards'taki kural geçerli: options + `ValidateOnStart`, kayıt anında okuma yok.
  - Açık "Sonraya bırakılanlar" maddeleri: `BadHttpRequestException` → 500, health check timeout, test yardımcılarındaki tekrar, OTel'in erken okuması.
- **Bekleyen:** önizleme yayını (tag + GitHub Packages) ayrı bir kullanıcı kararı.
- **Sıradaki:** yeni oturumda `/novacore-phase 2c`.
