import { ContactFooterLink } from "./ContactGate";

export function Footer() {
  return (
    <footer>
      <p>
        <a href="https://github.com/eelkhair/portfolio-project" target="_blank" rel="noopener noreferrer">GitHub</a>
        {" \u00B7 "}
        <a href="https://www.linkedin.com/in/elkhair-elkhair/" target="_blank" rel="noopener noreferrer">LinkedIn</a>
        {" \u00B7 "}
        <a href="mailto:elkhair@elkhair.tech">elkhair@elkhair.tech</a>
        <ContactFooterLink />
      </p>
      <p className="mt-footer">Self-hosted on Proxmox &middot; Exposed via Cloudflare Tunnel &middot; Deployed with GitHub Actions</p>
    </footer>
  );
}
