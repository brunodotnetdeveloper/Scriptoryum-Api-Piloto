using Microsoft.AspNetCore.Identity;

namespace Scriptoryum.Api.Domain.Entities;

public class ApplicationUser : IdentityUser<string>
{
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}