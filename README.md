# AI SQL Assistant

An enterprise-grade, decoupled desktop application that translates natural language into executable T-SQL queries using open-source LLMs (Llama 3.3). It features dynamic database schema discovery, a strict Regex security layer, a custom risk-analyzer engine, a "Human-in-the-Loop" (HITL) execution workflow, Identity Authentication, and comprehensive audit logging.

## 🏗️ Enterprise Architecture

This project strictly follows separation of concerns, dividing data orchestration, AI metadata injection, and desktop presentation into decoupled layers, protected by an identity-aware middleware shield.

```mermaid
graph TD
    %% Define Client Layer
    subgraph WPF Desktop Client
        UI[WPF UI - User Prompt]
        Review[HITL Review Screen]
        DataGrid[Dynamic DataGrid]
        RiskDash[Risk Dashboard]
        AuditUI[WPF Audit Log Viewer]
        AuthBox[Identity / API Key]
    end

    %% Define API Layer
    subgraph ASP.NET Core 8 Web API
        AuthMid[API Key Middleware]
        Generate["/api/SqlAssistant/generate"]
        Execute["/api/SqlAssistant/execute"]
        AuditAPI["/api/SqlAssistant/audit-logs"]
        RiskService[Query Risk Analyzer]
        Security[Regex Security Interceptor]
        EF[Entity Framework Core]
    end

    %% Define External & Data Layer
    subgraph Infrastructure
        LLM((Groq / Llama 3.3))
        SQLite[(SQLite Sandbox + Audit Tables)]
    end

    %% Auth Flow
    AuthBox -- "Headers: x-api-key" --> AuthMid
    AuthMid -- "Validates Identity" --> Generate
    AuthMid -- "Validates Identity" --> Execute
    AuthMid -- "Validates Identity" --> AuditAPI
    AuthMid -. "Logs Unauthorized Attempts" .-> EF

    %% Generation Flow
    UI -- "1. Prompt" --> Generate
    Generate -- "2. Schema Context" --> SQLite
    SQLite -. "Raw DDL" .-> Generate
    Generate -- "3. Prompt + Schema" --> LLM
    LLM -. "4. Raw T-SQL" .-> Generate
    Generate -- "5. Analyze SQL" --> RiskService
    RiskService -. "Risk Profile" .-> Generate
    Generate -- "6. Return SQL" --> RiskDash
    RiskDash --> Review

    %% Execution Flow
    Review -- "7. User Approves" --> Execute
    Execute -- "8. Scan for Destructive Commands" --> Security
    Security -- "9. Execute Safe Query" --> EF
    EF -- "10. Query DB" --> SQLite
    SQLite -. "Dynamic Rows" .-> EF
    EF -- "11. JSON Response" --> Execute
    Execute -- "12. Render Data" --> DataGrid

    %% Audit Flow
    RiskService -. "Log Blocks (with User)" .-> EF
    Security -. "Log Blocks (with User)" .-> EF
    Execute -. "Log Execution (with User)" .-> EF
    AuditUI -- "Fetch History" --> AuditAPI
    AuditAPI -- "Query Logs" --> EF

    %% Styling
    classDef secure fill:#dcfce7,stroke:#16a34a,stroke-width:2px,color:#000;
    classDef external fill:#f3f4f6,stroke:#6b7280,stroke-width:2px,color:#000;
    class Security,AuthMid secure
    class LLM external

```

## 🚀 Core Features

* **Identity & Access Management (IAM):** A custom ASP.NET Core Middleware intercepts all HTTP requests, validates `x-api-key` headers, and injects user identity into the server context. Unauthorized attempts are immediately blocked and logged.
* **Enterprise Audit Logging:** A governance pipeline that silently records every interaction. It tracks successful query execution times, logs `BLOCKED_BY_ANALYZER` events if the AI hallucinates a destructive query, and logs `BLOCKED_SECURITY` events for malicious edits—all tied to the authenticated user's identity.
* **Dynamic Schema Discovery (Context Injection):** The backend interrogates internal SQLite metadata (`sqlite_master`) to dynamically extract exact `CREATE TABLE` DDL structures at runtime, injecting them into the LLM system prompt.
* **Query Risk Analyzer:** Before presenting the SQL to the user, the API parses the query, maps out the execution path, identifies affected tables, and calculates a visual risk score (LOW/MEDIUM/CRITICAL).
* **Human-in-the-Loop (HITL) Workflow:** Queries are never executed blindly. The API returns the raw SQL to the client, forcing the user to manually review and approve the query.
* **Regex Security Interceptor:** If a user (or the AI) attempts to execute destructive DML/DDL commands, a server-side interceptor immediately kills the connection and returns a security alert.

## 🛠️ Tech Stack

* **Language:** C# 12
* **Framework:** .NET 8.0 / WPF
* **ORM & Database:** Entity Framework Core / SQLite
* **AI Integration:** `Betalgo.OpenAI` SDK (Rerouted to Groq for Llama 3.3 70B inference)

## ⚙️ How to Run Locally

### Prerequisites

* Visual Studio 2022
* .NET 8.0 SDK
* A free API Key from [Groq](https://console.groq.com/)

### Setup

1. Clone this repository.
2. Navigate to `AiSqlAssistant.Api/appsettings.json` and insert your API keys:

```json
"OpenAI": {
  "ApiKey": "gsk_YOUR_API_KEY_HERE"
},
"ApiKeys": {
  "dev-key-777": "Admin User",
  "guest-key-111": "Guest Analyst"
}

```

3. Set both `AiSqlAssistant.Api` and `AiSqlAssistant.Client` as startup projects in Visual Studio.
4. Run the solution (**F5**). The API will automatically provision the SQLite database sandbox and seed sample data on launch.

```
