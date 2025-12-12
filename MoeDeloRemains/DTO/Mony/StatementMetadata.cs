using System;

namespace MoeDeloRemains.DTO.Mony
{
    /// <summary>
    /// Метаданные файла выписки
    /// </summary>
    public class StatementMetadata
    {
        /// <summary>
        /// Дата первой операции в файле
        /// </summary>
        public DateTime FirstOperationDate { get; set; }

        /// <summary>
        /// Дата последней операции в файле
        /// </summary>
        public DateTime LastOperationDate { get; set; }

        /// <summary>
        /// Количество операций в файле
        /// </summary>
        public int OperationCount { get; set; }

        /// <summary>
        /// Дата последнего обновления файла
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// MD5 хеш содержимого
        /// </summary>
        public string ContentHash { get; set; }
    }
}