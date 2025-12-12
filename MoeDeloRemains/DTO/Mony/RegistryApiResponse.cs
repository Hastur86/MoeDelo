using System.Collections.Generic;
using Newtonsoft.Json;

namespace MoeDeloRemains.DTO.Mony
{
    /// <summary>
    /// Ответ от API реестра операций с оберткой "data"
    /// </summary>
    public class RegistryApiWrapperResponse
    {
        /// <summary>
        /// Обертка с данными ответа
        /// </summary>
        [JsonProperty("data")]
        public RegistryApiResponse Data { get; set; }
    }

    /// <summary>
    /// Основной ответ API
    /// </summary>
    public class RegistryApiResponse
    {
        public RegistryApiResponse()
        {
            ResourceList = new List<BankOperationDto>();
        }

        /// <summary>
        /// Список операций (теперь это ResourceList, а не Operations)
        /// </summary>
        [JsonProperty("ResourceList")]
        public List<BankOperationDto> ResourceList { get; set; }

        /// <summary>
        /// Общее количество операций
        /// </summary>
        [JsonProperty("TotalCount")]
        public int TotalCount { get; set; }

        /// <summary>
        /// Флаг успешности операции
        /// </summary>
        [JsonProperty("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Сообщение об ошибке (если есть)
        /// </summary>
        [JsonProperty("Message")]
        public string Message { get; set; }
    }
}