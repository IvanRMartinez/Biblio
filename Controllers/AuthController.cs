using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiblioAPI.Models;

namespace BiblioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] BiblioAPI.Models.LoginRequest request)
        {
            // 1. Validamos que el request traiga datos (aquí usamos las propiedades de tu LoginRequest)
            if (string.IsNullOrEmpty(request.Correo) || string.IsNullOrEmpty(request.Contrasena))
            {
                return BadRequest(new { mensaje = "El correo y la contraseña son obligatorios." });
            }

            // 2. Buscamos usando los nombres EXACTOS (Correo y PasswordHash)
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == request.Correo && u.PasswordHash == request.Contrasena);

            // 3. Si no coincide ningún usuario
            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
            }

            // 4. Si todo está bien, respondemos con los datos reales de su MySQL
            return Ok(new
            {
                id = usuario.Id,
                username = usuario.Username,
                nombre = usuario.NombreCompleto, // ?? Cambiado a NombreCompleto
                correo = usuario.Correo,
                rol = usuario.Rol // Esto devolverá "alumno" o "admin"
            });
        }
    }
}
