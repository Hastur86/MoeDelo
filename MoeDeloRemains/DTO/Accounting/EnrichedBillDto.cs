using MoeDeloRemains.DTO.Contragents;

namespace MoeDeloRemains.DTO.Accounting
{
    /// <summary>
    /// DTO для обогащенного счета с данными контрагента
    /// </summary>
    public class EnrichedBillDto
    {
        /// <summary>
        /// Детализированные данные счета
        /// </summary>
        public BillDetailDto BillDetail { get; set; }

        /// <summary>
        /// Данные контрагента (загруженные из API или кэша)
        /// </summary>
        public ContragentDto Contragent { get; set; }

        /// <summary>
        /// Была ли успешно загружена информация о контрагенте
        /// </summary>
        public bool ContragentLoaded { get; set; }

        /// <summary>
        /// Сообщение об ошибке загрузки контрагента
        /// </summary>
        public string ContragentErrorMessage { get; set; }

        /// <summary>
        /// Id контрагента в формате string (для сопоставления)
        /// </summary>
        public string KontragentIdString => ContragentLoaded ? Contragent?.Id : null;
    }
}