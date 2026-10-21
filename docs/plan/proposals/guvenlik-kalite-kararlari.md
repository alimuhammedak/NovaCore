# Güvenlik ve kod kalitesi: nihai kararlar ve faz önerisi

> **Durum: kullanıcı kararları alındı (2026-10-21), DECISIONS/PLAN'a taşınmayı bekliyor.** `DECISIONS.md` ve `PLAN.md` korumalı dosyalar. Bu belgedeki K-kararları ilk `/novacore-phase 6a` oturumunun başında tek bir düzenlemeyle D-042… olarak, faz önerisi de PLAN'a "Faz 6a–6e" olarak taşınır.
> Gerekçe ve kaynaklar: [guvenlik-kalite-arastirmasi.md](guvenlik-kalite-arastirmasi.md) (iki kanatlı araştırma raporu; tablo, bulgu ve birincil kaynak orada). Kod durumu: `main` @ `6f52826`.

**Etiketler:** **[kod]** NovaCore kodunda okuyarak doğrulandı · **[çalıştırıldı]** komutla ölçüldü · **[doğrulanacak]** fazın ilk adımında testle teyit edilir.

---

## 1. Kullanıcı kararları

| # | Konu | Karar | Etkilediği kayıt |
|---|---|---|---|
| K1 | Repo görünürlüğü | **Public.** Ön koşul tarandı **[çalıştırıldı]**: 156 commit, tüm ref'ler, gitleaks 8.30.1 + Betterleaks 1.9.0 + TruffleHog 3.99.3. 31 işaretin tamamı test sabiti (`"a long passphrase for tests"` vb.). `appsettings*`, `docker-compose*`, `.env.example`, `deploy/` geçmişinde gerçek sır yok. Commit e-postası (kişisel adres) bilerek açık kalıyor. Görünürlük değişikliğini kullanıcı GitHub ayarlarından yapar (oturum proxy'si ayar yazımını engelliyor). | D-039 (1) yeniden açılır (paketlerin görünürlüğü, aşağıda) |
| K2 | SQL Server test katmanı | **D-035 (7) yeniden açılıyor.** `Testcontainers.MsSql` (MIT) + `Respawn` (Apache-2.0) eklenir. SQLite hızlı varsayılan katman olarak kalır. | D-035 (7), Y-32 ile uyumlu |
| K3 | Güvenlik araç seti | **Ücretsiz + mevcut Claude aboneliği; ücretsiz denemeler kabul.** API ücretiyle çalışan koşular (Strix, Shannon, Codex Security) yok. Ajan katmanı: Cloudflare `security-audit-skill`, Claude Security plugin, `/security-review`. Black-box katmanı deterministik ve ücretsiz araçlarla: Schemathesis, ZAP, OpenID conformance suite (yerel Docker). | — |
| K4 | Kalite araç seti | **En kapsamlı set (C).** Kod public olduğu için SaaS analizi sorun değil: SonarQube Cloud (açık kaynakta ücretsiz), Qodana Community, Meziantou, Roslynator CLI, CodeRabbit (OSS ücretsiz) dahil. `SonarAnalyzer.CSharp` NuGet paketi **eklenmez** (SSALv1, Y-06); Sonar yalnızca CI'daki scanner ile çalışır. | Y-06 korunur |
| K5 | Güvenlik sınırı düzeltmeleri | **Dört grubun hepsi onaylandı:** MFA sertleştirme, yetki yükseltmeyi kapatma, token/protokol sertleştirme, NIST şifre politikası (madde listesi §3, Faz 6b). | D-026, D-036, D-038 kısmen değişir |
| K6 | Ödeme görünürlüğü | **İkisi birden:** seeding'deki "`:read` ile biten her izin `User`'a" kuralı kalkar, rol izinleri açık listeye döner. `Payment`'a sahip (owner) eklenir, kullanıcı yalnızca kendi ödemelerini görür; tüm ödemeleri yalnızca yetkili rol görür. | D-026 seeding maddesi |
| K7 | Tedarik zinciri ve süreç | **Hepsi onaylandı:** `BMD.*` önekinin nuget.org'da rezervasyonu (başvuruyu kullanıcı yapar), uygulama/test projelerinde lock dosyaları + `global.json` `rollForward: disable`, Claude hook'larının genişletilmesi (SHA'ya sabitlenmemiş action, sır desenleri), Ponytail'dan 2–3 kuralın CLAUDE.md'ye alınması (eklenti kurulmaz). | `.claude/hooks/`, CLAUDE.md |
| K8 | Yeni Microsoft dışı araç/paketler | K3–K4 setlerindekiler onaylı sayılır (hepsi MIT/Apache, lisanslar raporda). Hook her yeni paket için yine onay ister; bu onay o anda verilir. | D-016 |

**Sorulmadan varsayılan seçilenler (rapordaki öneriler, itiraz edilirse değişir):**
- **Y-44:** Bastırma gerektiren her yerde bastırmasız alternatif seçilir. Tek istisna Microsoft'un "When to suppress" bölümünün izin verdiği durumlar (ör. uygulama kodunda CA2007).
- **Test assertion'ları:** xUnit `Assert` kalır. AwesomeAssertions eklenmez (Y-01'in ruhu).
- **`claude-code-action`:** Kullanılırsa yalnızca salt okunur izinle çalışır.

