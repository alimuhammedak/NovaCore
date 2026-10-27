---
name: novacore-phase
description: NovaCore yeniden yapılanma planının (docs/plan/PLAN.md) tek bir fazını veya alt fazını disiplinli şekilde yazar. Kapsamı teyit eder, plan modunda onay alır, adım adım uygular, build ve adımın testleriyle doğrular, commit atar, ilerlemeyi kaydeder, doğrulama bulgularını kapatır ve tam testi, incelemeyi ve PR'ı `/novacore-verify`'a bırakır. Yalnızca kullanıcı `/novacore-phase <faz-id>` ile çağırır.
disable-model-invocation: true
argument-hint: "<faz-id>  (örn. 0, 1, 2a, 3b)"
---

# NovaCore faz yürütücü (yazan oturum)

Hedef faz: **$ARGUMENTS**

## Bu skill neden var

NovaCore uzun, çok oturumlu bir yeniden yapılanmadan geçiyor. Önceki bir denemede model, kullanıcının "devam" demesiyle ilerledi. Oturum uzadıkça kapsamdan saptı, uydurma bilgiler üretti ve kullanıcının açıkça "yapılmasın" dediği şeyleri yapmaya başladı. Bunun üç nedeni vardı:
1. Plan yalnızca konuşmanın içindeydi ve bağlam özetlenince kısıtlar kayboldu.
2. "devam" komutu hiçbir bilgi taşımıyordu.
3. Kontrol noktası yoktu.

Bu prosedürün her adımı bu üç nedenden birine karşılık gelir. Kısaca: **gerçeği her seferinde dosyadan oku, kapsamı yazılı olarak onaylat, küçük ve doğrulanmış adımlarla ilerle, bilmediğin şeyi kaynağından araştır, teknik kararı kendin ver ve kaydet, faz bitince dur.**

Bu skill **yazan** oturumdur (D-088). Her adımda build'i ve adımın kendi testlerini çalıştırır. Tam test paketini, code-review'u, CodeRabbit CLI'ı, PR'ı ve bulguların sınıflandırılmasını ayrı bir oturumda `/novacore-verify` yapar: kodu yazılırken izlememiş bir oturum en kolay bağımsız incelemecidir (R-135). Bulguları düzeltmek yine bu skill'in işidir.

Kapsam disiplini sıkıdır, soru sayısı düşüktür (D-016). Kullanıcı bir mühendis gibi düşünmeni ister. Microsoft dokümanı bir şey söylüyorsa öyle yap. Söylemiyorsa maliyeti (uygulama + bakım + risk) hesapla, mantıklı olanı seç ve kaydet. Bariz şeyleri sorma, geçici kod bırakma (Y-45).

Kullanıcı Türkçe konuşur. Raporlar ve sorular Türkçe yazılır.

---

## 1. Gerçeği dosyalardan oku (hafızaya veya özete güvenme)

Sırayla ve eksiksiz oku:

1. `docs/plan/DECISIONS.md`: tamamı. Özellikle **Yapılmayacaklar** bölümü. Bu dosya diğer her şeyden önce gelir.
2. `docs/plan/PLAN.md`: "Genel kurallar" bölümü ve **yalnızca hedef fazın** bölümü.
3. `docs/plan/PROGRESS.md`: faz tablosu, hedef fazın adımları ve `#### Doğrulama` bölümü, açık sorular, son devir notu. `PROGRESS-ARSIV.md`'yi okuma; eski bir kaydın geçmişi gerekirse orada `grep` ile ara (D-082).
4. Git durumu: `git status`, `git branch --show-current`, `git log --oneline -15`.

Ardından şunları kontrol et. Biri başarısızsa **dur ve kullanıcıya bildir**, çözüm üretmeye başlama:

