# NovaCore'u yeni projelerin altyapısı yapmak: sorumluluk, eksikler, fazlalar

> **Durum: plana yansıtıldı (2026-09-28, D-033).** Hangi bulgunun hangi adıma gittiği, hangisinin bilerek dışarıda kaldığı §8'de. Kalan kullanıcı kararları kendi fazlarının başında sorulur: K1 → F5.4, K2 → F4d.1. K3 projenin kararı; NovaCore'daki karşılığı F5.5'teki README önerisi. K4 D-033 ile cevaplandı. K5: `AppException` ailesi kalıyor (F4c.4).
> Kaynak özetleri: [RESEARCH.md](../RESEARCH.md) R-071…R-074. Kod durumu: `main` @ `8fec6b8`.

**Soru:** Sıfırdan bir web projesi açılacak ve altyapısı NovaCore olacak. Hangi iş NovaCore'un, hangisi projenin? Neyi doğru yapıyoruz, ne eksik, ne fazla?

**Etiketler:** **[kaynak]** birincil kaynakla doğrulandı · **[kod]** NovaCore kodunda doğrulandı · **[okuyarak]** koddan çıkarıldı, çalıştırılıp ölçülmedi · **[tercih]** birincil kaynağı yok.

---

## 1. Özet

1. **NovaCore'un tasarımı doğru yönde.** Microsoft'un önerdiği yerleşik yapı taşlarını kullanıyor: HybridCache, TimeProvider, validated options, .NET 10 validation, yalnızca asimetrik JWT, sunucu tarafı izinler, açık modül kaydı, PublicAPI takibi. Değiştirilmesi gereken bir temel yok.
2. **En büyük eksik dağıtım.** Bugün bir proje NovaCore'u paket olarak alamıyor. Şablon ise bütün repoyu kopyalıyor.
3. **İkinci eksik güvenli varsayılanlar.** Yetkilendirme varsayılan olarak açık. Proxy başlıkları işlenmiyor. Data Protection anahtarları kalıcı değil. Üçü de gerçek bir sunucuya çıkan her projeyi etkiliyor.
4. **Servisler açık, pipeline kapalı.** Proje NovaCore'un servis ayarlarını Options sistemiyle ezebiliyor, ama bu hiçbir yerde belgelenmemiş. Middleware sırası, exception handler sırası ve izin önbelleğinin anahtarı ise dışarıdan değiştirilemiyor.
5. **Sorumluluk sınırı Microsoft'un kendi modelinden okunabilir.** Aspire, ortak varsayılanları paket olarak değil, projeye kopyalanan ve "istediğin gibi değiştir" denen kaynak kod olarak dağıtıyor. NovaCore paketle dağıtıldığı için (D-001) proje onu düzenleyemez. Bu yüzden paket ya ayar noktası sunmalı ya da o iş şablona (kaynağa) bırakılmalı.
6. **Fazlalar küçük.** Şablon repoyu olduğu gibi kopyalıyor. Framework exception'ları 4xx'e eşleniyor ve kod hatalarını istemci hatası gibi gösteriyor. Şablonda iki ayrı log hattı var.

---

## 2. Sorumluluk modeli

### 2.1 Kaynakların söylediği

- **Aspire ServiceDefaults [kaynak]:** Health, OpenTelemetry (log, metrik, trace), resilience ve service discovery tek bir projede. Bu proje NuGet paketi değil, çözüme eklenen kaynak kod. Sayfa kodun bir başlangıç noktası olduğunu ve serbestçe değiştirilebileceğini söylüyor (R-071).
- **Kütüphane yazarları için Options rehberi [kaynak]:** Kütüphane, tüketicinin ayar verebilmesi için `Action<TOptions>` ya da `IConfiguration` alan overload'lar sunar (R-071).
- **Y-24 / R-018 [kaynak]:** Kütüphane host kararı vermez: sabit log sink'i, global state, `BuildServiceProvider()` yok.

Sonuç: Paketle dağıtılan bir kompozisyon kökü, projenin değiştirmek isteyeceği her yerde bir ayar noktası sunmalıdır. Değiştirilemeyecek kadar ortama bağlı olan her şey ise şablona ya da projeye aittir.

