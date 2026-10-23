# CRM-Nominal ve NovaCore: eksik bizde mi?

**Tarih:** 2026-10-23 (ikinci sürüm: ilk sürümün sorularına verilen cevaplarla yeniden yazıldı)
**Soru:** CRM-Nominal NovaCore üzerinde yeniden kurulacak. Bunun için NovaCore'da bir eksik var mı? Varsa yalnız CRM'e mi yarar, başka projelere de mi?
**İncelenen:** NovaCore `main` @ `8a75754` · CRM-Nominal `master` @ `8f9802b` (ADR-001…005; `Program.cs`, `AppDbContext`, `KapsamInterceptor`, `KapsamPolitikasi`, `KapsamBaglami`, `TenantDurumMiddleware`, `TokenService`) · Microsoft Learn · NuGet paket meta verisi.
**Durum:** Kararlar verildi ve plana işlendi (2026-10-23): DECISIONS D-068…D-071 (Y-06 → Y-47, Y-14 → Y-48), RESEARCH R-122…R-124, PLAN Faz 7a ve 7b (sıra `6d → 7a → 7b → 6e`), PROGRESS Q-53…Q-58. Kod değişikliği yok.

Etiketler: **[kod]** kaynak okundu · **[MS]** Microsoft Learn · **[NuGet]** paketin nuspec'i · **[kayıt]** plan belgeleri · **[ölçülmedi]** okunarak çıkarıldı, denenmedi.

---

## 0. Çerçeve (kullanıcının 2026-10-23 cevapları)

- CRM, NovaCore'un kurallarını **tamamen** devralır: modüler monolit, vertical slice; Mediator, FluentValidation, Shouldly yok.
- CRM'in altyapısı taşınmaz. Taşınan şey iş mantığı ve kurallardır; validasyonlar dahil, ama kod değil, mantık.
- NovaCore hiçbir projeyi bir veritabanına **mahkûm etmemeli**; her proje veritabanını özgürce seçebilmeli.
- Önce "eksik NovaCore'da mı" kararı verilir, geçiş ondan sonra planlanır. İşin büyüklüğü ölçüt değil; ölçüt, yapılan şeyin başka projelere de yarayıp yaramayacağı.

## 1. Kısa cevap

- **Eksik bizde, iki yerde:**
  1. **Veritabanı özgürlüğü.** Kesin. Paketler sağlayıcıdan bağımsız, ama şablon SQL Server'a kilitli, paketler başka bir sağlayıcıda hiç koşmadı ve Y-06 PostgreSQL'in sürücüsünü lisansı yüzünden reddediyor (§2). Bu CRM'den bağımsız: her proje için geçerli.
  2. **Çok kiracılılık altyapısı.** NovaCore'u birden çok kuruma satılan ürünlerin (B2B SaaS) temeli yapacaksak evet. NovaCore'un modüler mimarisinde her modülün kendi DbContext'i var; tenant filtresinin, yazma korumasının ve arka plan bağlamının bir projede NovaCore'dan başka koyacak yeri yok (§3).
- **CRM'e özgü kalanlar:** şube kapsamı, rol hiyerarşisi (SistemAdmini…Danışman), KVKK okuma denetiminin kuralları ve bütün domain kuralları.
- **Kimlik modeli** asıl ürün kararı (§4). Microsoft'un önerisi kişi başına tek hesap; NovaCore'un kimlik eklentisi o modelde değişmeden kalıyor.
- **İki kayıt yeniden açıldı:** Y-06 (veritabanı sağlayıcıları için) ve Y-14 ile tuketici-hazirligi §P2 (çok kiracılılık için). Kararlar §6'da.

## 2. Veritabanı özgürlüğü: eksik bizde

### 2.1 Bugünkü durum

