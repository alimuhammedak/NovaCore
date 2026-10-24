---
name: novacore-phase
description: NovaCore yeniden yapılanma planının (docs/plan/PLAN.md) tek bir fazını veya alt fazını disiplinli şekilde yürütür. Kapsamı teyit eder, plan modunda onay alır, adım adım uygular, doğrular, commit atar, ilerlemeyi kaydeder ve faz sonunda durur. Yalnızca kullanıcı `/novacore-phase <faz-id>` ile çağırır.
disable-model-invocation: true
argument-hint: "<faz-id>  (örn. 0, 1, 2a, 3b)"
---

# NovaCore faz yürütücü

Hedef faz: **$ARGUMENTS**

## Bu skill neden var

NovaCore uzun, çok oturumlu bir yeniden yapılanmadan geçiyor. Önceki bir denemede model, kullanıcının "devam" demesiyle ilerledi. Oturum uzadıkça kapsamdan saptı, uydurma bilgiler üretti ve kullanıcının açıkça "yapılmasın" dediği şeyleri yapmaya başladı. Bunun üç nedeni vardı:
1. Plan yalnızca konuşmanın içindeydi ve bağlam özetlenince kısıtlar kayboldu.
2. "devam" komutu hiçbir bilgi taşımıyordu.
3. Kontrol noktası yoktu.

Bu prosedürün her adımı bu üç nedenden birine karşılık gelir. Kısaca: **gerçeği her seferinde dosyadan oku, kapsamı yazılı olarak onaylat, küçük ve doğrulanmış adımlarla ilerle, bilmediğin şeyi kaynağından araştır, teknik kararı kendin ver ve kaydet, faz bitince dur.**

Kapsam disiplini sıkıdır, soru sayısı düşüktür (D-016). Kullanıcı bir mühendis gibi düşünmeni ister. Microsoft dokümanı bir şey söylüyorsa öyle yap. Söylemiyorsa maliyeti (uygulama + bakım + risk) hesapla, mantıklı olanı seç ve kaydet. Bariz şeyleri sorma, geçici kod bırakma (Y-45).

Kullanıcı Türkçe konuşur. Raporlar ve sorular Türkçe yazılır.

---

## 1. Gerçeği dosyalardan oku (hafızaya veya özete güvenme)

Sırayla ve eksiksiz oku:

1. `docs/plan/DECISIONS.md`: tamamı. Özellikle **Yapılmayacaklar** bölümü. Bu dosya diğer her şeyden önce gelir.
2. `docs/plan/PLAN.md`: "Genel kurallar" bölümü ve **yalnızca hedef fazın** bölümü.
3. `docs/plan/PROGRESS.md`: faz tablosu, hedef fazın adımları, açık sorular, son devir notu. `PROGRESS-ARSIV.md`'yi okuma; eski bir kaydın geçmişi gerekirse orada `grep` ile ara (D-076).
4. Git durumu: `git status`, `git branch --show-current`, `git log --oneline -15`.

Ardından şunları kontrol et. Biri başarısızsa **dur ve kullanıcıya bildir**, çözüm üretmeye başlama:

- **Argüman:** `$ARGUMENTS` boşsa PROGRESS'teki sıradaki fazı öner ve sor.
- **Bağımlılık:** Önceki fazların hepsi `✅ tamam` mı? Sıra PLAN.md'deki "Faz sırası ve bağımlılıklar" bölümündedir, buraya kopyalanmaz. Koşullu bir faz atlandıysa `✅ tamam (atlandı)` sayılır.
- **Açık sorular:** Bu fazın ihtiyaç duyduğu Q-kayıtları cevaplanmış mı? Cevaplanmamışsa `AskUserQuestion` ile şimdi sor.
- **Çalışma ağacı:** Temiz mi? Değilse neyin değiştiğini göster ve ne yapılacağını sor.
- **Devam eden faz:** Faz `🔄 devam ediyor` veya `⏸ duraklatıldı` durumundaysa bu bir devamdır. Son devir notunu esas al ve tamamlanmış adımları tekrar yapma. Plan fazın ilk oturumunda onaylandı: §2'yi atla. İşaretlenmemiş adım varsa ilkinden §3'e geç; kalmadıysa doğrudan §5'e (faz sonu) geç.

## 2. Kapsamı yazılı olarak onaylat (plan modu)

`EnterPlanMode` ile plan moduna geç. Kullanıcıya şunları sun:

