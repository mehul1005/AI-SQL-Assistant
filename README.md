# AI SQL Assistant

An enterprise-grade, decoupled desktop application that translates natural language into executable T-SQL queries using open-source LLMs (Llama 3.3), dynamically executes them against an isolated database sandbox, and renders runtime data dynamically.

## 🏗️ Architecture & Core Features

This project strictly follows separation of concerns, dividing data orchestration, AI metadata injection, and desktop presentation into decoupled layers:

1. **Dynamic Schema Discovery (Context Injection):** - The backend is fully database-aware. It interrogates internal database metadata (`sqlite_master`) to dynamically extract the exact `CREATE TABLE` DDL structures at runtime.
   - This schema context is automatically injected into the LLM system prompt, eliminating the need for users to provide or understand the database layout manually.

2. **Backend (ASP.NET Core Web API):**
   - Handles automated database schema reflection and prompt engineering pipelines.
   - Utilizes Entity Framework Core with low-level `DbDataReader` mapping to handle completely dynamic query outputs without hardcoded C# runtime models.
   - Currently configured to interface with high-throughput endpoints (Groq / Llama 3.3 70B).

3. **Frontend (WPF Client):**
   - A minimalist, single-input desktop UI focused purely on natural language interaction.
   - Binds directly to dynamic JSON data tables streamed back from the API, automatically constructing WPF `DataGrid` columns on the fly at runtime.

## 🚀 Tech Stack
* **Language:** C#
* **Framework:** .NET / WPF
* **ORM & Database:** Entity Framework Core / SQLite Sandbox
* **AI Integration:** `Betalgo.OpenAI` SDK (Rerouted to Groq for Llama 3.3 inference)

## ⚙️ How to Run Locally

### Prerequisites
* Visual Studio
* .NET SDK
* A free API Key from [Groq](https://console.groq.com/)

### Setup
1. Clone this repository.
2. Navigate to `AiSqlAssistant.Api/appsettings.json` and insert your API key placeholder:
   ```json
   "OpenAI": {
     "ApiKey": "gsk_YOUR_API_KEY_HERE"
   }