using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

/// <summary>
/// Controller para gestão de templates de tipos de documentos
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentTypeTemplateController : ControllerBase
{
    private readonly IDocumentTypeTemplateService _templateService;
    private readonly ILogger<DocumentTypeTemplateController> _logger;

    public DocumentTypeTemplateController(
        IDocumentTypeTemplateService templateService,
        ILogger<DocumentTypeTemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    #region CRUD de Templates

    /// <summary>
    /// Obtém todos os templates disponíveis para a organização
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Listar templates", Description = "Obtém todos os templates disponíveis para a organização (próprios e públicos)")]
    [SwaggerResponse(200, "Lista de templates", typeof(IEnumerable<DocumentTypeTemplateDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentTypeTemplateDto>>> GetTemplates([FromQuery] bool includePublic = true)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var templates = await _templateService.GetAllAsync(organizationId.Value, includePublic);
        return Ok(templates);
    }

    /// <summary>
    /// Obtém um template específico
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Obter template", Description = "Obtém um template específico por ID")]
    [SwaggerResponse(200, "Template encontrado", typeof(DocumentTypeTemplateDto))]
    [SwaggerResponse(404, "Template não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeTemplateDto>> GetTemplate(int id)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var template = await _templateService.GetByIdAsync(id, organizationId.Value);
        if (template == null)
            return NotFound();

        return Ok(template);
    }

    /// <summary>
    /// Cria um novo template
    /// </summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Criar template", Description = "Cria um novo template de tipo de documento")]
    [SwaggerResponse(201, "Template criado com sucesso", typeof(DocumentTypeTemplateDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeTemplateDto>> CreateTemplate([FromBody] CreateDocumentTypeTemplateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var template = await _templateService.CreateAsync(dto, organizationId.Value, userId);
            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Atualiza um template existente
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "Atualizar template", Description = "Atualiza um template existente")]
    [SwaggerResponse(200, "Template atualizado com sucesso", typeof(DocumentTypeTemplateDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Template não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeTemplateDto>> UpdateTemplate(int id, [FromBody] CreateDocumentTypeTemplateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var template = await _templateService.UpdateAsync(id, dto, organizationId.Value, userId);
            return Ok(template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Exclui um template
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Excluir template", Description = "Exclui um template (se não estiver sendo usado)")]
    [SwaggerResponse(204, "Template excluído com sucesso")]
    [SwaggerResponse(400, "Não é possível excluir o template")]
    [SwaggerResponse(404, "Template não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var success = await _templateService.DeleteAsync(id, organizationId.Value, userId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    #endregion

    #region Aplicação de Templates

    /// <summary>
    /// Aplica um template para criar um novo tipo de documento
    /// </summary>
    [HttpPost("{id}/apply")]
    [SwaggerOperation(Summary = "Aplicar template", Description = "Aplica um template para criar um novo tipo de documento")]
    [SwaggerResponse(201, "Tipo de documento criado a partir do template", typeof(DocumentTypeDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Template não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeDto>> ApplyTemplate(int id, [FromBody] ApplyTemplateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        // Definir o ID do template no DTO
        dto.TemplateId = id;

        try
        {
            var documentType = await _templateService.ApplyTemplateAsync(dto, organizationId.Value, userId);
            return CreatedAtAction("GetDocumentType", "DocumentType", new { id = documentType.Id }, documentType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Cria um template a partir de um tipo de documento existente
    /// </summary>
    [HttpPost("from-document-type/{documentTypeId}")]
    [SwaggerOperation(Summary = "Criar template a partir de tipo de documento", Description = "Cria um template baseado em um tipo de documento existente")]
    [SwaggerResponse(201, "Template criado com sucesso", typeof(DocumentTypeTemplateDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Tipo de documento não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeTemplateDto>> CreateTemplateFromDocumentType(
        int documentTypeId,
        [FromBody] CreateTemplateFromDocumentTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var template = await _templateService.CreateTemplateFromDocumentTypeAsync(
                documentTypeId, dto.TemplateName, dto.TemplateDescription, 
                dto.Category, dto.IsPublic, organizationId.Value, userId);
            
            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    #endregion

    #region Gestão de Templates Públicos/Privados

    /// <summary>
    /// Obtém apenas templates públicos
    /// </summary>
    [HttpGet("public")]
    [SwaggerOperation(Summary = "Listar templates públicos", Description = "Obtém todos os templates públicos disponíveis")]
    [SwaggerResponse(200, "Lista de templates públicos", typeof(IEnumerable<DocumentTypeTemplateDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentTypeTemplateDto>>> GetPublicTemplates()
    {
        var templates = await _templateService.GetPublicTemplatesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// Torna um template público
    /// </summary>
    [HttpPost("{id}/make-public")]
    [SwaggerOperation(Summary = "Tornar template público", Description = "Torna um template privado em público")]
    [SwaggerResponse(200, "Template tornado público com sucesso")]
    [SwaggerResponse(404, "Template não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> MakeTemplatePublic(int id)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        var success = await _templateService.MakeTemplatePublicAsync(id, organizationId.Value, userId);
        if (!success)
            return NotFound();

        return Ok(new { message = "Template tornado público com sucesso" });
    }

    /// <summary>
    /// Torna um template privado
    /// </summary>
    [HttpPost("{id}/make-private")]
    [SwaggerOperation(Summary = "Tornar template privado", Description = "Torna um template público em privado")]
    [SwaggerResponse(200, "Template tornado privado com sucesso")]
    [SwaggerResponse(404, "Template não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> MakeTemplatePrivate(int id)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        var success = await _templateService.MakeTemplatePrivateAsync(id, organizationId.Value, userId);
        if (!success)
            return NotFound();

        return Ok(new { message = "Template tornado privado com sucesso" });
    }

    /// <summary>
    /// Duplica um template
    /// </summary>
    [HttpPost("{id}/duplicate")]
    [SwaggerOperation(Summary = "Duplicar template", Description = "Cria uma cópia de um template existente")]
    [SwaggerResponse(201, "Template duplicado com sucesso", typeof(DocumentTypeTemplateDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Template não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<DocumentTypeTemplateDto>> DuplicateTemplate(int id, [FromBody] DuplicateTemplateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        
        if (organizationId == null || userId == null)
            return BadRequest("Usuário ou organização não identificados");

        try
        {
            var template = await _templateService.DuplicateTemplateAsync(id, dto.NewName, organizationId.Value, userId);
            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    #endregion

    #region Consultas e Relatórios

    /// <summary>
    /// Obtém templates por categoria
    /// </summary>
    [HttpGet("category/{category}")]
    [SwaggerOperation(Summary = "Templates por categoria", Description = "Obtém templates de uma categoria específica")]
    [SwaggerResponse(200, "Templates da categoria", typeof(IEnumerable<DocumentTypeTemplateDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentTypeTemplateDto>>> GetTemplatesByCategory(string category)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var templates = await _templateService.GetByCategory(category, organizationId.Value);
        return Ok(templates);
    }

    /// <summary>
    /// Obtém todas as categorias disponíveis
    /// </summary>
    [HttpGet("categories")]
    [SwaggerOperation(Summary = "Listar categorias", Description = "Obtém todas as categorias de templates disponíveis")]
    [SwaggerResponse(200, "Lista de categorias", typeof(IEnumerable<string>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var categories = await _templateService.GetCategoriesAsync(organizationId.Value);
        return Ok(categories);
    }

    /// <summary>
    /// Busca templates
    /// </summary>
    [HttpGet("search")]
    [SwaggerOperation(Summary = "Buscar templates", Description = "Busca templates por nome, descrição ou campos")]
    [SwaggerResponse(200, "Resultados da busca", typeof(IEnumerable<DocumentTypeTemplateDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentTypeTemplateDto>>> SearchTemplates([FromQuery] string searchTerm)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var results = await _templateService.SearchTemplatesAsync(searchTerm, organizationId.Value);
        return Ok(results);
    }

    /// <summary>
    /// Obtém estatísticas de um template
    /// </summary>
    [HttpGet("{id}/statistics")]
    [SwaggerOperation(Summary = "Estatísticas do template", Description = "Obtém estatísticas de uso de um template")]
    [SwaggerResponse(200, "Estatísticas do template", typeof(Dictionary<string, object>))]
    [SwaggerResponse(404, "Template não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<Dictionary<string, object>>> GetTemplateStatistics(int id)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var statistics = await _templateService.GetTemplateStatisticsAsync(id, organizationId.Value);
        if (statistics == null || !statistics.Any())
            return NotFound();

        return Ok(statistics);
    }

    /// <summary>
    /// Obtém os templates mais utilizados
    /// </summary>
    [HttpGet("most-used")]
    [SwaggerOperation(Summary = "Templates mais utilizados", Description = "Obtém os templates mais utilizados na organização")]
    [SwaggerResponse(200, "Templates mais utilizados", typeof(IEnumerable<DocumentTypeTemplateDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<DocumentTypeTemplateDto>>> GetMostUsedTemplates([FromQuery] int limit = 10)
    {
        var organizationId = GetOrganizationId();
        if (organizationId == null)
            return BadRequest("Organização não identificada");

        var mostUsed = await _templateService.GetMostUsedTemplatesAsync(organizationId.Value, limit);
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

#region DTOs Auxiliares

/// <summary>
/// DTO para criação de template a partir de tipo de documento
/// </summary>
public class CreateTemplateFromDocumentTypeDto
{
    public string TemplateName { get; set; } = string.Empty;
    public string? TemplateDescription { get; set; }
    public string? Category { get; set; }
    public bool IsPublic { get; set; } = false;
}

/// <summary>
/// DTO para duplicação de template
/// </summary>
public class DuplicateTemplateDto
{
    public string NewName { get; set; } = string.Empty;
}

#endregion