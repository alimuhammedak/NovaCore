---
name: novacore-verify
description: NovaCore yeniden yapılanma planındaki bir fazı, yazan oturumdan (`/novacore-phase`) sonra ayrı bir oturumda doğrular. Tam testi ve code-review'u çalıştırır, adımların kabul kriterlerini arar, bulguları PROGRESS'e yazar ve kod düzeltmez. Faz sonunda faz doğrulamasını ve CodeRabbit CLI'ı çalıştırır, PR'ı açar, PR yorumlarını sınıflandırıp yanıtlar ve merge'ü sorar. Yalnızca kullanıcı `/novacore-verify <faz-id>` ile çağırır.
disable-model-invocation: true
argument-hint: "<faz-id>  (örn. 6e, 7b)"
---

# NovaCore faz doğrulayıcı (doğrulayan oturum)

Hedef faz: **$ARGUMENTS**

## Bu skill neden var

Yazan oturum (`/novacore-phase`) her adımda yalnız build'i ve adımın kendi test sınıflarını çalıştırır (D-088 (1)). Tam test paketi, inceleme ve bulguların sınıflandırılması burada, kodu yazılırken izlememiş ayrı bir oturumda yapılır. Claude Code'un belgelerine göre ikinci bir oturum en kolay bağımsız incelemecidir (R-135). Bu ayrım yazan oturumun bağlamını da kısa tutar.

**Bu oturum yalnız raporlar** (D-088 (2)):
- `src/`, `tests/`, şablon içeriği (`src/Templates/.../content`), `eng/`, workflow'lar, `.claude/`, PLAN, DECISIONS, RESEARCH, README ve CLAUDE.md dosyalarında hiçbir şeyi değiştirmez; bir düzeltme ne kadar küçük olursa olsun bulgu olarak yazılır.
- Yalnız `docs/plan/PROGRESS.md`'yi değiştirir; faz sonundaki arşiv için `docs/plan/PROGRESS-ARSIV.md`'yi de (D-082 (3)). Commit mesajı `faz<id>: verify <kısa-hash>`'tir, `<kısa-hash>` doğrulanan HEAD'dir; sonuna oturumun attribution satırı eklenir.
- Faz sonunda branch'i push'lar ve PR'ı açar, PR yorumlarına GitHub'da cevap verir (D-067 akışının parçası, ayrıca sorulmaz). Merge'ü sorar.

Kapsam disiplini `/novacore-phase` ile aynıdır: DECISIONS > PLAN > PROGRESS > CLAUDE.md > konuşma. Kullanıcı Türkçe konuşur; raporlar ve sorular Türkçe yazılır.

---

## 1. Gerçeği dosyalardan oku

Sırayla oku:

1. `docs/plan/DECISIONS.md`:
   - **Yapılmayacaklar** bölümünün tamamı,
   - fazın PLAN bölümünün atıf yaptığı D-kayıtları,
   - bu fazda eklenen ya da değişen kayıtlar: `git diff <faz-tabanı>..HEAD -- docs/plan/DECISIONS.md`. Faz tabanı `git merge-base main HEAD`'dir.
2. `docs/plan/PLAN.md`: "Genel kurallar" ve **yalnızca hedef fazın** bölümü: adımlar, kabul kriterleri, "Faz doğrulaması".
3. `docs/plan/PROGRESS.md`: faz tablosundaki satır, fazın `#### Doğrulama` bölümü (son doğrulanan commit, bulgu tablosu), "Sonraya bırakılanlar" (aynı bulguyu iki kez yazmamak için). Fazın adım notlarını ve devir notlarını **şimdi okuma** (§2.4).
4. Git: `git status`, `git branch --show-current`, `git log --oneline -15`.

Ardından kontrol et. Biri başarısızsa **dur ve kullanıcıya bildir**:

