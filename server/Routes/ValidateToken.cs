using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace server.Routes
{
    public static class ValidateToken
    {
        public static void MapValidateToken(this WebApplication app)
        {
            var configuration = app.Services.GetRequiredService<IConfiguration>();

            app.MapPost("/api/validatetoken", async (HttpRequest req, HttpResponse res) =>
            {
                try
                {
                    string jwtToken = null;

                    // Intentar obtener el token del encabezado de autorización
                    var authorizationHeader = req.Headers["Authorization"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(authorizationHeader))
                    {
                        jwtToken = authorizationHeader.Split(" ").Last();
                    }

                    // Si no se encuentra en el encabezado, intentar obtenerlo del cuerpo de la solicitud
                    if (jwtToken == null)
                    {
                        using var bodyReader = new StreamReader(req.Body);
                        var body = await bodyReader.ReadToEndAsync();
                        var bodyJson = JsonDocument.Parse(body).RootElement;
                        jwtToken = bodyJson.GetProperty("token").GetString();
                    }

                    if (jwtToken == null)
                    {
                        res.StatusCode = 401;
                        await res.WriteAsJsonAsync(new { error = "Se requiere un token de autenticación" });
                        return;
                    }

                    var jwtSecret = configuration["JwtSecret"];
                    if (jwtSecret == null)
                    {
                        res.StatusCode = 500;
                        await res.WriteAsJsonAsync(new { error = "La clave secreta JWT no está configurada correctamente" });
                        return;
                    }

                    var key = Encoding.ASCII.GetBytes(jwtSecret);

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
                    var decodedToken = new
                    {
                        Issuer = jwtSecurityToken.Issuer,
                        Subject = jwtSecurityToken.Subject,
                        Expires = jwtSecurityToken.ValidTo
                    };

                    res.StatusCode = 200;
                    res.ContentType = "application/json";
                    await res.WriteAsJsonAsync(new { message = "Token válido", decoded = decodedToken });
                }
                catch (SecurityTokenException)
                {
                    res.StatusCode = 401;
                    await res.WriteAsJsonAsync(new { error = "Token inválido" });
                }
                catch (Exception)
                {
                    res.StatusCode = 500;
                    await res.WriteAsJsonAsync(new { error = "Error interno del servidor" });
                }
            });
        }
    }
}
