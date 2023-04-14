namespace MoeDeloRemains.ORP
{
    public class OperationResponseDto
    {
        public int DocumentBaseId { get; set; }
        public string PatentId { get; set; }
        public int TaxationSystemType { get; set; }
        public SourceDto Source { get; set; }
        public string Description { get; set; }
    }
}