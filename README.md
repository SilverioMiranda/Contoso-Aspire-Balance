# Contoso Cash Flow Management

Este projeto foi desenvolvido para atender a necessidade de um comerciante em controlar seu fluxo de caixa di�rio, gerenciando lan�amentos (d�bitos e cr�ditos) e gerando relat�rios com o saldo di�rio consolidado.

## Tecnologias Utilizadas

- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- Docker
- .NET Framework 8
- SQL Server
- Kafka
- Blazor
- Redis

## Estrutura do Projeto

O projeto consiste em v�rias APIs e servi�os que trabalham juntos para garantir o controle eficiente do fluxo de caixa:

**Transactions API**
- Endere�o: `https://localhost:9001`
- Fun��o: Receber lan�amentos (cr�ditos e d�bitos) e envi�-los para um t�pico no Kafka.

**Exemplo de Requisi��o de Cria��o de Lan�amento:**
```bash
curl --location 'https://localhost:9001/lancamentos' \
--header 'X-API-KEY: contoso' \
--header 'Content-Type: application/json' \
--data '{
    "Amount": -500
}'
```
Esta requisi��o adiciona um lan�amento ao banco de dados, que pode ser um valor positivo (cr�dito) ou negativo (d�bito).

**API para Listar Lan�amentos do Dia:**
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
- Fun��o: Consumir as mensagens do Kafka em um `BackgroundService` e salvar os lan�amentos no banco de dados SQL Server.

## Estrutura de Projetos e Pastas

- **src/0. Host / Contoso.AppHost**
  - Servi�o do .NET Aspire que orquestra os containers e servi�os necess�rios para o funcionamento do projeto. Este servi�o gerencia a configura��o dos containers, inicia e monitora as inst�ncias das aplica��es e servi�os, e garante a comunica��o entre os componentes do sistema.

- **1. Applications**
  
  - **Balances**
    - **Contoso.DailyBalance.API**: Projeto que cont�m a API `/consolidado/{data}` que retorna o saldo consolidado do dia informado.
    - **Contoso.DailyBalance.Services**: Projeto que cont�m a implementa��o dos servi�os utilizados pela `Contoso.DailyBalance.API` e `Contoso.DailyBalance.Worker`.
    - **Contoso.DailyBalance.Worker**: Projeto que executa um `BackgroundService` todos os dias �s 00:15, respons�vel por salvar o balan�o do dia anterior.

      O c�lculo do saldo di�rio � feito da seguinte forma:

      1. **Verifica��o do �ltimo Saldo**: O saldo mais recente � obtido do banco de dados.
      2. **C�lculo do Saldo Inicial**: Se n�o houver saldo anterior, o saldo total � calculado somando todos os lan�amentos desde o in�cio.
      3. **Atualiza��o do Saldo**: Se houver um saldo anterior, o sistema verifica se ele cobre o dia anterior. Caso contr�rio, ele calcula o saldo somando os lan�amentos realizados desde o �ltimo saldo at� o �ltimo segundo do dia anterior.
      4. **Persist�ncia do Novo Saldo**: O novo saldo consolidado � salvo no banco de dados com a data correspondente.

      **Exemplo de C�lculo do Saldo:**
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
          // Verifica se a data da �ltima transa��o � anterior ao dia anterior
          if (ultimoSaldo.Date < now.AddDays(-1))
          {
              // Usa UTCNow - 15 minutos como data m�xima
              var maxDate = now.AddMinutes(-15);

              saldo = ultimoSaldo.Value + await dbContext.Transactions
                  .Where(t => t.CreatedAt > ultimoSaldo.Date && t.CreatedAt <= maxDate)
                  .SumAsync(t => t.Value, cancellationToken: stoppingToken)
                  .ConfigureAwait(false);
          }
          else
          {
              // Usa o �ltimo segundo do dia anterior como data m�xima
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
    - **Contoso.Transactions.API**: API que implementa os m�todos `POST /lancamentos`, que faz o cadastro de um lan�amento, e o `GET /lancamentos/{date:datetime}`, que retorna a lista de todos os lan�amentos do dia informado.
    - **Contoso.Transactions.Services**: Projeto que implementa os servi�os utilizados pela `.API` e `.Worker`.
    - **Contoso.Transactions.Worker**: Projeto que � respons�vel por ler os lan�amentos do Kafka e salv�-los no SQL Server.

- **Opcionalmente existe o projeto Contoso.Web com uma aplica��o Blazor.**

## Dashboard do .NET Aspire

O .NET Aspire oferece uma dashboard integrada que permite aos desenvolvedores monitorar e gerenciar os servi�os e containers que comp�em a aplica��o. Atrav�s dessa dashboard, � poss�vel:

- Visualizar o status de todos os containers e servi�os em tempo real.
- Monitorar logs centralizados de todos os servi�os.
- Gerenciar configura��es e vari�veis de ambiente dos containers.
- Executar comandos diretamente nos containers, como iniciar, parar ou reiniciar servi�os.
- Obter insights detalhados sobre a performance e a sa�de da aplica��o, facilitando o diagn�stico de problemas.

Essa dashboard � uma ferramenta essencial para garantir a alta disponibilidade e o bom funcionamento do sistema, especialmente em ambientes de produ��o.

## Desenho da Solu��o

Para uma vis�o geral da arquitetura e da solu��o, consulte o [desenho da solu��o](https://drive.google.com/file/d/1EVUvdUuXVhNjj6GVJVIOMZG1EnVjOWdF/view?usp=sharing).

## Como Executar o Projeto

1. Certifique-se de ter o [Docker](https://www.docker.com/) e o .NET Framework 8 instalados em sua m�quina.
2. Abra a solu��o no Visual Studio ou em outro IDE de sua prefer�ncia.
3. Defina a aplica��o `Contoso.AppHost` como o projeto de inicializa��o.
4. Execute a aplica��o.

## Objetivo

O objetivo principal deste projeto � fornecer uma solu��o escal�vel e resiliente para o controle de fluxo de caixa di�rio, utilizando tecnologias modernas e boas pr�ticas de arquitetura de software.

---

Sinta-se � vontade para explorar, contribuir ou relatar problemas. Esperamos que este projeto seja �til para o seu caso de uso!
