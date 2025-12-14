using MoeDeloRemains.DTO.Mony;

namespace MoeDeloRemains.DTO.Contragents
{
    /// <summary>
    /// DTO для операции банковской выписки с контрагентом
    /// </summary>
    public class BankOperationWithContragentDto : BankOperationDto
    {
        /// <summary>
        /// Данные контрагента (загруженные из API)
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
    }
}