- **Argüman:** `$ARGUMENTS` boşsa PROGRESS'te `🔄`, `⏸` ya da PR'ı açık olan fazı öner ve sor.
- **Branch:** fazın `faz/<id>-...` branch'inde misin? Değilse dur.
- **Çalışma ağacı:** izlenen dosyalarda değişiklik varsa dur: yazan oturum yarım kalmış olabilir.
- **Aralık:** taban fazın "Son doğrulanan commit"idir; kayıt yoksa faz tabanıdır. Aralık `<taban>..HEAD`'dir. Doğrulanacak kod commit'leri: `git log --oneline <taban>..HEAD -- . ':(exclude)docs/plan/PROGRESS.md' ':(exclude)docs/plan/PROGRESS-ARSIV.md'`.
- **Mod:**
  - **PR modu:** fazın PR'ı açık (`gh pr list --head <branch> --state open`). §2, sonra §4.
  - **Faz sonu modu:** bütün adımlar `[x]` ve PR yok. §2, sonra koşulları tutarsa §3.
  - **Ara mod:** işaretlenmemiş adım var. Yalnız §2.
  - Ara modda doğrulanacak kod commit'i ve `kapatıldı` durumlu bulgu yoksa yapılacak bir şey yok: bunu söyle, son satır "Devam: `/clear`, ardından `/novacore-phase <id>`".

## 2. Her doğrulamada

### 2.1 Komutlar

Sırayla çalıştır. Çıktının tamamını bağlama alma: özeti ve hata satırlarını oku.

1. `dotnet restore NovaCore.slnx --locked-mode`
2. `dotnet build NovaCore.slnx -v q`
3. `dotnet format NovaCore.slnx --verify-no-changes`
4. **Docker:** `docker info --format '{{.ServerVersion}}'`. Docker yoksa SQL Server ve PostgreSQL katmanları atlanır. Testi yine çalıştır (SQLite katmanı hata bulabilir), ama aralığı doğrulanmış sayma (§2.5).
5. **Tam test** (D-088 (2), R-135: subagent'a verilmez, çıktı git'e girmeyen bir dosyaya yazılır):

   ```bash
   mkdir -p artifacts/verify
   dotnet test --solution NovaCore.slnx > artifacts/verify/test.log 2>&1; echo "exit=$?"
   sed -n '/^Test run summary/,$p' artifacts/verify/test.log | head -25
   grep -n -i -A15 "^\s*failed " artifacts/verify/test.log | head -200
   ```

   Özet günlüğün sonundadır: proje başına bir satır, ardından `total`, `failed`, `succeeded`, `skipped`. Özet başarısız test sayıyor ama ikinci grep bir şey bulmuyorsa günlükte `failed` geçen satırların çevresini oku. Başarısız bir testi bir kez tek başına yeniden koş (`dotnet test --project tests/<Proje> -- --filter-method "*.<Sınıf>.<Metot>"`). Yeniden koşuda geçse de bulgudur: aralıklı kırmızı bir test.

   Koşudan sonra `git status --short`'a bak. Testin değiştirdiği izlenen bir dosya bulgudur (kaynak `test`, yer dosya ve test adı). Bugün tek örnek `OpenApiContractTests`: yayımlanan belge eskiyse `docs/openapi/v1.json`'u yeniden yazar ve kırmızı olur. Dosyayı commit'leme; `git restore -- <yol>` ile geri al ki yazan oturum temiz bir ağaç devralsın. Yazan oturum o testi koşunca dosya yeniden üretilir.

### 2.2 İnceleme

1. `code-review` skill'ini (Skill aracıyla) aralık üzerinde çalıştır (`<taban>..HEAD`). Skill bir aralığı hedef olarak almıyorsa branch'i hedef ver ve yalnız aralıktaki commit'lerin değiştirdiği satırların bulgularını say.
2. Diff'i (`git diff <taban>..HEAD`) kendin de PLAN'a ve DECISIONS'a karşı oku:
   - Değişiklik adımın "Dosyalar / Kapsam" satırının dışına taşıyor mu?
   - Bir Yapılmayacaklar maddesini uyguluyor mu?
   - Bu fazın D-kayıtlarıyla çelişiyor mu?
   - Geçici kod ya da gerekçesiz bastırma var mı (Y-45, Y-46)?
