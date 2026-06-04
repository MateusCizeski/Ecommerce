# Infrastructure Layer - Refatoração Clean Architecture

## 📋 Visão Geral

A camada Infrastructure foi refatorada seguindo princípios de Clean Architecture com foco em:

- ✅ **Separação de Responsabilidades**: Cada camada tem uma função bem definida
- ✅ **Dependency Inversion**: Dependências injetadas, não criadas
- ✅ **Options Pattern**: Configurações estruturadas e validadas
- ✅ **Logging Apropriado**: Rastreamento detalhado de operações
- ✅ **Documentação XML**: Comentários para IntelliSense e documentação
- ✅ **Tratamento de Erros**: Validação e logging de exceções
- ✅ **Testabilidade**: Abstrações para facilitar testes

## 📁 Estrutura de Diretórios

```
Infrastructure/
├── Abstractions/              # Interfaces internas abstraindo SDKs externos
│   └── IStripePaymentService.cs
├── Caching/                   # Serviços de Cache
│   └── RedisCacheService.cs
├── Constants/                 # Constantes da camada (NOVO)
│   └── InfrastructureConstants.cs
├── DependencyInjection/       # Registro de serviços no DI
│   └── InfrastructureDependencyInjection.cs
├── MultiTenancy/              # Resolução de Tenant
│   └── HttpTenantContext.cs
├── Options/                   # Configurações estruturadas (NOVO)
│   ├── RedisOptions.cs
│   ├── StripeOptions.cs
│   └── OrderGenerationOptions.cs
├── Orders/                    # Geração de números de ordem
│   └── OrderNumberGenerator.cs
├── Payments/                  # Gateway de pagamentos
│   ├── StripePaymentGateway.cs
│   └── StripePaymentService.cs (novo - serviço interno)
├── Infrastructure.csproj
└── README.md
```

## 🎯 Componentes Principais

### 1. **Constants** (InfrastructureConstants.cs)

Centraliza todas as constantes da camada, organizadas por domínio:

- `MultiTenancy`: Chaves de contexto HTTP, headers
- `Cache`: Valores padrão de TTL
- `Orders`: Prefixos, comprimentos de valores
- `Payments`: Conversão de valores, status

**Benefício**: Mudanças em constantes em um único lugar, evita magic strings.

### 2. **Options** (Configuration Pattern)

Classes fortemente tipadas para cada serviço:

#### RedisOptions

```csharp
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "DefaultTtlMinutes": 15
  }
}
```

#### StripeOptions

```csharp
{
  "Stripe": {
    "SecretKey": "sk_live_...",
    "PublishableKey": "pk_live_...",
    "WebhookSigningSecret": "whsec_..."
  }
}
```

#### OrderGenerationOptions

```csharp
{
  "OrderGeneration": {
    "Prefix": "ORD",
    "TenantIdPrefixLength": 4,
    "UseUnixTimestamp": true
  }
}
```

**Benefício**: Type-safe configuration, validação automática, intellisense.

### 3. **Abstractions**

Abstrair SDKs externos protege a lógica de negócio de mudanças externas.

#### IStripePaymentService

- Interface interna encapsulando Stripe SDK
- Implementada por StripePaymentService (internal)
- Oferece método limpo e testável
- Logging estruturado

**Benefício**: Fácil swap de Stripe para outro provider sem mudanças na Domain/Application.

### 4. **Serviços Refatorados**

#### RedisCacheService

**Melhorias**:

- ✅ Validação de entrada (null, empty keys)
- ✅ Logging detalhado (DEBUG para operações normais, WARNING para falhas)
- ✅ Tratamento separado de JsonException
- ✅ XML documentation
- ✅ Injeção de IOptions para configuração
- ✅ Código legível com quebras de linha

#### HttpTenantContext

**Melhorias**:

- ✅ Constantes ao invés de magic strings
- ✅ Métodos privados para resolução (TryResolveFrom\*)
- ✅ Logging de sucesso e falha
- ✅ Documentação XML
- ✅ Validação explícita de ArgumentNull

#### OrderNumberGenerator

**Melhorias**:

- ✅ IOptions para configuração (prefixo, formato)
- ✅ Lock para thread-safety do Random
- ✅ Validação de TenantId
- ✅ Logging de erro e sucesso
- ✅ Métodos privados reutilizáveis
- ✅ Documentação completa

#### StripePaymentGateway

**Melhorias**:

