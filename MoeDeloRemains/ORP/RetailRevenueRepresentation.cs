using System.Collections.Generic;

namespace MoeDeloRemains.ORP
{
    public class RetailRevenueRepresentation
    {
        public int Id { get; set; }
        public string ZReportNumber { get; set; }
        public string DocDate { get; set; }
        public float Sum { get; set; }
        public float? PayByCard { get; set; }
        public string Description { get; set; }
        public float? UsnSum { get; set; }
        public float? EnvdSum { get; set; }
        public int? PayerId { get; set; }
        public List<СonsideredInPatent> СonsideredInPatents { get; set; }
    }
}