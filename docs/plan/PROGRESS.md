# İlerleme

> Tek doğru durum kaynağı. `/novacore-phase` her başlangıçta bu dosyayı okur ve her adımdan sonra günceller.
> Adım tanımları [PLAN.md](PLAN.md)'dedir. Burada yalnızca durum, commit hash'i ve notlar tutulur.

## Fazlar

| Faz | Durum | Branch | Başlangıç | Bitiş | Merge |
|---|---|---|---|---|---|
| 0 | ✅ tamam | `faz/0-stabilizasyon` | 2026-09-25 | 2026-09-25 | 2026-09-25 (kullanıcı onayı, `--no-ff`) |
| 1 | 🔄 devam ediyor | `faz/1-kutuphane-altyapisi` | 2026-09-25 | | |
| 2a | ⏳ başlamadı | | | | |
| 2b | ⏳ başlamadı | | | | |
| 2c | ⏳ başlamadı | | | | |
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
- [x] F1.4 (HASH): `Microsoft.CodeAnalysis.PublicApiAnalyzers` 5.6.0 (CPM) eklendi, 4 packable csproj'da `PrivateAssets=all`.
  - Paket `PublicAPI.*.txt`'yi kendisi eklemiyor (R-042). `AdditionalFiles` item'ları `Directory.Build.targets`'ta.
  - Shipped dosyaları yalnızca `#nullable enable` içeriyor. Tüm API Unshipped'te, `dotnet format` + RS0016 code fix ile üretildi: Abstractions 149, Infrastructure 53, Web 122, Contracts 22 satır.
  - **Sapma:** RS0026 → `ToHttpResult` 4 overload'a bölündü (Q-20, D-015). Çağrılar aynen derleniyor.
  - Kabul denemesi: geçici bir public üye RS0016 hatası verdi, commit'lenmedi.
  - Build 0/0, 46/46 yeşil.
- [ ] F1.5 Pack doğrulaması
- [ ] F1.6 Yayın akışı

### Faz 2a
- [ ] F2a.1 Çekirdek projeler
- [ ] F2a.2 Eklenti paketleri
- [ ] F2a.3 Açık modül kaydı
- [ ] F2a.4 Mimari testler
- [ ] F2a.5 Dokümanlar
- [ ] F2a.6 (opsiyonel) İlk önizleme sürümü

### Faz 2b
- [ ] F2b.1 Options pattern
- [ ] F2b.2 Test host sadeleştirme
- [ ] F2b.3 TimeProvider
- [ ] F2b.4 Exception handling
- [ ] F2b.5 Health
- [ ] F2b.6 LoggerMessage

### Faz 2c
- [ ] F2c.1 Validation
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
| Q-03 | GitHub remote/repo var mı? CI ve paket feed'i için | F0.8, F1.6 | ~~Henüz yok~~ → **Var** (2026-09-25, Faz 0 merge'ünden sonra, kullanıcı isteğiyle): `https://github.com/alimuhammedak/NovaCore`, **private**, yalnızca `main` push'landı. İlk CI koşusu (run 36098557732) yeşil. F1.6: remote artık var, paket feed'i ve yayın tetiklemesi F1.6'da ayrıca sorulmalı. |
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

## Son devir notu

> Her oturumun sonunda (faz bitmese bile) üzerine yazılır. Bir sonraki oturumun bilmesi gerekenler: nerede kalındığı, neyin yarım kaldığı, sürprizler ve bekleyen sorular.

**2026-09-25, Faz 0 bitti** ve kullanıcı onayıyla `faz/0-stabilizasyon` main'e merge edildi (`--no-ff`).

- **Yapılanlar:**
  - Git kuruldu. Baseline `849a30d` main'de.
  - LICENSE ve CHANGELOG eklendi. docker-compose SA parolası `.env`'e taşındı (D-008).
  - Paketler 10.0.12'ye çıktı ve iki yüksek açık kapandı. NU1903/4 artık hata.
  - "strict" rate limiter IP bazlı oldu (D-009).
  - `DomainEvent` base record eklendi.
  - `LoggingEmailSender` yalnızca Development'ta kayıtlı, başlangıçta kontrol var.
  - Scrutor, Asp.Versioning ve OutputCache kaldırıldı.
  - DECISIONS yasakları için 8 mimari test eklendi.
  - CI workflow yazıldı.
  - Testler 33'ten 46'ya çıktı, build 0 uyarı.
- **Sürprizler:**
  - `TestServer.SendAsync` gövdeyi algılamıyor. Testte `IHttpRequestBodyDetectionFeature` ayarlanıyor.
  - Microsoft.AspNetCore.OpenApi generator'ü assembly'lerimize file-local tipler gömüyor. Y-29 testi bu yüzden `NovaCore.*` namespace'iyle sınırlı.
  - `dotnet list --vulnerable` açık bulsa da exit 0 dönüyor (R-027).
  - PowerShell 5.1'de commit mesajındaki çift tırnaklar düşüyor. Bu yüzden F0.3 mesajında `strict` tırnaksız kaldı. Artık `git commit -F <dosya>` kullanılıyor.
- **Sonraki fazın bilmesi gerekenler:**
  - Test factory'lerinde eager config için `CreateHost` + `ConfigureHostConfiguration` (R-026), ortam için `EnvironmentName` override edilir.
  - Test factory'leri artık açık `ValidateScopes`/`ValidateOnBuild` ile kuruluyor.
  - Kök CLAUDE.md'deki Mediator notu Y-01 ile çelişiyor ("Sonraya bırakılanlar").
- **Remote:** `alimuhammedak/NovaCore` (private) oluşturuldu, `main` push'landı. İlk CI koşusu yeşil, `jq` adımı da gerçek ortamda doğrulandı (Q-03).
- **Sıradaki:** yeni oturumda `/novacore-phase 1`.
