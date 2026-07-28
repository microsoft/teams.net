// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HtmlWidgetBot;

/// <summary>
/// Browser-side HTML/JavaScript markup for the example widgets. Each constant is
/// the self-contained document rendered inside the widget iframe in Teams.
/// </summary>
public static class Widgets
{
    /// <summary>
    /// Simple static widget - no callbacks, no interactivity.
    /// </summary>
    public const string SimpleHtml = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{height:100%;overflow:auto}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;
  padding:16px;background:#fff;color:#242424;font-size:13px}
h3{margin:0 0 8px 0;color:#333}
p{margin:0;color:#666}
.status{margin-top:12px;padding:8px;background:#f0f9ff;border-radius:4px}
</style></head><body>
<h3>Simple HTML Widget</h3>
<p>This is a static HTML widget rendered inside a Teams message. No callbacks are needed.</p>
<div class=""status""><strong>Status:</strong> Rendered successfully</div>
</body></html>";

    /// <summary>
    /// CallTool widget - calls a "refresh" tool on the bot and displays the result.
    /// </summary>
    public const string CallToolHtml = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{height:100%;overflow:auto}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;
  padding:16px;background:#fff;color:#242424;font-size:13px}
h3{margin:0 0 8px 0}
p{margin:0 0 12px 0;color:#666}
button{padding:8px 16px;background:#5b5fc7;color:#fff;border:none;border-radius:4px;cursor:pointer}
button:hover{background:#4b4fb7}
#result{margin-top:12px;padding:8px;background:#f5f5f5;border-radius:4px}
</style></head><body>
<h3>CallTool Widget</h3>
<p>Click Refresh to call the bot's ""refresh"" tool.</p>
<button id=""refreshBtn"">Refresh</button>
<div id=""result"">Waiting for action...</div>
<script>
(function() {
  var callId = 0;

  // Send a tools/call JSON-RPC request when the button is clicked
  document.getElementById('refreshBtn').addEventListener('click', function() {
    var id = 'call-' + (++callId);
    document.getElementById('result').textContent = 'Calling refresh...';
    window.parent.postMessage({
      jsonrpc: '2.0',
      id: id,
      method: 'tools/call',
      params: { name: 'refresh', arguments: {} }
    }, '*');
  });

  // Listen for JSON-RPC responses from the host
  window.addEventListener('message', function(e) {
    var d = e.data;
    if (d && d.jsonrpc === '2.0' && d.id && typeof d.id === 'string' && d.id.startsWith('call-')) {
      if (d.result) document.getElementById('result').textContent = JSON.stringify(d.result);
      if (d.error) document.getElementById('result').textContent = 'Error: ' + JSON.stringify(d.error);
    }
  });
})()
</script>
</body></html>";

    /// <summary>
    /// MessageBack widget - sends a messageBack (ui/message) action to the bot.
    /// </summary>
    public const string MessageBackHtml = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{height:100%;overflow:auto}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;
  padding:16px;background:#fff;color:#242424;font-size:13px}
h3{margin:0 0 8px 0}
p{margin:0 0 12px 0;color:#666}
button{padding:8px 16px;background:#0078d4;color:#fff;border:none;border-radius:4px;cursor:pointer}
button:hover{background:#006cbd}
#status{margin-top:12px;color:#666}
</style></head><body>
<h3>MessageBack Widget</h3>
<p>Click the button to send a messageBack to the bot.</p>
<button id=""msgBtn"">Send MessageBack</button>
<div id=""status""></div>
<script>
(function() {
  // Send a ui/message JSON-RPC request (similar to messageBack in Adaptive Cards)
  document.getElementById('msgBtn').addEventListener('click', function() {
    var msgId = 'msg-' + Math.random().toString(36).slice(2);
    document.getElementById('status').textContent = 'Sending messageBack...';
    window.parent.postMessage({
      jsonrpc: '2.0',
      id: msgId,
      method: 'ui/message',
      params: {
        role: 'user',
        content: [{ type: 'text', text: 'Hello from the widget!' }]
      }
    }, '*');
    document.getElementById('status').textContent = 'MessageBack sent!';
  });
})()
</script>
</body></html>";

    /// <summary>
    /// Fullscreen widget - requests fullscreen display mode from the host.
    /// </summary>
    public const string FullscreenHtml = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{height:100%;overflow:auto}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;
  padding:16px;background:#fff;color:#242424;font-size:13px}
h3{margin:0 0 8px 0}
p{margin:0 0 12px 0;color:#666}
button{padding:8px 16px;background:#107c10;color:#fff;border:none;border-radius:4px;cursor:pointer}
button:hover{background:#0e6b0e}
#content{margin-top:12px;padding:16px;background:#f0fff0;border-radius:4px}
#modeLabel{font-weight:600}
</style></head><body>
<h3>Fullscreen Widget</h3>
<p>Click the button to request fullscreen mode from Teams.</p>
<button id=""fsBtn"">Go Fullscreen</button>
<div id=""content"">
  <p>In fullscreen mode, this widget will expand to fill the available space.</p>
  <p>Current mode: <span id=""modeLabel"">inline</span></p>
</div>
<script>
(function() {
  // Request fullscreen display mode from the Teams host
  document.getElementById('fsBtn').addEventListener('click', function() {
    var id = 'fs-' + Math.random().toString(36).slice(2);
    window.parent.postMessage({
      jsonrpc: '2.0',
      id: id,
      method: 'ui/request-display-mode',
      params: { mode: 'fullscreen' }
    }, '*');
  });

  // Listen for display mode change responses
  window.addEventListener('message', function(e) {
    var d = e.data;
    if (d && d.jsonrpc === '2.0') {
      if (d.result && d.result.mode) document.getElementById('modeLabel').textContent = d.result.mode;
      if (d.error) document.getElementById('modeLabel').textContent = 'Error: ' + JSON.stringify(d.error);
    }
  });
})()
</script>
</body></html>";

    /// <summary>
    /// Multi-tool widget - calls multiple different tools on the bot.
    /// </summary>
    public const string MultiToolHtml = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{height:100%;overflow:auto}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;
  padding:16px;background:#fff;color:#242424;font-size:13px}
h3{margin:0 0 8px 0}
p{margin:0 0 12px 0;color:#666}
.tools{display:flex;gap:8px;flex-wrap:wrap}
.tools button{padding:8px 12px;color:#fff;border:none;border-radius:4px;cursor:pointer}
#log{margin-top:12px;padding:8px;background:#1e1e1e;color:#d4d4d4;border-radius:4px;
  font-family:monospace;font-size:12px;max-height:200px;overflow-y:auto}
</style></head><body>
<h3>Multi-Tool Widget</h3>
<p>Each button calls a different tool on the bot.</p>
<div class=""tools"">
  <button data-tool=""getTime"" style=""background:#5b5fc7"">Get Time</button>
  <button data-tool=""roll"" data-args='{""sides"":20}' style=""background:#c75b5b"">Roll d20</button>
  <button data-tool=""echo"" data-args='{""hello"":""world""}' style=""background:#5bc75b"">Echo</button>
  <button data-tool=""unknownTool"" style=""background:#999"">Unknown (error)</button>
</div>
<div id=""log"">Available tools: getTime, roll, echo, unknownTool</div>
<script>
(function() {
  var callId = 0;
  var log = document.getElementById('log');

  // Each button sends tools/call with the tool name from data-tool attribute
  document.querySelectorAll('[data-tool]').forEach(function(btn) {
    btn.addEventListener('click', function() {
      var tool = btn.getAttribute('data-tool');
      var args = btn.getAttribute('data-args');
      var id = 'call-' + (++callId);
      log.textContent += '\nCalling ' + tool + '...';
      window.parent.postMessage({
        jsonrpc: '2.0',
        id: id,
        method: 'tools/call',
        params: { name: tool, arguments: args ? JSON.parse(args) : {} }
      }, '*');
    });
  });

  // Listen for JSON-RPC responses and display results
  window.addEventListener('message', function(e) {
    var d = e.data;
    if (d && d.jsonrpc === '2.0' && d.id && typeof d.id === 'string' && d.id.startsWith('call-')) {
      if (d.result) log.textContent += '\nResult: ' + JSON.stringify(d.result);
      if (d.error) log.textContent += '\nError: ' + JSON.stringify(d.error);
    }
  });
})()
</script>
</body></html>";

    /// <summary>
    /// Open Link widget - tests the ui/open-link method.
    /// </summary>
    public const string OpenLinkHtml = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{height:100%;overflow:auto}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;padding:16px;background:#fff;color:#242424;font-size:13px}
h3{margin:0 0 8px}
button{margin:4px 4px 4px 0;padding:6px 12px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5;color:#242424;cursor:pointer;font-size:12px}
button:hover{background:#e0e0e0}
#status{margin-top:12px;padding:8px;background:#f0f9ff;border-radius:4px;white-space:pre-wrap;font-family:monospace;font-size:11px}
</style></head><body>
<h3>Open Link Widget</h3>
<p>Tests the <code>ui/open-link</code> method (host opens a URL).</p>
<div style=""margin-top:12px"">
  <button onclick=""openLink('https://github.com/modelcontextprotocol/ext-apps')"">Open MCP Apps Repo</button>
  <button onclick=""openLink('https://learn.microsoft.com/en-us/microsoftteams/')"">Open Teams Docs</button>
  <button onclick=""openLink('not-a-valid-url')"">Open Invalid URL (error test)</button>
</div>
<div id=""status"">Waiting...</div>
<script>
let nextId = 100;
const pending = {};

window.addEventListener('message', (event) => {
  const data = event.data;
  if (data?.id && pending[data.id]) {
    pending[data.id](data);
  }
});

function sendRequest(method, params) {
  const id = nextId++;
  return new Promise((resolve) => {
    pending[id] = resolve;
    window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
  });
}

async function openLink(url) {
  const el = document.getElementById('status');
  el.textContent = 'Opening: ' + url + '...';
  try {
    const response = await sendRequest('ui/open-link', { url });
    if (response.error) {
      el.textContent = 'Error: ' + JSON.stringify(response.error);
    } else {
      el.textContent = 'Success! Host opened: ' + url;
    }
  } catch (e) {
    el.textContent = 'Exception: ' + e.message;
  }
}
</script>
</body></html>";

    /// <summary>
    /// Update Model Context widget - tests the ui/update-model-context method.
    /// </summary>
    public const string UpdateContextHtml = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{height:100%;overflow:auto}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;padding:16px;background:#fff;color:#242424;font-size:13px}
h3{margin:0 0 8px}
button{margin:4px 4px 4px 0;padding:6px 12px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5;color:#242424;cursor:pointer;font-size:12px}
button:hover{background:#e0e0e0}
textarea{width:100%;height:60px;margin:8px 0;padding:8px;border:1px solid #ccc;border-radius:4px;font-family:monospace;font-size:11px;resize:vertical}
#status{margin-top:12px;padding:8px;background:#f0f9ff;border-radius:4px;white-space:pre-wrap;font-family:monospace;font-size:11px}
</style></head><body>
<h3>Update Model Context Widget</h3>
<p>Tests <code>ui/update-model-context</code> - sends context for AI to use in future turns.</p>
<textarea id=""contextInput"">{""userPreference"": ""dark mode"", ""currentPage"": ""settings""}</textarea>
<div>
  <button onclick=""sendStructuredContext()"">Send Structured Context</button>
  <button onclick=""sendTextContext()"">Send Text Context</button>
  <button onclick=""sendBoth()"">Send Both</button>
</div>
<div id=""status"">Waiting...</div>
<script>
let nextId = 100;
const pending = {};

window.addEventListener('message', (event) => {
  const data = event.data;
  if (data?.id && pending[data.id]) {
    pending[data.id](data);
  }
});

function sendRequest(method, params) {
  const id = nextId++;
  return new Promise((resolve) => {
    pending[id] = resolve;
    window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
  });
}

async function sendStructuredContext() {
  const el = document.getElementById('status');
  const input = document.getElementById('contextInput').value;
  let parsed;
  try { parsed = JSON.parse(input); } catch (e) {
    el.textContent = 'Invalid JSON in textarea';
    return;
  }
  el.textContent = 'Sending structured context...';
  const response = await sendRequest('ui/update-model-context', {
    structuredContent: parsed
  });
  el.textContent = response.error
    ? 'Error: ' + JSON.stringify(response.error)
    : 'Success! Context updated with structured data.';
}

async function sendTextContext() {
  const el = document.getElementById('status');
  el.textContent = 'Sending text context...';
  const response = await sendRequest('ui/update-model-context', {
    content: [{ type: 'text', text: 'User is viewing the settings page and prefers dark mode.' }]
  });
  el.textContent = response.error
    ? 'Error: ' + JSON.stringify(response.error)
    : 'Success! Context updated with text content.';
}

async function sendBoth() {
  const el = document.getElementById('status');
  const input = document.getElementById('contextInput').value;
  let parsed;
  try { parsed = JSON.parse(input); } catch (e) {
    el.textContent = 'Invalid JSON in textarea';
    return;
  }
  el.textContent = 'Sending both text + structured context...';
  const response = await sendRequest('ui/update-model-context', {
    content: [{ type: 'text', text: 'User updated their preferences.' }],
    structuredContent: parsed
  });
  el.textContent = response.error
    ? 'Error: ' + JSON.stringify(response.error)
    : 'Success! Context updated with both text and structured data.';
}
</script>
</body></html>";

    /// <summary>
    /// Host Context widget - displays hostContext from the ui/initialize response.
    /// </summary>
    public const string HostContextHtml = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{overflow:auto}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;padding:16px;background:#fff;color:#242424;font-size:13px}
h3{margin:0 0 8px}
.section{margin-top:12px;padding:8px;background:#f0f9ff;border-radius:4px}
.section h4{margin:0 0 4px;font-size:12px;color:#333}
pre{white-space:pre-wrap;word-break:break-all;font-family:monospace;font-size:11px;color:#555}
.update{margin-top:8px;padding:6px;background:#fff3cd;border-radius:4px;font-size:11px}
</style></head><body>
<h3>Host Context Inspector</h3>
<p>Displays the <code>hostContext</code> from <code>ui/initialize</code> response and listens for changes.</p>
<div class=""section"">
  <h4>Initialize Result</h4>
  <pre id=""initResult"">Waiting for initialize...</pre>
</div>
<div class=""section"">
  <h4>Host Context</h4>
  <pre id=""hostContext"">-</pre>
</div>
<div class=""section"">
  <h4>Host Capabilities</h4>
  <pre id=""hostCaps"">-</pre>
</div>
<div id=""updates""></div>
<script>
let nextId = 100;
const pending = {};

window.addEventListener('message', (event) => {
  const data = event.data;
  if (!data || typeof data !== 'object') return;

  // Handle responses to our requests
  if (data.id && pending[data.id]) {
    pending[data.id](data);
    return;
  }

  // Handle notifications from host
  if (data.method === 'ui/notifications/host-context-changed') {
    const el = document.getElementById('updates');
    const div = document.createElement('div');
    div.className = 'update';
    div.textContent = '[' + new Date().toLocaleTimeString() + '] host-context-changed: ' + JSON.stringify(data.params);
    el.appendChild(div);

    // Update main display
    if (data.params) {
      document.getElementById('hostContext').textContent = JSON.stringify(data.params, null, 2);
    }
  }
});

function sendRequest(method, params) {
  const id = nextId++;
  return new Promise((resolve) => {
    pending[id] = resolve;
    window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
  });
}

async function init() {
  const response = await sendRequest('ui/initialize', {
    protocolVersion: '2026-01-26',
    appInfo: { name: 'host-context-inspector', version: '1.0.0' },
    appCapabilities: {}
  });

  document.getElementById('initResult').textContent = JSON.stringify(response.result || response.error, null, 2);

  if (response.result) {
    const ctx = response.result.hostContext;
    const caps = response.result.hostCapabilities;
    document.getElementById('hostContext').textContent = ctx ? JSON.stringify(ctx, null, 2) : '(none provided)';
    document.getElementById('hostCaps').textContent = caps ? JSON.stringify(caps, null, 2) : '(none provided)';
  }

  // Send initialized notification
  window.parent.postMessage({ jsonrpc: '2.0', method: 'ui/notifications/initialized', params: {} }, '*');

  // Report content size so the host can size the iframe to fit. The SDK-injected
  // protocol normally does this, but it skips injection for widgets (like this one)
  // that run their own ui/initialize handshake, so we report the size ourselves.
  notifySize();
  setTimeout(notifySize, 100);
}

function notifySize() {
  window.parent.postMessage({ jsonrpc: '2.0', method: 'ui/notifications/size-changed', params: { height: document.body.scrollHeight } }, '*');
}

init();
</script>
</body></html>";
}
