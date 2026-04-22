import Link from "next/link";
import { ContactForm } from "./ContactForm";

export function ContactGate() {
  return <ContactForm />;
}

export function ContactNavLink() {
  return (
    <li>
      <Link href="/contact">Contact</Link>
    </li>
  );
}

export function ContactFooterLink() {
  return (
    <>
      {" \u00B7 "}
      <Link href="/contact">Contact</Link>
    </>
  );
}