### 2.2 Üç katman

| Katman | İçerik | Sahibi | Nasıl değişir |
|---|---|---|---|
| **NovaCore paketleri** | Doğru yapılması zor, güvenlik açısından kritik ve her projede aynı olması gereken mekanizmalar | NovaCore | SemVer sürümü, PublicAPI takibi, CHANGELOG |
| **Şablon** (`dotnet new` çıktısı) | `Program.cs`, `appsettings*.json`, test projesi, CI, örnek modül | Üretildiği andan itibaren **proje** | Projede düzenlenir. NovaCore güncellemesi şablon kodunu değiştirmez |
| **Proje** | İş modülleri, ortam kararları, işletim | Proje | — |

**Bir şeyin nereye gittiğine karar kuralı:**
1. Güvenlik veya tutarlılık için kritik, doğru yapılması zor ve en az iki projede gerekiyor → **paket** (Y-14).
2. İskeleti ortak ama her proje farklı değer seçecek → **şablon**.
3. Ortama bağlı (proxy, anahtar deposu, secret deposu, veritabanı hesabı, IdP barındırma) → **proje**. Paket yalnızca doğru yeri ve sırayı sağlar.

### 2.3 Konu bazında sorumluluk

| Konu | NovaCore paketi | Şablon | Proje |
|---|---|---|---|
| HTTP pipeline sırası | `UseCoreWeb` sırayı belirler | `UseCoreWeb`'i çağırır | Kendi middleware'ini paketin ayırdığı yere koyar (bugün böyle bir yer yok, E3) |
| Token doğrulama | JwtBearer, yalnızca asimetrik, `Jwt:Authority` | `Jwt:Authority`/`Audience` değerleri | IdP'yi seçer ve işletir |
| Yetkilendirme | `RequirePermission`, önbellek, resolver sözleşmesi, varsayılan politika (E2) | — | İzin tanımları, harici IdP'de resolver, **nesne düzeyi yetki** (E11) |
| Hata sözleşmesi | ProblemDetails, `Result`→HTTP | — | Domain hataları |
| Veri | `AddModuleDbContext`, outbox, audit | Provider seçimi (`UseSqlServer`) | Şema, migration üretimi ve **uygulanması** (E8), yedekleme |
| Gözlem | Serilog ve OTel eklentileri | Hangisinin açılacağı (F4) | Backend (Seq, OTLP collector), alarmlar |
| Proxy ve TLS | `UseForwardedHeaders`'ı doğru sıraya koymak (E4) | `KnownProxies`/`KnownNetworks` config'i | Reverse proxy, TLS sertifikası |
| Data Protection | Kimlik eklentisi, anahtar deposu yoksa açılışı durdurur (E5) | Anahtar deposu örneği | Anahtar deposu (DB, volume ya da Key Vault) |
| Health | Kontroller (DB, liveness) | Üretim probe eşlemesi (E9) | Orchestrator probe ayarı |
| Frontend kimliği | — | — | SPA + BFF ya da server-rendered UI (K3) |
| Test | — | Test projesi, factory, test IdP (E12) | Testler |
| Yükseltme | SemVer, CHANGELOG, destek politikası | — | Paket ve .NET yükseltmesi |

### 2.4 Yeni proje açarken projenin yapacakları

Bu liste bugünkü koda göre hazırlandı. E-maddeleri kapandıkça kısalır.

