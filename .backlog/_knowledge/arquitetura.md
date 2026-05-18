# Conhecimento de Arquitetura

## Contoso Cash Flow

- Nao usar `Redis StringIncrement` com valores financeiros em `decimal(18,2)` sem uma estrategia explicita de escala e arredondamento; isso quebra a precisao monetaria e pode distorcer o saldo.
- Cache distribuido nao deve ser a fonte de verdade do saldo; em fluxos financeiros ele pode ser acelerador de leitura, mas a garantia final precisa estar em persistencia duravel e reconciliavel.
- Se a arquitetura separar `Transactions` e `DailyBalance`, o write model deve ser duravel e idempotente, e o read model deve ser projetado de forma assincrona com deduplicacao; caso contrario, o desacoplamento vira fonte de divergencia entre API, worker e cache.
- Frontends do repositorio devem consumir APIs/contratos do dominio em vez de acessar `DbContext` diretamente quando a proposta arquitetural for de servicos separados.
- Build/test baseline faz parte da definicao de pronto arquitetural: exemplos, testes e AppHost nao podem ficar presos a configuracao depreciada do SDK nem a testes que nem compilam.
