# Fase 5 — Billing / SaaS — Progresso

## ✅ O que foi implementado (15% da fase)

### 1. **Query: GetPlansQuery**

- Lista todos os planos ativos disponíveis
- Retorna: ID, Nome, Descrição, Preço, Ciclo de Cobrança, Features com Limits
- Sem validador (query pública)

### 2. **Query: GetPlanByIdQuery**

- Retorna detalhe completo de um plano específico
- Inclui todas as features vinculadas ao plano
- Lança `NotFoundException` se plano não existir

### 3. **Command: CreateSubscriptionCommand**

- Validação fluente (TrialDays entre 0-90)
- Verifica se o plano existe e está ativo
- Valida que tenant não tem subscription ativa
- Calcula datas de término baseado no `BillingCycle` do plano
- Cria entidade de domínio `Subscription` com eventos
- Persiste no banco

### 4. **Query: GetSubscriptionByTenantQuery**

- Retorna a subscription ativa do tenant atual
- Inclui dados do plano associado
- Retorna `null` se não houver subscription ativa

### 5. **Repositórios**

- `IPlanRepository` criada (interface em `Domain.Interfaces.Billing`)
- `PlanRepository` implementado com:
  - `GetByIdAsync()` — com eager loading de features
  - `GetActiveAsync()` — listagem de planos ativos
  - `AddAsync()` — persistência
- `ISubscriptionRepository` já existia, implementação verificada

### 6. **Controllers**

- `PlansController` (sem autenticação de tenant — público):
  - `GET /api/v1/plans` — listar planos
  - `GET /api/v1/plans/{id}` — detalhe do plano
- `SubscriptionsController` (com contexto de tenant):
  - `POST /api/v1/subscriptions` — criar subscription
  - `GET /api/v1/subscriptions/current` — obter subscription atual

### 7. **Injeção de Dependências**

- `IPlanRepository` registrado em `RepositoryDependencyInjection`
- MediatR já auto-registra os novos handlers via assembly reflection

---

## ⏳ Próximos passos (85% da fase)

### Middleware de Feature Flags

- [ ] `SubscriptionFeatureAttribute` para decorar endpoints
- [ ] `PlanFeatureMiddleware` para validação de features
- [ ] Retornar 402 Payment Required se feature não disponível
- [ ] Cache Redis da subscription ativa por tenant

### Stripe Subscriptions Sync

- [ ] `ProcessStripeWebhookCommand` para webhook de subscription
- [ ] Listener para `customer.subscription.deleted`
- [ ] Listener para `invoice.payment_failed` → marcar como `PastDue`
- [ ] Integration tests com Stripe

### Domínio Evoluído

- [ ] `SubscriptionFeatureUsageEvent` para log de uso de features
- [ ] `RenewSubscriptionCommand` para renovação automática

---

## 📝 Notas de Design

1. **Trial vs Active**: O `Subscription` inicia em `Trialing` se `TrialEndDate` for set, senão em `Active`
2. **Ciclo de Cobrança**: O cálculo de `EndDate` respeita o `BillingCycle` do plano (mensal/trimestral/anual)
3. **Isolamento Multi-tenant**: `GetActiveByTenantAsync()` garante que cada tenant vê só sua subscription
4. **Validação em Camadas**:
   - Validators: regras de formato (TrialDays)
   - Handlers: regras de negócio (subscription ativa, plano ativo)
   - Domínio: invariantes críticas (endDate > startDate)

---

## 🧪 Exemplos de Uso

### 1. Listar planos disponíveis

```bash
GET /api/v1/plans
```

**Response:**

```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Pro",
    "description": "Para empresas em crescimento",
    "price": 99.0,
    "billingCycle": "Monthly",
    "features": [
      {
        "featureKey": "max_products",
        "featureName": "Produtos Máximos",
        "limitValue": "1000"
      }
    ]
  }
]
```

### 2. Criar subscription com trial de 14 dias

```bash
POST /api/v1/subscriptions
X-Tenant-Id: {tenant-id}
Content-Type: application/json

{
  "planId": "550e8400-e29b-41d4-a716-446655440000",
  "trialDays": 14
}
```

**Response (201 Created):**

```json
{
  "subscriptionId": "660e8400-e29b-41d4-a716-446655440001",
  "planId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Trialing",
  "startDate": "2026-05-26T10:00:00Z",
  "endDate": "2026-06-26T10:00:00Z",
  "trialEndDate": "2026-06-09T10:00:00Z"
}
```

### 3. Obter subscription atual

```bash
GET /api/v1/subscriptions/current
X-Tenant-Id: {tenant-id}
```

**Response (200):**

```json
{
  "subscriptionId": "660e8400-e29b-41d4-a716-446655440001",
  "planId": "550e8400-e29b-41d4-a716-446655440000",
  "planName": "Pro",
  "planPrice": 99.0,
  "billingCycle": "Monthly",
  "status": "Trialing",
  "startDate": "2026-05-26T10:00:00Z",
  "endDate": "2026-06-26T10:00:00Z",
  "trialEndDate": "2026-06-09T10:00:00Z",
  "isActive": true
}
```

---

## 📂 Estrutura de Arquivos Criada

```
Application/
└── Features/
    └── Billing/
        ├── Plans/
        │   ├── GetPlans.cs
        │   └── GetPlanById.cs
        └── Subscriptions/
            ├── CreateSubscription.cs
            └── GetSubscriptionByTenant.cs

Api/
└── Controllers/
    └── Billing/
        ├── PlansController.cs
        └── SubscriptionsController.cs

Repository/
└── Repositories/
    └── Billing/
        └── PlanRepository.cs

Domain/
└── Interfaces/
    └── Billing/
        └── IPlanRepository.cs
```

---

## 🔄 Fluxo de Negócio

```
User acessa app
    ↓
GET /api/v1/plans (escolhe plano)
    ↓
POST /api/v1/subscriptions (cria subscription com trial)
    ↓
SubscriptionCreatedEvent disparado
    ↓
Middleware valida features do plano em cada request
    ↓
Se feature não disponível → 402 Payment Required (próxima fase)
```
