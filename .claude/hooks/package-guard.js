#!/usr/bin/env node
// PreToolUse guard (Edit|Write|Bash|PowerShell).
// - Yasaklı NuGet paketlerini reddeder (deny).
// - Allowlist dışındaki yeni paketler için kullanıcı onayı ister (ask).
// - Korunan dosyaların (PLAN, DECISIONS, hook'lar, settings) değiştirilmesi için kullanıcı onayı ister (ask).
// - Workflow'a yazılan, tam commit SHA'sına sabitlenmemiş `uses:` için onay ister (ask; D-051 (1)).
// - Bir dosyaya yazılan, sır gibi görünen değer için onay ister (ask; Y-35, D-048 (3)).
// Politika: .claude/hooks/package-policy.json. Kaynak: docs/plan/DECISIONS.md.
'use strict';

const fs = require('fs');
const path = require('path');

const projectDir = process.env.CLAUDE_PROJECT_DIR || process.cwd();

function readStdin() {
  try {
    return fs.readFileSync(0, 'utf8');
  } catch {
    return '';
  }
}

function respond(decision, reason) {
  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: decision,
      permissionDecisionReason: reason
    }
  }));
  process.exit(0);
}

let input;
try {
  input = JSON.parse(readStdin());
} catch {
  process.exit(0); // Girdi okunamazsa engelleme; hook hiçbir aracı kilitlememeli.
}

let policy;
try {
  policy = JSON.parse(fs.readFileSync(path.join(__dirname, 'package-policy.json'), 'utf8'));
} catch (e) {
  respond('ask', `package-policy.json okunamadı (${e.message}). Paket/korunan dosya kontrolü yapılamıyor; devam etmek için onay gerekiyor.`);
}

const tool = input.tool_name || '';
const ti = input.tool_input || {};

// Windows'ta hem "C:\x\y" hem Git Bash tarzı "/c/x/y" yollarını aynı biçime indir.
const toNative = p => String(p).replace(/^\/([a-zA-Z])\//, '$1:/');
const normalize = p => path.resolve(toNative(projectDir), toNative(p)).replace(/\\/g, '/').toLowerCase();
const protectedPaths = (policy.protectedPaths || []).map(p => ({ raw: p, abs: normalize(p) }));

function protectedMatch(filePath) {
  if (!filePath) return null;
  const abs = normalize(filePath);
  return protectedPaths.find(p => (p.raw.endsWith('/') ? abs.startsWith(p.abs + '/') || abs === p.abs : abs === p.abs)) || null;
}

const lower = s => s.toLowerCase();
const isBanned = id => (policy.banned || []).some(b => lower(id) === lower(b) || lower(id).startsWith(lower(b) + '.'));
const isAllowed = id =>
  (policy.allowedPrefixes || []).some(p => lower(id).startsWith(lower(p))) ||
  (policy.allowed || []).some(a => lower(a) === lower(id));

function packagesFromProjectText(text) {
  const ids = new Set();
  const re = /<Package(?:Reference|Version)\b[^>]*?\b(?:Include|Update)\s*=\s*"([^"]+)"/gi;
  let m;
  while ((m = re.exec(text)) !== null) ids.add(m[1].trim());
  return [...ids];
}

function packagesFromCommand(cmd) {
  const ids = new Set();
  const patterns = [
    /\bdotnet\s+add\s+(?:"[^"]*"\s+|\S+\s+)?package\s+([A-Za-z0-9_.\-]+)/gi, // dotnet add [proj] package X
    /\bdotnet\s+package\s+add\s+([A-Za-z0-9_.\-]+)/gi,                       // dotnet package add X (.NET 10)
    /\bInstall-Package\s+([A-Za-z0-9_.\-]+)/gi,
    /\bnuget(?:\.exe)?\s+install\s+([A-Za-z0-9_.\-]+)/gi
  ];
  for (const re of patterns) {
    let m;
    while ((m = re.exec(cmd)) !== null) ids.add(m[1]);
  }
  return [...ids];
}

function checkPackages(ids, where) {
  const banned = ids.filter(isBanned);
  if (banned.length) {
    respond('deny', `Yasaklı paket: ${banned.join(', ')} (${where}). DECISIONS.md Yapılmayacaklar: Y-01 (MediatR/AutoMapper/FluentAssertions: ticari), Y-32 (EF InMemory: testte gerçek DB kullanılır), Y-33 (EF Proxies: lazy loading). Alternatif gerekiyorsa kullanıcıya sor.`);
  }
  const unknown = ids.filter(id => !isAllowed(id));
  if (unknown.length) {
    respond('ask', `Onaylanmamış yeni paket: ${unknown.join(', ')} (${where}). DECISIONS D-003/Y-47: çekirdek yalnızca Microsoft/BCL; üçüncü taraf paket yalnızca eklenti paketlerinde ve MIT/Apache lisanslı olabilir. Onaylarsan paketi .claude/hooks/package-policy.json allowlist'ine de ekle ve DECISIONS'a D-kaydı yaz.`);
  }
}

const projectFile = f => /\.(csproj|props|targets|fsproj|vbproj)$/i.test(f || '');
const workflowFile = f => /(^|[\\/])\.github[\\/]workflows[\\/][^\\/]+\.ya?ml$/i.test(f || '');

