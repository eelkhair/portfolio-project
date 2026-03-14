from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.lib.colors import HexColor, black, white
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    PageBreak, KeepTogether
)

OUTPUT_PATH = "Job Board Platform - Future Microservices Architecture.pdf"

def build_pdf():
    doc = SimpleDocTemplate(
        OUTPUT_PATH,
        pagesize=letter,
        topMargin=0.75 * inch,
        bottomMargin=0.75 * inch,
        leftMargin=0.75 * inch,
        rightMargin=0.75 * inch,
    )

    styles = getSampleStyleSheet()

    # Custom styles
    title_style = ParagraphStyle(
        "DocTitle", parent=styles["Title"],
        fontSize=22, leading=28, spaceAfter=6,
        textColor=HexColor("#1a1a2e"),
    )
    subtitle_style = ParagraphStyle(
        "DocSubtitle", parent=styles["Normal"],
        fontSize=11, leading=14, spaceAfter=24,
        textColor=HexColor("#555555"), alignment=TA_CENTER,
    )
    h1 = ParagraphStyle(
        "H1", parent=styles["Heading1"],
        fontSize=16, leading=20, spaceBefore=18, spaceAfter=10,
        textColor=HexColor("#1a1a2e"),
    )
    h2 = ParagraphStyle(
        "H2", parent=styles["Heading2"],
        fontSize=13, leading=16, spaceBefore=14, spaceAfter=8,
        textColor=HexColor("#2d3436"),
    )
    body = ParagraphStyle(
        "Body", parent=styles["Normal"],
        fontSize=10, leading=14, spaceAfter=8,
    )
    mono = ParagraphStyle(
        "Mono", parent=styles["Code"],
        fontSize=8, leading=11, spaceAfter=8,
        fontName="Courier", backColor=HexColor("#f5f5f5"),
        leftIndent=12, rightIndent=12,
        borderPadding=(8, 8, 8, 8),
    )
    table_header_style = ParagraphStyle(
        "TableHeader", parent=styles["Normal"],
        fontSize=9, leading=12, textColor=white, fontName="Helvetica-Bold",
    )
    table_cell_style = ParagraphStyle(
        "TableCell", parent=styles["Normal"],
        fontSize=9, leading=12,
    )

    story = []

    # ── Title page ──
    story.append(Spacer(1, 1.5 * inch))
    story.append(Paragraph("Job Board Platform", title_style))
    story.append(Paragraph("Future Microservices Architecture", ParagraphStyle(
        "DocTitle2", parent=title_style, fontSize=18, leading=22, spaceAfter=12,
        textColor=HexColor("#4a4a6a"),
    )))
    story.append(Spacer(1, 12))
    story.append(Paragraph("Distributed Architecture Plan  &bull;  March 2026", subtitle_style))
    story.append(Spacer(1, 0.3 * inch))
    story.append(Paragraph(
        "This document outlines the target microservices architecture for the Job Board platform, "
        "mapping monolith endpoints to new bounded-context services and defining the event-driven "
        "communication patterns between them.",
        ParagraphStyle("BodyCenter", parent=body, alignment=TA_CENTER, textColor=HexColor("#555555")),
    ))
    story.append(PageBreak())

    # ── Section 1: Current State ──
    story.append(Paragraph("1. Current State", h1))
    story.append(Paragraph(
        "The platform currently runs as a monolith with a parallel set of microservices covering "
        "the <b>admin</b> bounded context (companies, jobs, drafts, settings). A Strangler-Fig "
        "connector API routes traffic between the two topologies based on a feature flag.",
        body,
    ))

    story.append(Paragraph("<b>Existing Services</b>", h2))
    existing_data = [
        [Paragraph("Service", table_header_style),
         Paragraph("Bounded Context", table_header_style),
         Paragraph("Status", table_header_style)],
        [Paragraph("admin-api", table_cell_style),
         Paragraph("Gateway / proxy for admin UI", table_cell_style),
         Paragraph("Exists", table_cell_style)],
        [Paragraph("job-api", table_cell_style),
         Paragraph("Jobs + Drafts persistence", table_cell_style),
         Paragraph("Exists", table_cell_style)],
        [Paragraph("company-api", table_cell_style),
         Paragraph("Companies + Industries", table_cell_style),
         Paragraph("Exists", table_cell_style)],
        [Paragraph("user-api", table_cell_style),
         Paragraph("Keycloak provisioning + Users", table_cell_style),
         Paragraph("Exists", table_cell_style)],
        [Paragraph("ai-service-v2", table_cell_style),
         Paragraph("LLM chat, embeddings, RAG search", table_cell_style),
         Paragraph("Exists", table_cell_style)],
    ]
    t = Table(existing_data, colWidths=[1.8 * inch, 3.0 * inch, 1.2 * inch])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), white),
        ("BACKGROUND", (0, 1), (-1, -1), HexColor("#f8f9fa")),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [HexColor("#ffffff"), HexColor("#f8f9fa")]),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#dee2e6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 6),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
        ("LEFTPADDING", (0, 0), (-1, -1), 8),
        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
    ]))
    story.append(t)
    story.append(Spacer(1, 12))

    # ── Section 2: Architecture Diagram ──
    story.append(Paragraph("2. Target Architecture", h1))
    story.append(Paragraph(
        "The diagram below shows the full target state with four new services "
        "(highlighted) completing the migration from the monolith.",
        body,
    ))

    diagram = """
    CLIENTS
    +----------------+  +----------------+  +----------------+
    |   Admin SPA    |  |  Public SSR    |  | Applicant SPA  |
    |   (Angular)    |  |  (Angular)     |  |                |
    +-------+--------+  +-------+--------+  +-------+--------+
            |                    |                    |
            v                    v                    v
    API GATEWAYS
    +----------------+  +----------------+  +----------------+
    |   admin-api    |  |  public-api    |  | applicant-api  |
    |   (exists)     |  |    (NEW)       |  |    (NEW)       |
    |   Admin CRUD   |  |  Read-only BFF |  | Applicant BFF  |
    +-------+--------+  +-------+--------+  +-------+--------+
            |                    |                    |
            v                    v                    v
    DOMAIN SERVICES
    +----------------+  +----------------+  +----------------+
    |  company-api   |  |   job-api      |  |  resume-api    |
    |  (exists)      |  |  (exists)      |  |    (NEW)       |
    |  Companies     |  |  Jobs, Drafts  |  |  Upload/Parse  |
    |  Industries    |  |                |  |  Blob Storage  |
    +----------------+  +----------------+  +----------------+

    +----------------+  +----------------+
    |   user-api     |  | application-api|
    |  (exists)      |  |    (NEW)       |
    |  Keycloak      |  |  Profiles      |
    |  Provisioning  |  |  Applications  |
    +----------------+  +----------------+

    INFRASTRUCTURE
    +----------------+  +----------------+  +----------------+
    |  ai-service    |  |   RabbitMQ     |  |    Redis       |
    |  LLM/Embed/RAG |  |   (pub/sub)    |  |   (config)     |
    +----------------+  +----------------+  +----------------+
    """

    for line in diagram.strip().split("\n"):
        story.append(Paragraph(line, mono))

    story.append(PageBreak())

    # ── Section 3: New Services Breakdown ──
    story.append(Paragraph("3. New Services Breakdown", h1))

    services_data = [
        [Paragraph("Service", table_header_style),
         Paragraph("Bounded Context", table_header_style),
         Paragraph("Monolith Endpoints Absorbed", table_header_style),
         Paragraph("Database", table_header_style)],
        [Paragraph("<b>public-api</b>", table_cell_style),
         Paragraph("Read-optimized BFF for public website", table_cell_style),
         Paragraph("GET /public/jobs/*, GET /public/companies/*, GET /public/stats", table_cell_style),
         Paragraph("None (proxies to job-api, company-api)", table_cell_style)],
        [Paragraph("<b>applicant-api</b>", table_cell_style),
         Paragraph("Applicant-facing BFF", table_cell_style),
         Paragraph("GET/PUT /applicant/profile, GET/POST /applicant/applications", table_cell_style),
         Paragraph("None (proxies to application-api, user-api)", table_cell_style)],
        [Paragraph("<b>application-api</b>", table_cell_style),
         Paragraph("Applications domain service", table_cell_style),
         Paragraph("Applicant profiles, job applications, application tracking", table_cell_style),
         Paragraph("Own SQL DB (Profiles, Applications)", table_cell_style)],
        [Paragraph("<b>resume-api</b>", table_cell_style),
         Paragraph("Resume lifecycle service", table_cell_style),
         Paragraph("POST/GET/DELETE /resumes, download, re-embed, parse callbacks", table_cell_style),
         Paragraph("Own SQL DB (Resumes) + Blob Storage", table_cell_style)],
    ]
    t2 = Table(services_data, colWidths=[1.2 * inch, 1.5 * inch, 2.2 * inch, 1.6 * inch])
    t2.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), white),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [HexColor("#ffffff"), HexColor("#f8f9fa")]),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#dee2e6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 6),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
    ]))
    story.append(t2)
    story.append(Spacer(1, 16))

    # ── Section 4: Monolith Endpoint Gap Analysis ──
    story.append(Paragraph("4. Monolith Endpoint Gap Analysis", h1))
    story.append(Paragraph(
        "The following monolith endpoints have no microservice equivalent today. "
        "Each maps to one of the four new services above.",
        body,
    ))

    story.append(Paragraph("Public Endpoints (9 endpoints)", h2))
    public_eps = [
        [Paragraph("Method", table_header_style),
         Paragraph("Route", table_header_style),
         Paragraph("Target Service", table_header_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/jobs", table_cell_style), Paragraph("public-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/jobs/{id}", table_cell_style), Paragraph("public-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/jobs/{id}/similar", table_cell_style), Paragraph("public-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/jobs/search", table_cell_style), Paragraph("public-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/jobs/latest", table_cell_style), Paragraph("public-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/companies", table_cell_style), Paragraph("public-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/companies/{id}", table_cell_style), Paragraph("public-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/companies/{id}/jobs", table_cell_style), Paragraph("public-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/public/stats", table_cell_style), Paragraph("public-api", table_cell_style)],
    ]
    t3 = Table(public_eps, colWidths=[0.8 * inch, 3.0 * inch, 2.0 * inch])
    t3.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#2d3436")),
        ("TEXTCOLOR", (0, 0), (-1, 0), white),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [HexColor("#ffffff"), HexColor("#f8f9fa")]),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#dee2e6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
    ]))
    story.append(t3)
    story.append(Spacer(1, 10))

    story.append(Paragraph("Applicant Endpoints (4 endpoints)", h2))
    applicant_eps = [
        [Paragraph("Method", table_header_style),
         Paragraph("Route", table_header_style),
         Paragraph("Target Service", table_header_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/applicant/profile", table_cell_style), Paragraph("applicant-api &rarr; application-api", table_cell_style)],
        [Paragraph("PUT", table_cell_style), Paragraph("/applicant/profile", table_cell_style), Paragraph("applicant-api &rarr; application-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/applicant/applications", table_cell_style), Paragraph("applicant-api &rarr; application-api", table_cell_style)],
        [Paragraph("POST", table_cell_style), Paragraph("/applicant/applications", table_cell_style), Paragraph("applicant-api &rarr; application-api", table_cell_style)],
    ]
    t4 = Table(applicant_eps, colWidths=[0.8 * inch, 3.0 * inch, 2.0 * inch])
    t4.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#2d3436")),
        ("TEXTCOLOR", (0, 0), (-1, 0), white),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [HexColor("#ffffff"), HexColor("#f8f9fa")]),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#dee2e6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
    ]))
    story.append(t4)
    story.append(Spacer(1, 10))

    story.append(Paragraph("Resume Endpoints (8+ endpoints)", h2))
    resume_eps = [
        [Paragraph("Method", table_header_style),
         Paragraph("Route", table_header_style),
         Paragraph("Target Service", table_header_style)],
        [Paragraph("POST", table_cell_style), Paragraph("/resumes", table_cell_style), Paragraph("resume-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/resumes", table_cell_style), Paragraph("resume-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/resumes/{id}/parsed-content", table_cell_style), Paragraph("resume-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/resumes/{id}/download", table_cell_style), Paragraph("resume-api", table_cell_style)],
        [Paragraph("PATCH", table_cell_style), Paragraph("/resumes/{id}/default", table_cell_style), Paragraph("resume-api", table_cell_style)],
        [Paragraph("DELETE", table_cell_style), Paragraph("/resumes/{id}", table_cell_style), Paragraph("resume-api", table_cell_style)],
        [Paragraph("GET", table_cell_style), Paragraph("/resumes/jobs/matching", table_cell_style), Paragraph("resume-api &rarr; ai-service", table_cell_style)],
        [Paragraph("POST", table_cell_style), Paragraph("/resumes/{id}/re-embed", table_cell_style), Paragraph("resume-api &rarr; ai-service", table_cell_style)],
    ]
    t5 = Table(resume_eps, colWidths=[0.8 * inch, 3.0 * inch, 2.0 * inch])
    t5.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#2d3436")),
        ("TEXTCOLOR", (0, 0), (-1, 0), white),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [HexColor("#ffffff"), HexColor("#f8f9fa")]),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#dee2e6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
    ]))
    story.append(t5)

    story.append(PageBreak())

    # ── Section 5: Event Flow ──
    story.append(Paragraph("5. Event-Driven Communication", h1))
    story.append(Paragraph(
        "All asynchronous communication uses RabbitMQ via Dapr pub/sub. "
        "Events follow the <b>EventDto&lt;T&gt;</b> envelope pattern from the shared library.",
        body,
    ))

    event_data = [
        [Paragraph("Publisher", table_header_style),
         Paragraph("Event", table_header_style),
         Paragraph("Subscriber(s)", table_header_style)],
        [Paragraph("resume-api", table_cell_style),
         Paragraph("ResumeUploadedV1Event", table_cell_style),
         Paragraph("ai-service (parse + embed)", table_cell_style)],
        [Paragraph("ai-service", table_cell_style),
         Paragraph("ResumeParsedV1Event", table_cell_style),
         Paragraph("resume-api (update status)", table_cell_style)],
        [Paragraph("ai-service", table_cell_style),
         Paragraph("ResumeDeletedV1Event", table_cell_style),
         Paragraph("resume-api (cleanup)", table_cell_style)],
        [Paragraph("job-api", table_cell_style),
         Paragraph("JobCreatedV1Event", table_cell_style),
         Paragraph("ai-service (embed job)", table_cell_style)],
        [Paragraph("admin-api", table_cell_style),
         Paragraph("company.created", table_cell_style),
         Paragraph("company-api, job-api, user-api", table_cell_style)],
        [Paragraph("user-api", table_cell_style),
         Paragraph("provision.user.success", table_cell_style),
         Paragraph("admin-api (activate company)", table_cell_style)],
    ]
    t6 = Table(event_data, colWidths=[1.5 * inch, 2.2 * inch, 2.8 * inch])
    t6.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), white),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [HexColor("#ffffff"), HexColor("#f8f9fa")]),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#dee2e6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 6),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
        ("LEFTPADDING", (0, 0), (-1, -1), 8),
        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
    ]))
    story.append(t6)
    story.append(Spacer(1, 16))

    # ── Section 6: Key Design Decisions ──
    story.append(Paragraph("6. Key Design Decisions", h1))

    decisions = [
        (
            "public-api as a Read-Only BFF",
            "No own database. Aggregates responses from job-api and company-api via Dapr invocation. "
            "Can add a Redis cache layer for high-traffic read optimization without touching domain services."
        ),
        (
            "application-api Owns Applicant Profiles",
            "Separates the applicant domain (profiles, applications, tracking) from user identity "
            "(user-api handles Keycloak only). This keeps the user-api focused on provisioning and "
            "avoids coupling identity management with business logic."
        ),
        (
            "resume-api Orchestrates the Pipeline",
            "Owns blob storage interaction and resume metadata. The AI service remains the "
            "parser/embedder, communicating via events. This keeps the AI service stateless "
            "(no resume storage) while resume-api manages the lifecycle."
        ),
        (
            "Each Gateway Maps to a Client App",
            "admin-api serves the Admin SPA, public-api serves the Public SSR app, and "
            "applicant-api serves the Applicant SPA. This BFF-per-frontend pattern allows each "
            "gateway to tailor its API surface to its client's needs."
        ),
        (
            "Strangler-Fig Continuity",
            "The connector API continues to route between monolith and microservices based on "
            "FeatureFlags:Monolith. New services follow the same pattern, allowing incremental "
            "migration without big-bang cutover."
        ),
    ]

    for title, description in decisions:
        story.append(KeepTogether([
            Paragraph(f"<b>{title}</b>", h2),
            Paragraph(description, body),
        ]))

    # ── Section 7: Implementation Priority ──
    story.append(Spacer(1, 8))
    story.append(Paragraph("7. Recommended Implementation Order", h1))

    priority_data = [
        [Paragraph("Priority", table_header_style),
         Paragraph("Service", table_header_style),
         Paragraph("Rationale", table_header_style)],
        [Paragraph("1", table_cell_style),
         Paragraph("<b>public-api</b>", table_cell_style),
         Paragraph("Read-only BFF, lowest risk. Angular public app already exists and needs API parity.", table_cell_style)],
        [Paragraph("2", table_cell_style),
         Paragraph("<b>resume-api</b>", table_cell_style),
         Paragraph("Resume pipeline already event-driven. Extracting blob storage + metadata is well-bounded.", table_cell_style)],
        [Paragraph("3", table_cell_style),
         Paragraph("<b>application-api</b>", table_cell_style),
         Paragraph("Applicant profiles and applications are a clean bounded context with few external dependencies.", table_cell_style)],
        [Paragraph("4", table_cell_style),
         Paragraph("<b>applicant-api</b>", table_cell_style),
         Paragraph("BFF layer added last, once application-api and resume-api backends are stable.", table_cell_style)],
    ]
    t7 = Table(priority_data, colWidths=[0.8 * inch, 1.5 * inch, 4.2 * inch])
    t7.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), white),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [HexColor("#ffffff"), HexColor("#f8f9fa")]),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#dee2e6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 6),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
        ("LEFTPADDING", (0, 0), (-1, -1), 8),
        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
    ]))
    story.append(t7)

    doc.build(story)
    print(f"PDF generated: {OUTPUT_PATH}")

if __name__ == "__main__":
    build_pdf()
