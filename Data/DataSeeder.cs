using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PlataformaIncidencias.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PlataformaIncidencias.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider, UserManager<IdentityUser> userManager)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Crea la DB si no existe (ideal para Render + SQLite temporal)
            context.Database.EnsureCreated();

            // GUID FIJO REQUERIDO (No usar aleatorios)
            var fixedUserId = "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"; 

            if (!context.Users.Any(u => u.Id == fixedUserId))
            {
                var user = new IdentityUser
                {
                    Id = fixedUserId,
                    UserName = "supervisor@bicioperaciones.com",
                    Email = "supervisor@bicioperaciones.com",
                    EmailConfirmed = true
                };
                
                // Password genérico para la prueba
                await userManager.CreateAsync(user, "Admin123!");
            }

            if (!context.Incidencias.Any())
            {
                context.Incidencias.AddRange(
                    new Incidencia
                    {
                        Id = 101, 
                        Estacion = "Estación Central",
                        Descripcion = "Falla en el anclaje del slot 4 de bicicletas",
                        Prioridad = "Alta",
                        Estado = "Abierta"
                    },
                    new Incidencia
                    {
                        Id = 102,
                        Estacion = "Plaza de Armas",
                        Descripcion = "La pantalla principal no responde al tacto",
                        Prioridad = "Media",
                        Estado = "Abierta"
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
