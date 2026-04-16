"use client";

import { useFeatureFlags } from "./FeatureFlags";
import { ContactForm } from "./ContactForm";

export function ContactGate() {
  const flags = useFeatureFlags();

  if (!flags.contactForm) {
    return (
      <div className="contact-success">
        <h2>Contact form coming soon</h2>
        <p>
          In the meantime, reach me at{" "}
          <a href="mailto:elkhair@elkhair.tech" style={{ color: "var(--accent)" }}>
            elkhair@elkhair.tech
          </a>
        </p>
      </div>
    );
  }

  return <ContactForm />;
}

export function ContactNavLink() {
  const flags = useFeatureFlags();
  if (!flags.contactForm) return null;
  return (
    <li>
      <a href="/contact">Contact</a>
    </li>
  );
}

export function ContactFooterLink() {
  const flags = useFeatureFlags();
  if (!flags.contactForm) return null;
  return (
    <>
      {" \u00B7 "}
      <a href="/contact">Contact</a>
    </>
  );
}
