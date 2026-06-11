// One-off direct WebSocket caller for the Unity-side mcp-unity server.
// Used when the Node MCP bridge process has died but Unity's listener (8091) is alive.
// Usage: node tools/ws-unity-call.cjs <method> '<json-params>' [timeoutMs]
const WebSocket = require('D:/BlackCommission/Packages/com.gamelovers.mcp-unity/Server~/node_modules/ws');

const method = process.argv[2];
const params = JSON.parse(process.argv[3] || '{}');
const timeoutMs = parseInt(process.argv[4] || '120000', 10);

const ws = new WebSocket('ws://localhost:8091/McpUnity');
const req = { id: 'cli-' + Date.now(), method, params };

const timer = setTimeout(() => { console.error('TIMEOUT after ' + timeoutMs + 'ms'); process.exit(2); }, timeoutMs);

ws.on('open', () => ws.send(JSON.stringify(req)));
ws.on('message', (d) => {
  clearTimeout(timer);
  console.log(d.toString());
  ws.close();
  process.exit(0);
});
ws.on('error', (e) => { console.error('WS ERROR: ' + e.message); process.exit(1); });
