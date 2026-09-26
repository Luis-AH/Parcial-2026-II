namespace PlataformaIncidencias.Models
{
    /// <summary>
    /// Representa el objeto que se indexa en Algolia.
    /// ObjectID debe coincidir con el Id de la Incidencia para que las operaciones
    /// de actualización/borrado sean idempotentes y predecibles.
    /// </summary>
    public class IncidenciaAlgolia
    {
        // Algolia usa "objectID" como clave primaria
        public string ObjectID { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Estacion { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}