| Katman | Durum | Kanıt |
|---|---|---|
| Çekirdek paketler | **Bağımsız.** `NovaCore.EntityFrameworkCore` yalnız `Microsoft.EntityFrameworkCore` ve `.Relational`'a bağlı; outbox tabloları adla eşleniyor, sağlayıcıya özgü kolon tipi yok. | csproj, `OutboxModelBuilderExtensions.cs` [kod] |
| Kimlik eklentisi | **Bağımsız.** `IdentityServerDbContext`'i host kendi sağlayıcısıyla kaydediyor (D-026 (2)). | [kod], [kayıt] |
| Şablon (`dotnet new novacore`) | **SQL Server'a kilitli.** Sağlayıcıyı her modül kendisi seçiyor (`options.UseSqlServer(...)`), ayrıca her design-time factory, host'un kimlik context'i, SqlClient'ın Pkcs pini ve compose dosyası. Veritabanını değiştirmek her modülü, her factory'yi ve bütün migration'ları değiştirmek demek. | şablonun `TodosModule.cs:29`, `TodosDbContextFactory.cs:15`, `Program.cs:31`; repo'da `CatalogModule.cs:32` [kod] |
| Testler | SQLite ve SQL Server. Paketler başka bir sağlayıcıda hiç koşmadı. | D-043, D-056 [kayıt] |
| Kural | Y-06 ve `eng/check-licenses.sh` yalnız MIT/Apache-2.0'ı kabul ediyor (adlı tek istisna Polly, D-052). PostgreSQL'in sürücüsü (Npgsql) `PostgreSQL` lisanslı; MySQL'in resmî sağlayıcısı `GPL-2.0-only WITH Universal-FOSS-exception-1.0`; Oracle'ınki ve MongoDB'ninki kendi lisans dosyasıyla geliyor; Pomelo (MySQL/MariaDB) MIT. | [NuGet] |
| Mimari varsayım | Her modül kendi şemasında (`HasDefaultSchema`). SQL Server, PostgreSQL ve Oracle'da şema var; MySQL/MariaDB'de şema bir veritabanı demek, SQLite şemayı yok sayıyor. | [kod]; MySQL davranışı [ölçülmedi] |

Microsoft, sağlayıcı seçiminde lisansın da değerlendirilmesini söylüyor: "be sure to evaluate quality, licensing, support" [MS: Database Providers]. Yani lisans meşru bir ölçüt. Ama bugünkü kural sağlayıcıyı NovaCore adına seçiyor; sizin ilkenizle çelişen tam bu.

### 2.2 Yapılması gerekenler (bir NovaCore fazı)

1. **Kural.** Y-06'ya bir sağlayıcı maddesi (karar R1).
2. **Şablon.** Sağlayıcı tek bir yerde seçilir; modüller seçmez.
   - `dotnet new novacore --db SqlServer|PostgreSql` (`--auth` gibi bir şablon parametresi).
   - Modüllerin bugün kendisinin çağırdığı `UseSqlServer`'ı host'un seçtiği sağlayıcıya bırakmanın yolu fazda seçilir. Seçenekler: (a) `AddModuleDbContext` host'un kaydettiği sağlayıcı yapılandırmasını her modüle uygular, modül yalnız kendi ayarını ekler (migration geçmişi tablosu, şema); (b) modül sağlayıcıyı bir NovaCore uzantısından alır. Kesin biçim R-kaydıyla.
   - Sağlayıcı başına ayrı migration seti: Microsoft'un çok sağlayıcılı projeler için önerdiği yol [MS: Migrations with multiple providers].
3. **Kanıt.** Outbox, migration servisi ve kimlik eklentisi ikinci bir sağlayıcıda (PostgreSQL, `Testcontainers.PostgreSql`, MIT) test edilir. Şablonun smoke testi iki sağlayıcıyla koşar.
4. **Belge.** Desteklenen sağlayıcılar ve "modül başına şema" varsayımının hangi veritabanlarında geçerli olduğu.

## 3. Çok kiracılılık: yalnız CRM'e mi, başka projelere de mi?

### 3.1 Parçalar ve hangisi genel

Başlıklar Microsoft'un çok kiracılı çözümler rehberinden (Azure Architecture Center). Rehber Azure'a özgü değil; her SaaS'ın çözmesi gereken konuları sayıyor.