- [ ] `Jwt:Authority` ve `Jwt:Audience` değerlerini ver. IdP modelini seç (K2).
- [ ] `Cors:AllowedOrigins`'i doldur. **Liste boşsa her origin'e izin veriliyor** ([CoreWebExtensions.cs:143](../../../src/BuildingBlocks/NovaCore.AspNetCore/Web/CoreWebExtensions.cs)) [kod].
- [ ] `AllowedHosts`'u gerçek alan adlarıyla sınırla. Şablonda `"*"` duruyor ([appsettings.json](../../../src/Api/NovaCore.Api/appsettings.json)) [kod].
- [ ] Bağlantı dizesini değiştir. Şablondaki Windows kimlik doğrulaması yalnızca yerel içindir [kod].
- [ ] Development dışında bir `IEmailSender` kaydet. Yoksa host açılmaz (F0.5) [kod].
- [ ] OpenIddict imza ve şifreleme sertifikalarını ekle (D-026 (5)).
- [ ] Data Protection anahtar deposunu yapılandır (E5).
- [ ] Reverse proxy arkasındaysan forwarded headers'ı yapılandır (E4).
- [ ] Migration'ları CI'da bundle ya da idempotent script olarak üret. Üretimde `Database:ApplyMigrations=false` kalsın, uygulama DB hesabı DDL yetkisi taşımasın (E8).
- [ ] Üretim health probe'larını eşle (E9).
- [ ] Log ve trace hedefini seç: `OTEL_EXPORTER_OTLP_ENDPOINT` ve/veya `Seq:ServerUrl` (F4).
- [ ] Secret'ları Development'ta user-secrets'ta, üretimde ortam değişkeni ya da bir vault'ta tut (Y-35).

---

## 3. Neyi doğru yapıyoruz (dokunmayın)

| Konu | Kaynakla karşılaştırma |
|---|---|
| Health uçları yalnızca Development'ta (D-022) | Aspire aynısını yapıyor: prod'da health ucu açmanın güvenlik etkisi olduğunu söylüyor [kaynak, R-071] |
| Yalnızca asimetrik JWT, parola karşılığında token üretilmiyor (D-002, D-028) | MS JWT rehberi: asimetrik anahtar kullan, kullanıcı adı/parola isteğinden token üretme [kaynak, R-010] |
| Startup migration opt-in ve varsayılanı kapalı | EF: runtime migration ödünleşimlerle kabul edilebilir, gözden geçirme ve en az yetki gerekiyorsa ayrı adım tercih edilir [kaynak, R-072] |
| HTTP istemcilerinde standard resilience handler | Aspire ServiceDefaults ile aynı [kaynak] |
| HybridCache, TimeProvider, validated options, .NET 10 validation | R-008, R-012, R-005, R-001 |
| İzinlerin sunucu tarafında çözülmesi | Token kimliği taşır, yetkiyi değil (D-024) |
| Paket ve API disiplini (PublicAPI, package validation, CPM, SemVer) | R-009, R-011 |
| Transactional outbox, idempotent tüketici | R-068 |

---

## 4. Eksikler

Öncelikler: **P0** bir projeye bağlamadan önce kapanmalı · **P1** ilk üretim sürümünden önce · **P2** projeye göre değişir.

### P0

**E1. Tüketim kanalı yok. [kod + kaynak]**
- Repoda `v*` tag'i yok, hiçbir paket yayımlanmadı.
- GitHub Packages paket herkese açık olsa bile indirmek için `read:packages` yetkili token istiyor (R-074).
- `.template.config/template.json` kaynak klasörü vermiyor, yani repo kökünü kopyalıyor: BuildingBlocks kaynağı, plan belgeleri, hook'lar, `release.yml`, yazar kimliği. Microsoft'un önerdiği yol, şablonların bir `content` klasöründe durduğu ayrı bir template package projesi (`templatepack`) (R-074).
- PLAN F5.1 ve F5.2 bunu kapsıyor, ama Faz 5 son sırada.
- **Seçenekler:**
  - (a) **nuget.org:** Token gerekmez, ama D-013'ü değiştirir.
  - (b) **GitHub Packages:** Her proje ve CI bir token taşır.
  - (c) **Yerel klasör feed'i:** Dışarıya hiçbir şey gitmez, ama yalnızca bu makinede çalışır.
- **Öneri:** İlk proje için (c), yayına hazır olunca (a). Karar kullanıcının (K1).

