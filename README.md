# Contoso Cash Flow Management

Este projeto foi desenvolvido para atender a necessidade de um comerciante em controlar seu fluxo de caixa diário, gerenciando lançamentos (débitos e créditos) e gerando relatórios com o saldo diário consolidado.

## Tecnologias Utilizadas

- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- Docker
- .NET Framework 8
- SQL Server
- Kafka

## Estrutura do Projeto

O projeto consiste em duas APIs principais:

1. **Transactions API**
   - Endereço: `https://localhost:9001`
   - Função: Receber lançamentos (créditos e débitos) e enviá-los para um tópico no Kafka.
   
   **Exemplo de Requisição:**
   ```bash
   curl --location 'https://localhost:9001' \
   --header 'X-API-KEY: contoso' \
   --header 'Content-Type: application/json' \
   --data '{
       "Amount": -500
   }'
