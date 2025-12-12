using System;

namespace MoeDeloRemains.DTO.Mony
{
    /// <summary>
    /// Запрос для получения операций
    /// </summary>
    public class RegistryRequestDto
    {
        /// <summary>
        /// Дата начала периода
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Дата окончания периода
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Источник операции (1 - расчетный счет)
        /// </summary>
        public int OperationSource { get; set; } = 1;

        /// <summary>
        /// Тип операции (16 - списание)
        /// </summary>
        public int OperationType { get; set; } = 16;

        /// <summary>
        /// Лимит записей на странице
        /// </summary>
        public int Limit { get; set; } = 1000;

        /// <summary>
        /// Номер страницы (для пагинации)
        /// </summary>
        public int Page { get; set; } = 1;
    }
}