3. **Kabul kriterleri:** aralıkta commit'i olan her adım için PLAN'daki kabul kriterinin istediği kanıtı ara.
   - Kriter bir test istiyorsa ("Test: ..."), testi adıyla bul ve §2.1'deki koşuda yeşil olduğunu gör.
   - Rapor, bulgu listesi, R/D kaydı ya da belge istiyorsa (ör. F6e.3 "Rapor", F7b.1 "R ve D kaydı"), o çıktının PROGRESS'te ya da repoda olduğunu gör; test arama.
   - Kanıt yoksa bulgudur (kaynak `code-review`).
4. Bir bulguyu doğrulamak için kaynak gerekiyorsa D-079'a göre araştır. R-kaydı yazma; URL'yi bulgu satırına ya da rapora koy (kaydı düzelten yazan oturum yazar).

### 2.3 Kapatılan bulgular

`kapatıldı (<hash>)` durumundaki her bulguyu yeniden kontrol et: kaynağı `test` ise test yeşil mi, diğerlerinde aynı sorun aralıkta hâlâ duruyor mu? Temizse durumu `doğrulandı` yap. Değilse `açık`'a döndür ve satıra nedenini ekle. Kaynağı `PR` olan bir bulgu `doğrulandı` olunca PR'daki yorumuna cevap ver ve konuyu çöz (§4.2).

### 2.4 Yazanın notları

Ancak inceleme bittikten sonra fazın PROGRESS'teki adım notlarını ve son devir notunu oku. Bunlar yalnız durumu anlamak içindir: hangi adımın bittiği, bilerek bırakılan neler. Bir bulgu bir D-kaydıyla bilerek seçilmiş bir yola dayanıyorsa bulgu değildir. Kaydın kendisi bir Y maddesiyle çelişiyorsa bu kullanıcıya sorulur.

### 2.5 Sınıflandır ve PROGRESS'e yaz

Bulguları sınıflandır:
- **Kapsam içi gerçek hata** → `açık` bulgu, sıradaki ID ile (`V<faz>-<n>`, numaralar fazda artar, yeniden kullanılmaz).
- **Kapsam dışı** → "Sonraya bırakılanlar"a, `(Faz <id> doğrulaması)` önekiyle ve bir *Koşul:* ile.
- **Yanlış pozitif** → yalnız raporda, tek cümleyle.

Fazın `### Faz <id>` bölümünün sonunda, adım listesinin altında, `#### Doğrulama` bölümünü kur ya da güncelle:

```
#### Doğrulama
Son doğrulanan commit: `<hash>` (<tarih>; test N/N, atlanan 0; code-review k bulgu)
| ID | Kaynak | Yer | Bulgu | Durum |
|---|---|---|---|---|
| V<faz>-1 | test / code-review / CLI / PR | dosya:satır ya da test adı | kısa hata satırı | açık · kapatıldı (<hash>) · doğrulandı |
```

- "Son doğrulanan commit" incelenen HEAD'dir; açık bulgu olsa da ilerler. Yazan oturumun kapısı (D-088 (3)) bu satırı okur.
- `test N/N` başarılı/toplam test sayısıdır. Atlanan test sayısı 0 değilse (Docker yok) satırı **ilerletme**. Altına "Deneme `<hash>` (<tarih>): atlanan N (<neden>); aralık doğrulanmadı" yaz.
- Commit: `git add docs/plan/PROGRESS.md` ve `faz<id>: verify <kısa-hash>`. Ara modda push etme; fazın push kararı (ör. Q-59, Q-60) yazan oturumun push'uyla gider.

Açık bulgu varsa, ya da faz sonu ve PR modunun koşulları tutmuyorsa §5'e geç.

## 3. Faz sonu modu

Koşul: bütün adımlar `[x]`, açık bulgu yok, §2.1'deki testler yeşil ve atlanan 0. Tutmuyorsa §5.

1. **PLAN'ın "Faz doğrulaması" komutları:** §2.1'de koşmayanları çalıştır ve her birinin sonucunu rapora koy. Uzun süren komutları arka planda çalıştır ve bitmesini bekle, poll etme (`bash eng/template-smoke.sh` ~6 dk).
2. **code-review**, bu kez fazın bütün branch'i üzerinde (`main...HEAD`).
3. **CodeRabbit CLI** (fazda bir kez, D-082 (2)): çalışma ağacı temizken `coderabbit review --agent --base main`. Uzun sürebilir: arka planda çalıştır, bitmesini bekle, poll etme. CLI kurulu ya da oturumu açık değilse (`coderabbit auth status`) ya da rate limit'e takılırsa fazı bekletme. Raporda "CLI incelemesi yok: <neden>" de; PR incelemesi yine yakalar.
4. Bulguları §2.5'teki gibi sınıflandır ve yaz (kaynak `code-review`, `CLI` ya da komutun kendisi için `test`).
   - **Bulgu varsa:** PROGRESS'i commit'le, raporla ve §5'teki son satırla yazana dön.
   - **Yoksa** devam et.
