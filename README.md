# CashFlow API

API REST para controle de fluxo de caixa de um pequeno comércio, desenvolvida como solução para o desafio técnico de desenvolvedor de software. A aplicação permite o registro de lançamentos (créditos e débitos) e disponibiliza o relatório de saldo diário consolidado, respeitando os requisitos de negócio, técnicos e não funcionais descritos no desafio.

## Sumário

- [Aderência ao desafio](#aderência-ao-desafio)
- [Visão geral da solução](#visão-geral-da-solução)
- [Arquitetura](#arquitetura)
- [Padrões de projeto utilizados](#padrões-de-projeto-utilizados)
- [Stack técnica](#stack-técnica)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Estrutura do banco de dados](#estrutura-do-banco-de-dados)
- [Como executar o projeto](#como-executar-o-projeto)
- [Documentação da API (Swagger)](#documentação-da-api-swagger)
- [Endpoints da API](#endpoints-da-api)
- [Estratégia de resiliência](#estratégia-de-resiliência)
- [Testes](#testes)
- [Decisões de projeto](#decisões-de-projeto)
- [Melhorias futuras](#melhorias-futuras)

## Aderência ao desafio

Checklist objetivo comparando o que o desafio pediu com o que foi efetivamente implementado no código.

**Requisitos de negócio**

| Requisito | Status | Onde |
|---|---|---|
| Gestão dos lançamentos financeiros (créditos e débitos) | Atendido | `POST /api/v1/launches`, `GET /api/v1/launches` |
| Relatório de saldo diário consolidado | Atendido | `GET /api/v1/daily-balances/{date}`, `GET /api/v1/daily-balances` |

**Requisitos técnicos obrigatórios**

| Requisito | Status | Observação |
|---|---|---|
| Linguagem C# | Atendido | .NET 8 / C# 12 |
| Rotinas de teste | Atendido | Testes de unidade (xUnit) e de integração (xUnit + Testcontainers) |
| Clean Code, SOLID e Design Patterns | Atendido | Ver [Padrões de projeto utilizados](#padrões-de-projeto-utilizados) e o documento [`ARQUITETURA-E-DECISOES-TECNICAS.txt`](./ARQUITETURA-E-DECISOES-TECNICAS.txt) |
| README com pré-requisitos, passos para rodar e modo de funcionamento | Atendido | Este arquivo |
| Código-fonte em repositório público no GitHub | Atendido | Repositório já publicado como público |

**Requisitos opcionais**

| Requisito | Status | Onde |
|---|---|---|
| Desenho da solução / diagramas de arquitetura | Atendido | Diagrama de camadas em [Arquitetura](#arquitetura) e diagrama entidade-relacionamento em [Estrutura do banco de dados](#estrutura-do-banco-de-dados) |
| Processamento assíncrono, filas ou mensageria | Atendido | Fila produtor/consumidor em memória (`System.Threading.Channels`) + `BackgroundService` dedicado |
| Uso de containers (Docker) | Atendido | `Dockerfile` + `docker-compose.yml` (API + PostgreSQL) |

**Requisitos não funcionais**

| Requisito | Status | Onde |
|---|---|---|
| Gestão de lançamentos deve continuar operante mesmo com falha na consolidação | Atendido | Registro é sempre síncrono/imediato; consolidação é sempre assíncrona e desacoplada. Ver [Estratégia de resiliência](#estratégia-de-resiliência) |
| Suportar pico de 50 req/s tolerando até 5% de perda | Atendido | Fila limitada (bounded) não bloqueante + retry/circuit breaker + reconciliação periódica. Ver [Estratégia de resiliência](#estratégia-de-resiliência) |

Nenhum requisito do desafio ficou pendente. Itens que vão além do mínimo pedido (e o porquê de cada um) estão detalhados em [Melhorias futuras](#melhorias-futuras) e no documento de arquitetura.

## Visão geral da solução

O domínio da aplicação é composto por dois conceitos centrais:

- **Lançamento (Launch)**: um crédito ou débito financeiro, associado a uma data, uma descrição e um valor. É a fonte única da verdade do fluxo de caixa.
- **Saldo diário (DailyBalance)**: uma projeção consolidada (derivada) dos lançamentos de uma determinada data, contendo o total de créditos, o total de débitos e o saldo de fechamento do dia.

O ponto mais sensível do desafio é o requisito não funcional de que o **registro de um lançamento nunca pode ficar indisponível ou lento por causa da consolidação diária**, mesmo sob picos de requisições. Por isso, a consolidação nunca acontece de forma síncrona dentro da requisição HTTP de registro: o lançamento é persistido de forma síncrona e imediata (fonte da verdade), e a atualização do saldo consolidado é feita de forma assíncrona, em background, por um worker dedicado.

## Arquitetura

O projeto segue os princípios da **Clean Architecture** combinados com **DDD (Domain-Driven Design)** em nível tático (entidades, objetos de valor, eventos de domínio) e **CQRS** para separar os fluxos de escrita e leitura através do MediatR. O racional completo de cada escolha está detalhado no arquivo [`ARQUITETURA-E-DECISOES-TECNICAS.txt`](./ARQUITETURA-E-DECISOES-TECNICAS.txt).

A solução é dividida em quatro camadas, com dependências apontando sempre para dentro (Api → Application → Domain; Infrastructure → Application/Domain):

```
┌─────────────────────────────────────────────────────────────┐
│                        CashFlow.Api                          │
│   Controllers · Contratos HTTP · Middleware de erros ·       │
│   Configuração do Swagger                                    │
└───────────────────────────┬───────────────────────────────────┘
                            │ depende de
┌───────────────────────────▼───────────────────────────────────┐
│                    CashFlow.Application                       │
│   Commands/Queries (CQRS) · Handlers (MediatR) · Validações    │
│   (FluentValidation) · Behaviors de pipeline · Interfaces       │
└───────────────────────────┬───────────────────────────────────┘
                            │ depende de
┌───────────────────────────▼───────────────────────────────────┐
│                       CashFlow.Domain                          │
│   Entidades · Objetos de Valor · Eventos de Domínio ·           │
│   Exceções de negócio · Contratos de repositório                │
│             (não depende de nenhuma outra camada)               │
└───────────────────────────▲───────────────────────────────────┘
                            │ implementa as interfaces de
┌───────────────────────────┴───────────────────────────────────┐
│                    CashFlow.Infrastructure                     │
│   EF Core + PostgreSQL · Fila de consolidação em memória ·      │
│   Workers em background · Políticas de resiliência (Polly)      │
└─────────────────────────────────────────────────────────────────┘
```

### Fluxo de registro de um lançamento

1. `POST /api/v1/launches` chega ao `LaunchesController`, que apenas traduz a requisição HTTP em um `RegisterLaunchCommand` e o envia ao MediatR.
2. O pipeline do MediatR executa os *behaviors* registrados (`LoggingBehavior` e `ValidationBehavior`) antes do handler.
3. O `RegisterLaunchCommandHandler` cria o agregado `Launch` (que valida seus próprios invariantes e dispara o evento de domínio `LaunchRegisteredEvent`), persiste-o via `ILaunchRepository` + `IUnitOfWork` e confirma a transação.
4. **Somente depois** de o lançamento estar persistido, o handler tenta enfileirar um sinal de consolidação (`IConsolidationQueue.TryEnqueue`) para aquela data. Esse envio é *best-effort*: se a fila estiver saturada, o sinal é descartado e apenas registrado em log — o lançamento **nunca é perdido**, pois já foi persistido no passo anterior.
5. Em background, o `ConsolidationWorker` consome a fila e recalcula o saldo daquela data através do `ConsolidateDailyBalanceCommandHandler`, aplicando políticas de retry e circuit breaker (Polly) para absorver falhas transitórias.
6. Um `ReconciliationWorker` roda periodicamente como rede de segurança final, reconsolidando qualquer data que tenha lançamentos mas ainda não possua um saldo diário com status `Consolidated` — cobrindo tanto sinais descartados quanto falhas que esgotaram todas as tentativas de retry.

Como a consolidação é **idempotente** (ela sempre recalcula o saldo do zero a partir dos lançamentos daquela data), reprocessar a mesma data múltiplas vezes nunca gera duplicidade ou inconsistência.

## Padrões de projeto utilizados

| Padrão | Onde é usado | Por quê |
|---|---|---|
| Clean Architecture | Divisão em `Domain`/`Application`/`Infrastructure`/`Api` | Isola a regra de negócio de detalhes técnicos, permitindo que banco, framework web ou novos módulos mudem sem tocar em código de domínio já validado |
| CQRS | `Commands` e `Queries` na camada Application | Separa claramente operações de escrita e leitura, cada uma com seu próprio modelo e handler |
| Mediator | MediatR (`ISender`) | Desacopla os controllers dos handlers de caso de uso; um novo módulo só adiciona seus próprios Commands/Queries sem alterar código existente |
| Decorator (pipeline behaviors) | `ValidationBehavior`, `LoggingBehavior` | Aplica validação e logging de forma transversal a todos os Commands/Queries, sem duplicar código em cada handler |
| Repository | `ILaunchRepository`, `IDailyBalanceRepository` | Abstrai a persistência por trás de interfaces definidas no Domain, implementadas pela Infrastructure (Dependency Inversion) |
| Unit of Work | `IUnitOfWork` | Abstrai o commit da transação sem expor o `DbContext` do EF Core às camadas internas |
| Aggregate Root / Entity | `Launch`, `DailyBalance` | `Launch` é o único ponto de entrada permitido para criar/alterar um lançamento, garantindo que nunca exista em estado inválido |
| Value Object | `Money` | Imutável e autovalidável: impossível instanciar um valor monetário zero ou negativo |
| Domain Events | `LaunchRegisteredEvent` | O agregado apenas anuncia o que aconteceu, sem saber quem reage ao evento, mantendo o Domain livre de preocupações de infraestrutura |
| Factory Method | `Launch.Create(...)`, `Money.Create(...)` | Garante que os invariantes são validados na única porta de entrada de criação do objeto |
| Producer/Consumer | `InMemoryConsolidationQueue` + `ConsolidationWorker` | Desacopla o registro do lançamento (produtor) da consolidação do saldo (consumidor) |
| Retry + Circuit Breaker | `ConsolidationResiliencePipelineFactory` (Polly) | Absorve falhas transitórias e evita sobrecarregar uma dependência já degradada |
| Dependency Injection / DIP | Em toda a base, via `AddApplication()`/`AddInfrastructure()` | As camadas internas dependem apenas de interfaces, nunca de implementações concretas |

Cada um desses padrões está justificado em detalhe (incluindo os trade-offs considerados) no documento [`ARQUITETURA-E-DECISOES-TECNICAS.txt`](./ARQUITETURA-E-DECISOES-TECNICAS.txt).

## Stack técnica

| Camada / Preocupação | Tecnologia |
|---|---|
| Linguagem / Runtime | C# 12 / .NET 8 |
| Framework Web | ASP.NET Core Web API |
| Mediador CQRS | MediatR |
| Validação | FluentValidation |
| ORM | Entity Framework Core 8 |
| Banco de dados | PostgreSQL 16 (via Npgsql) |
| Resiliência | Polly (retry + circuit breaker) |
| Fila / mensageria | `System.Threading.Channels` (fila em memória, produtor/consumidor) |
| Documentação da API | Swagger / OpenAPI (Swashbuckle) |
| Testes de unidade | xUnit, FluentAssertions, NSubstitute |
| Testes de integração | xUnit, WebApplicationFactory, Testcontainers (PostgreSQL real em container) |
| Containerização | Docker / Docker Compose |

## Estrutura de pastas

```
src/
  CashFlow.Api/              → Camada de apresentação (Controllers, Program.cs, Middleware)
  CashFlow.Application/      → Casos de uso (Commands, Queries, Handlers, Validators, DTOs)
  CashFlow.Domain/           → Entidades, Value Objects, Eventos e regras de negócio puras
  CashFlow.Infrastructure/   → Persistência (EF Core/PostgreSQL), fila em memória, workers, Polly
tests/
  CashFlow.UnitTests/        → Testes de unidade (Domain, Application, Infrastructure isolada)
  CashFlow.IntegrationTests/ → Testes de integração ponta a ponta (API + PostgreSQL real via Testcontainers)
ARQUITETURA-E-DECISOES-TECNICAS.txt → Racional detalhado de cada decisão técnica
Dockerfile
docker-compose.yml
CashFlow.sln
```

## Estrutura do banco de dados

O banco de dados é **PostgreSQL**, e o schema é gerado e versionado inteiramente por **migrations do Entity Framework Core** (pasta `src/CashFlow.Infrastructure/Migrations`) — não há necessidade de criar tabelas manualmente, pois isso é feito automaticamente na inicialização da aplicação (`dbContext.Database.MigrateAsync()` em `Program.cs`).

Existem apenas duas tabelas, refletindo os dois conceitos do domínio:

```
┌───────────────────────────────────┐        ┌────────────────────────────────────────┐
│              launches             │        │            daily_balances              │
├───────────────────────────────────┤        ├────────────────────────────────────────┤
│ Id                uuid       (PK) │        │ Id                  uuid          (PK) │
│ Description       varchar(200)    │        │ ReferenceDate       date       (UQ)*   │
│ amount             numeric(18,2)  │        │ total_credits       numeric(18,2)      │
│ Type               varchar(20)    │        │ total_debits        numeric(18,2)      │
│ launch_date        date      (IX) │◄──┐    │ closing_balance     numeric(18,2)      │
│ created_at_utc     timestamptz    │   │    │ Status              varchar(20)        │
└───────────────────────────────────┘   │    │ ConsolidatedAtUtc   timestamptz (null) │
                                        │    │ FailedAttempts      integer            │
                    agregados por data  └────┤ FailureReason       varchar(1000) null │
                    (sem FK física —          │ RowVersion          bytea (concorrência)│
                    relação é lógica,          └────────────────────────────────────────┘
                    calculada em runtime)

  (PK) = chave primária   (UQ) = índice único   (IX) = índice não único
  * ux_daily_balances_reference_date garante uma única linha por data
```

**Tabela `launches`** — fonte única da verdade; nunca é alterada ou apagada após criada (append-only).

| Coluna | Tipo | Nulo? | Descrição |
|---|---|---|---|
| `Id` | `uuid` | não | Identificador do lançamento (chave primária). |
| `Description` | `varchar(200)` | não | Descrição do lançamento. |
| `amount` | `numeric(18,2)` | não | Valor monetário, sempre estritamente positivo (invariante do Value Object `Money`). |
| `Type` | `varchar(20)` | não | `Credit` (entrada) ou `Debit` (saída), armazenado por nome, não por número. |
| `launch_date` | `date` | não | Data a que o lançamento se refere. Possui índice (`ix_launches_launch_date`) para acelerar consultas por data. |
| `created_at_utc` | `timestamp with time zone` | não | Data/hora (UTC) em que o registro foi criado no sistema. |

**Tabela `daily_balances`** — projeção derivada e idempotente; sempre recalculada por completo a partir de `launches`, nunca incrementada.

| Coluna | Tipo | Nulo? | Descrição |
|---|---|---|---|
| `Id` | `uuid` | não | Identificador do saldo diário (chave primária). |
| `ReferenceDate` | `date` | não | Data a que o saldo se refere. Possui índice único (`ux_daily_balances_reference_date`) — garante, no próprio banco, que a consolidação é idempotente e nunca cria duplicidade para a mesma data. |
| `total_credits` | `numeric(18,2)` | não | Soma de todos os créditos da data. |
| `total_debits` | `numeric(18,2)` | não | Soma de todos os débitos da data. |
| `closing_balance` | `numeric(18,2)` | não | `total_credits - total_debits`. Pode ser negativo. |
| `Status` | `varchar(20)` | não | `Pending`, `Consolidated` ou `Failed` (ciclo de vida da consolidação). |
| `ConsolidatedAtUtc` | `timestamp with time zone` | sim | Data/hora (UTC) da última consolidação bem-sucedida. |
| `FailedAttempts` | `integer` | não | Contador de tentativas de consolidação que falharam consecutivamente. |
| `FailureReason` | `varchar(1000)` | sim | Mensagem da última falha de consolidação, quando `Status = Failed`. |
| `RowVersion` | `bytea` | sim | Token de concorrência otimista (rowversion): impede que duas consolidações concorrentes para a mesma data se sobrescrevam silenciosamente sob alta carga. |

Não existe uma foreign key física entre `daily_balances` e `launches`: a relação é lógica (por `ReferenceDate`/`launch_date`) e calculada em tempo de execução pela consolidação, exatamente porque `daily_balances` é uma **projeção**, não uma entidade independente com vida própria.

## Como executar o projeto

### Opção 1 — Docker Compose (recomendada)

Pré-requisitos: Docker e Docker Compose instalados.

```bash
docker compose up --build
```

Isso sobe dois serviços: o banco de dados PostgreSQL (`cashflow-postgres`, porta `5432`) e a API (`cashflow-api`, porta `8080`). As migrations do Entity Framework Core são aplicadas automaticamente na inicialização da aplicação — não é necessário nenhum passo manual de setup do banco. Após a subida:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`

### Opção 2 — Execução local com .NET SDK

Pré-requisitos: .NET 8 SDK e uma instância de PostgreSQL acessível (pode ser a mesma do `docker-compose.yml`, subindo só o serviço `postgres`: `docker compose up postgres`).

```bash
# A connection string padrão já aponta para localhost:5432 (ver
# src/CashFlow.Api/appsettings.json). Ajuste-a se necessário, ou
# defina a variável de ambiente ConnectionStrings__CashFlowDatabase.

dotnet restore CashFlow.sln
dotnet build CashFlow.sln
dotnet run --project src/CashFlow.Api
```

As migrations também são aplicadas automaticamente ao iniciar a aplicação neste modo. Por padrão (`launchSettings.json`), a aplicação sobe em `http://localhost:5080` com `ASPNETCORE_ENVIRONMENT=Development`:

- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Health check: `http://localhost:5080/health`

### Executando os testes

```bash
# Testes de unidade (não exigem Docker nem banco de dados)
dotnet test tests/CashFlow.UnitTests/CashFlow.UnitTests.csproj

# Testes de integração (exigem Docker em execução - usam Testcontainers
# para subir um PostgreSQL real e descartável a cada execução)
dotnet test tests/CashFlow.IntegrationTests/CashFlow.IntegrationTests.csproj

# Toda a solução de uma vez
dotnet test CashFlow.sln
```

## Documentação da API (Swagger)

A API é inteiramente documentada via **Swagger / OpenAPI**, gerado automaticamente pela biblioteca Swashbuckle a partir dos próprios controllers — incluindo os comentários `///` de cada endpoint (resumo, parâmetros e o significado de cada código de resposta HTTP), que são exportados como arquivo XML e injetados na UI (`options.IncludeXmlComments(...)` em `Program.cs`).

**Como acessar:**

| Cenário | URL do Swagger UI | URL do JSON OpenAPI |
|---|---|---|
| Docker Compose | `http://localhost:8080/swagger` | `http://localhost:8080/swagger/v1/swagger.json` |
| `dotnet run` local | `http://localhost:5080/swagger` | `http://localhost:5080/swagger/v1/swagger.json` |

Pela UI do Swagger é possível ler a descrição de cada endpoint, ver o formato exato de request/response, e também **executar as chamadas diretamente pelo navegador** (botão "Try it out"), sem precisar de um cliente HTTP externo (Postman, curl etc.).

> O Swagger só é habilitado quando `ASPNETCORE_ENVIRONMENT=Development` (é assim que o `docker-compose.yml` e o `launchSettings.json` já estão configurados por padrão). Essa é uma prática comum de segurança: a documentação interativa (e a possibilidade de disparar chamadas por ela) não fica exposta publicamente em um ambiente de produção real.

## Endpoints da API

Resumo dos endpoints (a documentação completa e interativa está no Swagger, endereço acima).

### Lançamentos

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/v1/launches` | Registra um novo lançamento de crédito ou débito. |
| `GET` | `/api/v1/launches?date={date}` | Lista todos os lançamentos registrados em uma data (`yyyy-MM-dd`). |

Corpo esperado por `POST /api/v1/launches`:

```json
{
  "description": "Venda de produto X",
  "amount": 150.75,
  "type": "Credit",
  "launchDate": "2025-01-15"
}
```

Regras de validação aplicadas (`RegisterLaunchCommandValidator`):
- `description`: obrigatória, no máximo 200 caracteres.
- `amount`: deve ser estritamente maior que zero.
- `type`: deve ser `Credit` (entrada) ou `Debit` (saída).
- `launchDate`: opcional (assume a data atual quando omitida); não pode ser uma data futura.

### Saldo diário consolidado

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/v1/daily-balances/{date}` | Retorna o saldo consolidado de uma única data. |
| `GET` | `/api/v1/daily-balances?startDate=&endDate=` | Retorna o relatório de saldo diário consolidado em um intervalo de datas (`endDate` deve ser maior ou igual a `startDate`, e o intervalo não pode exceder 366 dias). |
| `POST` | `/api/v1/daily-balances/{date}/consolidate` | Força a (re)consolidação síncrona de uma data específica (útil para demonstração e recuperação operacional manual). |

### Tratamento de erros

Todas as respostas de erro são mapeadas centralmente pelo `GlobalExceptionMiddleware`, com status HTTP consistente:

| Situação | Status HTTP |
|---|---|
| Falha de validação de entrada (FluentValidation) | `400 Bad Request` |
| Recurso não encontrado (ex.: saldo ainda não consolidado para a data) | `404 Not Found` |
| Violação de regra de negócio/invariante de domínio (ex.: valor inválido) | `422 Unprocessable Entity` |
| Erro inesperado não tratado | `500 Internal Server Error` |

## Estratégia de resiliência

O requisito não funcional mais crítico do desafio — manter o registro de lançamentos sempre disponível, mesmo sob o pico documentado de 50 requisições/segundo com até 5% de perda tolerável — foi resolvido combinando:

1. **Separação de escrita e consolidação**: o lançamento é a fonte da verdade e é sempre persistido de forma síncrona; a consolidação é derivada e sempre assíncrona.
2. **Fila em memória limitada (bounded) e não bloqueante**: implementada sobre `System.Threading.Channels`, garante que o caminho de escrita nunca espera pela fila. Sob saturação extrema, um sinal de consolidação pode ser descartado — mas isso apenas atrasa a visibilidade do saldo, nunca perde dados financeiros (o lançamento já foi salvo antes desse ponto).
3. **Retry com backoff exponencial + Circuit Breaker (Polly)**: absorve falhas transitórias na consolidação (ex.: instabilidade momentânea do banco) sem sobrecarregar uma dependência já degradada.
4. **Job de reconciliação periódica**: rede de segurança final que reprocessa qualquer data com lançamentos e sem saldo consolidado, cobrindo tanto sinais descartados quanto falhas que esgotaram os retries.
5. **Idempotência da consolidação**: por recalcular o saldo do zero a cada execução, qualquer reprocessamento (retry, reconciliação, chamada manual) é sempre seguro.

Essa combinação atende ao pico de carga documentado sem exigir infraestrutura externa de mensageria — a fila em memória é a solução intencionalmente mais simples para o escopo atual (uma única instância da aplicação), e a interface `IConsolidationQueue` foi desenhada para ser substituída por um broker real (RabbitMQ, Azure Service Bus, Amazon SQS) caso a aplicação precise escalar horizontalmente em múltiplas instâncias. Detalhes de implementação estão documentados diretamente no código-fonte (`InMemoryConsolidationQueue.cs`) e no arquivo de arquitetura.

## Testes

O projeto possui duas suítes de teste com responsabilidades distintas:

- **`CashFlow.UnitTests`** (xUnit + FluentAssertions + NSubstitute): cobre regras de domínio (invariantes de `Launch` e `Money`), validadores do FluentValidation, handlers da Application (com dependências substituídas por dublês de teste) e o comportamento da fila de consolidação em memória (incluindo o cenário de saturação/descarte). Não depende de infraestrutura externa — roda em qualquer máquina apenas com o .NET SDK.
- **`CashFlow.IntegrationTests`** (xUnit + WebApplicationFactory + Testcontainers): sobe a aplicação real via `WebApplicationFactory` contra um PostgreSQL real e descartável, provisionado automaticamente pelo Testcontainers (exige Docker em execução). Cobre o fluxo ponta a ponta dos endpoints (registro de lançamento, consulta por data, consolidação síncrona e por intervalo, idempotência da consolidação).

## Decisões de projeto

Para uma explicação detalhada de cada padrão de projeto utilizado, das camadas da arquitetura e do racional por trás de cada decisão técnica (incluindo trade-offs considerados), consulte o documento [`ARQUITETURA-E-DECISOES-TECNICAS.txt`](./ARQUITETURA-E-DECISOES-TECNICAS.txt).

## Melhorias futuras

O desafio pede explicitamente para registrar aqui ideias que ficaram fora do escopo por limitação de tempo, mas que fariam sentido em uma evolução real do sistema:

- **Broker de mensageria real** (RabbitMQ, Azure Service Bus ou Amazon SQS) no lugar da fila em memória, caso a aplicação precise escalar horizontalmente em múltiplas instâncias — a interface `IConsolidationQueue` já isola essa troca sem exigir mudanças na Application ou no Domain.
- **Autenticação e autorização** (por exemplo, JWT) para os endpoints, hoje deliberadamente abertos por não fazer parte do escopo do desafio.
- **Paginação** no endpoint de listagem de lançamentos por data, para o cenário de um comércio com um volume muito alto de lançamentos em um único dia.
- **Observabilidade** mais completa: métricas (ex.: OpenTelemetry) sobre profundidade da fila de consolidação, taxa de sinais descartados e estado do circuit breaker, que hoje só são visíveis via log.
- **Múltiplas moedas**, caso o negócio precise operar em mais de um país/moeda no futuro.
- **Versionamento de API** mais explícito (a rota já usa o prefixo `v1`, mas hoje existe apenas uma versão) para suportar evolução de contrato sem quebrar clientes existentes.
- **Módulos adicionais** (ex.: contas a pagar/receber, conciliação bancária) aproveitando a mesma Clean Architecture — cada novo módulo adicionaria seus próprios Commands/Queries/Handlers sem exigir alteração no código já existente.