- **Argüman:** `$ARGUMENTS` boşsa PROGRESS'teki sıradaki fazı öner ve sor.
- **Bağımlılık:** Önceki fazların hepsi `✅ tamam` mı? Sıra PLAN.md'deki "Faz sırası ve bağımlılıklar" bölümündedir, buraya kopyalanmaz. Koşullu bir faz atlandıysa `✅ tamam (atlandı)` sayılır.
- **Açık sorular:** Bu fazın ihtiyaç duyduğu Q-kayıtları cevaplanmış mı? Cevaplanmamışsa `AskUserQuestion` ile şimdi sor.
- **Çalışma ağacı:** Temiz mi? Değilse neyin değiştiğini göster ve ne yapılacağını sor.
- **Doğrulama kapısı (D-088 (3)):** Faz başladıysa (branch'i varsa) önceki oturumların kodu doğrulanmış olmalı.
  - Taban, fazın `#### Doğrulama` bölümündeki "Son doğrulanan commit"tir. Kayıt yoksa taban `git merge-base main HEAD`'dir.
  - `git log --oneline <taban>..HEAD -- . ':(exclude)docs/plan/PROGRESS.md' ':(exclude)docs/plan/PROGRESS-ARSIV.md'` boş değilse **dur** ve tek satır yaz: "Önce `/clear`, ardından `/novacore-verify <id>`". Yalnız PROGRESS'e (ve faz sonundaki arşivine) dokunan commit'ler sayılmaz. Fazın henüz commit'lenmiş adımı yoksa liste boştur, kapı geçer.
- **Açık bulgular:** `#### Doğrulama` tablosunda `açık` durumlu satır varsa yeni adımdan önce onları kapat (aşağıda).
- **Devam eden faz:** Faz `🔄 devam ediyor` veya `⏸ duraklatıldı` durumundaysa bu bir devamdır. Son devir notunu esas al ve tamamlanmış adımları tekrar yapma. Plan fazın ilk oturumunda onaylandı: §2'yi atla. Açık bulguları kapattıktan sonra işaretlenmemiş adım varsa ilkinden §3'e geç; kalmadıysa §5'e (faz sonu) geç.
- **PR'ı açık faz:** Faz `✅ tamam` ve PR'ı açıksa (doğrulama oturumu açtı) bu bir PR düzeltme oturumudur: yalnız açık bulguları kapat, aynı branch'e push'la ve §5.4'e göre doğrulamaya yönlendir. Yeni adım yoktur.

**Açık bir bulguyu kapatmak** (her biri ayrı commit; oturum bütçesine bir adım gibi sayılır):
1. Hatayı gösteren testi koş ve kırmızı olduğunu gör. Kaynak `test` ise o test; `code-review`, `CLI` ya da `PR` ise hatayı gösteren mevcut bir test ya da önce yazılan yeni bir test (Genel kurallar: test önce gelir). Komut §3.2'deki gibidir. Bulgu bir testle gösterilemiyorsa (belge, yapılandırma, yorum) nedenini PROGRESS notuna yaz.
2. Düzelt. Kapsam dışına taşıyorsa §4.
3. `dotnet build NovaCore.slnx -v q`, testi yeniden koş ve yeşil gör.
4. Commit: `faz<id>(V<faz>-<n>): <özet>`, sonuna oturumun attribution satırını ekle; yalnız bulgunun dosyalarını stage'le.
5. PROGRESS'te bulgunun durumunu `kapatıldı (<kısa-hash>)` yap. `doğrulandı`'yı yalnız doğrulama oturumu yazar.

## 2. Kapsamı yazılı olarak onaylat (plan modu)

`EnterPlanMode` ile plan moduna geç. Kullanıcıya şunları sun:

- **Fazın amacı:** PLAN.md'den **alıntı** olarak. Kendi yorumunu ekleme.
- **Adımlar:** PLAN'daki ID'lerle (ör. F0.3). Her adım için:
  - dokunulacak dosyalar,
  - uygulama yaklaşımı (birkaç cümle),
  - yazılacak veya değişecek testler (test sınıflarının adlarıyla),
  - kabul kriteri.
- **Bu fazda yapılmayacaklar:** PLAN'daki faz notu ve DECISIONS'taki ilgili Y-maddeleri, açıkça listelenmiş olarak.
- **Kendi vereceğin kararlar:** PLAN'ın net söylemediği teknik noktalar. Her biri için seçtiğin çözümü, kaynağını ve kısa maliyet gerekçesini plana yaz; kullanıcı planı onaylarken bunları da görür. Bunları `AskUserQuestion` ile sorma.
- **Kullanıcının kararları:** Yalnızca §4'teki "sor" satırına uyan noktalar (D/Y çelişkisi, PLAN'ın kullanıcıya bıraktığı ürün kararı, Microsoft dışı paket, dışa dönük işlem). Bunları `AskUserQuestion` ile sor; her seçenekte önerini ve kaynağını belirt.

Kullanıcı onaylamadan hiçbir dosyayı değiştirme. Onaydan sonra:

- Git henüz yoksa F0.1 bunu kurar. Varsa `main`'den `faz/<id>-<kısa-ad>` branch'ini aç (ör. `faz/0-stabilizasyon`).
- PROGRESS'te faz durumunu `🔄 devam ediyor`, branch'i ve başlangıç tarihini işle.

## 3. Adım adım uygula

Her adım için aynı döngü:

