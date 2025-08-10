# Sistema de API Keys para Serviços Externos

Este documento descreve o sistema de API keys implementado no Scriptoryum para permitir que serviços externos se conectem de forma segura às APIs.

## Visão Geral

O sistema de API keys permite que serviços externos (como workers, APIs de análise, etc.) se autentiquem com a API principal do Scriptoryum usando chaves de API em vez de tokens JWT de usuário. Isso é mais apropriado para comunicação entre serviços.

## Características

### Segurança
- **Hash SHA-256**: As chaves são armazenadas como hash SHA-256 no banco de dados
- **Prefixo identificador**: Todas as chaves começam com `sk-` para fácil identificação
- **Expiração**: Suporte a data de expiração opcional
- **Revogação**: Chaves podem ser revogadas a qualquer momento
- **Status**: Controle de status (Ativa, Inativa, Revogada, Expirada, Suspensa)

### Controle de Uso
- **Limite mensal**: Controle opcional de uso mensal
- **Contador de uso**: Rastreamento de quantas vezes a chave foi usada
- **Último uso**: Registro da última vez que a chave foi utilizada
- **IPs permitidos**: Restrição opcional por endereços IP
- **Permissões**: Sistema de permissões customizáveis

### Auditoria
- **Criador**: Registro de qual usuário criou a chave
- **Timestamps**: Criação e última atualização
- **Logs de uso**: Rastreamento completo de utilização

## Estrutura do Banco de Dados

### Tabela `service_api_keys`

```sql
CREATE TABLE service_api_keys (
    id UUID PRIMARY KEY,
    service_name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    api_key_hash VARCHAR(128) NOT NULL UNIQUE,
    key_prefix VARCHAR(10) NOT NULL,
    key_suffix VARCHAR(10) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    expires_at TIMESTAMP,
    last_used_at TIMESTAMP,
    usage_count BIGINT DEFAULT 0,
    monthly_usage_limit INTEGER,
    current_month_usage INTEGER DEFAULT 0,
    current_month_year VARCHAR(7) NOT NULL,
    permissions VARCHAR(2000),
    allowed_ips VARCHAR(1000),
    created_by_user_id VARCHAR(450) NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    
    FOREIGN KEY (created_by_user_id) REFERENCES AspNetUsers(Id)
);
```

## API Endpoints

### Criar API Key
```http
POST /api/ServiceApiKey
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
    "serviceName": "Worker Service",
    "description": "API key para o worker de processamento de documentos",
    "monthlyUsageLimit": 10000,
    "expiresAt": "2024-12-31T23:59:59Z",
    "permissions": "documents:read,documents:write,notifications:create",
    "allowedIPs": "192.168.1.100,10.0.0.50"
}
```

**Resposta:**
```json
{
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "serviceName": "Worker Service",
    "description": "API key para o worker de processamento de documentos",
    "apiKey": "sk-1234567890abcdef...", // Retornado apenas na criação
    "keyPrefix": "sk-1234567",
    "keySuffix": "...cdef",
    "status": "Active",
    "expiresAt": "2024-12-31T23:59:59Z",
    "monthlyUsageLimit": 10000,
    "permissions": "documents:read,documents:write,notifications:create",
    "allowedIPs": "192.168.1.100,10.0.0.50",
    "createdAt": "2024-01-15T10:30:00Z"
}
```

### Listar API Keys
```http
GET /api/ServiceApiKey
Authorization: Bearer {jwt_token}
```

### Obter API Key por ID
```http
GET /api/ServiceApiKey/{id}
Authorization: Bearer {jwt_token}
```

### Atualizar API Key
```http
PUT /api/ServiceApiKey/{id}
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
    "serviceName": "Worker Service Updated",
    "description": "Descrição atualizada",
    "monthlyUsageLimit": 15000,
    "expiresAt": "2025-12-31T23:59:59Z",
    "permissions": "documents:read,documents:write,notifications:create,analytics:read",
    "allowedIPs": "192.168.1.100,10.0.0.50,10.0.0.51"
}
```

### Revogar API Key
```http
DELETE /api/ServiceApiKey/{id}
Authorization: Bearer {jwt_token}
```

