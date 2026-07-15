// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HtmlWidgetBot;

/// <summary>
/// HTML widget strings matching the TS/PY examples exactly.
/// These are browser-side JavaScript -- language-agnostic across all SDKs.
/// </summary>
public static class Widgets
{
    /// <summary>
    /// Simple static widget - no callbacks, no interactivity.
    /// Protocol: Bot sends HTML, SDK auto-injects MCP Apps protocol, Teams renders in iframe.
    /// No postMessage calls needed - purely static content.
    /// </summary>
    public const string SimpleHtml = """
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 16px; border-radius: 8px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white;">
          <h2 style="margin: 0 0 8px 0;">Hello from HTML Widget!</h2>
          <p style="margin: 0; opacity: 0.9;">This is a static widget with no callbacks. It demonstrates basic HTML rendering in Teams.</p>
          <div style="margin-top: 12px; padding: 8px; background: rgba(255,255,255,0.2); border-radius: 4px; font-family: monospace; font-size: 12px;">
            Rendered at: <span id="time"></span>
          </div>
          <script>document.getElementById('time').textContent=new Date().toLocaleTimeString();</script>
        </div>
        """;

    /// <summary>
    /// CallTool widget - calls a "refresh" tool on the bot and displays the result.
    /// Protocol: Widget sends { method: "tools/call" } via postMessage -> Teams routes as
    /// htmlwidget/calltool invoke -> bot returns CallToolResult -> Teams delivers JSON-RPC response.
    /// </summary>
    public const string CallToolHtml = """
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 16px;">
          <h3 style="margin: 0 0 12px 0;">CallTool Widget</h3>
          <div id="status" style="padding: 8px; background: #f0f0f0; border-radius: 4px; margin-bottom: 12px;">Ready</div>
          <button onclick="callRefresh()" style="padding: 8px 16px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Refresh</button>
          <pre id="result" style="margin-top: 12px; padding: 8px; background: #1e1e1e; color: #d4d4d4; border-radius: 4px; font-size: 11px; overflow: auto; max-height: 200px;"></pre>
          <script>
            var counter=0;
            function callRefresh(){
              counter++;
              document.getElementById('status').textContent='Calling tool...';
              var id='refresh-'+Date.now();
              window.parent.postMessage({jsonrpc:'2.0',method:'tools/call',id:id,params:{name:'refresh',arguments:{counter:counter}}},'*');
              window.addEventListener('message',function handler(ev){
                var d=ev.data;if(!d||d.id!==id)return;
                window.removeEventListener('message',handler);
                document.getElementById('status').textContent='Done (counter='+counter+')';
                document.getElementById('result').textContent=JSON.stringify(d.result||d.error,null,2);
              });
            }
          </script>
        </div>
        """;

    /// <summary>
    /// MessageBack widget - sends a messageBack action to the bot.
    /// Protocol: Widget sends { method: "ui/message" } via postMessage -> Teams delivers
    /// as a new message activity with activity.Value containing the messageBack content.
    /// </summary>
    public const string MessageBackHtml = """
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 16px;">
          <h3 style="margin: 0 0 12px 0;">MessageBack Widget</h3>
          <p style="margin: 0 0 12px 0; color: #666;">Click a button to send a messageBack to the bot:</p>
          <button onclick="send('hello')" style="padding: 8px 16px; margin-right: 8px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Say Hello</button>
          <button onclick="send('world')" style="padding: 8px 16px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Say World</button>
          <script>
            function send(text){
              window.parent.postMessage({jsonrpc:'2.0',method:'ui/message',params:{type:'messageBack',text:text,value:{source:'widget',action:text}}},'*');
            }
          </script>
        </div>
        """;

    /// <summary>
    /// Fullscreen widget - requests fullscreen display mode from the host.
    /// Protocol: Widget sends { method: "ui/request-display-mode", params: { mode: "fullscreen" } }
    /// via postMessage -> Teams expands the iframe -> host responds with new mode.
    /// </summary>
    public const string FullscreenHtml = """
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 16px;">
          <h3 style="margin: 0 0 12px 0;">Fullscreen Widget</h3>
          <p id="mode" style="margin: 0 0 12px 0;">Current mode: inline</p>
          <button onclick="requestFullscreen()" style="padding: 8px 16px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Go Fullscreen</button>
          <button onclick="requestInline()" style="padding: 8px 16px; margin-left: 8px; background: #666; color: white; border: none; border-radius: 4px; cursor: pointer;">Go Inline</button>
          <script>
            function requestFullscreen(){
              window.parent.postMessage({jsonrpc:'2.0',method:'ui/request-display-mode',id:'fs-'+Date.now(),params:{displayMode:'fullscreen'}},'*');
              document.getElementById('mode').textContent='Current mode: fullscreen (requested)';
            }
            function requestInline(){
              window.parent.postMessage({jsonrpc:'2.0',method:'ui/request-display-mode',id:'in-'+Date.now(),params:{displayMode:'inline'}},'*');
              document.getElementById('mode').textContent='Current mode: inline (requested)';
            }
          </script>
        </div>
        """;

