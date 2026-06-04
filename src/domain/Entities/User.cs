namespace domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty; 
    public string? PasswordHash { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
