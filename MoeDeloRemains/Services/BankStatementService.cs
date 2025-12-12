using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using MoeDeloRemains.DTO.Mony;
using Newtonsoft.Json;

namespace MoeDeloRemains.Services
{
    /// <summary>
    /// Сервис для работы с банковскими выписками
    /// </summary>
    public class BankStatementService
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _storagePath;

        private const string StatementFileName = "bank_statement.json";
        private const string MetadataFileName = "statement_metadata.json";

        /// <summary>
        /// Конструктор сервиса
        /// </summary>
        public BankStatementService(string apiKey, string baseUrl = "https://restapi.moedelo.org",
            string storagePath = null)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException("apiKey");

            _apiKey = apiKey;
            _baseUrl = baseUrl.TrimEnd('/');
            _storagePath = storagePath ?? Directory.GetCurrentDirectory();

            // Создаем директорию, если она не существует
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            // Настройка TLS для .NET 3.5
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback =
                new RemoteCertificateValidationCallback((sender, certificate, chain, errors) => true);
        }

        /// <summary>
        /// Получить выписку за период
        /// </summary>
        public List<BankOperationDto> GetBankStatement(int periodInYears = 1)
        {
            try
            {
                Console.WriteLine(string.Format("Начало получения выписки за {0} год(а)", periodInYears));

                // Определяем период
                DateTime endDate = DateTime.Now;
                DateTime startDate = endDate.AddYears(-periodInYears);

                Console.WriteLine(string.Format("Период запроса: с {0} по {1}",
                    startDate.ToString("yyyy-MM-dd"),
                    endDate.ToString("yyyy-MM-dd")));

                // Пути к файлам
                string statementFilePath = Path.Combine(_storagePath, StatementFileName);
                string metadataFilePath = Path.Combine(_storagePath, MetadataFileName);

                // Проверяем существование файлов
                bool statementExists = File.Exists(statementFilePath);
                bool metadataExists = File.Exists(metadataFilePath);

                if (!statementExists || !metadataExists)
                {
                    Console.WriteLine("Файлы выписки не найдены. Создаем новые...");
                    return CreateNewStatementFile(startDate, endDate, statementFilePath, metadataFilePath);
                }

                // Загружаем существующие данные
                var existingData = LoadExistingStatement(statementFilePath, metadataFilePath);
                StatementMetadata metadata = existingData.Item1;
                List<BankOperationDto> existingOperations = existingData.Item2;

                // Определяем дату начала для обновления
                DateTime updateStartDate = GetUpdateStartDate(metadata.LastOperationDate, startDate);
                Console.WriteLine(string.Format("Дата начала для обновления: {0}",
                    updateStartDate.ToString("yyyy-MM-dd")));

                // Получаем операции для обновления
                List<BankOperationDto> operationsToAdd = GetOperationsFromApi(updateStartDate, endDate);

                if (operationsToAdd.Count == 0)
                {
                    Console.WriteLine("Новых операций для добавления не найдено");
                    return FilterOperationsByDate(existingOperations, startDate, endDate);
                }

                // Объединяем и обновляем данные
                List<BankOperationDto> updatedOperations =
                    MergeAndUpdateOperations(existingOperations, operationsToAdd, startDate);

                // Сохраняем обновленные данные
                SaveStatementToFile(updatedOperations, statementFilePath, metadataFilePath, startDate, endDate);

                Console.WriteLine(string.Format("Выписка успешно обновлена. Всего операций: {0}",
                    updatedOperations.Count));
                return updatedOperations;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Ошибка при получении выписки: {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Создание нового файла выписки
        /// </summary>
        private List<BankOperationDto> CreateNewStatementFile(DateTime startDate, DateTime endDate,
            string statementPath, string metadataPath)
        {
            Console.WriteLine("Запрашиваем все операции за указанный период...");
            List<BankOperationDto> operations = GetOperationsFromApi(startDate, endDate);

            if (operations.Count == 0)
            {
                Console.WriteLine("Операции за указанный период не найдены");
                return new List<BankOperationDto>();
            }

            SaveStatementToFile(operations, statementPath, metadataPath, startDate, endDate);
            Console.WriteLine(string.Format("Новый файл выписки создан. Операций: {0}", operations.Count));

            return operations;
        }

        /// <summary>
        /// Загрузка существующих данных из файлов
        /// </summary>
        private Tuple<StatementMetadata, List<BankOperationDto>> LoadExistingStatement(string statementPath,
            string metadataPath)
        {
            // Загружаем метаданные
            string metadataJson = File.ReadAllText(metadataPath);
            StatementMetadata metadata = JsonConvert.DeserializeObject<StatementMetadata>(metadataJson);

            // Загружаем операции
            string operationsJson = File.ReadAllText(statementPath);
            List<BankOperationDto> operations = JsonConvert.DeserializeObject<List<BankOperationDto>>(operationsJson);

            Console.WriteLine(string.Format("Загружены существующие данные: {0} операций с {1} по {2}",
                operations.Count,
                metadata.FirstOperationDate.ToString("yyyy-MM-dd"),
                metadata.LastOperationDate.ToString("yyyy-MM-dd")));

            return Tuple.Create(metadata, operations);
        }

        /// <summary>
        /// Определение даты начала для обновления
        /// </summary>
        private DateTime GetUpdateStartDate(DateTime lastOperationDate, DateTime requestedStartDate)
        {
            // Берем более раннюю дату
            DateTime twoWeeksAgo = lastOperationDate.AddDays(-14);
            return twoWeeksAgo < requestedStartDate ? twoWeeksAgo : requestedStartDate;
        }

        /// <summary>
        /// Получение операций из API
        /// </summary>
        private List<BankOperationDto> GetOperationsFromApi(DateTime startDate, DateTime endDate)
        {
            List<BankOperationDto> allOperations = new List<BankOperationDto>();
            int currentPage = 1;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                Console.WriteLine(string.Format("Запрос страницы {0}...", currentPage));

                List<BankOperationDto> operations = GetOperationsPage(startDate, endDate, currentPage, 1000);

                if (operations.Count > 0)
                {
                    allOperations.AddRange(operations);
                    currentPage++;
                }
                else
                {
                    hasMorePages = false;
                }

                // Если получено меньше запрошенного лимита
                if (operations.Count < 1000)
                {
                    hasMorePages = false;
                }

                // Небольшая задержка между запросами
                System.Threading.Thread.Sleep(100);
            }

            Console.WriteLine(string.Format("Получено {0} операций из API", allOperations.Count));
            return allOperations;
        }

        /// <summary>
        /// Получение одной страницы операций
        /// </summary>
        private List<BankOperationDto> GetOperationsPage(DateTime startDate, DateTime endDate, int page, int limit)
        {
            try
            {
                // Формируем URL запроса
                string url = string.Format(
                    "{0}/money/api/v1/Registry?StartDate={1}&EndDate={2}&OperationSource=1&OperationType=16&Limit={3}",
                    _baseUrl,
                    startDate.ToString("yyyy.MM.dd"),
                    endDate.ToString("yyyy.MM.dd"),
                    limit);

                // Создаем WebRequest
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("md-api-key", _apiKey);
                request.Accept = "application/json";
                request.Timeout = 30000;

                // Получаем ответ
                using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new WebException(string.Format("API вернул ошибку: {0}", response.StatusCode));
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string content = reader.ReadToEnd();

                        // Используем JavaScriptSerializer как альтернативу Newtonsoft для .NET 3.5
                        var serializer = new JavaScriptSerializer();
                        var result = serializer.Deserialize<Dictionary<string, object>>(content);

                        if (result.ContainsKey("ResourceList"))
                        {
                            var operationsList = result["ResourceList"] as object[];
                            if (operationsList != null)
                            {
                                List<BankOperationDto> operations = new List<BankOperationDto>();

                                foreach (var item in operationsList)
                                {
                                    var itemDict = item as Dictionary<string, object>;
                                    if (itemDict != null)
                                    {
                                        BankOperationDto operation = new BankOperationDto();

                                        // Парсим поля
                                        if (itemDict.ContainsKey("Id") &&
                                            long.TryParse(itemDict["Id"].ToString(), out long id))
                                            operation.Id = id;

                                        if (itemDict.ContainsKey("Date") &&
                                            DateTime.TryParse(itemDict["Date"].ToString(), out DateTime date))
                                            operation.Date = date;

                                        if (itemDict.ContainsKey("Number"))
                                            operation.Number = itemDict["Number"].ToString();

                                        if (itemDict.ContainsKey("ContragentName"))
                                            operation.ContragentName = itemDict["ContragentName"].ToString();

                                        if (itemDict.ContainsKey("Description"))
                                            operation.Description = itemDict["Description"].ToString();

                                        if (itemDict.ContainsKey("Sum") &&
                                            decimal.TryParse(itemDict["Sum"].ToString(), out decimal sum))
                                            operation.Sum = sum;

                                        if (itemDict.ContainsKey("OperationSource") &&
                                            int.TryParse(itemDict["OperationSource"].ToString(), out int source))
                                            operation.OperationSource = source;

                                        if (itemDict.ContainsKey("OperationType") &&
                                            int.TryParse(itemDict["OperationType"].ToString(), out int type))
                                            operation.OperationType = type;

                                        operations.Add(operation);
                                    }
                                }

                                return operations;
                            }
                        }

                        return new List<BankOperationDto>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Ошибка при запросе к API: {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Объединение и обновление операций
        /// </summary>
        private List<BankOperationDto> MergeAndUpdateOperations(List<BankOperationDto> existingOperations,
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
                if (existingDict.ContainsKey(newOp.Id))
                {
                    existingDict[newOp.Id] = newOp;
                }
                else
                {
                    existingDict[newOp.Id] = newOp;
                }
            }

            // Преобразуем обратно в список
            List<BankOperationDto> updatedList = new List<BankOperationDto>();
            foreach (var kvp in existingDict)
            {
                updatedList.Add(kvp.Value);
            }

            // Удаляем операции старше startDate
            updatedList.RemoveAll(delegate(BankOperationDto op) { return op.Date < startDate; });

            // Сортируем по дате
            updatedList.Sort(delegate(BankOperationDto a, BankOperationDto b) { return a.Date.CompareTo(b.Date); });

            return updatedList;
        }

        /// <summary>
        /// Фильтрация операций по дате
        /// </summary>
        private List<BankOperationDto> FilterOperationsByDate(List<BankOperationDto> operations, DateTime startDate,
            DateTime endDate)
        {
            List<BankOperationDto> result = new List<BankOperationDto>();

            foreach (var op in operations)
            {
                if (op.Date >= startDate && op.Date <= endDate)
                {
                    result.Add(op);
                }
            }

            result.Sort(delegate(BankOperationDto a, BankOperationDto b) { return a.Date.CompareTo(b.Date); });

            return result;
        }

        /// <summary>
        /// Сохранение выписки в файл
        /// </summary>
        private void SaveStatementToFile(List<BankOperationDto> operations, string statementPath,
            string metadataPath, DateTime startDate, DateTime endDate)
        {
            if (operations.Count == 0)
            {
                Console.WriteLine("Нет операций для сохранения");
                return;
            }

            // Сортируем операции по дате
            operations.Sort(delegate(BankOperationDto a, BankOperationDto b) { return a.Date.CompareTo(b.Date); });

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
            File.WriteAllText(statementPath, statementJson);
            File.WriteAllText(metadataPath, metadataJson);

            Console.WriteLine("Данные сохранены в файлы:");
            Console.WriteLine(string.Format("  - Операции: {0}", statementPath));
            Console.WriteLine(string.Format("  - Метаданные: {0}", metadataPath));
        }

        /// <summary>
        /// Расчет MD5 хеша
        /// </summary>
        private string CalculateContentHash(List<BankOperationDto> operations)
        {
            string json = JsonConvert.SerializeObject(operations);
            using (MD5 md5 = MD5.Create())
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
            string metadataPath = Path.Combine(_storagePath, MetadataFileName);

            if (!File.Exists(metadataPath))
            {
                return null;
            }

            string metadataJson = File.ReadAllText(metadataPath);
            return JsonConvert.DeserializeObject<StatementMetadata>(metadataJson);
        }
    }
}