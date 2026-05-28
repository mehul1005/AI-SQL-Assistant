# AI SQL Assistant

An enterprise-grade, decoupled desktop application that translates natural language into executable T-SQL queries using open-source LLMs (Llama 3). 

## 🏗️ Architecture

This project strictly follows separation of concerns, dividing the AI orchestration from the client presentation layer:

1. **Backend (ASP.NET Core 8 Web API):** - Handles all AI API communication and prompt engineering.
   - Utilizes Dependency Injection for scalable service management.
   - Implements structured logging for observability.
   - Currently configured to interface with OpenAI-compatible endpoints (Groq/Llama 3).

2. **Frontend (WPF .NET 8 Client):**
   - A lightweight, responsive desktop UI.
   - Completely agnostic to the underlying AI logic; communicates entirely via REST HTTP calls to the backend API.

## 🚀 Tech Stack
* **Language:** C# 12
* **Framework:** .NET 8.0 (Long Term Support)
* **Backend:** ASP.NET Core Web API
* **Frontend:** Windows Presentation Foundation (WPF)
* **AI Integration:** `Betalgo.OpenAI` SDK (Rerouted to Groq for Llama 3 inference)

## ⚙️ How to Run Locally

### Prerequisites
* Visual Studio 2022
* .NET 8.0 SDK
* A free API Key from [Groq](https://console.groq.com/) (or OpenAI)

### Setup
1. Clone this repository.
2. Navigate to `AiSqlAssistant.Api/appsettings.json` and insert your API key:
   ```json
   "OpenAI": {
     "ApiKey": "gsk_YOUR_API_KEY_HERE"
   }
