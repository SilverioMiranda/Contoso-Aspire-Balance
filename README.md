# Contoso Cash Flow Management

Este projeto foi desenvolvido para atender a necessidade de um comerciante em controlar seu fluxo de caixa diário, gerenciando lançamentos (débitos e créditos) e gerando relatórios com o saldo diário consolidado.

## Tecnologias Utilizadas

- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- Docker
- .NET Framework 8
- SQL Server
- Kafka
- Blazor
- Redis

## Estrutura do Projeto

O projeto consiste em várias APIs e serviços que trabalham juntos para garantir o controle eficiente do fluxo de caixa:

**Transactions API**
- Endereço: `https://localhost:9001`
- Função: Receber lançamentos (créditos e débitos) e enviá-los para um tópico no Kafka.

**Exemplo de Requisição de Criação de Lançamento:**
```bash
curl --location 'https://localhost:9001/lancamentos' \
--header 'X-API-KEY: contoso' \
--header 'Content-Type: application/json' \
--data '{
    "Amount": -500
}'
```
Esta requisição adiciona um lançamento ao banco de dados, que pode ser um valor positivo (crédito) ou negativo (débito).

**API para Listar Lançamentos do Dia:**
```bash
curl --location 'https://localhost:9001/lancamentos/2024-08-13' \
--header 'X-API-KEY: contoso'
```

**API para Ver o Saldo Consolidado do Dia:**
```bash
curl --location 'https://localhost:9000/consolidado/2024-08-13' \
--header 'X-API-KEY: contoso'
```

**Contoso.Transactions.Worker**
- Função: Consumir as mensagens do Kafka em um `BackgroundService` e salvar os lançamentos no banco de dados SQL Server.

## Estrutura de Projetos e Pastas

- **src/0. Host / Contoso.AppHost**
  - Serviço do .NET Aspire que orquestra os containers e serviços necessários para o funcionamento do projeto. Este serviço gerencia a configuração dos containers, inicia e monitora as instâncias das aplicações e serviços, e garante a comunicação entre os componentes do sistema.

- **1. Applications**
  
  - **Balances**
    - **Contoso.DailyBalance.API**: Projeto que contém a API `/consolidado/{data}` que retorna o saldo consolidado do dia informado.
    - **Contoso.DailyBalance.Services**: Projeto que contém a implementação dos serviços utilizados pela `Contoso.DailyBalance.API` e `Contoso.DailyBalance.Worker`.
    - **Contoso.DailyBalance.Worker**: Projeto que executa um `BackgroundService` todos os dias às 00:15, responsável por salvar o balanço do dia anterior.

      O cálculo do saldo diário é feito da seguinte forma:

      1. **Verificação do Último Saldo**: O saldo mais recente é obtido do banco de dados.
      2. **Cálculo do Saldo Inicial**: Se não houver saldo anterior, o saldo total é calculado somando todos os lançamentos desde o início.
      3. **Atualização do Saldo**: Se houver um saldo anterior, o sistema verifica se ele cobre o dia anterior. Caso contrário, ele calcula o saldo somando os lançamentos realizados desde o último saldo até o último segundo do dia anterior.
      4. **Persistência do Novo Saldo**: O novo saldo consolidado é salvo no banco de dados com a data correspondente.

      **Exemplo de Cálculo do Saldo:**
      ```csharp
      var ultimoSaldo = await dbContext.Balances
          .OrderByDescending(x => x.Date)
          .FirstOrDefaultAsync(cancellationToken: stoppingToken)
          .ConfigureAwait(false);

      decimal saldo = 0;
      var now = timeProvider.GetUtcNow();
      var maxDatePreviousDay = now.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

      if (ultimoSaldo is null)
      {
          // Cache miss: Calcula o saldo total pela primeira vez
          saldo = await dbContext.Transactions
              .SumAsync(t => t.Value, cancellationToken: stoppingToken)
              .ConfigureAwait(false);
      }
      else
      {
          // Verifica se a data da última transação é anterior ao dia anterior
          if (ultimoSaldo.Date < now.AddDays(-1))
          {
              // Usa UTCNow - 15 minutos como data máxima
              var maxDate = now.AddMinutes(-15);

              saldo = ultimoSaldo.Value + await dbContext.Transactions
                  .Where(t => t.CreatedAt > ultimoSaldo.Date && t.CreatedAt <= maxDate)
                  .SumAsync(t => t.Value, cancellationToken: stoppingToken)
                  .ConfigureAwait(false);
          }
          else
          {
              // Usa o último segundo do dia anterior como data máxima
              saldo = ultimoSaldo.Value + await dbContext.Transactions
                  .Where(t => t.CreatedAt > ultimoSaldo.Date && t.CreatedAt <= maxDatePreviousDay)
                  .SumAsync(t => t.Value, cancellationToken: stoppingToken)
                  .ConfigureAwait(false);
          }
      }

      await dbContext.Balances.AddAsync(new Balance { Value = saldo, Date = maxDatePreviousDay }, stoppingToken)
          .ConfigureAwait(false);

      await dbContext.SaveChangesAsync().ConfigureAwait(false);
      ```

  - **Transactions**
    - **Contoso.Transactions.API**: API que implementa os métodos `POST /lancamentos`, que faz o cadastro de um lançamento, e o `GET /lancamentos/{date:datetime}`, que retorna a lista de todos os lançamentos do dia informado.
    - **Contoso.Transactions.Services**: Projeto que implementa os serviços utilizados pela `.API` e `.Worker`.
    - **Contoso.Transactions.Worker**: Projeto que é responsável por ler os lançamentos do Kafka e salvá-los no SQL Server.

- **Opcionalmente existe o projeto Contoso.Web com uma aplicação Blazor.**

## Dashboard do .NET Aspire

O .NET Aspire oferece uma dashboard integrada que permite aos desenvolvedores monitorar e gerenciar os serviços e containers que compõem a aplicação. Através dessa dashboard, é possível:

- Visualizar o status de todos os containers e serviços em tempo real.
- Monitorar logs centralizados de todos os serviços.
- Gerenciar configurações e variáveis de ambiente dos containers.
- Executar comandos diretamente nos containers, como iniciar, parar ou reiniciar serviços.
- Obter insights detalhados sobre a performance e a saúde da aplicação, facilitando o diagnóstico de problemas.

Essa dashboard é uma ferramenta essencial para garantir a alta disponibilidade e o bom funcionamento do sistema, especialmente em ambientes de produção.
![image](https://github.com/user-attachments/assets/2e9696a2-a229-4916-91d2-08059785508d)

## Desenho da Solução

Para uma visão geral da arquitetura e da solução, consulte o [desenho da solução](https://drive.google.com/file/d/1EVUvdUuXVhNjj6GVJVIOMZG1EnVjOWdF/view?usp=sharing).

## Como Executar o Projeto

1. Certifique-se de ter o [Docker](https://www.docker.com/) e o .NET Framework 8 instalados em sua máquina.
2. Abra a solução no Visual Studio ou em outro IDE de sua preferência.
3. Defina a aplicação `Contoso.AppHost` como o projeto de inicialização.
4. Execute a aplicação.

## Objetivo

O objetivo principal deste projeto é fornecer uma solução escalável e resiliente para o controle de fluxo de caixa diário, utilizando tecnologias modernas e boas práticas de arquitetura de software.

---

Sinta-se à vontade para explorar, contribuir ou relatar problemas. Esperamos que este projeto seja útil para o seu caso de uso!
