#nullable enable
using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Interfaces;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;
using System.Text.Json;

namespace Scriptoryum.Api.Application.Services;

/// <summary>
/// Serviço para gestão de valores de campos de documentos
/// </summary>
public class DocumentFieldService : IDocumentFieldService
{
    private readonly ScriptoryumDbContext _context;
    private readonly ILogger<DocumentFieldService> _logger;

    public DocumentFieldService(ScriptoryumDbContext context, ILogger<DocumentFieldService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Gestão de Valores de Campos

    public async Task<DocumentFieldValueDto?> GetFieldValueByIdAsync(int fieldValueId, int organizationId)
    {
        var fieldValue = await _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.Id == fieldValueId && fv.Document.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        return fieldValue != null ? MapToDto(fieldValue) : null;
    }

    public async Task<IEnumerable<DocumentFieldValueDto>> GetFieldValuesByDocumentAsync(int documentId, int organizationId)
    {
        var fieldValues = await _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.DocumentId == documentId && fv.Document.Workspace.OrganizationId == organizationId)
            .OrderBy(fv => fv.DocumentTypeField.FieldOrder)
            .ToListAsync();

        return fieldValues.Select(MapToDto);
    }

    public async Task<IEnumerable<DocumentFieldValueDto>> GetFieldValuesByFieldAsync(int documentTypeFieldId, int organizationId)
    {
        var fieldValues = await _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.DocumentTypeFieldId == documentTypeFieldId && 
                        fv.Document.Workspace.OrganizationId == organizationId)
            .OrderByDescending(fv => fv.CreatedAt)
            .ToListAsync();

