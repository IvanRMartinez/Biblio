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
                
                if (peticion.UsuarioId <= 0 || peticion.LibroId <= 0)
                {
                    return BadRequest(new { mensaje = "El ID de usuario y el ID de libro deben ser números válidos mayores a 0." });
                }

                
                var libro = await _context.Set<Libro>().FindAsync(peticion.LibroId);
                if (libro == null)
                {
                    return NotFound(new { mensaje = $"Error: El libro con ID {peticion.LibroId} no existe en el catálogo." });
                }

                
                if (libro.Ejemplares <= 0)
                {
                    return BadRequest(new { mensaje = $"Inventario insuficiente. El libro '{libro.Titulo}' tiene 0 ejemplares disponibles." });
                }

                
                libro.Ejemplares -= 1;

                
                var nuevoPrestamo = new Prestamo
                {
                    UsuarioId = peticion.UsuarioId,
                    LibroId = peticion.LibroId,
                    FechaPrestamo = DateTime.Now,
                    Estado = "Activo", 

                    
                    Titulo = libro.Titulo,
                    Autor = libro.Autor
                };

                
                _context.Set<Prestamo>().Add(nuevoPrestamo);
                await _context.SaveChangesAsync();

                
                return Ok(new
                {
                    mensaje = "¡Préstamo registrado con éxito en MySQL!",
                    libroPrestado = libro.Titulo,
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

        
        [HttpPut("devolver/{id}")]
        public async Task<IActionResult> DevolverLibro(int id)
        {
            try
            {
                var prestamo = await _context.Set<Prestamo>().FindAsync(id);
                if (prestamo == null)
                {
                    return NotFound(new { mensaje = "El registro de préstamo no existe." });
                }

                if (prestamo.Estado == "Devuelto")
                {
                    return BadRequest(new { mensaje = "Este libro ya fue devuelto anteriormente." });
                }

                prestamo.Estado = "Devuelto";
                prestamo.FechaDevolucion = DateTime.Now;

                var libro = await _context.Set<Libro>().FindAsync(prestamo.LibroId);
                if (libro != null)
                {
                    libro.Ejemplares += 1;
                }

                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Libro devuelto correctamente. Inventario de ejemplares actualizado." });
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
                        libroId = p.LibroId,
                        titulo = p.Titulo ?? "Libro sin título",
                        fechaPrestamo = p.FechaPrestamo,
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



    
    public class SolicitudPrestamoDTO
    {
        public int UsuarioId { get; set; }
        public int LibroId { get; set; }
    }
}