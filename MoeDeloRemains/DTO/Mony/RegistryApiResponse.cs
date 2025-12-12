using System.Collections.Generic;
using Newtonsoft.Json;

namespace MoeDeloRemains.DTO.Mony
{
    /// <summary>
    /// Ответ от API реестра операций
    /// </summary>
    public class RegistryApiResponse
    {
        public RegistryApiResponse()
        {
            Operations = new List<BankOperationDto>();
        }

        /// <summary>
        /// Список операций
        /// </summary>
        [JsonProperty("ResourceList")]
        public List<BankOperationDto> Operations { get; set; }

        /// <summary>
        /// Общее количество операций
        /// </summary>
        [JsonProperty("TotalCount")]
        public int TotalCount { get; set; }
    }
}