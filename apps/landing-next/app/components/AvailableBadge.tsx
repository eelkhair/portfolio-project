"use client";

import { FeatureGate } from "./FeatureFlags";

export function AvailableBadge() {
  return (
    <FeatureGate flag="availableBadge">
      <span className="hero-label">Available for Staff, Principal, and Architect roles</span>
    </FeatureGate>
  );
}
