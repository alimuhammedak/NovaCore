# "Yapılmayacaklar" önerisi (taslak, onay bekliyor)

> **Durum: ONAYLANDI (2026-09-25, D-007).** 31 maddenin tamamı `DECISIONS.md`'ye Y-13…Y-43 olarak taşındı (Ö-01 → Y-13 … Ö-31 → Y-43). A1–A4 kullanıcı tarafından açıkça onaylandı. A5 ve B için kullanıcı "doğru olanı yap" dedi, ikisi de dahil edildi. Bu dosya artık gerekçe arşividir; kurallar DECISIONS.md'dedir.
> Araştırma kaynakları [RESEARCH.md](../RESEARCH.md)'de R-015 ve sonrasında listeleniyor.

## Yöntem

Liste iki bakış açısıyla hazırlandı. Ağırlık birincisinde:

1. **Mühendislik (Bölüm A):** Her madde bir birincil kaynağa dayanıyor. Kaynaklar: Microsoft Learn, Framework Design Guidelines (FDG), Azure Architecture Center'daki "ne zaman kullanılmamalı" bölümleri, EF Core dokümanları ve Fowler'ın YAGNI / MonolithFirst yazıları. Kaynağı olmayan maddeler **"tercih"** olarak etiketlendi.
2. **Yapay zeka davranışı (Bölüm B):** Modelin "en tipik" kalıba kayma eğilimi ve buna karşı süreç kuralları.

Ortak ilke Fowler'ın YAGNI tanımından geliyor: öngörüyle eklenen bir yetenek dört maliyet taşır. **İnşa** (yapma emeği), **gecikme** (gerçekten gereken işin ertelenmesi), **taşıma** (eklenen karmaşıklığın her değişikliği yavaşlatması), **onarım** (yanlış kurulduğunda düzeltme borcu). YAGNI refactoring ve test için geçerli değildir; kodu değiştirilebilir tutan her şey serbesttir.

Bu listenin çoğu **NovaCore'da zaten yaşanmış** örneklerden çıktı (ANALYSIS.md). Bu yüzden her maddede "projedeki örnek" sütunu var.

---

## Bölüm A: Mühendislik

### A1. Aşırı mühendislik ve spekülatif genellik

