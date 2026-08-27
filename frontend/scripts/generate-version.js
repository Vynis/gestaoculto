const fs = require('fs');
const path = require('path');
const childProcess = require('child_process');

function readText(filePath) {
  try {
    return fs.readFileSync(filePath, 'utf8').trim();
  } catch {
    return '';
  }
}

function readCommit(repoRoot) {
  try {
    return childProcess.execSync('git rev-parse --short HEAD', {
      cwd: repoRoot,
      stdio: ['ignore', 'pipe', 'ignore']
    }).toString().trim();
  } catch {
    return 'unknown';
  }
}

const frontendRoot = path.resolve(__dirname, '..');
const projectRoot = path.resolve(frontendRoot, '..');
const versionFilePath = path.resolve(projectRoot, 'VERSION');
const outputPath = path.resolve(frontendRoot, 'src', 'assets', 'version.json');

const version = readText(versionFilePath) || '0.0.0';
const commit = readCommit(projectRoot);
const buildDate = new Date().toISOString();

const payload = {
  name: 'gestaoculto-web',
  version,
  commit,
  buildDate,
  environment: process.env['NODE_ENV'] || 'development'
};

fs.writeFileSync(outputPath, `${JSON.stringify(payload, null, 2)}\n`, 'utf8');
