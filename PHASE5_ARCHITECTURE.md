# Arquitetura da Fase 5 — Diagrama

## 🏗️ Fluxo de Dados

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENTE (HTTP)                               │
└─────────────────────────────┬───────────────────────────────────────┘
                              │
                              ▼
         ┌────────────────────────────────────────────────┐
         │          PlansController / SubscriptionsController      │
         │  • GET  /api/v1/plans                                  │
         │  • GET  /api/v1/plans/{id}                             │
         │  • POST /api/v1/subscriptions                          │
         │  • GET  /api/v1/subscriptions/current                  │
         └────────────────────────────────────────────────┘
                              │
                              ▼
         ┌────────────────────────────────────────────────┐
         │              MediatR (Pipeline)                │
         │  1. ValidationBehavior (FluentValidation)      │
         │  2. LoggingBehavior                           │
         │  3. PerformanceBehavior                       │
         └────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
         ▼                    ▼                    ▼
    ┌─────────┐         ┌────────┐          ┌──────────┐
    │ GetPlans │     │ GetPlans │   │ CreateSubscription │
    │ Handler  │     │ByIdHandler │  Handler       │
    └────┬────┘     └────┬────┘  └────┬─────┘
         │               │            │
         └───────┬───────┴────────────┘
                 │
                 ▼
   ┌──────────────────────────────────────┐
   │    Repositórios (Domain Interface)   │
   │  • IPlanRepository                   │
   │  • ISubscriptionRepository           │
   │  • IUnitOfWork                       │
   └──────────┬───────────────────────────┘
              │
              ▼
   ┌──────────────────────────────────────┐
   │  Implementações (Repository Layer)   │
   │  • PlanRepository                    │
   │  • SubscriptionRepository            │
   │  • UnitOfWork                        │
   └──────────┬───────────────────────────┘
              │
              ▼
   ┌──────────────────────────────────────┐
   │      Entity Framework Core 8         │
   │  • Include/ThenInclude otimizados    │
   │  • Query filtering (soft delete)     │
   │  • Multi-tenancy filtering           │
   └──────────┬───────────────────────────┘
              │
              ▼
   ┌──────────────────────────────────────┐
   │    PostgreSQL 16 (via Npgsql)        │
   │  • Table: Plans                      │
   │  • Table: Subscriptions              │
   │  • Table: PlanFeatures               │
   │  • Table: Features                   │
   └──────────────────────────────────────┘
```

---

## 📦 Estrutura de Entidades

```
┌──────────────────────┐
│       Plan           │
├──────────────────────┤
│ • Id (Guid)          │
│ • Name               │
│ • Description        │
│ • Price              │
│ • BillingCycle       │
│ • IsActive           │
│ • CreatedAt          │
│ • UpdatedAt          │
└────────┬─────────────┘
         │
         │ 1:N
         │
         ▼
┌──────────────────────┐
│    PlanFeature       │
├──────────────────────┤
│ • PlanId (FK)        │
│ • FeatureId (FK)     │
│ • LimitValue         │
└────────┬─────────────┘
         │
         │ N:1
         │
         ▼
┌──────────────────────┐
│      Feature         │
├──────────────────────┤
│ • Id (Guid)          │
│ • Key                │
│ • Name               │
│ • Description        │
└──────────────────────┘


┌──────────────────────┐
│    Subscription      │
├──────────────────────┤
│ • Id (Guid)          │
│ • TenantId (FK)      │
│ • PlanId (FK)        │
│ • Status (enum)      │
│ • StartDate          │
│ • EndDate            │
│ • TrialEndDate       │
│ • CancelledAt        │
│ • StripeSubId        │
│ • CreatedAt          │
│ • UpdatedAt          │
└──────────────────────┘
         │
         │ N:1
         │
         ▼
┌──────────────────────┐
│      Plan            │
└──────────────────────┘
```

---

## 🔄 Fluxo de Casos de Uso

### UC1: Listar Planos Disponíveis

```
User
  │
  ├─→ GET /api/v1/plans
  │
  └─→ PlansController.GetAll()
       │
       ├─→ Send(GetPlansQuery)
       │
       ├─→ GetPlansQueryHandler
       │
       ├─→ IPlanRepository.GetActiveAsync()
       │
       ├─→ [DB Query: Plans WHERE IsActive = true]
       │
       ├─→ Map to DTOs (with Features)
       │
       └─→ 200 OK [Plans[]]
```

### UC2: Criar Subscription com Trial

```
User (with X-Tenant-Id header)
  │
  ├─→ POST /api/v1/subscriptions
  │   {
  │     "planId": "...",
  │     "trialDays": 14
  │   }
  │
  ├─→ SubscriptionsController.Create()
  │
  ├─→ ValidationBehavior
  │   ├─→ Check: PlanId != empty
  │   ├─→ Check: TrialDays 0-90
  │   └─→ ✓ Valid
  │
  ├─→ CreateSubscriptionCommandHandler
  │   │
  │   ├─→ Get TenantId from HttpTenantContext
  │   │
  │   ├─→ IPlanRepository.GetByIdAsync(planId)
  │   │   └─→ ✓ Plan exists and IsActive
  │   │
  │   ├─→ ISubscriptionRepository.GetActiveByTenantAsync(tenantId)
  │   │   └─→ ✓ No active subscription for tenant
  │   │
  │   ├─→ Subscription.Create(tenantId, plan, startDate, endDate, trialEndDate)
  │   │   ├─→ Create domain entity
  │   │   ├─→ Set Status = Trialing (because trialEndDate is set)
  │   │   └─→ Add SubscriptionCreatedEvent
  │   │
  │   ├─→ ISubscriptionRepository.AddAsync(subscription)
  │   │   └─→ [Add to DbContext]
  │   │
  │   ├─→ IUnitOfWork.CommitAsync()
  │   │   └─→ [SaveChanges()]
  │   │
  │   └─→ Return CreateSubscriptionResult
  │
  └─→ 201 Created {subscriptionId, planId, status, ...}
