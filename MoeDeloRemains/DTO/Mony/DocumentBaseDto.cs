using Newtonsoft.Json;

namespace MoeDeloRemains.DTO.Mony
{
    /// <summary>
    /// Документ-основание операции
    /// </summary>
    public class DocumentBaseDto
    {
        /// <summary>
        /// ID документа
        /// </summary>
        [JsonProperty("Id")]
        public long Id { get; set; }

        /// <summary>
        /// Тип документа
        /// </summary>
        [JsonProperty("DocumentType")]
        public int DocumentType { get; set; }
    }
}