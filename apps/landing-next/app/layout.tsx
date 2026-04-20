import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { headers } from "next/headers";
import { ThemeProvider } from "./components/ThemeProvider";
import { FeatureFlagsProvider } from "./components/FeatureFlags";
import { FaroProvider } from "./components/FaroProvider";
import { fetchFeatureFlags } from "./lib/feature-flags";
import { resolveGeo, type CfProperties } from "./lib/geo";
import "./globals.css";

/**
 * On Cloudflare Pages, pull the Workers `request.cf` object (city, region,
 * lat/lon at zero latency) via `@cloudflare/next-on-pages` → `getRequestContext`.
 * Dynamic import + try/catch so this is a no-op on Proxmox / local dev where the
 * package isn't installed or the request context doesn't exist, letting
 * `resolveGeo` fall through to ipapi.co.
 */
async function getCloudflareCf(): Promise<CfProperties | undefined> {
  try {
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore – package only present on CF Pages build
    const mod: { getRequestContext?: () => { cf?: CfProperties } } =
      await import("@cloudflare/next-on-pages");
    return mod.getRequestContext?.()?.cf;
  } catch {
    return undefined;
  }
}

const inter = Inter({
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700"],
});

export const metadata: Metadata = {
  title: {
    default: "Elkhair Elkhair - Staff Software Engineer & Solutions Architect",
    template: "%s | Elkhair Elkhair",
  },
  description:
    "Staff Software Engineer & Solutions Architect with 15+ years building and scaling SaaS platforms. Portfolio showcasing distributed systems, AI integration, and enterprise architecture.",
  icons: {
    icon: "data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 48 48' width='48' height='48'><g transform='translate(24,24) rotate(45)'><rect x='-12' y='-12' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/><rect x='-3.5' y='-12' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/><rect x='5' y='-12' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/><rect x='-12' y='-3.5' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/><rect x='-3.5' y='-3.5' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/><rect x='5' y='-3.5' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/><rect x='-12' y='5' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/><rect x='-3.5' y='5' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/><rect x='5' y='5' width='7' height='7' fill='%231E293B' stroke='%230ea5e9' stroke-width='1'/></g><polygon points='24,8 27,18 38,18 29,25 32,36 24,30 16,36 19,25 10,18 21,18' fill='%230ea5e9' stroke='%230ea5e9' stroke-width='1.5'/></svg>",
  },
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const flags = await fetchFeatureFlags();
  const faroEnv =
    process.env.OTEL_RESOURCE_ATTRIBUTES?.split(",")
      .find((p) => p.trim().startsWith("deployment.environment="))
      ?.split("=")[1]
      ?.trim() ??
    process.env.NODE_ENV ??
    "local";

  // Resolve geo server-side so FaroProvider has country/city/lat/lon synchronously
  // when initializing the OTel tracer. Otherwise, the page-load spans fire before
  // an async client lookup can return, and city ends up empty in spanmetrics.
  //
  // Resolution order (see resolveGeo):
  //   1. `request.cf` from Cloudflare Pages (fast, no external call)
  //   2. ipapi.co (Proxmox / local — country also from CF-IPCountry header)
  //   3. `cf-ipcountry` header as last-ditch country-only fallback
  const h = await headers();
  const cf = await getCloudflareCf();
  const geo = await resolveGeo(h, cf);

  return (
    <html lang="en" suppressHydrationWarning>
      <body className={inter.className}>
        <ThemeProvider>
          <FeatureFlagsProvider flags={flags}>
            <FaroProvider env={faroEnv} geo={geo}>
              <a href="#main" className="skip-link">Skip to main content</a>
              {children}
            </FaroProvider>
          </FeatureFlagsProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
