# 🚀 Quick Start — Fase 5 (Billing/SaaS)

## ⚡ 30 segundos de contexto

Você agora tem **10% da Fase 5 implementada**:

- ✅ Listagem de planos (público)
- ✅ Detalhes de plano com features
- ✅ Criar subscription com trial
- ✅ Obter subscription ativa do tenant

**Todo registrado no MediatR, repositórios prontos, controllers feitos.**

---

## 🔗 Endpoints Prontos para Usar

### 1. Listar Planos (sem autenticação)

```bash
curl -X GET "http://localhost:5000/api/v1/plans"
```

### 2. Obter Detalhe de Plano

```bash
curl -X GET "http://localhost:5000/api/v1/plans/{planId}"
```

### 3. Criar Subscription (requer X-Tenant-Id)

```bash
curl -X POST "http://localhost:5000/api/v1/subscriptions" \
  -H "X-Tenant-Id: {tenant-id}" \
  -H "Content-Type: application/json" \
  -d '{
    "planId": "{planId}",
    "trialDays": 14
  }'
```

### 4. Obter Subscription Ativa (requer X-Tenant-Id)

```bash
curl -X GET "http://localhost:5000/api/v1/subscriptions/current" \
  -H "X-Tenant-Id: {tenant-id}"
```

---

## 📂 Estrutura de Arquivos

```
Application/Features/Billing/
├── Plans/
│   ├── GetPlans.cs ......................... Listar planos
│   └── GetPlanById.cs ...................... Detalhe do plano
└── Subscriptions/
    ├── CreateSubscription.cs ............... Criar subscription
    └── GetSubscriptionByTenant.cs ......... Obter subscription ativa

Api/Controllers/Billing/
├── PlansController.cs ..................... Endpoints públicos
└── SubscriptionsController.cs ............ Endpoints com tenant

Repository/Repositories/Billing/
└── PlanRepository.cs ..................... Acesso a dados

Domain/Interfaces/Billing/
└── IPlanRepository.cs ..................... Contrato
```

---

## 🧪 Exemplo Completo de Fluxo

### Passo 1: Listar planos disponíveis

```bash
GET /api/v1/plans

Response:
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Pro",
    "price": 99.00,
    "billingCycle": "Monthly",
    "features": [
      {
        "featureKey": "max_products",
        "limitValue": "1000"
      }
    ]
  }
]
```

### Passo 2: Usuário escolhe um plano e cria subscription

```bash
POST /api/v1/subscriptions
X-Tenant-Id: {tenant-id}

Request:
{
  "planId": "550e8400-e29b-41d4-a716-446655440000",
  "trialDays": 14
}

Response (201 Created):
{
  "subscriptionId": "660e8400-e29b-41d4-a716-446655440001",
  "status": "Trialing",
  "startDate": "2026-05-26T10:00:00Z",
  "trialEndDate": "2026-06-09T10:00:00Z",
  "endDate": "2026-06-26T10:00:00Z"
}
```

### Passo 3: Verificar subscription ativa

```bash
GET /api/v1/subscriptions/current
X-Tenant-Id: {tenant-id}

Response:
{
  "subscriptionId": "660e8400-e29b-41d4-a716-446655440001",
  "planName": "Pro",
  "status": "Trialing",
  "isActive": true
}
```

---

## 🔍 Validações Incluídas

| Validação              | Erro            | Quando               |
| ---------------------- | --------------- | -------------------- |
| TrialDays 0-90         | 400 Bad Request | Se > 90 ou < 0       |
| Plano existe           | 404 Not Found   | Se planId inválido   |
| Plano ativo            | 400 Bad Request | Se plano inativo     |
| Sem subscription ativa | 409 Conflict    | Se tenant já tem uma |

---

## 💾 Dados de Exemplo para Seed (SQL)

```sql
-- Criar um plano
INSERT INTO "Plans" (id, name, description, price, "BillingCycle", "IsActive", "CreatedAt", "UpdatedAt", "TenantId")
VALUES (
  '550e8400-e29b-41d4-a716-446655440000',
  'Pro',
  'Para empresas em crescimento',
  99.00,
  1, -- BillingCycle.Monthly
  true,
  NOW(),
  NOW(),
  NULL -- Planos são globais
);

-- Criar feature
INSERT INTO "Features" (id, "Key", "Name", description, "CreatedAt")
VALUES (
  '550e8400-e29b-41d4-a716-446655440100',
  'max_products',
  'Produtos Máximos',
  NULL,
  NOW()
);

-- Vincular feature ao plano
INSERT INTO "PlanFeatures" (id, "PlanId", "FeatureId", "LimitValue", "CreatedAt")
VALUES (
  '550e8400-e29b-41d4-a716-446655440200',
  '550e8400-e29b-41d4-a716-446655440000',
  '550e8400-e29b-41d4-a716-446655440100',
  '1000',
  NOW()
);
```

---

## 🎯 Próximas Tarefas

- [ ] Seed de planos no database (3 planos: Free, Pro, Enterprise)
- [ ] CancelSubscriptionCommand — para cancelar subscriptions
- [ ] Feature Flag Middleware — bloquear endpoints sem plano adequado
- [ ] Stripe webhook para subscription.deleted
- [ ] Unit tests para handlers
- [ ] Integration tests com banco

---

## 🛠️ Troubleshooting

### "404 Not Found" ao criar subscription

✓ Verifique se o `planId` existe no banco  
✓ Verifique se o plano tem `IsActive = true`

### "409 Conflict" ao criar subscription

✓ Tenant já tem uma subscription ativa  
✓ Cancele a anterior via `CancelSubscriptionCommand` (próxima fase)

### "X-Tenant-Id header missing"

✓ Adicione header `X-Tenant-Id: {tenant-id}` no request  
✓ Endpoints de planos NÃO requerem este header

### MediatR não encontra handlers

✓ Certifique-se de que os arquivos estão em `Application/Features/Billing/`  
✓ Controllers estão em `Api/Controllers/Billing/`  
✓ Compile o projeto: `dotnet build`

---

## 📚 Documentação Complementar

- **`PHASE5_ARCHITECTURE.md`** — Diagramas e design
- **`FASE5_PROGRESS.md`** — Detalhes técnicos
- **`project-phases.html`** — Roadmap visual (abrir em navegador)

---

## 💡 Padrões Seguidos

✅ **MediatR** para CQRS  
✅ **FluentValidation** para validações  
✅ **Entity Framework** com eager loading  
✅ **Domain-Driven Design** com eventos  
✅ **Multi-tenancy** garantida em camadas  
✅ **ProblemDetails** para erros (RFC 7807)  
✅ **Dependency Injection** automática

---

## ✨ Não é 100% porque...

Faltam 85% para Fase 5 completa:

- [ ] **Cancelamento** — `CancelSubscriptionCommand`
- [ ] **Feature Flags** — Middleware para validar features
- [ ] **Stripe Sync** — Webhooks e renovação automática
- [ ] **Cache Redis** — Subscription ativa por tenant
- [ ] **Notificações** — Webhooks disparados ao criar/cancelar
- [ ] **Testes** — Unit + Integration tests completos

---

## 🎓 O que aprender daqui

- Padrão CQRS com MediatR
- Validações em camadas
- Multi-tenancy no handler
- DTOs vs Entities
- Eager loading com EF Core
- Domain events

---

**Tudo pronto para evoluir para a próxima etapa! 🚀**
