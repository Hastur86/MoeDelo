using System;
using Newtonsoft.Json;

namespace MoeDeloRemains.DTO.Mony
{
    /// <summary>
    /// DTO для банковской операции
    /// </summary>
    public class BankOperationDto
    {
        /// <summary>
        /// Уникальный идентификатор операции
        /// </summary>
        [JsonProperty("Id")]
        public long Id { get; set; }

        /// <summary>
        /// Дата операции
        /// </summary>
        [JsonProperty("Date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Номер документа
        /// </summary>
        [JsonProperty("Number")]
        public string Number { get; set; }

        /// <summary>
        /// Контрагент
        /// </summary>
        [JsonProperty("Contractor")]
        public ContractorDto Contractor { get; set; }

        public SourceDto Source { get; set; }

        /// <summary>
        /// Назначение платежа
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; }

        /// <summary>
        /// Сумма операции
        /// </summary>
        [JsonProperty("Sum")]
        public decimal Sum { get; set; }

        /// <summary>
        /// ID банковского счета
        /// </summary>
        [JsonProperty("BankAccountId")]
        public long BankAccountId { get; set; }

        /// <summary>
        /// Источник операции: 1 - расчетный счет
        /// </summary>
        [JsonProperty("OperationSource")]
        public int OperationSource { get; set; }

        /// <summary>
        /// Тип операции: 16 - списание с расчетного счета
        /// </summary>
        [JsonProperty("OperationType")]
        public int OperationType { get; set; }

        /// <summary>
        /// Документ-основание
        /// </summary>
        [JsonProperty("DocumentBase")]
        public DocumentBaseDto DocumentBase { get; set; }
    }
}