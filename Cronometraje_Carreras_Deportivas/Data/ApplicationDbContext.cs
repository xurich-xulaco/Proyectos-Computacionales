using Microsoft.EntityFrameworkCore;
using static Cronometraje_Carreras_Deportivas.Controllers.HomeController;

namespace Cronometraje_Carreras_Deportivas.Data // Cambia "YourNamespace" por el namespace real de tu proyecto
{
    public class CronometrajeContext : DbContext
    {
        public CronometrajeContext(DbContextOptions<CronometrajeContext> options)
            : base(options)
        {
        }

        // Define propiedades DbSet para cada tabla
        public DbSet<Carrera> CARRERA { get; set; }
        public DbSet<Categoria> CATEGORIA { get; set; }
        public DbSet<Corredor> CORREDOR { get; set; }
        public DbSet<Tiempo> TIEMPO { get; set; }
        public DbSet<Vincula_participante> Vincula_participante { get; set; }
        public DbSet<Carr_cat> CARR_Cat { get; set; }
    }
}
