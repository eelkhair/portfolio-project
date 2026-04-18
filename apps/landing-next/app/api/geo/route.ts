import { NextRequest, NextResponse } from "next/server";
import { resolveGeo } from "../../lib/geo";

// Edge Runtime keeps cold start tiny. Returns the same shape used SSR-side.
export const runtime = "edge";

export async function GET(req: NextRequest) {
  const geo = await resolveGeo(req.headers);
  return NextResponse.json(geo, {
    headers: { "Cache-Control": "private, max-age=86400" },
  });
}