- **Fazın amacı:** PLAN.md'den **alıntı** olarak. Kendi yorumunu ekleme.
- **Adımlar:** PLAN'daki ID'lerle (ör. F0.3). Her adım için:
  - dokunulacak dosyalar,
  - uygulama yaklaşımı (birkaç cümle),
  - yazılacak veya değişecek testler,
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
2. **Test:** Adım bir hata düzeltmesi veya yeni davranış içeriyorsa önce testi yaz ve kırmızı olduğunu gör.
3. **Uygula:** Yalnızca o adımın kapsamı. Aynı dosyada yan bir şey gözüne çarparsa dokunma, §4'ü uygula.
4. **Doğrula:** `dotnet build NovaCore.slnx` ve `dotnet test NovaCore.slnx`. Paket değiştiyse `dotnet restore NovaCore.slnx --force` da çalıştır; sürüm değiştiyse `--force-evaluate` ile lock dosyalarını yeniden yaz ve onları da commit'le. Kırmızıysa düzelt. Düzeltme kapsam dışına taşıyorsa §4. Çıktının tamamını bağlama alma: özeti ve hata satırlarını oku (ör. `dotnet build -v q`, testlerde yalnız başarısızlar ve toplam). CodeRabbit CLI adımda çalışmaz, faz sonunda bir kez çalışır (§5.4, D-076).
5. **Commit:** `faz<id>(<adım-id>): <özet>`, sonuna oturumun attribution satırını ekle. Sadece o adımın dosyalarını stage'le (`git add <yol>`); `git add -A` kullanma.
6. **Kaydet:** PROGRESS'te `- [x] <adım-id> (<kısa-hash>): <kısa not>`.
7. **Bilgilendir:** Kullanıcıya tek satır yaz, ör. "F0.3 tamam: strict limiter IP bazlı, 2 yeni test." Adımda kendi aldığın bir karar varsa aynı satıra ekle. Plan onaylandığı için adımlar arasında onay bekleme. Yalnızca §4'teki "sor" durumunda dur.

Bağlam uzarsa ya da özetlenirse (compaction), devam etmeden önce PROGRESS.md'yi ve PLAN'daki faz bölümünü **yeniden oku**. Kapsam için konuşma özetine güvenme.

**Oturum bütçesi (D-076):** bir oturumda en fazla 3 adım commit'le; adımlar büyükse ya da uzun bir hata ayıklama olduysa daha önce dur. Model her araç çağrısında bütün bağlamı yeniden okur, bu yüzden uzayan oturumun her adımı bir öncekinden pahalıdır ve kullanıcının kotasını eritir. Durunca PROGRESS'e devir notu yaz (nerede kalındı, sıradaki adım, yarım kalan bir şey varsa ne), fazı `⏸ duraklatıldı` yap ve commit'le. Mesajının son satırı kopyalanacak komut olsun: "Devam: `/clear`, ardından `/novacore-phase <id>`" (`/clear` bağlamı sıfırlar; maliyet açısından yeni oturumla aynıdır). Yeni oturum §1'deki "Devam eden faz" kuralıyla sürer, plan yeniden onaylatılmaz.

## 4. Sapma protokolü

**Sapma nedir:**
- PLAN'daki adımın kapsamında olmayan bir dosya, davranış, paket ya da public API değişikliği.
- PLAN'daki yaklaşımın çalışmaması.
- Bir D veya Y kaydıyla çelişki.
- "Bu arada şunu da düzelteyim" isteği.
- Bilmediğin veya emin olmadığın bir API ya da davranış.

**Ne yapılır:**
1. **Tahminle ilerleme.** Uydurma bir API, paket sürümü ya da davranış, sessiz bir hatadan daha pahalıdır.
2. **Araştır.** Birincil kaynağa git:
   - Microsoft teknolojileri için Microsoft Learn MCP: önce `microsoft_docs_search`, sayfa önemliyse `microsoft_docs_fetch`, kod örneği için `microsoft_code_sample_search`. Araçlar yüklü değilse ToolSearch ile yükle.
   - Diğer paketler için resmi repo veya doküman (WebFetch).
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

**Hook'lar.** Allowlist dışındaki yeni NuGet paketleri ve PLAN, DECISIONS, `.claude/settings.json`, `.claude/hooks/` düzenlemeleri hook tarafından kullanıcı onayına düşer. DECISIONS'a yeni kayıt eklemek D-016'ya göre önceden onaylıdır, ama hook yine de sorar. Kullanıcıyı yormamak için bir adımdaki D-kayıtlarını tek bir düzenlemede topla. Stop hook ise build veya test kırmızıyken durmanı engeller. Bunlar kasıtlı güvenlik ağlarıdır. **Atlatmaya çalışma.** Başka bir araç, dosya yolu ya da komut deneme. Hook bir işlemi durdurduysa nedenini kullanıcıya açıkla.

## 5. Faz sonu

