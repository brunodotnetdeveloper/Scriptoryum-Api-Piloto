using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Scriptoryum.Api.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[Route("api/documents")]
[ApiController]
[Authorize]
public class DocumentsController(IDocumentsService documentsService, IDocumentTypeService documentTypeService) : ControllerBase
{

    /// <summary>
    /// Faz upload de um documento
    /// </summary>
    /// <param name="uploadDto">Dados do documento para upload</param>
    /// <returns>Resultado do upload</returns>
    [HttpPost("upload")]
    [SwaggerOperation(Summary = "Upload de documento", Description = "Faz upload de documentos em formatos PDF, DOCX, DOC, TXT, RTF, ODT, HTML, XML")]
    [SwaggerResponse(200, "Upload realizado com sucesso", typeof(UploadDocumentResponseDto))]
    [SwaggerResponse(400, "Dados inválidos ou arquivo não suportado")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadDocumentResponseDto>> UploadDocument([FromForm] UploadDocumentDto uploadDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // var userId = "d18abe22-bab3-4cf1-89a3-20be6e4519c4";

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("Usuário não identificado");
        }

        var result = await documentsService.UploadDocumentAsync(uploadDto, userId, uploadDto.WorkspaceId);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    #region Helper Methods

    private int? GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (int.TryParse(organizationIdClaim, out var organizationId))
            return organizationId;
        
        // Fallback: buscar da primeira organização do usuário (temporário)
        // TODO: Implementar seleção de organização ativa
        return null;
    }

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    #endregion

    /// <summary>
    /// Detalhes completos de um documento, incluindo texto extraído, entidades, riscos, insights e timeline.
    /// </summary>
    [HttpGet("{id}/details")]
    [SwaggerOperation(Summary = "Detalhes completos do documento", Description = "Retorna texto extraído, entidades, riscos, insights e timeline do documento")]
    [SwaggerResponse(200, "Detalhes do documento", typeof(DocumentDetailsDto))]
    [SwaggerResponse(404, "Documento não encontrado")]
    public async Task<ActionResult<DocumentDetailsDto>> GetDocumentDetails(int id)
    {
        var details = await documentsService.GetDocumentDetailsByIdAsync(id);

        if (details == null)
            return NotFound();

        return Ok(details);
    }

    /// Gera uma URL pré-assinada para download do documento
    /// </summary>
    [HttpGet("{id}/download-url")]
    [SwaggerOperation(Summary = "Obter URL de download do documento", Description = "Gera uma URL pré-assinada para download do documento no Cloudflare R2")]
    [SwaggerResponse(200, "URL de download gerada com sucesso", typeof(string))]
    [SwaggerResponse(404, "Documento não encontrado")]
    public async Task<IActionResult> GetDocumentDownloadUrl(int id)
    {
        var details = await documentsService.GetDocumentDetailsByIdAsync(id);
        
        if (details == null || string.IsNullOrEmpty(details.StoragePath))
            return NotFound();

        // 10 minutos de validade
        var url = await documentsService.GetDocumentDownloadUrlAsync(details.StoragePath, TimeSpan.FromMinutes(10));
        
        if (string.IsNullOrEmpty(url))
            return StatusCode(500, "Falha ao gerar URL de download");
        
        return Ok(new { url });
    }

    /// <summary>
    /// Associa um documento a um tipo de documento
    /// </summary>
    [HttpPost("{id}/associate-type")]
    [SwaggerOperation(Summary = "Associar documento a tipo", Description = "Associa um documento existente a um tipo de documento")]
    [SwaggerResponse(200, "Documento associado com sucesso", typeof(DocumentOperationResponseDto))]
    [SwaggerResponse(404, "Documento ou tipo não encontrado")]
    [SwaggerResponse(400, "Dados inválidos")]
    public async Task<ActionResult<DocumentOperationResponseDto>> AssociateDocumentType(int id, [FromBody] AssociateDocumentTypeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        // Criar DTO com o ID do documento
        var associateDto = new AssociateDocumentTypeDto
        {
            DocumentId = id,
            DocumentTypeId = dto.DocumentTypeId
        };

        var result = await documentTypeService.AssociateDocumentAsync(associateDto, organizationId.Value, userId);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Remove a associação de um documento com seu tipo
    /// </summary>
    [HttpDelete("{id}/dissociate-type")]
    [SwaggerOperation(Summary = "Remover associação de tipo", Description = "Remove a associação de um documento com seu tipo")]
    [SwaggerResponse(200, "Associação removida com sucesso", typeof(DocumentOperationResponseDto))]
    [SwaggerResponse(404, "Documento não encontrado")]
    public async Task<ActionResult<DocumentOperationResponseDto>> DissociateDocumentType(int id)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        var result = await documentTypeService.RemoveDocumentAssociationAsync(id, organizationId.Value, userId);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Extrai valores de campos de um documento baseado em seu tipo
    /// </summary>
    [HttpPost("{id}/extract-fields")]
    [SwaggerOperation(Summary = "Extrair campos do documento", Description = "Extrai automaticamente os valores dos campos baseado no tipo do documento")]
    [SwaggerResponse(200, "Campos extraídos com sucesso", typeof(DocumentOperationResponseDto))]
    [SwaggerResponse(404, "Documento não encontrado")]
    [SwaggerResponse(400, "Documento não possui tipo associado")]
    public async Task<ActionResult<DocumentOperationResponseDto>> ExtractDocumentFields(int id)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        var result = await documentTypeService.TriggerFieldExtractionAsync(id, organizationId.Value, userId);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(new DocumentOperationResponseDto
        {
            Success = result.Success,
            Message = result.Message,
            DocumentId = result.DocumentId
        });
    }

    /// <summary>
    /// Obtém os valores dos campos de um documento
    /// </summary>
    [HttpGet("{id}/field-values")]
    [SwaggerOperation(Summary = "Obter valores dos campos", Description = "Retorna os valores dos campos extraídos/validados do documento")]
    [SwaggerResponse(200, "Valores dos campos", typeof(List<DocumentFieldValueDto>))]
    [SwaggerResponse(404, "Documento não encontrado")]
    public async Task<ActionResult<List<DocumentFieldValueDto>>> GetDocumentFieldValues(int id)
    {
        var organizationId = HttpContext.Items["OrganizationId"] as int?;
        if (!organizationId.HasValue)
        {
            return BadRequest("Organização não identificada");
        }

        var fieldValues = await documentTypeService.GetDocumentFieldValuesAsync(id, organizationId.Value);
        
        if (fieldValues == null)
        {
            return NotFound();
        }

        return Ok(fieldValues);
    }

    /// <summary>
    /// Valida um valor de campo específico
    /// </summary>
    [HttpPost("{id}/validate-field")]
    [SwaggerOperation(Summary = "Validar campo", Description = "Valida ou corrige o valor de um campo específico do documento")]
    [SwaggerResponse(200, "Campo validado com sucesso", typeof(ValidationResult))]
    [SwaggerResponse(404, "Documento ou campo não encontrado")]
    [SwaggerResponse(400, "Dados inválidos")]
    public async Task<ActionResult<ValidationResult>> ValidateDocumentField(int id, [FromBody] ValidateFieldValueDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var organizationId = HttpContext.Items["OrganizationId"] as int?;
        var userId = HttpContext.Items["UserId"] as string;
        
        if (!organizationId.HasValue || string.IsNullOrEmpty(userId))
        {
            return BadRequest("Usuário ou organização não identificados");
        }

        var result = await documentTypeService.ValidateDocumentFieldAsync(id, dto.FieldName, dto.Value, organizationId.Value, userId);
        
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
