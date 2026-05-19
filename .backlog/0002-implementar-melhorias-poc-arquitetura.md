# [0002] Implementar melhorias arquiteturais da PoC

## Status
- [ ] Pendente
- [ ] Em progresso
- [x] Concluido

## Contexto
Com base na analise arquitetural registrada em `0001`, a PoC precisa ser estabilizada para entrevista: build funcional, autenticacao simples por API key efetivamente aplicada, fluxo financeiro coerente, worker diario operacional, frontend respeitando boundaries e README atualizado com a arquitetura implementada e evolucoes futuras.

## Objetivo
Entregar uma PoC executavel e arquiteturalmente consistente, preservando simplicidade adequada para entrevista e melhorando resiliencia, integridade e clareza da solucao.

## Escopo
- Corrigir o baseline de build relacionado ao AppHost/Aspire
- Tornar a autenticacao por API key efetiva e configuravel
- Corrigir o fluxo de persistencia de lancamentos com idempotencia
- Corrigir o fluxo de saldo consolidado diario e seu cache
- Ativar e corrigir o worker diario
- Ajustar frontend para consumir a API em vez de acessar banco diretamente
- Corrigir e executar build/testes pertinentes
- Atualizar o README com arquitetura, trade-offs e proximas evolucoes

- Nao esta incluido elevar a autenticacao para JWT/OAuth
- Nao esta incluido redesenhar toda a topologia da solucao

## Criterios de Aceite
- [x] A solution ou o escopo principal volta a buildar
- [x] A API key e exigida de fato pelas APIs
- [x] O fluxo de lancamentos evita perda de precisao monetaria
- [x] O worker de consolidacao diaria esta registrado e com logica correta de agendamento
- [x] O frontend deixa de ler `DbContext` diretamente para listar lancamentos
- [x] O README reflete a arquitetura atual e as evolucoes futuras
- [x] Evidencias de validacao foram registradas

## Plano de Execucao
- [x] Registrar item de backlog
- [x] Corrigir baseline do AppHost/Aspire
- [x] Refatorar autenticacao por API key para configuracao
- [x] Refatorar persistencia de lancamentos e contrato de mensageria
- [x] Refatorar saldo consolidado e cache
- [x] Corrigir workers e scheduling
- [x] Ajustar frontend e boundary de acesso a dados
- [x] Atualizar testes e validar build/test
- [x] Atualizar README e knowledge
- [x] Concluir com commit local

## Decisoes Tecnicas
- A PoC mantera API key simples, conforme alinhado pelo usuario.
- O desenho com servico de lancamentos e servico de saldo consolidado sera preservado.
- Redis sera usado como cache de leitura de curta duracao, nao como fonte de verdade financeira.

## Descobertas
- O baseline do Aspire estava misturando AppHost `9.2.0` com pacotes de runtime `8.1.0`, o que introduzia risco real de incompatibilidade operacional.
- A interface de cache monetario usando `long` quebrava precisao financeira e precisava ser trocada por `decimal`.
- O fluxo assíncrono precisava de idempotencia no banco, nao apenas no broker, para evitar duplicidade em retries.
- O schema de testes ficou mais previsivel ao consolidar a migration inicial com o modelo atual da PoC.
- O smoke test de `GET /` ficou mais estavel quando passou a validar o `Contoso.Web` diretamente; o teste distribuido completo via AppHost/Kafka fica como proxima evolucao.

## Follow-ups de Conhecimento
- Registrar no knowledge do repositorio que servicos sem dependencia real de mensageria nao devem esperar Kafka no AppHost.
- Registrar no knowledge que upgrades de AppHost exigem alinhar os pacotes Aspire de runtime.

## Riscos
- Atualizacao do AppHost/Aspire pode exigir pequenos ajustes de compatibilidade.
- A interpretacao funcional de saldo consolidado sera mantida como saldo acumulado ate o fechamento do dia, coerente com o README original.

## Evidencias
- `0001-analise-arquitetural-desafio-jan25.md`
- `dotnet build Contoso.sln --nologo`
- `dotnet test Contoso.sln --no-build --nologo --blame-hang --blame-hang-timeout 5m`
- `README.md`

## Referencias
- `README.md`
- `desafio-arquiteto-software-jan25.pdf`
