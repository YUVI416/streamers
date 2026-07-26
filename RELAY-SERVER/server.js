// LUCAS CHEATS - WebSocket Relay Server
// Deploy on Render.com (free) via GitHub
// EXE and Web both connect here using session code - NO SAME WIFI NEEDED

const WebSocket = require('ws');
const http = require('http');

const PORT = process.env.PORT || 3000;

// Active sessions: code -> { exe: ws, web: ws, createdAt }
const sessions = {};

// Cleanup expired sessions every 60 seconds (2 hour expiry)
setInterval(() => {
  const now = Date.now();
  for (const code in sessions) {
    if (now - sessions[code].createdAt > 7200000) {
      if (sessions[code].exe) sessions[code].exe.close();
      if (sessions[code].web) sessions[code].web.close();
      delete sessions[code];
      console.log(`[CLEANUP] Session ${code} expired`);
    }
  }
}, 60000);

// HTTP server (required by Render to bind port)
const httpServer = http.createServer((req, res) => {
  // Health check endpoint - keeps server alive
  if (req.url === '/ping') {
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('pong');
    return;
  }
  res.writeHead(200, { 'Content-Type': 'text/html' });
  res.end(`
    <!DOCTYPE html>
    <html>
    <head><title>LUCAS CHEATS RELAY</title>
    <style>
      body { background:#0a0a0a; color:#fff; font-family:monospace; display:flex; 
             align-items:center; justify-content:center; height:100vh; margin:0; }
      .box { text-align:center; }
      h1 { color:#ff0000; font-size:2rem; text-shadow:0 0 20px #ff0000; }
      p  { color:#ffffff44; }
    </style></head>
    <body>
      <div class="box">
        <h1>LUCAS CHEATS</h1>
        <p>Relay Server Online</p>
        <p style="color:#00ff8844; margin-top:10px;">● Active Sessions: ${Object.keys(sessions).length}</p>
      </div>
    </body>
    </html>
  `);
});

const wss = new WebSocket.Server({ server: httpServer });

wss.on('connection', (ws) => {
  ws.sessionCode = null;
  ws.role = null; // 'exe' or 'web'

  ws.on('message', (raw) => {
    try {
      const msg = JSON.parse(raw.toString());

      // === EXE registers with session code ===
      if (msg.type === 'register_exe') {
        const code = msg.code;
        if (!sessions[code]) {
          sessions[code] = { exe: null, web: null, createdAt: Date.now() };
        }
        sessions[code].exe = ws;
        ws.sessionCode = code;
        ws.role = 'exe';
        console.log(`[EXE] Registered: ${code}`);
        ws.send(JSON.stringify({ type: 'registered', code }));

        // If web already waiting, connect both
        if (sessions[code].web) {
          sessions[code].web.send(JSON.stringify({ type: 'connected', message: 'EXE Connected' }));
          ws.send(JSON.stringify({ type: 'web_connected' }));
        }
        return;
      }

      // === WEB joins with session code ===
      if (msg.type === 'join_web') {
        const code = msg.code;
        if (!sessions[code] || !sessions[code].exe) {
          ws.send(JSON.stringify({ type: 'error', message: 'Invalid code or EXE not running' }));
          return;
        }
        sessions[code].web = ws;
        ws.sessionCode = code;
        ws.role = 'web';
        console.log(`[WEB] Joined: ${code}`);

        // Notify EXE that web connected
        sessions[code].exe.send(JSON.stringify({ type: 'web_connected' }));
        ws.send(JSON.stringify({ type: 'connected', message: 'Connected to EXE' }));
        return;
      }

      // === WEB → EXE command ===
      if (msg.type === 'command' && ws.role === 'web') {
        const code = ws.sessionCode;
        if (sessions[code] && sessions[code].exe) {
          sessions[code].exe.send(JSON.stringify({ type: 'command', action: msg.action }));
        }
        return;
      }

      // === EXE → WEB response ===
      if (msg.type === 'response' && ws.role === 'exe') {
        const code = ws.sessionCode;
        if (sessions[code] && sessions[code].web) {
          sessions[code].web.send(JSON.stringify({ type: 'response', text: msg.text }));
        }
        return;
      }

    } catch (e) {
      console.error('Parse error:', e.message);
    }
  });

  ws.on('close', () => {
    const code = ws.sessionCode;
    if (!code || !sessions[code]) return;

    if (ws.role === 'exe') {
      console.log(`[EXE] Disconnected: ${code}`);
      if (sessions[code].web) {
        sessions[code].web.send(JSON.stringify({ type: 'error', message: 'EXE Disconnected' }));
      }
      delete sessions[code];
    } else if (ws.role === 'web') {
      console.log(`[WEB] Disconnected: ${code}`);
      sessions[code].web = null;
    }
  });

  ws.on('error', () => {});
});

httpServer.listen(PORT, () => {
  console.log(`LUCAS CHEATS Relay Server running on port ${PORT}`);
});
