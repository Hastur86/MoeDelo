using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MoeDeloRemains.DTO.Accounting
{
    /// <summary>
    /// DTO для представления счета на оплату (детализированный ответ API)
    /// </summary>
    public class BillDetailDto
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Number")]
        public string Number { get; set; }

        [JsonProperty("DocDate")]
        public DateTime DocDate { get; set; }

        [JsonProperty("Items")]
        public List<SalesDocumentItemRepresentation> Items { get; set; }

        [JsonProperty("Online")]
        public string Online { get; set; }

        [JsonProperty("Context")]
        public Context Context { get; set; }

        [JsonProperty("Payments")]
        public List<BillPayment> Payments { get; set; }

        [JsonProperty("UseStampAndSign")]
        public bool UseStampAndSign { get; set; }

        [JsonProperty("Type")]
        public int? Type { get; set; }

        [JsonProperty("Status")]
        public int? Status { get; set; }

        // ВАЖНО: В детализированном ответе это integer, а не string
        [JsonProperty("KontragentId")]
        public int? KontragentId { get; set; }

        [JsonProperty("SettlementAccount")]
        public SettlementAccountModel SettlementAccount { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("StockId")]
        public int? StockId { get; set; }

        [JsonProperty("DeadLine")]
        public DateTime? DeadLine { get; set; }

        [JsonProperty("AdditionalInfo")]
        public string AdditionalInfo { get; set; }

        [JsonProperty("ContractSubject")]
        public string ContractSubject { get; set; }

        [JsonProperty("NdsPositionType")]
        public int? NdsPositionType { get; set; }

        [JsonProperty("IsCovered")]
        public bool? IsCovered { get; set; }

        [JsonProperty("Sum")]
        public float? Sum { get; set; }

        [JsonProperty("PaidSum")]
        public float? PaidSum { get; set; }

        [JsonProperty("Comment")]
        public string Comment { get; set; }
    }

    /// <summary>
    /// Позиция документа
    /// </summary>
    public class SalesDocumentItemRepresentation
    {
        [JsonProperty("DiscountRate")]
        public float? DiscountRate { get; set; }

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Count")]
        public float? Count { get; set; }

        [JsonProperty("Unit")]
        public string Unit { get; set; }

        [JsonProperty("Type")]
        public int Type { get; set; }

        [JsonProperty("ActivityAccountCode")]
        public int? ActivityAccountCode { get; set; }

        [JsonProperty("Price")]
        public float? Price { get; set; }

        [JsonProperty("NdsType")]
        public int NdsType { get; set; }

        [JsonProperty("SumWithoutNds")]
        public float? SumWithoutNds { get; set; }

        [JsonProperty("NdsSum")]
        public float? NdsSum { get; set; }

        [JsonProperty("SumWithNds")]
        public float? SumWithNds { get; set; }

        [JsonProperty("StockProductId")]
        public int? StockProductId { get; set; }

        [JsonProperty("Country")]
        public string Country { get; set; }

        [JsonProperty("CountryIso")]
        public string CountryIso { get; set; }
    }

    /// <summary>
    /// Информация об изменениях документа
    /// </summary>
    public class Context
    {
        [JsonProperty("CreateDate")]
        public DateTime CreateDate { get; set; }

        [JsonProperty("ModifyDate")]
        public DateTime ModifyDate { get; set; }

        [JsonProperty("ModifyUser")]
        public string ModifyUser { get; set; }
    }

    /// <summary>
    /// Платеж, связанный со счетом
    /// </summary>
    public class BillPayment
    {
        [JsonProperty("Number")]
        public string Number { get; set; }

        [JsonProperty("Date")]
        public DateTime? Date { get; set; }

        [JsonProperty("Sum")]
        public float? Sum { get; set; }

        [JsonProperty("Id")]
        public int? Id { get; set; }
    }

    /// <summary>
    /// Расчетный счет
    /// </summary>
    public class SettlementAccountModel
    {
        [JsonProperty("AccountId")]
        public int? AccountId { get; set; }

        [JsonProperty("AccountNumber")]
        public string AccountNumber { get; set; }
    }

    /// <summary>
    /// DTO для запроса детальных счетов по ID
    /// </summary>
    public class BillDetailRequestDto
    {
        [JsonProperty("Ids")]
        public List<int> Ids { get; set; }

        public BillDetailRequestDto()
        {
            Ids = new List<int>();
        }

        public BillDetailRequestDto(List<int> ids)
        {
            Ids = ids;
        }
    }
}