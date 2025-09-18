# Banking System - Microservices com .NET 9

Sistema de exemplo para demonstração no TDC São Paulo 2025, implementando microserviços bancários com .NET 9, CQRS, RabbitMQ e Redis.

## 🏗️ Arquitetura

O sistema é composto por 3 microserviços:

### 1. **Account API** (porta 5000)
- Gerenciamento de contas bancárias
- Operações: criar, editar, inativar e consultar contas
- Consulta de saldo e transações
- Cache com Redis usando OutputCache
- Integração com Transaction API

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

## 🚀 Como Executar

### Pré-requisitos
- .NET 9 SDK
- Docker e Docker Compose

### 1. Subir a infraestrutura

```bash
docker-compose up -d
```

Isso iniciará:
- PostgreSQL (Account DB) - porta 5432
- PostgreSQL (Transaction DB) - porta 5433
- Redis - porta 6379
- RabbitMQ - porta 5672 (Admin UI: 15672)

### 2. Executar os microserviços

Em terminais separados:

```bash
# Terminal 1 - Account API
cd src/BankingSystem.Account.Api
dotnet run

# Terminal 2 - Transaction API
cd src/BankingSystem.Transaction.Api
dotnet run

# Terminal 3 - Processor
cd src/BankingSystem.Processor
dotnet run
```

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
- **MediatR** - Implementação CQRS
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **RabbitMQ** - Mensageria
- **Redis** - Cache
- **OutputCache** - Cache de respostas HTTP
- **Minimal APIs** - Endpoints

## 📂 Estrutura do Projeto

```
├── src/
│   ├── BankingSystem.Account.Api/       # API de Contas
│   ├── BankingSystem.Transaction.Api/   # API de Transações
│   ├── BankingSystem.Processor/         # Worker Service
│   └── BankingSystem.Shared/            # Classes compartilhadas
├── docker-compose.yml                   # Infraestrutura
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

As configurações estão em `appsettings.Development.json` de cada projeto:
- Connection strings para PostgreSQL
- Configuração do Redis
- Configuração do RabbitMQ
- URLs das APIs