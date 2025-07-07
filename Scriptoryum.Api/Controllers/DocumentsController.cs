using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[Route("api/documents")]
[ApiController]
//[Authorize]
public class DocumentsController(IDocumentsService documentsService) : ControllerBase
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

        var result = await documentsService.UploadDocumentAsync(uploadDto, userId);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

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
}
