# [0001] Analise arquitetural do desafio jan25

## Status
- [ ] Pendente
- [ ] Em progresso
- [x] Concluido

## Contexto
O repositorio contem uma solucao Contoso baseada em .NET/Aspire e um documento de desafio arquitetural (`desafio-arquiteto-software-jan25.pdf`). O usuario solicitou uma analise do projeto com identificacao de problemas, oportunidades de melhoria e um plano de implementacao orientado a melhores praticas de arquitetura de software.

## Objetivo
Produzir um diagnostico arquitetural baseado no desafio e no codigo existente, com riscos, melhorias priorizadas, perguntas estrategicas e um plano de implementacao executavel.

## Escopo
- Ler o desafio arquitetural e a documentacao local relevante
- Inspecionar a estrutura da solucao e os principais projetos
- Identificar problemas arquiteturais, tecnicos e operacionais
- Montar um plano de implementacao priorizado
- Registrar descobertas e evidencias no backlog

- Nao esta incluido implementar mudancas funcionais nesta atividade
- Nao esta incluido executar refatoracoes amplas nesta atividade

## Criterios de Aceite
- [x] O desafio arquitetural foi lido e sintetizado
- [x] A estrutura da solucao foi mapeada a partir do codigo real
- [x] Problemas e melhorias foram identificados com base em evidencias
- [x] Foi produzido um plano de implementacao priorizado
- [x] Descobertas e evidencias foram registradas

## Plano de Execucao
- [x] Registrar a atividade em `.backlog/`
- [x] Ler o PDF do desafio e extrair seus requisitos
- [x] Revisar documentacao principal do repositorio
- [x] Inspecionar os principais projetos e dependencias
- [x] Consolidar diagnostico arquitetural
- [x] Montar plano de implementacao
- [x] Atualizar knowledge e concluir a atividade

## Decisoes Tecnicas
- A analise sera baseada no codigo real do workspace e na documentacao disponivel no repositorio.
- O escopo desta atividade e diagnostico + plano, sem alterar o comportamento da aplicacao.
- O plano proposto preserva a separacao logica entre servico de lancamentos e servico de consolidado, mas recomenda fortalecer a durabilidade, a idempotencia e a coerencia entre write model, read model e cache.

## Descobertas
- A pasta `.github/` foi adicionada durante a atividade e agora contem as instrucoes canonicamente referenciadas por `AGENTS.md`.
- O fluxo atual de dinheiro perde precisao monetaria porque cache e incrementos usam `long` enquanto os valores de dominio usam `decimal(18,2)`.
- O `Contoso.DailyBalance.Worker` nao registra `BalanceWorker`, entao a consolidacao diaria nao executa no estado atual.
- Mesmo se registrado, o `BalanceWorker` executaria continuamente a cada minuto apos o primeiro disparo do cron, gerando risco de multiplas consolidacoes para a mesma data.
- O `DailyBalanceService` nao usa a tabela `balances`; ele soma `transactions` do dia e devolve o resultado como se fosse o consolidado, divergindo da modelagem descrita no `README.md`.
- O `TransactionsWorker` nao possui protecao de idempotencia e pode duplicar registros se houver falha entre `SaveChangesAsync` e `Commit` do offset.
- O frontend Blazor viola o boundary dos servicos ao consultar `ContosoDbContext` diretamente em vez de consumir a API de lancamentos.
- O baseline de build/testes esta quebrado: `dotnet build Contoso.sln` falha com `NETSDK1228` no AppHost, o teste `DailyBalanceServiceTest` nao compila por tipo incorreto de cache, e `dotnet test Contoso.sln --no-build` nao encontra assemblies porque o build falha antes.
- O pacote `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.9.0 esta vulneravel segundo `dotnet list package --vulnerable --include-transitive`.

## Follow-ups de Conhecimento
- Registrar padrao permanente para este repositorio: nao usar cache distribuido como fonte de verdade de valores financeiros e nao converter montantes monetarios de `decimal` para `long` sem escala explicita.
- Backlog recomendado: Sim
- Sugestao de item futuro: `Refatorar fluxo de saldo para write model duravel + outbox + read model idempotente`

## Riscos
- O desafio pode exigir interpretacao adicional se o PDF estiver com texto pouco extraivel.
- O repositorio aparenta estar sem baseline commitado, entao qualquer evidencia de historico tera de vir do estado atual dos arquivos.
- O significado de `saldo diario consolidado` precisa ser alinhado antes da implementacao: saldo acumulado de fechamento ou saldo liquido do dia. O codigo e a documentacao divergem hoje.
- A estrategia de autenticacao esperada para o desafio tambem precisa de alinhamento: chave de API simples para ambiente de exercicio ou autenticacao mais robusta.

## Evidencias
- `AGENTS.md`
- `.github/copilot-instructions.md`
- `.codex/skills/backlog-operacional-baseado-em-arquivos/SKILL.md`
- `desafio-arquiteto-software-jan25.pdf`
- `README.md`
- `DESIGN.md`
- `src/Contoso.Transactions.Services/TransactionQueueService.cs`
- `src/Contoso.DailyBalance.Services/DailyBalanceService.cs`
- `src/Contoso.DailyBalance.Worker/Program.cs`
- `src/Contoso.DailyBalance.Worker/BalanceWorker.cs`
- `src/Contoso.DailyBalance.Worker/CronBackgroundWorker.cs`
- `src/Contoso.Transactions.Worker/TransactionsWorker.cs`
- `src/Contoso.Web/Components/Pages/Lancamentos.razor`
- `src/Contoso.ServiceDefaults/ApiKeyValidator.cs`
- `src/Contoso.Data/Migrations/20240813035212_inicio.cs`
- `tests/Contoso.DailyBalance.Tests/DailyBalanceServiceTest.cs`
- Comandos executados:
  - `dotnet build Contoso.sln --nologo`
  - `dotnet test Contoso.sln --nologo --no-build`
  - `dotnet list src\\Contoso.ServiceDefaults\\Contoso.ServiceDefaults.csproj package --vulnerable --include-transitive`

## Referencias
- `desafio-arquiteto-software-jan25.pdf`
- `README.md`
- `DESIGN.md`
