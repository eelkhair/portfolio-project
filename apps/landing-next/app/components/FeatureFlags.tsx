"use client";

import { createContext, useContext, ReactNode } from "react";

interface FeatureFlags {
  availableBadge: boolean;
  serviceStatus: boolean;
  demoChat: boolean;
}

const defaults: FeatureFlags = {
  availableBadge: false,
  serviceStatus: false,
  demoChat: true,
};

const FeatureFlagsContext = createContext<FeatureFlags>(defaults);

export function FeatureFlagsProvider({ children, flags }: { children: ReactNode; flags?: Partial<FeatureFlags> }) {
  const merged = { ...defaults, ...flags };
  return (
    <FeatureFlagsContext.Provider value={merged}>
      {children}
    </FeatureFlagsContext.Provider>
  );
}

export function useFeatureFlags() {
  return useContext(FeatureFlagsContext);
}

export function FeatureGate({ flag, children }: { flag: keyof FeatureFlags; children: ReactNode }) {
  const flags = useFeatureFlags();
  return flags[flag] ? <>{children}</> : null;
}