**E2. Yetkilendirme varsayılan olarak açık. [kod + kaynak]**
- `AddAuthorization()` bir FallbackPolicy olmadan çağrılıyor ([AuthorizationExtensions.cs:25](../../../src/BuildingBlocks/NovaCore.AspNetCore/Authorization/AuthorizationExtensions.cs)). `RequirePermission`'ı unutulan bir endpoint herkese açık kalır.
- MS: Varsayılan olarak kimlik doğrulama istemenin tercih edilen yolu fallback policy (R-073).
- OWASP API5:2023 "Broken Function Level Authorization" tam olarak bu sınıfı tanımlıyor (R-073).
- **Öneri:** `FallbackPolicy = RequireAuthenticatedUser`. Anonim uçlar tek tek `AllowAnonymous` alır: kimlik eklentisinin giriş, kayıt ve `/connect/*` uçları, health, OpenAPI ve Scalar.
- **Daha basit alternatif:** Her endpoint'i tarayan bir mimari test. Bu test NovaCore'un kendi test projesinde kalır, projeye gitmez.

**E3. Servisler açık ama belgesiz, pipeline kapalı. [kod + kaynak]**
- **Açık olanlar:** NovaCore servis ayarlarını Options sistemiyle kaydediyor. Proje `PostConfigure<JwtBearerOptions>`, `PostConfigure<RateLimiterOptions>`, `PostConfigure<CorsOptions>`, `PostConfigure<AuthorizationOptions>` ile bunları ezebilir. Ama hangi ayarın ezilebileceği hiçbir yerde yazmıyor.
- **Kapalı olanlar:**
  - Middleware sırası: `UseCoreWeb` kimlik doğrulama ile yetkilendirmeyi art arda çağırıyor, araya bir şey konamıyor ([CoreWebExtensions.cs:76](../../../src/BuildingBlocks/NovaCore.AspNetCore/Web/CoreWebExtensions.cs)).
  - Exception handler sırası: Her istisnayı yakalayan handler `AddCoreWeb` sırasında kaydediliyor ([InfrastructureServiceCollectionExtensions.cs:27](../../../src/BuildingBlocks/NovaCore.AspNetCore/DependencyInjection/InfrastructureServiceCollectionExtensions.cs)). MS'e göre handler'lar kayıt sırasıyla çalışır ve ilk `true` zinciri keser. Sonradan kaydedilen bir handler hiç çalışmaz (R-073).
  - İzin önbelleği anahtarı: Yalnızca `sub` + security stamp ([PermissionClaimsTransformation.cs:58](../../../src/BuildingBlocks/NovaCore.AspNetCore/Authorization/PermissionClaimsTransformation.cs)). `iss` yok (PROGRESS'te zaten bekliyor), proje ek bir bölümleme anahtarı veremiyor.
- **Öneriler:**
  - Servis tarafı için kod değil **belge**: README'de "ezilebilir ayarlar" tablosu. Bu, her yere `Action<TOptions>` overload'u eklemekten daha basit, çünkü Options sistemi zaten açık.
  - Pipeline için `UseCoreWeb`'e kimlik doğrulamadan hemen sonra çalışan tek bir yer.
  - Exception eşlemesini `ExceptionHandlerOptions.StatusCodeSelector` ve ProblemDetails özelleştirmesine taşımak. MS'e göre bu yedek yol yalnızca kayıtlı her handler `false` dönerse kullanılır, yani sıra önemini yitirir. `detail` ve log davranışının korunduğu bir fazda testle doğrulanmalı.
  - İzin önbelleği anahtarına `iss` eklemek.

**E4. Proxy başlıkları işlenmiyor. [kod + kaynak]**
- Kodda `ForwardedHeaders` yok.
- MS: Middleware IIS dışında varsayılan olarak kapalı, açılınca bile `ForwardedHeaders.None` ile başlar. Diğer middleware'lerden, en geç `UseHsts`'ten önce çalışmalıdır. Başlıkları yalnızca `KnownProxies`/`KnownNetworks`'teki proxy'lerden kabul eder (R-072).
- **Etkisi:**
  - Rate limiter istemcileri `RemoteIpAddress`'e göre ayırıyor, yani proxy arkasında herkes proxy'nin IP'si sayılır. Tek bir istemci limiti herkes için doldurur.
  - İstek şeması `http` görünür.
  - Kimlik eklentisinin 2FA ve antiforgery cookie'leri `Secure` bayrağı olmadan çıkar (PROGRESS notu).
- **Sorumluluk:** Paket `UseForwardedHeaders`'ı pipeline'ın en başına koyar ve ayarlarını config'ten doğrular. Hangi proxy'ye güvenileceğini proje söyler.
- **Alternatif:** Tamamen host'a bırakmak. Ama `UseCoreWeb` pipeline'ı kendisi kurduğu için host bu middleware'i doğru yere koyamaz.

**E5. Data Protection anahtarları kalıcı değil. [kod + kaynak]**
- Kimlik eklentisi Data Protection'a üç yerde dayanıyor: uygulama ve 2FA cookie'leri, antiforgery ve e-posta onay token'ları ([IdentityServerHostingExtensions.cs:74](../../../src/BuildingBlocks/NovaCore.Identity.OpenIddict/IdentityServerHostingExtensions.cs)). Kodda hiçbir yerde anahtar deposu yapılandırılmıyor.
- MS: Container'da anahtarlar kalıcı bir volume'a ya da harici bir sağlayıcıya yazılmalı. Web farm'da varsayılan yapılandırma genellikle uygun değil (R-072).
- **Etkisi:** Her yeniden dağıtımda oturumlar düşer ve onay linkleri geçersiz olur. Birden çok örnek çalışırsa her örnek diğerinin cookie'sini reddeder.
- **Seçenekler:**
  - (a) Eklenti `PersistKeysToDbContext<IdentityServerDbContext>` ile anahtarları kendi DB'sine yazar. Microsoft'un `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` paketini gerektirir: MIT lisanslı, ama yeni paket olduğu için hook onayı ister.
  - (b) Depo seçimini host'a bırakıp Development dışında depo yoksa açılışı durdurmak (F0.5'teki `IEmailSender` kalıbı).
