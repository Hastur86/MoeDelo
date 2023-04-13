using System.Collections.Generic;

namespace MoeDeloRemains.ORP
{
    public class RetailReportModel
    {
        public int Id { get; set; }
        public int TaxationSystemType { get; set; }
        public string DocDate { get; set; }
        public string Number { get; set; }
        public List<RetailReportItem> Items { get; set; }
        public int StockId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<RetailReportReasonRevenue> ReasonRevenues { get; set; }

        public RetailReportModel()
        {
            TaxationSystemType = 6;
        }

        public RetailReportModel(RetailReportRepresentation orp)
        {
            TaxationSystemType = 6;
            DocDate = orp.DocDate;
            Number = orp.Number;
            Items = orp.Items;
            StockId = orp.StockId;
            Id = orp.Id;
            StartDate = orp.StartDate;
            EndDate = orp.EndDate;
            ReasonRevenues = orp.ReasonRevenues;
        }
    }
}