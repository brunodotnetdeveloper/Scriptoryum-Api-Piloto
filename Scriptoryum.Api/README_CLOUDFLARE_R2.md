# Configuração do Cloudflare R2

Este documento explica como configurar o Cloudflare R2 para upload de arquivos na API Scriptoryum.

## 📋 Pré-requisitos

1. **Conta Cloudflare** com acesso ao R2
2. **Bucket criado** no Cloudflare R2
3. **Credenciais de API** (Access Key ID e Secret Access Key)

## 🔧 Configuração

### 1. Obter Credenciais do R2

1. Acesse o [Dashboard do Cloudflare](https://dash.cloudflare.com/)
2. Vá para **R2 Object Storage**
3. Clique em **Manage R2 API tokens**
4. Crie um novo token com permissões de **Object Read and Write**
5. Anote o **Access Key ID** e **Secret Access Key**

### 2. Criar Bucket

1. No dashboard do R2, clique em **Create bucket**
2. Escolha um nome único para o bucket (ex: `scriptoryum-documents`)
3. Selecione a região desejada
4. Configure as permissões conforme necessário

### 3. Configurar a Aplicação

Atualize os arquivos de configuração com suas credenciais:

#### appsettings.json
```json
{
  "CloudflareR2": {
    "BucketName": "seu-bucket-name",
    "AccessKey": "sua-access-key-id",
    "SecretKey": "sua-secret-access-key",
    "ServiceUrl": "https://seu-account-id.r2.cloudflarestorage.com"
  }
}
```

#### appsettings.Development.json
```json
{
  "CloudflareR2": {
    "BucketName": "seu-bucket-dev",
    "AccessKey": "sua-dev-access-key-id",
    "SecretKey": "sua-dev-secret-access-key",
    "ServiceUrl": "https://seu-account-id.r2.cloudflarestorage.com"
  }
}
```

### 4. Encontrar o Account ID

O Account ID pode ser encontrado:
1. No dashboard do Cloudflare, na barra lateral direita
2. Ou na URL do R2: `https://dash.cloudflare.com/{ACCOUNT_ID}/r2`

## 🔒 Segurança

### Variáveis de Ambiente (Recomendado para Produção)

Em produção, use variáveis de ambiente em vez de hardcoded values:

```bash
export CloudflareR2__BucketName="seu-bucket-prod"
export CloudflareR2__AccessKey="sua-access-key-prod"
export CloudflareR2__SecretKey="sua-secret-key-prod"
export CloudflareR2__ServiceUrl="https://seu-account-id.r2.cloudflarestorage.com"
```

### User Secrets (Desenvolvimento)

Para desenvolvimento local, use o User Secrets:

```bash
dotnet user-secrets set "CloudflareR2:AccessKey" "sua-access-key"
dotnet user-secrets set "CloudflareR2:SecretKey" "sua-secret-key"
dotnet user-secrets set "CloudflareR2:BucketName" "seu-bucket"
dotnet user-secrets set "CloudflareR2:ServiceUrl" "https://seu-account-id.r2.cloudflarestorage.com"
```

## 🚀 Funcionalidades Implementadas

### CloudflareR2Client

O cliente implementa os seguintes métodos:

- **UploadFileAsync**: Upload de arquivos via Stream
- **DeleteFileAsync**: Exclusão de arquivos
- **GetPresignedUrlAsync**: Geração de URLs pré-assinadas para download

### Integração com DocumentsService

O `DocumentsService` foi atualizado para:
- Usar o `CloudflareR2Client` para upload
- Definir `StorageProvider.CloudflareR2` nos documentos
- Manter compatibilidade com tipos de arquivo existentes

## 📝 Exemplo de Uso

```csharp
// Upload de arquivo
var uploadDto = new UploadDocumentDto
{
    File = formFile,
    Title = "Meu Documento",
    Description = "Descrição do documento"
};

var result = await documentsService.UploadDocumentAsync(uploadDto, userId);
```

## 🔍 Troubleshooting

### Erro de Autenticação
- Verifique se as credenciais estão corretas
- Confirme se o token tem as permissões necessárias

### Erro de Bucket
- Verifique se o bucket existe
- Confirme se o nome do bucket está correto na configuração

### Erro de Endpoint
- Verifique se o Account ID está correto na ServiceUrl
- Confirme se a URL segue o formato: `https://{ACCOUNT_ID}.r2.cloudflarestorage.com`

### Erro STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER
Este erro ocorre quando o AWS SDK tenta usar um método de assinatura mais recente que o Cloudflare R2 não suporta.

**Solução implementada:**
- Configuração `UseChunkEncoding = false` no `AmazonS3Config`
- Configuração `UseChunkEncoding = false` no `PutObjectRequest`
- Uso da versão de assinatura "4" explicitamente
- Desabilitação do payload signing quando necessário

Essas configurações garantem compatibilidade total com o Cloudflare R2.

## 📚 Referências

- [Documentação oficial do Cloudflare R2](https://developers.cloudflare.com/r2/)
- [AWS SDK for .NET S3 Documentation](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/s3-apis-intro.html)
- [Compatibilidade S3 do R2](https://developers.cloudflare.com/r2/api/s3/api/)