using System;
using System.Collections.Generic;

namespace MoeDeloRemains
{
    public class PurchasesWaybillRepresentation
    {
        public string Number { get; set; }//(string) : Номер документа(уникальный в пределах года) ,
        public string DocDate { get; set; }//(string) : Дата документа,
        public List<PurchasesWaybillItemRepresentation> Items { get; set; }// (Array[PurchasesWaybillItemRepresentation]): Позиции накладной,
        public int KontragentId { get; set; }//(integer) : Контрагент ,
        public int NdsPositionType { get; set; }//(integer) : Тип начисления НДС 0 - Нет, 1 - Сверху, 2 - В том числе ,
        public int StockId { get; set; }// (integer): Id склада, если товар/материал оприходован на склад,
        public bool DiscrepancyNumberOrQuality { get; set; }//(boolean, optional) : Наличие несоответствия по количеству/качеству
        public int TaxationSystemType { get; set; }
        public PurchasesWaybillRepresentation()
        {
            DocDate = "2022-11-30";
            NdsPositionType = 1;
            DiscrepancyNumberOrQuality = false;
            KontragentId = 21221433;
            TaxationSystemType = 6;
            Items = new List<PurchasesWaybillItemRepresentation>();
        }
    }
}