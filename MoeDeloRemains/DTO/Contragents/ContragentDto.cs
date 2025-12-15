using System;

namespace MoeDeloRemains.DTO.Contragents
{
    /// <summary>
    /// DTO для контрагента
    /// </summary>
    public class ContragentDto
    {
        /// <summary>
        /// Идентификатор контрагента
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Наименование контрагента
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ИНН
        /// </summary>
        public string Inn { get; set; }

        /// <summary>
        /// КПП
        /// </summary>
        public string Kpp { get; set; }

        /// <summary>
        /// Полное наименование
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Краткое наименование
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// Тип контрагента (1 - Юр. лицо, 2 - Физ. лицо, 3 - ИП)
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Адрес
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Контактные данные
        /// </summary>
        public ContactInfoDto ContactInfo { get; set; }

        /// <summary>
        /// Дата последнего обновления
        /// </summary>
        public DateTime? Updated { get; set; }
    }
}