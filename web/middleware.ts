import { NextRequest, NextResponse } from "next/server";

const publicPaths = ["/login", "/password-reset"];

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  if (publicPaths.some((path) => pathname === path || pathname.startsWith(`${path}/`))) return NextResponse.next();
  if (pathname.startsWith("/api/")) return NextResponse.next();

  const api = process.env.WIKI_API_URL;
  if (pathname === "/setup" || pathname.startsWith("/setup/")) {
    if (!api) return NextResponse.json({ error: "Private service is not configured." }, { status: 503 });
    try {
      const response = await fetch(new URL("/auth/setup-status", api), { cache: "no-store" });
      const status = await response.json() as { setupAvailable?: boolean };
      if (response.ok && status.setupAvailable) return NextResponse.next();
      if (!response.ok) return NextResponse.json({ error: "Setup status is unavailable." }, { status: 503 });
      return NextResponse.redirect(new URL("/login", request.url));
    } catch {
      return NextResponse.json({ error: "Private service is unavailable." }, { status: 503 });
    }
  }
  const session = request.cookies.get("__Host-wiki-session")?.value;
  if (!session) {
    const login = new URL("/login", request.url);
    login.searchParams.set("returnTo", pathname);
    return NextResponse.redirect(login);
  }
  if (!api) return NextResponse.json({ error: "Private service is not configured." }, { status: 503 });
  try {
    const response = await fetch(new URL("/auth/session", api), {
      headers: { cookie: `__Host-wiki-session=${session}` }, cache: "no-store"
    });
    if (!response.ok) {
      const login = new URL("/login", request.url);
      login.searchParams.set("returnTo", pathname);
      return NextResponse.redirect(login);
    }
  } catch {
    return NextResponse.json({ error: "Private service is unavailable." }, { status: 503 });
  }
  return NextResponse.next();
}

export const config = { matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"] };
