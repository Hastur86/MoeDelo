using System;

namespace MoeDeloRemains.DTO.Accounting
{
    /// <summary>
    /// DTO для счета на оплату
    /// </summary>
    public class BillDto
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public DateTime DocDate { get; set; }
        public int? Status { get; set; }
        public string KontragentId { get; set; }
        public float? Sum { get; set; }
        public float? PaidSum { get; set; }
        public string Comment { get; set; }
    }
}