1. **Başlat:** PROGRESS'te adıma "devam ediyor" notu düş.
2. **Test:** Adım bir hata düzeltmesi veya yeni davranış içeriyorsa önce testi yaz, adımın test sınıfını koş ve kırmızı olduğunu gör: `dotnet test --project tests/<Proje> -- --filter-class "*.<Sınıf>"` (birden çok sınıf için `--filter-class` tekrarlanır). Çözüm genelinde filtre kullanma: eşleşen testi olmayan proje exit 8 verir (D-063). Sınıf birden çok veritabanında varsa (ör. `TenancyScenarios`: `SqliteTenancyTests`, `SqlServerTenancyTests`, `PostgreSqlTenancyTests`) SQLite olanı koş; diğerleri doğrulamada çalışır.
3. **Uygula:** Yalnızca o adımın kapsamı. Aynı dosyada yan bir şey gözüne çarparsa dokunma, §4'ü uygula. Bir sembole dokunmadan önce çağıranlarını LSP'yle bul (`findReferences`, `incomingCalls`, `goToImplementation`); metin, yapılandırma ve şablon içeriği için grep (D-079). Adımın konusuna uyan .NET skill'ini (`dotnet-data`, `dotnet-test`, `dotnet-aspnetcore`, `dotnet`) kontrol listesi olarak aç; DECISIONS, PLAN ya da CLAUDE.md ile çelişirse onlar geçerlidir.
4. **Doğrula (yalnız bunlar, D-088 (1)):**
   - `dotnet build NovaCore.slnx -v q` (analizörler dahil, 0 uyarı).
   - Adımın yeni ya da değişen test sınıfları, 2'deki komutla: önce kırmızı görüldüyse şimdi yeşil.
   - Adım bir uç noktanın yolunu, isteğini, yanıtını ya da yetkisini değiştirdiyse `OpenApiContractTests`'i de koş. Yayımlanan belge eskiyse test `docs/openapi/v1.json`'u yeniden yazar ve kırmızı olur. Diff'ini incele, adımın commit'ine kat ve testi yeniden koş.
   - Paket değiştiyse `dotnet restore NovaCore.slnx --force`; sürüm değiştiyse `--force-evaluate` ile lock dosyalarını yeniden yaz ve onları da commit'le.
   - Adım kod ya da proje dosyasına dokunduysa commit'ten önce `dotnet format NovaCore.slnx` (CI `--verify-no-changes` koşar); adımın dosyalarında yaptığı değişikliği commit'e kat.

   Kırmızıysa düzelt; düzeltme kapsam dışına taşıyorsa §4. Çıktının tamamını bağlama alma: özeti ve hata satırlarını oku. Tam test paketi, coverage, smoke, `check-openapi`, code-review ve CodeRabbit CLI burada **çalışmaz**: `/novacore-verify`'ın işi.
5. **Commit:** `faz<id>(<adım-id>): <özet>`, sonuna oturumun attribution satırını ekle. Sadece o adımın dosyalarını stage'le (`git add <yol>`); `git add -A` kullanma.
6. **Kaydet:** PROGRESS'te `- [x] <adım-id> (<kısa-hash>): <kısa not>`. Notta koşulan test sınıflarını ve doğrulamaya kalanları (ör. SQL Server ve PostgreSQL katmanları) yaz.
7. **Bilgilendir:** Kullanıcıya tek satır yaz, ör. "F0.3 tamam: strict limiter IP bazlı, 2 yeni test." Adımda kendi aldığın bir karar varsa aynı satıra ekle. Plan onaylandığı için adımlar arasında onay bekleme. Yalnızca §4'teki "sor" durumunda dur.

Bağlam uzarsa ya da özetlenirse (compaction), devam etmeden önce PROGRESS.md'yi ve PLAN'daki faz bölümünü **yeniden oku**. Kapsam için konuşma özetine güvenme.

**Oturum bütçesi (D-082, D-088):** bir oturumda en fazla 3 adım (kapatılan bulgular dahil) commit'le; adımlar büyükse ya da uzun bir hata ayıklama olduysa daha önce dur. Model her araç çağrısında bütün bağlamı yeniden okur, bu yüzden uzayan oturumun her adımı bir öncekinden pahalıdır ve kullanıcının kotasını eritir. Durunca PROGRESS'e devir notu yaz (nerede kalındı, sıradaki adım, yarım kalan bir şey varsa ne, doğrulamaya kalan test katmanları), fazı `⏸ duraklatıldı` yap ve commit'le. Mesajının son satırı kopyalanacak komut olsun: "Doğrulama: `/clear`, ardından `/novacore-verify <id>`" (`/clear` bağlamı sıfırlar; maliyet açısından yeni oturumla aynıdır). Doğrulamadan sonra kullanıcı `/novacore-phase <id>` ile sürdürür; yeni oturum §1'deki "Devam eden faz" kuralıyla sürer, plan yeniden onaylatılmaz.