- **Öneri:** (b). Depo seçimi ortama bağlı bir karar (Y-24), ama unutulması sessiz bir kesinti üretiyor. Şablona (a) bir örnek olarak girebilir.

### P1

**E6. Yanıt sözleşmesi dönüş tipinde değil. [kod + kaynak]**
- `ToHttpResult` `IResult` dönüyor. Örnek modülde [GetProductById.cs:25](../../../src/Modules/Catalog/NovaCore.Modules.Catalog/Features/GetProductById.cs) `Task<IResult>` dönüp yanıtı `.Produces<>` ile ayrıca bildiriyor.
- MS: `Produces` endpoint'in davranışını kısıtlamaz. `Results<T1,…>` ise meta veriyi kendisi taşır ve yanlış bir yanıt tipini derleme anında yakalar (R-073).
- PROGRESS'teki Y-31 kör noktası (mimari testin `IResult` dönen handler'ların yanıtını görememesi) da buradan geliyor.
- **Öneri:** `Result<T>`'yi tipli bir union'a çeviren bir yol. Public API değişikliği olduğu için 1.0'dan önce yapılmalı.

**E7. Framework exception'ları 4xx'e eşleniyor.** Bkz. F3.

**E8. Migration'ı üretime taşıyan bir yol yok. [kod + kaynak]**
- NovaCore'un CI'ı yalnızca `has-pending-model-changes` kontrolü yapıyor. Şablon bir bundle ya da script üretmiyor.
- EF: Otomatik dağıtımda migration bundle, SQL'in gözden geçirilmesi gerekiyorsa idempotent script kullanılmalı. Şemayı değiştiren hesap, uygulamanın çalışma zamanı hesabından ayrı olmalı (R-072).
- Bir NovaCore projesinde birden çok context var: modül başına bir, artı kimlik. Her context için ayrı bundle gerekir.
- **Sorumluluk:** Şablonun CI'ı bundle'ları üretir. Proje dağıtımda uygular.

**E9. Üretim health probe'u için bir örnek yok. [kod]**
- D-022 doğru (Aspire de aynısını yapıyor), ama host'un prod'da nasıl probe açacağı kod olarak hiçbir yerde yok.
- R-049'daki yöntem (yönetim portuna `LocalPort` kontrolü) şablonda örnek olarak durmalı. Timeout eksiği PROGRESS'te notlu.

