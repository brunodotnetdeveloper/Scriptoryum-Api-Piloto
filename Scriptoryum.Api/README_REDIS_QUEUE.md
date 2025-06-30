# Redis Queue Implementation - Scriptoryum API

## Visão Geral

Esta implementação adiciona suporte a filas Redis no Scriptoryum API para processamento assíncrono de documentos. Quando um documento é enviado, ele é salvo no CloudFlare R2 e no banco de dados, e então é adicionado a uma fila Redis para processamento posterior.

## Arquitetura

### Fluxo de Upload de Documentos

1. **Upload** → CloudFlare R2
2. **Salvar** → Banco de dados PostgreSQL
3. **Enfileirar** → Redis Queue
4. **Processar** → Worker assíncrono

### Componentes Implementados

#### 1. RedisQueueService
- **Localização**: `Infrastructure/Services/IRedisQueueService.cs`
- **Função**: Gerencia operações de fila Redis (enqueue/dequeue)
- **Métodos**:
  - `EnqueueDocumentAsync<T>()`: Adiciona item à fila
  - `DequeueDocumentAsync<T>()`: Remove item da fila
  - `GetQueueLengthAsync()`: Obtém tamanho da fila

#### 2. DocumentQueueDto
- **Localização**: `Application/Dtos/DocumentQueueDto.cs`
- **Função**: DTO para dados do documento na fila
- **Propriedades**: ID, nomes de arquivo, tipo, usuário, timestamps

#### 3. DocumentProcessorService
- **Localização**: `Infrastructure/Services/DocumentProcessorService.cs`
- **Função**: Processa documentos da fila Redis
- **Características**:
  - Execução contínua em background
  - Atualização de status no banco
  - Tratamento de erros robusto

## Configuração

### 1. Pacotes NuGet

```xml
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
```

### 2. Configuração no appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=scriptoryum;Username=postgres;Password=postgres;Port=5432",
    "Redis": "localhost:6379,password=P@ssw0rd@2026"
  }
}
```

**Note:** If your Redis instance requires authentication, include the password in the connection string as shown above. For Redis without authentication, use: `"Redis": "localhost:6379"`

### 3. Configuração no Program.cs

```csharp
// Configure Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

// Register Redis Queue Service
builder.Services.AddScoped<IRedisQueueService, RedisQueueService>();

// Register Document Processor Service
builder.Services.AddScoped<IDocumentProcessorService, DocumentProcessorService>();
```

## Instalação e Execução do Redis

### Opção 1: Docker (Recomendado)

```bash
# Redis sem senha
docker run -d --name redis-scriptoryum -p 6379:6379 redis:7-alpine

# Redis com autenticação por senha
docker run -d --name redis-scriptoryum -p 6379:6379 redis:7-alpine redis-server --requirepass "P@ssw0rd@2026"

# Verificar se está rodando
docker ps

# Acessar CLI do Redis (opcional)
docker exec -it redis-scriptoryum redis-cli
```

### Opção 2: Instalação Local (Windows)

1. Baixar Redis para Windows: https://github.com/microsoftarchive/redis/releases
2. Extrair e executar `redis-server.exe`
3. Verificar conexão: `redis-cli ping`

### Configurando Autenticação Redis

Se você precisar definir uma senha para uma instância Redis existente:
```bash
# Conectar ao Redis CLI
redis-cli

# Definir senha
CONFIG SET requirepass "P@ssw0rd@2026"

# Salvar configuração
CONFIG REWRITE
```

## Como Usar

### 1. Upload de Documento

O upload funciona normalmente através do endpoint existente. O documento será automaticamente adicionado à fila Redis após ser salvo.

```http
POST /api/documents/upload
Content-Type: multipart/form-data

file: [arquivo]
description: "Descrição do documento"
```

### 2. Processamento da Fila

Para processar documentos da fila, você pode:

#### Opção A: Executar Manualmente (Para Testes)

```csharp
// Em um controller ou serviço
public async Task ProcessQueue()
{
    var processor = serviceProvider.GetRequiredService<IDocumentProcessorService>();
    await processor.ProcessDocumentsAsync();
}
```

#### Opção B: Background Service (Produção)

Crie um Background Service para execução contínua:

```csharp
public class DocumentProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentProcessorBackgroundService> _logger;

    public DocumentProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DocumentProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IDocumentProcessorService>();
            
            try
            {
                await processor.ProcessDocumentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento de documentos");
                await Task.Delay(30000, stoppingToken); // Aguarda 30s em caso de erro
            }
        }
    }
}

