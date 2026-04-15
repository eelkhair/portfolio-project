export interface FeatureFlagsDto {
  [key: string]: boolean;
}

export interface FeatureFlags {
  deepDives: boolean;
}

const FLAG_URL = process.env.FEATURE_FLAGS_URL || "http://localhost:5280/api/public/feature-flags";

const defaults: FeatureFlags = {
  deepDives: false,
};

export async function fetchFeatureFlags(): Promise<FeatureFlags> {
  try {
    const res = await fetch(FLAG_URL, { next: { revalidate: 10 } });
    if (!res.ok) return defaults;
    const data: FeatureFlagsDto = await res.json();
    const flags: FeatureFlags = {
      deepDives: data.DeepDives ?? defaults.deepDives,
    };
    console.log("[FeatureFlags] fetched:", flags);
    return flags;
  } catch {
    return defaults;
  }
}
