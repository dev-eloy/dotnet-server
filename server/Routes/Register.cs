using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.Data;
using server.Models;
using BCrypt.Net;

namespace server.Routes
{
    public static class Register
    {
        public static void MapRegister(this WebApplication app)
        {
            app.MapPost("/api/register", async (ConnectionDB db, HttpRequest req, HttpResponse res) =>
            {
                try
                {
                    // Extraer la información del cuerpo de la solicitud
                    var body = await req.ReadFromJsonAsync<Usuarios>();
                    if (body == null)
                    {
                        res.StatusCode = 400;
                        await res.WriteAsJsonAsync(new { error = "Cuerpo de solicitud no válido" });
                        return;
                    }

                    // Verificar si el correo electrónico ya está registrado
                    var existingUser = await db.Usuarios.FirstOrDefaultAsync(u => u.Mail == body.Mail);
                    if (existingUser != null)
                    {
                        res.StatusCode = 400;
                        await res.WriteAsJsonAsync(new { error = "El correo electrónico ya está registrado. Por favor, utiliza otro correo electrónico." });
                        return;
                    }

                    // Verificar si el rol proporcionado es válido
                    var validRoles = new[] { "admin", "user" }; // Definir los roles válidos aquí
                    if (!validRoles.Contains(body.Role.ToLower()))
                    {
                        res.StatusCode = 400;
                        await res.WriteAsJsonAsync(new { error = "El rol proporcionado no es válido. Los roles válidos son 'admin' o 'user'." });
                        return;
                    }

                    // Encriptar la contraseña antes de almacenarla
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(body.Password);

                    // Insertar el nuevo usuario en la base de datos
                    db.Usuarios.Add(new Usuarios
                    {
                        Name = body.Name,
                        Mail = body.Mail,
                        Password = hashedPassword,
                        Role = body.Role
                    });
                    await db.SaveChangesAsync();

                    res.StatusCode = 201;
                    await res.WriteAsJsonAsync(new { message = "Registro exitoso" });
                }
                catch (System.Exception error)
                {
                    System.Console.WriteLine("Error en el endpoint de registro: " + error.Message);
                    res.StatusCode = 500;
                    await res.WriteAsJsonAsync(new { error = "Error interno del servidor" });
                }
            });
        }
    }
}
