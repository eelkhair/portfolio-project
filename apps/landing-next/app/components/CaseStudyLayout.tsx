import Image from "next/image";
import Link from "next/link";
import { Header } from "./Header";
import { Footer } from "./Footer";
import { DeepDivesDropdown } from "./DeepDivesSection";

const caseStudyNavLinks = [
  { href: "/", label: "About Me" },
  { href: "/portfolio", label: "Portfolio" },
  { href: "/contact", label: "Contact" },
];

interface TocItem {
  href: string;
  label: string;
}

interface Decision {
  title: string;
  content: string;
}

interface CaseStudyProps {
  title: string;
  summary: string;
  toc: TocItem[];
  children: React.ReactNode;
  prevLink?: { href: string; label: string };
  nextLink?: { href: string; label: string };
}

export function CaseStudyLayout({ title, summary, toc, children, prevLink, nextLink }: CaseStudyProps) {
  return (
    <>
      <Header links={caseStudyNavLinks} dropdownSlot={<DeepDivesDropdown />} />
      <main id="main">
        <section className="cs-hero" aria-labelledby="cs-heading">
          <p className="section-title">Case Study</p>
          <h1 id="cs-heading" className="cs-title">{title}</h1>
          <p className="cs-summary">{summary}</p>
          <nav className="cs-toc" aria-label="Page sections">
            {toc.map((item) => (
              <a href={item.href} key={item.href}>{item.label}</a>
            ))}
          </nav>
        </section>

        {children}

        <nav className="cs-bottom-nav" aria-label="Case study navigation">
          {prevLink ? (
            <Link href={prevLink.href} className="btn btn-outline">{prevLink.label}</Link>
          ) : <span />}
          {nextLink ? (
            <Link href={nextLink.href} className="btn btn-primary">{nextLink.label}</Link>
          ) : <span />}
        </nav>
      </main>
      <Footer />
    </>
  );
}

export function CsSection({ id, children, alt }: { id: string; children: React.ReactNode; alt?: boolean }) {
  return (
    <section id={id} className={`cs-section${alt ? " section-alt" : ""}`}>
      {children}
    </section>
  );
}

export function CsDecisionGrid({ decisions }: { decisions: Decision[] }) {
  return (
    <div className="cs-decision-grid">
      {decisions.map((d) => (
        <div className="cs-decision" key={d.title}>
          <h3>{d.title}</h3>
          <div dangerouslySetInnerHTML={{ __html: d.content }} />
        </div>
      ))}
    </div>
  );
}

export function CsTradeoffList({ items }: { items: string[] }) {
  return (
    <ul className="cs-tradeoff-list">
      {items.map((item, i) => (
        <li key={i} dangerouslySetInnerHTML={{ __html: item }} />
      ))}
    </ul>
  );
}

export function CsPlaceholder({ text, caption }: { text: string; caption: string }) {
  return (
    <figure className="cs-figure">
      <div className="cs-placeholder">{text}</div>
      <figcaption>{caption}</figcaption>
    </figure>
  );
}

export function CsScreenshot({ src, alt, caption }: { src: string; alt: string; caption: string }) {
  return (
    <figure className="cs-figure cs-figure-screenshot">
      <a href={src} target="_blank" rel="noopener noreferrer" className="cs-screenshot-link">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={src} alt={alt} className="cs-screenshot-img" />
      </a>
      <figcaption>{caption}</figcaption>
    </figure>
  );
}
