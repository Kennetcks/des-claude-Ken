#!/usr/bin/env node
// Deswik CAD bridge for the Claude desktop app.
// Speaks MCP (JSON-RPC over stdio) to Claude, and forwards tool calls to the
// "Claude Assistant" plugin running inside Deswik CAD over a local named pipe.
// No dependencies: runs on the Node.js that ships with Claude Desktop.
'use strict';

const net = require('net');
const fs = require('fs');
const path = require('path');

const VERSION = '0.2.0';
const PIPE = process.env.DESWIK_PIPE || '\\\\.\\pipe\\KennetDeswikClaude';
const FALLBACK_TOOLS = JSON.parse(fs.readFileSync(path.join(__dirname, 'tools.json'), 'utf8'));

const INSTRUCTIONS =
  'These tools act on the user\'s live Deswik CAD session (open drawing + Interactive Scheduler schedule) ' +
  'through the Claude Assistant plugin inside Deswik. Start each conversation with get_deswik_guide and get_context. ' +
  'Every change pops up an approval window inside Deswik - tell the user to switch to Deswik to approve. ' +
  'If a tool says Deswik is not reachable, ask the user to open Deswik CAD and load the Claude Assistant plugin.';

const NOT_REACHABLE =
  'Deswik is not reachable. Open Deswik CAD and load the "Kennet.DeswikClaude" plugin ' +
  '(Plugin Manager > Load). The Claude Assistant panel shows "Claude desktop app link is on" when ready.';

const log = (...a) => process.stderr.write('[deswik-bridge] ' + a.join(' ') + '\n');

// ---------------------------------------------------------------- pipe client
function pipeRequest(payload, { timeoutMs = 0, retries = 20 } = {}) {
  return new Promise((resolve, reject) => {
    let attempt = 0;
    const tryOnce = () => {
      attempt++;
      let buf = '';
      let done = false;
      const sock = net.connect(PIPE);
      let timer = null;
      const finish = (err, val) => {
        if (done) return;
        done = true;
        if (timer) clearTimeout(timer);
        sock.destroy();
        err ? reject(err) : resolve(val);
      };
      if (timeoutMs > 0) timer = setTimeout(() => finish(new Error('timeout')), timeoutMs);
      sock.setEncoding('utf8');
      sock.on('connect', () => sock.write(JSON.stringify(payload) + '\n'));
      sock.on('data', (d) => {
        buf += d;
        const nl = buf.indexOf('\n');
        if (nl >= 0) {
          try { finish(null, JSON.parse(buf.slice(0, nl))); }
          catch (e) { finish(e); }
        }
      });
      sock.on('end', () => finish(new Error('Deswik closed the connection without replying')));
      sock.on('error', (e) => {
        if (done) return;
        // All pipe instances busy (EBUSY / EAGAIN): back off and retry.
        if ((e.code === 'EBUSY' || e.code === 'EAGAIN') && attempt < retries) {
          done = true;
          if (timer) clearTimeout(timer);
          sock.destroy();
          setTimeout(tryOnce, 250);
          return;
        }
        finish(e);
      });
    };
    tryOnce();
  });
}

// ---------------------------------------------------------------- MCP handlers
let lastKnownTools = FALLBACK_TOOLS;

async function listTools() {
  try {
    const r = await pipeRequest({ op: 'list' }, { timeoutMs: 3000 });
    if (r && Array.isArray(r.tools) && r.tools.length) lastKnownTools = r.tools;
  } catch (e) {
    log('list from Deswik failed, using built-in list:', e.code || e.message);
  }
  return { tools: lastKnownTools };
}

async function callTool(params) {
  const name = params && params.name;
  const input = (params && params.arguments) || {};
  try {
    const r = await pipeRequest({ op: 'call', name, input }); // no timeout: user may be approving
    return { content: [{ type: 'text', text: String(r.text || '(empty)') }], isError: !!r.isError };
  } catch (e) {
    const unreachable = e.code === 'ENOENT' || e.code === 'ECONNREFUSED' || e.code === 'EBUSY';
    return {
      content: [{ type: 'text', text: unreachable ? NOT_REACHABLE : 'Bridge error: ' + (e.message || e) }],
      isError: true,
    };
  }
}

async function handle(msg) {
  const { id, method, params } = msg;
  const isRequest = id !== undefined && id !== null;
  try {
    let result;
    switch (method) {
      case 'initialize':
        result = {
          protocolVersion: (params && params.protocolVersion) || '2025-06-18',
          capabilities: { tools: { listChanged: true } },
          serverInfo: { name: 'deswik-claude', version: VERSION },
          instructions: INSTRUCTIONS,
        };
        break;
      case 'ping':
        result = {};
        break;
      case 'tools/list':
        result = await listTools();
        break;
      case 'tools/call':
        result = await callTool(params);
        break;
      case 'resources/list':
        result = { resources: [] };
        break;
      case 'prompts/list':
        result = { prompts: [] };
        break;
      default:
        if (!isRequest) return; // notifications (initialized, cancelled, ...) need no reply
        return send({ jsonrpc: '2.0', id, error: { code: -32601, message: 'Method not found: ' + method } });
    }
    if (isRequest) send({ jsonrpc: '2.0', id, result });
  } catch (e) {
    if (isRequest) send({ jsonrpc: '2.0', id, error: { code: -32603, message: String(e.message || e) } });
  }
}

function send(obj) {
  process.stdout.write(JSON.stringify(obj) + '\n');
}

// ---------------------------------------------------------------- stdio loop
let inbuf = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => {
  inbuf += chunk;
  let nl;
  while ((nl = inbuf.indexOf('\n')) >= 0) {
    const line = inbuf.slice(0, nl).trim();
    inbuf = inbuf.slice(nl + 1);
    if (!line) continue;
    let msg;
    try { msg = JSON.parse(line); }
    catch (e) { send({ jsonrpc: '2.0', id: null, error: { code: -32700, message: 'Parse error' } }); continue; }
    if (Array.isArray(msg)) msg.forEach(handle); else handle(msg);
  }
});
process.stdin.on('end', () => process.exit(0));

// Tell Claude to refresh the tool list once Deswik becomes reachable after startup.
let wasReachable = null;
setInterval(async () => {
  let ok = false;
  try { await pipeRequest({ op: 'ping' }, { timeoutMs: 2000, retries: 1 }); ok = true; } catch (_) {}
  if (ok && wasReachable === false) send({ jsonrpc: '2.0', method: 'notifications/tools/list_changed' });
  wasReachable = ok;
}, 15000).unref();

log('started v' + VERSION + ', pipe ' + PIPE);
