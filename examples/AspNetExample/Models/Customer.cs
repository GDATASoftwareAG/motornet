using AspNetExample.Persistency;

namespace AspNetExample.Models;

public class Customer : IEntity
{
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public long? Age { get; set; }
}
