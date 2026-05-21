using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace BiblioAPI.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alumno> Alumnos { get; set; }

    public virtual DbSet<Libro> Libros { get; set; }

    public virtual DbSet<Prestamo> Prestamos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("name=ConnectionStrings:DefaultConnection", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.44-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Alumno>(entity =>
        {
            entity.HasKey(e => e.NoControl).HasName("PRIMARY");

            entity.ToTable("alumnos");

            entity.HasIndex(e => e.UsuarioId, "usuario_id").IsUnique();

            entity.Property(e => e.NoControl)
                .HasMaxLength(20)
                .HasColumnName("no_control");
            entity.Property(e => e.Escuela)
                .HasColumnType("enum('Sistemas Computacionales','Gastronomía','Electrónica')")
                .HasColumnName("escuela");
            entity.Property(e => e.Genero)
                .HasColumnType("enum('Masculino','Femenino')")
                .HasColumnName("genero");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithOne(p => p.Alumno)
                .HasForeignKey<Alumno>(d => d.UsuarioId)
                .HasConstraintName("alumnos_ibfk_1");
        });

        modelBuilder.Entity<Libro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("libros");

            entity.HasIndex(e => e.Isbn, "isbn").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnioPublicacion)
                .HasColumnType("year")
                .HasColumnName("anio_publicacion");
            entity.Property(e => e.Autor)
                .HasMaxLength(200)
                .HasColumnName("autor");
            entity.Property(e => e.Categoria)
                .HasMaxLength(100)
                .HasColumnName("categoria");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Editorial)
                .HasMaxLength(200)
                .HasColumnName("editorial");
            entity.Property(e => e.Ejemplares).HasColumnName("ejemplares");
            entity.Property(e => e.Idioma)
                .HasMaxLength(50)
                .HasColumnName("idioma");
            entity.Property(e => e.Isbn)
                .HasMaxLength(20)
                .HasColumnName("isbn");
            entity.Property(e => e.Paginas).HasColumnName("paginas");
            entity.Property(e => e.PortadaUrl)
                .HasMaxLength(255)
                .HasColumnName("portada_url");
            entity.Property(e => e.Resumen)
                .HasColumnType("text")
                .HasColumnName("resumen");
            entity.Property(e => e.Titulo)
                .HasMaxLength(255)
                .HasColumnName("titulo");
        });

        modelBuilder.Entity<Prestamo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("prestamos");

            entity.HasIndex(e => e.LibroId, "libro_id");

            entity.HasIndex(e => e.UsuarioId, "usuario_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Autor)
                .HasMaxLength(200)
                .HasColumnName("autor");
            entity.Property(e => e.Editorial)
                .HasMaxLength(200)
                .HasColumnName("editorial");
            entity.Property(e => e.Escuela)
                .HasColumnType("enum('Sistemas Computacionales','Gastronomía','Electrónica')")
                .HasColumnName("escuela");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'activo'")
                .HasColumnType("enum('activo','devuelto','vencido','cancelado')")
                .HasColumnName("estado");
            entity.Property(e => e.FechaDevolucion)
                .HasColumnType("datetime")
                .HasColumnName("fecha_devolucion");
            entity.Property(e => e.FechaLimite)
                .HasColumnType("datetime")
                .HasColumnName("fecha_limite");
            entity.Property(e => e.FechaPrestamo)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_prestamo");
            entity.Property(e => e.LibroId).HasColumnName("libro_id");
            entity.Property(e => e.Titulo)
                .HasMaxLength(255)
                .HasColumnName("titulo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Libro).WithMany(p => p.Prestamos)
                .HasForeignKey(d => d.LibroId)
                .HasConstraintName("prestamos_ibfk_2");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Prestamos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("prestamos_ibfk_1");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Correo, "correo").IsUnique();

            entity.HasIndex(e => e.Username, "username").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(150)
                .HasColumnName("nombre_completo");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Rol)
                .HasColumnType("enum('alumno','maestro','admin')")
                .HasColumnName("rol");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
