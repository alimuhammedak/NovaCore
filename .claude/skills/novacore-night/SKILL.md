---
name: novacore-night
description: NovaCore planında kalan işleri (Faz 8'in doğrulaması, Faz 9, Faz 6e) tek gözetimsiz koşuda bitirir (D-096). Orkestratördür; her adımı taze bağlamlı bir alt ajana verir, tam testi CI'a bırakır ve beklemez, faz sonunda PR açar, koşullar tutuyorsa merge eder, soru sormaz. Yalnızca kullanıcı `/novacore-night` ile çağırır.
disable-model-invocation: true
---

# NovaCore gece koşusu (orkestratör)

## Bu skill neden var

Kullanıcı kalan işlerin, başında kimse yokken, durmadan bitmesini istedi (D-096). Normal akış (`/novacore-phase`, `/novacore-verify`) dört yerde durur: üç adımlık oturum bütçesi, faz sonu, yerel tam test ve merge sorusu. Bu skill o dördünü kaldırır, kapsam disiplinini kaldırmaz:

- **Bağlam:** her adımı taze bağlamlı bir alt ajan yazar. Sen yalnız kuyruğu, commit'i, build'i ve PROGRESS'i tutarsın; bağlamın küçük kalır.
- **Test:** tam paketi CI koşar (~7 dk, her push'ta). Beklemezsin; sonucu bir sonraki adımın başında okursun.
- **Soru:** soru sormazsın. Kullanıcının kararı olan iş yapılmaz, "Kullanıcıya kalanlar"a yazılır ve sıradaki işe geçilir.

Öncelik sırası değişmez: DECISIONS > PLAN > PROGRESS > CLAUDE.md > konuşma. Raporlar Türkçe yazılır.

## 0. Yetki (D-096)

Kullanıcının bu skill'i çağırması şunlara izindir, ayrıca sorulmaz:
- faz branch'lerini push etmek, PR açmak, PR yorumlarını yanıtlamak,
- §4.5'teki koşullar tutunca fazın PR'ını ve CI'ı yeşil Dependabot PR'larını `gh pr merge --merge` ile merge etmek,
- PROGRESS kayıtlarını `main`'e push etmek (D-067 (1)).

**İzin olmayanlar (D-096 (5)):** tag, sürüm, yayın; force push; branch ya da veri silme; GitHub ayarları; Microsoft dışı yeni paket; bastırma (Y-46); `PLAN.md`, `DECISIONS.md`, `.claude/settings.json` ve `.claude/hooks/` düzenlemesi; Yapılmayacaklar listesindeki her şey. Bunlardan birini gerektiren iş **yapılmaz**: PROGRESS'in "Kullanıcıya kalanlar" tablosuna yazılır.

`AskUserQuestion` ve plan modu kullanılmaz. Bir hook ya da izin katmanı bir işlemi onaya düşürür ya da reddederse o iş yanlış yoldadır: işlemi bırak, başka bir yoldan deneme, "Kullanıcıya kalanlar"a yaz.

## 1. Her turun başında (bağlam özetlendiyse bu dosyayı da yeniden oku)

1. `wc -c docs/plan/PROGRESS.md`. 25 KB'ı aşıyorsa önce D-097 (2)'yi uygula (kapanan kayıtları arşive taşı).
2. Oku: `docs/plan/PROGRESS.md` (tamamı), `docs/plan/DECISIONS.md`'de "Yapılmayacaklar" bölümü ile D-096 ve D-097 satırları (`grep -n "^| D-09[67]"`), `docs/plan/PLAN.md`'de "Genel kurallar" ve sıradaki fazın bölümü.
3. `git status --short`, `git branch --show-current`, `git log --oneline -5`.
4. İzlenen bir dosyada commit'lenmemiş değişiklik varsa yarım kalmış bir adımdır: diff'in özetini oku ve adımı bir yazan alt ajana "yarım kalanı tamamla" diye ver. İzlenmeyen `.agents/`, `.codex/`, `AGENTS.md` dosyalarına dokunulmaz (Q-40).
5. PROGRESS'in "İş kuyruğu"ndan sıradaki işi seç, durumunu `🔄` yap.

## 2. Alt ajanlar

**Sen kod, test ya da belge yazmazsın** (kullanıcı, 2026-11-13: "Her şeyi sen yazma, opus agentlerine böl işleri"; ardından "sonnet de kullanabilirsin"). Senin işin kuyruk, brif, denetim (§3.2), PROGRESS ve git/gh komutlarıdır. Kendi yazdığın tek dosyalar `docs/plan/PROGRESS.md`, `PROGRESS-ARSIV.md` ve `SONRAYA-BIRAKILANLAR.md`'dir.

- **Model işe göre seçilir** (`Agent` çağrısında `model`, `subagent_type: "general-purpose"`; `effort` verme):
  - **Opus:** güvenlik, kimlik ve yetki kodu; hata düzeltmesi; yeni davranış; public API; migration; tasarım kararı isteyen adım; bütün incelemeler ve denetimler.
  - **Sonnet:** mekanik ve düşük riskli iş: belge, paket sürümü ve lock dosyaları, action pin'leri, yeniden adlandırma, tekrar eden test yardımcılarını birleştirmek, sınıflandırma ve arama gibi okuyan işler. Kuşkuluysa Opus.
- **Sonnet'in yazdığı her commit incelenir** (kullanıcı, 2026-11-13: "incelemesi yapılmalı, kötü yazmadığından emin olunmalı"). §3.2'deki denetimden sonra, PROGRESS'e `[x]` yazılmadan önce bir Opus okuyan ajanı commit'in diff'ini (`git show <hash>`) adımın PLAN satırına, kabul kriterine ve bu deponun standartlarına karşı okur: doğru mu, eksik mi, kapsam dışına taşmış mı, test gerçekten hatayı yakalıyor mu, belge kodla tutuyor mu. Bulgu varsa bir Opus yazan düzeltir (ayrı commit); bulgu yoksa adım kaydedilir. İnceleme faz sonundaki genel incelemenin (§4.2) yerine geçmez. Sonnet'in okuyan işinin sonucu (ör. sınıflandırma) bir karara dayanak olacaksa Opus örnekleyerek kontrol eder: en az beş maddede bir, `yapılacak` denenlerin hepsi.
- **Yazan:** çalışma ağacında aynı anda **bir tane**. Kod, test, belge yazar ve commit'ler.
- **Okuyan:** paralel, aynı mesajda birden çok `Agent` çağrısıyla. Sınıflandırma, inceleme, denetim, araştırma. Dosya değiştirmez, commit atmaz.
- Alt ajanın raporu kanıt değildir: commit'i, çalışma ağacını ve build'i kendin görürsün (§3.2).
- Tam test bir alt ajana da verilmez; CI koşar.

**Boş durma, boşa da harcama:**
- Yazan çalışırken okuyanları aynı anda çalıştır: sıradaki adımın araştırması, bitmiş commit'lerin incelemesi, Faz 9'un sınıflandırması, 6e'nin denetimleri. Alt ajanlar arka planda yürür; biri bitince haber gelir, beklerken başka iş başlat.
- Aynı alana dokunan en çok üç küçük alt adımı tek bir yazana ver (her biri yine ayrı commit): her alt ajan soğuk başlar ve plan dosyalarını yeniden okur, küçük işleri tek tek dağıtmak bu maliyeti katlar. Büyük ya da riskli adım tek başına gider.
- Brifi dar yaz: adımın PLAN satırı, dokunulacak dosyalar, kabul kriteri, bilinen tuzaklar. Alt ajana "depoyu tanı" dedirtme.
- Alt ajanın dökümünü okuma; yalnız raporunu oku. Eksik bir şey varsa `SendMessage` ile aynı ajana sor, yenisini başlatma.
- İnceleme `code-review` skill'inin `high` düzeyindedir; `max` ya da `ultra` kullanılmaz.
- Kaliteden kısılmaz: önce kırmızı test, §3.2'deki denetim ve §4.5'teki merge koşulları hiçbir optimizasyon için atlanmaz.

**Yazan alt ajanın brifi** (her seferinde eksiksiz; alt ajan bu konuşmayı görmez):

```
NovaCore deposunda Faz <id>'nin <adım-id> adımını yaz. Bu bir gece koşusudur (DECISIONS D-096): kullanıcı yok, soru sorma.

Önce oku: docs/plan/DECISIONS.md'nin "Yapılmayacaklar" bölümü ve D-096 satırı; docs/plan/PLAN.md'nin "Genel kurallar" ve "<faz başlığı>" bölümleri; docs/plan/PROGRESS.md; dokunacağın dizinin CLAUDE.md'si.

Adım: <PLAN'daki satırın alıntısı, kabul kriteriyle>.
<Bulgu kapatılıyorsa: bulgunun ID'si, yeri ve hata satırı. CI hatasıysa günlüğün ilgili satırları.>

`.claude/skills/novacore-phase/SKILL.md`'nin §3 (adım döngüsü) ve §4 (sapma protokolü) bölümlerini uygula, şu farklarla:
- Yalnız build, adımın test sınıfları (hata düzeltmesinde ve yeni davranışta önce kırmızı, sonra yeşil) ve `dotnet format NovaCore.slnx` çalışır. Tam test, coverage, smoke ve inceleme çalışmaz.
- PROGRESS.md, DECISIONS.md, PLAN.md ve `.claude/` altına yazma. Push etme.
- §4'ün "Sor" satırına düşen ya da tag, yayın, silme, yeni Microsoft dışı paket, bastırma gerektiren işi yapma; raporda "kullanıcıya kalan" olarak yaz ve adımın geri kalanını bitir.
- Kalıcı bir karar verdiysen D-kaydı biçiminde rapora yaz. Araştırmanı RESEARCH.md'ye R-kaydı olarak yaz.
- Tek commit: `faz<id>(<adım-id>): <özet>`, sonuna şu satır: <oturumun attribution satırı>. Yalnız adımın dosyalarını `git add <yol>` ile stage'le; `.agents/`, `.codex/`, `AGENTS.md`'ye dokunma.

Rapor, en çok 150 kelime: commit hash'i; koşulan test sınıfları ve sonuçları (kırmızı, sonra yeşil); karar adayları; kullanıcıya kalanlar; yapmadıkların ve kontrol etmediklerin.
```

## 3. Bir adımın döngüsü

1. **Önceki push'un CI'ı** (bir kez oku, bekleme):
   `gh run list --branch <branch> --workflow ci.yml --limit 1 --json databaseId,status,conclusion,headSha`
   - `failure`: `gh run view <id> --log-failed | tail -80`. Hata bir bulgudur: fazın `#### Doğrulama` tablosuna `V<faz>-<n>`, `açık` olarak yaz ve **yeni adımdan önce** bir yazan alt ajana kapattır.
   - `in_progress`: bekleme, adıma geç. Sonucu bir sonraki adımın başında okursun.
2. **Yazan alt ajanı gönder.** Raporu gelince kendin gör:
   - `git status --short`: izlenen dosyada değişiklik kalmamış olmalı,
   - `git log -1 --stat | tail -15`: commit yalnız adımın dosyalarına dokunmuş olmalı,
   - `dotnet build NovaCore.slnx -v q 2>&1 | tail -5`: 0 uyarı, 0 hata.

   Biri tutmuyorsa aynı alt ajana (`SendMessage`) düzelttir. İki denemeden sonra da tutmuyorsa adımı "Kullanıcıya kalanlar"a yaz; çalışma ağacı kirliyse `git stash push -m "night: <adım-id> yarım"` ile kenara al (silme) ve stash'i tabloda belirt.
3. **PROGRESS:** `- [x] <adım-id> (<kısa-hash>): <not>`. Not en çok iki satırdır (D-097 (4)). Karar adaylarını `DA-n` olarak, kullanıcıya kalanları kendi tablosuna yaz. Commit `faz<id>: record <adım-id>`, sonuna attribution satırı; ardından `git push` (ilk push'ta `-u origin <branch>`).
4. Sıradaki adım.

## 4. Faz sonu

1. **Son push'un CI'ı** yeşil olmalı. Koşarken boş durma: sıradaki fazın okuyan işlerini yürüt (Faz 9'un sınıflandırması, 6e'nin araştırması). Yapılacak başka iş yoksa `gh run watch <id> --exit-status` komutunu arka planda (`run_in_background`) başlat ve turu bitir; komut bitince yeniden çağrılırsın. Elle poll etme.
2. **İnceleme**, paralel okuyan alt ajanlarla (kodu yazmamış olmaları yeter):
   - `code-review` skill'i `main...HEAD` üzerinde,
   - diff'i PLAN'a ve DECISIONS'a karşı okuyan bir ajan (`.claude/skills/novacore-verify/SKILL.md` §2.2: kapsam dışına taşma, Yapılmayacaklar, fazın D-kayıtlarıyla çelişki, geçici kod ya da gerekçesiz bastırma, her adımın kabul kriterinin kanıtı),
   - CodeRabbit CLI arka planda: `mkdir -p artifacts/verify && coderabbit review --agent --base main > artifacts/verify/coderabbit.log 2>&1`. Kurulu ya da oturumu açık değilse atla (D-067 (3)).
3. **Bulguları sınıflandır** (`novacore-verify` §2.5):
   - kapsam içi gerçek hata → `#### Doğrulama` tablosu, `açık`; bir yazan alt ajan kapatır (§3), durum `kapatıldı (<hash>)`; sonraki CI yeşilse `doğrulandı`,
   - kapsam dışı → `docs/plan/SONRAYA-BIRAKILANLAR.md` (önce `grep` ile aynısı var mı bak), bir *Koşul:* ile,
   - yanlış pozitif → sabah raporunda tek cümle.
4. **PR:** `gh pr create --base main --head <branch>`, başlık `faz<id>: <faz adı>`, gövde fazın özeti ve PR attribution satırı. Açtıktan sonra uygulamanın PR araçları varsa durumu onlarla oku (`mcp__ccd_pr__get_status`); yoksa `gh pr checks <no>` bir kez.
5. **Merge koşulları (D-096 (4)), hepsi birden:**
   - PR'ın son commit'i yerel HEAD ve o commit'te bütün kontroller yeşil,
   - `#### Doğrulama` tablosunda `açık` bulgu yok,
   - o ana kadar gelmiş inceleme yorumları (CodeRabbit, Greptile) `novacore-verify` §4.2'ye göre sınıflandırılmış ve yanıtlanmış. İnceleme CI yeşile dönene kadar gelmediyse beklenmez (danışman, D-067 (2)); sonradan gelen yorumlar bir sonraki fazın başında okunur ve bulgu olarak işlenir.

   Tutuyorsa:
   - `gh pr merge <no> --merge`, `git switch main`, `git pull --ff-only`,
   - PROGRESS: faz `✅ tamam`, bitiş tarihi, Merge sütununa "D-096 ön onayı, PR #N, merge commit'i `<hash>`"; fazın adımlarını arşive taşı (D-097 (2)),
   - commit `faz<id>: record PR #N's merge in PROGRESS`, `git push`.

   Tutmuyorsa ve iki düzeltme turundan sonra da tutmuyorsa merge **yapılmaz**: PR açık kalır, "Kullanıcıya kalanlar"a yazılır.
6. **Sıradaki fazın branch'i:** `git switch -c faz/<id>-<kısa-ad>`, `main`'den; önceki faz merge edilemediyse onun branch'inden (PROGRESS'e not). Faz satırını `🔄 devam ediyor` yap.

## 5. Kuyruk

### Faz 8 (adımlar yazıldı)

§3.1 ile CI'ı oku, sonra §4. İnceleyen ajanlara söyle: F8.4–F8.8'in testleri önce kırmızı görülmedi (D-094 (3)); her kabul kriterinin testi hatayı gerçekten yakalıyor mu diye bakılmalı. F8.7'nin kabul kriteri CI'ın `template-smoke` job'udur.

### Faz 9 (PLAN "Faz 9: Bakım")

- **Önce, `main`'deyken:** `gh pr list --author "app/dependabot" --json number,title,headRefName,mergeable,statusCheckRollup`. Kontrolleri yeşil ve çakışmasız olanları sırayla merge et. Merge sonrası çakışan ya da kırmızı olanlar F9.3'te branch'te yapılır. `global.json`'ı değiştiren PR, o SDK `dotnet --list-sdks`'te yoksa kullanıcıya kalır.
- **F9.1:** okuyan alt ajanlara `SONRAYA-BIRAKILANLAR.md`'yi bölümlerine göre paylaştır (paralel). Her ajan maddenin bugünkü kodda hâlâ geçerli olup olmadığına bakar ve PLAN F9.1'deki kurala göre sınıflandırır. Kuşkulu madde `kalır`. Tabloyu PROGRESS'in `### Faz 9` bölümüne yaz: madde başına tek satır, en çok 150 karakter.
- **F9.2:** alt adımlar sırayla, her biri bir yazan alt ajan. Biten maddeyi listeden arşive taşı.
- **F9.3, F9.4:** PLAN'daki gibi.

### Faz 6e (PLAN "Faz 6e")

- **F6e.1, F6e.2:** workflow'da her `uses:` tam commit SHA'sına sabitlenir (`gh api repos/<owner>/<repo>/git/ref/tags/<tag>`), `permissions: {}`, `persist-credentials: false`. Test client'ının secret'ı bir dosyaya yazılmaz (Y-35): koşu anında üretilir. `workflow_dispatch` yalnız varsayılan branch'teki workflow'u tetikler; ilk koşu için faz iki PR'la yürüyebilir (F6e.1–F6e.2 merge edilir, `gh workflow run` ile ilk koşu alınır, sonra F6e.3–F6e.5). Bu bir teknik karardır: `DA` olarak kaydet.
- **F6e.3:** `docker info` çalışmıyorsa kullanıcıya kalır.
- **F6e.4:** okuyan alt ajanlar paralel: iki kapsam (auth çekirdeği, bütün repo), her biri en az iki koşu. Kurulu araçlarla koş (`security-review` skill'i). PLAN'ın adını verdiği Cloudflare `security-audit-skill` ve Claude Security plugin kurulu değilse o araçlarla koşu kullanıcıya kalır: indirme ve kurma. Girdi: Faz 6b'nin listesi (arşivde `grep`) ve `docs/plan/proposals/guvenlik-kalite-arastirmasi.md`'nin OIDC kontrol listesi.
- **F6e.5:** her bulgu için bir yazan alt ajan önce başarısız xUnit testini yazar. Yazılamıyorsa bulgu `needs_validation` olarak PROGRESS'e girer (tek satır).
- Faz bitince `SONRAYA-BIRAKILANLAR.md`'deki "Faz 6e sonu, unutulmamalı" (D-032) maddesini sabah raporuna koy.

## 6. Bitiş

Kuyruk bittiğinde ya da kalan her iş kullanıcıya kaldığında:

1. PROGRESS: iş kuyruğunun durumları, "Gece koşusu" durumu, yeni devir notu (bir öncekini arşive taşı, D-097 (2)). Commit ve push.
2. Sabah raporu:

```
## Gece koşusu raporu (<tarih>)

**Kuyruk:** Faz 8 <durum> · Faz 9 <durum> · Faz 6e <durum>
**Merge edilenler:** PR #N (<faz>, `<merge commit>`), ...
**Açık PR'lar:** PR #N: neden merge edilmedi

### DECISIONS'a işlenecek karar adayları
- DA-n: karar, gerekçe

### Kullanıcıya kalanlar
- iş: neden, ne gerekli

### Bulgular
- kapatılan / Sonraya bırakılan / yanlış pozitif (her biri tek cümle)

### Kontrol edilmeyenler
- ...
```

3. Dur. Başka iş başlatma.

## 7. Bağlam ve kota

- Komut çıktısının tamamını bağlama alma: `| tail`, uzun günlükler `artifacts/verify/` altına.
- Bağlam özetlenirse devam etmeden önce bu dosyayı ve PROGRESS'i yeniden oku. Kuyruk için konuşma özetine güvenme.
- Kota biterse koşu yarıda kalır. Her adım commit, push ve PROGRESS kaydıyla bittiği için `/novacore-night` yeniden çağrılınca §1'den, kaldığı yerden sürer.
- Stop hook build kırmızıyken durmayı engeller; testi çalıştırmaz.