// D-051 (1): her action tam commit SHA'sına sabitlenir. Yerel action'lar (./) ve digest'li Docker imajları zaten sabittir.
function unpinnedUses(text) {
  const bad = [];
  const re = /^\s*(?:-\s*)?uses:\s*['"]?([^'"\s#]+)/gm;
  let m;
  while ((m = re.exec(text)) !== null) {
    const ref = m[1];
    if (ref.startsWith('./') || /^docker:\/\/[^@]+@sha256:[0-9a-f]{64}$/i.test(ref)) continue;
    if (!/@[0-9a-f]{40}$/i.test(ref)) bad.push(ref);
  }
  return bad;
}

// Y-35: repoya sır girmez. Yalnızca biçimi belli, yanlış alarmı düşük desenler; bulunan değer mesaja yazılmaz.
const secretPatterns = [
  ['özel anahtar', /-----BEGIN (?:RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-----/],
  ['GitHub token', /\b(?:gh[pousr]_[A-Za-z0-9]{36,}|github_pat_[A-Za-z0-9_]{22,})/],
  ['AWS erişim anahtarı', /\b(?:AKIA|ASIA)[0-9A-Z]{16}\b/],
  ['Slack token', /\bxox[abprs]-[A-Za-z0-9-]{10,}/],
  ['nuget.org API anahtarı', /\boy2[a-z0-9]{43}\b/],
  ['bağlantı dizesinde parola', /\b(?:Password|Pwd)=(?![<$*{])[^;"'\s<>]{8,}/i]
];

const secretKinds = text => secretPatterns.filter(([, re]) => re.test(text)).map(([kind]) => kind);

if (tool === 'Edit' || tool === 'Write' || tool === 'MultiEdit' || tool === 'NotebookEdit') {
  const filePath = ti.file_path || ti.notebook_path || '';
  const prot = protectedMatch(filePath);
  if (prot) {
    respond('ask', `Korunan dosya değişikliği: ${prot.raw}. Bu dosya yalnızca kullanıcı onayıyla değişir (DECISIONS D-005, Y-11). Değişikliğin gerekçesini kullanıcıya açıkla.`);
  }
  const text = [ti.new_string, ti.content, ...(Array.isArray(ti.edits) ? ti.edits.map(e => e.new_string) : [])]
    .filter(Boolean).join('\n');
  const secrets = secretKinds(text);
  if (secrets.length) {
    respond('ask', `Yazılan içerik sır gibi görünüyor (${secrets.join(', ')}; ${path.basename(filePath)}). DECISIONS Y-35: repoya sır girmez. Gerçek bir sırsa user-secrets ya da ortam değişkeni kullan; test değeriyse kullanıcıya göster ve .betterleaks.toml listesine ekle.`);
  }
  if (workflowFile(filePath)) {
    const unpinned = unpinnedUses(text);
    if (unpinned.length) {
      respond('ask', `SHA'ya sabitlenmemiş action: ${unpinned.join(', ')} (${path.basename(filePath)}). DECISIONS D-051 (1): her action tam commit SHA'sına sabitlenir, yanına sürüm yorumu yazılır (uses: owner/repo@<40 haneli SHA> # vX.Y.Z). SHA'yı gh api repos/<owner>/<repo>/git/ref/tags/<tag> ile çöz.`);
    }
  }
  if (projectFile(filePath)) {
    checkPackages(packagesFromProjectText(text), path.basename(filePath));
    // Y-22: tam sürüm ([1.0]) veya aralık/üst sınır ([1.0,2.0), (,2.0]) içeren paket sürümleri.
    if (/<Package(?:Reference|Version)\b[^>]*\bVersion\s*=\s*"\s*[\[(]/i.test(text)) {
      respond('deny', `Tam sürüm veya aralıklı paket sürümü (${path.basename(filePath)}). DECISIONS.md Y-22 / R-009: "AVOID NuGet package references that demand an exact version / with a version upper limit". Minimum sürüm kullan (ör. Version="10.0.12").`);
    }
  }
  process.exit(0);
}

if (tool === 'Bash' || tool === 'PowerShell') {
  const cmd = String(ti.command || '');
  checkPackages(packagesFromCommand(cmd), 'komut');

  // Kabuk ile korunan dosyaya yazma girişimi (sed -i, >, rm, mv, Set-Content...).
  const writesSomething = /(\s>>?\s*|\bsed\s+-i\b|\btee\b|\brm\b|\bmv\b|\bcp\b|\btruncate\b|Set-Content|Add-Content|Out-File|Remove-Item|Move-Item|Copy-Item|New-Item)/i.test(cmd);
  if (writesSomething) {
    const lc = cmd.replace(/\\/g, '/').toLowerCase();
    const hit = protectedPaths.find(p => lc.includes(p.raw.toLowerCase().replace(/\/$/, '')));
    if (hit) {
      respond('ask', `Komut korunan bir yola yazıyor olabilir: ${hit.raw}. Korunan dosyalar yalnızca kullanıcı onayıyla değişir (DECISIONS D-005, Y-11).`);
    }
  }
  process.exit(0);
}

process.exit(0);
