#!/usr/bin/env node
/**
 * Claude Code Token Setup Script
 * Automates token acquisition through tokens-ui service with Okta SSO
 *
 * Usage:
 *   node setup-claude.js           # Install and configure
 *   node setup-claude.js --cleanup # Remove Mercury configuration
 */

// Check Node.js version early
if (!process.versions || !process.versions.node) {
  console.error('❌ Node.js is required but not detected');
  const platform = require('os').platform();
  if (platform === 'darwin') {
    console.error('Install with: brew install node');
    console.error('Or download from: https://nodejs.org/');
  } else if (platform === 'win32') {
    console.error('Download from: https://nodejs.org/');
    console.error('Or use winget: winget install OpenJS.NodeJS');
  } else {
    console.error('Install with your package manager or download from: https://nodejs.org/');
  }
  process.exit(1);
}

const nodeVersion = process.versions.node.split('.').map(Number);
if (nodeVersion[0] < 14) {
  console.error(`❌ Node.js ${process.versions.node} detected, but v14+ is required`);
  const platform = require('os').platform();
  if (platform === 'darwin') {
    console.error('Update with: brew upgrade node');
    console.error('Or download from: https://nodejs.org/');
  } else if (platform === 'win32') {
    console.error('Download latest from: https://nodejs.org/');
    console.error('Or use winget: winget upgrade OpenJS.NodeJS');
  } else {
    console.error('Update with your package manager or download from: https://nodejs.org/');
  }
  process.exit(1);
}

const fs = require('fs');
const path = require('path');
const { spawn, execSync } = require('child_process');
const http = require('http');
const url = require('url');
const os = require('os');
const readline = require('readline');

// Configuration - this will be dynamically replaced by the API
const TOKENS_UI_URL = 'https://mercury.weather.com/tokens';
const CALLBACK_PORT = 8083;

// Parse command line arguments
const args = process.argv.slice(2);
const isUninstall = args.includes('--uninstall') || args.includes('--cleanup');

// Check if Claude Code is installed
function checkClaudeCodeInstalled() {
  try {
    execSync('claude --version', { stdio: 'pipe' });
    return true;
  } catch (error) {
    return false;
  }
}

// Check if Homebrew is installed (macOS)
function checkBrewInstalled() {
  try {
    execSync('brew --version', { stdio: 'pipe' });
    return true;
  } catch (error) {
    return false;
  }
}

// Prompt user for yes/no answer
function promptYesNo(question) {
  return new Promise((resolve) => {
    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout
    });

    rl.question(`${question} (y/n): `, (answer) => {
      rl.close();
      resolve(answer.toLowerCase().trim() === 'y' || answer.toLowerCase().trim() === 'yes');
    });
  });
}

// Request sudo access upfront
function requestSudoAccess() {
  console.log('🔐 Requesting administrator access...');
  console.log('Please enter your password when prompted:');
  console.log('');
  try {
    execSync('sudo -v', { stdio: 'inherit' });
    console.log('✅ Administrator access granted');
    console.log('');
    return true;
  } catch (error) {
    console.error('❌ Failed to obtain administrator access');
    return false;
  }
}

// Keep sudo alive during installation
function keepSudoAlive() {
  // Refresh sudo timestamp every 60 seconds
  const interval = setInterval(() => {
    try {
      execSync('sudo -v', { stdio: 'pipe' });
    } catch (error) {
      clearInterval(interval);
    }
  }, 60000);

  return interval;
}

// Install Homebrew on macOS
function installHomebrew() {
  console.log('🍺 Installing Homebrew...');
  try {
    execSync('/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"', {
      stdio: 'inherit'
    });
    console.log('✅ Homebrew installed successfully');
    return true;
  } catch (error) {
    console.error('❌ Failed to install Homebrew:', error.message);
    return false;
  }
}

