# Banking System - Microservices com .NET 9 e Aspire

Sistema de exemplo para demonstração no TDC São Paulo 2025, implementando microserviços bancários com .NET 9, Aspire, CQRS, RabbitMQ e Redis.

## 🏗️ Arquitetura

O sistema utiliza **.NET Aspire** para orquestração de microserviços e é composto por:

### 1. **Account API** (porta 5000)
- Gerenciamento de contas bancárias
- Operações: criar, editar, inativar e consultar contas
- Consulta de saldo e transações
- Cache com Redis usando OutputCache
- Integração com Transaction API via Service Discovery

### 2. **Transaction API** (porta 5001)
- Gerenciamento de transações
- Tipos: Depósito, Saque, Compra, Salário, Estorno
- Publicação de eventos no RabbitMQ
- Cache com Redis

### 3. **Processor** (Worker Service)
- Processamento assíncrono de mensagens
- Atualização de saldos
- Criação de transações de estorno
- Consumidor RabbitMQ

### 4. **AppHost** (Orquestrador Aspire)
- Gerenciamento centralizado de todos os serviços
- Service Discovery automático
- Dashboard integrado para monitoramento
- Configuração automática de dependências

## 🚀 Como Executar

### Pré-requisitos
- .NET 9 SDK
- Docker Desktop
- .NET Aspire workload

### Instalar Aspire Workload

```bash
dotnet workload update
dotnet workload install aspire
```

### Executar com Aspire

```bash
# Na raiz do projeto
cd src/BankingSystem.AppHost
dotnet run
```

Isso iniciará automaticamente:
- **Aspire Dashboard** - http://localhost:15888
- **Account API** - http://localhost:5000
- **Transaction API** - http://localhost:5001
- **Processor** (Worker Service)
- **PostgreSQL** (2 instâncias)
- **Redis**
- **RabbitMQ** com Admin UI

### Dashboard Aspire

Acesse http://localhost:15888 para:
- Visualizar todos os serviços em execução
- Monitorar logs em tempo real
- Acompanhar métricas e traces
- Gerenciar recursos e dependências

## 📝 APIs Disponíveis

### Account API (http://localhost:5000)

- `POST /api/accounts` - Criar conta
- `PUT /api/accounts/{number}` - Editar conta
- `DELETE /api/accounts/{number}` - Inativar conta
- `GET /api/accounts/{number}` - Consultar conta
- `GET /api/accounts/{number}/balance` - Consultar saldo
- `GET /api/accounts/{number}/transactions` - Consultar transações

### Transaction API (http://localhost:5001)

- `POST /api/transactions` - Criar transação
- `DELETE /api/transactions/{id}` - Cancelar transação
- `GET /api/transactions/account/{accountNumber}` - Listar transações por período

## 📡 RabbitMQ

Admin UI: http://localhost:15672
- Usuário: admin
- Senha: admin

### Eventos:
- `transaction.created` - Publicado ao criar transação
- `transaction.cancelled` - Publicado ao cancelar transação

## 🛠️ Tecnologias

- **.NET 9 / C# 13**
- **.NET Aspire** - Orquestração de microserviços
- **MediatR** - Implementação CQRS
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **RabbitMQ** - Mensageria
- **Redis** - Cache
- **OutputCache** - Cache de respostas HTTP
- **Minimal APIs** - Endpoints
- **Service Discovery** - Descoberta automática de serviços

## 📂 Estrutura do Projeto

```
├── src/
│   ├── BankingSystem.AppHost/           # Orquestrador Aspire
│   ├── BankingSystem.ServiceDefaults/   # Configurações padrão Aspire
│   ├── BankingSystem.Account.Api/       # API de Contas
│   ├── BankingSystem.Transaction.Api/   # API de Transações
│   ├── BankingSystem.Processor/         # Worker Service
│   └── BankingSystem.Shared/            # Classes compartilhadas
└── BankingSystem.sln                    # Solution
```

## 🔄 Fluxo de Exemplo

1. **Criar uma conta**
   ```json
   POST /api/accounts
   {
     "number": "12345",
     "holderName": "João Silva"
   }
   ```

2. **Criar uma transação**
   ```json
   POST /api/transactions
   {
     "accountNumber": "12345",
     "type": 1,
     "description": "Depósito inicial",
     "amount": 1000.00
   }
   ```

3. O **Processor** consome o evento e atualiza o saldo da conta automaticamente

4. **Consultar saldo atualizado**
   ```
   GET /api/accounts/12345/balance
   ```

## ⚙️ Configurações

Com Aspire, as configurações são gerenciadas automaticamente:
- **Service Discovery** - URLs descobertas automaticamente
- **Connection Strings** - Configuradas pelo AppHost
- **Recursos** - PostgreSQL, Redis e RabbitMQ provisionados automaticamente
- **Monitoramento** - Logs, traces e métricas centralizados

Para configurações customizadas, verifique:
- `appsettings.json` em cada projeto
- `Program.cs` do AppHost para configuração de recursos