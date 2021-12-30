using Microsoft.EntityFrameworkCore;

namespace Bios
{
    public class UserContext : DbContext 
    {
        public UserContext(DbContextOptions<UserContext> options):base(options)
        {

        }

        public DbSet<User> Users { get; set; }
    }

    public class User
    {
        public int Id { get; set; }

        public string? EmployeeNumber { get; set; }

        public string? PersonId { get; set; }

        public string? PersonGroupId { get; set; }
    }
}
