# İlerleme

> Tek doğru durum kaynağı. `/novacore-phase` her başlangıçta bu dosyayı okur ve her adımdan sonra günceller.
> Adım tanımları [PLAN.md](PLAN.md)'dedir. Burada yalnızca durum, commit hash'i ve notlar tutulur.

## Fazlar

| Faz | Durum | Branch | Başlangıç | Bitiş | Merge |
|---|---|---|---|---|---|
| 0 | ⏳ başlamadı | | | | |
| 1 | ⏳ başlamadı | | | | |
| 2a | ⏳ başlamadı | | | | |
| 2b | ⏳ başlamadı | | | | |
| 2c | ⏳ başlamadı | | | | |
| 3a | ⏳ başlamadı | | | | |
| 3b | ⏳ başlamadı | | | | |
| 4a | ⏳ başlamadı | | | | |
| 4b | ⏳ başlamadı | | | | |
| 5 | ⏳ başlamadı | | | | |

Durum değerleri: `⏳ başlamadı` · `🔄 devam ediyor` · `⏸ duraklatıldı` (devir notuna bak) · `✅ tamam`

## Adımlar

Biçim: `- [x] F0.3 (abc1234): not`

### Faz 0
- [ ] F0.1 Git başlangıcı, LICENSE, CHANGELOG
- [ ] F0.2 Paketler ve açıklar
- [ ] F0.3 Rate limiter bölümleme
- [ ] F0.4 IDomainEvent sabit değerler
- [ ] F0.5 E-posta gönderici yalnızca dev
- [ ] F0.6 Ölü kod ve doküman düzeltmesi
- [ ] F0.7 Yasakları testlere çevirme
- [ ] F0.8 CI
- [~] F0.9 (koşullu) Identity minimal düzeltmeleri. **Atlandı** (Q-04: Identity kullanılmayacak)

### Faz 1
- [ ] F1.1 Build ayarları
- [ ] F1.2 Uyarıların düzeltilmesi
- [ ] F1.3 Versiyon
- [ ] F1.4 Public API takibi
- [ ] F1.5 Pack doğrulaması
- [ ] F1.6 Yayın akışı

### Faz 2a
- [ ] F2a.1 Çekirdek projeler
- [ ] F2a.2 Eklenti paketleri
- [ ] F2a.3 Açık modül kaydı
- [ ] F2a.4 Mimari testler
- [ ] F2a.5 Dokümanlar
- [ ] F2a.6 (opsiyonel) İlk önizleme sürümü

### Faz 2b
- [ ] F2b.1 Options pattern
- [ ] F2b.2 Test host sadeleştirme
- [ ] F2b.3 TimeProvider
- [ ] F2b.4 Exception handling
- [ ] F2b.5 Health
- [ ] F2b.6 LoggerMessage

### Faz 2c
- [ ] F2c.1 Validation
- [ ] F2c.2 Yetkilendirme
- [ ] F2c.3 Cache
- [ ] F2c.4 Paket temizliği

### Faz 3a
- [ ] F3a.1 JWT doğrulama (Authority/JWKS)
- [ ] F3a.2 İzin çözümleme
- [ ] F3a.3 Test IdP yardımcısı

### Faz 3b
- [ ] F3b.1 OpenIddict eklentisi
- [ ] F3b.2 Sertleştirmelerin taşınması
- [ ] F3b.3 Eski modülün emekliye ayrılması
- [ ] F3b.4 docker-compose
- [ ] F3b.5 Uçtan uca doğrulama

### Faz 4a
- [ ] F4a.1 Event'lerin outbox'a yazılması
- [ ] F4a.2 Dispatcher
- [ ] F4a.3 Eski dispatcher düzeltmesi

### Faz 4b
- [ ] F4b.1 Stok kararı
- [ ] F4b.2 Uygulama
- [ ] F4b.3 Eşzamanlılık testi

### Faz 5
- [ ] F5.1 Template
- [ ] F5.2 Tüketici smoke testi
- [ ] F5.3 Test borcu
- [ ] F5.4 1.0.0 kararı
- [ ] F5.5 Final dokümanlar

## Açık sorular (Q)

Soru cevaplanınca buraya cevap yazılır. Kalıcı bir karar ortaya çıkarsa DECISIONS'a D-kaydı olarak eklenir.

| ID | Soru | Ne zaman gerekli | Cevap |
|---|---|---|---|
| Q-01 | Önceki oturumdan gelen "yapılmasın" listesi (DECISIONS → Y-13+) | Faz 0'dan önce | Kullanıcı araştırma istedi (2026-09-25). Öneri: [proposals/yapilmayacaklar-onerisi.md](proposals/yapilmayacaklar-onerisi.md). **Onaylandı** → DECISIONS Y-13…Y-43 (D-007). Hook'un yasaklı listesine EF InMemory ve Proxies eklendi, tam sürümlü paket referansı engelleniyor. F0.7 kapsamı genişletildi. |
| Q-02 | LICENSE türü (öneri: MIT) | F0.1 | **MIT** (2026-09-25, kullanıcı) → D-006 |
| Q-03 | GitHub remote/repo var mı? CI ve paket feed'i için | F0.8, F1.6 | **Henüz yok** (2026-09-25). F0.8: workflow yazılır, komutlar yerelde çalıştırılarak doğrulanır, ilk CI koşusu repo açılınca yapılır. F1.6: workflow yazılır ama tetiklenmez. Repo açıldığında bu satır güncellenir. |
| Q-04 | Faz 3'e kadar Identity modülü aktif kullanılacak mı? (F0.9 yapılsın mı?) | Faz 0 başı | **Hayır.** Kullanıcı, plan bitene kadar Identity'yi hiç kullanmayacak (2026-09-25). **F0.9 atlanır.** F0.5'in (yalnızca Identity'ye ait) kaderi Q-05'te. |
| Q-05 | F0.5 (LoggingEmailSender yalnızca dev) hâlâ yapılsın mı? Identity kullanılmayacak ve Faz 3b'de emekliye ayrılacak | Faz 0 başı | **Evet, yapılsın** (2026-09-25, kullanıcı). F0.5 planda olduğu gibi kalır. |

## Sonraya bırakılanlar

Plan dışında fark edilen, ama kapsam dışı olduğu için dokunulmayan konular. Her faz sonunda kullanıcı bunları gözden geçirir.

- xUnit v3 → 4.x ve xunit.runner.visualstudio 4.x major güncellemesi (F0.2 kapsamı dışında). Kaynak: `dotnet list package --outdated`, 2026-09-24.

## Son devir notu

> Her oturumun sonunda (faz bitmese bile) üzerine yazılır. Bir sonraki oturumun bilmesi gerekenler: nerede kalındığı, neyin yarım kaldığı, sürprizler ve bekleyen sorular.

(Henüz faz oturumu yok. Kurulum 2026-09-24'te tamamlandı. 2026-09-25 itibarıyla **tüm açık sorular cevaplandı**: Q-01 (Y-13…Y-43, D-007), Q-02 (MIT, D-006), Q-03 (remote yok), Q-04 (Identity kullanılmayacak, F0.9 atlandı), Q-05 (F0.5 yapılacak). Faz 0 başlamaya hazır. Sıradaki adım: yeni bir oturumda `/novacore-phase 0`.)