| Parça | Ne yapar | Genel mi? | Kaynak |
|---|---|---|---|
| Tenant çözümü | İsteğin hangi kurum için olduğunu bulur: alan adı, URL yolu, başlık ya da token claim'i | **Genel.** Proje yöntemini seçer. | [MS: Map requests to tenants] |
| İsteğin doğrulanması | Kullanıcının o kurumda yetkili olduğunu her istekte sunucuda kontrol eder | **Genel** | aynı sayfa, "Request validation" |
| Veri izolasyonu (okuma) | `TenantId` kolonu + adlı query filter | **Genel** | [MS: EF multitenancy, query filters] |
| Veri izolasyonu (yazma) | Boş `TenantId`'yi doldurur, başka kuruma yazmayı reddeder | **Genel** | [MS: EF interceptors] |
| Sistem işi kaçışı | Seed, migration, bakım işleri için açık ve dar bir "tenant'sız" bağlam | **Genel** | CRM `KapsamBaglami.SistemOlarakCalis` |
| Arka plan bağlamı | Event'e tenant yazılır, handler'ın scope'unda geri yüklenir | **Genel** (event kullanan her SaaS) | [kod] `OutboxDispatcher.cs:163` |
| Üyelik | Kullanıcı ↔ kurum ↔ rol | **Genel** (tek hesap modelinde) | [MS: Identity approaches] |
| Kuruma göre izin | Aynı kişi iki kurumda farklı izin | **Genel** | aynı sayfa |
| Kurum yaşam döngüsü | Aktif, askıda, kapalı; ayrılan kurumun verisinin silinmesi | **Genel** | [MS: Tenant life cycle] |
| Gürültülü komşu | Kurum başına istek sınırı | **Genel**, ikinci adım | [MS: Noisy Neighbor] |
| Veritabanı arka durağı (RLS) | Veritabanının da tenant'ı zorlaması | **Sağlayıcıya özgü** (SQL Server `SESSION_CONTEXT`, PostgreSQL `set_config`). Microsoft, karmaşıklığı yüzünden çoğu çözümün bunu kullanmadığını söylüyor. Paketlere girmez; seçmeli, sağlayıcı başına belgelenmiş bir örnek olur. | [MS: Storage and data, Row-level security] |
| Kurum başına veritabanı | Her kuruma ayrı veritabanı | Genel ama ayrı bir model; ilk adımda kapsam dışı | [MS: EF multitenancy] |
| Şube kapsamı | Kurum içinde ikinci izolasyon ekseni | **CRM'e özgü.** "Kurum içi birim" (mağaza, departman) başka ürünlerde de var, ama tek örnekten genelleştirmek Y-14'ün korktuğu şey. | CRM ADR-003/005 |
| Roller | SistemAdmini, KurumYöneticisi, ŞubeMüdürü, Danışman | **CRM'e özgü** | CRM |
| KVKK okuma denetimi | Kim hangi kişisel kaydı gördü | Mekanizma genel (endpoint filter), kurallar CRM'in | [MS: Minimal API filters] |

### 3.2 Neden NovaCore'dan başka yeri yok

- NovaCore'da her modülün kendi DbContext'i ve şeması var. CRM yeniden kurulduğunda altı-yedi modül olur (adaylar, randevular, talepler, kayıtlar, katalog, raporlar, şubeler). Tenant filtresi, yazma koruması ve arka plan bağlamı **her birinde** gerekir.
- Bir NovaCore projesinde modüllerin ortak kullandığı altyapının tek yeri BuildingBlocks. Projelerde ortak bir "shared kernel" projesi yok; Y-17'ye göre yeni proje yalnız bir bağımlılık sınırı için açılır.
- NovaCore'a girmezse iki yol kalır:
  - Her modül aynı kodu kopyalar. Bu, ADR-003'ün ölçtüğü "hatırlanması gereken kural" sorunu, bu sefer modül düzeyinde.
  - CRM ortak bir proje açar. Bu, bir NovaCore paketinin projeye özel kopyası olur.
- NovaCore bunu bir kez yaşadı: outbox interceptor'ı her modüle `AddModuleDbContext` ekliyor, çünkü "unutan bir modül event'lerini hatasız kaybederdi" (`PersistenceExtensions.cs:71`). Tenant koruması aynı türden bir güvenlik garantisi.

### 3.3 Başka projelere yarar mı?

Cevap gelecekteki projelerin türüne bağlı (karar R4):

- **Birden çok kuruma satılan ürün (B2B SaaS):** evet, aynı parçaların hepsi gerekir.
- **Tek bir kurumun iç uygulaması:** hayır.
- **Son kullanıcıya açık uygulama (B2C):** çoğunlukla hayır; veri kullanıcıya bağlıdır, kuruma değil. NovaCore'un nesne düzeyi yetki önerisi (F5.5) orada yeter.

Paket isteğe bağlı olursa ihtiyacı olmayan proje bir bedel ödemez; kimlik eklentisi de böyle.

