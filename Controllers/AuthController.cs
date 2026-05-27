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
            
            if (string.IsNullOrEmpty(request.Correo) || string.IsNullOrEmpty(request.Contrasena))
            {
                return BadRequest(new { mensaje = "El correo y la contraseña son obligatorios." });
            }

            
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == request.Correo && u.PasswordHash == request.Contrasena);

            
            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
            }

            
            return Ok(new
            {
                id = usuario.Id,
                username = usuario.Username,
                nombre = usuario.NombreCompleto,
                correo = usuario.Correo,
                rol = usuario.Rol
            });
        }
    }
}