// Install Claude Code
function installClaudeCode() {
  const platform = os.platform();
  console.log('📦 Installing Claude Code...');

  try {
    if (platform === 'darwin' || platform === 'linux') {
      execSync('curl -fsSL https://claude.ai/install.sh | bash', {
        stdio: 'inherit',
        shell: '/bin/bash'
      });
    } else if (platform === 'win32') {
      // Try PowerShell first
      try {
        execSync('powershell -Command "irm https://claude.ai/install.ps1 | iex"', {
          stdio: 'inherit'
        });
      } catch {
        // Fallback to CMD method
        execSync('curl -fsSL https://claude.ai/install.cmd -o install.cmd && install.cmd && del install.cmd', {
          stdio: 'inherit'
        });
      }
    }
    console.log('✅ Claude Code installed successfully');
    return true;
  } catch (error) {
    console.error('❌ Failed to install Claude Code:', error.message);
    return false;
  }
}

function createClaudeConfigDir() {
  const claudeDir = path.join(os.homedir(), '.claude');
  if (!fs.existsSync(claudeDir)) {
    fs.mkdirSync(claudeDir, { recursive: true });
  }
  return claudeDir;
}

function openBrowser(url) {
  const platform = os.platform();
  const command = platform === 'darwin' ? 'open' : 
                 platform === 'win32' ? 'start' : 'xdg-open';
  
  console.log(`🌐 Opening browser for authentication...`);
  console.log(`If browser doesn't open, visit: ${url}`);
  
  spawn(command, [url], { detached: true, stdio: 'ignore' });
}

