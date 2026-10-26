"use client";

import { FormEvent, useEffect, useRef, useState } from "react";

type Props = { mode: "login" | "setup" | "password-reset" };

export function AuthPanel({ mode }: Props) {
  const [step, setStep] = useState(mode === "login" ? "password" : mode === "setup" ? "create" : "request");
  const [message, setMessage] = useState("");
  const [secret, setSecret] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const confirmationStarted = useRef(false);

  async function post(path: string, body: Record<string, string>) {
    const csrfResponse = await fetch("/api/auth/csrf", { cache: "no-store" });
    if (!csrfResponse.ok) throw new Error("Security token unavailable. Refresh and try again.");
    const { token } = await csrfResponse.json() as { token: string };
    return fetch(`/api/auth/${path}`, {
      method: "POST", headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": token },
      body: JSON.stringify(body), cache: "no-store"
    });
  }

  useEffect(() => {
    if (mode !== "setup" || typeof window === "undefined" || !window.location.hash || confirmationStarted.current) return;
    confirmationStarted.current = true;
    const query = new URLSearchParams(window.location.hash.slice(1));
    const email = query.get("email"); const code = query.get("code");
    window.history.replaceState(null, "", "/setup");
    if (!email || !code) return;
    setBusy(true); setMessage("Confirming your email…");
    void post("setup/confirm-email", { email, code }).then(async (response) => {
      if (!response.ok) throw new Error("Email confirmation failed. Restart setup or request a fresh confirmation.");
      const result = await response.json() as { authenticatorKey: string };
      setSecret(result.authenticatorKey); setStep("enroll"); setMessage("");
    }).catch((error: unknown) => setMessage(error instanceof Error ? error.message : "Email confirmation failed."))
      .finally(() => setBusy(false));
  }, [mode]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true); setMessage("");
    const values = Object.fromEntries(new FormData(event.currentTarget).entries()) as Record<string, string>;
    try {
      if (mode === "login" && step === "password") {
        const response = await post("login", { email: values.email, password: values.password });
        if (!response.ok) throw new Error("Sign-in failed. Check your details or try again later.");
        const result = await response.json() as { requiresMfa: boolean };
        if (result.requiresMfa) setStep("totp");
        else window.location.assign("/");
      } else if (mode === "login" && step === "totp") {
        const response = await post(values.recovery ? "login/recovery" : "login/totp", { code: values.code });
        if (!response.ok) throw new Error("Verification failed. Check the code or try an unused recovery code.");
        window.location.assign("/");
      } else if (mode === "setup" && step === "create") {
        const response = await post("setup", { setupKey: values.setupKey, email: values.email, password: values.password });
        if (!response.ok) throw new Error("Owner setup is unavailable or the account details were rejected.");
        setStep("verify-email");
      } else if (mode === "setup" && step === "enroll") {
        const response = await post("setup/confirm-totp", { code: values.code });
        if (!response.ok) throw new Error("That code was not accepted. Check your authenticator and try again.");
        const result = await response.json() as { recoveryCodes: string[] };
        setRecoveryCodes(result.recoveryCodes); setStep("recovery");
      } else if (mode === "password-reset" && step === "request") {
        await post("password-reset/request", { email: values.email });
        setMessage("If the address is verified, reset instructions will be sent.");
      } else if (mode === "password-reset" && step === "complete") {
        const response = await post("password-reset/complete", { email: values.email, code: values.code, newPassword: values.password });
        if (!response.ok) throw new Error("The reset could not be completed. Request a fresh code and try again.");
        setMessage("Password reset complete. You can now sign in with your new password.");
      }
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Something went wrong. Try again.");
    } finally { setBusy(false); }
  }

  const title = mode === "login" ? "Welcome back" : mode === "setup" ? "Set up your private wiki" : "Reset your password";
  return <main className="auth-page"><section className="auth-card" aria-labelledby="auth-title">
    <a className="auth-brand" href="/login"><span className="brand-mark">W</span><span>Worldbuilding Wiki</span></a>
    <p className="eyebrow">PRIVATE OWNER WORKSPACE</p><h1 id="auth-title">{title}</h1>
    {mode === "login" && step === "password" && <form onSubmit={submit}>
      <label>Email<input name="email" type="email" autoComplete="username" required /></label>
      <label>Password<input name="password" type="password" autoComplete="current-password" required /></label>
      <button className="button auth-submit" disabled={busy}>{busy ? "Signing in…" : "Continue"}</button>
      <a className="auth-link" href="/password-reset">Forgot password?</a>
    </form>}
    {mode === "login" && step === "totp" && <form onSubmit={submit}>
      <p className="auth-copy">Enter your authenticator code. To use a recovery code, select the recovery option.</p>
      <label>Authenticator or recovery code<input name="code" inputMode="numeric" autoComplete="one-time-code" required /></label>
      <label className="checkbox-label"><input name="recovery" type="checkbox" value="yes" /> Use a recovery code</label>
      <button className="button auth-submit" disabled={busy}>{busy ? "Verifying…" : "Verify and sign in"}</button>
    </form>}
    {mode === "setup" && step === "create" && <form onSubmit={submit}>
      <p className="auth-copy">This one-time setup requires the key configured by the operator. Use an email address you can access for password recovery.</p>
      <label>One-time setup key<input name="setupKey" type="password" autoComplete="off" required /></label>
      <label>Email<input name="email" type="email" autoComplete="email" required /></label>
      <label>Password<input name="password" type="password" autoComplete="new-password" minLength={12} required /></label>
      <button className="button auth-submit" disabled={busy}>{busy ? "Creating account…" : "Create owner account"}</button>
    </form>}
    {mode === "setup" && step === "enroll" && <form onSubmit={submit}>
      <p className="auth-copy">Add this key to an authenticator app, then enter its current six-digit code. Save the key securely while completing setup.</p>
      <label>Authenticator key<code className="secret-value">{secret}</code></label>
      <label>Six-digit code<input name="code" inputMode="numeric" autoComplete="one-time-code" minLength={6} maxLength={8} required /></label>
      <button className="button auth-submit" disabled={busy}>{busy ? "Verifying…" : "Enable TOTP"}</button>
    </form>}
    {mode === "setup" && step === "verify-email" && <p className="auth-copy">Check your inbox and open the confirmation link to verify this address. TOTP enrollment starts after confirmation.</p>}
    {mode === "setup" && step === "recovery" && <div><p className="auth-copy">Store these one-time recovery codes offline. They will not be shown again.</p><ul className="recovery-list">{recoveryCodes.map((code) => <li key={code}><code>{code}</code></li>)}</ul><a className="button auth-submit" href="/login">Continue to sign in</a></div>}
    {mode === "password-reset" && step === "request" && <><form onSubmit={submit}><p className="auth-copy">Request a single-use reset code for your verified email.</p><label>Email<input name="email" type="email" autoComplete="email" required /></label><button className="button auth-submit" disabled={busy}>Send reset instructions</button></form><button className="text-button" onClick={() => { setStep("complete"); setMessage(""); }}>I have a reset code</button></>}
    {mode === "password-reset" && step === "complete" && <form onSubmit={submit}><label>Email<input name="email" type="email" autoComplete="email" required /></label><label>Reset code<input name="code" autoComplete="one-time-code" required /></label><label>New password<input name="password" type="password" autoComplete="new-password" minLength={12} required /></label><button className="button auth-submit" disabled={busy}>Reset password</button></form>}
    {message && <p className="form-message" role="status">{message}</p>}
    {mode !== "login" && <a className="auth-link" href="/login">Return to sign in</a>}
  </section></main>;
}