```

### UC3: Obter Subscription Ativa

```
User (with X-Tenant-Id header)
  │
  ├─→ GET /api/v1/subscriptions/current
  │
  ├─→ SubscriptionsController.GetCurrent()
  │
  ├─→ Send(GetSubscriptionByTenantQuery)
  │
  ├─→ GetSubscriptionByTenantQueryHandler
  │   │
  │   ├─→ Get TenantId from HttpTenantContext
  │   │
  │   ├─→ ISubscriptionRepository.GetActiveByTenantAsync(tenantId)
  │   │   ├─→ Include(Plan)
  │   │   └─→ [DB Query with eager loading]
  │   │
  │   ├─→ If null → return null
  │   │
  │   └─→ Map to GetSubscriptionByTenantResult (with Plan data)
  │
  └─→ 200 OK {subscriptionId, planName, status, isActive, ...}
      OR
      204 No Content (if no subscription)
```

---

## ✔️ Validações em Camadas

```
┌─────────────────────────────────────────────────┐
│         1. Validator (FluentValidation)         │
│                                                 │
│  • PlanId != empty                              │
│  • TrialDays >= 0 AND <= 90                     │
└─────────────────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────┐
│      2. Handler (Business Logic)                │
│                                                 │
│  • Plan existe? (NotFoundException)             │
│  • Plan está ativo? (ValidationException)       │
│  • Tenant já tem subscription? (ConflictEx)     │
└─────────────────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────┐
│         3. Domain (Invariants)                  │
│                                                 │
│  • EndDate > StartDate                          │
│  • TrialEndDate <= EndDate (se houver)          │
│  • Subscription state is consistent             │
└─────────────────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────┐
│         4. Database (Constraints)               │
│                                                 │
│  • Foreign key integrity                        │
│  • Unique constraints                           │
│  • Not null constraints                         │
└─────────────────────────────────────────────────┘
```

---

## 🎯 Decisões de Design

### 1️⃣ Status Trialing vs Active

```
Subscription.Create(tenantId, plan, startDate, endDate, trialEndDate)

if (trialEndDate.HasValue)
    status = SubscriptionStatus.Trialing
else
    status = SubscriptionStatus.Active
```

**Por quê?** Permite diferenciação entre trial e pagos nos relatórios.

---

### 2️⃣ Cálculo de EndDate baseado em BillingCycle

```
var endDate = plan.BillingCycle switch
{
    BillingCycle.Monthly    => now.AddMonths(1),
    BillingCycle.Quarterly  => now.AddMonths(3),
    BillingCycle.Annually   => now.AddYears(1),
    _                       => now.AddMonths(1)
};
```

**Por quê?** Elimina duplicação e garante consistência com preço.

---

### 3️⃣ GetActiveByTenantAsync inclui Plan + Features

```
.Include(s => s.Plan)
    .ThenInclude(p => p.PlanFeatures)
    .ThenInclude(pf => pf.Feature)
```

**Por quê?** Evita N+1 queries ao retornar dados do plano.

---

### 4️⃣ PlansController sem validação de X-Tenant-Id

```
[AllowAnonymous] // No X-Tenant-Id required
```

**Por quê?** Planos são públicos, qualquer user pode listar.

---

## 📊 Dependências de Projeto

```
Application/
├─→ Domain (referência)
├─→ MediatR
├─→ FluentValidation
└─→ AutoMapper

Api/
├─→ Application
└─→ MediatR

Repository/
├─→ Domain
├─→ EntityFrameworkCore
└─→ Npgsql

Domain/
└─→ Nenhuma dependência externa (puro)
```

---

## 🚦 Status de Implementação

```
✅ GetPlansQuery              [100%]
✅ GetPlanByIdQuery           [100%]
✅ CreateSubscriptionCommand  [100%]
✅ GetSubscriptionByTenantQuery [100%]
✅ PlanRepository             [100%]
✅ IPlanRepository            [100%]
✅ PlansController            [100%]
✅ SubscriptionsController    [100%]
✅ Dependency Injection       [100%]

⏳ CancelSubscriptionCommand  [0%]
⏳ Feature Flag Middleware    [0%]
⏳ Stripe Webhook Integration [0%]
⏳ Tests & Integration        [0%]
```

---

## 📈 Próximas Integrações

```
Fase 5 (Billing) → Stripe Subscriptions
      ↓
      ├─→ Webhook: customer.subscription.deleted
      ├─→ Webhook: invoice.payment_failed
      └─→ Sync subscriptions status

                        ↓

Fase 6 (Qualidade) → Feature Flags Middleware
      ├─→ Valida features por plano
      ├─→ Cache da subscription em Redis
      └─→ Retorna 402 Payment Required

                        ↓

Fase 7 (Portfólio) → Documentação
      └─→ README com SaaS patterns
```