        return fieldValues.Select(MapToDto);
    }

    public async Task<DocumentFieldValueDto> CreateFieldValueAsync(int documentId, int documentTypeFieldId, 
        string extractedValue, decimal confidenceScore, int organizationId, string userId)
    {
        // Verificar se o documento existe e pertence à organização
        var document = await _context.Documents
            .Include(d => d.Workspace)
            .Where(d => d.Id == documentId && d.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (document == null)
        {
            throw new InvalidOperationException("Documento não encontrado ou não pertence à organização.");
        }

        // Verificar se o campo existe
        var field = await _context.DocumentTypeFields
            .Include(f => f.DocumentType)
            .Where(f => f.Id == documentTypeFieldId && f.DocumentType.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (field == null)
        {
            throw new InvalidOperationException("Campo não encontrado ou não pertence à organização.");
        }

        // Verificar se já existe um valor para este campo neste documento
        var existingValue = await _context.DocumentFieldValues
            .Where(fv => fv.DocumentId == documentId && fv.DocumentTypeFieldId == documentTypeFieldId)
            .FirstOrDefaultAsync();

        if (existingValue != null)
        {
            // Atualizar valor existente
            existingValue.ExtractedValue = extractedValue;
            existingValue.ConfidenceScore = confidenceScore;
            existingValue.ValidationStatus = ValidationStatus.Pending.ToString();
            existingValue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(existingValue);
        }

        // Criar novo valor
        var fieldValue = new DocumentFieldValue
        {
            DocumentId = documentId,
            DocumentTypeFieldId = documentTypeFieldId,
            ExtractedValue = extractedValue,
            ConfidenceScore = confidenceScore,
            ValidationStatus = ValidationStatus.Pending.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentFieldValues.Add(fieldValue);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Valor de campo criado para documento {DocumentId}, campo {FieldId} por usuário {UserId}", 
            documentId, documentTypeFieldId, userId);

        return MapToDto(fieldValue);
    }

    public async Task<DocumentFieldValueDto> UpdateFieldValueAsync(int fieldValueId, string newValue, 
        int organizationId, string userId)
    {
        var fieldValue = await _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.Id == fieldValueId && fv.Document.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (fieldValue == null)
        {
            throw new InvalidOperationException("Valor de campo não encontrado.");
        }

        // Salvar histórico antes da alteração
        await SaveFieldValueHistoryAsync(fieldValue, userId, "Manual Update");

        fieldValue.ExtractedValue = newValue;
        fieldValue.ValidationStatus = ValidationStatus.Pending.ToString();
        fieldValue.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Valor do campo {FieldValueId} atualizado por usuário {UserId}", fieldValueId, userId);

        return MapToDto(fieldValue);
    }

    public async Task<bool> DeleteFieldValueAsync(int fieldValueId, int organizationId, string userId)
    {
        var fieldValue = await _context.DocumentFieldValues
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.Id == fieldValueId && fv.Document.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (fieldValue == null)
            return false;

        // Salvar histórico antes da exclusão
        await SaveFieldValueHistoryAsync(fieldValue, userId, "Deleted");

        _context.DocumentFieldValues.Remove(fieldValue);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Valor do campo {FieldValueId} excluído por usuário {UserId}", fieldValueId, userId);

        return true;
    }

    #endregion

    #region Extração de Campos

    public async Task<IEnumerable<DocumentFieldValueDto>> ExtractFieldsFromDocumentAsync(int documentId, 
        int organizationId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.DocumentType)
            .ThenInclude(dt => dt!.Fields.Where(f => f.Status == "Active"))
            .Include(d => d.Workspace)
            .Where(d => d.Id == documentId && d.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (document == null)
        {
            throw new InvalidOperationException("Documento não encontrado.");
        }

        if (document.DocumentType == null)
        {
            throw new InvalidOperationException("Documento não possui tipo associado.");
        }

        var results = new List<DocumentFieldValueDto>();

        foreach (var field in document.DocumentType.Fields)
        {
            try
            {
                // TODO: Implementar chamada para serviço de IA para extração
                // Por enquanto, criar valores vazios pendentes
                var fieldValue = await CreateFieldValueAsync(
                    documentId, 
                    field.Id, 
                    string.Empty, 
                    0m, 
                    organizationId, 
                    userId
                );

                results.Add(fieldValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao extrair campo {FieldId} do documento {DocumentId}", 
                    field.Id, documentId);
            }
        }

        _logger.LogInformation("Extração de campos iniciada para documento {DocumentId} por usuário {UserId}", 
            documentId, userId);

        return results;
    }

    public async Task<DocumentFieldValueDto> ExtractSingleFieldAsync(int documentId, int documentTypeFieldId, 
        int organizationId, string userId)
    {
        // TODO: Implementar extração de campo específico usando IA
        // Por enquanto, retornar valor vazio
        return await CreateFieldValueAsync(documentId, documentTypeFieldId, string.Empty, 0m, organizationId, userId);
    }

    public async Task<IEnumerable<DocumentFieldValueDto>> ReextractFieldsAsync(int documentId, 
        IEnumerable<int> fieldIds, int organizationId, string userId)
    {
        var results = new List<DocumentFieldValueDto>();

        foreach (var fieldId in fieldIds)
        {
            try
            {
                // Buscar valor existente
                var existingValue = await _context.DocumentFieldValues
                    .Where(fv => fv.DocumentId == documentId && fv.DocumentTypeFieldId == fieldId)
                    .FirstOrDefaultAsync();

                if (existingValue != null)
                {
                    // Salvar histórico
                    await SaveFieldValueHistoryAsync(existingValue, userId, "Re-extraction");

                    // TODO: Implementar re-extração usando IA
                    existingValue.ExtractedValue = string.Empty;
                    existingValue.ConfidenceScore = 0m;
                    existingValue.ValidationStatus = ValidationStatus.Pending.ToString();
                    existingValue.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    results.Add(MapToDto(existingValue));
                }
                else
                {
                    // Criar novo valor
                    var newValue = await CreateFieldValueAsync(documentId, fieldId, string.Empty, 0m, organizationId, userId);
                    results.Add(newValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao re-extrair campo {FieldId} do documento {DocumentId}", 
                    fieldId, documentId);
            }
        }

        return results;
    }

    #endregion

    #region Validação de Campos

    public async Task<DocumentFieldValueDto> ValidateFieldValueAsync(int fieldValueId, ValidationStatus status, 
        string? correctedValue, int organizationId, string userId)
    {
        var fieldValue = await _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.Id == fieldValueId && fv.Document.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (fieldValue == null)
        {
            throw new InvalidOperationException("Valor de campo não encontrado.");
        }

        // Salvar histórico antes da validação
        await SaveFieldValueHistoryAsync(fieldValue, userId, $"Validation: {status}");

        fieldValue.ValidationStatus = status.ToString();
        fieldValue.CorrectedValue = correctedValue;
        fieldValue.ValidatedByUserId = userId;
        fieldValue.ValidatedAt = DateTime.UtcNow;
        fieldValue.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Campo {FieldValueId} validado como {Status} por usuário {UserId}", 
            fieldValueId, status, userId);

        return MapToDto(fieldValue);
    }

    public async Task<IEnumerable<DocumentFieldValueDto>> ValidateMultipleFieldValuesAsync(
        IEnumerable<(int FieldValueId, ValidationStatus Status, string? CorrectedValue)> validations, 
        int organizationId, string userId)
    {
        var results = new List<DocumentFieldValueDto>();

        foreach (var (fieldValueId, status, correctedValue) in validations)
        {
            try
            {
                var result = await ValidateFieldValueAsync(fieldValueId, status, correctedValue, organizationId, userId);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar campo {FieldValueId}", fieldValueId);
            }
        }

        return results;
    }

    public async Task<ValidationResult> ValidateFieldValueFormatAsync(int fieldValueId, int organizationId)
    {
        var fieldValue = await _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.Id == fieldValueId && fv.Document.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (fieldValue == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = ["Valor de campo não encontrado."]
            };
        }

        var result = new ValidationResult { IsValid = true };
        var value = fieldValue.CorrectedValue ?? fieldValue.ExtractedValue;

        if (string.IsNullOrEmpty(value))
        {
            if (fieldValue.DocumentTypeField.IsRequired)
            {
                result.IsValid = false;
                result.Errors.Add("Campo obrigatório não pode estar vazio.");
            }
            return result;
        }

        // Validar regex se definido
        if (!string.IsNullOrEmpty(fieldValue.DocumentTypeField.ValidationRegex))
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(fieldValue.DocumentTypeField.ValidationRegex);
                if (!regex.IsMatch(value))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Valor não atende ao padrão esperado: {fieldValue.DocumentTypeField.ValidationRegex}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar regex para campo {FieldId}", fieldValue.DocumentTypeFieldId);
                result.IsValid = false;
                result.Errors.Add("Erro na validação do formato.");
            }
        }

        // Validar tipo de campo
        if (Enum.TryParse<DocumentFieldType>(fieldValue.DocumentTypeField.FieldType, out var fieldType))
        {
            result = ValidateFieldTypeFormat(fieldType, value, result);
        }
        else
        {
            result.Errors.Add($"Tipo de campo inválido: {fieldValue.DocumentTypeField.FieldType}");
        }

        return result;
    }

    #endregion

    #region Correção de Campos

    public async Task<DocumentFieldValueDto> CorrectFieldValueAsync(int fieldValueId, string correctedValue, 
        int organizationId, string userId)
    {
        var fieldValue = await _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.Id == fieldValueId && fv.Document.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (fieldValue == null)
        {
            throw new InvalidOperationException("Valor de campo não encontrado.");
        }

        // Salvar histórico antes da correção
        await SaveFieldValueHistoryAsync(fieldValue, userId, "Manual Correction");

        fieldValue.CorrectedValue = correctedValue;
        fieldValue.ValidationStatus = ValidationStatus.Valid.ToString();
        fieldValue.ValidatedByUserId = userId;
        fieldValue.ValidatedAt = DateTime.UtcNow;
        fieldValue.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Campo {FieldValueId} corrigido por usuário {UserId}", fieldValueId, userId);

        return MapToDto(fieldValue);
    }

    public async Task<IEnumerable<DocumentFieldValueDto>> CorrectMultipleFieldValuesAsync(
        IEnumerable<(int FieldValueId, string CorrectedValue)> corrections, 
        int organizationId, string userId)
    {
        var results = new List<DocumentFieldValueDto>();

        foreach (var (fieldValueId, correctedValue) in corrections)
        {
            try
            {
                var result = await CorrectFieldValueAsync(fieldValueId, correctedValue, organizationId, userId);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao corrigir campo {FieldValueId}", fieldValueId);
            }
        }

        return results;
    }

    #endregion

    #region Histórico de Campos

    public async Task<IEnumerable<DocumentFieldValueHistoryDto>> GetFieldValueHistoryAsync(int fieldValueId, 
        int organizationId)
    {
        var history = await _context.DocumentFieldValueHistories
            .Include(h => h.DocumentFieldValue)
            .ThenInclude(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(h => h.DocumentFieldValueId == fieldValueId && 
                       h.DocumentFieldValue.Document.Workspace.OrganizationId == organizationId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return history.Select(h => new DocumentFieldValueHistoryDto
        {
            Id = h.Id,
            DocumentFieldValueId = h.DocumentFieldValueId,
            PreviousValue = h.PreviousValue,
            NewValue = h.NewValue,
            ChangeType = h.ChangeType,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByUserName = string.Empty, // TODO: Buscar nome do usuário            
        });
    }

    public async Task<IEnumerable<DocumentFieldValueHistoryDto>> GetDocumentFieldHistoryAsync(int documentId, 
        int organizationId)
    {
        var history = await _context.DocumentFieldValueHistories
            .Include(h => h.DocumentFieldValue)
            .ThenInclude(fv => fv.DocumentTypeField)
            .Include(h => h.DocumentFieldValue)
            .ThenInclude(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(h => h.DocumentFieldValue.DocumentId == documentId && 
                       h.DocumentFieldValue.Document.Workspace.OrganizationId == organizationId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return history.Select(h => new DocumentFieldValueHistoryDto
        {
            Id = h.Id,
            DocumentFieldValueId = h.DocumentFieldValueId,
            FieldName = h.DocumentFieldValue.DocumentTypeField?.FieldName ?? string.Empty,
            PreviousValue = h.PreviousValue,
            NewValue = h.NewValue,
            ChangeType = h.ChangeType,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByUserName = string.Empty // TODO: Buscar nome do usuário
        });
    }

    #endregion

    #region Consultas e Relatórios

    public async Task<IEnumerable<DocumentFieldValueDto>> GetPendingValidationsAsync(int organizationId, 
        int? documentTypeId = null)
    {
        var query = _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.ValidationStatus == ValidationStatus.Pending.ToString() && 
                        fv.Document.Workspace.OrganizationId == organizationId);

        if (documentTypeId.HasValue)
        {
            query = query.Where(fv => fv.Document.DocumentTypeId == documentTypeId.Value);
        }

        var fieldValues = await query
            .OrderBy(fv => fv.CreatedAt)
            .ToListAsync();

        return fieldValues.Select(MapToDto);
    }

    public async Task<Dictionary<string, object>> GetFieldValueStatisticsAsync(int documentTypeFieldId, 
        int organizationId)
    {
        var fieldValues = await _context.DocumentFieldValues
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.DocumentTypeFieldId == documentTypeFieldId && 
                        fv.Document.Workspace.OrganizationId == organizationId)
            .ToListAsync();

        var totalValues = fieldValues.Count;
        var validatedValues = fieldValues.Count(fv => fv.ValidationStatus == ValidationStatus.Valid.ToString());
        var invalidValues = fieldValues.Count(fv => fv.ValidationStatus == ValidationStatus.Invalid.ToString());
        var pendingValues = fieldValues.Count(fv => fv.ValidationStatus == ValidationStatus.Pending.ToString());
        var averageConfidence = fieldValues.Any() ? fieldValues.Average(fv => (decimal)fv.ConfidenceScore) : 0m;
        var correctedValues = fieldValues.Count(fv => !string.IsNullOrEmpty(fv.CorrectedValue));

        return new Dictionary<string, object>
        {
            ["totalValues"] = totalValues,
            ["validatedValues"] = validatedValues,
            ["invalidValues"] = invalidValues,
            ["pendingValues"] = pendingValues,
            ["correctedValues"] = correctedValues,
            ["averageConfidence"] = Math.Round(averageConfidence, 2),
            ["validationRate"] = totalValues > 0 ? Math.Round((double)validatedValues / totalValues * 100, 2) : 0,
            ["correctionRate"] = totalValues > 0 ? Math.Round((double)correctedValues / totalValues * 100, 2) : 0
        };
    }

    public async Task<IEnumerable<DocumentFieldValueDto>> GetLowConfidenceValuesAsync(int organizationId, 
        decimal maxConfidence = 0.7m, int? documentTypeId = null)
    {
        var query = _context.DocumentFieldValues
            .Include(fv => fv.DocumentTypeField)
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.ConfidenceScore <= maxConfidence && 
                        fv.Document.Workspace.OrganizationId == organizationId);

        if (documentTypeId.HasValue)
        {
            query = query.Where(fv => fv.Document.DocumentTypeId == documentTypeId.Value);
        }

        var fieldValues = await query
            .OrderBy(fv => fv.ConfidenceScore)
            .ToListAsync();

        return fieldValues.Select(MapToDto);
    }

    #endregion

    #region Métodos Auxiliares

    private async Task SaveFieldValueHistoryAsync(DocumentFieldValue fieldValue, string userId, string changeType)
    {
        var history = new DocumentFieldValueHistory
        {
            DocumentFieldValueId = fieldValue.Id,
            PreviousValue = fieldValue.ExtractedValue,
            NewValue = fieldValue.CorrectedValue ?? fieldValue.ExtractedValue,
            ChangeType = changeType,
            ChangedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.DocumentFieldValueHistories.Add(history);
    }

    private static ValidationResult ValidateFieldTypeFormat(DocumentFieldType fieldType, string value, 
        ValidationResult result)
    {
        try
        {
            switch (fieldType)
            {
                case DocumentFieldType.NUMBER:
                    if (!int.TryParse(value, out _))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Valor deve ser um número inteiro.");
                    }
                    break;

                case DocumentFieldType.DECIMAL:
                case DocumentFieldType.CURRENCY:
                    if (!decimal.TryParse(value, out _))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Valor deve ser um número decimal.");
                    }
                    break;

                case DocumentFieldType.DATE:
                    if (!DateTime.TryParse(value, out _))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Valor deve ser uma data válida.");
                    }
                    break;

                case DocumentFieldType.DATETIME:
                    if (!DateTime.TryParse(value, out _))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Valor deve ser uma data e hora válidas.");
                    }
                    break;

                case DocumentFieldType.EMAIL:
                    if (!IsValidEmail(value))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Valor deve ser um email válido.");
                    }
                    break;

                case DocumentFieldType.URL:
                    if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Valor deve ser uma URL válida.");
                    }
                    break;

                case DocumentFieldType.BOOLEAN:
                    if (!bool.Parse(value) && !IsValidBooleanText(value))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Valor deve ser verdadeiro/falso, sim/não, ou 1/0.");
                    }
                    break;

                case DocumentFieldType.CNPJ:
                    if (!IsValidCNPJ(value))
                    {
                        result.IsValid = false;
                        result.Errors.Add("CNPJ inválido.");
                    }
                    break;

                case DocumentFieldType.CPF:
                    if (!IsValidCPF(value))
                    {
                        result.IsValid = false;
                        result.Errors.Add("CPF inválido.");
                    }
                    break;
            }
        }
        catch (Exception)
        {
            result.IsValid = false;
            result.Errors.Add("Erro na validação do formato do campo.");
        }

        return result;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidBooleanText(string value)
    {
        var lowerValue = value.ToLowerInvariant();
        return lowerValue is "sim" or "não" or "nao" or "yes" or "no" or "1" or "0" or "true" or "false";
    }

    private static bool IsValidCNPJ(string cnpj)
    {
        // Remover caracteres não numéricos
        cnpj = System.Text.RegularExpressions.Regex.Replace(cnpj, @"[^\d]", "");

        if (cnpj.Length != 14)
            return false;

        // Verificar se todos os dígitos são iguais
        if (cnpj.All(c => c == cnpj[0]))
            return false;

        // Calcular dígitos verificadores
        var multiplicadores1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicadores2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var soma = 0;
        for (int i = 0; i < 12; i++)
        {
            soma += int.Parse(cnpj[i].ToString()) * multiplicadores1[i];
        }

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (int.Parse(cnpj[12].ToString()) != digito1)
            return false;

        soma = 0;
        for (int i = 0; i < 13; i++)
        {
            soma += int.Parse(cnpj[i].ToString()) * multiplicadores2[i];
        }

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return int.Parse(cnpj[13].ToString()) == digito2;
    }

    private static bool IsValidCPF(string cpf)
    {
        // Remover caracteres não numéricos
        cpf = System.Text.RegularExpressions.Regex.Replace(cpf, @"[^\d]", "");

        if (cpf.Length != 11)
            return false;

        // Verificar se todos os dígitos são iguais
        if (cpf.All(c => c == cpf[0]))
            return false;

        // Calcular primeiro dígito verificador
        var soma = 0;
        for (int i = 0; i < 9; i++)
        {
            soma += int.Parse(cpf[i].ToString()) * (10 - i);
        }

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (int.Parse(cpf[9].ToString()) != digito1)
            return false;

        // Calcular segundo dígito verificador
        soma = 0;
        for (int i = 0; i < 10; i++)
        {
            soma += int.Parse(cpf[i].ToString()) * (11 - i);
        }

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return int.Parse(cpf[10].ToString()) == digito2;
    }

    private static DocumentFieldValueDto MapToDto(DocumentFieldValue fieldValue)
    {
        return new DocumentFieldValueDto
        {
            Id = fieldValue.Id,
            DocumentId = fieldValue.DocumentId,
            DocumentTypeFieldId = fieldValue.DocumentTypeFieldId,
            FieldName = fieldValue.DocumentTypeField?.FieldName ?? string.Empty,
            FieldType = fieldValue.DocumentTypeField?.FieldType ?? string.Empty,
            ExtractedValue = fieldValue.ExtractedValue,
            ConfidenceScore = fieldValue.ConfidenceScore,
            ContextExcerpt = fieldValue.ContextExcerpt,
            StartPosition = fieldValue.StartPosition,
            EndPosition = fieldValue.EndPosition,
            ValidationStatus = fieldValue.ValidationStatus.ToString(),
            ValidatedByUserId = fieldValue.ValidatedByUserId,
            ValidatedByUserName = string.Empty, // TODO: Buscar nome do usuário
            ValidatedAt = fieldValue.ValidatedAt,
            CorrectedValue = fieldValue.CorrectedValue,
            CreatedAt = fieldValue.CreatedAt,
            UpdatedAt = fieldValue.UpdatedAt
        };
    }

    #endregion

    #region Métodos da Interface Faltantes

    public async Task<DocumentTypeOperationResponseDto> ExtractFieldValuesAsync(int documentId, int organizationId, string userId)
    {
        // TODO: Implementar extração automática de campos usando IA
        _logger.LogInformation("Extração automática de campos solicitada para documento {DocumentId} por usuário {UserId}", documentId, userId);
        
        await Task.Delay(1);
        return new DocumentTypeOperationResponseDto
        {
            Success = true,
            Message = "Extração de campos não implementada"
        };
    }

    public async Task<DocumentTypeOperationResponseDto> ExtractSpecificFieldAsync(int documentId, int documentTypeFieldId, int organizationId, string userId)
    {
        // TODO: Implementar extração de campo específico usando IA
        _logger.LogInformation("Extração de campo específico {FieldId} solicitada para documento {DocumentId} por usuário {UserId}", 
            documentTypeFieldId, documentId, userId);
        
        await Task.Delay(1);
        return new DocumentTypeOperationResponseDto
        {
            Success = true,
            Message = "Extração de campo específico não implementada"
        };
    }

    public async Task<DocumentTypeOperationResponseDto> ReextractFieldValuesAsync(int documentId, int organizationId, string userId)
    {
        // TODO: Implementar re-extração de campos usando IA
        _logger.LogInformation("Re-extração de campos solicitada para documento {DocumentId} por usuário {UserId}", documentId, userId);
        
        await Task.Delay(1);
        return new DocumentTypeOperationResponseDto
        {
            Success = true,
            Message = "Reextração de campos não implementada"
        };
    }

    public async Task<IEnumerable<DocumentFieldValueDto>> BulkValidateFieldValuesAsync(
        Dictionary<int, (ValidationStatus status, string? correctedValue)> validations, 
        int organizationId, 
        string userId)
    {
        var results = new List<DocumentFieldValueDto>();
        
        foreach (var validation in validations)
        {
            try
            {
                var result = await ValidateFieldValueAsync(validation.Key, validation.Value.status, 
                    validation.Value.correctedValue, organizationId, userId);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar campo {FieldValueId} em lote", validation.Key);
            }
        }
        
        return results;
    }

    public async Task<Dictionary<string, object>> GetFieldExtractionStatisticsAsync(int documentTypeFieldId, int organizationId)
    {
        return await GetFieldValueStatisticsAsync(documentTypeFieldId, organizationId);
    }

    public async Task<Dictionary<string, object>> GetDocumentExtractionStatisticsAsync(int documentId, int organizationId)
    {
        var fieldValues = await _context.DocumentFieldValues
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Where(fv => fv.DocumentId == documentId && fv.Document.Workspace.OrganizationId == organizationId)
            .ToListAsync();

        var totalFields = fieldValues.Count;
        var extractedFields = fieldValues.Count(fv => !string.IsNullOrEmpty(fv.ExtractedValue));
        var validatedFields = fieldValues.Count(fv => fv.ValidationStatus == ValidationStatus.Valid.ToString());
        var correctedFields = fieldValues.Count(fv => !string.IsNullOrEmpty(fv.CorrectedValue));
        var averageConfidence = fieldValues.Any() ? fieldValues.Average(fv => (decimal)fv.ConfidenceScore) : 0m;

        return new Dictionary<string, object>
        {
            ["totalFields"] = totalFields,
            ["extractedFields"] = extractedFields,
            ["validatedFields"] = validatedFields,
            ["correctedFields"] = correctedFields,
            ["averageConfidence"] = Math.Round(averageConfidence, 2),
            ["extractionRate"] = totalFields > 0 ? Math.Round((double)extractedFields / totalFields * 100, 2) : 0,
            ["validationRate"] = totalFields > 0 ? Math.Round((double)validatedFields / totalFields * 100, 2) : 0,
            ["correctionRate"] = totalFields > 0 ? Math.Round((double)correctedFields / totalFields * 100, 2) : 0
        };
    }

    #endregion
}