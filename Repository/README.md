# Repository Layer - Refatoração Clean Architecture

## 📋 Visão Geral

A camada Repository foi refatorada seguindo princípios de Clean Architecture com foco em:

- ✅ **Separação de Responsabilidades**: DbContext, Unit of Work, Base Repository
- ✅ **Options Pattern**: Configurações estruturadas para EF Core
- ✅ **Logging Estruturado**: Em todas as operações
- ✅ **Documentação XML**: Comentários completos para cada classe/método
- ✅ **Validação Robusta**: Verificação de input em repositórios
- ✅ **Base Repository Class**: Reutilização de código comum
- ✅ **Query Extensions**: Métodos auxiliares para queries
- ✅ **Testabilidade**: Design facilitando testes unitários

## 📁 Estrutura de Diretórios

```
Repository/
├── Base/                      # Classes base para repositórios
│   └── BaseRepository.cs       # Implementação genérica
├── Constants/                 # Constantes da camada (NOVO)
│   └── RepositoryConstants.cs
├── Extensions/                # Extensões para queries (NOVO)
│   └── QueryExtensions.cs
├── Options/                   # Configurações estruturadas (NOVO)
│   └── EntityFrameworkOptions.cs
├── Repositories/              # Implementações de repositórios
│   ├── Billing/
│   ├── Catalog/
│   ├── Commerce/
│   ├── Identity/
│   └── Tenancy/
├── SettingsEF/                # Configurações de entidades
│   └── *.Config.cs
├── AppDbContext.cs            # Contexto principal (refatorado)
├── AppDbContextFactory.cs     # Factory para design-time (refatorado)
├── RepositoryDependencyInjection.cs  # Registro DI (refatorado)
├── UnitOfWork.cs              # Unit of Work (refatorado)
└── README.md
```

## 🎯 Componentes Principais

### 1. **Constants** (RepositoryConstants.cs)

Centraliza constantes por categoria:

- `EntityFramework`: Assembly, provider, defaults de precisão
- `Queries`: Profundidade de includes, rastreamento
- `SoftDelete`: Propriedade deletedAt
- `Audit`: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
- `MultiTenancy`: TenantIdPropertyName

### 2. **Options** (EntityFrameworkOptions.cs)

Configuração type-safe para EF Core:

```json
{
  "EntityFramework": {
    "ConnectionString": "Host=localhost;Database=ecommerce;Username=postgres;Password=****",
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "UseQueryTracking": true,
    "DefaultIncludeDepth": 1
  }
}
```

### 3. **Base Repository** (BaseRepository.cs)

Classe genérica com funcionalidade comum:

- `AddAsync<T>` - Adiciona entidade
- `GetByIdAsync<T>` - Recupera por ID
- `Query<T>` - Retorna IQueryable base
- `DeleteAsync<T>` - Marca como deletada
- `ExistsAsync<T>` - Verifica existência
- `CountAsync<T>` - Conta registros

### 4. **Query Extensions** (QueryExtensions.cs)

Métodos auxiliares para queries:

- `ByTenant()` - Filtra por tenant
- `OnlyActive()` - Exclui deletados
- `Paginate()` - Paginação
- `AsNoTrackingQuery()` - Desativa rastreamento

### 5. **AppDbContext** (refatorado)

Melhorias:

- ✅ Documentação XML para cada DbSet
- ✅ Logging estruturado em SaveChangesAsync
- ✅ Tratamento de erro em domain event dispatching
- ✅ Métodos privados para organização
- ✅ ILogger injetado via construtor

**Exemplo de Logging**:

```csharp
_logger?.LogInformation(
    "Mudanças salvas com sucesso. {ChangeCount} registros afetados",
    result);
```

### 6. **AppDbContextFactory** (refatorado)

Melhorias:

- ✅ Documentação detalhada
- ✅ Métodos privados para cada etapa (ResolvePath, BuildConfiguration, etc.)
- ✅ Uso de constantes
- ✅ Melhor tratamento de erros
- ✅ Instruções de uso do EF Core tooling

### 7. **UnitOfWork** (refatorado)

Melhorias:

- ✅ Logging em commit e rollback
- ✅ Tratamento específico de DbUpdateException
- ✅ Documentação clara
- ✅ Logger opcional via injeção

### 8. **RepositoryDependencyInjection** (refatorado)

Melhorias:

- ✅ Métodos privados por categoria (Cache, Payment, Tenancy, Orders)
- ✅ Validação de configuração no startup
- ✅ Validação de null nos parâmetros
- ✅ Comentários explicativos
- ✅ Grouping lógico de registros

**Exemplo**:

```csharp
private static void RegisterEntityFrameworkOptions(
    IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<EntityFrameworkOptions>(
        configuration.GetSection(EntityFrameworkOptions.SectionName));

    var efOptions = configuration
        .GetSection(EntityFrameworkOptions.SectionName)
        .Get<EntityFrameworkOptions>();

    if (efOptions != null)
    {
        efOptions.Validate();
    }
}
```

### 9. **Repositórios** (refatorados)

Todos os repositórios agora têm:

- ✅ Documentação XML completa
- ✅ Validação de argumentos
- ✅ Logging em operações importantes
- ✅ Injeção de logger opcional
- ✅ Nomes de variáveis descritivos

**Exemplo - CategoryRepository**:

```csharp
/// <summary>
/// Adiciona uma nova categoria ao banco de dados.
/// </summary>
public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(category);
    await _context.Categories.AddAsync(category, cancellationToken);
    _logger?.LogDebug("Categoria adicionada: {CategoryId} - {Name}", category.Id, category.Name);
}
```

## 🔧 Configuração (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ecommerce;Username=postgres;Password=****"
  },
  "EntityFramework": {
    "ConnectionString": "Host=localhost;Database=ecommerce;Username=postgres;Password=****",
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "UseQueryTracking": true,
    "DefaultIncludeDepth": 1
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Repository": "Debug"
    }
  }
}
```

## 📊 Dependências

```
Repository
├── Domain (interfaces)
├── Microsoft.EntityFrameworkCore
├── Npgsql.EntityFrameworkCore.PostgreSQL
├── Microsoft.Extensions.*
└── MediatR (opcional, para domain events)
```

## 🧪 Testabilidade

Base Repository permite testes facilmente:

```csharp
// Mock do DbContext
var mockContext = new Mock<AppDbContext>();
var mockDbSet = new Mock<DbSet<Category>>();

// Injetar em repositório
var repository = new CategoryRepository(mockContext.Object);

// Testar
await repository.AddAsync(new Category { /* ... */ });
```

## 📝 Boas Práticas Aplicadas

### 1. **Principios SOLID**

- **S**ingle Responsibility: Cada repositório tem uma responsabilidade
- **O**pen/Closed: Aberto para extensão via BaseRepository
- **L**iskov Substitution: Implementações intercambiáveis
- **I**nterface Segregation: Interfaces focadas
- **D**ependency Inversion: Depende de abstrações

### 2. **Clean Code**

- Nomes descritivos
- Métodos pequenos e focados
- Sem magic strings (constantes)
- Documentação XML completa

### 3. **Logging Estruturado**

- `LogDebug`: Operações normais
- `LogInformation`: Eventos importantes (cliente criado, pedido adicionado)
- `LogWarning`: Situações não-ideais
- `LogError`: Erros reais

### 4. **Configuração**

- Options Pattern ao invés de magic strings
- Validação no startup
- Valores padrão sensatos

### 5. **Query Extensions**

- Reutilização de padrões comuns
- Type-safe
- Fluent API

## ⚠️ Breaking Changes

Se você atualizou de uma versão anterior:

1. **Repositórios agora recebem Logger**:

   ```csharp
   // Antes
   new CategoryRepository(dbContext)

   // Agora (automático via DI)
   services.AddScoped<ICategoryRepository, CategoryRepository>();
   ```

2. **AppDbContext agora recebe ILogger**:

   ```csharp
   // Automático via DI
   new AppDbContext(options, tenantContext, mediator, logger)
   ```

3. **UnitOfWork agora recebe ILogger**:
   ```csharp
   // Automático via DI
   services.AddScoped<IUnitOfWork, UnitOfWork>();
   ```

## 🔍 Exemplo de Uso

```csharp
// Injetar repositório
public class CreateProductHandler
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        // Criar produto
        var product = Product.Create(cmd.Name, cmd.Description);

        // Adicionar via repositório (com logging automático)
        await _repository.AddAsync(product, ct);

        // Confirmar transação
        await _unitOfWork.CommitAsync(ct);
    }
}
```

## 🔌 Query Extensions em Ação

```csharp
// Sem extensões
var query = _context.Products
    .Where(p => p.TenantId == tenantId)
    .Where(p => p.DeletedAt == null)
    .Skip((page - 1) * pageSize)
    .Take(pageSize);

// Com extensões (mais legível)
var query = _context.Products
    .ByTenant(tenantId)
    .OnlyActive()
    .Paginate(page, pageSize);
```

## 🚀 Próximos Passos (Sugestões)

1. Adicionar Unit Tests para cada repositório
2. Implementar Specification Pattern para queries complexas
3. Adicionar Repository pattern para agregações
4. Implementar IAsyncDisposable em AppDbContext
5. Adicionar Health Checks para banco de dados
6. Implementar Bulk operations para performance
7. Adicionar caching de segundo nível
8. Implementar Soft Delete policy centralizada

---

**Refatoração completa em 2025-06-04** ✨