### 3.4 Y-14 ile ilişkisi

- Y-14: "Çekirdeğe girmek için 2+ gerçek tüketici gerekir." tuketici-hazirligi §P2 çok kiracılılığı bu kurala bağladı: "İkinci bir proje aynı ihtiyacı duyarsa çekirdek değerlendirilir."
- Parçaların bir kısmı ayrı, isteğe bağlı bir pakete girebilir. Ama bir kısmı çekirdeğe dokunmak zorunda: izin önbelleğinin kuruma göre bölümlenmesi, outbox'ta bağlam taşınması, `AddModuleDbContext`'in korumayı otomatik eklemesi. Yani Y-14 doğrudan ilgili.
- **Y-14'ü çok kiracılılık için yeniden açmayı öneriyorum.** Gerekçeler:
  1. Modüler mimaride başka bir yer yok (§3.2).
  2. Y-14'ün korktuğu şey, tek bir tüketicinin şeklini almış yanlış bir soyutlama. Bu risk, tasarımı CRM'den değil Microsoft'un genel rehberinden çıkararak ve CRM'e özgü parçaları (şube, roller) dışarıda bırakarak küçülüyor.
  3. NovaCore'un amacı bütün projelerin temeli olmak.
- **Karşı görüş:** İkinci bir çok kiracılı proje hiç gelmezse NovaCore tek bir proje için bakım yükü taşır. Paket isteğe bağlı olduğu için diğer projeler bundan etkilenmez.

### 3.5 Tasarımın ana çizgileri (fazda kesinleşir; şimdi karar değil)

- İsteğe bağlı paket; projeye açıkça eklenir (Y-20).
- Tenant çözümü için Microsoft'un saydığı yöntemler (yol, alan adı, başlık, claim) arasından proje seçer.
- Paylaşılan veritabanı + `TenantId` + adlı filtre + yazma koruması. `ITenantEntity` işaretli her varlığa `AddModuleDbContext` korumayı kendisi ekler.
- Tenant'sız iş yalnız açık bir "sistem işi" bağlamıyla mümkün; varsayılan kapalı (fail-closed).
- Outbox mesajı tenant'ı (ve "Sonraya bırakılanlar"daki trace bağlamını) taşır.
- İzinler kuruma göre bölümlenir. Kimlik eklentisinin platform izinleri (Administrator, 2FA zorunlu, D-053 (5)) korunur. Bugün DI'daki son `IPermissionResolver` kaydı kazanıyor; birden çok kaynaktan çözüm gerekir.
- Kurum durumu (aktif/askıda/kapalı) tenant çözümünde kontrol edilir: askıdaki kurum hiç çözülmez.
- RLS paketlere girmez; sağlayıcı başına belgelenmiş örnek.
- **Açık tasarım seçimi:** Finbuckle tabanlı bir eklenti (Apache-2.0; stratejiler ve tenant store hazır) mı, yoksa Microsoft'un yapı taşları üzerine kendi küçük uygulamamız mı? EF'in belgesi çekirdeğin küçük olduğunu gösteriyor; Finbuckle hazır ve denenmiş stratejiler getiriyor. Seçim fazda, maliyet hesabıyla (D-016) yapılır.

## 4. Kimlik modeli: senaryolarla

Üç gerçekten farklı model var:

- **P:** Okul başına ayrı hesap (bugünkü CRM).
- **G-a:** Kişi başına tek hesap; okul üyeliği uygulamada; okul her istekte gelir ve sunucu üyeliği doğrular.
- **G-b:** Kişi başına tek hesap; okul girişte seçilir ve token'a yazılır.

