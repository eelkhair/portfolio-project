import Image from "next/image";
import Link from "next/link";
import { Header } from "./components/Header";
import { Footer } from "./components/Footer";
import { BookIcon, GitHubIcon, LinkedInIcon } from "./components/Icons";
import { aboutCards, experienceItems, skillCategories } from "./data/home-data";
import { AvailableBadge } from "./components/AvailableBadge";
import { ContactNavLink } from "./components/ContactGate";

const homeNavLinks = [
  { href: "#about", label: "About" },
  { href: "#experience", label: "Experience" },
  { href: "#skills", label: "Skills" },
  { href: "#education", label: "Education" },
  { href: "/portfolio", label: "Portfolio" },
];

export default function Home() {
  return (
    <>
      <Header links={homeNavLinks} dropdownSlot={<ContactNavLink />} />
      <main id="main">
        {/* Hero */}
        <section className="hero" id="home" aria-labelledby="hero-heading">
          <div className="hero-inner">
            <Image
              src="/profile.jpg"
              alt="Elkhair Elkhair"
              width={180}
              height={180}
              className="hero-photo"
              priority
            />
            <div className="hero-content">
              <AvailableBadge />
              <h1 id="hero-heading">I build systems that <span className="accent">scale</span></h1>
              <p className="subtitle">
                I&apos;m a staff-level engineer and Founding Engineer with 13+ years of experience building production systems across distributed platforms, observability, and AI integration.
              </p>
              <p className="subtitle">
                My work is grounded in software that needs to scale, evolve, and remain clear and observable in production.
              </p>
              <div className="hero-links">
                <Link href="/portfolio" className="btn btn-primary"><BookIcon /> See My Work</Link>
                <a href="https://github.com/eelkhair/portfolio-project" target="_blank" rel="noopener noreferrer" className="btn btn-outline"><GitHubIcon /> GitHub</a>
                <a href="https://www.linkedin.com/in/elkhair-elkhair/" target="_blank" rel="noopener noreferrer" className="btn btn-outline"><LinkedInIcon /> LinkedIn</a>
              </div>
            </div>
          </div>
        </section>

        {/* About */}
        <section id="about" aria-labelledby="about-heading">
          <p className="section-title">About</p>
          <h2 id="about-heading">What I Bring to the Table</h2>
          <p className="section-text">
            I joined a startup as the first engineer, built the product from an empty repo, and stayed through
            Fortune 500 adoption. That experience shaped how I think about software &mdash; every system I design
            is built to evolve, not just to ship.
          </p>
          <p className="section-text mb-5">
            Today I architect distributed platforms in .NET and Azure, integrate AI into production workflows,
            and lead teams through complex migrations &mdash; all while staying hands-on with the code.
            SOC2, GDPR, and observability aren&apos;t afterthoughts; they&apos;re wired in from day one.
          </p>

          <div className="card-grid">
            {aboutCards.map((card) => (
              <div className="card" key={card.title}>
                <div className="card-icon" role="img" aria-hidden="true">{card.icon}</div>
                <h3>{card.title}</h3>
                <p>{card.desc}</p>
              </div>
            ))}
          </div>
        </section>

        {/* Experience */}
        <section id="experience" aria-labelledby="experience-heading">
          <p className="section-title">Experience</p>
          <h2 id="experience-heading">Professional Experience</h2>
          <div className="timeline">
            {experienceItems.map((item, i) => (
              <div className="timeline-item" key={i}>
                <div className="timeline-role">{item.role}</div>
                <div className="timeline-meta">{item.meta}</div>
                <div className="timeline-desc">
                  <ul>
                    {item.bullets.map((b, j) => (
                      <li key={j} dangerouslySetInnerHTML={{ __html: b }} />
                    ))}
                  </ul>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Skills */}
        <section id="skills" aria-labelledby="skills-heading">
          <p className="section-title">Skills</p>
          <h2 id="skills-heading">Technical Skills</h2>
          <div className="skill-grid">
            {skillCategories.map((cat) => (
              <div className="skill-category" key={cat.title}>
                <h3>{cat.title}</h3>
                <div className="skill-tags">
                  {cat.skills.map((s) => (
                    <span className="skill-tag" key={s}>{s}</span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Portfolio CTA */}
        <section id="portfolio" className="section-alt" aria-labelledby="portfolio-heading">
          <div className="container-center">
            <span className="hero-label">Live System &mdash; 16 Containers, Fully Deployed</span>
            <h2 id="portfolio-heading" className="portfolio-cta-title">This isn&apos;t a tutorial project</h2>
            <p className="portfolio-cta-text">
              A distributed job board platform with AI-powered resume parsing, vector matching,
              event-driven microservices, and full observability &mdash; running in production on my own infrastructure.
            </p>
            <p className="section-meta">
              20 ADRs &middot; Saga orchestration &middot; Strangler-fig migration &middot; Dapr &middot; OpenTelemetry &middot; Bicep IaC &middot; GitHub Actions CI/CD
            </p>
            <div className="coming-soon-banner" role="status">
              <span className="coming-soon-icon" aria-hidden="true">{"\u2728"}</span>
              <div>
                <div className="coming-soon-title">The demo is live — register and try it out</div>
                <div className="coming-soon-desc">
                  Create an account on the{" "}
                  <a href="https://job-admin.elkhair.tech/signup" target="_blank" rel="noopener noreferrer" className="service-register-link">Admin app</a>
                  {" "}to post jobs and explore the admin experience, or on the{" "}
                  <a href="https://jobs.elkhair.tech/signup" target="_blank" rel="noopener noreferrer" className="service-register-link">Public app</a>
                  {" "}to apply as a candidate. Feel free to poke around.
                </div>
              </div>
            </div>
            <Link href="/portfolio" className="btn btn-primary btn-lg"><BookIcon /> Explore the Architecture</Link>
          </div>
        </section>

        {/* Education */}
        <section id="education" aria-labelledby="education-heading">
          <p className="section-title">Education &amp; Community</p>
          <h2 id="education-heading">Education</h2>
          <div className="edu-item">
            <div className="edu-degree">B.A. in Informatics (Man-Machine Systems)</div>
            <div className="edu-school">The University of Iowa, Iowa City, IA &middot; December 2009</div>
          </div>
          <div className="mt-3">
            <h3 className="mb-1">Community</h3>
            <ul className="community-list">
              <li>Member, Association for Computing Machinery (ACM) &mdash; 2008 to Present</li>
              <li>Member, Sudanese Community Services Inc., Iowa City &mdash; 2009 to Present</li>
            </ul>
          </div>
        </section>
      </main>
      <Footer />
    </>
  );
}
