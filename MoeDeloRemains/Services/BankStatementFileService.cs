using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MoeDeloRemains.DTO.Mony;
using Newtonsoft.Json;

namespace MoeDeloRemains.Services
{
    /// <summary>
    /// Сервис для работы с файлами выписки
    /// </summary>
    public class BankStatementFileService
    {
        private readonly string _storagePath;

        private const string StatementFileName = "bank_statement.json";
        private const string MetadataFileName = "statement_metadata.json";

        /// <summary>
        /// Конструктор сервиса работы с файлами
        /// </summary>
        public BankStatementFileService(string storagePath = null)
        {
            _storagePath = storagePath ?? Directory.GetCurrentDirectory();

            // Создаем директорию, если она не существует
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        /// <summary>
        /// Получить пути к файлам
        /// </summary>
        public Tuple<string, string> GetFilePaths()
        {
            string statementFilePath = Path.Combine(_storagePath, StatementFileName);
            string metadataFilePath = Path.Combine(_storagePath, MetadataFileName);
            return Tuple.Create(statementFilePath, metadataFilePath);
        }

        /// <summary>
        /// Проверить существование файлов выписки
        /// </summary>
        public bool StatementFilesExist()
        {
            var paths = GetFilePaths();
            return File.Exists(paths.Item1) && File.Exists(paths.Item2);
        }

        /// <summary>
        /// Загрузка существующих данных из файлов
        /// </summary>
        public Tuple<StatementMetadata, List<BankOperationDto>> LoadExistingStatement()
        {
            var paths = GetFilePaths();

            // Загружаем метаданные
            string metadataJson = File.ReadAllText(paths.Item2);
            StatementMetadata metadata = JsonConvert.DeserializeObject<StatementMetadata>(metadataJson);

            // Загружаем операции
            string operationsJson = File.ReadAllText(paths.Item1);
            List<BankOperationDto> operations = JsonConvert.DeserializeObject<List<BankOperationDto>>(operationsJson);

            return Tuple.Create(metadata, operations);
        }

        /// <summary>
        /// Сохранение выписки в файл
        /// </summary>
        public void SaveStatementToFile(List<BankOperationDto> operations, DateTime startDate, DateTime endDate)
        {
            if (operations.Count == 0)
            {
                return;
            }

            var paths = GetFilePaths();

            // Сортируем операции по дате
            operations.Sort(delegate (BankOperationDto a, BankOperationDto b) {
                return a.Date.CompareTo(b.Date);
            });

            // Создаем метаданные
            StatementMetadata metadata = new StatementMetadata
            {
                FirstOperationDate = operations[0].Date,
                LastOperationDate = operations[operations.Count - 1].Date,
                OperationCount = operations.Count,
                LastUpdated = DateTime.Now,
                ContentHash = CalculateContentHash(operations)
            };

            // Сериализуем данные
            string statementJson = JsonConvert.SerializeObject(operations, Formatting.Indented);
            string metadataJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);

            // Сохраняем в файлы
            File.WriteAllText(paths.Item1, statementJson);
            File.WriteAllText(paths.Item2, metadataJson);
        }

        /// <summary>
        /// Расчет MD5 хеша
        /// </summary>
        private string CalculateContentHash(List<BankOperationDto> operations)
        {
            string json = JsonConvert.SerializeObject(operations);
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(json);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Получение информации о файле выписки
        /// </summary>
        public StatementMetadata GetStatementInfo()
        {
            var paths = GetFilePaths();

            if (!File.Exists(paths.Item2))
            {
                return null;
            }

            string metadataJson = File.ReadAllText(paths.Item2);
            return JsonConvert.DeserializeObject<StatementMetadata>(metadataJson);
        }
    }
}