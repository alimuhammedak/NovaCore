#!/usr/bin/env node
// Stop hook: kaynak dosyalar son başarılı doğrulamadan beri değiştiyse `dotnet build` + `dotnet test` çalıştırır.
// Kırmızıysa (exit 2) durmayı engeller ve hatayı modele geri verir. Değişiklik yoksa hiçbir şey çalıştırmaz.
// Başarılı doğrulamanın zaman damgası: .claude/hooks/.last-verified (git'e girmez).
'use strict';

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const projectDir = process.env.CLAUDE_PROJECT_DIR || process.cwd();
const stampFile = path.join(__dirname, '.last-verified');
const solution = 'NovaCore.slnx';

let input = {};
try {
  input = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
} catch { /* girdi olmadan da çalışır */ }

const SKIP_DIRS = new Set(['bin', 'obj', '.vs', '.git', 'node_modules', 'TestResults', 'artifacts']);
// .txt: PublicAPI.*.txt; a public API change without them fails the build (RS0016/RS0017).
const WATCHED_EXT = /\.(cs|csproj|props|targets|json|slnx|editorconfig|txt)$/i;

function newestMtime(dir) {
  let newest = 0;
  let entries;
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return 0;
  }
  for (const e of entries) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (!SKIP_DIRS.has(e.name)) newest = Math.max(newest, newestMtime(full));
    } else if (WATCHED_EXT.test(e.name)) {
      try { newest = Math.max(newest, fs.statSync(full).mtimeMs); } catch { /* yok say */ }
    }
  }
  return newest;
}

function rootFilesMtime() {
  const names = ['Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'NovaCore.slnx', 'global.json', '.editorconfig'];
  return Math.max(0, ...names.map(n => {
    try { return fs.statSync(path.join(projectDir, n)).mtimeMs; } catch { return 0; }
  }));
}

const newest = Math.max(
  newestMtime(path.join(projectDir, 'src')),
  newestMtime(path.join(projectDir, 'tests')),
  rootFilesMtime()
);

let lastVerified = 0;
try { lastVerified = fs.statSync(stampFile).mtimeMs; } catch { /* ilk çalışma */ }

if (newest <= lastVerified) process.exit(0);

function run(args) {
  return spawnSync('dotnet', args, {
    cwd: projectDir,
    encoding: 'utf8',
    maxBuffer: 64 * 1024 * 1024,
    windowsHide: true
  });
}

function summarize(res) {
  const text = `${res.stdout || ''}\n${res.stderr || ''}`;
  const lines = text.split(/\r?\n/);
  const important = lines.filter(l => /\berror\b|\bFailed\b|\bfailed\b|Assert\.|Exception/.test(l)).slice(0, 40);
  const tail = lines.filter(l => l.trim()).slice(-15);
  return [...new Set([...important, ...tail])].join('\n').slice(0, 6000);
}

function fail(stage, res) {
  const msg = `Doğrulama başarısız (${stage}). Kaynak dosyalar değişti ama ${stage} kırmızı:\n${summarize(res)}`;
  if (input.stop_hook_active) {
    // Sonsuz döngüyü önle: ikinci denemede durmaya izin ver ama kullanıcıyı uyar.
    process.stdout.write(JSON.stringify({
      systemMessage: `⚠ NovaCore: ${stage} hâlâ kırmızı. Model durdu ama doğrulama geçmedi; raporu kontrol et.`
    }));
    process.exit(0);
  }
  process.stderr.write(msg + '\n\nBuild ve testleri yeşile döndürmeden durma. Düzeltme kapsam dışına taşıyorsa novacore-phase §4 sapma protokolünü uygula ve kullanıcıya sor.');
  process.exit(2);
}

const build = run(['build', solution, '-nologo', '-v', 'q']);
if (build.error) {
  process.stdout.write(JSON.stringify({ systemMessage: `⚠ NovaCore Stop hook: dotnet çalıştırılamadı (${build.error.message}).` }));
  process.exit(0);
}
if (build.status !== 0) fail('build', build);

const test = run(['test', solution, '--no-build', '-nologo']);
if (test.status !== 0) fail('test', test);

fs.writeFileSync(stampFile, new Date().toISOString() + '\n');
process.exit(0);
