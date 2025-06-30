# Scriptoryum API

API para análise de documentos com autenticação baseada em ASP.NET Core Identity e PostgreSQL.

## Pré-requisitos

- .NET 9.0 SDK
- PostgreSQL 12 ou superior
- Visual Studio 2022 ou VS Code

## Configuração do Banco de Dados

### 1. Instalar PostgreSQL

Baixe e instale o PostgreSQL em: https://www.postgresql.org/download/

### 2. Criar o Banco de Dados

```sql
-- Conecte-se ao PostgreSQL como superusuário
CREATE DATABASE scriptoryum_db;
CREATE DATABASE scriptoryum_dev_db;

-- Opcional: criar usuário específico
CREATE USER scriptoryum_user WITH PASSWORD 'sua_senha_segura';
GRANT ALL PRIVILEGES ON DATABASE scriptoryum_db TO scriptoryum_user;
GRANT ALL PRIVILEGES ON DATABASE scriptoryum_dev_db TO scriptoryum_user;
```

### 3. Configurar Connection String

Atualize as connection strings nos arquivos `appsettings.json` e `appsettings.Development.json` conforme necessário:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=scriptoryum_db;Username=postgres;Password=sua_senha;Port=5432"
  }
}
```

## Executar Migrações

### 1. Instalar EF Core Tools (se não estiver instalado)

```bash
dotnet tool install --global dotnet-ef
```

### 2. Criar e Aplicar Migrações

```bash
# Navegar para o diretório do projeto
cd Scriptoryum.Api

# Criar migração inicial
dotnet ef migrations add InitialCreate

# Aplicar migrações ao banco
dotnet ef database update
```

## Estrutura do Banco de Dados

O projeto utiliza convenção de nomenclatura **snake_case** para tabelas e colunas:

### Tabelas Principais

- `documents` - Documentos uploadados
- `extracted_entities` - Entidades extraídas dos documentos
- `insights` - Insights gerados pela análise
- `risks_detected` - Riscos identificados
- `timeline_events` - Eventos da linha do tempo

### Tabelas do Identity

- `asp_net_users` - Usuários do sistema
- `asp_net_roles` - Roles/perfis
- `asp_net_user_roles` - Relacionamento usuário-role
- E outras tabelas padrão do ASP.NET Core Identity

## Relacionamentos

- **ApplicationUser** → **Documents** (1:N)
- **Document** → **ExtractedEntities** (1:N)
- **Document** → **Insights** (1:N)
- **Document** → **RisksDetected** (1:N)
- **Document** → **TimelineEvents** (1:N)

## Executar o Projeto

```bash
# Restaurar dependências
dotnet restore

# Executar em modo desenvolvimento
dotnet run

# Ou executar com hot reload
dotnet watch run
```

## Funcionalidades do Identity

- Autenticação baseada em cookies
- Registro e login de usuários
- Gerenciamento de senhas
- Lockout de conta
- Validação de email único

## Configurações de Segurança

### Senhas
- Mínimo 6 caracteres
- Requer pelo menos 1 dígito
- Requer pelo menos 1 letra minúscula
- Requer pelo menos 1 letra maiúscula
- Não requer caracteres especiais

### Lockout
- Máximo 5 tentativas de login
- Bloqueio por 5 minutos após exceder tentativas

## Desenvolvimento

### Adicionar Nova Migração

```bash
dotnet ef migrations add NomeDaMigracao
dotnet ef database update
```

### Reverter Migração

```bash
dotnet ef database update NomeDaMigracaoAnterior
dotnet ef migrations remove
```

### Gerar Script SQL

```bash
dotnet ef migrations script
```

## Logs

O projeto está configurado para logar comandos SQL do Entity Framework em desenvolvimento. Verifique os logs no console para debug.

## Troubleshooting

### Erro de Conexão com PostgreSQL

1. Verifique se o PostgreSQL está rodando
2. Confirme as credenciais na connection string
3. Teste a conexão com um cliente PostgreSQL (pgAdmin, DBeaver)

### Erro de Migração

1. Verifique se o banco de dados existe
2. Confirme as permissões do usuário
3. Execute `dotnet ef database drop` e recrie se necessário

### Problemas com Identity

1. Verifique se as tabelas do Identity foram criadas
2. Confirme se `UseAuthentication()` está antes de `UseAuthorization()`
3. Verifique se o middleware está na ordem correta