**Diğer modelin raporundan alınanlar** (`deep-research-report.md`, 2026-10-21): Administrator için zorunlu 2FA / step-up (K5'e eklendi) ve CI'da `timeout-minutes` + `concurrency` (6a'ya eklendi). Raporun repo ağacı erişimsiz tahmine dayanıyor ve bazı yolları yanlış (Identity `Modules` altında değil, `BuildingBlocks` altında). Bu yüzden yalnızca bu iki öneri alındı.

---

## 2. Public geçişin açtıkları ve değiştirdikleri

- **Ücretsiz açılanlar:** CodeQL + code scanning sekmesi (SARIF yüklemesi dahil), secret scanning + push protection, dependency review, rulesets / korumalı branch, artifact attestations, OpenSSF Scorecard action ([raporda Güvenlik §"Özel kişisel repo"](guvenlik-kalite-arastirmasi.md)).
- **D-039 (1) yeniden açılır:** "Repo private olduğu için paketler de private" önermesi artık geçerli değil. GitHub Packages'ın NuGet kayıt defteri depo kapsamlıdır; paketlerin görünürlüğü depoya bağlanır **[doğrulanacak]**. Tüketen projelerin hâlâ token taşıması gerekip gerekmediği 6a'nın ilk adımında belgeden ve gerçek bir restore ile doğrulanır.
- **CodeQL modu:** `build-mode: autobuild` veya `manual` (`dotnet build NovaCore.slnx`). `none` uygun değil: kod tabanı source generator'lara dayanıyor (LoggerMessage, OptionsValidator, Mapperly, validation, OpenAPI).
- **Fork PR'ları:** `ci.yml` `push` + `pull_request` ile tetikleniyor, `pull_request_target` yok, izin `contents: read`. Public'te fork PR'ları salt okunur token alır. Ek önlem gerekmiyor, zizmor yine de denetler.

---

## 3. Faz önerisi (PLAN'a "Faz 6" olarak)

Sıra: `5 → 6a → 6b → 6c → 6d → 6e`. Gerekçe: 6a ucuz ve CodeQL ile secret scanning'i açarak 6b'nin değişikliklerini de taratıyor. 6b, public kodda görünen doğrulanmış açıkları kapatıyor. 6c gerçek SQL Server'ı getirerek 6b'deki migration'ı (Payment owner) ve eski kör noktaları sınıyor. 6d analizörleri, 6c'nin ölçüm araçları (coverage, Stryker) devredeyken sıkılaştırıyor. 6e en pahalı ve en gürültülü katmanı en son ekliyor.

### Faz 6a: Public geçiş ve CI/tedarik zinciri sertleştirme
- Action'ları tam commit SHA'sına sabitlemek (pinact), zizmor + actionlint CI adımı. Kapsam: `ci.yml`, `release.yml`, şablonun `ci.yml`'i.
- `persist-credentials: false` (ci.yml:18,68); `release.yml:85-86`'daki `${{ }}` interpolasyonunu env değişkenine taşımak; `packages: write`'ı job düzeyine indirmek; yayına `environment:` kapısı; `timeout-minutes`, `concurrency`.
- `.github/dependabot.yml`: `nuget`, `github-actions`, `dotnet-sdk` ekosistemleri. Şablondaki ikinci `Directory.Packages.props` için bilinen sorunlar (#10717, #15699) raporda.
- CodeQL (autobuild/manual), secret scanning + push protection, `main` ve `v*` için ruleset (required checks), Scorecard action.
- `nuget-license` ile MIT/Apache/Microsoft izin listesi kapısı (Y-06 otomasyonu). CycloneDX SBOM + `actions/attest` ile build provenance (yalnızca release).
- Lock dosyaları (uygulama + test projeleri) + `RestoreLockedMode` CI'da + `rollForward: disable`.
- Betterleaks pre-commit + CI tam geçmiş taraması; hook genişletmesi (K7).
- `docker-compose.yml`: portları `127.0.0.1`'e bağlamak, image'ları digest ile sabitlemek.
- D-039 (1)'in güncellenmesi (§2).

### Faz 6b: Kimlik güvenlik sınırı (K5, K6). Her madde önce kırmızı test
- **MFA:** TOTP tekrar kullanımını engellemek (son kabul edilen zaman adımını saklamak, ASVS 6.5.1); kurtarma kodlarını hash'lemek (ASVS 6.5.2); `/2fa/setup|enable|disable` ve kurtarma kodu yenileme için yeniden kimlik doğrulama (ASVS 7.5.1); Administrator için zorunlu 2FA.
- **Yetki:** Administrator'ı yalnızca Administrator verebilir (`identity:roles:manage` kullanılır); son admin koruması; rol değişikliğinde izin önbelleğinin silinmesi + refresh token iptali.
- **Seeding + Payments (K6):** Açık rol-izin listesi; `Payment.OwnerId` + migration; `GetPayments` çağıranın kendi ödemelerine filtrelenir, tüm liste ayrı bir izinle.
- **Token/protokol:** `ValidTypes = ["at+jwt"]`; `/connect/authorize` ve `/account/*` CORS dışında (RFC 9700 §2.6); Development dışında `http` redirect URI reddi; client secret minimum gücü; mutlak refresh ömrü (`FakeTimeProvider` testiyle önce doğrulanır, açık çıkarsa `DisableSlidingRefreshTokenExpiration`).
- **Şifre:** Minimum 15 karakter + yerel blocklist (dış çağrı yok; NIST SP 800-63B-4).
- **Doğrulanacaklar** (ölçülüp açık çıkarsa aynı fazda kapanır): register/forgot-password/resend zamanlama farkı, 2FA ve antiforgery cookie bayrakları, `/connect/revoke` üzerinde strict rate limit.
- Eksik negatif protokol testleri: `redirect_uri` varyantları, kod yeniden kullanımı, başka client ile kod kullanımı, PKCE düşürme, `response_type=token` / `grant_type=password`, `/connect/revoke`, `iss` parametresi, JWKS'te özel parça olmaması, `/account/*` clickjacking başlıkları.

### Faz 6c: Test altyapısı (K2)
- MTP + xUnit 4'e geçiş (`global.json` `test.runner`). `--filter "FullyQualifiedName~"` örnekleri xUnit bayraklarına güncellenir (CLAUDE.md, tests/CLAUDE.md).
- SQL Server katmanı: Testcontainers + Respawn; `MigrateAsync` ile gerçek migration'lar (EXEC'li `*_OutboxConsumersByMessage` dahil); `GET /payments`; şema izolasyonu; `EF.Functions.Like` collation; `decimal(18,2)`.
- `OutboxDispatcher.cs:178`'deki SQLite yüzünden bellekte yapılan karşılaştırma gözden geçirilir (test aracının üretim kodunu şekillendirmesi).
- Coverage: coverlet.MTP + ReportGenerator, main'e göre düşüşü engelleyen ratchet.
- API sözleşmesi: OpenAPI dokümanını build'de üretip commit'lemek + oasdiff `breaking` (`review: false`).
- CsCheck property testleri: `Result`, sayfalama, Payment/Product durum kuralları, outbox handler idempotency.
- Stryker.NET haftalık, Abstractions + modül domain kodu, ilk aşamada `break-at: 0` (yalnızca rapor).

### Faz 6d: Analizör ve platform sıkılaştırma (K4)
- Kategori bazlı `AnalysisMode<Kategori>=All`: önce Security (CA3001–CA3012, CA2100, CA5394, CA5404 vb.), sonra Reliability, Performance, Usage. Her kategori ayrı commit; çıkan hata sayısı önce ölçülür.
- `.editorconfig`: IDE0051/52/59/60 ve IDE0005 warning'e (IDE0005 için bastırmasız kapsam seçimi). CA1502/CA1505/CA1506 + `CodeMetricsConfig.txt`. CI'da `dotnet format --verify-no-changes`.
- BannedApiAnalyzers (saat, `Console`, `Environment.Exit` vb.). Meziantou.Analyzer kademeli. Roslynator CLI `find-symbol --unused` periyodik.
- SonarQube Cloud (scanner CI'da), Qodana Community deneme koşusu, CodeRabbit (OSS ücretsiz) PR incelemesi.
- `PackageValidationBaselineVersion` ilk kararlı sürümden sonra (D-039 (2) ile uyumlu).
- csharp-lsp (Claude Code); Ponytail kuralları CLAUDE.md'ye (K7).

### Faz 6e: Dinamik ve ajan tabanlı denetim (K3)
- Yalnızca DAST ortamı yapılandırmasında bulunan, izinleri sınırlı bir test OIDC client'ı. Üretim yapılandırmasında olmadığını doğrulayan bir test.
- CI'da (zamanlanmış + `workflow_dispatch`) atılabilir instance + SQL Server container: Schemathesis (birincil), ZAP API scan (ikincil). `/connect/*` ve `/account/*` spec dışında olduğu için ayrı hedeflenir.
- OpenID conformance suite (yerel Docker), sürüm öncesi.
- Ajan koşuları: Cloudflare `security-audit-skill` + Claude Security plugin, önce auth çekirdeği sonra tüm repo, her kapsam en az iki kez (Cloudflare'in kendi notu: tek koşu bulguların yaklaşık yarısını buluyor). Girdi olarak §3 6b listesi ve raporun OIDC kontrol listesi verilir.
- **Triage kuralı:** Ajan bulgusu ancak başarısız bir xUnit testine dönüşebiliyorsa düzeltilir. Test regresyon testi olarak kalır. Dönüşmeyen bulgu `needs_validation` olarak PROGRESS'e yazılır.

---

## 4. Açık kalan küçük konular

- `BMD.*` rezervasyonu: nuget.org hesabı ve ön ek rezervasyon başvurusu kullanıcıda. Paketler GitHub Packages'ta kaldığı sürece bu yalnızca savunma amaçlı.
- GitHub Pro fiyatı, Qodana'nın .NET 10 desteği, oasdiff'in OpenAPI 3.1 kapsamı: raporda doğrulanmamış olarak işaretli; ilgili fazda ölçülür.
