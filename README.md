# ResumeFitConsole (LLM-first, domain-agnostic)

This console app extracts requirements (from Job description) and resume evidence items using an LLM, embeds both sets with an embedding model, and finds resume detail that matches any of the requirements by computing cosine similarity per requirement.
It also uses an LLM to evaluate hard constraints (education, experience, location, work authorization, etc.) with explicit pass/fail/unknown outcomes.

## Why this design is generic
- No hardcoded domain keywords
- Uses LLM extraction prompts to work across industries/domains
- Uses embedding similarity for semantic matching
- Uses LLM-based hard-constraint classification and evidence evaluation (not keyword-only rules)
- OpenAI-compatible API interface (can point to different providers)

## Environment variables
- `LLM_API_KEY` (required)
- `LLM_BASE_URL` (optional, default: `https://api.openai.com/v1`)
- `LLM_CHAT_MODEL` (optional, default: `gpt-5-mini-2025-08-07`)
- `LLM_EMBED_MODEL` (optional, default: `text-embedding-3-large`)
- `LLM_TIMEOUT_SECONDS` (optional, default: `120`)
- `MATCH_THRESHOLD` (optional, default: `0.50`)
- `REQUIRED_HARD_CONSTRAINTS` (optional CSV, default: empty/no gate)
- `ELIGIBILITY_MODE` (optional, default: `llm`; options: `llm`, `strict`)

Examples:
- `REQUIRED_HARD_CONSTRAINTS=WorkAuthorization,Location`
- `REQUIRED_HARD_CONSTRAINTS=Education,Experience,WorkAuthorization,Location`
- `REQUIRED_HARD_CONSTRAINTS=` (empty disables hard-constraint gating)
- `ELIGIBILITY_MODE=llm` (LLM decides final eligibility)
- `ELIGIBILITY_MODE=strict` (deterministic gate on required categories)

## Run
```powershell
dotnet run --project ResumeFitConsole
```

Optional custom paths:
```powershell
dotnet run --project ResumeFitConsole -- "path/to/requirement.md" "path/to/resume.txt"
```
