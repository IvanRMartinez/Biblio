using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiblioAPI.Models;


namespace BiblioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrestamosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PrestamosController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpPost]
        public async Task<IActionResult> CrearPrestamo([FromBody] SolicitudPrestamoDTO peticion)
        {
            try
            {
                
                if (peticion.UsuarioId <= 0 || string.IsNullOrWhiteSpace(peticion.NombreLibro))
                {
                    return BadRequest(new { mensaje = "El ID de usuario debe ser mayor a 0 y el nombre del libro no puede estar vacío." });
                }
                var usuario = await _context.Set<Usuario>().FindAsync(peticion.UsuarioId);
                if (usuario == null)
                {
                    return NotFound(new { mensaje = $"El alumno con ID {peticion.UsuarioId} no está registrado en el sistema." });
                }

                string tituloBuscado = peticion.NombreLibro.ToLower().Trim();

               
                var libro = await _context.Set<Libro>()
                    .FirstOrDefaultAsync(l => l.Titulo.ToLower().Trim() == tituloBuscado);

                if (libro == null)
                {
                    return NotFound(new { mensaje = $"Error: El libro '{peticion.NombreLibro}' no existe en el catálogo." });
                }

                if (libro.Ejemplares <= 0)
                {
                    return BadRequest(new { mensaje = $"Inventario insuficiente. El libro '{libro.Titulo}' tiene 0 ejemplares disponibles." });
                }

                libro.Ejemplares -= 1;

                var nuevoPrestamo = new Prestamo
                {
                    UsuarioId = peticion.UsuarioId,
                    LibroId = libro.Id, 
                    FechaPrestamo = DateTime.Now,
                    FechaLimite = DateTime.Now.AddDays(7), 
                    Estado = "Activo",
                    Titulo = libro.Titulo,
                    Autor = libro.Autor
                };

                _context.Set<Prestamo>().Add(nuevoPrestamo);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "¡Préstamo registrado con exito.",
                    libroPrestado = libro.Titulo,
                    fechaVencimiento = nuevoPrestamo.FechaLimite, 
                    ejemplaresRestantes = libro.Ejemplares
                });
            }
            catch (DbUpdateException dbEx)
            {
                var errorInterno = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                return StatusCode(500, new
                {
                    error = "Error crítico al guardar en la Base de Datos (MySQL)",
                    detalles = errorInterno,
                    consejo = "Verifica que el usuarioId realmente exista en la tabla Usuarios de tu base de datos."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error interno del servidor (C#)",
                    detalles = ex.Message
                });
            }
        }


        [HttpGet("reporte-mensual")]
        public async Task<IActionResult> GetReporteMensual()
        {
            try
            {
                var prestamosEnMemoria = await _context.Set<Prestamo>().ToListAsync();

                if (!prestamosEnMemoria.Any())
                {
                    return Ok(new List<object>()); 
                }

                var datosConsumo = prestamosEnMemoria
                    .GroupBy(p => new { p.FechaPrestamo.Year, p.FechaPrestamo.Month })
                    .Select(g => new
                    {
                        Anio = g.Key.Year,
                        MesNum = g.Key.Month,
                        MesNombre = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                        CantidadConsumida = g.Count()
                    })
                    .OrderByDescending(r => r.Anio)
                    .ThenByDescending(r => r.MesNum)
                    .ToList();

                return Ok(datosConsumo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al generar el reporte mensual", detalles = ex.Message });
            }
        }

        
        [HttpGet("reporte-periodo")]
        public async Task<IActionResult> GetReportePorPeriodo([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
        {
            try
            {
                if (fechaInicio == DateTime.MinValue || fechaFin == DateTime.MinValue)
                {
                    return BadRequest(new { mensaje = "Debes proporcionar parámetros válidos de fechaInicio y fechaFin." });
                }

                var librosMasRequeridos = await _context.Set<Prestamo>()
                    .Where(p => p.FechaPrestamo >= fechaInicio && p.FechaPrestamo <= fechaFin)
                    .GroupBy(p => p.LibroId)
                    .Select(g => new
                    {
                        LibroId = g.Key,
                        Titulo = _context.Set<Libro>().Where(l => l.Id == g.Key).Select(l => l.Titulo).FirstOrDefault(),
                        VecesRequerido = g.Count()
                    })
                    .OrderByDescending(x => x.VecesRequerido)
                    .ToListAsync();

                return Ok(librosMasRequeridos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al generar el reporte por periodos", detalles = ex.Message });
            }
        }


        [HttpPut("devolver")]
        public async Task<IActionResult> DevolverLibro([FromBody] SolicitudPrestamoDTO peticion)
        {
            try
            {
                // 1. Validamos que no nos manden un texto vacío
                if (string.IsNullOrWhiteSpace(peticion.NombreLibro))
                {
                    return BadRequest(new { mensaje = "El nombre del libro no puede estar vacío." });
                }

                // 2. Preparamos el texto que mandó React (minúsculas y sin espacios a los lados)
                string tituloBuscado = peticion.NombreLibro.ToLower().Trim();

                // 3. Buscamos en MySQL aplicando el mismo filtro a la columna Titulo
                var prestamo = await _context.Set<Prestamo>()
                    .FirstOrDefaultAsync(p => p.UsuarioId == peticion.UsuarioId
                                           && p.Estado == "Activo"
                                           && p.Titulo.ToLower().Trim() == tituloBuscado); 

                if (prestamo == null)
                {
                    return NotFound(new { mensaje = $"No se encontró un préstamo activo del libro '{peticion.NombreLibro}' para el usuario {peticion.UsuarioId}." });
                }

                // 4. Procesamos la devolución normal
                prestamo.Estado = "Devuelto";
                prestamo.FechaDevolucion = DateTime.Now;

                // 5. Devolvemos el ejemplar al inventario usando el ID que el préstamo ya tenía guardado
                var libro = await _context.Set<Libro>().FindAsync(prestamo.LibroId);
                if (libro != null)
                {
                    libro.Ejemplares += 1;
                }

                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Libro devuelto correctamente.." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al procesar la devolución", detalles = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTodosLosPrestamos()
        {
            try
            {
                var prestamos = await _context.Set<Prestamo>()
                    .OrderByDescending(p => p.FechaPrestamo)
                    .Select(p => new
                    {
                        id = p.Id,
                        usuarioId = p.UsuarioId,
                        nombreUsuario = p.Usuario.NombreCompleto, 
                        libroId = p.LibroId,
                        titulo = p.Titulo ?? "Libro sin título",
                        fechaPrestamo = p.FechaPrestamo,
                        fechaVencimiento = p.FechaLimite, 
                        fechaDevolucion = p.FechaDevolucion,
                        estado = p.Estado
                    })
                    .ToListAsync();

                return Ok(prestamos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener la tabla de préstamos", detalles = ex.Message });
            }
        }
    }



    
    
}