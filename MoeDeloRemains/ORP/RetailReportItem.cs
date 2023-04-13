namespace MoeDeloRemains.ORP
{
    public class RetailReportItem
    {
        public int StockProductId { get; set; }
        public string Name { get; set; }
        public float Count { get; set; } 
        public string Unit { get; set; }
        public float? TotalSum { get; set; }
    }
}