import type { Metadata } from "next";
import { CaseStudyLayout, CsSection, CsDecisionGrid, CsTradeoffList, CsScreenshot } from "../components/CaseStudyLayout";

// Required by @cloudflare/next-on-pages: every SSR route must opt into edge runtime.
export const runtime = "edge";

export const metadata: Metadata = {
  title: "AI Orchestration",
  description: "Multi-provider LLM chat with scoped tool registries, MCP server integration, and an event-driven resume RAG pipeline.",
};

const toc = [
  { href: "#problem", label: "Problem" },
  { href: "#solution", label: "Solution" },
  { href: "#architecture", label: "Architecture" },
  { href: "#user-view", label: "What You See" },
  { href: "#behind-scenes", label: "Behind the Scenes" },
  { href: "#decisions", label: "Key Decisions" },
  { href: "#tradeoffs", label: "Tradeoffs" },
];

export default function AiOrchestrationPage() {
  return (
    <CaseStudyLayout
      title="AI Orchestration"
      summary="Multi-provider LLM chat with scoped tool registries, MCP server integration, and an event-driven resume RAG pipeline."
      toc={toc}
      prevLink={{ href: "/architecture", label: "Previous: Architecture" }}
      nextLink={{ href: "/observability", label: "Next: Observability" }}
    >
      <CsSection id="problem">
        <h2>The Problem</h2>
        <p>Most AI integrations are tightly coupled to a single provider. Swap OpenAI for Claude and you&apos;re rewriting the integration layer. Tools are hardcoded in the AI service, so adding a new capability means redeploying the whole thing.</p>
        <p>Meanwhile, resume parsing that blocks the upload request creates a poor user experience. The user stares at a spinner while an LLM processes their document &mdash; or worse, the request times out.</p>
      </CsSection>

      <CsSection id="solution" alt>
        <h2>The Solution</h2>
        <p>A provider-agnostic AI service built on <strong>Microsoft.Extensions.AI</strong>. The abstraction layer means switching between OpenAI, Claude, and Gemini is a configuration change, not a code change. Each provider is registered as a keyed <code>IChatClient</code> singleton.</p>
        <p>Tools aren&apos;t hardcoded &mdash; they&apos;re discovered at runtime via the <strong>Model Context Protocol (MCP)</strong>. The monolith and microservices each expose their own MCP server. The AI service connects to whichever one matches the user&apos;s current session mode, discovering available tools dynamically.</p>
        <p>Resume processing is fully async. Upload triggers an event, a background handler downloads and parses the resume, generates embeddings, and stores them in pgvector. The user gets real-time progress updates via SignalR.</p>
        <CsScreenshot src="/images/ai-chat-tool-calling.png" alt="AI chat creating a company with sequence diagram showing the full saga flow across 8 services" caption="AI assistant creating a company via function calling — the sequence diagram shows the full request flow across gateway, AI service, MCP server, and connector saga (2115ms end-to-end)" />
      </CsSection>

      <CsSection id="architecture">
        <h2>Architecture</h2>
        <p>The AI service uses a <strong>scoped tool registry</strong> pattern. Four chat scopes (SystemAdmin, Admin, CompanyAdmin, Public) each get their own tool set. A <code>ChatOptionsFactory</code> resolves the correct registry based on the authenticated user&apos;s role and reads the <code>x-mode</code> header to select the right MCP topology.</p>
        <p>The resume pipeline is a three-stage event-driven flow: <code>ResumeUploadedV1Event</code> triggers download and parsing, <code>ResumeParsedV1Event</code> triggers embedding generation, and <code>ResumeDeletedV1Event</code> triggers cleanup. Each stage communicates through Dapr pub/sub on RabbitMQ.</p>
        <CsScreenshot src="/images/mcp-inspector.png" alt="MCP Inspector showing dynamically discovered tools from the monolith MCP server" caption="MCP Inspector connected to the monolith server — tools like company_list, draft_job, and finalize_job are discovered at runtime via SSE transport" />
      </CsSection>

      <CsSection id="user-view" alt>
        <h2>What You See</h2>
        <p>In the admin app, open the AI chat and ask it to create a company. Watch the conversation &mdash; you&apos;ll see the model decide which tool to call, the function invocation, and the result. Switch between monolith and microservices mode and ask the same question: the AI discovers different tools from different MCP servers.</p>
        <p>In the public app, upload a resume. A progress indicator updates in real-time as the system downloads, parses, and embeds the document. Then ask the AI for job recommendations &mdash; it queries the pgvector embeddings to find semantic matches.</p>
        <div className="cs-evidence-grid">
          <CsScreenshot src="/images/resume-parsing-progress.png" alt="Resume parsing progress showing 3/5 sections extracted with real-time section checklist" caption="Real-time resume processing progress streamed via SignalR — section-by-section extraction with live status updates" />
          <CsScreenshot src="/images/ai-provider-settings.png" alt="AI Provider Settings page showing dropdown with Azure, OpenAI, Gemini, and Claude options" caption="AI provider configuration — switching between Azure, OpenAI, Gemini, and Claude is a dropdown change, not a code change" />
        </div>
      </CsSection>

      <CsSection id="behind-scenes">
        <h2>Behind the Scenes</h2>
        <p>The <code>FunctionInvokingChatClient</code> from Microsoft.Extensions.AI handles the tool-calling loop automatically. When the model returns a tool call, the middleware invokes the matching <code>AIFunction</code>, feeds the result back, and lets the model decide whether to call another tool or respond to the user.</p>
        <p>MCP tool discovery happens through <code>McpToolProvider</code>, which connects to the backend MCP servers via SSE transport. The provider resolves tools at startup and caches them. When the user&apos;s session mode changes, a different MCP topology is selected.</p>
        <p>JWT tokens are forwarded through <code>AsyncLocal</code> storage so that tool calls from the AI service authenticate against the backend APIs with the original user&apos;s identity. This means authorization rules apply consistently &mdash; a CompanyAdmin can only create jobs in their own company, even through the AI chat.</p>
      </CsSection>

      <CsSection id="decisions" alt>
        <h2>Key Decisions</h2>
        <CsDecisionGrid decisions={[
          { title: "MCP servers over in-process tools", content: "<p><strong>Why:</strong> In-process tools couple the AI service to domain logic. MCP servers let each backend expose its own tools independently. Adding a tool to the monolith doesn't require redeploying the AI service.</p><p><strong>Alternative:</strong> Shared NuGet package with tool definitions. Simpler but creates tight coupling.</p>" },
          { title: "Microsoft.Extensions.AI over direct SDK calls", content: "<p><strong>Why:</strong> The abstraction lets us swap providers without changing application code. The decorator pipeline applies uniformly regardless of which provider is active.</p><p><strong>Alternative:</strong> Semantic Kernel. More opinionated, heavier dependency.</p>" },
          { title: "pgvector over a dedicated vector database", content: "<p><strong>Why:</strong> PostgreSQL was already in the stack. Adding the pgvector extension avoids introducing another database to operate. For portfolio scale, it performs well.</p><p><strong>Alternative:</strong> Pinecone, Qdrant, or Weaviate. Better at scale but adds operational complexity.</p>" },
          { title: "Three chat scopes with policy-based auth", content: "<p><strong>Why:</strong> Different user roles need different capabilities. Scoping at the chat level prevents privilege escalation through the AI.</p><p><strong>Alternative:</strong> Single chat endpoint with runtime permission checks per tool. Simpler routing but harder to audit.</p>" },
        ]} />
      </CsSection>

      <CsSection id="tradeoffs">
        <h2>Tradeoffs &amp; Lessons Learned</h2>
        <CsTradeoffList items={[
          "<strong>MCP adds a network hop per tool call:</strong> Each tool invocation goes from AI service to MCP server to backend API and back. The latency is acceptable (tens of milliseconds) but visible in traces.",
          "<strong>pgvector limits:</strong> Cosine similarity search with 1536-dimension embeddings works well up to roughly a million vectors. Beyond that, you'd need approximate nearest neighbor indexes or a dedicated vector database.",
          "<strong>Tool descriptions matter enormously:</strong> The LLM selects tools based on their descriptions. Vague descriptions cause N+1 tool calls. We saw 20x token savings by making descriptions explicit.",
          "<strong>Redis conversation window:</strong> Chat history is stored in Redis and truncated at 40 messages. A smarter approach would summarize older messages instead of dropping them.",
        ]} />
      </CsSection>
    </CaseStudyLayout>
  );
}