| ID | Yapılmayacak | Neden (mühendislik) | Kaynak | Projedeki örnek | Zorlama |
|---|---|---|---|---|---|
| Ö-01 | **Somut ihtiyacı olmayan soyutlama.** Interface/abstract class yalnızca (a) en az iki gerçek implementasyon varsa **veya** (b) bir süreç sınırını (saat, e-posta, dış HTTP, dosya sistemi) test için ayırıyorsa eklenir. "Her sınıfa bir `IXxx`" yapılmaz. | Soyutlamanın isimleri ve üyeleri doğası gereği soyuttur ve anlamayı zorlaştırır. Yanlış üye kümesi sonradan kırıcı değişiklik gerektirir. | FDG Abstractions: *"DO NOT provide abstractions unless they are tested by developing several concrete implementations"* | `ICacheService` (HybridCache zaten soyut) | Code review kontrol listesi |
| Ö-02 | **Çekirdeğe "belki lazım olur" özellik eklemek.** Bir şey çekirdek pakete (`NovaCore.*`) ancak en az iki gerçek tüketici (modül/proje) ihtiyaç gösterdiğinde girer. O zamana kadar onu kullanan modülde kalır. | Çekirdekteki her özellik tüm projelere taşıma maliyeti ve bağımlılık getirir. Sonradan çıkarmak kırıcı değişikliktir. | Fowler YAGNI (taşıma maliyeti) · Library guidance: *"review your library for unnecessary dependencies"* | API versioning, output cache, domain event dispatcher, `IModule.IsEnabled`: hepsi kayıtlı ama hiç kullanılmıyordu | Review; yeni çekirdek API'si PublicAPI analyzer'da görünür (F1.4) |
| Ö-03 | **Framework'ü saran ince sarmalayıcılar.** EF üzerine generic `IRepository<T>`/`IUnitOfWork`; `ILogger` üzerine `ILoggerService`; `IOptions` üzerine "ConfigService"; HybridCache üzerine cache servisi. | DbContext zaten Unit of Work + repository'dir. EF dokümanına göre repository katmanı *"significant cost to implement and maintain"* ve LINQ'i uygulamanın her yerinde kullanamazsın. Yalnızca veri katmanını bütünüyle stub'lamak istiyorsan gerekçelidir; bizim test stratejimiz gerçek veritabanı (Ö-20). | EF Core: Choosing a testing strategy (Repository pattern bölümü) | `ICacheService` | Mimari test: NovaCore assembly'lerinde `IRepository`/`IUnitOfWork`/`Repository<` adlı tip yok |
| Ö-04 | **Mimari desenleri her yere uygulamak.** CQRS (ayrı okuma/yazma modeli veya store), Event Sourcing, Saga ve DDD taktik desenleri (aggregate, value object, domain service) yalnızca modülün iş karmaşıklığı gerektirdiğinde **ve kullanıcı onayıyla** kullanılır. Basit CRUD modülünde "anemik" entity + doğrudan DbContext kabul edilir. | Azure: CQRS *"might not be suitable when the domain or the business rules are simple"*. Event Sourcing basit CRUD için uygun değil. eShop: *"forcing the same pattern everywhere leads to failure"*. | Azure Architecture Center (CQRS, Event Sourcing) · .NET mikroservis e-kitabı · MS "When shouldn't you apply DDD" | — | Review; Y-03 ile birlikte |
| Ö-05 | **Katman/proje enflasyonu.** Modül başına ayrı Domain/Application/Infrastructure/Api projeleri açmak. Yeni proje yalnızca bir bağımlılık sınırı gerektiğinde açılır (Contracts, eklenti paketi). | Proje sayısı derleme süresini, referans grafiğini ve bilişsel yükü büyütür. Vertical slice zaten ayrım sağlıyor. | MS "When shouldn't you apply DDD" (katman yatırımı küçük uygulamalarda karşılığını vermez) · D-004 | — | Mimari test: `NovaCore.Modules.*` altında izin verilen proje soneklerinin listesi |
| Ö-06 | **Dağıtık sistem tekniklerini süreç içi sorunlara uygulamak.** Modüller arası in-process çağrıya retry/circuit breaker, mesaj kuyruğu, eventual consistency. Outbox yalnızca Faz 4a'daki tanımlı kapsamda kullanılır. | Süreçleri ayırmak yöntem çağrısını asenkron iletişime, event bus'a, retry'a ve eventual consistency'ye çevirir; bu ek yük ancak gerçek bir dağıtım ihtiyacında karşılığını verir. Fowler: yeni proje mikroservisle başlamamalı. | MS "Common web application architectures" · Fowler MonolithFirst | — | Review (Y-02'nin genişletilmiş hali) |
| Ö-07 | **Her davranışı config'le açılıp kapanır yapmak.** Feature flag, `Enabled` bayrağı ya da "strateji seçimi" yalnızca gerçek bir tüketici farklı davranışa ihtiyaç duyduğunda eklenir. | Her bayrak test matrisini ikiye katlar. Kullanılmayan dal zamanla bozulur ama bunu fark eden olmaz. | Fowler YAGNI (taşıma ve onarım maliyeti) | `IModule.IsEnabled` config'e hiç bağlanmamıştı | Review |
| Ö-08 | **Reflection ile "sihirli" otomatik kayıt.** Assembly tarama ile konvansiyonel servis kaydı (Scrutor tarzı) ya da deploy klasörü taraması. Kayıtlar açık yapılır. | Kaydın nerede olduğu görünmez. AOT/trim uyumsuzdur. Klasöre düşen herhangi bir dll yüklenebilir. | ANALYSIS §2 · R-005 (source-gen tercih edilir) | `ModuleLoader`, Scrutor referansı | F2a.3 sonrası mimari test |

### A2. Bağımlılık ve kütüphane dağıtımı

| ID | Yapılmayacak | Neden | Kaynak | Zorlama |
|---|---|---|---|---|
| Ö-09 | **Yerleşik .NET API'si varken paket eklemek.** Bu kural modüller için de geçerli (çekirdek için zaten D-003 var). Örnekler: `System.Text.Json` varken Newtonsoft.Json, `Microsoft.AspNetCore.OpenApi` varken Swashbuckle, `TimeProvider` varken saat kütüphanesi. | Her paket diamond-dependency ve güvenlik açığı yüzeyidir (F0.2'deki iki HIGH açık transitive'di). | Library guidance: Dependencies | Hook (yeni paket onay ister) |
| Ö-10 | **Tam sürüm veya üst sınırlı paket referansı** (`[1.0]`, `[1.0,2.0)`). | NuGet çakışmada hata verir, tüketici projeleri kilitler. | Library guidance: *"AVOID NuGet package references that demand an exact version / with a version upper limit"* | Hook'a eklenebilir (`Version="[` deseni) |
| Ö-11 | **Minor/patch sürümde ikili kırıcı değişiklik. Gereksiz public API.** Tipler varsayılan olarak `internal`'dır, public yüzey bilerek açılır. | Düşük seviye kütüphanede kırıcı değişiklik tüm tüketicileri etkiler. | Library guidance: Breaking changes · Package validation (R-011) | Package validation + PublicAPI analyzer (Faz 1) |
| Ö-12 | **Kütüphane kodunda host kararı vermek.** Kütüphane şunları yapmaz: global static state, `Console.WriteLine`, `Environment.Exit`, kendi `BuildServiceProvider()` çağrısı, sabit log sink'i, global serializer ayarı. | Bunlar host'un kararıdır. Kütüphane yalnızca soyutlamaları (`ILogger`, `IOptions`, DI) kullanır. | DI guidelines: *"Avoid calls to BuildServiceProvider when configuring services"*, *"Avoid stateful, static classes"* · Logging library guidance | Review; Faz 2a eklenti ayrımı |

### A3. Kod düzeyi

| ID | Yapılmayacak | Neden | Kaynak | Projedeki örnek | Zorlama |
|---|---|---|---|---|---|
| Ö-13 | **Service locator.** İş kodunda `GetService`/`GetRequiredService`, çalışma zamanında bağımlılık çözen bir factory enjekte etmek, servislere static erişim. İstisna: altyapıda, gerekçesi yazılmış dar noktalar (hosted service scope'u gibi). | IoC ile karışınca bağımlılıklar görünmez olur ve test zorlaşır. | DI guidelines: *"Avoid using the service locator pattern"* | `ValidationFilter` (F2c.1'de kalkıyor) | Review |
| Ö-14 | **Captive dependency.** Singleton bir servisin scoped bir servisi tutması. Scope doğrulaması Development ve testte açık kalır. | Scoped servis (DbContext) istekler arasında paylaşılır: veri sızıntısı ve thread-safety hatası. | DI guidelines: Captive dependency | `DomainEventDispatcher` (singleton, root provider'dan çözüyor) | `ValidateScopes`/`ValidateOnBuild` + test |
| Ö-15 | **Sync-over-async ve async hataları.** `.Result`, `.Wait()`, `async void`, `Task.Run(...)` sonrası hemen `await`, `HttpContext`'i arka plan işine taşımak. | Thread pool tükenmesi, deadlock, yanıt tamamlandıktan sonra erişim nedeniyle process çökmesi. | ASP.NET Core Best Practices: *"async void is ALWAYS a bad practice"*, "Avoid blocking calls" | — | Analyzer (CA2012 vb.) + review |
| Ö-16 | **Akış kontrolü için exception.** Beklenen hatalar `Result` ile dönülür. Ayrıca catch-log-rethrow zinciri kurulmaz, exception yutulmaz, sınır dışında `catch (Exception)` yazılmaz. | Exception'lar yavaştır ve akışı gizler. Tek bir global handler yeterlidir. | FDG: *"DO NOT use exceptions for the normal flow of control"* · ASP.NET Core Best Practices: Minimize exceptions | — | Review |
| Ö-17 | **"Helper/Utils/Common/Manager" çöp sınıfları.** Static sınıf yalnızca extension metot tutucusu ya da gerçek bir kısayol olabilir. | Sorumluluğu belirsiz sınıflar zamanla her şeyin toplandığı yere dönüşür. | FDG Static classes: *"DO NOT treat static classes as a miscellaneous bucket"* | — | Mimari test: tip adında `Helper`/`Helpers`/`Util`/`Utils`/`Manager` yok |
| Ö-18 | **İstek başına `new HttpClient()`. Typed client'ı singleton'da tutmak.** | Socket tükenmesi ve DNS değişikliklerinin görülmemesi. | HttpClient guidelines · IHttpClientFactory troubleshooting | — | Review |

### A4. Veri erişimi ve API çıktısı

| ID | Yapılmayacak | Neden | Kaynak | Zorlama |
|---|---|---|---|---|
| Ö-19 | **Entity'yi API'den doğrudan almak veya döndürmek.** Giriş ve çıkış için DTO/record kullanılır. | Over-posting (mass assignment) açığı; iç alanların sızması. | MS Minimal API tutorial: "Prevent over-posting" · Threat modeling: mass assignment | Mimari test (endpoint imzalarında `*.Domain` tipi yok; uygulanabilirliği F0.7'de değerlendirilir) + review |
| Ö-20 | **Testte EF Core InMemory provider'ı veya sorgu için `DbSet` mock'u.** Veri erişimi gerçek veritabanı sistemine karşı test edilir. SQLite yalnızca geçici bir çözümdür. | EF dokümanı InMemory için *"highly discouraged"* diyor. Sorgular farklı davranır, transaction ve raw SQL çalışmaz. | EF Core: Testing (Choosing a testing strategy) | Hook: `Microsoft.EntityFrameworkCore.InMemory` yasaklı paket listesine |
| Ö-21 | **Lazy loading proxy.** | Fark edilmeden N+1 sorguya yol açar; geliştirmede görünmez, üretimde yavaşlatır. | EF Core Efficient querying: *"recommended to avoid it"* | Hook: `Microsoft.EntityFrameworkCore.Proxies` yasaklı paket listesine |
| Ö-22 | **Sınırsız sorgu. Projeksiyon yerine tam entity yüklemek.** Liste uç noktaları sayfalıdır, okuma sorguları `Select` projeksiyonu kullanır. | Test verisi küçükken görünmeyen bellek ve performans sorunları. | EF Core Efficient querying: "Limit the resultset size", "Project only properties you need" | Review |
| Ö-23 | **İstemciye iç hata detayı sızdırmak. Loglara secret veya kişisel veri yazmak. Repoya secret commit'lemek.** | Bilgi ifşası. | EF Core "O/RM considerations" (secret ve connection string güvenlik incelemesi) · ANALYSIS P0-4 | F2b.4 testi + review; docker-compose'taki parola F3b.4'te taşınıyor |

### A5. Süreç ve geliştirme

| ID | Yapılmayacak | Neden | Kaynak | Zorlama |
|---|---|---|---|---|
| Ö-24 | **Kırmızı build veya testle adımı bitirmek, commit'lemek ya da merge etmek.** | Kırık durum birikir ve kaynağı bulunamaz. | D-005 | Stop hook (mevcut) |
| Ö-25 | **Tek commit'te ilgisiz değişiklikleri karıştırmak. "Big-bang" refactor.** | Geri almak ve incelemek imkânsız hale gelir. | D-005 | Skill §3 (adım başına commit) |
| Ö-26 | **Takip edilmeyen TODO/FIXME bırakmak.** Kalan iş PROGRESS'teki "Sonraya bırakılanlar" listesine yazılır. | Kodda unutulan borç. | **Tercih** | Mimari test veya grep (opsiyonel) |
| Ö-27 | **Kodu tekrar eden yorum.** Yorum "ne"yi değil "neden"i anlatır. Paketlerin public API XML dokümanı bu kuralın dışındadır. | Gürültü; kod değişince yalan söyleyen yorum. | **Tercih** | Review |

---

## Bölüm B: Yapay zeka davranışı (kaygılar)

### Mekanizma

Dil modelleri, tercih verisindeki "tipiklik yanlılığı" nedeniyle en tipik cevaba çöker (mode collapse). Senin "renk seç → mor" gözlemin bunun tam bir örneği (Zhang ve ark., 2025, *Verbalized Sampling*, R-022). Aynı eğilim .NET mimarisinde de hep aynı paketi getiriyor: generic repository + unit of work, her sınıfa `IService`, MediatR + pipeline behavior, AutoMapper, 4 projeli Clean Architecture şablonu, `BaseEntity`/`BaseService`/`BaseController` hiyerarşileri, `Helpers` klasörü, her yerde Polly, Swashbuckle ve Newtonsoft.Json. Bölüm A bu kalıpların çoğunu tek tek yasaklıyor. Bölüm B ise kalıbın **farklı bir isimle geri gelmesini** engelleyen süreç kurallarını içeriyor.

Makalenin önerdiği çare, modelden tek cevap yerine olasılıklarıyla birlikte birden çok seçenek istemek. Ö-29 bu fikri uyguluyor.

| ID | Yapılmayacak | Neden | Kaynak | Zorlama |
|---|---|---|---|---|
| Ö-28 | **Gerekçesiz desen, soyutlama ya da paket önermek.** Her öneride şu üçü bulunur: (1) **bu kod tabanında** çözdüğü somut sorun (dosya/satır), (2) en az bir daha basit alternatif ve neden yetmediği, (3) birincil kaynak (R-ID). | Tipik cevaba kaymanın panzehiri, öneriyi projenin somut kısıtına bağlamaktır. | Zhang ve ark. 2025 (R-022) · FDG (Ö-01) | Skill §4 + CLAUDE.md |
| Ö-29 | **Seçim sorulduğunda tek "popüler" cevap vermek.** En az 2–3 gerçekten farklı seçenek, her birinin ödünleşimi ve projeye özgü gerekçeyle önerilen seçenek sunulur. Tasarım dışı kararlar (isimlendirme, renk, metin) için de geçerli. | Verbalized Sampling: birden çok seçenek istemek çeşitliliği belirgin şekilde artırıyor. | R-022 | CLAUDE.md |
| Ö-30 | **"Genelde böyle yapılır" / "best practice" ifadesini tek başına gerekçe saymak.** | "Genel" olan, bu projenin kısıtlarına (tek geliştirici, Microsoft-only çekirdek, NuGet dağıtımı) uymayabilir. | D-001…D-003 | Review |
| Ö-31 | **Reddedilmiş bir yaklaşımı farklı isimle ya da parça parça yeniden önermek.** Örnekler: MediatR reddedildikten sonra "hafif bir dispatcher", repository reddedildikten sonra "QueryService<T>". Yeniden açmak gerekiyorsa açıkça "Y-xx'i yeniden tartışmayı öneriyorum" denir. | Önceki oturumda yaşanan tam olarak buydu. | Kullanıcı deneyimi (D-005) | Skill §4 |

---

## Onay sonrası yapılacaklar

1. Onaylanan maddeler DECISIONS.md'ye Y-13'ten itibaren eklenir (korunan dosya olduğu için hook onay ister).
2. Hook ile zorlanabilir olanlar (`package-policy.json` → `banned`): `Microsoft.EntityFrameworkCore.InMemory` (Ö-20), `Microsoft.EntityFrameworkCore.Proxies` (Ö-21). İsteğe bağlı: Ö-10'daki `Version="[` deseni.
3. Mimari testle zorlanabilir olanlar F0.7'nin kapsamına eklenir: Ö-03, Ö-05, Ö-17, Ö-19 (uygulanabilirse), Ö-26 (opsiyonel).
4. Bölüm B'nin kuralları CLAUDE.md'deki protokol bölümüne ve skill §4'e kısa referans olarak eklenir.
