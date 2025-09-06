using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

/// <summary>
/// Controller para gestão de tipos de documentos e campos dinâmicos
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentTypeController : ControllerBase
{
    private readonly IDocumentTypeService _documentTypeService;
    private readonly IDocumentFieldService _documentFieldService;
    private readonly IDocumentTypeTemplateService _documentTypeTemplateService;
    private readonly ILogger<DocumentTypeController> _logger;

    public DocumentTypeController(
        IDocumentTypeService documentTypeService,
        IDocumentFieldService documentFieldService,
        IDocumentTypeTemplateService documentTypeTemplateService,
        ILogger<DocumentTypeController> logger)
    {
        _documentTypeService = documentTypeService;
        _documentFieldService = documentFieldService;
        _documentTypeTemplateService = documentTypeTemplateService;
        _logger = logger;
    }

    #region CRUD de Tipos de Documentos

    /// <summary>
    /// Obtém todos os tipos de documentos da organização
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Listar tipos de documentos", Description = "Obtém todos os tipos de documentos da organização do usuário")]
    [SwaggerResponse(200, "Lista de tipos de documentos", typeof(IEnumerable<DocumentTypeDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> GetDocumentTypes([FromQuery] bool includeInactive = false)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var documentTypes = await _documentTypeService.GetAllByOrganizationAsync(organizationId.Value, includeInactive);
        return Ok(documentTypes);
    }

    /// <summary>
    /// Obtém um tipo de documento específico
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Obter tipo de documento", Description = "Obtém um tipo de documento específico por ID")]
    [SwaggerResponse(200, "Tipo de documento encontrado", typeof(DocumentTypeDto))]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeDto>> GetDocumentType(int id)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var documentType = await _documentTypeService.GetByIdAsync(id, organizationId.Value);
        if (documentType == null)
            return NotFound();

        return Ok(documentType);
    }

    /// <summary>
    /// Cria um novo tipo de documento
    /// </summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Criar tipo de documento", Description = "Cria um novo tipo de documento com campos dinâmicos")]
    [SwaggerResponse(201, "Tipo de documento criado com sucesso", typeof(DocumentTypeDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeDto>> CreateDocumentType([FromBody] CreateDocumentTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var documentType = await _documentTypeService.CreateAsync(dto, organizationId.Value, userId);
            return CreatedAtAction(nameof(GetDocumentType), new { id = documentType.Id }, documentType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Atualiza um tipo de documento existente
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "Atualizar tipo de documento", Description = "Atualiza um tipo de documento existente")]
    [SwaggerResponse(200, "Tipo de documento atualizado com sucesso", typeof(DocumentTypeDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeDto>> UpdateDocumentType(int id, [FromBody] UpdateDocumentTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var documentType = await _documentTypeService.UpdateAsync(id, dto, organizationId.Value, userId);
            return Ok(documentType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Verifica se um tipo de documento pode ser excluído
    /// </summary>
    [HttpGet("{id}/can-delete")]
    [SwaggerOperation(Summary = "Verificar se pode excluir", Description = "Verifica se um tipo de documento pode ser excluído")]
    [SwaggerResponse(200, "Resultado da verificação", typeof(object))]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<object>> CanDeleteDocumentType(int id)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        try
        {
            var canDelete = await _documentTypeService.CanDeleteDocumentTypeAsync(id, organizationId.Value);
            return Ok(new { 
                canDelete = canDelete,
                reason = canDelete ? null : "Este tipo de documento possui documentos associados e não pode ser excluído."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Exclui um tipo de documento
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Excluir tipo de documento", Description = "Exclui um tipo de documento (se não estiver sendo usado)")]
    [SwaggerResponse(204, "Tipo de documento excluído com sucesso")]
    [SwaggerResponse(400, "Não é possível excluir o tipo de documento")]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> DeleteDocumentType(int id)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var success = await _documentTypeService.DeleteAsync(id, organizationId.Value, userId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Ativa um tipo de documento
    /// </summary>
    [HttpPost("{id}/activate")]
    [SwaggerOperation(Summary = "Ativar tipo de documento", Description = "Ativa um tipo de documento inativo")]
    [SwaggerResponse(200, "Tipo de documento ativado com sucesso")]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> ActivateDocumentType(int id)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        var success = await _documentTypeService.ActivateAsync(id, organizationId.Value, userId);
        if (!success)
            return NotFound();

        return Ok(new { message = "Tipo de documento ativado com sucesso" });
    }

    /// <summary>
    /// Desativa um tipo de documento
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [SwaggerOperation(Summary = "Desativar tipo de documento", Description = "Desativa um tipo de documento")]
    [SwaggerResponse(200, "Tipo de documento desativado com sucesso")]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> DeactivateDocumentType(int id)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        var success = await _documentTypeService.DeactivateAsync(id, organizationId.Value, userId);
        if (!success)
            return NotFound();

        return Ok(new { message = "Tipo de documento desativado com sucesso" });
    }

    #endregion

    #region Gestão de Campos

    /// <summary>
    /// Adiciona um campo a um tipo de documento
    /// </summary>
    [HttpPost("{documentTypeId}/fields")]
    [SwaggerOperation(Summary = "Adicionar campo", Description = "Adiciona um novo campo dinâmico a um tipo de documento")]
    [SwaggerResponse(201, "Campo adicionado com sucesso", typeof(DocumentTypeFieldDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeFieldDto>> AddField(int documentTypeId, [FromBody] CreateDocumentTypeFieldDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var field = await _documentTypeService.AddFieldAsync(documentTypeId, dto, organizationId.Value, userId);
            return CreatedAtAction(nameof(GetDocumentType), new { id = documentTypeId }, field);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Atualiza um campo de um tipo de documento
    /// </summary>
    [HttpPut("{documentTypeId}/fields/{fieldId}")]
    [SwaggerOperation(Summary = "Atualizar campo", Description = "Atualiza um campo dinâmico existente")]
    [SwaggerResponse(200, "Campo atualizado com sucesso", typeof(DocumentTypeFieldDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Campo não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeFieldDto>> UpdateField(int documentTypeId, int fieldId, [FromBody] UpdateDocumentTypeFieldDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var field = await _documentTypeService.UpdateFieldAsync(fieldId, dto, organizationId.Value, userId);
            return Ok(field);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Remove um campo de um tipo de documento
    /// </summary>
    [HttpDelete("{documentTypeId}/fields/{fieldId}")]
    [SwaggerOperation(Summary = "Remover campo", Description = "Remove um campo dinâmico de um tipo de documento")]
    [SwaggerResponse(204, "Campo removido com sucesso")]
    [SwaggerResponse(400, "Não é possível remover o campo")]
    [SwaggerResponse(404, "Campo não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> RemoveField(int documentTypeId, int fieldId)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var success = await _documentTypeService.RemoveFieldAsync(fieldId, organizationId.Value, userId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reordena os campos de um tipo de documento
    /// </summary>
    [HttpPost("{documentTypeId}/fields/reorder")]
    [SwaggerOperation(Summary = "Reordenar campos", Description = "Reordena os campos de um tipo de documento")]
    [SwaggerResponse(200, "Campos reordenados com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> ReorderFields(int documentTypeId, [FromBody] Dictionary<int, int> fieldOrders)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var success = await _documentTypeService.ReorderFieldsAsync(documentTypeId, fieldOrders, organizationId.Value, userId);
            if (!success)
                return NotFound();

            return Ok(new { message = "Campos reordenados com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    #endregion

    #region Associação de Documentos

    /// <summary>
    /// Associa um documento a um tipo de documento
    /// </summary>
    [HttpPost("associate")]
    [SwaggerOperation(Summary = "Associar documento", Description = "Associa um documento existente a um tipo de documento")]
    [SwaggerResponse(200, "Documento associado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Documento ou tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> AssociateDocument([FromBody] AssociateDocumentTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var result = await _documentTypeService.AssociateDocumentAsync(dto, organizationId.Value, userId);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new { message = "Documento associado com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Altera o tipo de documento de um documento
    /// </summary>
    [HttpPost("change-type")]
    [SwaggerOperation(Summary = "Alterar tipo de documento", Description = "Altera o tipo de documento de um documento existente")]
    [SwaggerResponse(200, "Tipo de documento alterado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Documento ou tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> ChangeDocumentType([FromBody] AssociateDocumentTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var result = await _documentTypeService.ChangeDocumentTypeAsync(dto.DocumentId, dto.DocumentTypeId, organizationId.Value, userId);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new { message = "Tipo de documento alterado com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Remove a associação de um documento com seu tipo
    /// </summary>
    [HttpDelete("documents/{documentId}/association")]
    [SwaggerOperation(Summary = "Remover associação", Description = "Remove a associação de um documento com seu tipo")]
    [SwaggerResponse(204, "Associação removida com sucesso")]
    [SwaggerResponse(404, "Documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> RemoveDocumentAssociation(int documentId)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        var result = await _documentTypeService.RemoveDocumentAssociationAsync(documentId, organizationId.Value, userId);
        if (!result.Success)
            return BadRequest(result.Message);

        return NoContent();
    }

    #endregion

    #region Extração e Validação de Campos

    /// <summary>
    /// Obtém os valores dos campos de um documento
    /// </summary>
    [HttpGet("documents/{documentId}/field-values")]
    [SwaggerOperation(Summary = "Obter valores dos campos", Description = "Obtém os valores extraídos dos campos de um documento")]
    [SwaggerResponse(200, "Valores dos campos", typeof(IEnumerable<DocumentFieldValueDto>))]
    [SwaggerResponse(404, "Documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentFieldValueDto>>> GetDocumentFieldValues(int documentId)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var fieldValues = await _documentTypeService.GetDocumentFieldValuesAsync(documentId, organizationId.Value);
        return Ok(fieldValues);
    }

    /// <summary>
    /// Dispara a extração de campos para um documento
    /// </summary>
    [HttpPost("documents/{documentId}/extract-fields")]
    [SwaggerOperation(Summary = "Extrair campos", Description = "Dispara o processo de extração de campos para um documento")]
    [SwaggerResponse(200, "Extração iniciada com sucesso")]
    [SwaggerResponse(404, "Documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> TriggerFieldExtraction(int documentId)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var result = await _documentTypeService.TriggerFieldExtractionAsync(documentId, organizationId.Value, userId);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new { message = "Extração de campos iniciada com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Valida um valor de campo específico
    /// </summary>
    [HttpPost("field-values/{fieldValueId}/validate")]
    [SwaggerOperation(Summary = "Validar campo", Description = "Valida um valor de campo específico")]
    [SwaggerResponse(200, "Campo validado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Valor do campo não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> ValidateFieldValue(int fieldValueId, [FromBody] ValidateFieldValueDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var validationDto = new ValidateFieldValueDto
            {
                FieldValueId = fieldValueId,
                IsValid = dto.IsValid,
                ValidationNotes = dto.CorrectedValue
            };
            var result = await _documentTypeService.ValidateFieldValueAsync(validationDto, organizationId.Value, userId);
            if (result == null)
                return NotFound();

            return Ok(new { message = "Campo validado com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    #endregion

    #region Consultas e Relatórios

    /// <summary>
    /// Busca tipos de documentos
    /// </summary>
    [HttpGet("search")]
    [SwaggerOperation(Summary = "Buscar tipos de documentos", Description = "Busca tipos de documentos por nome ou descrição")]
    [SwaggerResponse(200, "Resultados da busca", typeof(IEnumerable<DocumentTypeDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> SearchDocumentTypes([FromQuery] string searchTerm)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var results = await _documentTypeService.SearchDocumentTypesAsync(searchTerm, organizationId.Value);
        return Ok(results);
    }

    /// <summary>
    /// Obtém estatísticas de um tipo de documento
    /// </summary>
    [HttpGet("{id}/statistics")]
    [SwaggerOperation(Summary = "Estatísticas do tipo de documento", Description = "Obtém estatísticas de uso de um tipo de documento")]
    [SwaggerResponse(200, "Estatísticas do tipo de documento", typeof(Dictionary<string, object>))]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<Dictionary<string, object>>> GetDocumentTypeStatistics(int id)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var statistics = await _documentTypeService.GetDocumentTypeStatisticsAsync(id, organizationId.Value);
        if (statistics == null || !statistics.Any())
            return NotFound();

        return Ok(statistics);
    }

    /// <summary>
    /// Obtém os tipos de documentos mais utilizados
    /// </summary>
    [HttpGet("most-used")]
    [SwaggerOperation(Summary = "Tipos mais utilizados", Description = "Obtém os tipos de documentos mais utilizados na organização")]
    [SwaggerResponse(200, "Tipos de documentos mais utilizados", typeof(IEnumerable<DocumentTypeDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> GetMostUsedDocumentTypes([FromQuery] int limit = 10)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var mostUsed = await _documentTypeService.GetMostUsedDocumentTypesAsync(organizationId.Value, limit);
        return Ok(mostUsed);
    }

    #endregion

    #region Métodos Auxiliares

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
}