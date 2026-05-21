using System;
using System.Collections.Generic;

namespace BiblioAPI.Models;

public partial class Libro
{
    public int Id { get; set; }

    public string Titulo { get; set; } = null!;

    public string Autor { get; set; } = null!;

    public short? AnioPublicacion { get; set; }

    public string? Editorial { get; set; }

    public string? Categoria { get; set; }

    public string? Isbn { get; set; }

    public string? Idioma { get; set; }

    public int? Paginas { get; set; }

    public string? Resumen { get; set; }

    public string? PortadaUrl { get; set; }

    public int Ejemplares { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
