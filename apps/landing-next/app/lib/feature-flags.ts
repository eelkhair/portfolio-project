export interface FeatureFlagsDto {
  [key: string]: boolean;
}

export interface FeatureFlags {
  deepDives: boolean;
  availableBadge: boolean;
}

const FLAG_URL = process.env.FEATURE_FLAGS_URL || "http://localhost:5280/api/public/feature-flags";

const defaults: FeatureFlags = {
  deepDives: false,
  availableBadge: true,
};

export async function fetchFeatureFlags(): Promise<FeatureFlags> {
  try {
    const res = await fetch(FLAG_URL, { cache: "no-store" });
    if (!res.ok) return defaults;
    const data: FeatureFlagsDto = await res.json();
    const normalized = Object.fromEntries(
      Object.entries(data).map(([k, v]) => [k.toLowerCase(), v])
    );
    const flags: FeatureFlags = {
      deepDives: normalized["deepdives"] ?? defaults.deepDives,
      availableBadge: normalized["availablebadge"] ?? defaults.availableBadge,
    };
    console.log("[FeatureFlags] fetched:", flags);
    return flags;
  } catch {
    return defaults;
  }
}
