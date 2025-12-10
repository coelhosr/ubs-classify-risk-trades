
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
4. Abrir Swagger UI em `http://localhost:5000/swagger` (ou porta mostrada no terminal).

## Endpoints

### POST `/trades/classify`
**Body**
```json
[
  { "value": 2000000, "clientSector": "Private" },
  { "value": 400000,  "clientSector": "Public"  },
  { "value": 500000,  "clientSector": "Public"  },
  { "value": 3000000, "clientSector": "Public"  }
]
```
**Response**
```json
{ "categories": ["HIGHRISK","LOWRISK","LOWRISK","MEDIUMRISK"] }
```

### POST `/trades/analyze`
**Body**
```json
[
  { "value": 2000000, "clientSector": "Private", "clientId": "CLI003" },
  { "value": 400000,  "clientSector": "Public",  "clientId": "CLI001" },
  { "value": 500000,  "clientSector": "Public",  "clientId": "CLI001" },
  { "value": 3000000, "clientSector": "Public",  "clientId": "CLI002" }
]
```
**Response** (exemplo)
```json
{
  "categories": ["HIGHRISK","LOWRISK","LOWRISK","MEDIUMRISK"],
  "summary": {
    "LOWRISK":   { "count": 2, "totalValue": 900000,  "topClient": "CLI001" },
    "MEDIUMRISK":{ "count": 1, "totalValue": 3000000, "topClient": "CLI002" },
    "HIGHRISK":  { "count": 1, "totalValue": 2000000,  "topClient": "CLI003" }
  },
  "processingTimeMs": 45
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

## Estrutura do projeto
```
src/
  Domain/
  Application/
  WebApi/
```

## Extensão futura
- Novo endpoint para filtros por data/cliente.
- Persistência opcional.
- Autenticação se necessário.