## 4. Sapma protokolü

**Sapma nedir:**
- PLAN'daki adımın kapsamında olmayan bir dosya, davranış, paket ya da public API değişikliği.
- PLAN'daki yaklaşımın çalışmaması.
- Bir D veya Y kaydıyla çelişki.
- "Bu arada şunu da düzelteyim" isteği.
- Bilmediğin veya emin olmadığın bir API ya da davranış.

**Ne yapılır:**
1. **Tahminle ilerleme.** Uydurma bir API, paket sürümü ya da davranış, sessiz bir hatadan daha pahalıdır.
2. **Araştır.** Birincil kaynağa git; kaynağı konu seçer (D-079):
   - Microsoft teknolojileri için yalnızca Microsoft Learn MCP: önce `microsoft_docs_search`, sayfa önemliyse `microsoft_docs_fetch`, kod örneği için `microsoft_code_sample_search`. Context7'ye gitme. Araçlar yüklü değilse ToolSearch ile yükle.
   - Üçüncü taraf paketler için konuyu Context7 ile bul, R-kaydına paketin resmî repo ya da doküman URL'sini yaz (Context7'yi değil). Context7 yoksa ya da oturumu açık değilse resmî repoyu WebFetch ile oku.
   - Paket sürümü, güvenlik açığı ve tedarik zinciri için NuGet MCP'nin okuyan araçları: `get_latest_package_version`, `get_package_context`, `review_supply_chain_security`. Paket değiştiren araçlarını kullanma; sürüm `Directory.Packages.props`'ta değişir.
   - SDK davranışı için SDK'nın kendi dosyalarına ve `dotnet msbuild -pp`/`-getProperty` çıktısına bak (R-040 böyle bulundu).
   - Kaynak bulamazsan bunu açıkça söyle.
   - Konu başka bir fazsa o fazın PLAN bölümünü ve PROGRESS'teki durumunu oku.