5. **Fazı kapat (PROGRESS):**
   - faz `✅ tamam` ve bitiş tarihi,
   - "Son doğrulanan commit" satırı,
   - **son devir notu** en üste: yapılanlar, sürprizler, bekleyen sorular, sonraki fazın bilmesi gerekenler; PROGRESS'teki adım notlarından ve fazın D-kayıtlarından derlenir,
   - **arşiv (D-082 (3)):** bir önceki fazın "Adımlar" bölümünü, son üç devir notundan eskilerini ve üstü çizilmiş "Sonraya bırakılanlar" maddelerini değiştirmeden `PROGRESS-ARSIV.md`'nin ilgili bölümlerine taşı. Biten fazın adımları PROGRESS'te kalır.
   - Faz bir konvansiyonu değiştirdiyse README, CLAUDE.md ve memory'nin güncellendiğini kontrol et. Eksikse bu bir bulgudur: faz kapanmaz, yazana dön.
6. Commit (`faz<id>: verify <kısa-hash>`); çalışma ağacında bir şey kalmasın.
7. **Push ve PR (D-067 (1)):** `git push -u origin <branch>`, ardından `gh pr create --base main --head <branch>`. Başlık `faz<id>: <faz adı>`. Gövde son devir notunun özeti ve oturumun PR attribution satırıdır. PR'ı açmak sürecin parçasıdır, ayrıca sorulmaz. CodeRabbit kendiliğinden inceler. İncelemeyi bu oturumda bekleme.
8. Raporla. Son satır: "PR #N açıldı. İnceleme gelince: `/clear`, ardından `/novacore-verify <id>`".

## 4. PR modu

Koşul: fazın PR'ı açık. §2'den sonra (aralıkta yazanın PR düzeltmeleri ve `kapatıldı` bulgular vardır):

1. **PR'ı oku:**
   - PR'ın kendisi ve konuşma yorumları: `gh pr view <no> --comments`,
   - satır yorumları: `gh api repos/{owner}/{repo}/pulls/<no>/comments`,
   - incelemeler: `gh api repos/{owner}/{repo}/pulls/<no>/reviews`,
   - CodeRabbit'in özet yorumundaki pre-merge check uyarıları,
   - CI durumu: `gh pr checks <no>`.

   Cevaplanmış ya da çözülmüş yorumları atla.
2. **Her yorumu sınıflandır:**
   - **Kapsam içi gerçek hata** → bulgu (kaynak `PR`, yer `dosya:satır` ve yorumun ID'si, ör. `Foo.cs:12 (yorum 4388968154)`), `açık`. Yoruma şimdi değil, düzeltme doğrulanınca cevap verilir (§2.3).
   - **Kapsam dışı** → "Sonraya bırakılanlar". Yorumu GitHub'da gerekçesiyle yanıtla ("Sonraya bırakılanlar"a alındı, ilgili D/Y ID'si).
   - **Yanlış pozitif ya da bir D/Y kaydıyla çelişen öneri** → yorumu gerekçesi ve kaydın ID'siyle yanıtla.

   Satır yorumuna cevap: `gh api repos/{owner}/{repo}/pulls/<no>/comments/<id>/replies -f body=...`. Konuşma yorumuna cevap: `gh pr comment <no> --body ...`. Düzeltmesi doğrulanan bir yorumun cevabı tek satırdır: düzelten commit'in hash'i ve ne değiştiği. Ardından konuyu çöz:
   - konunun ID'sini `gh api graphql` ile bul: `pullRequest(number: <no>) { reviewThreads { nodes { id isResolved comments(first: 1) { nodes { databaseId } } } } }`, yorumun ID'si `databaseId`'dir,
   - `resolveReviewThread(input: { threadId: "<id>" })` mutation'ını çalıştır.

   CodeRabbit inceleme yapmadıysa (rate limit, kesinti) bunu raporda söyle; merge'ü engellemez (D-067 (2)).
