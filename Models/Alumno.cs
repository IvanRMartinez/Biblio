using System;
using System.Collections.Generic;

namespace BiblioAPI.Models;

public partial class Alumno
{
    public string NoControl { get; set; } = null!;

    public int UsuarioId { get; set; }

    public string Genero { get; set; } = null!;

    public string Escuela { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