| Senaryo | P | G-a | G-b |
|---|---|---|---|
| Ayşe iki okulda danışman | İki hesap, iki parola, iki 2FA | Bir hesap. Okulu ekranda seçer; iki sekmede iki okul açık olabilir. | Bir hesap. Okul değiştirmek için sağlayıcıya gidip yeni token alır; aynı anda tek okul. |
| Giriş ekranı | Okul kodu + e-posta + parola | E-posta + parola | E-posta + parola, ardından okul seçimi |
| Okul askıya alındı | O okulun hesapları giriş yapamaz | O okuldaki üyelik hiçbir şey göstermez; Ayşe öteki okulda çalışmaya devam eder | O okul için token verilmez |
| Yönetici yeni danışman ekliyor | Hesabı kendisi açar | Davet eder: e-posta kayıtlıysa üyelik eklenir, değilse hesap açılır | G-a ile aynı |
| Danışman şube değiştiriyor | Şube token'daysa token yenilenene kadar (en çok 15 dk) eski şubede | Anında: şube sunucuda | Şube token'daysa yenilenene kadar; sunucudaysa anında |
| Okul ayrılıyor (KVKK silme) | Okulun hesapları da silinir; sınır temiz | Üyelikler ve okulun verisi silinir. Hesap kişinindir: başka üyeliği yoksa ayrıca silinir. | G-a ile aynı |
| Parola unutuldu | Okul koduyla sıfırlanır | Tek sıfırlama | Tek sıfırlama |
| Hesap verisinin sorumlusu (KVKK) | Okul | Platform (hesap), okul (üyelik ve okul verisi) | G-a ile aynı |
| NovaCore işi | Kimlik eklentisi baştan sona tenant'lı olur: kullanıcı ve rol tablolarına `TenantId`, tenant'lı unique index'ler, giriş, kayıt ve sıfırlama akışlarında okul ([LoginPage.cs:60](../../../src/BuildingBlocks/NovaCore.Identity.OpenIddict/LoginPage.cs), [AccountEndpoints.cs:132](../../../src/BuildingBlocks/NovaCore.Identity.OpenIddict/AccountEndpoints.cs), [PasswordResetPage.cs:63](../../../src/BuildingBlocks/NovaCore.Identity.OpenIddict/PasswordResetPage.cs)), admin API'si okula kapsamlı | Kimlik eklentisi değişmez. Gereken: üyelik ve kuruma göre izin (§3.5). | G-a'nın hepsi, ek olarak eklentide okul seçim adımı, token'da okul claim'i ve token üretilirken üyelik sorgusu |
| Başka projelere etkisi | Eklenti ya her projede tenant'lı olur ya iki türlü yazılır | Eklenti tek türlü kalır; üyelik isteğe bağlı | G-a'ya isteğe bağlı bir adım |
| Microsoft | "Avoid storing credentials multiple times if you use per-tenant identities" | "Use a single identity for each person when possible" + "Application-based authorization" | Tek kimlik + "the IdP can issue a token that includes the correct tenant identifier claim"; okul değiştirmek yeni token ister |

Kaynak: [MS: Identity considerations], [MS: Identity approaches]. Microsoft ayrıca kullanıcıyla tenant'ı aynı şey saymayı ("User and tenant conflation") bir anti-pattern olarak sayıyor.

**Öneri: G-a.** Gerekçeler:

- Microsoft'un önerdiği model.
- NovaCore'un kendi ilkesiyle aynı: token kim olduğunu söyler, ne yapabileceğini sunucu söyler (D-002, F3a.2, F4c.2).
- Kimlik eklentisinin güvenlik sınırı (2FA, lockout, kurtarma kodları) olduğu gibi kalıyor.
- Eklenti tek türlü kalıyor, yani başka projelere en kolay taşınan model.
- G-b'ye sonradan geçmek mümkün: başka bir API token'dan okul okumak isterse claim adımı eklenir. Tersi daha pahalı.

**G-a'nın bedeli:**

- Hesap okullar arasında ortak: okul yöneticisi hesabı silemez, yalnız üyeliği kaldırır.
- KVKK'da hesap verisinin sorumlusu platform olur. Bu bir ürün ve hukuk kararı.
- Kimlik eklentisinin açık kayıt ucu (`/api/v1/auth/register`) kapatılamıyor [kod]. Üyeliği olmayan hesap hiçbir şey göremez, ama bir B2B üründe açık kayıt anlamsız. Kapatma seçeneği gerekirse bu gerçek bir tüketici isteği olur; Y-19'un şartı karşılanır.

## 5. İlk analizdeki (Haiku) iddiaların doğrulaması

Ayrıntılı tablo bu belgenin ilk sürümündeydi; sonuçları:

