using Microsoft.EntityFrameworkCore;
using BetIQ.API.Models;

namespace BetIQ.API.Data
{
    public class BetIQContext : DbContext
    {
        public BetIQContext(DbContextOptions<BetIQContext> options) : base(options)
        {
        }

        public DbSet<NBAMatch> Partidos_NBA { get; set; }
        public DbSet<FutbolMatch> Partidos_Futbol { get; set; }
        public DbSet<TenisMatch> Partidos_Tenis { get; set; }
        public DbSet<PartidoMaestro> Partidos_Maestro { get; set; }
        public DbSet<Equipo> Equipos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Mapeo para la tabla Equipos
            modelBuilder.Entity<Equipo>(entity =>
            {
                entity.ToTable("Equipos");
                entity.HasKey(e => e.NombreEquipo); // Primary Key
            });

            // Mapeo para la tabla Partidos_Maestro
            modelBuilder.Entity<PartidoMaestro>(entity =>
            {
                entity.ToTable("Partidos_Maestro");
                entity.HasKey(e => e.ID_Partido);
                entity.Property(e => e.ID_Partido).ValueGeneratedOnAdd();
            });
            
            // Mapeo para la tabla Partidos_NBA
            modelBuilder.Entity<NBAMatch>(entity =>
            {
                entity.ToTable("Partidos_NBA");
                entity.HasKey(e => e.ID_Partido);
                entity.Property(e => e.ID_Partido).ValueGeneratedNever();
                entity.Property(e => e.EquipoLocal).HasColumnName("Equipo_Local");
                entity.Property(e => e.EquipoVisitante).HasColumnName("Equipo_Visitante");
                entity.Property(e => e.PuntosLocal).HasColumnName("Puntos_Local");
                entity.Property(e => e.PuntosVisitante).HasColumnName("Puntos_Visitante");
                entity.Property(e => e.EficienciaOfensivaLocal).HasColumnName("Eficiencia_Ofensiva_Local");
                entity.Property(e => e.EficienciaDefensivaLocal).HasColumnName("Eficiencia_Defensiva_Local");
                entity.Property(e => e.ELOLocal).HasColumnName("ELO_Local");
                entity.Property(e => e.ELOVisita).HasColumnName("ELO_Visita");
                entity.Property(e => e.PromedioPuntosTotal).HasColumnName("Promedio_Puntos_Total");
            });

            // Mapeo para la tabla Partidos_Futbol
            modelBuilder.Entity<FutbolMatch>(entity =>
            {
                entity.ToTable("Partidos_Futbol");
                entity.HasKey(e => e.ID_Partido);
                entity.Property(e => e.ID_Partido).ValueGeneratedNever();
                entity.Property(e => e.EquipoLocal).HasColumnName("Equipo_Local");
                entity.Property(e => e.EquipoVisitante).HasColumnName("Equipo_Visitante");
                entity.Property(e => e.FuerzaAtaqueLocal).HasColumnName("Fuerza_Ataque_Local");
                entity.Property(e => e.FuerzaDefensaLocal).HasColumnName("Fuerza_Defensa_Local");
                entity.Property(e => e.FuerzaAtaqueVisita).HasColumnName("Fuerza_Ataque_Visita");
                entity.Property(e => e.FuerzaDefensaVisita).HasColumnName("Fuerza_Defensa_Visita");
            });

            // Mapeo para la tabla Partidos_Tenis
            modelBuilder.Entity<TenisMatch>(entity =>
            {
                entity.ToTable("Partidos_Tenis");
                entity.HasKey(e => e.ID_Partido);
                entity.Property(e => e.ID_Partido).ValueGeneratedNever();
                entity.Property(e => e.Jugador1).HasColumnName("Jugador_1");
                entity.Property(e => e.Jugador2).HasColumnName("Jugador_2");
                entity.Property(e => e.EloJugador1).HasColumnName("ELO_Jugador_1");
                entity.Property(e => e.EloJugador2).HasColumnName("ELO_Jugador_2");
            });

            // Configuración de las relaciones uno-a-uno
            modelBuilder.Entity<PartidoMaestro>()
                .HasOne(pm => pm.NbaMatch)
                .WithOne(nba => nba.PartidoMaestro)
                .HasForeignKey<NBAMatch>(nba => nba.ID_Partido);

            modelBuilder.Entity<PartidoMaestro>()
                .HasOne(pm => pm.FutbolMatch)
                .WithOne(f => f.PartidoMaestro)
                .HasForeignKey<FutbolMatch>(f => f.ID_Partido);

            modelBuilder.Entity<PartidoMaestro>()
                .HasOne(pm => pm.TenisMatch)
                .WithOne(t => t.PartidoMaestro)
                .HasForeignKey<TenisMatch>(t => t.ID_Partido);
        }
    }
}
