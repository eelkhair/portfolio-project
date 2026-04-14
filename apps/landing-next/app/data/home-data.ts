export const aboutCards = [
  { icon: "\u{1F680}", title: "Founding Engineer", desc: "Built a SaaS product from zero to enterprise adoption at AgilityHealth. Defined architecture, hired the team, and scaled through six years of growth." },
  { icon: "\u{1F9E0}", title: "AI in Production", desc: "Designed resume parsing pipelines with LLMs, vector search with pgvector, and agentic tool-calling systems \u2014 not prototypes, production workloads." },
  { icon: "\u2699\uFE0F", title: "Systems at Scale", desc: "Outbox patterns processing 400K+ records, event-driven microservices, OAuth/SSO integrations across regulated healthcare platforms." },
  { icon: "\u{1F3D7}\uFE0F", title: "End-to-End Ownership", desc: "From Bicep IaC and CI/CD pipelines to Angular frontends. I own the full stack, including the infrastructure it runs on." },
  { icon: "\u{1F504}", title: "Legacy Modernization", desc: "Migrating monoliths to microservices incrementally using the Strangler-Fig pattern \u2014 no big-bang rewrites, no downtime, bidirectional sync until you're ready." },
  { icon: "\u{1F50D}", title: "Observability", desc: "Instrumenting distributed systems with OpenTelemetry, correlated logging, and end-to-end tracing \u2014 so every request is debuggable across every service." },
];

export const experienceItems = [
  {
    role: "Solutions Architect",
    meta: "Corteva (via Insight Global) \u00B7 Johnston, IA \u00B7 Jun 2025 \u2013 Present",
    bullets: [
      'Designed outbox-based architecture to reliably process <strong>~400K records</strong> with transactional consistency and zero data loss',
      'Stood up end-to-end observability (OpenTelemetry tracing, structured logging, dashboards) \u2014 first distributed tracing in the org',
      'Introduced feature flagging via Azure App Configuration, decoupling deployment from release for the first time',
    ],
  },
  {
    role: "Solutions Architect (Consultant)",
    meta: "Lunavi \u00B7 Omaha, NE \u00B7 Mar 2024 \u2013 Mar 2025",
    bullets: [
      'Led delivery of a <strong>Medicare Advantage member portal</strong> end-to-end across a cross-functional team',
      'Architected and shipped <strong>five SSO integrations</strong> (OAuth 2.0 / OIDC) for healthcare partner onboarding',
      'Built automated document ingestion pipeline with Azure Logic Apps, replacing a fully manual workflow',
    ],
  },
  {
    role: "Staff Backend Engineer",
    meta: "Medical Solutions \u00B7 Omaha, NE \u00B7 Feb 2023 \u2013 Mar 2024",
    bullets: [
      "Built the company's <strong>first AI-powered workflow</strong> \u2014 automated resume extraction pipeline using OpenAI",
      'Led DocuSign eSignature integration, eliminating manual contract processing',
    ],
  },
  {
    role: "Lead Backend Engineer (Consultant)",
    meta: "AgilityHealth \u00B7 Omaha, NE \u00B7 Jun 2023 \u2013 Oct 2023",
    bullets: [
      'Returned to the company I helped build \u2014 led architectural modernization from monolith to microservices',
      'Implemented distributed tracing and centralized logging across the platform',
    ],
  },
  {
    role: "Staff Software Engineer",
    meta: "Fusion Medical Staffing \u00B7 Omaha, NE \u00B7 Sep 2021 \u2013 Feb 2023",
    bullets: [
      'Led org-wide auth migration from IdentityServer4 to Auth0 across <strong>multiple teams and services</strong>',
      'Proved out event-driven microservices architecture with Cosmos DB change feed + RabbitMQ',
      'Ran engineering workshops on query optimization, directly improving platform performance',
    ],
  },
  {
    role: "Senior Backend Software Engineer",
    meta: "Fusion Medical Staffing \u00B7 Omaha, NE \u00B7 Jan 2020 \u2013 Sep 2021",
    bullets: [
      'Built centralized IdentityServer4 auth serving multiple API gateways and web clients',
      'Partnered with DevOps to establish CI/CD pipelines, cutting deploy times significantly',
    ],
  },
  {
    role: "Founding Engineer",
    meta: "Agile Transformation Inc (AgilityHealth) \u00B7 Omaha, NE \u00B7 Nov 2013 \u2013 Oct 2019",
    bullets: [
      '<strong>First engineer hired.</strong> Designed and built the entire product from an empty repository to production',
      'Defined architecture, coding standards, and engineering culture that supported <strong>Fortune 500 clients</strong>',
      'Grew the engineering team \u2014 hired, mentored, and established delivery processes from scratch',
      'Led six years of platform evolution from startup MVP to enterprise-grade SaaS',
    ],
  },
];

export const skillCategories = [
  {
    title: "Architecture & Backend",
    skills: ["C# / .NET", "ASP.NET Core", "TypeScript", "Node.js", "Clean Architecture", "DDD", "CQRS", "Microservices", "Outbox Pattern", "Saga Orchestration", "Dapr", "RabbitMQ", "SignalR"],
  },
  {
    title: "AI & Data",
    skills: ["OpenAI", "Function Calling", "Embeddings", "PostgreSQL / pgvector", "SQL Server", "CosmosDB", "Redis", "EF Core", "Dapper"],
  },
  {
    title: "Cloud & Platform",
    skills: ["Microsoft Azure", "Docker", "Proxmox", "OpenTelemetry", "Grafana", "Jaeger", "Azure Logic Apps", "Bicep IaC", "GitHub Actions"],
  },
  {
    title: "Identity & Frontend",
    skills: ["Keycloak", "OAuth 2.0 / OIDC", "Auth0", "IdentityServer4", "Angular", "React", "Tailwind CSS"],
  },
];