| İddia | Sonuç | Kaynak |
|---|---|---|
| "`UseCoreWeb`'de kimlik doğrulama ile yetkilendirme arasına girilemiyor, kanca gerekir" | **Yanlış öncül.** Microsoft'a göre yetkilendirme kimlik doğrulamanın hemen ardından gelir; uygulamanın middleware'i ondan sonra, endpoint'ten önce çalışır. İzin çözümlemesi kimlik doğrulamanın içinde (`IClaimsTransformation`, her `AuthenticateAsync`'te) çalıştığı için araya konan kanca ona yetişmez. Kanca eklenmez; askı kontrolü tenant çözümüne girer. | [MS: ASP.NET Core middleware, IClaimsTransformation] |
| "Tenant yalnız token claim'inden çözülmeli" | **Tek güvenli yol değil.** Yol, alan adı, başlık ya da claim; şart, sunucuda doğrulama. | [MS: Map requests to tenants] |
| "Aynı e-posta iki okulda iki kullanıcı olmalı" | Kod tespiti doğru (`RequireUniqueEmail`, `UserName = Email`, `NormalizedUserName` unique). Gereksinim, Microsoft'un önerisine ters (§4). | [kod], [MS] |
| "İzin modeli şube kapsamını bilmiyor" | Doğru, ve öyle kalmalı: işlem yetkisi endpoint'te, satır kapsamı filtrede (CRM'in ADR-003'ü de böyle ayırıyor). Kuruma göre değişen izin ise NovaCore'un işi (§3.5). | [kod] |
| "Askıdaki kurum için NovaCore'un sırası izin vermiyor" | **Yanlış.** `UseCoreWeb`'den sonra konan middleware her endpoint'ten önce çalışır. Daha sağlamı: askıdaki kurumu hiç çözmemek. | [MS: ASP.NET Core middleware] |
| "NovaCore `CreatedBy`'a kullanıcı adını yazıyor" | **Doğru**, ve NovaCore'un kendi sorunu: kimlik eklentisinde kullanıcı adı e-posta; doğrusu `sub` (D-054 (11) ile tutarlı). CRM'den bağımsız düzeltilir. | `AuditableEntityInterceptor.cs` [kod] |
| "Outbox dağıtıcısı tenant bağlamını geri yüklemiyor" | **Doğru.** Trace bağlamıyla birlikte çözülür (§3.5). | `OutboxDispatcher.cs:163` [kod] |
| "PostgreSQL için testler uyarlanmalı" | Asıl engel test değil, lisans ve şablon (§2). | [NuGet], [kod] |
| RLS, soft delete, jsonb, öğrenci modeli, huni raporu | CRM'in domain'i ya da açık işi. RLS sağlayıcıya özgü bir örnek olarak belgelenir. | — |

**İlk analizin kaçırdıkları:**

1. **Lisanslar.** Npgsql ve Shouldly (`BSD-3-Clause`) Y-06'nın dışında [NuGet].
2. **Tek `IClaimsTransformation`.** `AuthenticationService` kurucusunda tek bir `IClaimsTransformation` alıyor [MS: AuthenticationService ctor]; NovaCore kendininkini `AddScoped` ile kaydediyor ([AuthorizationExtensions.cs:25](../../../src/BuildingBlocks/NovaCore.AspNetCore/Authorization/AuthorizationExtensions.cs)). Bir proje sonradan kendi dönüşümünü eklerse NovaCore'un izin çözümlemesi sessizce devreden çıkar: her korumalı uç 403 döner, token'daki `permission` claim'leri de artık silinmez. NovaCore bunu açılışta yakalamalı; kalıp, resolver eksikken açılışı durduran mevcut `PermissionResolverStartupCheck`'inki.
3. **Kendi IdP'ni çalıştırmak** Microsoft'un çok kiracılı rehberinde bir anti-pattern ("Self-administered identity systems") [MS: Identity approaches]. D-037 bunu bilerek seçti; yeniden açmıyorum, ama kişisel veri tutan çok kiracılı bir ürün için bu uyarı bilinmeli.

## 6. Kararlar (kullanıcı, 2026-10-23)