1. PLAN'daki "Faz doğrulaması" komutlarını çalıştır. Her komutun sonucunu rapora koy (geçti/kaldı ve özet).
2. `code-review` skill'ini (Skill aracıyla) bu branch'in değişiklikleri üzerinde çalıştır. Bulguları rapora ekle. Kapsam dışı bulguları **düzeltme**, "Sonraya bırakılanlar" listesine yaz. Kapsam içi gerçek hataları düzelt ve yeniden doğrula.
3. PROGRESS'i güncelle: faz `✅ tamam`, bitiş tarihi, **son devir notu** (yapılanlar, sürprizler, bekleyen sorular, sonraki fazın bilmesi gerekenler). Ardından arşivle (D-076): bir önceki fazın "Adımlar" bölümünü, son üç devir notundan eskilerini ve üstü çizilmiş (kapanmış) "Sonraya bırakılanlar" maddelerini değiştirmeden `PROGRESS-ARSIV.md`'nin ilgili bölümlerine taşı. PROGRESS'te biten fazın adımları kalır. Bu güncellemeleri commit'le.
4. **CodeRabbit CLI, fazda bir kez (D-076):** 3'teki commit'ten sonra, çalışma ağacı temizken faz branch'inin tamamını incelet: `coderabbit review --agent --base main`. İnceleme uzun sürebilir; arka planda çalıştır ve bitmesini bekle, poll etme. Kapsam içi gerçek bulguyu düzelt, build ile testleri (paket değiştiyse restore'u da) yeniden çalıştır ve commit'le (`faz<id>: address the CodeRabbit CLI review`); kapsam dışını §4'e göre "Sonraya bırakılanlar"a yaz; yanlış pozitifi raporda tek cümleyle belirt. CLI kurulu ya da oturum açık değilse (`coderabbit auth status`) ya da rate limit'e takılırsa fazı bekletme: raporda "CLI incelemesi yok: <neden>" de, PR incelemesi yine yakalar.
5. Faz bir konvansiyonu değiştirdiyse README, CLAUDE.md dosyaları ve memory'nin güncellendiğini doğrula (Genel kurallar).
6. **PR ve CodeRabbit (D-067):** 2–5'teki düzeltmeleri ve PROGRESS güncellemesini (son adımın `[x]` kaydı dahil) doğrulayıp commit'le; çalışma ağacı temizken branch'i push et ve `gh pr create --base main --head faz/<id>-<kısa-ad>` ile PR aç (başlık `faz<id>: <faz adı>`, gövde: son devir notunun özeti). PR'ı açmak bu sürecin parçasıdır, ayrıca sorulmaz; merge sorulur. CodeRabbit kendiliğinden inceler. İncelemeyi bekle (poll etme; PR araçları CI'ı ve yorumları bildirir), sonra her yorumu sınıflandır: kapsam içi gerçek hata → düzelt, doğrula, aynı branch'e commit ve push; kapsam dışı → "Sonraya bırakılanlar"; yanlış pozitif ya da bir D/Y kaydıyla çelişen öneri → yorumu gerekçesi ve kaydın ID'siyle yanıtla. Pre-merge check'lerin uyarılarını da aynı şekilde ele al. Sonucu PROGRESS'e yaz, commit'le ve aynı branch'e push et; merge'ten önce PR'da commit'lenmemiş bir şey kalmasın. CodeRabbit inceleme yapmadıysa (rate limit, kesinti) bunu raporda söyle; merge'ü engellemez (danışman, D-067 (2)).
7. Aşağıdaki rapor şablonuyla raporla.
8. **Dur.** Sonraki faza başlama. Merge için kullanıcıya sor. Onay gelince `gh pr merge <no> --merge` (merge commit; squash ya da rebase değil, adım commit'leri korunur), ardından yerel `main`'i `git pull --ff-only` ile güncelle. Kullanıcı sonraki fazı yeni bir oturumda `/novacore-phase <id>` ile başlatacak. "devam" bir sonraki faz için onay **değildir**.

### Rapor şablonu

```
## Faz <id> raporu: <ad>

**Durum:** ✅ tamam | ⏸ duraklatıldı (neden)
**Branch:** faz/...  ·  **Commit sayısı:** N

### Yapılan adımlar
| Adım | Commit | Sonuç |
|---|---|---|

### Doğrulama
- `komut` → geçti/kaldı (özet)

### Kendi aldığım kararlar
- D-xxx / R-xxx: ne, neden (kaynak / maliyet). Yoksa "yok".

### Kullanıcıya sorulanlar
- Q-xxx: soru → cevap. Yoksa "yok".

### Sonraya bırakılanlar (bu fazda eklenenler)
- ...

### Code review özeti
- ...

### CodeRabbit
- CLI (faz sonu, §5.4): bulgu sayısı; düzeltilen / Sonraya bırakılan / yanlış pozitif. Çalışmadıysa nedeni.
- PR #N: düzeltilen / Sonraya bırakılan / yanıtlanan yorumlar; pre-merge check sonuçları.

### Sonraki adım
PR #N için merge onayı bekleniyor. Ardından yeni oturumda: `/novacore-phase <sonraki-id>`
```
