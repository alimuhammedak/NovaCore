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

Bu prosedürün her adımı bu üç nedenden birine karşılık gelir. Kısaca: **gerçeği her seferinde dosyadan oku, kapsamı yazılı olarak onaylat, küçük ve doğrulanmış adımlarla ilerle, bilmediğin şeyi kaynağından araştır, emin olmadığında sor, faz bitince dur.**

Kullanıcı Türkçe konuşur. Raporlar ve sorular Türkçe yazılır.

---

## 1. Gerçeği dosyalardan oku (hafızaya veya özete güvenme)

Sırayla ve eksiksiz oku:

1. `docs/plan/DECISIONS.md`: tamamı. Özellikle **Yapılmayacaklar** bölümü. Bu dosya diğer her şeyden önce gelir.
2. `docs/plan/PLAN.md`: "Genel kurallar" bölümü ve **yalnızca hedef fazın** bölümü.
3. `docs/plan/PROGRESS.md`: faz tablosu, hedef fazın adımları, açık sorular, son devir notu.
4. Git durumu: `git status`, `git branch --show-current`, `git log --oneline -15`.

Ardından şunları kontrol et. Biri başarısızsa **dur ve kullanıcıya bildir**, çözüm üretmeye başlama:

- **Argüman:** `$ARGUMENTS` boşsa PROGRESS'teki sıradaki fazı öner ve sor.
- **Bağımlılık:** Önceki fazların hepsi `✅ tamam` mı? (Sıra: 0 → 1 → 2a → 2b → 2c → 3a → 3b → 4a → 4b → 5)
- **Açık sorular:** Bu fazın ihtiyaç duyduğu Q-kayıtları cevaplanmış mı? Cevaplanmamışsa `AskUserQuestion` ile şimdi sor.
- **Çalışma ağacı:** Temiz mi? Değilse neyin değiştiğini göster ve ne yapılacağını sor.
- **Devam eden faz:** Faz `🔄 devam ediyor` veya `⏸ duraklatıldı` durumundaysa bu bir devamdır. Son devir notunu ve işaretlenmemiş ilk adımı esas al, tamamlanmış adımları tekrar yapma.

## 2. Kapsamı yazılı olarak onaylat (plan modu)

`EnterPlanMode` ile plan moduna geç. Kullanıcıya şunları sun:

- **Fazın amacı:** PLAN.md'den **alıntı** olarak. Kendi yorumunu ekleme.
- **Adımlar:** PLAN'daki ID'lerle (ör. F0.3). Her adım için:
  - dokunulacak dosyalar,
  - uygulama yaklaşımı (birkaç cümle),
  - yazılacak veya değişecek testler,
  - kabul kriteri.
- **Bu fazda yapılmayacaklar:** PLAN'daki faz notu ve DECISIONS'taki ilgili Y-maddeleri, açıkça listelenmiş olarak.
- **Belirsizlikler:** PLAN'ın net söylemediği her nokta. Bunları `AskUserQuestion` ile sor. Her seçenekte önerini ve kaynağını belirt.

Kullanıcı onaylamadan hiçbir dosyayı değiştirme. Onaydan sonra:

- Git henüz yoksa F0.1 bunu kurar. Varsa `main`'den `faz/<id>-<kısa-ad>` branch'ini aç (ör. `faz/0-stabilizasyon`).
- PROGRESS'te faz durumunu `🔄 devam ediyor`, branch'i ve başlangıç tarihini işle.

## 3. Adım adım uygula

Her adım için aynı döngü:

1. **Başlat:** PROGRESS'te adıma "devam ediyor" notu düş.
2. **Test:** Adım bir hata düzeltmesi veya yeni davranış içeriyorsa önce testi yaz ve kırmızı olduğunu gör.
3. **Uygula:** Yalnızca o adımın kapsamı. Aynı dosyada yan bir şey gözüne çarparsa dokunma, §4'ü uygula.
4. **Doğrula:** `dotnet build NovaCore.slnx` ve `dotnet test NovaCore.slnx`. Paket değiştiyse `dotnet restore NovaCore.slnx --force` da çalıştır. Kırmızıysa düzelt. Düzeltme kapsam dışına taşıyorsa §4.
5. **Commit:** `faz<id>(<adım-id>): <özet>`, sonuna oturumun attribution satırını ekle. Sadece o adımın dosyalarını stage'le (`git add <yol>`); `git add -A` kullanma.
6. **Kaydet:** PROGRESS'te `- [x] <adım-id> (<kısa-hash>): <kısa not>`.
7. **Bilgilendir:** Kullanıcıya tek satır yaz, ör. "F0.3 tamam: strict limiter IP bazlı, 2 yeni test." Plan onaylandığı için adımlar arasında onay beklemen gerekmez. Ama §4 devreye girerse dur.

Bağlam uzarsa ya da özetlenirse (compaction), devam etmeden önce PROGRESS.md'yi ve PLAN'daki faz bölümünü **yeniden oku**. Kapsam için konuşma özetine güvenme.

