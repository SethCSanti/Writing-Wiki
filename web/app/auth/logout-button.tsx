"use client";

export function LogoutButton() {
  async function logout() {
    const csrf = await fetch("/api/auth/csrf", { cache: "no-store" });
    if (!csrf.ok) return;
    const { token } = await csrf.json() as { token: string };
    await fetch("/api/auth/logout", { method: "POST", headers: { "X-CSRF-TOKEN": token }, cache: "no-store" });
    window.location.assign("/login");
  }
  return <button className="logout-button" type="button" onClick={() => void logout()}>Sign out</button>;
}
