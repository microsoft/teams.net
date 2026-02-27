import { useState, useEffect, useCallback } from 'react'
import { app } from '@microsoft/teams-js'
import { createNestablePublicClientApplication, InteractionRequiredAuthError, IPublicClientApplication } from '@azure/msal-browser'

const clientId = import.meta.env.VITE_CLIENT_ID as string
let _msal: IPublicClientApplication

//TODO : do we want to take dependency on teams.client 
async function getMsal(): Promise<IPublicClientApplication> {
    if (!_msal) {
      _msal = await createNestablePublicClientApplication({
      auth: { clientId, authority: '', redirectUri: '/' },
    })
  }
    return _msal
}

async function acquireToken(scopes: string[], context: app.Context | null): Promise<string> {
  const loginHint = context?.user?.loginHint
  const msal = await getMsal()

  const accounts = msal.getAllAccounts()
  const account = loginHint
    ? (accounts.find(a => a.username === loginHint) ?? accounts[0])
    : accounts[0]

  try {
    if (!account) throw new InteractionRequiredAuthError('no_account')
    const result = await msal.acquireTokenSilent({ scopes, account })
    return result.accessToken
  } catch (e) {
    if (!(e instanceof InteractionRequiredAuthError)) throw e
    const result = await msal.acquireTokenPopup({ scopes, loginHint })
    return result.accessToken
  }
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
    const msal = await getMsal()
    const [{ accessToken }, ctx] = await Promise.all([
      msal.acquireTokenSilent({ scopes: [`api://${clientId}/access_as_user`] }),
      app.getContext(),
    ])

    const res = await fetch(`/functions/${name}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ payload: body, context: ctx }),
    })
    //TODO : pass entire ctx or specific fields ?
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
    const accessToken = await acquireToken(['User.Read'], context)
    return fetch('https://graph.microsoft.com/v1.0/me', {
      headers: { Authorization: `Bearer ${accessToken}` },
    }).then(r => r.json())
  }), [context])

  const toggleStatus = useCallback(() => run(async () => {
    const accessToken = await acquireToken(['Presence.ReadWrite'], context)

    const presenceRes = await fetch('https://graph.microsoft.com/v1.0/me/presence', {
      headers: { Authorization: `Bearer ${accessToken}` },
    })
    if (!presenceRes.ok) throw new Error(`Graph ${presenceRes.status}`)
    const { availability: current } = await presenceRes.json()

    const isAvailable = current === 'Available'
    const availability = isAvailable ? 'DoNotDisturb' : 'Available'
    const activity = isAvailable ? 'DoNotDisturb' : 'Available'

    const res = await fetch('https://graph.microsoft.com/v1.0/me/presence/setUserPreferredPresence', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${accessToken}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ availability, activity }),
    })
    if (!res.ok) {
      const body = await res.json().catch(() => ({}))
      throw new Error(`Graph ${res.status}: ${JSON.stringify(body)}`)
    }
    setStatus(availability === 'DoNotDisturb')
    return { availability, activity }
  }), [context])

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