## Autenticação com API Key

### Formato do Header
```http
Authorization: Bearer sk-1234567890abcdef...
```

### Middleware de Autenticação
O middleware `ServiceApiKeyMiddleware` intercepta requisições com API keys e:

1. Valida a chave no banco de dados
2. Verifica se a chave está ativa e não expirada
3. Verifica limites de uso mensal
4. Atualiza estatísticas de uso
5. Cria claims de autenticação para o contexto da requisição

### Claims Criados
- `NameIdentifier`: ID do usuário que criou a chave
- `ServiceApiKeyId`: ID da chave de API
- `ServiceName`: Nome do serviço
- `AuthType`: "ServiceApiKey"
- `Permissions`: Permissões da chave (se definidas)

## Configuração no Worker

### Variáveis de Ambiente
```bash
# .env do worker
MAIN_API_URL=https://api.scriptoryum.com.br
MAIN_API_TOKEN=sk-1234567890abcdef...
```

### Código de Exemplo
```python
import httpx
import os

async def create_notification(user_id: str, title: str, message: str, notification_type: str = "info"):
    """Cria uma notificação via API principal usando API key"""
    
    api_url = os.getenv("MAIN_API_URL")
    api_token = os.getenv("MAIN_API_TOKEN")
    
    if not api_url or not api_token:
        print("MAIN_API_URL ou MAIN_API_TOKEN não configurados")
        return
    
    headers = {
        "Authorization": f"Bearer {api_token}",
        "Content-Type": "application/json"
    }
    
    payload = {
        "userId": user_id,
        "title": title,
        "message": message,
        "type": notification_type
    }
    
    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(
                f"{api_url}/api/notifications",
                json=payload,
                headers=headers,
                timeout=30.0
            )
            
            if response.status_code == 201:
                print(f"Notificação criada com sucesso: {title}")
            else:
                print(f"Erro ao criar notificação: {response.status_code} - {response.text}")
                
    except Exception as e:
        print(f"Erro ao conectar com a API principal: {e}")
```

## Migração do Sistema Atual

### Para o Worker
1. Criar uma API key através da interface web ou API
2. Configurar `MAIN_API_TOKEN` com a nova chave
3. Remover configurações JWT antigas se existirem

### Para a API de Análise
1. Criar uma API key específica para análise
2. Atualizar configuração `MAIN_API_TOKEN`
3. Verificar permissões necessárias

## Boas Práticas

### Segurança
- **Rotação regular**: Renove as chaves periodicamente
- **Princípio do menor privilégio**: Configure apenas as permissões necessárias
- **Monitoramento**: Acompanhe o uso das chaves regularmente
- **Revogação imediata**: Revogue chaves comprometidas imediatamente

### Gerenciamento
- **Nomes descritivos**: Use nomes claros para identificar o propósito
- **Documentação**: Mantenha registro de onde cada chave é usada
- **Limites apropriados**: Configure limites mensais baseados no uso esperado
- **Expiração**: Defina datas de expiração apropriadas

### Desenvolvimento
- **Variáveis de ambiente**: Nunca hardcode chaves no código
- **Logs seguros**: Não registre chaves completas em logs
- **Tratamento de erros**: Implemente retry e fallback apropriados

## Troubleshooting

### Erro 401 - Unauthorized
- Verifique se a chave está correta
- Confirme se a chave não expirou
- Verifique se o status é "Active"
- Confirme se o IP está na lista permitida

### Limite de uso excedido
- Verifique o limite mensal configurado
- Considere aumentar o limite ou otimizar o uso
- Monitore padrões de uso anômalos

### Chave não encontrada
- Confirme se a chave não foi revogada
- Verifique se foi digitada corretamente
- Confirme se está usando o formato correto (`sk-...`)

## Monitoramento e Métricas

O sistema registra automaticamente:
- Número de usos por chave
- Último acesso
- Uso mensal atual
- Tentativas de acesso com chaves inválidas

Essas métricas podem ser usadas para:
- Detectar uso anômalo
- Planejar limites de capacidade
- Identificar chaves não utilizadas
- Auditoria de segurança