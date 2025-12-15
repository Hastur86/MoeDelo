using System;

namespace MoeDeloRemains.DTO.Accounting
{
    /// <summary>
    /// DTO для счета на оплату
    /// </summary>
    public class BillDto
    {
        public long Id { get; set; }
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public long? ContractorId { get; set; }
        public string ContractorName { get; set; }
        public decimal? Sum { get; set; }
        public decimal? VatSum { get; set; }
        public decimal? TotalSum { get; set; }
        public string Currency { get; set; }
        public string Comment { get; set; }
        public DateTime? DueDate { get; set; }
        public int? Status { get; set; }
        public string StatusText { get; set; }
        public long? DocumentId { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}