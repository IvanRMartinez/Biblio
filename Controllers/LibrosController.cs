using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiblioAPI.Models;

namespace BiblioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LibrosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LibrosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Libro>>> GetLibros()
        {
            // Retorna la lista completa de libros directo de MySQL
            return await _context.Libros.ToListAsync();
            
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Libro>> GetLibro(int id)
        {
            var libro = await _context.Set<Libro>().FindAsync(id);

            if (libro == null)
            {
                return NotFound(new { mensaje = $"El libro con ID {id} no existe." });
            }

            return libro;
        }

        [HttpPost]
        public async Task<ActionResult<Libro>> PostLibro([FromBody] Libro libro)
        {
            // Validaciones básicas antes de insertar en MySQL
            if (string.IsNullOrEmpty(libro.Titulo) || string.IsNullOrEmpty(libro.Autor))
            {
                return BadRequest(new { mensaje = "El título y el autor son obligatorios." });
            }

            libro.CreatedAt = DateTime.Now; // Asignamos la fecha de creación automáticamente

            _context.Set<Libro>().Add(libro);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLibro), new { id = libro.Id }, libro);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutLibro(int id, [FromBody] Libro libroActualizado)
        {
            if (id != libroActualizado.Id)
            {
                return BadRequest(new { mensaje = "El ID del libro no coincide con el enviado en el cuerpo." });
            }

            // Buscamos el libro original en la base de datos
            var libroExistente = await _context.Set<Libro>().FindAsync(id);
            if (libroExistente == null)
            {
                return NotFound(new { mensaje = "Libro no encontrado para actualizar." });
            }

            // Actualizamos los campos uno por uno para no romper referencias
            libroExistente.Titulo = libroActualizado.Titulo;
            libroExistente.Autor = libroActualizado.Autor;
            libroExistente.AnioPublicacion = libroActualizado.AnioPublicacion;
            libroExistente.Editorial = libroActualizado.Editorial;
            libroExistente.Categoria = libroActualizado.Categoria;
            libroExistente.Isbn = libroActualizado.Isbn;
            libroExistente.Idioma = libroActualizado.Idioma;
            libroExistente.Paginas = libroActualizado.Paginas;
            libroExistente.Resumen = libroActualizado.Resumen;
            libroExistente.PortadaUrl = libroActualizado.PortadaUrl;
            libroExistente.Ejemplares = libroActualizado.Ejemplares; // Clave para las estadísticas de la maestra

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { mensaje = "Error de concurrencia al actualizar el libro." });
            }

            return Ok(new { mensaje = "Libro actualizado con éxito.", libro = libroExistente });
        }

    }
}