| # | Soru | Karar | Etkisi |
|---|---|---|---|
| R1 | Veritabanı sağlayıcısının lisansı (Y-06'yı yeniden açar) | **Projede adlı istisna.** Sağlayıcı ve sürücüsü projenin seçimidir; projenin lisans kapısı onu, projede adıyla kaydedilmiş bir istisna olarak geçirir (D-052'deki Polly kalıbı). Y-06 diğer paketler için değişmez. | Y-06 revizyon kuralıyla işaretlenir, yeni bir Y/D kaydı sağlayıcı maddesini taşır. NovaCore'un kendi PostgreSQL test katmanı Npgsql'i aynı yolla adlı istisna olarak alır. |
| R2 | Çok kiracılılık NovaCore'a girsin mi (Y-14'ü ve tuketici-hazirligi §P2'yi yeniden açar) | **Evet, isteğe bağlı bir paket** ve gereken çekirdek değişiklikleri (§3.5). Şube, roller ve KVKK kuralları CRM'de kalır. | Y-14 bu konu için istisnalı olur, tuketici-hazirligi §P2'nin çok kiracılılık cümlesi kapanır. |
| R3 | Kimlik modeli | **G-a:** kişi başına tek hesap, okul üyeliği uygulamada, okul her istekte gelir ve sunucu doğrular. | Kimlik eklentisi değişmez. KVKK'da hesap verisinin sorumlusu platform. |
| R4 | Gelecekteki projeler | **Birçok kuruma satılan ürünler (B2B SaaS).** | R2'nin yeniden kullanım gerekçesi karşılanıyor (§3.3). |

Seçeneklerin tamamı ve gerekçeleri §2–§4'te. R1'de reddedilenler: izin listesini OSI onaylı izin veren lisanslarla genişletmek (yalnız veritabanını değil bütün paketleri açardı) ve sağlayıcıları kapının tamamen dışında tutmak (GPL'li bir sürücü sessizce girerdi).

**Sıradaki adımlar:**

1. **NovaCore fazı: veritabanı özgürlüğü (§2.2).** CRM'den bağımsız; çok kiracılılıktan önce gelir, çünkü çok kiracılılığın testleri iki sağlayıcıda koşmalı.
2. **NovaCore fazı: çok kiracılılık (§3.5).** Kapsamı: kuruma göre izin, tek `IClaimsTransformation` koruması ve `CreatedBy` = `sub`.
3. **CRM:** İş kuralı ve validasyon envanteri (kod değil, mantık). Sonra modül modül yeniden kurulum.

**Doğrulanmayanlar:**

- Finbuckle'ın davranışları Microsoft kaynağı değil; CRM'in ADR'lerinden alındı ve burada denenmedi.
- NovaCore'un PostgreSQL üzerinde çalıştığı ölçülmedi.
- MySQL/MariaDB'nin şema davranışı denenmedi.
- CRM'in `Adaylar`, `Raporlar` ve `Randevular` kodu okunmadı; iş kuralı envanteri ayrı bir iş.

## Kaynaklar (Microsoft Learn)

- EF Core, Database Providers: https://learn.microsoft.com/ef/core/providers/
- EF Core, Migrations with multiple providers: https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers
- EF Core, Multi-tenancy: https://learn.microsoft.com/ef/core/miscellaneous/multitenancy
- EF Core, Global query filters (EF 10'da adlı filtreler): https://learn.microsoft.com/ef/core/querying/filters
- EF Core, Interceptors: https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors
- EF Core, Applying migrations: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying
- ASP.NET Core middleware (sıra, `WebApplication`'ın eklediği middleware): https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0
- `IClaimsTransformation.TransformAsync`: https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authentication.iclaimstransformation.transformasync?view=aspnetcore-10.0
- `AuthenticationService` kurucusu: https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authentication.authenticationservice.-ctor?view=aspnetcore-10.0
- Identity model customization: https://learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0
- Minimal API filters: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-10.0
- SQL Server, Row-level security: https://learn.microsoft.com/sql/relational-databases/security/row-level-security
- Azure Architecture Center, Identity considerations: https://learn.microsoft.com/azure/architecture/guide/multitenant/considerations/identity
- Azure Architecture Center, Identity approaches: https://learn.microsoft.com/azure/architecture/guide/multitenant/approaches/identity
- Azure Architecture Center, Map requests to tenants: https://learn.microsoft.com/azure/architecture/guide/multitenant/considerations/map-requests
- Azure Architecture Center, Storage and data approaches: https://learn.microsoft.com/azure/architecture/guide/multitenant/approaches/storage-data
- Azure Architecture Center, Tenant life cycle: https://learn.microsoft.com/azure/architecture/guide/multitenant/considerations/tenant-life-cycle
- Azure Architecture Center, Noisy Neighbor antipattern: https://learn.microsoft.com/azure/architecture/antipatterns/noisy-neighbor/noisy-neighbor
