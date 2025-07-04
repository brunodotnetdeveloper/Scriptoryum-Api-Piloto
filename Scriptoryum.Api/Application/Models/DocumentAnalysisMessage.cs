namespace Scriptoryum.Api.Application.Models;

public record DocumentAnalysisMessage(
    int DocumentId,
    string UserId,
    DateTime QueuedAt
);