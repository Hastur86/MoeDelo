using System.Collections.Generic;

namespace MoeDeloRemains.ORP
{
    public class RetailReportRepresentation
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string DocDate { get; set; }
        public List<RetailReportItem> Items { get; set; }
        public int StockId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<RetailReportReasonRevenue> ReasonRevenues { get; set; }
    }
}