3. PROGRESS'i §2.5'e göre güncelle, commit'le ve aynı branch'e push'la: merge'ten önce PR'da commit'lenmemiş bir şey kalmasın.
4. **Açık bulgu varsa** raporla, §5'teki son satırla yazana dön.
5. **Her şey temizse** (açık bulgu yok, bütün yorumlar ele alındı, CI yeşil) merge'ü `AskUserQuestion` ile sor. Soru merge'ü ve merge kaydının `main`'e push'unu birlikte kapsar.
   - Kontroller hâlâ koşuyorsa bunu soruda söyle, poll etme.
   - Onay gelince:
     - `gh pr merge <no> --merge` (merge commit; squash ya da rebase değil, adım commit'leri korunur),
     - `git switch main` ve `git pull --ff-only`,
     - PROGRESS'te faz tablosunun Merge sütununa ve son devir notuna merge kaydı: tarih, onay, merge commit'i,
     - `faz<id>: record PR #N's merge in PROGRESS` ile commit ve `git push` (D-067 (1): PROGRESS kayıtları doğrudan `main`'e gidebilir).
   - Merge onaylanınca o turda yapılır; CI hâlâ koşuyorsa son yeşil koşunun kodu kapsayıp kapsamadığına bak, tekrar sorma.
6. **Dur.** Sonraki faza başlama. Kullanıcı onu yeni bir oturumda `/novacore-phase <id>` ile başlatır.

## 5. Rapor

Her doğrulamanın sonunda kısa bir rapor yaz:

```
## Faz <id> doğrulaması: <ara | faz sonu | PR #N>

**Aralık:** `<taban>..<hash>` (N kod commit'i)  ·  **Branch:** faz/...
**Sonuç:** temiz | k açık bulgu | doğrulanmadı (neden)

### Komutlar
- `dotnet restore --locked-mode` → geçti/kaldı
- `dotnet build -v q` → geçti/kaldı (özet)
- `dotnet format --verify-no-changes` → geçti/kaldı
- `dotnet test --solution NovaCore.slnx` → N/N, atlanan M (Docker)
- (faz sonu) PLAN'ın faz doğrulaması: her komut → geçti/kaldı

### Bulgular (yeni ve durumu değişenler)
| ID | Kaynak | Yer | Bulgu | Durum |
|---|---|---|---|---|

### Code review özeti
- bulgu sayısı; açık / Sonraya bırakılan / yanlış pozitif (her yanlış pozitif tek cümle)

### Kabul kriterleri
- <adım-id>: kanıtlayan test (sınıf.metot) | eksik

### CodeRabbit (faz sonu ve PR modu)
- CLI (faz sonu): bulgu sayısı; açık / Sonraya bırakılan / yanlış pozitif. Çalışmadıysa nedeni.
- PR #N: bulgu yapılan / Sonraya bırakılan / yanıtlanan yorumlar; pre-merge check'ler; CI.

### Kontrol edilmeyenler
- ...

<son satır>
```

Son satır kopyalanacak bir komut ya da merge sorusudur:
- Açık bulgu var ya da adım kaldı: "Devam: `/clear`, ardından `/novacore-phase <id>`"
- Docker yoktu: "Docker'ı açıp: `/clear`, ardından `/novacore-verify <id>`"
- PR açıldı: "PR #N açıldı. İnceleme gelince: `/clear`, ardından `/novacore-verify <id>`"
- PR temiz: merge sorusu (`AskUserQuestion`, §4.5).

Bağlam uzarsa ya da özetlenirse devam etmeden önce PROGRESS'i ve PLAN'daki faz bölümünü yeniden oku.

**Hook'lar.** Bu oturum kaynak dosyalara dokunmadığı için Stop hook'u (yalnız build, D-088 (6)) normalde çalışmaz. Bir hook bir işlemi durdurduysa atlatmaya çalışma, nedenini kullanıcıya açıkla.
