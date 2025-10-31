using EBEntities;
using Microsoft.EntityFrameworkCore;

namespace EBData
{
    public class EBDbContext : DbContext
    {
        public EBDbContext(DbContextOptions<EBDbContext> options) : base(options) { }

        public DbSet<CatalogoEstado> CatalogoEstado { get; set; }
        public DbSet<EstatusReferencia> EstatusReferencia { get; set; }
        public DbSet<CatalogoPais> CatalogoPais { get; set; }
        public DbSet<Empresa> Empresa { get; set; }
        public DbSet<FuenteOrigen> FuenteOrigen { get; set; }
        public DbSet<Producto> Producto { get; set; }
        public DbSet<Referido> Referido { get; set; }
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<PasswordRecovery> PasswordRecovery { get; set; }
        public DbSet<SeguimientoReferido> SeguimientoReferido { get; set; }
        public DbSet<BancoUsuario> BancoUsuario { get; set; }
        public DbSet<Grupo> Grupo { get; set; }
        public DbSet<EmpresaGrupo> EmpresaGrupo { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<UsuarioEmpresa> UsuarioEmpresa { get; set; }
        public DbSet<ProductoComision> ProductoComision { get; set; }
        public DbSet<Periodo> Periodo { get; set; }
        public DbSet<Cupones> Cupones { get; set; }
        public DbSet<CatBanco> CatBanco { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Empresa).WithMany().HasForeignKey(p => p.EmpresaID);

            modelBuilder.Entity<Referido>()
                .HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioID);
            modelBuilder.Entity<Referido>()
                .HasOne(p => p.Producto).WithMany().HasForeignKey(p => p.ProductoID);
            modelBuilder.Entity<Referido>()
                .HasOne(p => p.EstatusReferencia).WithMany().HasForeignKey(p => p.EstatusReferenciaID);

            modelBuilder.Entity<Usuario>()
                .HasOne(p => p.CatalogoPais).WithMany().HasForeignKey(p => p.CatalogoPaisID);
            modelBuilder.Entity<Usuario>()
                .HasOne(p => p.CatalogoEstado).WithMany().HasForeignKey(p => p.CatalogoEstadoID);
            modelBuilder.Entity<Usuario>()
                .HasOne(p => p.FuenteOrigen).WithMany().HasForeignKey(p => p.FuenteOrigenID);
            modelBuilder.Entity<Usuario>()
                .HasOne(p => p.Roles).WithMany().HasForeignKey(p => p.RolesID);
            modelBuilder.Entity<Usuario>()
                .HasOne(p => p.Grupo).WithMany().HasForeignKey(p => p.GrupoID);

            modelBuilder.Entity<PasswordRecovery>()
                .HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioID);

            modelBuilder.Entity<SeguimientoReferido>()
                .HasOne(p => p.Referido).WithMany().HasForeignKey(p => p.ReferidoID);

            // 🔑 Para BancoUsuario: usar Id como PK y evitar doble mapeo de Base.ID
            modelBuilder.Entity<BancoUsuario>(e =>
            {
                e.HasKey(x => x.Id);
                e.Ignore(x => x.ID);     // MUY IMPORTANTE
            });

            base.OnModelCreating(modelBuilder);
        }

    }
}