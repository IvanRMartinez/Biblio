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

        // ==========================================
        // 1. ENDPOINT: SOLICITAR UN PRÉSTAMO
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CrearPrestamo([FromBody] SolicitudPrestamoDTO peticion)
        {
            // CONTROL DE ERRORES GLOBAL: Si algo truena en la BD, el try-catch lo atrapa 
            // y te dice exactamente qué pasó en lugar de dar un simple Error 500 genérico.
            try
            {
                // Paso A: Validar que los datos de entrada no vengan en cero o negativos
                if (peticion.UsuarioId <= 0 || peticion.LibroId <= 0)
                {
                    return BadRequest(new { mensaje = "El ID de usuario y el ID de libro deben ser números válidos mayores a 0." });
                }

                // Paso B: Buscar el libro en MySQL para comprobar existencia y traer sus datos reales
                var libro = await _context.Set<Libro>().FindAsync(peticion.LibroId);
                if (libro == null)
                {
                    return NotFound(new { mensaje = $"Error: El libro con ID {peticion.LibroId} no existe en el catálogo." });
                }

                // Paso C: VALIDACIÓN DE INVENTARIO (Regla de la Maestra)
                if (libro.Ejemplares <= 0)
                {
                    return BadRequest(new { mensaje = $"Inventario insuficiente. El libro '{libro.Titulo}' tiene 0 ejemplares disponibles." });
                }

                // Paso D: Restar un ejemplar del stock
                libro.Ejemplares -= 1;

                // Paso E: Crear la entidad física 'Prestamo' resolviendo el misterio de los campos obligatorios.
                // Como no modificamos el modelo original, le inyectamos los datos del libro que acabamos de buscar.
                // Así, si la BD exige Titulo y Autor, aquí ya van llenos con la información correcta.
                var nuevoPrestamo = new Prestamo
                {
                    UsuarioId = peticion.UsuarioId,
                    LibroId = peticion.LibroId,
                    FechaPrestamo = DateTime.Now,
                    Estado = "Activo", // Asignamos el Estado obligatorio que pedía el validador

                    // SOLUCIÓN AL DESFASE: Rellenamos estos campos por si el modelo original los exige como no-nulos
                    Titulo = libro.Titulo,
                    Autor = libro.Autor
                };

                // Paso F: Guardar cambios en la base de datos
                _context.Set<Prestamo>().Add(nuevoPrestamo);
                await _context.SaveChangesAsync();

                // Respuesta exitosa (200 OK)
                return Ok(new
                {
                    mensaje = "¡Préstamo registrado con éxito en MySQL!",
                    libroPrestado = libro.Titulo,
                    ejemplaresRestantes = libro.Ejemplares
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Este bloque atrapa errores específicos de MySQL (Llaves foráneas rotas, columnas faltantes)
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
                // Atrapa cualquier otro tipo de error inesperado en el código C#
                return StatusCode(500, new
                {
                    error = "Error interno del servidor (C#)",
                    detalles = ex.Message
                });
            }
        }

        // ==========================================
        // 2. ENDPOINT: REPORTE MENSUAL (Para la Maestra)
        // ==========================================
        [HttpGet("reporte-mensual")]
        public async Task<IActionResult> GetReporteMensual()
        {
            try
            {
                var prestamosEnMemoria = await _context.Set<Prestamo>().ToListAsync();

                if (!prestamosEnMemoria.Any())
                {
                    return Ok(new List<object>()); // Si no hay préstamos, regresa una lista vacía limpia
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

        // ==========================================
        // 3. ENDPOINT: REPORTE POR PERIODO (Gráficos)
        // ==========================================
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

        // ==========================================
        // 4. ENDPOINT: DEVOLVER UN LIBRO
        // ==========================================
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
    }

    // ==========================================
    // CLASE DTO: OBJETO DE TRANSFERENCIA DE DATOS
    // ==========================================
    public class SolicitudPrestamoDTO
    {
        public int UsuarioId { get; set; }
        public int LibroId { get; set; }
    }
}