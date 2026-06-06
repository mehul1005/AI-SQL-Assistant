<div align="center">

# 🤖 AI SQL Assistant

### *Speak Human. Query Data.*

An enterprise-grade, decoupled desktop application that translates natural language
into executable T-SQL using open-source LLMs — protected by a multi-layer security shield.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://docs.microsoft.com/en-us/aspnet/core/)
[![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![Llama](https://img.shields.io/badge/Llama_3.3-70B-FF6B35?style=for-the-badge&logo=meta&logoColor=white)](https://groq.com/)
[![License](https://img.shields.io/badge/License-MIT-22c55e?style=for-the-badge)](LICENSE)

<br/>

> **"Never execute a query you didn't understand. Never understand a query without context."**

<br/>

[🚀 Quick Start](#️-how-to-run-locally) · [🏗️ Architecture](#️-enterprise-architecture) · [✨ Features](#-core-features) · [☁️ Cloud Deployment](#-cloud-deployment-azure) · [🛠️ Tech Stack](#️-tech-stack)

</div>

---

## 📖 What Is This?

AI SQL Assistant bridges the gap between **human intent** and **database queries**.
Instead of writing T-SQL by hand, analysts simply describe what they want in plain English —
the system handles schema discovery, query generation, risk analysis, security validation,
and audit logging automatically.

Built for teams that need **AI-powered productivity without sacrificing governance**.

---

## 🏗️ Enterprise Architecture

This project enforces strict **separation of concerns** — splitting data orchestration,
AI metadata injection, and desktop presentation into fully decoupled layers,
protected by an identity-aware middleware shield.

```mermaid
graph TB
    %% ─────────────────────────────────────────
    %% LAYER DEFINITIONS
    %% ─────────────────────────────────────────
    subgraph CLIENT ["🖥️  WPF Desktop Client"]
        direction TB
        AuthBox("🔑 Identity / API Key")
        UI("💬 Natural Language Prompt")
        Review("👁️  HITL Review Screen")
        RiskDash("📊 Risk Dashboard")
        DataGrid("📋 Dynamic DataGrid")
        AuditUI("📜 Audit Log Viewer")
    end

    subgraph API ["⚙️  ASP.NET Core 8 Web API"]
        direction TB
        AuthMid("🛡️ API Key Middleware")

        subgraph ENDPOINTS ["REST Endpoints"]
            direction LR
            Generate("POST /generate")
            Execute("POST /execute")
            AuditAPI("GET  /audit-logs")
        end

        subgraph SERVICES ["Business Services"]
            direction LR
            RiskService("🔬 Risk Analyzer")
            Security("🚧 Regex Interceptor")
        end

        EF("🗄️ Entity Framework Core")
    end

    subgraph INFRA ["☁️  Infrastructure"]
        direction TB
        LLM(("🦙 Groq\nLlama 3.3 70B"))
        SQLite[("💾 SQLite\nSandbox + Audit Tables")]
    end

    %% ─────────────────────────────────────────
    %% AUTH FLOW
    %% ─────────────────────────────────────────
    AuthBox -->|"x-api-key header"| AuthMid
    AuthMid -->|"✅ Validated"| ENDPOINTS
    AuthMid -.->|"🚫 Log Unauthorized"| EF

    %% ─────────────────────────────────────────
    %% GENERATION FLOW
    %% ─────────────────────────────────────────
    UI -->|"① Natural Language"| Generate
    Generate -->|"② Fetch Schema"| SQLite
    SQLite -.->|"Raw DDL"| Generate
    Generate -->|"③ Prompt + Schema"| LLM
    LLM -.->|"④ Raw T-SQL"| Generate
    Generate -->|"⑤ Analyze"| RiskService
    RiskService -.->|"Risk Profile"| Generate
    Generate -->|"⑥ SQL + Risk Score"| RiskDash
    RiskDash --> Review

    %% ─────────────────────────────────────────
    %% EXECUTION FLOW
    %% ─────────────────────────────────────────
    Review -->|"⑦ User Approves ✅"| Execute
    Execute -->|"⑧ Security Scan"| Security
    Security -->|"⑨ Safe — Execute"| EF
    EF -->|"⑩ Run Query"| SQLite
    SQLite -.->|"Result Rows"| EF
    EF -->|"⑪ JSON Response"| Execute
    Execute -->|"⑫ Render Results"| DataGrid

    %% ─────────────────────────────────────────
    %% AUDIT FLOW
    %% ─────────────────────────────────────────
    RiskService -.->|"📝 Log BLOCKED_ANALYZER"| EF
    Security -.->|"📝 Log BLOCKED_SECURITY"| EF
    Execute -.->|"📝 Log Execution + Timing"| EF
    AuditUI -->|"Fetch History"| AuditAPI
    AuditAPI -->|"Query Logs"| EF

    %% ─────────────────────────────────────────
    %% STYLING
    %% ─────────────────────────────────────────
    classDef clientStyle   fill:#dbeafe,stroke:#2563eb,stroke-width:2px,color:#1e3a5f,font-weight:bold
    classDef apiStyle      fill:#f3e8ff,stroke:#7c3aed,stroke-width:2px,color:#3b0764,font-weight:bold
    classDef secureStyle   fill:#dcfce7,stroke:#16a34a,stroke-width:2.5px,color:#14532d,font-weight:bold
    classDef externalStyle fill:#fef9c3,stroke:#ca8a04,stroke-width:2px,color:#713f12,font-weight:bold
    classDef dbStyle       fill:#fee2e2,stroke:#dc2626,stroke-width:2px,color:#7f1d1d,font-weight:bold
    classDef endpointStyle fill:#e0f2fe,stroke:#0284c7,stroke-width:1.5px,color:#0c4a6e

    class AuthBox,UI,Review,RiskDash,DataGrid,AuditUI clientStyle
    class AuthMid,RiskService secureStyle
    class Security secureStyle
    class LLM externalStyle
    class SQLite dbStyle
    class Generate,Execute,AuditAPI endpointStyle
    class EF apiStyle

```

---

## 🔄 Request Lifecycle

```mermaid
sequenceDiagram
    actor User
    participant Client as 🖥️ WPF Client
    participant Auth   as 🛡️ Auth Middleware
    participant API    as ⚙️ Web API
    participant Risk   as 🔬 Risk Analyzer
    participant LLM    as 🦙 Llama 3.3
    participant Guard  as 🚧 Security Guard
    participant DB     as 💾 SQLite

    User->>Client: Types natural language prompt
    Client->>Auth: POST /generate (x-api-key header)

    alt Invalid Key
        Auth-->>Client: 401 Unauthorized
        Auth--)DB: Log unauthorized attempt
    else Valid Key
        Auth->>API: Identity injected ✅
        API->>DB: Fetch live schema DDL
        DB-->>API: CREATE TABLE statements
        API->>LLM: System prompt + schema + user query
        LLM-->>API: Generated T-SQL
        API->>Risk: Analyze query
        Risk-->>API: Risk score (LOW / MEDIUM / CRITICAL)

        alt CRITICAL Risk — Destructive Command
            API--)DB: Log BLOCKED_ANALYZER event
            API-->>Client: ⛔ Blocked — shows alert
        else Acceptable Risk
            API-->>Client: SQL + Risk Dashboard
            User->>Client: Reviews query manually (HITL)
            User->>Client: Clicks APPROVE ✅

            Client->>Auth: POST /execute (x-api-key header)
            Auth->>Guard: Forward approved SQL
            Guard->>Guard: Regex scan for DML/DDL

            alt Malicious Pattern Detected
                Guard--)DB: Log BLOCKED_SECURITY event
                Guard-->>Client: 🚫 Security Alert
            else Clean Query
                Guard->>DB: Execute query safely
                DB-->>Guard: Result rows
                Guard-->>Client: JSON response
                Client->>User: Renders results in DataGrid
                Guard--)DB: Log execution + duration
            end
        end
    end

```

---

## ✨ Core Features

### 🔑 Identity & Access Management

A custom **ASP.NET Core Middleware** intercepts every HTTP request before it reaches any controller. It validates `x-api-key` headers, resolves user identity, and injects it into the server context. Unauthorized attempts are immediately blocked **and silently logged** for forensic review.

---

### 🚀 Dynamic Schema Discovery

The backend interrogates SQLite's internal `sqlite_master` catalog at runtime to extract live `CREATE TABLE` DDL. This schema is injected into the LLM system prompt — so the model *always* generates SQL against your actual table structure, eliminating hallucinated column names.

---

### 🔬 Query Risk Analyzer

Before any SQL reaches the user, the API parses the query, maps the execution path, identifies affected tables, and assigns a **risk classification**:

| Score | Label | Meaning |
| --- | --- | --- |
| 🟢 | `LOW` | Safe read operation |
| 🟡 | `MEDIUM` | Complex joins / aggregations |
| 🔴 | `CRITICAL` | Destructive or schema-altering |

### 👁️ Human-in-the-Loop (HITL)

Queries are **never executed blindly**. The API returns the raw SQL alongside its risk profile. The user must manually review, understand, and explicitly approve execution — turning every query into a conscious decision.

---

### 🚧 Regex Security Interceptor

Even after HITL approval, a **server-side regex engine** performs a final scan at the execution endpoint. If destructive `DROP`, `DELETE`, `TRUNCATE`, or DDL commands are detected — whether from AI hallucination or malicious edits — the request is killed instantly and a security event is logged.

---

### 📜 Enterprise Audit Logging

A silent governance pipeline records every interaction with full identity attribution:

* ✅ `EXECUTED` — query, duration, row count, user
* ⛔ `BLOCKED_ANALYZER` — risk-blocked events
* 🚫 `BLOCKED_SECURITY` — interceptor-blocked events
* 🔒 `UNAUTHORIZED` — identity failures

---

## ☁️ Cloud Deployment (Azure)

The production backend is architected to run in a cloud-native ecosystem.

### Serverless Hosting

* **App Layer:** Hosted seamlessly via **Azure App Services** on a scalable infrastructure.
* **Configuration Override:** Real API secrets and user key registries are safely isolated within **Azure Environment Variables**, keeping raw credentials completely out of source control.

### Shifting WPF to Cloud Mode

To switch your native client interface to communicate with your live cloud environment rather than a local sandbox machine, configure the endpoint inside `ApiService.cs`:

```csharp
// Local Development Sandbox
// private readonly string _baseUrl = "https://localhost:7092/api/SqlAssistant";

// Production Azure Cloud
private readonly string _baseUrl = "[https://your-app-service-name.azurewebsites.net/api/SqlAssistant](https://your-app-service-name.azurewebsites.net/api/SqlAssistant)";

```

---

## 🛠️ Tech Stack

```
┌─────────────────────────────────────────────────────────────────┐
│                        AI SQL ASSISTANT                         │
├──────────────────────────┬──────────────────────────────────────┤
│  Presentation Layer      │  WPF (.NET 8) · C# 12                │
├──────────────────────────┼──────────────────────────────────────┤
│  API Layer               │  ASP.NET Core 8 Web API              │
├──────────────────────────┼──────────────────────────────────────┤
│  AI / LLM                │  Llama 3.3 70B via Groq              │
│                          │  Betalgo.OpenAI SDK (rerouted)       │
├──────────────────────────┼──────────────────────────────────────┤
│  Data / ORM              │  Entity Framework Core · SQLite      │
├──────────────────────────┼──────────────────────────────────────┤
│  Language                │  C# 12                               │
│  Target Framework        │  .NET 8.0                            │
└──────────────────────────┴──────────────────────────────────────┘

```

---

## 🔐 Security Architecture

```mermaid
flowchart LR
    REQ(["📨 HTTP Request"]) --> L1

    subgraph LAYERS ["Defence in Depth — 3 Layers"]
        direction LR
        L1["🛡️ Layer 1\nAPI Key Auth\nIdentity Injection"]
        L2["🔬 Layer 2\nRisk Analyzer\nQuery Classification"]
        L3["🚧 Layer 3\nRegex Interceptor\nPattern Matching"]
    end

    L1 -->|"❌ Invalid Key"| BLOCK1(["🚫 401 Blocked\n+ Logged"])
    L1 -->|"✅ Valid"| L2
    L2 -->|"❌ CRITICAL Risk"| BLOCK2(["⛔ Blocked\n+ Logged"])
    L2 -->|"✅ Acceptable"| HITL["👁️ HITL Review\n(User Approves)"]
    HITL --> L3
    L3 -->|"❌ Malicious Pattern"| BLOCK3(["🚫 Blocked\n+ Logged"])
    L3 -->|"✅ Clean"| EXEC(["✅ Execute\n+ Audit Log"])

    style LAYERS fill:#f8fafc,stroke:#cbd5e1,stroke-width:2px
    style EXEC fill:#dcfce7,stroke:#16a34a,color:#14532d
    style BLOCK1 fill:#fee2e2,stroke:#dc2626,color:#7f1d1d
    style BLOCK2 fill:#fee2e2,stroke:#dc2626,color:#7f1d1d
    style BLOCK3 fill:#fee2e2,stroke:#dc2626,color:#7f1d1d
    style HITL fill:#fef9c3,stroke:#ca8a04,color:#713f12

```

---

## ⚙️ How to Run Locally

### Prerequisites

| Requirement | Version | Link |
| --- | --- | --- |
| Visual Studio | 2022+ | [Download](https://visualstudio.microsoft.com/) |
| .NET SDK | 8.0 | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Groq API Key | Free tier | [Get Key](https://console.groq.com/) |

### 1️⃣ Clone the Repository

```bash
git clone [https://github.com/your-username/ai-sql-assistant.git](https://github.com/your-username/ai-sql-assistant.git)
cd ai-sql-assistant

```

### 2️⃣ Configure API Keys

Open `AiSqlAssistant.Api/appsettings.json` and insert your credentials:

```json
{
  "OpenAI": {
    "ApiKey": "gsk_YOUR_GROQ_API_KEY_HERE"
  },
  "ApiKeys": {
    "dev-key-777":   "Admin User",
    "guest-key-111": "Guest Analyst"
  }
}

```

> 💡 **Tip:** The `ApiKeys` section is your user registry.
> Each key maps to a named identity visible in audit logs.

### 3️⃣ Configure Startup Projects

In Visual Studio:

```
Right-click Solution → Properties
  → Startup Project → Multiple startup projects
    → AiSqlAssistant.Api    [Start]
    → AiSqlAssistant.Client [Start]

```

### 4️⃣ Launch

Press **`F5`** — the API automatically:

* Provisions the SQLite database
* Runs EF Core migrations
* Seeds sample business data

The WPF client launches and connects automatically. ✅

---

## 🤝 Contributing

Contributions are welcome! Please open an issue first to discuss what you'd like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

Distributed under the MIT License. See [`LICENSE`](https://www.google.com/search?q=LICENSE) for more information.

Built with ❤️ using .NET 8 · WPF · ASP.NET Core · Llama 3.3

⭐ **Star this repo if it was useful to you!** ⭐