using System.Collections.Generic;

namespace MoeDeloRemains.DTO.Accounting
{
    /// <summary>
    /// Ответ API для счетов
    /// </summary>
    public class BillApiResponse
    {
        public List<BillDto> ResourceList { get; set; }
        public int? Count { get; set; }
    }
}