function startCallbackServer() {
  return new Promise((resolve, reject) => {
    const server = http.createServer((req, res) => {
      const parsedUrl = url.parse(req.url, true);
      
      if (parsedUrl.pathname === '/callback' && parsedUrl.query.token) {
        const token = parsedUrl.query.token;
        
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(`
          <html><head><meta charset="utf-8"></head><body style="font-family: -apple-system, sans-serif; text-align: center; margin-top: 50px;">
            <h2 style="color: #4CAF50;">✅ Success!</h2>
            <p>Token received and saved to ~/.claude/settings.json</p>
            <p style="color: #666;">You can close this window and return to the terminal.</p>
          </body></html>
        `);
        
        server.close();
        resolve(token);
      } else {
        res.writeHead(400, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end('<html><head><meta charset="utf-8"></head><body><h2>❌ Error</h2><p>No token received</p></body></html>');
        server.close();
        reject(new Error('No token received'));
      }
    });
    
    server.listen(CALLBACK_PORT, 'localhost', () => {
      console.log(`📡 Callback server listening on port ${CALLBACK_PORT}`);
    });
    
    server.on('error', reject);
    
    // Timeout after 5 minutes
    setTimeout(() => {
      server.close();
      reject(new Error('Authentication timed out'));
    }, 300000);
  });
}

function saveTokenToConfig(token) {
  const claudeDir = createClaudeConfigDir();
  const configFile = path.join(claudeDir, 'settings.json');

  const config = {
    env: {
      ANTHROPIC_AUTH_TOKEN: token,
      ANTHROPIC_BASE_URL: "https://api.mercury.weather.com/litellm",
    }
  };

  fs.writeFileSync(configFile, JSON.stringify(config, null, 2));
  console.log(`✅ Token saved to ${configFile}`);

  return config.env;
}

function setupShellEnvironment(envVars) {
  const homeDir = os.homedir();
  const platform = os.platform();
  const shell = process.env.SHELL || '';

  let rcFile = null;
  let envComment = '# Mercury Claude Code environment variables';
  let exportSyntax = 'unix'; // 'unix' or 'powershell'

  // Determine which shell/profile to use
  if (platform === 'win32') {
    // Windows PowerShell
    // Try PowerShell Core first, then Windows PowerShell
    const psCore = path.join(homeDir, 'Documents', 'PowerShell', 'Microsoft.PowerShell_profile.ps1');
    const ps5 = path.join(homeDir, 'Documents', 'WindowsPowerShell', 'Microsoft.PowerShell_profile.ps1');

    if (fs.existsSync(path.dirname(psCore))) {
      rcFile = psCore;
    } else if (fs.existsSync(path.dirname(ps5))) {
      rcFile = ps5;
    } else {
      // Create PowerShell profile directory and use PS5 by default
      const ps5Dir = path.dirname(ps5);
      try {
        fs.mkdirSync(ps5Dir, { recursive: true });
        rcFile = ps5;
      } catch (error) {
        return { success: false, message: 'Could not create PowerShell profile directory' };
      }
    }
    exportSyntax = 'powershell';
  } else {
    // Unix-like systems (macOS, Linux)
    if (shell.includes('zsh')) {
      rcFile = path.join(homeDir, '.zshrc');
    } else if (shell.includes('bash')) {
      rcFile = path.join(homeDir, '.bashrc');
    }
  }

  if (!rcFile) {
    return { success: false, message: 'Could not detect shell type' };
  }

  // Read existing content if file exists
  let rcContent = '';
  if (fs.existsSync(rcFile)) {
    rcContent = fs.readFileSync(rcFile, 'utf-8');
  }

  // If already configured, skip
  if (rcContent.includes(envComment)) {
    return { success: true, message: 'Shell already configured', alreadyConfigured: true };
  }

  // Append environment variables with appropriate syntax
  let envLines;
  if (exportSyntax === 'powershell') {
    envLines = [
      '',
      envComment,
      `$env:ANTHROPIC_AUTH_TOKEN = "${envVars.ANTHROPIC_AUTH_TOKEN}"`,
      `$env:ANTHROPIC_BASE_URL = "${envVars.ANTHROPIC_BASE_URL}"`,
      ''
    ].join('\r\n'); // Windows line endings
  } else {
    envLines = [
      '',
      envComment,
      `export ANTHROPIC_AUTH_TOKEN="${envVars.ANTHROPIC_AUTH_TOKEN}"`,
      `export ANTHROPIC_BASE_URL="${envVars.ANTHROPIC_BASE_URL}"`,
      ''
    ].join('\n');
  }

  fs.appendFileSync(rcFile, envLines);

  return {
    success: true,
    message: `Environment variables added to ${rcFile}`,
    rcFile,
    needsReload: true,
    platform: platform === 'win32' ? 'windows' : 'unix'
  };
}

const MERCURY_BASE_URLS = [
  'https://api.mercury.weather.com/litellm',
  'https://api.mercury.weather.com',
];

function isMercuryBaseUrl(value) {
  if (!value) return false;
  const normalized = value.replace(/\/+$/, '').toLowerCase();
  return MERCURY_BASE_URLS.some(u => normalized === u.toLowerCase());
}

function findLocalSettingsConflicts() {
  const homeDir = os.homedir();
  const conflicts = [];

  try {
    // Search CWD and common project directories for .claude/settings.local.json with Mercury env vars
    const searchDirs = [
      path.join(homeDir, 'Documents'),
      path.join(homeDir, 'Projects'),
      path.join(homeDir, 'Developer'),
      path.join(homeDir, 'repos'),
      path.join(homeDir, 'src'),
      path.join(homeDir, 'code'),
      path.join(homeDir, 'workspace'),
      path.join(homeDir, 'work'),
      path.join(homeDir, 'Desktop'),
    ];

    // Check CWD directly (may live outside the common roots)
    const cwdSettings = path.join(process.cwd(), '.claude', 'settings.local.json');
    if (fs.existsSync(cwdSettings)) {
      try {
        const content = JSON.parse(fs.readFileSync(cwdSettings, 'utf-8'));
        if (content.env && (content.env.ANTHROPIC_AUTH_TOKEN || isMercuryBaseUrl(content.env.ANTHROPIC_BASE_URL))) {
          conflicts.push(cwdSettings);
        }
      } catch (e) {
        // Skip files that can't be parsed
      }
    }

    function scanDir(dir, depth) {
      if (depth > 4) return;
      try {
        const entries = fs.readdirSync(dir, { withFileTypes: true });
        for (const entry of entries) {
          if (!entry.isDirectory()) continue;
          if (entry.name.startsWith('.') && entry.name !== '.claude') continue;
          if (entry.name === 'node_modules' || entry.name === '.git' || entry.name === '.venv') continue;

          const fullPath = path.join(dir, entry.name);
          if (entry.name === '.claude') {
            const localSettings = path.join(fullPath, 'settings.local.json');
            if (fs.existsSync(localSettings)) {
              try {
                const content = JSON.parse(fs.readFileSync(localSettings, 'utf-8'));
                if (content.env && (content.env.ANTHROPIC_AUTH_TOKEN || isMercuryBaseUrl(content.env.ANTHROPIC_BASE_URL))) {
                  conflicts.push(localSettings);
                }
              } catch (e) {
                // Skip files that can't be parsed
              }
            }
          } else {
            scanDir(fullPath, depth + 1);
          }
        }
      } catch (e) {
        // Skip directories we can't read
      }
    }

    for (const dir of searchDirs) {
      if (fs.existsSync(dir)) {
        scanDir(dir, 0);
      }
    }
  } catch (e) {
    // Silently skip if scan fails
  }

  return conflicts;
}

function cleanLocalSettingsConflicts(files) {
  const results = [];
  for (const file of files) {
    try {
      const config = JSON.parse(fs.readFileSync(file, 'utf-8'));
      if (config.env) {
        delete config.env.ANTHROPIC_AUTH_TOKEN;
        if (isMercuryBaseUrl(config.env.ANTHROPIC_BASE_URL)) {
          delete config.env.ANTHROPIC_BASE_URL;
        }
        if (Object.keys(config.env).length === 0) {
          delete config.env;
        }
      }
      if (Object.keys(config).length === 0) {
        fs.unlinkSync(file);
        results.push({ file, deleted: true });
      } else {
        fs.writeFileSync(file, JSON.stringify(config, null, 2));
        results.push({ file, cleaned: true });
      }
    } catch (e) {
      results.push({ file, error: e.message });
    }
  }
  return results;
}

function cleanupShellEnvironment() {
  const homeDir = os.homedir();
  const platform = os.platform();
  const shell = process.env.SHELL || '';
  const results = [];

  // Determine which shell rc files to clean
  const rcFiles = [];

  if (platform === 'win32') {
    // Windows PowerShell profiles
    rcFiles.push(path.join(homeDir, 'Documents', 'PowerShell', 'Microsoft.PowerShell_profile.ps1'));
    rcFiles.push(path.join(homeDir, 'Documents', 'WindowsPowerShell', 'Microsoft.PowerShell_profile.ps1'));
  } else {
    // Unix-like systems
    if (shell.includes('zsh')) {
      rcFiles.push(path.join(homeDir, '.zshrc'));
    } else if (shell.includes('bash')) {
      rcFiles.push(path.join(homeDir, '.bashrc'));
    } else {
      // Try both if we can't detect
      rcFiles.push(path.join(homeDir, '.zshrc'));
      rcFiles.push(path.join(homeDir, '.bashrc'));
    }
  }

  const envComment = '# Mercury Claude Code environment variables';

  for (const rcFile of rcFiles) {
    if (!fs.existsSync(rcFile)) {
      continue;
    }

    let content = fs.readFileSync(rcFile, 'utf-8');

    if (!content.includes(envComment)) {
      continue;
    }

    // Remove Mercury Claude Code section
    // Handle both Unix export syntax and PowerShell $env: syntax
    let mercurySection;
    if (platform === 'win32') {
      // PowerShell syntax with Windows line endings
      mercurySection = new RegExp(
        `\\r?\\n?${envComment}\\r?\\n` +
        `\\$env:ANTHROPIC_AUTH_TOKEN = "[^"]*"\\r?\\n` +
        `\\$env:ANTHROPIC_BASE_URL = "[^"]*"\\r?\\n?`,
        'g'
      );
    } else {
      // Unix export syntax
      mercurySection = new RegExp(
        `\\n?${envComment}\\n` +
        `export ANTHROPIC_AUTH_TOKEN="[^"]*"\\n` +
        `export ANTHROPIC_BASE_URL="[^"]*"\\n?`,
        'g'
      );
    }

    const newContent = content.replace(mercurySection, '');

    if (newContent !== content) {
      fs.writeFileSync(rcFile, newContent);
      results.push({ file: rcFile, cleaned: true });
    }
  }

  return results;
}

function cleanupSettings() {
  const claudeDir = path.join(os.homedir(), '.claude');
  const configFile = path.join(claudeDir, 'settings.json');

  if (!fs.existsSync(configFile)) {
    return { exists: false };
  }

  try {
    const config = JSON.parse(fs.readFileSync(configFile, 'utf-8'));

    // Remove Mercury-specific env vars
    if (config.env) {
      delete config.env.ANTHROPIC_AUTH_TOKEN;
      delete config.env.ANTHROPIC_BASE_URL;

      // If env object is now empty, remove it
      if (Object.keys(config.env).length === 0) {
        delete config.env;
      }
    }

    // If config is now empty, delete the file
    if (Object.keys(config).length === 0) {
      fs.unlinkSync(configFile);
      return { exists: true, deleted: true };
    } else {
      fs.writeFileSync(configFile, JSON.stringify(config, null, 2));
      return { exists: true, cleaned: true };
    }
  } catch (error) {
    return { exists: true, error: error.message };
  }
}

async function uninstall() {
  console.log('🧹 Cleaning up Mercury Claude Code configuration');
  console.log('='.repeat(50));

  // Clean shell environment
  console.log('\n🐚 Cleaning shell configuration...');
  const shellResults = cleanupShellEnvironment();

  if (shellResults.length === 0) {
    console.log('   No Mercury environment variables found in shell profiles');
  } else {
    for (const result of shellResults) {
      console.log(`   ✅ Removed Mercury variables from ${result.file}`);
    }

    if (os.platform() === 'win32') {
      console.log('   ⚠️  Restart PowerShell or run:');
      console.log('     Remove-Item Env:\\ANTHROPIC_AUTH_TOKEN');
      console.log('     Remove-Item Env:\\ANTHROPIC_BASE_URL');
    } else {
      console.log('   ⚠️  Restart your terminal or run:');
      console.log('     unset ANTHROPIC_AUTH_TOKEN ANTHROPIC_BASE_URL');
    }
  }

  // Clean settings.json
  console.log('\n📝 Cleaning settings.json...');
  const settingsResult = cleanupSettings();

  if (!settingsResult.exists) {
    console.log('   No settings.json found');
  } else if (settingsResult.deleted) {
    console.log('   ✅ Deleted empty settings.json');
  } else if (settingsResult.cleaned) {
    console.log('   ✅ Removed Mercury environment variables from settings.json');
    console.log('   (Preserved other settings)');
  } else if (settingsResult.error) {
    console.log(`   ❌ Error cleaning settings.json: ${settingsResult.error}`);
  }

  // Clean project-local settings files
  console.log('\n🔍 Scanning for project-local settings with Mercury env vars...');
  const conflicts = findLocalSettingsConflicts();

  if (conflicts.length === 0) {
    console.log('   No project-local settings found');
  } else {
    console.log(`   Found ${conflicts.length} project-local settings file(s):`);
    for (const file of conflicts) {
      console.log(`   ${file}`);
    }
    const results = cleanLocalSettingsConflicts(conflicts);
    for (const r of results) {
      if (r.deleted) {
        console.log(`   ✅ Deleted empty ${r.file}`);
      } else if (r.cleaned) {
        console.log(`   ✅ Cleaned ${r.file}`);
      } else if (r.error) {
        console.log(`   ❌ Error with ${r.file}: ${r.error}`);
      }
    }
  }

  console.log('\n✅ Cleanup complete!');
  console.log('\nYou can now configure Claude Code with your own Anthropic API key or another provider.');
}

async function main() {
  // Handle uninstall/cleanup mode
  if (isUninstall) {
    await uninstall();
    process.exit(0);
  }

  console.log('🔧 Claude Code Token Setup');
  console.log('='.repeat(30));
  
  // Check if Claude Code is installed
  const claudeCodeInstalled = checkClaudeCodeInstalled();
  const platform = os.platform();

  if (!claudeCodeInstalled) {
    console.log('⚠️  Claude Code not detected');

    // Check for Homebrew on macOS
    if (platform === 'darwin') {
      const brewInstalled = checkBrewInstalled();
      if (!brewInstalled) {
        console.log('⚠️  Homebrew not detected');
        console.log('First, install Homebrew (required for Claude Code):');
        console.log('   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"');
        console.log('');
        console.log('Then install Claude Code:');
        console.log('   curl -fsSL https://claude.ai/install.sh | bash');
      } else {
        console.log('After token setup, install with:');
        console.log('   curl -fsSL https://claude.ai/install.sh | bash');
      }
    } else if (platform === 'linux') {
      console.log('After token setup, install with:');
      console.log('   curl -fsSL https://claude.ai/install.sh | bash');
    } else if (platform === 'win32') {
      console.log('After token setup, install with:');
      console.log('   PowerShell: irm https://claude.ai/install.ps1 | iex');
      console.log('   CMD: curl -fsSL https://claude.ai/install.cmd -o install.cmd && install.cmd && del install.cmd');
    }
    console.log('');
  } else {
    console.log('✅ Claude Code detected');
  }
  
  try {
    // Create config directory
    createClaudeConfigDir();
    
    // Start callback server
    const tokenPromise = startCallbackServer();
    
    // Open browser to tokens-ui Claude setup endpoint with callback URL
    const callbackUrl = `http://localhost:${CALLBACK_PORT}/callback`;
    const authUrl = `https://mercury.weather.com/tokens?page=claude-setup&callback=${encodeURIComponent(callbackUrl)}`;

    openBrowser(authUrl);

    console.log('🔑 Please log in through Okta SSO');
    console.log('💫 Token will be created automatically and saved');

    // Wait for token
    const token = await tokenPromise;

    // Save token to settings.json
    const envVars = saveTokenToConfig(token);

    // Setup persistent shell environment
    console.log('\n🐚 Setting up shell environment...');
    const shellResult = setupShellEnvironment(envVars);

    if (shellResult.success) {
      if (shellResult.alreadyConfigured) {
        console.log('✅ Shell environment already configured');
      } else {
        console.log(`✅ ${shellResult.message}`);
        console.log('⚠️  Run this to apply environment in current terminal:');

        if (shellResult.platform === 'windows') {
          console.log('   PowerShell:');
          console.log(`     $env:ANTHROPIC_AUTH_TOKEN = "${envVars.ANTHROPIC_AUTH_TOKEN}"`);
          console.log(`     $env:ANTHROPIC_BASE_URL = "${envVars.ANTHROPIC_BASE_URL}"`);
          console.log('   Or restart PowerShell');
        } else {
          console.log(`   eval $(jq -r '.env | to_entries | .[] | "export \\(.key)=\\(.value)"' ~/.claude/settings.json)`);
          console.log('   Or restart your terminal');
        }
      }
    } else {
      console.log(`⚠️  ${shellResult.message}`);
      console.log('💡 Manually export environment variables in your current terminal:');

      if (os.platform() === 'win32') {
        console.log('   PowerShell:');
        console.log(`     $env:ANTHROPIC_AUTH_TOKEN = "${envVars.ANTHROPIC_AUTH_TOKEN}"`);
        console.log(`     $env:ANTHROPIC_BASE_URL = "${envVars.ANTHROPIC_BASE_URL}"`);
      } else {
        console.log(`   export ANTHROPIC_AUTH_TOKEN="${envVars.ANTHROPIC_AUTH_TOKEN}"`);
        console.log(`   export ANTHROPIC_BASE_URL="${envVars.ANTHROPIC_BASE_URL}"`);
      }
    }

    // Check for project-local settings files that could override the global config
    console.log('\n🔍 Checking for conflicting local settings...');
    const conflicts = findLocalSettingsConflicts();

    if (conflicts.length > 0) {
      console.log(`⚠️  Found ${conflicts.length} project-local settings file(s) with Mercury env vars:`);
      for (const file of conflicts) {
        console.log(`   ${file}`);
      }
      console.log('   These will override your global config and may cause 400/401 errors in those folders.');

      const shouldClean = await promptYesNo('Remove Mercury env vars from these local files? (files left empty will be deleted)');
      if (shouldClean) {
        const results = cleanLocalSettingsConflicts(conflicts);
        for (const r of results) {
          if (r.deleted) {
            console.log(`   ✅ Deleted empty ${r.file}`);
          } else if (r.cleaned) {
            console.log(`   ✅ Cleaned ${r.file}`);
          } else if (r.error) {
            console.log(`   ❌ Error with ${r.file}: ${r.error}`);
          }
        }
      }
    } else {
      console.log('   No conflicting local settings found');
      console.log('   (Scanned CWD and common project directories — check manually if your projects live elsewhere)');
    }

    console.log('\n✅ Setup complete! Claude Code is now configured.');

    // Auto-install Claude Code if not present
    if (!claudeCodeInstalled) {
      console.log('\n🚀 Claude Code is not installed yet.');

      const shouldInstall = await promptYesNo('Would you like to install Claude Code now?');

      if (shouldInstall) {
        let sudoKeepAlive = null;
        let needsSudo = false;

        // Check if we'll need sudo (macOS/Linux installations typically do)
        if (platform === 'darwin' || platform === 'linux') {
          needsSudo = true;
        }

        // Request sudo access upfront only if needed
        if (needsSudo) {
          console.log('');
          const sudoGranted = requestSudoAccess();
          if (!sudoGranted) {
            console.log('\n❌ Administrator access is required for installation.');
            console.log('Please try again or install manually.');
            process.exit(1);
          }

          // Keep sudo alive during installation
          sudoKeepAlive = keepSudoAlive();
        }

        try {
          // On macOS, check and install Homebrew first if needed
          if (platform === 'darwin') {
            const brewInstalled = checkBrewInstalled();
            if (!brewInstalled) {
              console.log('⚠️  Homebrew is required for Claude Code on macOS');
              const shouldInstallBrew = await promptYesNo('Would you like to install Homebrew first?');

              if (!shouldInstallBrew) {
                console.log('\n📋 To install manually:');
                console.log('   1. Install Homebrew: /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"');
                console.log('   2. Install Claude Code: curl -fsSL https://claude.ai/install.sh | bash');
                if (sudoKeepAlive) clearInterval(sudoKeepAlive);
                process.exit(0);
              }

              console.log('');
              const brewSuccess = installHomebrew();
              if (!brewSuccess) {
                console.log('\n❌ Could not install Homebrew.');
                console.log('Please install manually:');
                console.log('   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"');
                console.log('\nThen install Claude Code:');
                console.log('   curl -fsSL https://claude.ai/install.sh | bash');
                if (sudoKeepAlive) clearInterval(sudoKeepAlive);
                process.exit(1);
              }
              console.log('');
            }
          }

          // Install Claude Code
          const installSuccess = installClaudeCode();

          // Stop keeping sudo alive
          if (sudoKeepAlive) clearInterval(sudoKeepAlive);

          if (installSuccess) {
            console.log('\n✅ All done! Claude Code is ready to use.');
            console.log('🚀 Run: claude');
          } else {
            console.log('\n⚠️  Automatic installation failed. Please install manually:');
            if (platform === 'darwin' || platform === 'linux') {
              console.log('   curl -fsSL https://claude.ai/install.sh | bash');
            } else if (platform === 'win32') {
              console.log('   PowerShell: irm https://claude.ai/install.ps1 | iex');
              console.log('   CMD: curl -fsSL https://claude.ai/install.cmd -o install.cmd && install.cmd && del install.cmd');
            }
          }
        } catch (error) {
          if (sudoKeepAlive) clearInterval(sudoKeepAlive);
          throw error;
        }
      } else {
        console.log('\n📋 To install Claude Code later, run:');
        if (platform === 'darwin' || platform === 'linux') {
          console.log('   curl -fsSL https://claude.ai/install.sh | bash');
        } else if (platform === 'win32') {
          console.log('   PowerShell: irm https://claude.ai/install.ps1 | iex');
          console.log('   CMD: curl -fsSL https://claude.ai/install.cmd -o install.cmd && install.cmd && del install.cmd');
        }
      }
    } else {
      console.log('🚀 You can now use Claude Code in this project.');
      console.log('   Run: claude');
    }

    // Exit cleanly after a brief delay to let user see the message
    setTimeout(() => {
      process.exit(0);
    }, 3000);

  } catch (error) {
    console.error(`❌ Setup failed: ${error.message}`);
    console.log('\n📖 Manual setup:');
    console.log(`1. Visit https://mercury.weather.com/tokens?page=tokens`);
    console.log('2. Log in and create a token');
    console.log('3. Create ~/.claude/settings.json with:');
    console.log('   {"env": {"ANTHROPIC_AUTH_TOKEN": "your-token", "ANTHROPIC_BASE_URL": "https://api.mercury.weather.com/litellm"}}');
    console.log('4. Add to your shell profile (~/.zshrc or ~/.bashrc):');
    console.log('   export ANTHROPIC_AUTH_TOKEN="your-token"');
    console.log('   export ANTHROPIC_BASE_URL="https://api.mercury.weather.com/litellm"');
    console.log('5. Restart your terminal or source your profile');
    process.exit(1);
  }
}

if (require.main === module) {
  main();
}
