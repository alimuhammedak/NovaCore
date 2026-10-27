#!/usr/bin/env node
// SessionStart hook: oturumun başladığı kaynak durumunu kaydeder; Stop hook'u yalnız bu oturumun değiştirdiğini
// doğrular (D-088). stdout'a hiçbir şey yazmaz: SessionStart çıktısı Claude'un bağlamına eklenir.
'use strict';

const fs = require('fs');
const path = require('path');
const { sourceState, baselineFile, sessionsDir } = require('./source-state');

const projectDir = process.env.CLAUDE_PROJECT_DIR || process.cwd();
const RETENTION_MS = 14 * 24 * 60 * 60 * 1000;

let input = {};
try {
  input = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
} catch { /* girdi yoksa oturum kimliği de yok: Stop hook'u HEAD'i esas alır */ }

const file = baselineFile(input.session_id);
// Sürdürülen ya da özetlenen oturum başladığı durumu korur: daha önce değiştirdiği de kendi değişikliğidir.
if (!file || fs.existsSync(file)) process.exit(0);

try {
  fs.mkdirSync(sessionsDir, { recursive: true });
  fs.writeFileSync(file, sourceState(projectDir).state + '\n');
} catch {
  process.exit(0); // Stop hook'u HEAD'i esas alır
}

// Sıfırdan farklı çıkış kodu kullanıcıya hata notu gösterir: eski kayıtları temizlemek bunu hak etmez.
try {
  const now = Date.now();
  for (const name of fs.readdirSync(sessionsDir)) {
    const old = path.join(sessionsDir, name);
    if (now - fs.statSync(old).mtimeMs > RETENTION_MS) fs.unlinkSync(old);
  }
} catch { /* başka bir oturum aynı anda temizledi */ }
