# Autorização da API - JWT Tokens e API Keys

Este documento explica como funciona o sistema de autorização da API Scriptoryum, que suporta tanto tokens JWT para usuários registrados quanto API Keys para integração de serviços.

## Tipos de Autenticação Suportados

### 1. JWT Tokens (Usuários Registrados)
- **Formato**: `Bearer <jwt_token>`
- **Uso**: Autenticação de usuários que fizeram login na aplicação
- **Obtenção**: Através dos endpoints `/api/auth/login` ou `/api/auth/register`
- **Validade**: Configurável via `appsettings.json`

### 2. API Keys (Integração de Serviços)
- **Formato**: `Bearer sk-<api_key>`
- **Uso**: Autenticação de serviços externos e integrações
- **Obtenção**: Através do endpoint `/api/serviceapikey` (requer autenticação JWT)
- **Prefixo**: Todas as API keys começam com `sk-`

## Como Funciona a Autorização

### Pipeline de Autenticação
1. **ServiceApiKeyMiddleware**: Intercepta requests com API keys (`Bearer sk-*`)
2. **JWT Authentication**: Processa tokens JWT padrão
3. **Authorization**: Valida se o usuário tem permissão para acessar o endpoint

### Middleware ServiceApiKeyMiddleware
O middleware `ServiceApiKeyMiddleware` é executado **antes** da autenticação JWT e:

1. Verifica se o header `Authorization` contém `Bearer sk-`
2. Extrai a API key e valida no banco de dados
3. Se válida, cria um `ClaimsPrincipal` com as seguintes claims:
   - `NameIdentifier`: ID do usuário que criou a API key
   - `ServiceApiKeyId`: ID da API key
   - `ServiceName`: Nome do serviço
   - `AuthType`: "ServiceApiKey"
   - `Permissions`: Permissões específicas (se configuradas)

### Atributo [Authorize]
O atributo `[Authorize]` funciona com **ambos** os tipos de autenticação:
- **JWT Tokens**: Usuário autenticado via login
- **API Keys**: Serviço autenticado via API key válida

## Configuração no Program.cs

```csharp
// Middleware de API Key ANTES da autenticação
app.UseMiddleware<ServiceApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
```

## Swagger/OpenAPI
A documentação Swagger suporta ambos os tipos de autenticação:
- **Bearer**: Para tokens JWT
- **ApiKey**: Para API keys (prefixo `sk-`)

## Exemplos de Uso

### Usando JWT Token
```bash
curl -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
     https://api.scriptoryum.com/api/documents
```

### Usando API Key
```bash
curl -H "Authorization: Bearer sk-1234567890abcdef..." \
     https://api.scriptoryum.com/api/documents
```

## Gerenciamento de API Keys

### Criar API Key
```bash
POST /api/serviceapikey
Authorization: Bearer <jwt_token>
{
  "serviceName": "MeuServiço",
  "description": "API key para integração",
  "monthlyUsageLimit": 10000,
  "permissions": "read,write"
}
```

### Listar API Keys
```bash
GET /api/serviceapikey
Authorization: Bearer <jwt_token>
```

### Revogar API Key
```bash
DELETE /api/serviceapikey/{id}
Authorization: Bearer <jwt_token>
```

## Segurança

### API Keys
- Armazenadas como hash SHA-256 no banco de dados
- Prefixo e sufixo visíveis para identificação
- Controle de uso mensal
- Data de expiração configurável
- Permissões específicas por key
- Restrição por IP (opcional)

### JWT Tokens
- Assinatura HMAC SHA-256
- Validação de issuer, audience e lifetime
- Claims customizadas para autorização

## Troubleshooting

### API Key não funciona
1. Verificar se a API key começa com `sk-`
2. Verificar se não está expirada
3. Verificar se não excedeu o limite mensal
4. Verificar se o status é "Active"

### JWT Token não funciona
1. Verificar se não está expirado
2. Verificar configuração de `Jwt:Key`, `Jwt:Issuer` e `Jwt:Audience`
3. Verificar se o usuário ainda existe no sistema

## Logs
O sistema registra eventos de autenticação para auditoria:
- Uso de API keys
- Falhas de autenticação
- Limites excedidos