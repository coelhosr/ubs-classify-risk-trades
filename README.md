
# API de Classificação de Risco de Trades (ASP.NET Core 8)

Arquitetura em camadas (Domain, Application, WebApi) com regras de risco extensíveis, validação, logging, Swagger e testes.

## Como executar

### Pré-requisitos
- .NET SDK 8.0 instalado.

### Passos
1. Clonar ou extrair este projeto.
2. Restaurar pacotes e compilar:
   ```bash
   dotnet restore
   dotnet build -c Release
   ```
3. Executar a Web API:
   ```bash
   dotnet run --project src/WebApi/WebApi.csproj
   ```
4. Abrir Swagger UI em `https://localhost:5001/swagger/index.html` (ou porta mostrada no terminal).

## Endpoints

### POST `/api/trades/classify`
**Body**
```json
[
  {
    "value": 8719.41,
    "clientSector": "Public",
    "clientId": null
  },
  {
    "value": 1000000.89,
    "clientSector": "Public",
    "clientId": "Cliente1Mi"
  },
  {
    "value": 1796.05,
    "clientSector": "Private",
    "clientId": "C002"
  },
  {
    "value": 2399.26,
    "clientSector": "Private",
    "clientId": "C003"
  },
  {
    "value": 60865.75,
    "clientSector": "Private",
    "clientId": null
  },
  {
    "value": 67.89,
    "clientSector": "Public",
    "clientId": null
  },

  {
    "value": 2000000.89,
    "clientSector": "Private",
    "clientId": "Cliente2MiMi"
  }
]
```
**Response**
```json
{
  "categories": [
    "LOWRISK",
    "MEDIUMRISK",
    "LOWRISK",
    "LOWRISK",
    "LOWRISK",
    "LOWRISK",
    "HIGHRISK"
  ]
}
```
### POST `/api/trades/analyze/queue`
**Body**
```json
[
  {
    "value": 8719.41,
    "clientSector": "Public",
    "clientId": null
  },
  {
    "value": 1000000.89,
    "clientSector": "Public",
    "clientId": "Cliente1Mi"
  },
  {
    "value": 1796.05,
    "clientSector": "Private",
    "clientId": "C002"
  },
  {
    "value": 2399.26,
    "clientSector": "Private",
    "clientId": "C003"
  },
  {
    "value": 60865.75,
    "clientSector": "Private",
    "clientId": null
  },
  {
    "value": 67.89,
    "clientSector": "Public",
    "clientId": null
  },

  {
    "value": 2000000.89,
    "clientSector": "Private",
    "clientId": "Cliente2MiMi"
  }
]
```
**Response**
```json
{
  "jobId": "b707bd139973450385906b851ee60c63",
  "enqueued": 7,
  "statusUrl": "https://localhost:5001/api/trades/analyze/b707bd139973450385906b851ee60c63"
}
```

### POST `/api/trades/analyze/{jobId}`
**Response** (exemplo)
```json
{
  "status": "Processando",
  "processed": 0,
  "total": 7,
  "progress": 0
}
```

### POST `/api/trades/analyze`
**Body**
```json
[
  {
    "value": 8719.41,
    "clientSector": "Public",
    "clientId": null
  },
  {
    "value": 1000000.89,
    "clientSector": "Public",
    "clientId": "Cliente1Mi"
  },
  {
    "value": 1796.05,
    "clientSector": "Private",
    "clientId": "C002"
  },
  {
    "value": 2399.26,
    "clientSector": "Private",
    "clientId": "C003"
  },
  {
    "value": 60865.75,
    "clientSector": "Private",
    "clientId": null
  },
  {
    "value": 67.89,
    "clientSector": "Public",
    "clientId": null
  },

  {
    "value": 2000000.89,
    "clientSector": "Private",
    "clientId": "Cliente2MiMi"
  }
]
```
**Response**
```json
{
  "summary": {
    "LOWRISK": {
      "count": 5,
      "totalValue": 73848.36,
      "topClient": null
    },
    "MEDIUMRISK": {
      "count": 1,
      "totalValue": 1000000.89,
      "topClient": "Cliente1Mi"
    },
    "HIGHRISK": {
      "count": 1,
      "totalValue": 2000000.89,
      "topClient": "Cliente2MiMi"
    }
  },
  "processingTimeMs": 4,
  "categories": [
    "LOWRISK",
    "MEDIUMRISK",
    "LOWRISK",
    "LOWRISK",
    "LOWRISK",
    "LOWRISK",
    "HIGHRISK"
  ]
}
```

## Decisões técnicas
- **Extensibilidade**: Regras implementadas como `IRiskRule` (Strategy/Chain). Para novas regras, crie uma classe e registre-a na DI na ordem de prioridade.
- **Validação**: `TradeValidator` garante valores positivos, setor válido e `clientId` quando necessário.
- **Performance**: Processamento linear O(n) e uso de estruturas de dicionário para agregação por categoria/cliente.
- **Logging**: `ILogger` registra quantidade de trades e tempo de processamento.
- **Documentação de API**: Swagger (`AddEndpointsApiExplorer` + `AddSwaggerGen`).

## Testes
Executar testes:
```bash
dotnet test
```
Cobre regras de classificação e resumo.
Bibliotecas predominantes: xUnit e NSubstitute.

## Estrutura do projeto
```
src/
  Domain/
  Application/
  Infrastructure
  WebApi/
tests/
  ApplicationServiceTests/
  DomainTests/
  TradesControllerTests
```

## Extensão futura
- Novo endpoint para filtros por data/cliente.
- Persistência opcional.
- Autenticação se necessário.
- Melhorias no enfileiramento das trades e remoção do endpoint sem filas.
