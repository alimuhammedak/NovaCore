# Kararlar ve yasaklar

> **Bu dosya korunur.** Buradaki hiçbir madde kullanıcı onayı olmadan eklenmez, değiştirilmez ya da silinmez; hook her düzenlemede onay ister.
> Öncelik sırasında en üsttedir: PLAN.md veya konuşma bu dosyayla çelişirse bu dosya geçerlidir. Çelişkiyi fark edersen dur ve kullanıcıya sor.

## Kararlar (D)

Kayıt biçimi: `D-NNN | tarih | karar | gerekçe/kaynak | onaylayan`

| ID | Tarih | Karar | Gerekçe / kaynak | Onay |
|---|---|---|---|---|
| D-001 | 2026-09-24 | **Dağıtım modeli:** BuildingBlocks, SemVer ile versiyonlanan NuGet paketleri olarak dağıtılır. `dotnet new` template'i yalnızca bu paketlere referans veren ince bir host iskeleti üretir. | Library guidance: "DO distribute your library using a NuGet package" (R-009) | Kullanıcı |
| D-002 | 2026-09-24 | **Kimlik:** Çekirdek yalnızca standart JWT doğrulaması yapar (Authority/JWKS, asimetrik anahtar). Token'ı ayrı bir IdP üretir: OpenIddict eklenti paketi veya harici bir IdP (Keycloak/Entra). Parola karşılığında kendi JWT'sini üretme yaklaşımı bırakılır. | MS JWT rehberi: asimetrik anahtar kullanılmalı, username/password'den token üretilmemeli (R-010) | Kullanıcı |
| D-003 | 2026-09-24 | **Bağımlılık politikası:** Çekirdek paketler (`NovaCore.Abstractions`, `NovaCore.AspNetCore`, `NovaCore.EntityFrameworkCore`) yalnızca BCL, ASP.NET Core ve `Microsoft.*` paketlerine bağlanır. Üçüncü taraf paketler (Serilog, OpenTelemetry, Scalar, OpenIddict…) opsiyonel `NovaCore.*` eklenti paketlerinde yer alır. FluentValidation yerine .NET 10'un `AddValidation`'ı kullanılır. | Library guidance: "review your library for unnecessary dependencies" (R-009) | Kullanıcı |
| D-004 | (öncesi) | **Mevcut standartlar** (CLAUDE.md'den): yalnızca MIT/Apache veya Microsoft paketleri. Mediator/CQRS dispatcher yok, handler'lar bağımlılıklarını minimal API DI'dan alır. Vertical slice (bir özellik = bir dosya). Beklenen hatalar için `Result`/`Result<T>`. Uç noktalar permission ile korunur. Merkezi paket yönetimi (CPM). Modüller birbirine yalnızca Contracts veya event üzerinden bağlanır. | CLAUDE.md | Kullanıcı |
| D-006 | 2026-09-25 | **Lisans:** MIT. LICENSE dosyası F0.1'de eklenir. Paketlerde `PackageLicenseExpression=MIT` kullanılır (F1.1). | Q-02 | Kullanıcı |
| D-007 | 2026-09-25 | **Yapılmayacaklar genişletildi (Y-13…Y-43).** Kaynak araştırması R-015…R-025, öneri dosyası `proposals/yapilmayacaklar-onerisi.md`. Kullanıcı A1–A4'ü onayladı; A5 ve B için kararı bana bıraktı ("doğru olanı yap"), ikisi de dahil edildi. Kaynağı olmayan Y-38 ve Y-39 "tercih" etiketiyle girdi. | Q-01 | Kullanıcı |
| D-008 | 2026-09-25 | **docker-compose parolası (F0.1):** `docker-compose.yml`'deki düz metin SA parolası repoya girmez. Baseline commit bu dosya olmadan atılır. Dosya ayrı bir commit'te parolayı `${MSSQL_SA_PASSWORD}` ile gitignore'daki `.env`'den okuyacak şekilde eklenir. | Y-35; Q-06 | Kullanıcı |
| D-009 | 2026-09-25 | **F0.3 kabul kriteri:** Test gerçek `/api/v1/auth/login` üzerinde yapılır. "Farklı bir IP 200 alır" kriteri "farklı bir IP 429 almaz" olarak okunur: DB olmadan izin verilen istek 400 validation döner. | "strict" policy'yi yalnızca /auth grubu kullanıyor; Q-07 | Kullanıcı |
| D-005 | 2026-09-24 | **Yürütme protokolü:** Plan, fazlar halinde `/novacore-phase` skill'i ile yürütülür. Her faz yeni bir oturumda ve ayrı bir branch'te çalışır, her adım ayrı bir commit'tir. Planda olmayan durumlar araştırılır (RESEARCH.md) ve kapsamı etkiliyorsa kullanıcıya sorulur. | Kullanıcının önceki oturumda yaşadığı kapsam kayması | Kullanıcı |

## Yapılmayacaklar (Y)

Buradaki maddeler **hiçbir gerekçeyle** uygulanmaz ve "tasarım önerisi" olarak da gündeme getirilmez. Bir madde gerçekten gerekli görünüyorsa dur, gerekçeyi ve kaynağı göster, kullanıcıya sor. Test edilebilir olanlar Faz 0.7'de mimari testlere çevrilir.

| ID | Yasak | Kaynak | Testle zorlanıyor mu? |
|---|---|---|---|
| Y-01 | MediatR, AutoMapper, FluentAssertions (ticari lisans). Ayrıca herhangi bir mediator veya request-dispatch soyutlaması. | CLAUDE.md | F0.7 (paket kısmı) + hook |
| Y-02 | Mikroservislere bölmek. Harici mesaj kuyruğu (RabbitMQ, Kafka, MassTransit…) eklemek. | ANALYSIS §8 | — |
| Y-03 | Tam Clean/DDD katmanlaması. Tek istisna: belirli bir modül için kullanıcı açıkça onaylarsa. | ANALYSIS §8 | — |
| Y-04 | Aspire'ı zorunlu bağımlılık yapmak. NativeAOT'u hedef olarak almak. | ANALYSIS §8 | — |
| Y-05 | Çekirdek paketlere Microsoft/BCL dışı bağımlılık eklemek (D-003). | D-003 | F2a.4 |
| Y-06 | MIT/Apache veya Microsoft dışı lisanslı paket kullanmak. | CLAUDE.md | hook (yeni paket = onay) |
| Y-07 | Mevcut kendi token üretimini (HS256, parola karşılığında token) genişletmek veya yeni özellik eklemek. Faz 3'e kadar yalnızca F0.9'daki minimal güvenlik düzeltmelerine izin var. | D-002 | — |
| Y-08 | Bir modülün başka bir modülün implementasyonuna veya DbContext'ine doğrudan erişmesi. | D-004 | F0.7 |
| Y-09 | Plan dışı "fırsatçı" iyileştirme, refactor veya temizlik. Görülen şeyler PROGRESS'teki "Sonraya bırakılanlar" listesine yazılır, dokunulmaz. | D-005 | — |
| Y-10 | Testleri geçirmek için testi zayıflatmak, silmek ya da skip etmek. Uyarıyı bastırmak (`NoWarn`, `#pragma warning disable`, severity düşürmek). Bunlar ancak kullanıcı onayıyla yapılabilir. | Kurulum önerisi | — |
| Y-11 | Hook'ları, analyzer'ları veya NuGet audit'i devre dışı bırakmak ya da atlatmaya çalışmak. | Kurulum önerisi | hook (korunan dosyalar) |
| Y-12 | Bir fazı bitirince kullanıcı yeni bir oturumda `/novacore-phase` çağırmadan sonraki faza başlamak. | D-005 | — |

### Araştırmaya dayalı yasaklar (Y-13…Y-43)

2026-09-25'te kullanıcı onayıyla eklendi (Q-01, D-007). Her maddenin gerekçesi, projedeki örneği ve tam kaynağı [proposals/yapilmayacaklar-onerisi.md](proposals/yapilmayacaklar-onerisi.md)'de, kaynak özetleri [RESEARCH.md](RESEARCH.md)'de. Parantezdeki Ö-numarası öneri dosyasındaki karşılığıdır. **"Tercih"** etiketli maddelerin birincil kaynağı yok.

**Aşırı mühendislik**

| ID | Yasak | Kaynak | Zorlama |
|---|---|---|---|
| Y-13 (Ö-01) | Somut ihtiyacı olmayan soyutlama. Interface/abstract class için ya 2+ gerçek implementasyon ya da süreç sınırı (saat, e-posta, HTTP, dosya) gerekir. "Her sınıfa `IXxx`" yok. | R-015 | Review |
| Y-14 (Ö-02) | Çekirdeğe "belki lazım olur" özellik. Çekirdeğe girmek için 2+ gerçek tüketici gerekir, o zamana kadar modülde kalır. | R-023, R-009 | Review + PublicAPI analyzer |
| Y-15 (Ö-03) | Framework'ü saran ince sarmalayıcılar: EF üzerine generic `IRepository<T>`/`IUnitOfWork`, `ILogger`/`IOptions`/HybridCache sarmalayıcıları. | R-016 | Mimari test (F0.7) |
| Y-16 (Ö-04) | CQRS (ayrı model/store), Event Sourcing, Saga, DDD taktik desenlerini her yere uygulamak. Yalnızca iş karmaşıklığı gerektirdiğinde ve kullanıcı onayıyla kullanılır. Basit CRUD'da anemik entity kabul edilir. | R-017 | Review (Y-03'ü genişletir) |
| Y-17 (Ö-05) | Katman/proje enflasyonu: modül başına Domain/Application/Infrastructure/Api projeleri. Yeni proje yalnızca bir bağımlılık sınırı için açılır. | R-017, D-004 | Mimari test (F0.7) |
| Y-18 (Ö-06) | Dağıtık sistem tekniklerini (retry/circuit breaker, kuyruk, eventual consistency) süreç içi çağrılara uygulamak. Outbox yalnızca Faz 4a kapsamında. | R-024 | Review (Y-02'yi genişletir) |
| Y-19 (Ö-07) | Gerçek bir tüketici istemeden davranışı config/feature flag ile açılıp kapanır yapmak. | R-023 | Review |
| Y-20 (Ö-08) | Reflection/assembly taraması ile "sihirli" otomatik servis veya modül kaydı. Kayıtlar açık yapılır. | ANALYSIS §2, R-005 | Mimari test (F2a.3 sonrası) |

**Bağımlılık ve kütüphane**

| ID | Yasak | Kaynak | Zorlama |
|---|---|---|---|
| Y-21 (Ö-09) | Yerleşik .NET API'si varken paket eklemek (Newtonsoft.Json, Swashbuckle, saat kütüphaneleri vb.). | R-009 | Hook (yeni paket = onay) |
| Y-22 (Ö-10) | Tam sürüm veya üst sınırlı paket referansı (`[1.0]`, `[1.0,2.0)`). | R-009 | Hook (reddeder) |
| Y-23 (Ö-11) | Minor/patch sürümde ikili kırıcı değişiklik. Gereksiz public API; tipler varsayılan olarak `internal`. | R-009, R-011 | Package validation + PublicAPI analyzer |
| Y-24 (Ö-12) | Kütüphane kodunda host kararı: global static state, `Console.WriteLine`, `Environment.Exit`, `BuildServiceProvider()`, sabit log sink'i, global serializer ayarı. | R-018 | Review |

**Kod düzeyi**

| ID | Yasak | Kaynak | Zorlama |
|---|---|---|---|
| Y-25 (Ö-13) | Service locator (iş kodunda `GetService`, runtime'da bağımlılık çözen factory, static servis erişimi). İstisna: altyapıda, gerekçesi yazılmış dar noktalar. | R-018 | Review |
| Y-26 (Ö-14) | Captive dependency (singleton → scoped). Scope doğrulaması Development ve testte açık. | R-018 | `ValidateScopes`/`ValidateOnBuild` + test (F0.7) |
| Y-27 (Ö-15) | Sync-over-async (`.Result`, `.Wait()`), `async void`, `Task.Run` + hemen `await`, `HttpContext`'i arka plana taşımak. | R-019 | Analyzer + review |
| Y-28 (Ö-16) | Akış kontrolü için exception (beklenen hata `Result` ile döner). catch-log-rethrow zinciri, exception yutma, sınır dışında `catch (Exception)`. | R-015, R-019 | Review |
| Y-29 (Ö-17) | "Helper/Helpers/Util/Utils/Manager/Common" çöp sınıfları. | R-015 | Mimari test (F0.7) |
| Y-30 (Ö-18) | İstek başına `new HttpClient()`. Typed client'ı singleton'da tutmak. | R-025 | Review |

**Veri erişimi ve API çıktısı**

| ID | Yasak | Kaynak | Zorlama |
|---|---|---|---|
| Y-31 (Ö-19) | Entity'yi API'den doğrudan almak veya döndürmek. Giriş ve çıkış DTO/record ile yapılır. | R-020 | Mimari test (uygulanabilirse, F0.7) + review |
| Y-32 (Ö-20) | Testte EF Core InMemory provider ya da sorgu için `DbSet` mock'u. Veri erişimi gerçek veritabanı sistemine karşı test edilir. | R-016 | Hook: `Microsoft.EntityFrameworkCore.InMemory` yasaklı |
| Y-33 (Ö-21) | Lazy loading proxy. | R-021 | Hook: `Microsoft.EntityFrameworkCore.Proxies` yasaklı |
| Y-34 (Ö-22) | Sınırsız sorgu (liste uç noktaları sayfalıdır). Projeksiyon yeterken tam entity yüklemek. | R-021 | Review |
| Y-35 (Ö-23) | İstemciye iç hata detayı sızdırmak. Loglara secret veya kişisel veri yazmak. Repoya secret commit'lemek. | EF O/RM considerations, ANALYSIS P0-4 | Test (F2b.4) + review |

**Süreç**

| ID | Yasak | Kaynak | Zorlama |
|---|---|---|---|
| Y-36 (Ö-24) | Kırmızı build veya testle adımı bitirmek, commit'lemek ya da merge etmek. | D-005 | Stop hook |
| Y-37 (Ö-25) | Tek commit'te ilgisiz değişiklikleri karıştırmak. "Big-bang" refactor. | D-005 | Skill §3 |
| Y-38 (Ö-26) | Takip edilmeyen TODO/FIXME bırakmak. Kalan iş PROGRESS'teki "Sonraya bırakılanlar" listesine yazılır. | **Tercih** | grep/mimari test (opsiyonel) |
| Y-39 (Ö-27) | Kodu tekrar eden yorum. Yorum "neden"i anlatır. Paketlerin public API XML dokümanı bu kuralın dışındadır. | **Tercih** | Review |

**Yapay zeka davranışı** (mode collapse / tipiklik yanlılığı, R-022)

| ID | Yasak | Kaynak | Zorlama |
|---|---|---|---|
| Y-40 (Ö-28) | Gerekçesiz desen, soyutlama ya da paket önermek. Her öneride şu üçü bulunur: (1) **bu kod tabanında** çözdüğü somut sorun (dosya/satır), (2) en az bir daha basit alternatif ve neden yetmediği, (3) birincil kaynak (R-ID). | R-022, R-015 | Skill §4 + CLAUDE.md |
| Y-41 (Ö-29) | Seçim sorulduğunda tek "popüler" cevap vermek. En az 2–3 gerçekten farklı seçenek, ödünleşimleri ve projeye özgü gerekçeyle önerilen seçenek sunulur. Tasarım dışı kararlar (isim, renk, metin) için de geçerli. | R-022 | CLAUDE.md |
| Y-42 (Ö-30) | "Genelde böyle yapılır" / "best practice" ifadesini tek başına gerekçe saymak. Kaynak ve bu projeye uygunluk gösterilir. | D-001…D-003 | Review |
| Y-43 (Ö-31) | Reddedilmiş bir yaklaşımı farklı isimle ya da parça parça yeniden önermek (ör. MediatR → "hafif dispatcher", repository → "QueryService<T>"). Yeniden açmak gerekiyorsa açıkça "Y-xx'i yeniden tartışmayı öneriyorum" denir. | D-005 | Skill §4 |

## Kalıcı kararların revizyonu

Bir D veya Y maddesi değişecekse satır silinmez. Eski satırın başına `~~üstü çizili~~` + "yerine D-0XX" notu eklenir, yeni karar yeni bir ID ile yazılır. Kararın geçmişi böylece görünür kalır.
