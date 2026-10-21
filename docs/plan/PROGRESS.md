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
| 2c | ✅ tamam | `faz/2c-yerlesik-karsiliklar` | 2026-09-26 | 2026-09-26 | 2026-09-27 (kullanıcı onayı, `--no-ff`; push 2026-09-27) |
| 3a | ✅ tamam | `faz/3a-kimlik-dogrulama` | 2026-09-27 | 2026-09-27 | 2026-09-27 (kullanıcı onayı, `--no-ff`; push 2026-09-27) |
| 3b | ✅ tamam | `faz/3b-idp` | 2026-09-27 | 2026-09-27 | 2026-09-27 (kullanıcı onayı, `--no-ff`; push 2026-09-28, kullanıcı isteği) |
| 4a | ✅ tamam | `faz/4a-outbox` | 2026-09-28 | 2026-09-28 | 2026-09-28 (kullanıcı onayı, `--no-ff`; push 2026-09-28, kullanıcı isteği) |
| 4b | ✅ tamam | `faz/4b-referans-moduller` | 2026-09-28 | 2026-09-28 | 2026-09-28 (kullanıcı onayı, `--no-ff`; push edilmedi) |
| 4c | ✅ tamam | `faz/4c-tuketici-hazirligi` | 2026-10-20 | 2026-10-20 | 2026-10-20 (kullanıcı onayı, `--no-ff`; push edilmedi) |
| 4d | ✅ tamam | `faz/4d-kimlik-uretim` | 2026-10-20 | 2026-10-20 | 2026-10-20 (kullanıcı onayı, `--no-ff`; push 2026-10-20, kullanıcı isteği) |
| 5 | ✅ tamam | `faz/5-sablon-yayin` | 2026-10-20 | 2026-10-20 | 2026-10-20 (kullanıcı onayı, `--no-ff`; push 2026-10-20; push'taki CI yeşil (`build-and-test`, `template-smoke`); `v0.1.0-preview.1` tag'i kullanıcı onayıyla, release.yml yeşil, 8 paket GitHub Packages'ta) |
| 6a | 🔄 devam ediyor | `faz/6a-ci-sertlestirme` | 2026-10-21 | | |
| 6b | ⏳ başlamadı | | | | |
| 6c | ⏳ başlamadı | | | | |
| 6d | ⏳ başlamadı | | | | |
| 6e | ⏳ başlamadı | | | | |

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
- [x] F6a.8: docker-compose. Bütün portlar `127.0.0.1`'e bağlandı (SQL Server 1433, Seq 5341/8081, Jaeger 16686/4317/4318; Keycloak zaten öyleydi). İmajlar `tag@sha256:<digest>` ile sabitlendi: digest'ler `docker buildx imagetools inspect` ile okundu (Seq, Jaeger, Keycloak çok mimarili index; SQL Server tek manifest). `docker compose config` iki dosyayla geçerli.

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
| Q-27 | Faz 3b'nin IdP'si: OpenIddict eklentisi (PLAN F3b.1–3, Keycloak yalnızca F3b.4'te opsiyonel), yalnızca Keycloak ya da ikisi birden? (PLAN: 3a sonunda sorulur) | F3b.1'den önce | **OpenIddict eklentisi, Keycloak opsiyonel** (2026-09-27, kullanıcı; önce farkların açıklanmasını istedi, açıklama faz raporunda). PLAN F3b.1–F3b.5 olduğu gibi geçerli → D-025 |
| Q-28 | Eski Identity modülünün PLAN F3b.2 listesinde olmayan özellikleri (2FA, parola değiştirme, logout-all, `/me`) yeni eklentiye taşınsın mı? | F3b.2 | **Tam parite** (2026-09-27, kullanıcı) → D-027 |
| Q-29 | F3b.3'te Development'taki simetrik JWT doğrulaması (D-024 (1)) ne olsun? | F3b.3 | **Kaldır**: `Jwt:SigningKey`/`Issuer`/geliştirme anahtarı silinir, `Jwt:Authority` her ortamda zorunlu (2026-09-27, kullanıcı) → D-028 |
| Q-30 | F4b.1: Stok rezervasyonu senkron bir contract (`ReserveStockAsync`) ile mi, event ile mi yapılsın? | F4b.1 | **Event akışı** (2026-09-28, kullanıcı). Sunulan seçenekler: senkron contract (iki commit arasında stok kaybı, outbox kullanılmaz), senkron rezervasyon + event'le ödeme kaydı (POST 202'ye döner), event akışı (önerilen). R-075 → D-034 |
| Q-31 | F4d.1: İlk proje hangi IdP modelini kullanacak? (a) kendi host'unda eklenti, (b) projeler arası merkezi IdP, (c) harici IdP (Entra, Keycloak) | F4d.1 | **(a) kendi host'unda eklenti** (2026-10-20, kullanıcı; önerilen seçenek). Faz 4d yürür → D-037 |
| Q-32 | F5.4 (1): NovaCore paketleri ve şablon paketi hangi kanaldan tüketilsin? (GitHub Packages, nuget.org, yalnızca yerel klasör feed'i) | F5.4 (şablonun `nuget.config`'i ve CI'ı buna bağlı, bu yüzden plan aşamasında) | **GitHub Packages** (2026-10-20, kullanıcı; önerilen seçenek). D-013 olduğu gibi kalır → D-039 |
| Q-33 | F5.4 (2): İlk sürüm `0.1.0-preview.1` mi, `0.1.0` mı? | F5.4 | **`0.1.0-preview.1`** (2026-10-20, kullanıcı; önerilen seçenek). Tag ve push faz sonunda ayrıca sorulur → D-039 |
| Q-34 | F5.1b: nuget.org'da başka sahiplerin `NovaCore.Templates`, `NovaCore.AspNetCore`, `NovaCore.EntityFrameworkCore` paketleri var (R-091). Bizim paket ID'lerimiz ne olsun? (yalnızca şablonun ID'si değişsin / hiçbiri değişmesin / hepsine sahip öneki) | F5.1b | Kullanıcı: "hiçbir sorun yaşamayacak şekilde ilerleyelim, benim ismimi koyma"; önek sorusuna: **"BMD ile başlasın"** (2026-10-20). Bütün ID'ler `BMD.NovaCore.*`, assembly ve namespace'ler aynı → D-041 |
| Q-35 | F6a.1: Repo public oldu. GitHub Packages'taki 8 paket private mı kalsın, public mi olsun? (Anonim istek 401; public paket de token istiyor; public yapılan paket private'a dönemez.) | F6a.1 (D-039 (1) yeniden açıldı) | **Public yap** (2026-10-21, kullanıcı; önerilen "private kalsın"dı). Ayarı kullanıcı paket sayfalarından yapar; token gerekliliği sürer → D-050, R-093 |
| Q-36 | F6a.11: GitHub ayarlarını (secret scanning + push protection, Dependabot alerts/security updates, ruleset'ler, `release` environment'ı) kim yapsın? | F6a.11 | **Model, `gh api` ile**, her istek önce gösterilip onaylanarak, sonra API'den geri okunarak (2026-10-21, kullanıcı; önerilen seçenek) |
| Q-37 | F6a.11: `main` ve `v*` ruleset'leri ne kadar sıkı olsun? (Fazlar yerelde `--no-ff` merge edilip main'e doğrudan push ediliyor.) | F6a.11 | **Koruma, akış aynı** (2026-10-21, kullanıcı; önerilen): main'de force push ve silme engellenir; `v*` tag'lerini yalnızca kullanıcı oluşturur, günceller, siler; `release` environment'ı yalnızca `v*` tag'lerinden ve kullanıcının onayıyla çalışır; status check'ler zorunlu değil |
| Q-38 | Faz branch'i faz sırasında GitHub'a push edilsin mi? (Workflow'lar ancak GitHub'da koşunca doğrulanıyor.) | F6a.2 | **Evet, faz branch'i** (2026-10-21, kullanıcı; önerilen). main'e dokunulmaz |
| Q-39 | F6a.4: Lisans kapısı bugünkü grafikte yalnızca Polly.Core/Extensions/RateLimiting 8.4.2'yi (BSD-3-Clause) buldu; `Microsoft.Extensions.Http.Resilience` üzerinden geliyor. Kapı nasıl davransın? (Polly'ye adlı istisna / BSD-3-Clause'a izin / yalnızca doğrudan paketler) | F6a.4 | **Polly'ye adlı istisna** (2026-10-21, kullanıcı; önerilen). Kapı geçişlileri de denetler, Y-06 değişmez → D-052, R-096 |

## Sonraya bırakılanlar

Plan dışında fark edilen, ama kapsam dışı olduğu için dokunulmayan konular. Her faz sonunda kullanıcı bunları gözden geçirir.

**D-033 ile plana alınanlar (2026-09-28).** Aşağıdaki maddeler artık bir adıma bağlı. O adım bitince madde üstü çizilir.
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

**D-033 ile bilerek plana alınmayanlar.** Bugün bir tüketicileri yok (Y-14). Madde, yanındaki koşul gerçekleşince plana alınır:
- `UseCoreWeb`'e kimlik doğrulama ile yetkilendirme arasında bir middleware yeri. *Koşul:* bir proje yetkilendirmeden önce kimliğe bağlı bir middleware'e (ör. tenant çözümü) ihtiyaç duyar. Bugün proje kendi middleware'ini `UseCoreWeb`'den sonra ekleyebiliyor.
- İzin önbelleğinin anahtarına `iss` ya da ek bir bölümleme. *Koşul:* ikinci bir issuer'a güvenilir, ya da izinler kullanıcıdan başka bir şeye (ör. tenant) göre değişir. Bu yapılmadan önce iki issuer'dan gelen aynı `sub` başka bir kullanıcının izinlerini alabilir (Faz 3a maddesi).
- Request timeouts middleware'i (R-073). *Koşul:* uzun süren bir uç ya da bir OWASP API4 bulgusu.
- Outbox'ın birden çok örnekte çalışması (kilit/lease) ve mesajda trace bağlamı. *Koşul:* proje yatayda ölçeklenir, ya da event zincirlerini trace'te izlemek gerekir.
- `ApplicationUser` genişletme ve token'a claim ekleme kancası. *Koşul:* bir proje kullanıcıya bağlı ek bir claim ister.
- BFF paketi ya da şablonu (R-073). *Koşul:* iki proje tarayıcı frontend'i için aynı BFF'e ihtiyaç duyar. O güne kadar README önerisi (F5.5) yeterli.

- xUnit v3 → 4.x ve xunit.runner.visualstudio 4.x major güncellemesi (F0.2 kapsamı dışında). Kaynak: `dotnet list package --outdated`, 2026-09-24.
- (Faz 0, Q-09) `Microsoft.Extensions.Caching.Hybrid` ve `Microsoft.Extensions.Http.Resilience` 10.0.0 → 10.10.0, `Microsoft.NET.Test.Sdk` 18.6.0 → 18.10.1. Açıkları yok. Hybrid için F2c.3, Resilience için F2a.2 (eklenti paketi) uygun yer olabilir.
- ~~(Faz 0) Global ve "strict" rate limiter partition anahtarı olarak `RemoteIpAddress` kullanıyor. Bir reverse proxy arkasında bu proxy'nin IP'si olur. `ForwardedHeaders` yapılandırılmamış.~~ → F4c.3'te çözüldü: güvenilen proxy'nin `X-Forwarded-For`'u `RemoteIpAddress`'i istemcinin adresi yapıyor.
- (Faz 0, F0.2) Bu makinede kurulu shared framework 10.0.8, paketler artık 10.0.12. Runtime'ı 10.0.12'ye güncellemek ortam işi, repo işi değil.
- (Faz 0, F0.8) GitHub Actions uyarısı (ilk CI koşusu, 2026-09-25): `ubuntu-latest` etiketi 19 Ekim 2026'dan itibaren Ubuntu 26'ya geçecek (actions/runner-images#14748). Etki beklenmiyor. İlk koşularda kırmızı olursa runner'ı `ubuntu-24.04`'e sabitlemek bir seçenek.
- ~~(Faz 0, F0.7) Y-31 testi ApiExplorer'a dayanıyor. `Task<IResult>` dönen ve `Produces<T>` bildirmeyen handler'ların yanıt payload'ını göremez (Identity uç noktalarının çoğu böyle).~~ → F4c.5'te çözüldü: belgede görünen bütün handler'lar tipli sonuç dönüyor ve `EndpointContractTests` bunu doğruluyor. ApiExplorer dışı uçlar (health, `ExcludeFromDescription`) hâlâ kapsam dışı.
- ~~(Faz 0, F0.6)~~ → F5.5'te düzeltildi (kök CLAUDE.md ve README Y-01'e yönlendiriyor). Kök `CLAUDE.md` Standards bölümünde "If a mediator is ever truly needed, use MIT `martinothamar/Mediator`" yazıyor. Bu, DECISIONS Y-01 ile çelişiyor ("herhangi bir mediator veya request-dispatch soyutlaması" yasak). Öncelik DECISIONS'ta, ama not gelecekteki oturumları yanıltabilir. Cümle kaldırılmalı veya Y-01'e yönlendirilmeli.
- ~~(Faz 0) "strict" policy'nin tek tüketicisi Identity'nin `/api/v1/auth` grubu.~~ → Faz 3b'de değerlendirildi: policy'yi artık eklentinin `/connect/token`, `/account/*` ve `/api/v1/auth/*` uçları kullanıyor, `StrictRateLimitTests` `/connect/token`'ı sınıyor (D-009'un anlamı korundu).
- (Faz 1) ~~F2b.6'da yapılacak: `.editorconfig`'teki geçici CA1848/CA1873 satırları silinecek.~~ → F2b.6 Faz 2a'nın ilk adımı oldu (D-017), Faz 2a adım listesinde.
- (Faz 1, F2a) `NovaCore.Modules.Catalog.Contracts` packable. ANALYSIS §4'teki paket haritasında yok. Örnek modülün sözleşmesi paket olmalı mı? 2a'da karar verilmeli. Packable kalırsa XML doc ve PublicAPI dosyaları da onunla gider.
- ~~(Faz 1, F2a.2) **Yeni packable projeler PublicAPI takibini sessizce atlayabilir.**~~ → F2a.2a'da çözüldü: `Directory.Build.targets`'taki `NovaCoreRequirePublicApiTracking` hedefi, packable bir projede referans yoksa build'i durduruyor (D-020 (7)). Eski not: `AdditionalFiles` ve doc koruması `Directory.Build.targets`'ta, ama `Microsoft.CodeAnalysis.PublicApiAnalyzers` referansı her csproj'da ayrı (PLAN F1.4 böyle istiyordu). F2a.2'deki 3 yeni pakete referans eklenmeli. Alternatif: referansı targets'a taşımak ya da bir koruma eklemek; bu PLAN'dan farklı bir yaklaşım olduğu için sorulmalı (code review bulgusu).
- (Faz 1) **Stop hook değişiklik algılaması eksik:** `verify-on-stop.js` kök `Directory.Build.targets`'ı ve `*.txt` dosyalarını (PublicAPI) izlemiyor. Yalnızca bunlar değişirse build/test doğrulaması çalışmaz. Dosya korunan bir yolda, düzeltmek kullanıcı onayı ister (code review bulgusu).
- ~~(Faz 1, F5.1)~~ → F5.1b'de çözüldü. **Template paket kimliğini taşıyor:** `dotnet new novacore`, `Directory.Build.targets`'ı (Authors = Muhammed Ali AK), `src/PACKAGE.md`'yi, `release.yml`'i ve PublicAPI dosyalarını kopyalıyor. `sourceName` değişimi yüzünden RepositoryUrl `github.com/alimuhammedak/<YeniAd>` oluyor (code review bulgusu).
- (Faz 1, F2a) CA1711 (`IDomainEventHandler<T>`) ve CA1716 (`Error`) gerekçeli bastırıldı. 2a'da namespace'ler değişirken bu isimler de yeniden düşünülebilir.
- ~~(Faz 1, F2a.2) Serilog log kültürü:~~ → F2a.2a'da karar verildi: `null` korunuyor, 2a'da davranış değişmiyor (D-020 (8)). Eski not: `formatProvider: null` Serilog'un varsayılanı. Logların culture-invariant olup olmayacağına NovaCore.Serilog'da karar verilmeli.
- (Faz 1) Çözüm genelinde `dotnet pack`, `NovaCore.Api` için kodsuz bir uyarı veriyor: Web SDK `WarnOnPackingNonPackableProject=true` ayarlıyor. Pack başarısız olmuyor, uyarı bastırılmadı (Y-10). Kapatmak için Api csproj'da bu özellik `false` yapılabilir (kullanıcı onayıyla).
- ~~(Faz 2a, F2a.5)~~ → F5.5'te düzeltildi. Kök CLAUDE.md Standards'taki "0 warnings" maddesi bastırma için hâlâ "DECISIONS Y-10" diyor. Y-10'un yerini Y-44 aldı (D-016), ifade de değişti: yalnızca MS'in "When to suppress" iznine dayanan kalıcı bastırma. Tek kelimelik ama bir kuralı değiştiriyor, 2a kapsamı dışı.
- (Faz 2a, code review) `NovaCore.FluentValidation`, `NovaCore.Serilog`, `NovaCore.OpenTelemetry` ve `NovaCore.Scalar` namespace'leri, `NovaCore.*` altındaki kodda üçüncü taraf kök namespace'leri gölgeliyor. Örneğin bir modülde kısmen nitelenmiş `FluentValidation.Results.X` artık `NovaCore.FluentValidation`'a bağlanıp derlenmez, `global::` gerekir. Bugünkü kod dosya başındaki using'lerle çalışıyor. Namespace adları D-020 (4)'te seçildi; değiştirmek public API kararı.
- (Faz 2a, code review) `PackageDependencyTests`'in csproj kontrolü `Directory.Build.*` ya da `GlobalPackageReference` ile eklenen paketleri görmüyor. `Include` yerine `Update` kullanan bir `PackageReference`'ta NRE veriyor. `bin/obj` altındaki csproj'ları da dışlamıyor. Assembly düzeyindeki test kısmen telafi ediyor.
- (Faz 2a, code review) `ExplicitRegistrationTests` ile `SourceHygieneTests` aynı "src altındaki .cs dosyaları, bin/obj hariç" kodunu tekrar ediyor. `Solution`'a bir yardımcı taşınabilir. Bu mevcut bir testi değiştireceği için 2a'da yapılmadı.
- ~~(Faz 2a, code review)~~ → Faz 3b'de çözüldü: eklentinin logger'ında `LoginFailed` da guard'lı. Eski not: `SecurityAuditLogger.LoginFailed` maskeyi guard'sız hesaplıyor, `UserRegistered` ise guard'lı. CA1873 varsayılan olarak yalnızca Information ve altına bakıyor (R-047). Davranış F2b.6'dan önceki gibi. Identity F3b.3'te emekliye ayrılacak.
- (Faz 2a, code review) DECISIONS'ta D-018…D-020 satırları D-017'nin üstüne eklendi, tablo ID sırasında değil. Düzeltmek korunan dosyada yalnızca biçimsel bir düzenleme.
- (Faz 2a) Kodsuz Api pack uyarısı hâlâ açık (Y-10 → Y-44 notu ve mediator cümlesi F5.5'te kapandı).
- (Faz 1) `.snupkg` GitHub Packages'a gitmiyor (R-039), yalnızca workflow artifact'i. İleride nuget.org seçilirse sembol sunucusu kullanılabilir.
- (Faz 1, F5.4) Package validation baseline'ı (`PackageValidationBaselineVersion`) `Microsoft.NET.ApiCompat.targets:18`'de değerlendirme anında okunuyor, import'u Directory.Build.targets'tan sonra geliyor. F5.4'te nereye yazılacağı doğrulanmalı (R-040). **F5.4'te ertelendi (D-039):** ilk sürüm önizleme, baseline 1.0 kararıyla gelir; nereye yazılacağı o gün doğrulanır.
- ~~(Faz 1) `NovaCore.Core.Web` paketinin nuspec bağımlılıklarında FluentValidation, Serilog, OpenTelemetry ve Scalar var (D-003 ihlali). F2a.2 ve F2c'nin işi.~~ → F2a.2'de çözüldü (Serilog/OTel/Scalar eklentilere, FluentValidation `NovaCore.FluentValidation`'a taşındı), F2a.4 testleri koruyor.
- ~~(Faz 2a, F2a.4 → F2c.1) **Y-20'nin modül kısmı henüz testli değil.**~~ → F2c.1e'de çözüldü: `ExplicitRegistrationTests` artık tüm `src`'yi tarıyor ve `*FromAssembly*` kayıt yardımcılarını da yakalıyor. EF'in `ApplyConfigurationsFromAssembly`'si model yapılandırması olduğu için hariç (D-023 (5)). Eski not: Modüller validator'ları `AddValidatorsFromAssembly` ile (assembly taraması) kaydediyor. Bu FluentValidation ile birlikte F2c.1'de gidiyor; o zaman `ExplicitRegistrationTests`'in taraması `src/Modules`'ü de kapsamalı. `ApplyConfigurationsFromAssembly` (EF model yapılandırması, DI kaydı değil) Y-20 kapsamında mı, o gün karar verilmeli.
- (Faz 2a, F2a.2c) `SecurityHeadersMiddleware` hâlâ `/scalar` yolunu CSP'den muaf tutuyor. Scalar artık bir eklenti; çekirdeğin eklentinin yolunu bilmesi küçük bir bağlılık. Davranış değişikliği olacağı için 2a'da dokunulmadı.
- (Faz 2a, F2a.2b) OpenTelemetry `service.version` olarak uygulamanın değil NovaCore eklentisinin assembly sürümünü (0.1.0.0) raporluyor. Davranış 2a'da korundu. Doğrusu muhtemelen giriş assembly'sinin sürümü.
- ~~(Faz 2b, code review) `GlobalExceptionHandler.Map`'te `BadHttpRequestException` için bir durum yok.~~ → F4c.4'te çözüldü: kendi durum kodunu dönüyor. Development'ta minimal API'ler bozuk gövdede bu exception'ı fırlatıyor (`ThrowOnBadRequest`), yanıt 400 yerine 500 oluyor ve Error seviyesinde log düşüyor. Bu 2b'den önce de böyleydi; status eşlemesini değiştirmek F2b.4'ün kapsamı dışında. Çözüm: `BadHttpRequestException.StatusCode`'u kullanmak.
- (Faz 2b, code review) DbContext health check'lerinde timeout yok. SQL Server'a erişilemezse `/health` bağlantı timeout'u kadar bekleyebilir. Uçlar yalnızca Development'ta olduğu için şimdi etkisi düşük. Production'da probe gerekince (D-022) `HealthCheckRegistration.Timeout` ile birlikte ele alınmalı.
- (Faz 2b, code review) Test yardımcıları tekrar ediyor: `NoOpEmailSender` iki dosyada (`EmailSenderRegistrationTests`, `ConfigurationValidationTests`), kullanıcı seed'i iki dosyada (`AuthFlowTests`, `RefreshTokenExpiryTests`). Paylaşılan bir test yardımcısına taşınabilir; mevcut testleri değiştirmek gerektiği için yapılmadı.
- (Faz 2b) `NovaCore.OpenTelemetry`, `OTEL_EXPORTER_OTLP_ENDPOINT`'i kayıt anında okuyor (`OpenTelemetryHostingExtensions.cs:35`). F2b.1'in kapsamı iki çekirdek paketti. Bu okuma `ConfigureAppConfiguration` ile ezilemiyor, ama MS ServiceDefaults da aynı kalıbı kullanıyor.
- (Faz 2c, R-055) **Aralıklı test hatası (framework yarışı):** Açılışta hata bekleyen testler, test yükü altında ara sıra gerçek hata yerine `ObjectDisposedException ('IServiceProvider')` alıyor. ~45 tam koşuda 2 kez görüldü.
  - Etkilenebilecek testler: `ConfigurationValidationTests` Theory'si, `EmailSenderRegistrationTests` (Production, sender yok), `DatabaseMigrationServiceTests` (`yes`).
  - Kök neden: `Mvc.Testing` `DeferredHost.StartAsync`, dispose edilmiş host'tan servis çözüyor.
  - F2c'den önce de vardı. CI'da da aralıklı kırmızıya yol açabilir.
  - Olası yollar: upstream'e bildirmek; açılış doğrulamasını `IStartupValidator` sarmalayıcısı ile yakalamak (hosted service hatalarını kapsamaz); bu testleri `WebApplicationFactory` dışında kurmak. Karar test altyapısını değiştireceği için kullanıcıya bırakıldı.
- ~~(Faz 2c)~~ → F5.5'te düzeltildi. README "Paket / Lisans Tablosu"ndaki `Microsoft.EntityFrameworkCore / Extensions.Hosting.Abstractions / Configuration.Binder` satırı eskimiş. EF paketi F2b'den beri Binder yerine `Options.ConfigurationExtensions` ve `Diagnostics.HealthChecks.EntityFrameworkCore` kullanıyor. F2c.4 aynı tabloda yalnızca FluentValidation satırını sildi.
- (Faz 2c, code review) **İki farklı validation 400 başlığı:** Yerleşik validation "One or more validation errors occurred." diyor (D-023 (4)). `Result` üzerinden dönen `ErrorType.Validation` (`ResultHttpExtensions.TitleFor`) ve `AppException` validation'ı (`GlobalExceptionHandler`) hâlâ "Validation failed" diyor. F2c'den önce ikisi de "Validation failed" idi. İki yanıtın `type`'ı zaten farklıydı (RFC 9110 / `httpstatuses.io`). Hizalamak public davranış değişikliği, F2c'nin kapsamı dışında.
- ~~(Faz 2c, code review → F5.3)~~ → Faz 4b'de çözüldü: `CatalogEndpointsTests` (ürün oluştur, oku, listele, güncelle, sil) ve `StockReservationTests` (POST'tan Completed'a ödeme). Eski not: Catalog ve Payments için geçerli gövdeyle uçtan uca test yok. `ValidationTests` yalnızca reddedilen gövdeleri sabitliyor, `PermissionAuthorizationTests`'teki POST 403'te duruyor. Geçerli girdiyi yanlışlıkla reddeden bir attribute değişikliği yakalanmaz. F5.3'ün (Catalog/Payments entegrasyon testleri) kapsamına giriyor.
- (Faz 2c, R-052) Record ctor parametrelerindeki DataAnnotations (`[MaxLength]`, `[Range]`, `[Length]`) OpenAPI şemasına yansımıyor. Faz sonunda belge main ile bayt bayt aynıydı. FluentValidation döneminde de şemada kısıt yoktu, yani bu bir gerileme değil. Kısıtları belgeye taşımanın yolu araştırılmadı.
- ~~(Faz 2c)~~ → F5.5'te düzeltildi. Y-10'un yerini Y-44 aldı, ama iki yer hâlâ Y-10 diyor: `tests/CLAUDE.md` ("never weaken one to pass (Y-10)") ve `Directory.Build.props`'taki `TreatWarningsAsErrors` yorumu. Kök CLAUDE.md'deki aynı not yukarıda (Faz 2a, F2a.5).
- (Faz 2b → Faz 3a; **F3a.1'de değerlendirildi, yapılmadı**, R-058: `TokenValidationParameters`'ta `TimeProvider` yok, tek yol varsayılan doğrulamanın yerini alan bir `LifetimeValidator` delegesi.) Token üretimi `TimeProvider`'dan okuyor, JwtBearer'ın lifetime doğrulaması (IdentityModel) ise gerçek saatten. Sahte saatli testler bu yüzden gerçek "şimdi"den başlıyor (R-050). F3a.1'de doğrulama yeniden yazılırken `TimeProvider` ile hizalanabilir.

- ~~(Faz 3a → F3b.3)~~ → F3b.3b'de çözüldü (D-028): üretim kodu ve Development simetrik modu kaldırıldı. Eski not: Çekirdekteki `ITokenService`/`JwtTokenService`/`AccessToken` ve `JwtOptions`'ın issuance alanlarının (`AccessTokenMinutes`, `RefreshTokenDays`) tek tüketicisi Identity modülü (D-024 (3)). Modülle birlikte kaldırılmalı. O gün HS256 üreten bir issuer kalmayacak; Development'taki simetrik doğrulamanın (`Jwt:SigningKey`) kalıp kalmayacağına karar verilmeli (ör. `dotnet user-jwts` için).
- ~~(Faz 3a, code review → 3b; **3b'de yapılmadı**, plan kapsamı dışında bırakıldı) **Super-admin bypass'ı token'daki rol claim'ine dayanıyor.**~~ → F4c.2'de çözüldü (D-036 (2)). `Administrator` rolünden çıkarılan bir kullanıcı, token'ın ömrü boyunca (Identity'de 15 dk) her izni geçmeye devam ediyor. İzinler ise cache TTL'iyle yansıyor. IdP 3b'de gelirken super-admin de sunucu tarafında çözülmeli (resolver'dan dönen bir işaret ya da rol).
- ~~(Faz 3a, code review)~~ → F3b.3b'de çözüldü: `Jwt:SigningKey` yok (D-028). Eski not: Development'ta hem `Jwt:Authority` hem `Jwt:SigningKey` varsa JwtBearer issuer'ları ve anahtarları bağsız birleştiriyor (R-056): Authority'nin issuer'ını taşıyan ama HS256 geliştirme anahtarıyla imzalanmış bir token kabul ediliyor. Yalnızca Development. Simetrik mod 3b'de yeniden değerlendirilince (yukarıdaki madde) birlikte ele alınmalı.
- (Faz 3a → 3b; **3b'de yapılmadı**: Api host'u tek Authority'ye, kendi sağlayıcısına güveniyor, Keycloak ise yerine geçen bir seçenek) İzin önbelleğinin anahtarında `iss` yok (PLAN: `sub` + stamp). OIDC'de `sub` yalnızca issuer içinde tekil. Tek Authority ile sorun yok, ama Development/test'te iki issuer (test IdP + Identity) ve 3b'de OpenIddict + Keycloak birlikte güvenilirse `sub` çakışabilir. İkinci bir issuer desteklenirken anahtara `iss` eklenmeli.
- (Faz 3a, code review) İzin önbelleğinin değeri `string[]`. HybridCache bunu değişmez saymadığı için her L1 okumasında JSON'dan yeniden üretiyor, yani her kimlikli istekte küçük bir deserialize maliyeti var. `[ImmutableObject(true)]` işaretli bir tip bunu önler, ama L2 (Redis) eklenince serileştirilebilir kalması doğrulanmalı.
- (Faz 3a, code review) `NovaCoreApiFactory.Dispose`, `TestIdentityProvider`'ı (ve SQLite bağlantılarını) host durdurulmadan önce dispose ediyor. Düşük risk; kapanışta metadata yenilenirse `ObjectDisposedException` görülebilir.
- (Faz 3a, code review; **3b'de kısmen çözüldü**: Identity kullanıcısı oluşturma artık tek yerde, `IdentityServerTestHost.CreateUserAsync`; Catalog şeması oluşturma hâlâ üç yerde) Test yardımcıları tekrarı büyüdü: Identity kullanıcısı oluşturma artık üç yerde (`AuthFlowTests`, `RefreshTokenExpiryTests`, `RoleChangeTests`), "Catalog şemasını oluştur" iki yerde (`PermissionAuthorizationTests`, `JwtValidationTests`). Yukarıdaki Faz 2b maddesiyle birlikte ele alınmalı.
- ~~(Faz 3a → 3b) Keycloak rolleri~~ → F3b.4'te örnek realm'deki mapper rolleri `role` claim'ine yazıyor, F3b.5a'da OpenAPI belgesi OAuth2 akışını yayımlıyor. Kalan kısım (Keycloak için permission resolver) aşağıda, Faz 3b maddelerinde. Eski not: Keycloak rolleri düz `role` claim'i olarak değil `realm_access.roles` içinde geliyor. Super-admin kontrolü ve bir Keycloak resolver'ı bunları eşlemeli. OpenAPI belgesi hâlâ düz Bearer şeması yayımlıyor; Scalar için OAuth2/OIDC akışı F3b.5'in işi.
- (Faz 3a) DECISIONS D-024 (7) "eksikse DI doğrulaması açılışı durdurur" diyor. Code review'dan sonra açılışı her ortamda `PermissionResolverStartupCheck` durduruyor (DI doğrulaması yalnızca Development'ta). Sonuç aynı, mekanizmanın adı farklı. Satırı düzeltmek korunan dosyada kullanıcı onayı ister.

- (Faz 3b, plan kapsamı dışı) Keycloak ya da başka bir harici sağlayıcı için `IPermissionResolver` yok. README ve compose yorumu host'un kendisinin kaydetmesini söylüyor. İki issuer'a birlikte güvenilecekse izin anahtarına `iss` eklenmeli (yukarıdaki Faz 3a maddesi).
- ~~(Faz 3b, plan kapsamı dışı) Parola sıfırlama yok (eski modülde de yoktu). Onaylanmamış hesaba yeniden onay e-postası gönderme ucu da yok.~~ → F4d.2'de yapıldı.
- ~~(Faz 3b, plan kapsamı dışı) OpenIddict token ve authorization kayıtları hiç temizlenmiyor (Quartz entegrasyonu ya da `PruneAsync` çağıran bir hosted service). Tablolar zamanla büyür.~~ → F4d.5'te çözüldü (`ExpiredTokenPruner`).
- (Faz 3b, plan kapsamı dışı) userinfo ucu, Razor/Identity UI, OpenIddict.Validation ve reference token yok (D-026).
- (Faz 3b) Test süresi: eklenti testleri `DisableParallelization` koleksiyonunda ve PBKDF2 600k ile yaklaşık 1 dk 20 sn. Tüm paket yaklaşık 2 dk. Testlerde iterasyon sayısını düşürmek hızlandırır, ama sertleştirme testini ayrı tutmak gerekir.
- ~~(Faz 3b, code review) Giriş sayfasında zamanlama farkı: bilinmeyen e-posta PBKDF2'siz hemen dönüyor, bilinen e-posta 600k iterasyon harcıyor. Aynı mesaja rağmen yanıt süresi hesabın varlığını ele veriyor (eski modülde de vardı). Çözüm: bilinmeyen kullanıcıda sahte bir hash doğrulamak. Kayıt ucunda da benzer bir fark var (yeni hesapta hash + e-posta).~~ → F4d.4'te giriş sayfası için çözüldü (sahte hash). Kayıt ucundaki ve parola sıfırlama formundaki fark kalıyor (aşağıdaki F4d.2 maddesi).
- ~~(Faz 3b, code review) Kurtarma kodları kullanılamıyor: `/2fa/enable` 10 kod veriyor ama giriş sayfasının kod adımı yalnızca authenticator kodunu kabul ediyor (`TwoFactorRecoveryCodeSignInAsync` çağrılmıyor). Eski modülde de böyleydi.~~ → F4d.3'te çözüldü.
- ~~(Faz 3b, code review) `change-password`'da yanlış mevcut parola lockout sayacını artırmıyor. Çalınmış bir access token'la yalnızca IP rate limit'iyle sınırlı bir parola denemesi yapılabiliyor (eski modülde de vardı).~~ → F4d.4'te çözüldü.
- ~~(Faz 3b, code review) `register`, `User` rolü yoksa (seed kapalıyken, Development varsayılanı) kullanıcıyı oluşturduktan sonra 500 veriyor. Onay e-postası gitmiyor, tekrar kayıt genel 200'le dönüyor; adres takılı kalıyor (eski modülde de vardı).~~ → F4d.2'de çözüldü: rol yoksa hesap rolsüz oluşuyor; takılan adres için yeniden gönderme ucu var.
- (Faz 3b, code review) Refresh'te lockout kontrolü: e-postayı bilen biri 5 yanlış parolayla kurbanın bütün refresh oturumlarını düşürebiliyor. OpenIddict örneği yalnızca `CanSignIn`'e bakıyor. Bu bir tasarım ödünleşimi; kaldırmak D-029'a eklenecek bir karar ister.
- ~~(Faz 3b, code review) `/connect/endsession`, `id_token_hint` ya da onay adımı olmadan GET'le oturumu kapatıyor (logout CSRF). OpenIddict örnekleri bir onay formu gösteriyor.~~ → F4d.4'te çözüldü.
- (Faz 3b, code review, altitude) Eklenti token'a kopyaladığı principal'dan `permission` claim'lerini ayıklıyor, çünkü çekirdeğin `IClaimsTransformation`'ı her şemada (cookie, OpenIddict) çalışıyor ve her `/connect/token`'da resolver'a gidiyor. Kök çözüm izinleri yalnızca bearer principal'ı için çözmek (ör. `JwtBearerEvents.OnTokenValidated`). Bu R-057 ve D-029 (3)'ü yeniden açar; çekirdek değişikliği.
- (Faz 3b, code review) Seed her açılışta gizli client'ların secret'ını yeniden hash'leyip yazıyor (`UpdateAsync(app, descriptor)` düz secret'ı hash'le karşılaştırıyor).
- (Faz 3b, code review) `IdentityPermissionResolver` rol başına iki sorgu atıyor (N+1). Cache'li; eski modülden geldi.
- (Faz 3b, code review; **F4c.3'te kısmen çözüldü**: güvenilen proxy yapılandırılınca istek şeması https olur ve `SameAsRequest` cookie'ler `Secure` alır) `SecurePolicy = Always` yalnızca uygulama cookie'sinde. 2FA kullanıcı cookie'si ve antiforgery cookie'si varsayılanda. Forwarded headers'sız bir TLS proxy'si arkasında `Secure` bayrağı olmadan çıkıyorlar (yukarıdaki Faz 0 `ForwardedHeaders` maddesiyle ilgili).
- (Faz 3b, F3b.5b, R-066) Scalar'da OAuth2 şeması seçili ama token'sız kalınca elle eklenen `Authorization` başlığı işe yaramıyor (401). Normal kullanımda Scalar popup'la token alıyor; built-in browser popup'ı aynı sekmede açtığı için bu tarayıcıda denenemedi. Gerçek bir tarayıcıda Scalar'ın popup akışı bir kez elle denenmeli.

- (Faz 4a, plan kapsamı dışı) **Birden çok uygulama örneği:** dispatcher veritabanı başına tek örnek varsayıyor. İki host aynı outbox'u okursa aynı mesajı ikisi de teslim edebilir. Çözüm bir claim/lease olabilir (örneğin koşullu `ExecuteUpdate` ile kilit süresi). SQLite `DateTimeOffset` karşılaştırmasını çeviremediği için süre alanının tipi test tarafını da etkiler (R-068).
- ~~(Faz 4a, plan kapsamı dışı) Outbox ve `outbox_consumers` satırları hiç silinmiyor.~~ → F4c.7'de çözüldü: işlenmiş mesajlar 7 gün sonra kayıtlarıyla siliniyor (OpenIddict kayıtları F4d.5'te). MS Idempotent Consumer tekrar teslim penceresinden uzun bir saklama süresi öneriyor. İşlenmiş mesajları ve kayıtlarını belli bir süre sonra silen bir temizlik gerekecek (OpenIddict kayıtları için olan maddeyle birlikte ele alınabilir).
- (Faz 4a, plan kapsamı dışı) Yeniden deneme her turda (5 sn) yapılıyor, geri çekilme (backoff) yok. 10 deneme yaklaşık 50 saniye sürüyor; bundan uzun süren bir bağımlılık kesintisi mesajı incelemeye düşürür. Aralık, parça boyu ve deneme sayısı sabit (Y-19). Bir tüketici ihtiyaç duyarsa options'a taşınabilir.
- (Faz 4a → 4b; **4b'de yapılmadı**) Teslim gecikmesi en fazla bir tur (5 sn); event zincirinde her halka bir tur ekliyor. 4b event akışını seçti (D-034), bu yüzden ödemenin sonucu en fazla iki tur (yaklaşık 10 sn) sonra görünüyor. Kayıttan sonra dispatcher'a sinyal veren anında teslim bir çekirdek değişikliği; 4b'nin kapsamı dışında. *Koşul:* bir tüketici bu gecikmeyi sorun eder.
- ~~(Faz 4a) `IDomainEventDispatcher`'ın `src`'de çağıranı yok.~~ → F4c.7'de kaldırıldı. PLAN F4a.3 gereği düzeltildi ve korunuyor, ama entity event'leri outbox'tan dağıtılıyor. Faz 5'te (ya da kullanıcı kararıyla) kaldırılması değerlendirilebilir.
- (Faz 4a, R-068) `src/Modules/CLAUDE.md` Guid v7 anahtarları için "friendly to SQL Server clustering" diyor. SQL Server `uniqueidentifier`'ı son 6 bayta göre sıralıyor, v7'nin zaman damgası ise baştaki baytlarda; yani anahtarlar SQL Server sırasında artan değil ve Catalog/Payments'ın clustered PK'ları rastgele sırayla dolar. Çözüm için belge düzeltmesi ya da anahtar stratejisi kararı gerekir (örneğin `SequentialGuidValueGenerator`). Kapsam dışı. Faz 4b'de eklenen `stock_reservations` da aynı durumda: anahtarı ödemenin Guid v7'si (code review #13).
- (Faz 4a) Outbox interceptor'ını `AddModuleDbContext` ekliyor, audit interceptor'ını ise hâlâ her modül kendisi ekliyor. İkisini aynı yere toplamak modül kaydını kısaltır; modül ve CLAUDE.md değişikliği gerektiği için yapılmadı.
- (Faz 4a, code review #3) Interceptor event tamponunu event'ler kayda katılırken temizliyor. Kayıt başarısız olur ve aynı entity örneği *yeni* bir context'e eklenirse event'leri kalmamış olur. Örnek: execution strategy'yle elle yazılmış, entity'yi blok dışında oluşturan bir yeniden deneme bloğu. Belgelendi (aynı context'te `SaveChanges`'i yeniden dene). Kalıcı çözüm için tamponu `SavedChanges`'te temizlemek ve zaten izlenen `EventId`'leri atlamak gerekir; açık transaction'da commit hatası yine kapsanmaz.
- (Faz 4a, code review #5) Başarısız bir tur her tick'te (5 sn) modül başına Error logluyor ve geri çekilmiyor. Örnek: migration uygulanmamış bir geliştirme veritabanında dakikada 24 hata logu. Context başına artan bekleme ya da seri başına tek log düşünülebilir.
- (Faz 4a, code review #8) Generic bir event'in saklanan tip adı, tip argümanlarının sürümlü assembly-qualified adlarını taşıyor. Sürümü düşüren bir dağıtımdan sonra `Type.GetType` bağlanamayabilir. Çok uzun bir ad 512 karakteri geçerse iş kaydının kendisi SQL Server'da düşer. Belgelendi; sürümsüz ad üretmek `TypeName` ile ayrıştırma gerektirir.
- (Faz 4a, code review #9) Modül outbox'ları bir turda sırayla işleniyor. Erişilemeyen bir modül veritabanı (`EnableRetryOnFailure` geri çekilmesiyle) diğer modüllerin teslimini dakikalarca bekletebilir. Modüller aynı SQL Server'ı kullandığı sürece etkisi yok.
- (Faz 4a → ~~Faz 5 sonu~~ → **Faz 6e sonu, unutulmamalı**; plan 6e'ye uzadığı için D-032'nin "bütün fazlar bitince" koşulu oraya kaydı, F6a.1) D-032: `docs/plan/DECISIONS.md` `.claude/hooks/package-policy.json` `protectedPaths` listesinden kullanıcı tarafından geçici olarak çıkarıldı. Bütün fazlar bitince satır geri eklenir (hook dosyası olduğu için kullanıcı ekler ya da onaylar); DECISIONS başlığındaki "hook onay ister" notu o zaman yeniden geçerli olur.
- ~~(Faz 4a, code review #12) `outbox_consumers`'ın clustered anahtarı (uniqueidentifier, nvarchar(256)) geniş ve SQL Server sırasında rastgele.~~ → F4c.7'de çözüldü (D-036 (7)). Outbox tablosu için R-068'de kullanılan gerekçenin aynısı geçerli. Mesaj satırının `bigint Id`'si + Handler (FK ve cascade delete ile, temizlik için de uygun) daha iyi olur. D-031 (5)'i değiştirir; tablo boşken, ilk kullanımdan önce yapmak ucuz.

- (Faz 4b, F4b.2b) `GET /api/v1/payments` SQLite test veritabanında 500 dönüyor: liste `CreatedAtUtc`'ye (DateTimeOffset) göre sıralıyor, SQLite bunu çeviremiyor (R-068). SQL Server'da sorun yok, ama listenin başarı yolu testlenmiyor. Çözüm yolları: test context'lerinde DateTimeOffset'i sıralanabilir bir türe çeviren bir dönüştürücü ya da gerçek SQL Server'la test (Testcontainers, paket onayı gerekir).
- (Faz 4c, D-036 (8)) Catalog, Payments ve Api'de `System.Security.Cryptography.Pkcs` 10.0.13'e doğrudan referans var: Microsoft.Data.SqlClient açıklı 9.0.11'e bağlı (R-083). *Koşul:* EF Core SqlServer'ın getirdiği SqlClient düzeltilmiş bir Pkcs'e (≥ 9.0.21 / 10.0.13) bağlanınca referans ve CPM satırı kalkar; `dotnet nuget why <modül> System.Security.Cryptography.Pkcs` ile kontrol edilir.
- (Faz 4c, code review #4) Outbox'ın saklama taraması her turda (5 sn) her modül context'inde bir sorgu atıyor; silinecek mesajlar 7 günlük ölçekte birikiyor. *Koşul:* veritabanı yükü ölçülür ve sorun olursa tarama seyrekleştirilir (ör. saatte bir, `TimeProvider`'la).
- (Faz 4c, code review #5) `AdminEndpoints`, `AccountEndpoints.MessageResponse`'u kullanıyor (iç içe record). Ortak bir yanıt tipi olarak üst düzeye taşınabilir; yalnızca okunabilirlik.
- (Faz 4d, F4d.2, R-084; code review #1) Parola sıfırlama formunda ve `resend-confirmation-email`'de zamanlama farkı: e-posta alan hesapta (onaylı / onaysız) token üretilip e-posta gönderiliyor, diğerlerinde hemen dönülüyor. Yanıt aynı, süre gerçek bir SMTP ile farklı olabilir (OWASP "consistent amount of time"). Kayıt ucundaki farkla aynı tür. *Koşul:* gerçek göndericiyle ölçülebilir bir fark ya da bir sızma testi bulgusu. Çözüm e-postayı kuyruğa ya da outbox'a almak.
- (Faz 4d, code review #5) `/connect/endsession`'ın sorusunda yalnızca "Sign out" düğmesi var; vazgeçen kullanıcıyı uygulamaya geri götüren bir yol yok (OpenIddict'in Velusia örneğinde de yok). *Koşul:* kullanıcı geri bildirimi. Client'ın kayıtlı adresine dönen bir link `post_logout_redirect_uri`'yi oturum kapanmadan kullanırdı; bu bir tasarım kararı ister.
- (Faz 4d, code review #7) Parolanın en fazla 128 karakter sınırı iki yerde: `AccountRequests`'teki `[Length(12, 128)]` ve `PasswordResetPage.MaxPasswordLength`. *Koşul:* sınır değişirse ikisi birlikte değişmeli; ortak bir sabit attribute'larda da kullanılabilir.
- (Faz 4d, F4d.3) Kurtarma kodları azaldığında uyarı yok ve kodları yenilemenin ayrı bir ucu yok; bugün yenilemek için 2FA kapatılıp yeniden açılıyor. *Koşul:* kullanıcılar kodları tüketmeye başlar ya da ürün ister.
- (Faz 4d, F4d.2) Parola değişti/sıfırlandı bildirim e-postası yok (OWASP Forgot Password önerisi). *Koşul:* ürün ister ya da bir güvenlik incelemesi bulgusu.
- (Faz 4b, D-035 (6)) Failed bir ödemenin nedeni istemciye dönmüyor; neden yalnızca Catalog'daki rezervasyon satırında (`Reason`) duruyor. *Koşul:* bir istemci nedeni göstermek ister.
- (Faz 4b) İade ve iadede stoğun geri verilmesi yok (`Refund` kullanılmadığı için silindi). *Koşul:* iade bir ürün gereksinimi olur. Rezervasyonu serbest bırakan bir event ve rezervasyona yeni bir durum gerekir.
- (Faz 4b, code review #2) Pending'de kalan bir ödeme için zaman aşımı ya da telafi yok. Catalog'un handler'ı 10 denemede başarısız olursa mesaj incelemeye bırakılır ve ödeme Pending kalır. *Koşul:* üretimde görülür. Pending ödemelerin yaşını izlemek ya da bir telafi adımı gerekir.
- (Faz 4b) `Product.Deactivate`/`Reactivate`'i çağıran bir uç yok; akıştaki `product.inactive` reddi bugün yalnızca testte oluşuyor. Genel `Product` kuralları D-033 ile kapsam dışında.
- (Faz 4b, code review #3) `EnableRetryOnFailure`, commit'in sonucu belirsiz kalınca `SaveChanges`'i yeniden oynatıyor. Stok belirteci yüzünden yeniden oynatılan UPDATE/DELETE 0 satır buluyor ve `UpdateProduct`/`DeleteProduct`, yazım aslında uygulanmış olsa bile 409 dönüyor. Bu MS connection-resiliency rehberindeki "Option 1: do (almost) nothing" ile kabul edildi: nadir bir durum ve istemci yeniden okuyunca kendi yazımını görüyor. *Koşul:* gözlenirse `ExecuteInTransaction` + `verifySucceeded`.
- (Faz 4b, code review #5) Satır eşzamanlı olarak silinmişse de 409 `product.concurrency_conflict` dönüyor; 404 daha doğru olurdu. Çakışmada `exception.Entries[0].GetDatabaseValuesAsync()` null dönüyorsa 404 verilebilir. İstemci yeniden okuyunca zaten 404 alıyor.
- (Faz 4b, code review #9) `DbUpdateConcurrencyException`'ın 409'a çevrilmesi, `Product`'ı yazan her uçta ayrı ayrı yapılıyor. Bunu unutan yeni bir uç 500 döner. *Koşul:* `Product`'ı yazan üçüncü bir uç gelir. F4c.4'ten beri kanca var: modül kendi `IExceptionHandler`'ını kaydedebilir ve o handler çekirdeğin eşlemesinden önce çalışır. Çeviri o gün oraya taşınır.

- (Faz 5) `PermissionResolverStartupCheck`'in hata mesajı "register the identity integration's resolver (e.g. the Identity module's)" diyor; Identity modülü Faz 3b'de kalktı. Çekirdek kodu, Faz 5'in kapsamı dışında. *Koşul:* çekirdeğe bir sonraki dokunuş; mesaj eklentinin resolver'ını ya da projenin kendi resolver'ını söylemeli.
- (Faz 5, D-040 (2)) Repo'nun Api host'u Serilog'u (log) ve OpenTelemetry'yi (metrik, trace) birlikte kullanıyor; şablon yalnızca OpenTelemetry. Api host'u Serilog eklentisinin tek tüketicisi. *Koşul:* referans host'un şablona benzemesi istenirse ya da Serilog eklentisinin bir tüketicisi kalmazsa.
- (Faz 5, D-040 (4)) `--auth External`'da örnek bir `IPermissionResolver` yok; proje kendi resolver'ını yazar, yoksa host açılmaz. *Koşul:* iki proje aynı türde harici IdP resolver'ına (ör. token'daki rollerden izin eşlemesi) ihtiyaç duyar.
- (Faz 5) Şablonun Development dışı probe örneği (`UseHealthChecks(path, port)`) testle korunmuyor: TestServer'da bağlantının yerel portu yok. *Koşul:* ilk container dağıtımında bir kez elle denenir.
- (Faz 5, R-092) Üretilen projenin CI'ındaki bundle adımı yalnızca varsayılan dala push'ta çalışıyor ve repo'nun `template-smoke` job'ı onu koşturmuyor (yalnızca yerelde denendi). *Koşul:* `content/novacore/.github/workflows/ci.yml` değişirse adımlar üretilen bir projede yeniden denenir (src/Templates/CLAUDE.md).
- (Faz 5) Üretilen projede `using`'ler kaynak adına göre sıralanmıyor (ör. `using Siparis.Modules.Todos;` `NovaCore.*`'dan önce kalıyor); şablon `MyService`'e göre sıralı. Analyzer bunu hata saymıyor. Kozmetik.
- (Faz 5, code review #4) Şablonun örnek `CompleteTodo`'su okuyup `IsCompleted`'ı kontrol ediyor ve concurrency token olmadan kaydediyor; aynı anda gelen iki tamamlama ikisi de 204 alır (biri 409 almalıydı). Örnek basit tutuldu; token eklemek migration'ı ve örneği büyütür. *Koşul:* örnek modül bir eşzamanlılık kalıbı da öğretecekse (repo'da `Product.Stock` örneği var).
- (Faz 5, code review #5) Üretilen projenin CI'ı restore için `NOVACORE_PACKAGES_TOKEN` secret'ını ister; fork'tan gelen ve Dependabot PR'larında secret olmadığı için restore 401 alır. *Koşul:* üretilen bir projede Dependabot ya da dış katkı açılır (Dependabot secret'ı ya da `pull_request_target` tasarımı gerekir).
- (Faz 5, code review #6) Repo CI'ında `template-smoke` job'ı çözümü `build-and-test`'ten ayrı olarak bir kez daha derleyip pack ediyor. *Koşul:* CI süresi sorun olur; pack edilmiş paketler artifact olarak job'lar arasında taşınabilir.

- (Faz 6a, F6a.6) **CodeQL'in ilk 4 uyarısı (6b girdisi), 2026-10-21, faz branch'i:**
  - `cs/user-controlled-bypass` (high) ×2: `NovaCore.Identity.OpenIddict/LoginPage.cs:53` ve `:125`. "Hassas bir işlemi koruyan koşulu kullanıcının verdiği bir değer belirliyor."
  - `cs/exposure-of-sensitive-information` (medium) ×2: `SecurityAuditLogger.cs:14` ve `:25`. E-posta adresi loga yazılıyor.
  - Hiçbiri henüz doğrulanmadı. *Koşul:* Faz 6b'de her biri ya kırmızı bir testle doğrulanıp düzeltilir ya da gerekçesiyle "false positive" olarak kapatılır (D-044'ün triage kuralı).

## Son devir notu

> Her oturumun sonunda (faz bitmese bile) üzerine yazılır. Bir sonraki oturumun bilmesi gerekenler: nerede kalındığı, neyin yarım kaldığı, sürprizler ve bekleyen sorular.

**2026-10-20, Faz 5: Şablon, dağıtım, yayın hazırlığı (tamam, `main`'e merge ve push edildi, `v0.1.0-preview.1` yayımlandı).** Branch `faz/5-sablon-yayin`, 8 commit.

- **Yapılanlar:**
  - F5.4: kanal GitHub Packages (D-013 kalır), ilk sürüm `0.1.0-preview.1` (Q-32, Q-33 → D-039).
  - F5.1a: tek log hattı OpenTelemetry; `AddCoreObservability` log'ları biçimlenmiş mesaj ve scope'larla taşıyor (R-089).
  - F5.1b: template package `BMD.NovaCore.Templates` (`src/Templates/NovaCore.Templates`): Api host, `Todos` örnek modülü, test projesi, `nuget.config` (source mapping), kontrol listeli `appsettings`, probe örneği, `--auth BuiltIn|External`. Kökteki `.template.config` silindi. Bütün paket ID'leri `BMD.NovaCore.*` (Q-34 → D-041).
  - F5.1c: üretilen projenin CI'ı (migration kontrolü, varsayılan dalda bundle'lar).
  - F5.2: `eng/template-smoke.sh` ve CI'da `template-smoke` job'ı.
  - F5.5: README'de tüketici rehberi, CHANGELOG `[0.1.0-preview.1]`, CLAUDE.md'ler (yeni `src/Templates/CLAUDE.md`), memory.
  - Code review düzeltmeleri. Testler 276 → 277 (repo); üretilen projelerde BuiltIn 8, External 7. Kararlar D-039, D-040 (1)–(9), D-041; R-089…R-092.
- **Sürprizler:**
  - `UseOtlpExporter` log'ları zaten açıyordu; eksik olan mesaj ve scope'lardı (R-089).
  - nuget.org'da başka sahiplerin `NovaCore.Templates`, `NovaCore.AspNetCore`, `NovaCore.EntityFrameworkCore` paketleri var; `dotnet new`'in güncelleme kontrolü yabancı şablonu önerdi. Paket ID'leri `BMD.NovaCore.*` oldu (R-091).
  - `ContentTargetFolders=content` içeriği `content/content` altına koydu (R-090).
  - Bundle'lar context başına 82 MB (framework-dependent) / 180 MB (self-contained); varsayılan dalla sınırlandı (R-092).
- **Kullanıcıya:**
  - Merge ve push: kullanıcı onayıyla ("Merge + push, tag CI'dan sonra") `main`'e `--no-ff` merge edildi ve push'landı (2026-10-20). Push'taki CI (run 39477442933) iki job'da da yeşil: Linux'ta pack ve şablon smoke testi ilk kez doğrulandı. Kullanıcı onayıyla `v0.1.0-preview.1` tag'i atıldı; release.yml (run 39478498854) yeşil, sekiz paket (`BMD.NovaCore.*` yedi kütüphane + `BMD.NovaCore.Templates`) GitHub Packages'a yüklendi. Tag `release.yml`'i tetikler ve sekiz paketi GitHub Packages'a yükler; Linux'ta pack'in ilk doğrulaması da o zaman (ve push'taki `template-smoke` job'ında) olur.
  - **D-032:** `docs/plan/DECISIONS.md`'yi `.claude/hooks/package-policy.json` `protectedPaths` listesine geri ekleyin (hook dosyasını model düzenleyemiyor). DECISIONS başlığındaki "hook onay ister" notu o zaman yeniden geçerli olur.
  - Tüketen projeler için: GitHub'da `read:packages` yetkili bir token; yerelde `dotnet nuget add source ... -n novacore`, üretilen repoda `NOVACORE_PACKAGES_TOKEN` secret'ı.
  - Çalışma ağacındaki izlenmeyen `AGENTS.md`, `.agents/` ve `.codex/` dosyalarına dokunulmadı.
- **Sonrası:** plan bitti. İlk gerçek proje şablonla açılır; 1.0 kararı (PublicAPI.Shipped, package validation baseline) o proje bu sürümle çalıştıktan sonra ayrıca verilir (PLAN F5.4). "Sonraya bırakılanlar"da Faz 5'ten 9 madde var.
