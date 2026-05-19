# Conhecimento de Arquitetura

## Contoso Cash Flow

- Nao usar `Redis StringIncrement` com valores financeiros em `decimal(18,2)` sem uma estrategia explicita de escala e arredondamento; isso quebra a precisao monetaria e pode distorcer o saldo.
- Cache distribuido nao deve ser a fonte de verdade do saldo; em fluxos financeiros ele pode ser acelerador de leitura, mas a garantia final precisa estar em persistencia duravel e reconciliavel.
- Se a arquitetura separar `Transactions` e `DailyBalance`, o write model deve ser duravel e idempotente, e o read model deve ser projetado de forma assincrona com deduplicacao; caso contrario, o desacoplamento vira fonte de divergencia entre API, worker e cache.
- Frontends do repositorio devem consumir APIs/contratos do dominio em vez de acessar `DbContext` diretamente quando a proposta arquitetural for de servicos separados.
- Build/test baseline faz parte da definicao de pronto arquitetural: exemplos, testes e AppHost nao podem ficar presos a configuracao depreciada do SDK nem a testes que nem compilam.
- Quando o `Contoso.AppHost` for atualizado de versao, os pacotes Aspire de runtime (`Kafka`, `Redis`, `SqlServer` e afins) tambem precisam ser alinhados; manter `AppHost 9.x` com runtime `8.x` aumenta risco de comportamento operacional inconsistente.
- No AppHost, servicos que nao usam mensageria nao devem depender de `WithReference(messaging)` nem de `WaitFor(messaging)`; esse acoplamento operacional aumenta tempo de subida e expande superficie de falha sem ganho funcional.
