using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.Data;
using server.Models;
using BCrypt.Net;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace server.Routes
{
    public static class Login
    {
        public static void MapLogin(this WebApplication app)
        {
            var configuration = app.Services.GetRequiredService<IConfiguration>();

            app.MapPost("/api/login", async (ConnectionDB db, HttpRequest req, HttpResponse res) =>
            {
                try
                {
                    // Extraer la información del cuerpo de la solicitud
                    var body = await req.ReadFromJsonAsync<LoginRequest>();
                    if (body == null)
                    {
                        res.StatusCode = 400;
                        await res.WriteAsJsonAsync(new { error = "Cuerpo de solicitud no válido" });
                        return;
                    }

                    // Buscar al usuario en la base de datos por correo electrónico
                    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Mail == body.Mail);
                    if (usuario == null)
                    {
                        res.StatusCode = 404;
                        await res.WriteAsJsonAsync(new { error = "Usuario no encontrado" });
                        return;
                    }

                    // Verificar la contraseña utilizando BCrypt
                    var passwordValida = BCrypt.Net.BCrypt.Verify(body.Password, usuario.Password);
                    if (!passwordValida)
                    {
                        res.StatusCode = 401;
                        await res.WriteAsJsonAsync(new { error = "Credenciales incorrectas" });
                        return;
                    }

                    // Crear un token JWT utilizando la clave secreta de appsettings.json
                    var jwtSecret = configuration["JwtSecret"];
                    if (jwtSecret == null)
                    {
                        // Handle the case where the configuration value is null
                        res.StatusCode = 500;
                        await res.WriteAsJsonAsync(new { error = "JWT secret key is not configured properly" });
                        return;
                    }

                    var key = Encoding.ASCII.GetBytes(jwtSecret);
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.Name, usuario.Mail)
                        }),
                        Expires = DateTime.UtcNow.AddHours(1), // Token válido por 1 hora
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var tokenString = tokenHandler.WriteToken(token);

                    // Devolver el token como respuesta
                    res.StatusCode = 200;
                    await res.WriteAsJsonAsync(new { message = "Inicio de sesión exitoso", token = tokenString });
                }
                catch (Exception error)
                {
                    Console.WriteLine("Error en el endpoint de login: " + error.Message);
                    res.StatusCode = 500;
                    await res.WriteAsJsonAsync(new { error = "Error interno del servidor" });
                }
            });
        }

        // Clase para deserializar la solicitud de inicio de sesión
        public class LoginRequest
        {
            public string Mail { get; set; }
            public string Password { get; set; }
        }
    }
}