- ✅ Injeção do serviço abstrato (não instancia Services)
- ✅ Métodos privados para conversão e validação
- ✅ Logging apropriado (INFO para sucesso, ERROR para falhas)
- ✅ Validação de argumentos
- ✅ Adaptação para Application.Interfaces
- ✅ Uso de constantes para status

#### StripePaymentService (NOVO)

- Camada interna encapsulando Stripe SDK
- Injeta cada Service do Stripe (não instancia a cada chamada)
- Tratamento separado de StripeException vs Exception
- Logging estruturado

## 🔧 Configuração (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=Ecommerce;Trusted_Connection=true;",
    "Redis": "localhost:6379"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "DefaultTtlMinutes": 15
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSigningSecret": null
  },
  "OrderGeneration": {
    "Prefix": "ORD",
    "TenantIdPrefixLength": 4,
    "UseUnixTimestamp": true
  }
}
```

## 📊 Dependências

```
Infrastructure
├── Domain (interfaces)
├── Application (interfaces, exceptions)
├── Repository
├── StackExchange.Redis
├── Stripe.net
├── Microsoft.AspNetCore.Http.Abstractions
└── Microsoft.Extensions.*
```

## 🧪 Testabilidade

Cada componente é facilmente testável:

```csharp
// Mock de IOptions<RedisOptions>
var mockOptions = new Mock<IOptions<RedisOptions>>();
mockOptions.Setup(x => x.Value).Returns(new RedisOptions { DefaultTtlMinutes = 15 });

// Mock de IConnectionMultiplexer
var mockRedis = new Mock<IConnectionMultiplexer>();

// Injetar mocks
var cache = new RedisCacheService(mockRedis.Object, mockOptions.Object, mockLogger.Object);

// Testar
await cache.GetAsync<string>("key");
```

## 📝 Boas Práticas Aplicadas

### 1. **Principios SOLID**

- **S**ingle Responsibility: Cada classe tem uma responsabilidade
- **O**pen/Closed: Aberto para extensão via interfaces
- **L**iskov Substitution: Implementações intercambiáveis
- **I**nterface Segregation: Interfaces pequenas e focadas
- **D**ependency Inversion: Depende de abstrações

### 2. **Clean Code**

- Nomes descritivos (não usam abreviações como `ct`, `db`, `ex`)
- Métodos pequenos e focados
- Sem magic numbers (constantes extraídas)
- Documentação XML completa

### 3. **Logging Estruturado**

- `LogDebug`: Operações normais
- `LogInformation`: Eventos importantes (cliente criado, pagamento processado)
- `LogWarning`: Situações não-ideais que podem levar a erros
- `LogError`: Erros reais com exceção

### 4. **Configuração**

- Options Pattern ao invés de configuration strings
- Validação de configuração no startup
- Valores padrão sensatos
- Documentação dos parâmetros

### 5. **Validação**

- `ArgumentNullException.ThrowIfNull()` para nulls
- Validação de strings vazias
- Validação de ranges (valores positivos)
- Erros descritivos com nomes de parâmetros

## ⚠️ Breaking Changes

Se você atualizou de uma versão anterior, note:

1. **RedisCacheService** agora requer `IOptions<RedisOptions>`

   ```csharp
   // Antes
   new RedisCacheService(redis, logger)

   // Agora (automático via DI)
   services.Configure<RedisOptions>(configuration.GetSection("Redis"));
   ```

2. **HttpTenantContext** agora requer `ILogger<HttpTenantContext>`

   ```csharp
   // Antes
   new HttpTenantContext(accessor)

   // Agora (automático via DI)
   services.AddScoped<ITenantContext, HttpTenantContext>();
   ```

3. **OrderNumberGenerator** agora requer `IOptions<OrderGenerationOptions>`

   ```csharp
   // Configura em appsettings
   {
     "OrderGeneration": { "Prefix": "ORD" }
   }
   ```

4. **StripePaymentGateway** agora depende de `IStripePaymentService`
   ```csharp
   // Automático via DI - não precisa fazer nada!
   ```

## 🔍 Próximos Passos (Sugestões)

1. **Adicionar Unit Tests** para cada serviço
2. **Implementar Cache Policy Pattern** com CircuitBreaker para Redis
3. **Adicionar Health Checks** para Redis e Stripe
4. **Implementar Retry Policy** com Polly para falhas transientes
5. **Adicionar Rate Limiting** no gateway de pagamentos
6. **Implementar IDisposable** se necessário
7. **Adicionar suporte a Webhooks do Stripe** (validação de assinatura)

---

**Refatoração completa em 2025-06-04** ✨