**E10. İstek zaman aşımı yok. [kaynak]**
- ASP.NET Core varsayılan olarak istekleri zaman aşımına uğratmıyor. Request timeouts middleware'i açıkça eklenmeli (R-073).
- OWASP API4:2023 "Unrestricted Resource Consumption": rate limiting ve sayfalama NovaCore'da var, zaman aşımı yok.
- **Öneri:** Middleware paket içinde doğru sırada dursun, varsayılan süreyi şablon versin. MS'e göre uygun süre senaryoya göre çok değişiyor.

**E11. Nesne düzeyi yetkilendirme için bir kalıp yok. [kaynak]**
- NovaCore'un izin modeli işlev düzeyinde: "bu kullanıcı ürün silebilir mi" (OWASP API5).
- OWASP'ın 2019'dan beri bir numaralı riski API1:2023 "Broken Object Level Authorization": "bu kullanıcı *bu* kaydı silebilir mi".
- MS'in çözümü resource-based authorization: `IAuthorizationService.AuthorizeAsync(user, resource, requirement)` (R-073).
- **Sorumluluk:** Kontrolün kendisi projenin. Örnek modülde bu kalıbın bir örneği olmalı (şablon). Çekirdek özelliği değil (Y-14).

**E12. Test altyapısı paketlenmemiş. [kod + kaynak]**
- [TestIdentityProvider.cs](../../../tests/NovaCore.Api.Tests/TestIdentityProvider.cs) ve `NovaCoreApiFactory` test projesinde. Kimlikli entegrasyon testi yazmak isteyen proje bunları kopyalamak zorunda.
- MS'in daha basit yolu bir `TestAuthHandler` (R-073). Ama JwtBearer doğrulamasını atladığı için NovaCore'un token doğrulama yolunu test etmez.
- **Öneri:** Factory ve test IdP'si şablonun test projesine girsin (kaynak). Ayrı bir `NovaCore.Testing` paketi Y-14 gereği iki tüketiciyi bekler.

**E13. Outbox birden çok örnekte güvenli değil. [kod]**
- Dispatcher veritabanı başına tek bir örnek varsayıyor. İki örnek aynı mesajı aynı anda işleyebilir. `outbox_consumers` kaydı handler'ın transaction'ına girmediği için bu durumda çift çalışmayı önleyemez.
- İşlenmiş kayıtlar hiç silinmiyor. Mesajda trace bağlamı yok, bu yüzden handler'ın span'i isteğin trace'ine bağlanmıyor.
- **Sorumluluk:** Proje yatayda ölçeklenecekse bu P0 olur. Tek örnekle çalışacaksa P2 kalır.

**E14. Kimlik eklentisinin eksikleri (proje eklentiyi kullanacaksa). [kod]**
- Parola sıfırlama yok, kurtarma kodları kullanılamıyor.
- `/connect/endsession` CSRF'e açık.
- Giriş sayfası bilinen e-postayı yanıt süresinden ele veriyor.
- OpenIddict token tabloları hiç temizlenmiyor.
- `ApplicationUser` sealed, token'a claim eklemenin bir yolu yok.
- Super-admin kontrolü token'daki rol claim'ine dayanıyor: Administrator rolünden çıkarılan biri token dolana kadar (15 dk) admin kalır.
- Hepsi PROGRESS'te notlu. Kullanıcıları olan bir ürün için ilk ikisi zorunlu.

### P2 (projeye göre)

- **API sürümleme:** Şablon `/api/v1` önekini kullanıyor. İkinci bir sürüm gerçekten gerekene kadar bu yeterli (Y-19). Sürümleme gerekirse projenin kararı.
- **Frontend kimliği:** Bkz. K3.
- **Çok kiracılılık, arka plan işleri, dosya depolama, yerelleştirme:** Projenin işi. İkinci bir proje aynı ihtiyacı duyarsa çekirdek değerlendirilir (Y-14).

---

## 5. Fazlalar

**F1. Şablon bütün repoyu kopyalıyor. [kod]**
Bkz. E1. Üretilen projeye BuildingBlocks kaynağı, plan belgeleri, `.claude/` hook'ları, `release.yml` ve yazar kimliği gidiyor. F5.1 bunu kapsıyor, ama yeni bir proje açılmadan önce yapılmalı.

