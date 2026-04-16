import type { Metadata } from "next";
import { CaseStudyLayout, CsSection } from "../components/CaseStudyLayout";
import { adrs } from "../data/portfolio-data";

export const metadata: Metadata = {
  title: "Architecture Decision Records",
  description: "20 ADRs documenting every significant architectural decision with context, alternatives, and rationale.",
};

const toc = [
  { href: "#overview", label: "Overview" },
  { href: "#adrs", label: "All ADRs" },
];

export default function AdrsPage() {
  return (
    <CaseStudyLayout
      title="Architecture Decision Records"
      summary="20 ADRs documenting every significant architectural decision &mdash; context, alternatives considered, and rationale for each trade-off."
      toc={toc}
      prevLink={{ href: "/observability", label: "Previous: Observability" }}
      nextLink={{ href: "/portfolio", label: "Back to Portfolio" }}
    >
      <CsSection id="overview">
        <h2>Why ADRs Matter</h2>
        <p>Every significant architectural decision in this project is documented as an Architecture Decision Record. ADRs capture <em>why</em> a decision was made, not just what was built. They show how I think about trade-offs: what alternatives were considered, what constraints existed, and what the consequences are.</p>
        <p>These aren&apos;t retroactive documentation &mdash; they were written at the time each decision was made, forming a decision log that any team member could reference.</p>
      </CsSection>

      <CsSection id="adrs" alt>
        <h2>All Architecture Decision Records</h2>
        <div className="adr-list">
          {adrs.map((adr) => (
            <details className="adr-item" key={adr.number}>
              <summary>
                <span className="adr-number">{adr.number}</span>
                <div>
                  <div className="adr-title">{adr.title}</div>
                  <div className="adr-summary">{adr.summary}</div>
                </div>
              </summary>
              <div className="adr-content" dangerouslySetInnerHTML={{ __html: adr.content }} />
            </details>
          ))}
        </div>
      </CsSection>
    </CaseStudyLayout>
  );
}
