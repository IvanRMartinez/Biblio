using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiblioAPI.Models;

namespace BiblioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Reportes/morosidad
        [HttpGet("morosidad")]
        public async Task<IActionResult> GetReporteMorosidad()
        {
            try
            {
                var hoy = DateTime.Today;

                var prestamosVencidos = await _context.Prestamos
                    .Where(p =>
                        p.FechaDevolucion == null &&
                        p.FechaLimite != null &&
                        p.FechaLimite.Value.Date < hoy &&
                        p.Estado != "devuelto" &&
                        p.Estado != "cancelado"
                    )
                    .Select(p => new
                    {
                        nombreUsuario = p.Usuario.NombreCompleto,
                        correo = p.Usuario.Correo,
                        tituloLibro = p.Titulo,
                        fechaLimite = p.FechaLimite.Value
                    })
                    .ToListAsync();

                var reporte = prestamosVencidos
                    .Select(p => new
                    {
                        p.nombreUsuario,
                        p.correo,
                        p.tituloLibro,
                        p.fechaLimite,
                        diasRetraso = (hoy - p.fechaLimite.Date).Days
                    })
                    .OrderByDescending(p => p.diasRetraso)
                    .ToList();

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al generar el reporte de morosidad",
                    detalles = ex.Message
                });
            }
        }

        // GET: api/Reportes/libros-rotacion
        [HttpGet("libros-rotacion")]
        public async Task<IActionResult> GetReporteLibrosRotacion()
        {
            try
            {
                var reporte = await _context.Libros
                    .Select(l => new
                    {
                        tituloLibro = l.Titulo,
                        autor = l.Autor,
                        totalPrestamosRealizados = l.Prestamos
                            .Count(p => p.Estado != "cancelado"),
                        estadoActual = l.Ejemplares > 0 ? "Disponible" : "No disponible",
                        ejemplaresDisponibles = l.Ejemplares
                    })
                    .OrderByDescending(l => l.totalPrestamosRealizados)
                    .ThenBy(l => l.tituloLibro)
                    .ToListAsync();

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al generar el reporte de libros fantasma o alta rotación",
                    detalles = ex.Message
                });
            }
        }

        // GET: api/Reportes/demanda-perfil
        [HttpGet("demanda-perfil")]
        public async Task<IActionResult> GetReporteDemandaPerfil()
        {
            try
            {
                var reporte = await _context.Prestamos
                    .Where(p => p.Estado != "cancelado")
                    .GroupBy(p => new
                    {
                        escuela = p.Escuela ?? "Sin escuela",
                        categoriaLibro = p.Libro.Categoria ?? "Sin categoría"
                    })
                    .Select(g => new
                    {
                        escuela = g.Key.escuela,
                        categoriaLibro = g.Key.categoriaLibro,
                        totalPrestamos = g.Count()
                    })
                    .OrderBy(r => r.escuela)
                    .ThenByDescending(r => r.totalPrestamos)
                    .ToListAsync();

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al generar el reporte de demanda por perfil académico",
                    detalles = ex.Message
                });
            }
        }
    }
}