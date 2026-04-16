import type { Metadata } from "next";
import { Header } from "../components/Header";
import { Footer } from "../components/Footer";
import { ContactForm } from "../components/ContactForm";

export const metadata: Metadata = {
  title: "Contact",
  description: "Get in touch with Elkhair Elkhair — Staff Software Engineer & Solutions Architect.",
};

const navLinks = [
  { href: "/", label: "Home" },
  { href: "/portfolio", label: "Portfolio" },
  { href: "/contact", label: "Contact" },
];

export default function ContactPage() {
  return (
    <>
      <Header links={navLinks} />
      <main id="main">
        <section className="contact-section" aria-labelledby="contact-heading">
          <div className="container-center">
            <h1 id="contact-heading" className="contact-heading">
              Get in <span className="accent">Touch</span>
            </h1>
            <p className="section-text" style={{ margin: "0 auto 2.5rem" }}>
              Questions, feedback, opportunities, or want to talk about software architecture, AI-enabled systems, <br/>or platform modernization?
              <br /><br/>Send me a message — I&apos;d be glad to connect.
            </p>
          </div>
          <div className="container">
            <ContactForm />
          </div>
        </section>
      </main>
      <Footer />
    </>
  );
}
