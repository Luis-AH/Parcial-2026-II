using System;
using System.ComponentModel.DataAnnotations;

namespace PlataformaIncidencias.Models
{
    public class Incidencia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Estacion { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Prioridad { get; set; } = string.Empty; // "Alta", "Media", "Baja"

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Abierta"; // "Abierta", "Cerrada"
    }
}
