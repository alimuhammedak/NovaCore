// SessionStart ve Stop hook'larının ortak parçası: build'in okuduğu dosyaların durumu, dosya zamanından değil git'ten.
// Dosya zamanı dosyayı kimin değiştirdiğini söylemez: yeni klon, `git pull` ve `git checkout` dosyaları o anın zamanıyla
// yazar ve zamana bakan kontrol oturumda kimsenin dokunmadığı kodu build ediyordu (D-088, R-135).
'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { spawnSync } = require('child_process');

const WATCHED_PATHS = [
  'src', 'tests',
  'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'NovaCore.slnx', 'global.json',
  '.editorconfig'
];
// .txt: PublicAPI.*.txt; a public API change without them fails the build (RS0016/RS0017).
const WATCHED_EXT = /\.(cs|csproj|props|targets|json|slnx|editorconfig|txt)$/i;

function git(projectDir, args) {
  const res = spawnSync('git', args, { cwd: projectDir, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, windowsHide: true });
  if (res.error) throw res.error;
  if (res.status !== 0) throw new Error(`git ${args[0]}: ${(res.stderr || '').trim()}`);
  return res.stdout;
}

const entries = text => text.split('\0').filter(Boolean);

// `committed`: izlenen dosyaların HEAD'deki blob'ları. `state`: üstüne çalışma ağacının commit'lenmemiş değişiklikleri.
// Yalnız başka dosyalara (PROGRESS, belgeler) dokunan bir commit ikisini de değiştirmez.
function sourceState(projectDir) {
  const committed = crypto.createHash('sha256');
  for (const line of entries(git(projectDir, ['ls-tree', '-r', '-z', 'HEAD', '--', ...WATCHED_PATHS]))) {
    // "<mode> <type> <object>\t<path>"
    const file = line.slice(line.indexOf('\t') + 1);
    if (WATCHED_EXT.test(file)) committed.update(`${line}\0`);
  }
  const committedDigest = committed.digest('hex');

  const changed = new Set([
    ...entries(git(projectDir, ['diff', '--name-only', '-z', 'HEAD', '--', ...WATCHED_PATHS])),
    ...entries(git(projectDir, ['ls-files', '--others', '--exclude-standard', '-z', '--', ...WATCHED_PATHS]))
  ]);
  const files = [...changed].filter(f => WATCHED_EXT.test(f)).sort();
  if (files.length === 0) return { state: committedDigest, committed: committedDigest };

  const state = crypto.createHash('sha256').update(committedDigest);
  for (const file of files) {
    let content;
    try {
      content = crypto.createHash('sha256').update(fs.readFileSync(path.join(projectDir, file))).digest('hex');
    } catch {
      content = 'missing'; // silinmiş dosya
    }
    state.update(`\0${file}\0${content}`);
  }
  return { state: state.digest('hex'), committed: committedDigest };
}

// Oturum başına bir dosya: aynı anda açık iki oturum aynı dosyaya yazmaz. .last-verified gibi git'e girmez.
const sessionsDir = path.join(__dirname, '.sessions');

function baselineFile(sessionId) {
  const id = String(sessionId || '').replace(/[^A-Za-z0-9_-]/g, '');
  return id ? path.join(sessionsDir, id) : null;
}

module.exports = { sourceState, baselineFile, sessionsDir };