    /// <summary>
    /// Multi-tool widget - calls multiple different tools on the bot.
    /// Protocol: Each button sends tools/call with a different tool name -> Teams routes each
    /// as an htmlwidget/calltool invoke -> bot dispatches by name -> results displayed in log.
    /// </summary>
    public const string MultiToolHtml = """
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 16px;">
          <h3 style="margin: 0 0 12px 0;">Multi-Tool Widget</h3>
          <div style="display: flex; gap: 8px; margin-bottom: 12px;">
            <button onclick="call('getTime',{})" style="padding: 8px 12px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Get Time</button>
            <button onclick="call('roll',{sides:20})" style="padding: 8px 12px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Roll d20</button>
            <button onclick="call('echo',{msg:'hello'})" style="padding: 8px 12px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Echo</button>
          </div>
          <pre id="log" style="padding: 8px; background: #1e1e1e; color: #d4d4d4; border-radius: 4px; font-size: 11px; max-height: 200px; overflow: auto;"></pre>
          <script>
            function call(name,args){
              var id=name+'-'+Date.now();
              window.parent.postMessage({jsonrpc:'2.0',method:'tools/call',id:id,params:{name:name,arguments:args}},'*');
              window.addEventListener('message',function handler(ev){
                var d=ev.data;if(!d||d.id!==id)return;
                window.removeEventListener('message',handler);
                var log=document.getElementById('log');
                log.textContent+=name+': '+JSON.stringify(d.result||d.error)+'\n';
              });
            }
          </script>
        </div>
        """;

    /// <summary>
    /// Open Link widget - tests ui/open-link method.
    /// Protocol: Widget sends { method: "ui/open-link", params: { url } } via postMessage ->
    /// Teams host opens the URL in the user's default browser -> responds with success/error.
    /// </summary>
    public const string OpenLinkHtml = """
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 16px;">
          <h3 style="margin: 0 0 12px 0;">Open Link Widget</h3>
          <button onclick="openUrl('https://learn.microsoft.com/microsoftteams/')" style="padding: 8px 16px; margin-right: 8px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Teams Docs</button>
          <button onclick="openUrl('https://github.com/microsoft/teams.ts')" style="padding: 8px 16px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Teams SDK</button>
          <script>
            function openUrl(url){
              window.parent.postMessage({jsonrpc:'2.0',method:'ui/open-link',id:'ol-'+Date.now(),params:{url:url}},'*');
            }
          </script>
        </div>
        """;

    /// <summary>
    /// Update Model Context widget - tests ui/update-model-context method.
    /// Protocol: Widget sends { method: "ui/update-model-context", params: { content, structuredContent } }
    /// via postMessage -> Teams stores context for AI in subsequent turns -> responds with success/error.
    /// </summary>
    public const string UpdateContextHtml = """
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 16px;">
          <h3 style="margin: 0 0 12px 0;">Update Context Widget</h3>
          <textarea id="ctx" rows="3" style="width: 100%; box-sizing: border-box; padding: 8px; border: 1px solid #ddd; border-radius: 4px;" placeholder="Type context to send to the model..."></textarea>
          <button onclick="updateCtx()" style="margin-top: 8px; padding: 8px 16px; background: #6264a7; color: white; border: none; border-radius: 4px; cursor: pointer;">Send Context</button>
          <div id="status" style="margin-top: 8px; color: #666; font-size: 12px;"></div>
          <script>
            function updateCtx(){
              var text=document.getElementById('ctx').value;
              var id='ctx-'+Date.now();
              window.parent.postMessage({jsonrpc:'2.0',method:'ui/update-model-context',id:id,params:{modelContext:{content:text}}},'*');
              window.addEventListener('message',function handler(ev){
                var d=ev.data;if(!d||d.id!==id)return;
                window.removeEventListener('message',handler);
                document.getElementById('status').textContent=d.error?'Error: '+JSON.stringify(d.error):'Context updated!';
              });
            }
          </script>
        </div>
        """;

    /// <summary>
    /// Host Context inspector widget - displays hostContext from ui/initialize response.
    /// Protocol: SDK-injected protocol sends ui/initialize -> host responds with hostContext
    /// (theme, dimensions, locale) and hostCapabilities -> widget displays the data.
    /// </summary>
    public const string HostContextHtml = """
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 16px;">
          <h3 style="margin: 0 0 12px 0;">Host Context Inspector</h3>
          <pre id="ctx" style="padding: 8px; background: #1e1e1e; color: #d4d4d4; border-radius: 4px; font-size: 11px; overflow: auto; max-height: 300px;">Waiting for ui/initialize...</pre>
          <script>
            window.addEventListener('message',function(ev){
              var d=ev.data;
              if(d&&d.method==='ui/initialize'){
                document.getElementById('ctx').textContent=JSON.stringify(d.params,null,2);
                window.parent.postMessage({jsonrpc:'2.0',method:'ui/notifications/initialized',params:{}},'*');
              }
            });
          </script>
        </div>
        """;
}