**F2. Log hattı ikiye bölünmüş. [kod + kaynak]**
- Serilog eklentisi varsayılan log sağlayıcılarını değiştiriyor ([SerilogHostingExtensions.cs](../../../src/BuildingBlocks/NovaCore.Serilog/SerilogHostingExtensions.cs)). OTel eklentisi yalnızca metrik ve trace topluyor.
- Şablon ikisini birden açtığı için loglar console/Seq'e, trace'ler OTLP'ye gidiyor.
- Aspire ServiceDefaults log, metrik ve trace'in üçünü de OpenTelemetry'den geçiriyor (R-071).
- **Öneri:** Şablon varsayılan olarak tek bir hat açsın. Serilog opsiyonel eklenti olarak kalabilir. Hangisinin açılacağı şablonun, hedef backend projenin kararı.

**F3. Framework exception'ları 4xx'e eşleniyor. [kod + kaynak]**
- `GlobalExceptionHandler` `ArgumentException`, `FormatException` ve `KeyNotFoundException`'ı 400/404'e, `UnauthorizedAccessException`'ı 403'e eşliyor ([GlobalExceptionHandler.cs](../../../src/BuildingBlocks/NovaCore.AspNetCore/Exceptions/GlobalExceptionHandler.cs)).
- Bunlar çoğunlukla bir kod hatasının işareti. İstemciye 4xx olarak döner ve yalnızca Warning seviyesinde loglanırlar, yani hata gizlenir.
- MS: Exception'lar olağandışı durumlar içindir, normal akışı yönetmek için kullanılmaz (R-073). NovaCore'da beklenen hataların yolu zaten `Result` (Y-28).
- **Öneri:** Eşlemede `AppException` ve `BadHttpRequestException` kalsın. `BadHttpRequestException`'ın kendi durum kodu kullanılsın; bu, PROGRESS'teki "bozuk gövde 500 dönüyor" maddesini de kapatır.
- `AppException` ailesinin kendisi CLAUDE.md'de bir standart. Onu kaldırmak kullanıcının kararı (K5).

**F4. Çekirdek bir eklentinin yolunu biliyor. [kod]**
`SecurityHeadersMiddleware` `/scalar` yolunu CSP'den muaf tutuyor. Küçük bir bağımlılık, PROGRESS'te notlu.

**F5. Outbox'ı atlayan ikinci bir event yolu açık. [kod]**
- `IDomainEventDispatcher` Abstractions'ta public ve DI'da kayıtlı. Bugün yalnızca outbox dispatcher'ı kullanıyor.
- Proje onu doğrudan çağırırsa event transactional outbox'ı atlar.
- **Öneri:** Arayüz internal olsun. Public API değişikliği olduğu için 1.0'dan önce yapılmalı.

---

## 6. Kullanıcının vereceği kararlar

- **K1. Dağıtım kanalı:** nuget.org (D-013 değişir), GitHub Packages (token gerekir) ya da yerel feed.
- **K2. IdP modeli:**
  - (a) Her proje kendi API host'unda kendi IdP'sini çalıştırır (bugünkü model).
  - (b) Tüm projeler için tek bir merkezi IdP. Projeler arası tek oturum açma mümkün olur. API'ler yalnızca token doğrular ve her proje kendi `IPermissionResolver`'ını yazar.
  - (c) Harici bir IdP: Entra ya da Keycloak.
  - NovaCore çekirdeği üçünü de destekliyor (D-002). Fark operasyon yükünde ve E14'ün kimin sorunu olduğunda.
- **K3. Frontend:**
  - MS: Public OIDC/OAuth istemcileri web uygulamaları için artık önerilmiyor. Tarayıcı uygulamaları için BFF öneriliyor: token'lar sunucuda kalır, tarayıcıya yalnızca HttpOnly bir cookie gider (R-073).
  - **Seçenekler:** (a) SPA + BFF host'u (OIDC confidential client + cookie + YARP forwarder); (b) server-rendered UI (Blazor Web App ya da Razor); (c) yalnızca API.
  - Bu seçim şablonu ve kimlik eklentisinin client ayarlarını belirler. NovaCore'da bir BFF paketi ancak iki proje ihtiyaç duyarsa düşünülür (Y-14).
