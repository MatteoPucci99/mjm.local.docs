# mjm.local.docs

## Descrizione

Server MCP locale per archiviazione e ricerca di documentazione tramite embeddings.

## Obiettivo

Collegare client MCP per recuperare informazioni di progetto tramite ricerca semantica.

## Stack Tecnologico

- **Linguaggio**: C# / .NET 8+
- **Framework AI**: Microsoft Semantic Kernel
- **Astrazioni**: Microsoft.Extensions.VectorData.Abstractions
- **Vector Store**:
  - SQLite con sqlite-vec (produzione locale) - NuGet: `Microsoft.SemanticKernel.Connectors.SqliteVec`
  - In-Memory (test/dev)
- **Embeddings**: Astrazioni SK con supporto multi-provider

## Architettura

### Componenti principali

1. **MCP Server** - Endpoint MCP per client esterni
2. **Document Processor** - Ingestion e chunking documenti
3. **Embedding Service** - Astrazione per generazione embeddings (via SK)
4. **Vector Store** - Storage e ricerca semantica (SQLite + sqlite-vec)

### Provider Embeddings supportati (via Semantic Kernel)

- OpenAI
- Azure OpenAI
- Ollama (completamente locale)
- Hugging Face

### Capacita ricerca vettoriale SQLite

- Funzioni distanza: Cosine, Manhattan, Euclidean
- Vettori in tabelle virtuali (prefisso `vec_`)
- Filtri: EqualTo
- Note: No hybrid search (solo vector search pura)

## Struttura Progetto

```
mjm.local.docs/
├── src/
│   ├── Core/           # Astrazioni e interfacce
│   ├── Embeddings/     # Implementazioni embedding providers
│   ├── VectorStore/    # Implementazioni storage
│   ├── McpServer/      # Server MCP
│   └── Documents/      # Processamento documenti
├── tests/
└── docs/
```

## Tools MCP esposti

- `search_docs` - Ricerca semantica nella documentazione
- `add_document` - Aggiunta nuovo documento
- `list_collections` - Elenco collezioni disponibili

## NuGet Packages necessari

- Microsoft.SemanticKernel
- Microsoft.SemanticKernel.Connectors.SqliteVec
- Microsoft.Extensions.VectorData.Abstractions
- (opzionale) Microsoft.SemanticKernel.Connectors.Ollama

## Note evolutive

Per hybrid search (vector + keyword) in futuro, considerare:

- Qdrant (Docker locale, UI dashboard)
- PostgreSQL con pgvector
