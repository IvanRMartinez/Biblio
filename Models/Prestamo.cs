using System;
using System.Collections.Generic;

namespace BiblioAPI.Models;

public partial class Prestamo
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int LibroId { get; set; }

    public string Titulo { get; set; } = null!;

    public string Autor { get; set; } = null!;

    public string? Editorial { get; set; }

    public string? Escuela { get; set; }

    public DateTime FechaPrestamo { get; set; }

    public DateTime? FechaLimite { get; set; }

    public DateTime? FechaDevolucion { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Libro Libro { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
