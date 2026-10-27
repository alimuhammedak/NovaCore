#!/usr/bin/env node
// Stop hook: bu oturum build'in okuduğu dosyaları değiştirdiyse `dotnet build` + `dotnet test` çalıştırır.
// Kırmızıysa (exit 2) durmayı engeller ve hatayı modele geri verir. Oturum o dosyalara dokunmadıysa hiçbir şey
// çalıştırmaz ve hiçbir şey yazmaz: değişiklik dosya zamanından değil git'ten okunur (source-state.js, D-088).
// Oturumun başlangıç durumu .claude/hooks/.sessions/<session_id>'de (SessionStart hook'u yazar), son kontrolün durumu
// ve sonucu .claude/hooks/.last-verified'da; ikisi de git'e girmez.
'use strict';

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');
const { sourceState, baselineFile } = require('./source-state');

const projectDir = process.env.CLAUDE_PROJECT_DIR || process.cwd();
const stampFile = path.join(__dirname, '.last-verified');
const solution = 'NovaCore.slnx';

let input = {};
try {
  input = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
} catch { /* girdi olmadan da çalışır */ }

let current;
try {
  current = sourceState(projectDir);
} catch (e) {
  process.stdout.write(JSON.stringify({ systemMessage: `⚠ NovaCore Stop hook: git okunamadı (${e.message}); doğrulama yapılmadı.` }));
  process.exit(0);
}

// Başlangıç durumu yoksa (hook oturum açıkken eklendi) HEAD sayılır: commit'lenmemiş her değişiklik yine yakalanır.
let baseline = current.committed;
const baselinePath = baselineFile(input.session_id);
if (baselinePath) {
  try {
    baseline = fs.readFileSync(baselinePath, 'utf8').trim() || baseline;
  } catch { /* başlangıç durumu yok */ }
}

// Bu oturum build'in okuduğu bir dosyayı değiştirmedi (ya da değişikliğini geri aldı): doğrulanacak bir şey yok.
if (current.state === baseline) process.exit(0);

// Damga son *kontrolü* tutar, sonucu ne olursa olsun: kırmızı bir ağaçta her sohbet turu build + test'i baştan
// çalıştırmasın. Durum o kontrolden beri değişmediyse yeniden çalıştırmaz, kırmızıysa uyarır.
let last = {};
try {
  last = JSON.parse(fs.readFileSync(stampFile, 'utf8'));
} catch { /* ilk çalışma ya da eski biçim */ }

if (last.state === current.state) {
  if (last.result === 'red') {
    process.stdout.write(JSON.stringify({
      systemMessage: '⚠ NovaCore: son doğrulama kırmızıydı ve o zamandan beri kaynak dosya değişmedi; tekrar çalıştırılmadı.'
    }));
  }
  process.exit(0);
}

function record(result) {
  fs.writeFileSync(stampFile, JSON.stringify({ state: current.state, result, at: new Date().toISOString() }) + '\n');
}

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
  record('red');
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
  // Aynı durum için bir kez uyarır: dotnet'siz bir ortamda (ör. bulut oturumu) her tur aynı mesajı yazmasın.
  record('skipped');
  process.stdout.write(JSON.stringify({
    systemMessage: `⚠ NovaCore Stop hook: dotnet çalıştırılamadı (${build.error.message}); bu oturumun değişiklikleri doğrulanmadı.`
  }));
  process.exit(0);
}
if (build.status !== 0) fail('build', build);

// global.json runs `dotnet test` in Microsoft.Testing.Platform mode: the solution goes through --solution.
const test = run(['test', '--solution', solution, '--no-build']);
if (test.status !== 0) fail('test', test);

record('green');
process.exit(0);
