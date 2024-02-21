using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Data
{
    public class ConnectionDB : DbContext
    {
        public ConnectionDB(DbContextOptions<ConnectionDB> options) : base(options)
        {
        } 

        public DbSet<Usuarios> Usuarios => Set<Usuarios>();
    }
}