3. **Kaydet.** `docs/plan/RESEARCH.md`'ye R-kaydı ekle: soru, bulgu, URL, sonuç.
4. **Sınıflandır ve uygula (D-016):**

   | Durum | Ne yapılır |
   |---|---|
   | Adımı tamamlamak için gereken düzeltme; kaynağın net söylediği yol; planlanan yaklaşımın teknik olarak çalışmaması; analyzer veya MS kuralının gerektirdiği public API değişikliği (yayımlanmamış 0.x paket); adım için gereken yeni dosya | **Kendin karar ver ve uygula.** Kalıcıysa DECISIONS'a D-kaydı ekle (ekleme onay istemez), PROGRESS notuna ve adım satırına yaz, CHANGELOG'a işle. Faz raporunda "Kendi aldığım kararlar" altında listele. |
   | Başka bir fazın işi ve şimdi gerekli | O fazı oku. Toplamda şimdi yapmak daha ucuzsa **eksiksiz** yap (D-kaydı, adım ID'si korunur); değilse dokunma. Geçici çözüm yok (Y-45). |
   | Kapsam dışı ama değerli, şimdi gerekli değil | PROGRESS → "Sonraya bırakılanlar" listesine yaz, **dokunma** (Y-09). |
   | Uyarı çıktı | Önce düzelt. Düzeltme yerine bastırmak ancak MS dokümanının o kural için "When to suppress" bölümünün açıkça izin verdiği kalıcı durumda olur, gerekçe ve kaynakla; bunun dışındaki her bastırma kullanıcıya sorulur ve onayı bir D/Q kaydına yazılır (Y-46). Geçici bastırma yok. |
   | **Sor (`AskUserQuestion`):** bir D/Y kaydıyla çelişki ya da onu yeniden açma; PLAN'ın kullanıcıya bıraktığı ürün veya sahiplik kararı; Microsoft dışı yeni paket; mevcut bir D/Y satırını değiştirmek; merge, push, tag, yayın, silme | Seçenekler, önerin, kaynak (R-ID). |
   | Yapılmayacaklar listesindeki bir şey gerekli görünüyor | **Uygulama.** Gerekçeyi ve kaynağı göster, sor. |

   Soru sorarken ya da kullanıcıya öneri sunarken Y-40…Y-43 geçerlidir:
   - Her seçenek bu kod tabanındaki somut soruna (dosya/satır) bağlanır.
   - En az bir daha basit alternatif ve neden yetmediği yazılır.
   - Kaynağı (R-ID) belirtilir.
   - 2–3 gerçekten farklı seçenek sunulur.
   - Reddedilmiş bir yaklaşım başka bir isimle geri getirilmez.

   Modelin "en tipik" cevaba kayma eğilimi var (R-022). Kendi verdiğin kararda da aynı disiplin geçerli: kaydederken bu kod tabanındaki somut sorunu, daha basit alternatifi ve kaynağı yaz. "Best practice" gerekçe değildir.

**Hook'lar.** Allowlist dışındaki yeni NuGet paketleri ve PLAN, DECISIONS, `.claude/settings.json`, `.claude/hooks/` düzenlemeleri hook tarafından kullanıcı onayına düşer. DECISIONS'a yeni kayıt eklemek D-016'ya göre önceden onaylıdır, ama hook yine de sorar. Kullanıcıyı yormamak için bir adımdaki D-kayıtlarını tek bir düzenlemede topla. Stop hook ise build kırmızıyken durmanı engeller; testi çalıştırmaz (D-088 (6)), adımın testini sen koşarsın. Bunlar kasıtlı güvenlik ağlarıdır. **Atlatmaya çalışma.** Başka bir araç, dosya yolu ya da komut deneme. Hook bir işlemi durdurduysa nedenini kullanıcıya açıkla.

## 5. Faz sonu

İnceleme doğrulamada, düzeltme burada (D-088 (4)).

1. Son adım (ve varsa son açık bulgu) commit'lenince PLAN'ın "Faz doğrulaması" komutlarını, code-review'u, CodeRabbit CLI'ı ve PR'ı **yapma**: bunları son `/novacore-verify` yapar.
2. Faz bir konvansiyonu değiştirdiyse README, CLAUDE.md dosyaları ve memory'nin adımlarda güncellendiğini kontrol et (Genel kurallar); eksikse bir adım gibi tamamla ve commit'le.
3. PROGRESS'e devir notu yaz (yapılanlar, sürprizler, bekleyen sorular, doğrulamaya kalan test katmanları), fazı `⏸ duraklatıldı (faz sonu doğrulaması bekliyor)` yap ve commit'le; çalışma ağacında bir şey kalmasın. Mesajının son satırı: "Doğrulama: `/clear`, ardından `/novacore-verify <id>`".
4. **PR açıkken** (doğrulama oturumu PR'ı açtıktan sonra): kapsam içi PR bulguları `#### Doğrulama` tablosuna `açık` olarak gelir. Her birini §1'deki gibi kapat, commit'le ve aynı branch'e push'la (`git push`; D-067 akışının parçası, ayrıca sorulmaz). PROGRESS'i commit'le ve push'la; PR'da commit'lenmemiş bir şey kalmasın. Mesajının son satırı: "Doğrulama: `/clear`, ardından `/novacore-verify <id>`". PR yorumlarına GitHub'da cevap vermek, fazı `✅ tamam` yapmak ve merge'ü sormak doğrulama oturumunun işidir.
5. Aşağıdaki rapor şablonuyla raporla.
6. **Dur.** Sonraki faza başlama. Kullanıcı sonraki fazı yeni bir oturumda `/novacore-phase <id>` ile başlatacak. "devam" bir sonraki faz için onay **değildir**.

### Rapor şablonu

```
## Faz <id> raporu: <ad>

**Durum:** ⏸ duraklatıldı (neden) | ⏸ duraklatıldı (faz sonu doğrulaması bekliyor)
**Branch:** faz/...  ·  **Commit sayısı:** N  ·  **Doğrulanmamış aralık:** `<taban>..<hash>`

### Yapılan adımlar
| Adım | Commit | Sonuç |
|---|---|---|

### Kapatılan bulgular
| ID | Commit | Sonuç |
|---|---|---|

### Doğrulama (yazan oturum)
- `dotnet build NovaCore.slnx -v q` → geçti/kaldı (özet)
- `dotnet test --project ... -- --filter-class ...` → önce kırmızı, sonra yeşil (N test)
- Doğrulamaya kalanlar: tam paket, SQL Server/PostgreSQL katmanları, ...

### Kendi aldığım kararlar
- D-xxx / R-xxx: ne, neden (kaynak / maliyet). Yoksa "yok".

### Kullanıcıya sorulanlar
- Q-xxx: soru → cevap. Yoksa "yok".

### Sonraya bırakılanlar (bu oturumda eklenenler)
- ...

Doğrulama: `/clear`, ardından `/novacore-verify <id>`
```
