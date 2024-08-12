# Contoso Cash Flow Management

Este projeto foi desenvolvido para atender a necessidade de um comerciante em controlar seu fluxo de caixa di�rio, gerenciando lan�amentos (d�bitos e cr�ditos) e gerando relat�rios com o saldo di�rio consolidado.

## Tecnologias Utilizadas

- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- Docker
- .NET Framework 8
- SQL Server
- Kafka

## Estrutura do Projeto

O projeto consiste em duas APIs principais:

1. **Transactions API**
   - Endere�o: `https://localhost:9001`
   - Fun��o: Receber lan�amentos (cr�ditos e d�bitos) e envi�-los para um t�pico no Kafka.
   
   **Exemplo de Requisi��o:**
   ```bash
   curl --location 'https://localhost:9001' \
   --header 'X-API-KEY: contoso' \
   --header 'Content-Type: application/json' \
   --data '{
       "Amount": -500
   }'