- **K4. Plan:** E2–E7 ve F3–F5 public API'yi ya da güvenlik varsayılanlarını değiştiriyor. 1.0'dan (F5.4) önce yapılmaları gerekiyor. 4b ile 5 arasına yeni bir faz mı eklenecek, yoksa Faz 5'in başına mı alınacaklar? PLAN.md korunuyor.
- **K5. `AppException` ailesi:** Olduğu gibi mi kalacak, yalnızca belgelenmiş bir kaçış yolu olarak mı kullanılacak, yoksa kaldırılıp `Result` tek yol mu olacak?

## 7. Önerilen sıra

1. **Faz 4b** (planlandığı gibi).
2. **Yeni faz "tüketici hazırlığı"** (K4): E2, E3, E4, E5, E6, E10, F3, F5. Hepsi paket içinde, testle korunabilir, public API değişiklikleri 1.0'dan önce toplanır.
3. **Faz 5, genişletilmiş:**
   - E1 ve F1: template package ve tüketici smoke testi.
   - E12: test projesi şablonda.
   - E8: bundle üreten CI.
   - E9 ve E11: şablondaki örnekler.
   - F2: şablonun log hattı.
   - §2.4'teki kontrol listesi README'ye.
4. **İlk gerçek proje:** §2.4'teki listeyi uygular. E13 ve E14'ten gerekenler projenin ihtiyacına göre öne alınır.

---

## 8. Plana yansıması (D-033)

| Bulgu | Adım | Not |
|---|---|---|
| E1 Tüketim kanalı | F5.1, F5.2, F5.4 | Kanal kararı F5.4'te (K1) |
| E2 Yetkilendirme varsayılanı açık | F4c.1 | Development dışında boş CORS listesi de aynı adımda kapanıyor |
| E3 Servisler belgesiz, pipeline kapalı | F4c.4 (handler sırası), F5.5 (ezilebilir ayarlar tablosu) | Middleware yeri ve izin önbelleği bölümlemesi bilerek dışarıda (tüketicisi yok, Y-14) |
| E4 Forwarded headers | F4c.3 | |
| E5 Data Protection | F4c.6 | Seçenek (b): açılış koruması, depoyu host seçer |
| E6 Tipli yanıt sözleşmesi | F4c.5 | |
| E7 / F3 Framework exception eşlemesi | F4c.4 | `AppException` kalıyor |
| E8 Migration dağıtımı | F5.1 (şablon CI'ı), F5.5 (README) | |
| E9 Üretim health probe'u | F5.1 | |
| E10 Request timeouts | — | Dışarıda, tetik koşulu PROGRESS'te |
| E11 Nesne düzeyi yetki | F5.5 | Şablon koda değil, README rehberine |
| E12 Test altyapısı | F5.1 | Şablonun test projesinde (kaynak) |
| E13 Outbox | F4c.7 (anahtar, temizlik) | Çoklu örnek kilidi ve trace bağlamı dışarıda |
| E14 Kimlik eklentisi | F4d.2–F4d.5; super-admin F4c.2 | 4d, F4d.1'e (K2) bağlı. Claim kancası dışarıda |
| F1 Şablon repoyu kopyalıyor | F5.1 | |
| F2 İki log hattı | F5.1 | Tek hat fazda bir R-kaydıyla seçilecek |
| F4 Çekirdekte `/scalar` yolu | — | PROGRESS'te kalıyor |
| F5 İkinci event yolu | F4c.7 | Arayüz ve implementasyonu siliniyor (Y-13) |

**4b ve 5'ten çıkarılanlar:**
- Abstractions'ta Id bazlı entity eşitliği (Y-14).
- 4b'deki akışın gerektirmediği genel `Product` invariant'ları (Y-16).
- Faz 5'teki 1.0.0 kararı: ilk gerçek projeden sonraya bırakıldı.
- F5.3 kaldırılmadı, 4b'ye taşındı.
