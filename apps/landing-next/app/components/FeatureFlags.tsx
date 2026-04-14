"use client";

import { createContext, useContext, ReactNode } from "react";

interface FeatureFlags {
  deepDives: boolean;
}

const defaults: FeatureFlags = {
  deepDives: false,
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
