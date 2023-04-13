using System.Collections.Generic;

namespace MoeDeloRemains.ORP
{
    public class RetailReportRepresentationCollection
    {
        public int Count { get; set; }
        public List<RetailReportCollectionItemRepresentation> ResourceList { get; set; }
        public int TotalCount { get; set; }
    }
}