// Registrar no Program.cs
builder.Services.AddHostedService<DocumentProcessorBackgroundService>();
```

## Monitoramento

### 1. Health Checks
A aplicação inclui health checks para monitorar o status do Redis:

```bash
# Verificar saúde geral da aplicação
curl http://localhost:5000/health

# Verificar especificamente o Redis
curl http://localhost:5000/health/redis
```

Respostas possíveis:
- `Healthy`: Redis conectado e funcionando
- `Unhealthy`: Redis indisponível ou com problemas de conexão

### 2. Verificar Fila Redis

```bash
# Conectar ao Redis CLI
redis-cli

# Verificar tamanho da fila
LLEN document-processing-queue

# Ver itens da fila (sem remover)
LRANGE document-processing-queue 0 -1

# Limpar fila (se necessário)
DEL document-processing-queue
```

### 3. Logs da Aplicação

Os logs incluem informações sobre:
- Documentos adicionados à fila
- Documentos processados
- Erros de processamento
- Status de conexão Redis
- Status de reconexão automática

### 3. Status dos Documentos

Os documentos passam pelos seguintes status:
- `Uploaded`: Documento enviado e salvo
- `Processing`: Em processamento
- `Completed`: Processamento concluído
- `Error`: Erro durante processamento

## Tratamento de Erros e Resiliência

### Conexão Redis Resiliente
A aplicação implementa conexão resiliente ao Redis com:

- **AbortOnConnectFail = false**: Permite que a aplicação continue funcionando mesmo se o Redis estiver indisponível
- **Retry automático**: 3 tentativas de reconexão com timeout de 5 segundos
- **Graceful degradation**: Se o Redis não estiver disponível, as operações de fila são ignoradas sem quebrar a aplicação
- **Logs detalhados**: Eventos de conexão, falha e restauração são registrados

### Cenários de Erro
- **Falha no upload**: Documento não é adicionado à fila
- **Redis indisponível**: Operações de fila são ignoradas, aplicação continua funcionando
- **Falha na fila**: Log de erro, documento permanece no banco
- **Falha no processamento**: Status do documento fica como "Error"
- **Reconexão automática**: Redis tenta reconectar automaticamente quando disponível

### Monitoramento de Falhas
- Health checks em `/health` e `/health/redis`
- Logs estruturados com níveis apropriados (Warning, Error, Information)
- Eventos de conexão e reconexão registrados

## Personalização

### 1. Nome da Fila

Para alterar o nome da fila, modifique a constante:

```csharp
// Em DocumentsService.cs e DocumentProcessorService.cs
const string QueueName = "document-processing-queue";
```

### 2. Lógica de Processamento

Modifique o método `SimulateDocumentProcessing` em `DocumentProcessorService.cs` para implementar sua lógica específica:

```csharp
private async Task ProcessDocument(Document document)
{
    // Sua lógica de processamento aqui
    // Exemplos:
    // - Extração de texto
    // - Análise de conteúdo
    // - Geração de thumbnails
    // - Indexação para busca
}
```

### 3. Configurações Avançadas do Redis

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var options = ConfigurationOptions.Parse(connectionString);
    options.ReconnectRetryPolicy = new ExponentialRetry(5000);
    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 10000;
    return ConnectionMultiplexer.Connect(options);
});
```

## Próximos Passos

1. **Implementar Background Service** para processamento contínuo
2. **Adicionar métricas** de performance da fila
3. **Implementar retry policy** para documentos com falha
4. **Adicionar dashboard** para monitoramento
5. **Configurar clustering Redis** para alta disponibilidade

## Troubleshooting

### Redis não conecta
- Verificar se Redis está rodando: `redis-cli ping`
- Verificar string de conexão no appsettings.json
- Verificar firewall/portas

### Documentos não são processados
- Verificar logs da aplicação
- Verificar se há itens na fila: `LLEN document-processing-queue`
- Verificar se DocumentProcessorService está sendo executado

### Performance lenta
- Ajustar intervalo de polling
- Implementar processamento em lote
- Otimizar queries do banco de dados
- Considerar múltiplos workers