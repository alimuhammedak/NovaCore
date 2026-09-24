#!/usr/bin/env node
// PreToolUse guard (Edit|Write|Bash|PowerShell).
// - Yasaklı NuGet paketlerini reddeder (deny).
// - Allowlist dışındaki yeni paketler için kullanıcı onayı ister (ask).
// - Korunan dosyaların (PLAN, DECISIONS, hook'lar, settings) değiştirilmesi için kullanıcı onayı ister (ask).
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
    respond('ask', `Onaylanmamış yeni paket: ${unknown.join(', ')} (${where}). DECISIONS D-003/Y-06: çekirdek yalnızca Microsoft/BCL; üçüncü taraf paket yalnızca eklenti paketlerinde ve MIT/Apache lisanslı olabilir. Onaylarsan paketi .claude/hooks/package-policy.json allowlist'ine de ekle ve DECISIONS'a D-kaydı yaz.`);
  }
}

const projectFile = f => /\.(csproj|props|targets|fsproj|vbproj)$/i.test(f || '');

if (tool === 'Edit' || tool === 'Write' || tool === 'MultiEdit' || tool === 'NotebookEdit') {
  const filePath = ti.file_path || ti.notebook_path || '';
  const prot = protectedMatch(filePath);
  if (prot) {
    respond('ask', `Korunan dosya değişikliği: ${prot.raw}. Bu dosya yalnızca kullanıcı onayıyla değişir (DECISIONS D-005, Y-11). Değişikliğin gerekçesini kullanıcıya açıkla.`);
  }
  if (projectFile(filePath)) {
    const text = [ti.new_string, ti.content, ...(Array.isArray(ti.edits) ? ti.edits.map(e => e.new_string) : [])]
      .filter(Boolean).join('\n');
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
