import { NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

async function proxy(request: NextRequest) {
  const base = process.env.WIKI_API_URL;
  if (!base) return NextResponse.json({ error: "API gateway is not configured." }, { status: 503 });

  const target = new URL(request.nextUrl.pathname.replace(/^\/api/, ""), base.endsWith("/") ? base : `${base}/`);
  target.search = request.nextUrl.search;
  const headers = new Headers();
  for (const name of ["accept", "content-type", "cookie", "x-csrf-token"]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  headers.set("X-Forwarded-Proto", request.nextUrl.hostname === "localhost" || request.nextUrl.hostname === "127.0.0.1"
    ? "https" : request.nextUrl.protocol.slice(0, -1));

  try {
    const upstream = await fetch(target, {
      method: request.method,
      headers,
      body: ["GET", "HEAD"].includes(request.method) ? undefined : await request.arrayBuffer(),
      cache: "no-store",
      redirect: "manual"
    });
    const responseHeaders = new Headers();
    for (const name of ["content-type", "cache-control"]) {
      const value = upstream.headers.get(name);
      if (value) responseHeaders.set(name, value);
    }
    for (const value of upstream.headers.getSetCookie()) responseHeaders.append("Set-Cookie", value);
    responseHeaders.set("Cache-Control", "no-store");
    return new NextResponse(upstream.body, { status: upstream.status, headers: responseHeaders });
  } catch {
    return NextResponse.json({ error: "The private service is unavailable." }, { status: 503 });
  }
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;
