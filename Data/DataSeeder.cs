using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PlataformaIncidencias.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(
            IServiceProvider serviceProvider,
            UserManager<IdentityUser> userManager,
            AlgoliaService algoliaService)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Crea la DB si no existe (ideal para Render + SQLite efímero)
            context.Database.EnsureCreated();

            // GUID FIJO — nunca aleatorio para evitar huérfanos en RabbitMQ tras reinicio
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
                await userManager.CreateAsync(user, "Admin123!");
            }

            if (!context.Incidencias.Any())
            {
                var incidencias = new[]
                {
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
                };

                context.Incidencias.AddRange(incidencias);
                await context.SaveChangesAsync();

                // Indexar en Algolia después de guardar en SQLite
                await algoliaService.IndexarTodasAsync(incidencias);
            }
        }
    }
}
