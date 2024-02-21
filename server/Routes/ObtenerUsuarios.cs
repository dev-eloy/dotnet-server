using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.Data;
using server.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace server.Routes
{
    public static class ObtenerUsuarios
    {
        public static void MapObtenerUsuarios(this WebApplication app)
        {
            var configuration = app.Services.GetRequiredService<IConfiguration>();

            app.MapGet("/api/obtenerusuarios", async (ConnectionDB db, HttpRequest req, HttpResponse res) =>
            {
                try
                {
                    // Verificar el JWT
                    var jwtToken = req.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (jwtToken == null)
                    {
                        res.StatusCode = 401;
                        await res.WriteAsJsonAsync(new { error = "Se requiere un token de autenticación" });
                        return;
                    }

                    // Obtener la clave secreta del appsettings.json
                    var configuration = app.Services.GetRequiredService<IConfiguration>();
                    var jwtSecret = configuration["JwtSecret"];
                    if (jwtSecret == null)
                    {
                        // Handle the case where the configuration value is null
                        res.StatusCode = 500;
                        await res.WriteAsJsonAsync(new { error = "JWT secret key is not configured properly" });
                        return;
                    }

                    var key = Encoding.ASCII.GetBytes(jwtSecret);

                    // Validar el token JWT
                    var tokenHandler = new JwtSecurityTokenHandler();
                    tokenHandler.ValidateToken(jwtToken, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    }, out var validatedToken);

                    var jwtSecurityToken = (JwtSecurityToken)validatedToken;

                    // Obtener los usuarios de la base de datos
                    var usuarios = await db.Usuarios.ToListAsync();

                    res.StatusCode = 200;
                    await res.WriteAsJsonAsync(usuarios);
                }
                catch (Exception error)
                {
                    Console.WriteLine("Error en el endpoint de obtener usuarios: " + error.Message);
                    res.StatusCode = 500;
                    await res.WriteAsJsonAsync(new { error = "Error interno del servidor" });
                }
            });
        }
    }
}
