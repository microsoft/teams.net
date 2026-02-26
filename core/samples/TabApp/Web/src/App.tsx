import { useState, useEffect, useCallback } from 'react'
import { app, authentication } from '@microsoft/teams-js'
import { PublicClientApplication, InteractionRequiredAuthError } from '@azure/msal-browser'

let _msal: PublicClientApplication | null = null

async function getMsal(tenantId: string): Promise<PublicClientApplication> {
  if (!_msal) {
    _msal = new PublicClientApplication({
      auth: {
        clientId: import.meta.env.VITE_CLIENT_ID as string,
        authority: `https://login.microsoftonline.com/${tenantId}`,
        redirectUri: window.location.origin + window.location.pathname,
      },
    })
    await _msal.initialize()
  }
  return _msal
}

export default function App() {
  const [context, setContext] = useState<app.Context | null>(null)
  const [message, setMessage] = useState<string>('Hello from the tab!')
  const [result, setResult] = useState<string>('')
  const [initialized, setInitialized] = useState(false)
  const [status, setStatus] = useState(false)

  useEffect(() => {
    app.initialize().then(() => {
      app.getContext().then((ctx) => {
        setContext(ctx)
        setInitialized(true)
      })
    })
  }, [])

  async function callFunction(name: string, body: unknown): Promise<unknown> {
    const [token, ctx] = await Promise.all([
      authentication.getAuthToken(),
      app.getContext(),
    ])

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
    }

    if (ctx.app.sessionId)    headers['X-Teams-App-Session-Id'] = ctx.app.sessionId
    if (ctx.page.id)          headers['X-Teams-Page-Id']         = ctx.page.id
    if (ctx.page.subPageId)   headers['X-Teams-Sub-Page-Id']     = ctx.page.subPageId
    if (ctx.channel?.id)      headers['X-Teams-Channel-Id']      = ctx.channel.id
    if (ctx.chat?.id)         headers['X-Teams-Chat-Id']         = ctx.chat.id
    if (ctx.meeting?.id)      headers['X-Teams-Meeting-Id']      = ctx.meeting.id
    if (ctx.team?.groupId)    headers['X-Teams-Team-Id']         = ctx.team.groupId

    const res = await fetch(`/functions/${name}`, {
      method: 'POST',
      headers,
      body: JSON.stringify(body),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    return res.json()
  }

  async function run(fn: () => Promise<unknown>) {
    try {
      const res = await fn()
      setResult(JSON.stringify(res, null, 2))
    } catch (e) {
      setResult(String(e))
    }
  }

  const showContext = useCallback(() => run(async () => context), [context])
  const postToChat  = useCallback(() => run(() => callFunction('post-to-chat', { message })), [message])
  const whoAmI = useCallback(() => run(async () => {
    const tenantId = context?.user?.tenant?.id ?? 'common'
    const loginHint = context?.user?.loginHint
    const msal = await getMsal(tenantId)
    const scopes = ['User.Read']

    const accounts = msal.getAllAccounts()
    const account = loginHint
      ? (accounts.find(a => a.username === loginHint) ?? accounts[0])
      : accounts[0]

    let accessToken: string
    try {
      if (!account) throw new InteractionRequiredAuthError('no_account')
      const result = await msal.acquireTokenSilent({ scopes, account })
      accessToken = result.accessToken
    } catch (e) {
      if (!(e instanceof InteractionRequiredAuthError)) throw e
      const result = await msal.acquireTokenPopup({ scopes, loginHint })
      accessToken = result.accessToken
    }

    return fetch('https://graph.microsoft.com/v1.0/me', {
      headers: { Authorization: `Bearer ${accessToken}` },
    }).then(r => r.json())
  }), [context])

  // TODO: Move whoAmI and toggleStatus to server-side bot functions once SSO OBO is implemented,
  //       so Graph token acquisition happens on the server via the On-Behalf-Of flow.
  const toggleStatus = useCallback(() => run(async () => {
    const tenantId = context?.user?.tenant?.id ?? 'common'
    const loginHint = context?.user?.loginHint
    const msal = await getMsal(tenantId)
    const scopes = ['Presence.ReadWrite']

    const accounts = msal.getAllAccounts()
    const account = loginHint
      ? (accounts.find(a => a.username === loginHint) ?? accounts[0])
      : accounts[0]

    let accessToken: string
    try {
      if (!account) throw new InteractionRequiredAuthError('no_account')
      const result = await msal.acquireTokenSilent({ scopes, account })
      accessToken = result.accessToken
    } catch (e) {
      if (!(e instanceof InteractionRequiredAuthError)) throw e
      const result = await msal.acquireTokenPopup({ scopes, loginHint })
      accessToken = result.accessToken
    }

    const newStatus = !status
    const availability = newStatus ? 'DoNotDisturb' : 'Available'

    const res = await fetch('https://graph.microsoft.com/v1.0/me/presence/setUserPreferredPresence', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${accessToken}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ availability, activity: availability }),
    })
    if (!res.ok) {
      const body = await res.json().catch(() => ({}))
      throw new Error(`Graph ${res.status}: ${JSON.stringify(body)}`)
    }
    setStatus(newStatus)
    return { availability }
  }), [status, context])

  if (!initialized) {
    return <div className="loading">Initializing Teams SDK…</div>
  }

  return (
    <div className="app">
      <h1>Teams Tab Sample</h1>

      <section className="card">
        <h2>Teams Context</h2>
        <p className="hint">Shows the raw Teams context for this session.</p>
        <button onClick={showContext}>Show Context</button>
      </section>

      <section className="card">
        <h2>Post to Chat</h2>
        <p className="hint">Sends a proactive message via the bot.</p>
        <input
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          placeholder="Message text"
        />
        <button onClick={postToChat}>Post to Chat</button>
      </section>

      <section className="card">
        <h2>Who Am I</h2>
        <p className="hint">Looks up your member record.</p>
        <button onClick={whoAmI}>Who Am I</button>
      </section>

      <section className="card">
        <h2>Toggle Presence</h2>
        <p className="hint">Sets your Teams presence via Graph. Current: <strong>{status ? 'DoNotDisturb' : 'Available'}</strong></p>
        <button onClick={toggleStatus}>{status ? 'Set Available' : 'Set DND'}</button>
      </section>

      {result && (
        <section className="card result">
          <h2>Result</h2>
          <pre>{result}</pre>
        </section>
      )}
    </div>
  )
}