Faz tek oturuma sığmayacak kadar büyükse bir adım sınırında dur. PROGRESS'e devir notu yaz, fazı `⏸ duraklatıldı` yap ve kullanıcıya yeni bir oturumda aynı komutla devam etmesini söyle.

## 4. Sapma protokolü

**Sapma nedir:**
- PLAN'daki adımın kapsamında olmayan bir dosya, davranış, paket ya da public API değişikliği.
- PLAN'daki yaklaşımın çalışmaması.
- Bir D veya Y kaydıyla çelişki.
- "Bu arada şunu da düzelteyim" isteği.
- Bilmediğin veya emin olmadığın bir API ya da davranış.

**Ne yapılır:**
1. **Dur.** Tahminle ilerleme. Uydurma bir API, paket sürümü ya da davranış, sessiz bir hatadan daha pahalıdır.
2. **Araştır.** Birincil kaynağa git:
   - Microsoft teknolojileri için Microsoft Learn MCP: önce `microsoft_docs_search`, sayfa önemliyse `microsoft_docs_fetch`, kod örneği için `microsoft_code_sample_search`. Araçlar yüklü değilse ToolSearch ile yükle.
   - Diğer paketler için resmi repo veya doküman (WebFetch).
   - Kaynak bulamazsan bunu açıkça söyle.
3. **Kaydet.** `docs/plan/RESEARCH.md`'ye R-kaydı ekle: soru, bulgu, URL, sonuç.
4. **Sınıflandır:**

   | Durum | Ne yapılır |
   |---|---|
   | Adımın kendi dosyasında, adımı tamamlamak için gereken küçük bir düzeltme (ör. derleme hatası) | Uygula, PROGRESS notuna yaz |
   | Yeni paket, public API değişikliği, yeni proje veya dosya, silme, başka bir fazın işi, bir D/Y kaydıyla çelişki, PLAN'dan farklı yaklaşım | **`AskUserQuestion`**: seçenekler, önerin ve kaynak (R-ID). Onaylanırsa DECISIONS'a D-kaydı ekle (hook onay isteyecek) |
   | Kapsam dışı ama değerli | PROGRESS → "Sonraya bırakılanlar" listesine yaz, **dokunma** |
   | Yapılmayacaklar listesindeki bir şey gerekli görünüyor | **Uygulama.** Gerekçeyi ve kaynağı göster, sor |

   Bir soru veya öneri sunarken Y-40…Y-43 geçerlidir:
   - Her seçenek bu kod tabanındaki somut soruna (dosya/satır) bağlanır.
   - En az bir daha basit alternatif ve neden yetmediği yazılır.
   - Kaynağı (R-ID) belirtilir.
   - 2–3 gerçekten farklı seçenek sunulur.
   - Reddedilmiş bir yaklaşım başka bir isimle geri getirilmez.

   Modelin "en tipik" cevaba kayma eğilimi var (R-022). Tek seçenekli bir öneri ya da "best practice" gerekçesi bu eğilimin işaretidir.

**Hook'lar.** Yeni NuGet paketi veya korunan bir dosyanın (PLAN, DECISIONS, `.claude/settings.json`, `.claude/hooks/`) düzenlenmesi hook tarafından kullanıcı onayına düşer. Stop hook ise build veya test kırmızıyken durmanı engeller. Bunlar kasıtlı güvenlik ağlarıdır. **Atlatmaya çalışma.** Başka bir araç, dosya yolu ya da komut deneme. Hook bir işlemi durdurduysa nedenini kullanıcıya açıkla.

## 5. Faz sonu

1. PLAN'daki "Faz doğrulaması" komutlarını çalıştır. Her komutun sonucunu rapora koy (geçti/kaldı ve özet).
2. `code-review` skill'ini (Skill aracıyla) bu branch'in değişiklikleri üzerinde çalıştır. Bulguları rapora ekle. Kapsam dışı bulguları **düzeltme**, "Sonraya bırakılanlar" listesine yaz. Kapsam içi gerçek hataları düzelt ve yeniden doğrula.
3. PROGRESS'i güncelle: faz `✅ tamam`, bitiş tarihi, **son devir notu** (yapılanlar, sürprizler, bekleyen sorular, sonraki fazın bilmesi gerekenler).
4. Faz bir konvansiyonu değiştirdiyse README, CLAUDE.md dosyaları ve memory'nin güncellendiğini doğrula (Genel kurallar).
5. Aşağıdaki rapor şablonuyla raporla.
6. **Dur.** Sonraki faza başlama. Merge için kullanıcıya sor (`faz/...` → `main`). Kullanıcı sonraki fazı yeni bir oturumda `/novacore-phase <id>` ile başlatacak. "devam" bir sonraki faz için onay **değildir**.

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

### Sapmalar ve kararlar
- R-xxx / D-xxx / Q-xxx: kısa açıklama (yoksa "yok")

### Sonraya bırakılanlar (bu fazda eklenenler)
- ...

### Code review özeti
- ...

### Sonraki adım
Merge onayı bekleniyor. Ardından yeni oturumda: `/novacore-phase <sonraki-id>`
```
