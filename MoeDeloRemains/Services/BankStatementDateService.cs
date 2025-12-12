using System;
using System.Collections.Generic;
using MoeDeloRemains.DTO.Mony;

namespace MoeDeloRemains.Services
{
    /// <summary>
    /// Сервис для манипуляций с датами выписки
    /// </summary>
    public class BankStatementDateService
    {
        /// <summary>
        /// Определение периода запроса для новой выписки
        /// </summary>
        public Tuple<DateTime, DateTime> GetNewStatementPeriod(int periodInYears)
        {
            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate.AddYears(-periodInYears);
            return Tuple.Create(startDate, endDate);
        }

        /// <summary>
        /// Определение даты начала для обновления выписки
        /// </summary>
        public DateTime GetUpdateStartDate(DateTime lastOperationDate, DateTime requestedStartDate)
        {
            // Берем более раннюю дату: либо две недели назад от последней операции,
            // либо запрошенную дату начала, если она раньше
            DateTime twoWeeksAgo = lastOperationDate.AddDays(-14);
            return twoWeeksAgo < requestedStartDate ? twoWeeksAgo : requestedStartDate;
        }

        /// <summary>
        /// Фильтрация операций по дате
        /// </summary>
        public List<BankOperationDto> FilterOperationsByDate(List<BankOperationDto> operations, DateTime startDate, DateTime endDate)
        {
            List<BankOperationDto> result = new List<BankOperationDto>();

            foreach (var op in operations)
            {
                if (op.Date >= startDate && op.Date <= endDate)
                {
                    result.Add(op);
                }
            }

            result.Sort(delegate (BankOperationDto a, BankOperationDto b) {
                return a.Date.CompareTo(b.Date);
            });

            return result;
        }

        /// <summary>
        /// Объединение и обновление операций с устранением дубликатов
        /// </summary>
        public List<BankOperationDto> MergeAndUpdateOperations(List<BankOperationDto> existingOperations,
                                                              List<BankOperationDto> newOperations,
                                                              DateTime startDate)
        {
            // Создаем словарь существующих операций
            Dictionary<long, BankOperationDto> existingDict = new Dictionary<long, BankOperationDto>();
            foreach (var op in existingOperations)
            {
                existingDict[op.Id] = op;
            }

            // Добавляем/обновляем операции
            foreach (var newOp in newOperations)
            {
                existingDict[newOp.Id] = newOp; // Добавит или обновит
            }

            // Преобразуем обратно в список
            List<BankOperationDto> updatedList = new List<BankOperationDto>();
            foreach (var kvp in existingDict)
            {
                updatedList.Add(kvp.Value);
            }

            // Удаляем операции старше startDate
            updatedList.RemoveAll(delegate (BankOperationDto op) { return op.Date < startDate; });

            // Сортируем по дате
            updatedList.Sort(delegate (BankOperationDto a, BankOperationDto b) {
                return a.Date.CompareTo(b.Date);
            });

            return updatedList;
        }
    }
}