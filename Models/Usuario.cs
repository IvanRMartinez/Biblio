using System;
using System;
using System.Collections.Generic;

namespace BiblioAPI.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Alumno? Alumno { get; set; }

    public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
