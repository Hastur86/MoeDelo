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
using MoeDeloRemains.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeDeloRemains.Services
{
    /// <summary>
    /// Сервис для работы с банковскими выписками
    /// </summary>
    public class BankStatementService
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly BankStatementFileService _fileService;
        private readonly BankStatementDateService _dateService;

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
            _fileService = new BankStatementFileService(storagePath);
            _dateService = new BankStatementDateService();

            // Инициализируем SSL настройки
            SslHelper.InitializeSslSettings();

            Console.WriteLine("Сервис инициализирован");
            Console.WriteLine("Базовый URL: " + _baseUrl);
        }

        /// <summary>
        /// Получить выписку за период
        /// </summary>
        public List<BankOperationDto> GetBankStatement(int periodInYears = 1)
        {
            try
            {
                Console.WriteLine(string.Format("Начало получения выписки за {0} год(а)", periodInYears));

                // Определяем период для запроса
                var period = _dateService.GetNewStatementPeriod(periodInYears);
                DateTime startDate = period.Item1;
                DateTime endDate = period.Item2;

                Console.WriteLine(string.Format("Период запроса: с {0} по {1}",
                    startDate.ToString("yyyy-MM-dd"),
                    endDate.ToString("yyyy-MM-dd")));

                // Проверяем существование файлов
                bool statementExists = _fileService.StatementFilesExist();

                if (!statementExists)
                {
                    Console.WriteLine("Файлы выписки не найдены. Создаем новые...");
                    return CreateNewStatementFile(startDate, endDate);
                }

                // Загружаем существующие данные
                var existingData = _fileService.LoadExistingStatement();
                StatementMetadata metadata = existingData.Item1;
                List<BankOperationDto> existingOperations = existingData.Item2;

                Console.WriteLine(string.Format("Загружены существующие данные: {0} операций с {1} по {2}",
                    existingOperations.Count,
                    metadata.FirstOperationDate.ToString("yyyy-MM-dd"),
                    metadata.LastOperationDate.ToString("yyyy-MM-dd")));

                // Определяем дату начала для обновления
                DateTime updateStartDate = _dateService.GetUpdateStartDate(metadata.LastOperationDate, startDate);
                Console.WriteLine(string.Format("Дата начала для обновления: {0}", updateStartDate.ToString("yyyy-MM-dd")));

                // Получаем операции для обновления
                List<BankOperationDto> operationsToAdd = GetOperationsFromApi(updateStartDate, endDate);

                if (operationsToAdd.Count == 0)
                {
                    Console.WriteLine("Новых операций для добавления не найдено");
                    return _dateService.FilterOperationsByDate(existingOperations, startDate, endDate);
                }

                // Объединяем и обновляем данные (устраняем дубликаты)
                List<BankOperationDto> updatedOperations = _dateService.MergeAndUpdateOperations(
                    existingOperations, operationsToAdd, startDate);

                // Сохраняем обновленные данные
                _fileService.SaveStatementToFile(updatedOperations, startDate, endDate);

                Console.WriteLine(string.Format("Выписка успешно обновлена. Всего операций: {0}", updatedOperations.Count));

                // Возвращаем отфильтрованные по запрошенному периоду операции
                return _dateService.FilterOperationsByDate(updatedOperations, startDate, endDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Ошибка при получении выписки: {0}", ex.Message));
                Console.WriteLine("StackTrace: " + ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Создание нового файла выписки
        /// </summary>
        private List<BankOperationDto> CreateNewStatementFile(DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Запрашиваем все операции за указанный период...");
            List<BankOperationDto> operations = GetOperationsFromApi(startDate, endDate);

            if (operations.Count == 0)
            {
                Console.WriteLine("Операции за указанный период не найдены");
                return new List<BankOperationDto>();
            }

            _fileService.SaveStatementToFile(operations, startDate, endDate);
            Console.WriteLine(string.Format("Новый файл выписки создан. Операций: {0}", operations.Count));

            return operations;
        }

        /// <summary>
        /// Получение операций из API с обработкой SSL ошибок
        /// </summary>
        private List<BankOperationDto> GetOperationsFromApi(DateTime startDate, DateTime endDate)
        {
            List<BankOperationDto> allOperations = new List<BankOperationDto>();
            int currentPage = 1;
            bool hasMorePages = true;
            int maxRetries = 2;

            while (hasMorePages)
            {
                Console.WriteLine(string.Format("Запрос страницы {0}...", currentPage));

                bool success = false;
                int retryCount = 0;
                List<BankOperationDto> operations = new List<BankOperationDto>();

                // Повторные попытки при ошибках
                while (!success && retryCount < maxRetries)
                {
                    try
                    {
                        operations = GetOperationsPage(startDate, endDate, currentPage, 1000, (currentPage - 1) * 1000);
                        success = true;
                    }
                    catch (WebException webEx)
                    {
                        retryCount++;
                        Console.WriteLine(string.Format("Ошибка WebException (попытка {0}/{1}): {2}",
                            retryCount, maxRetries, webEx.Message));

                        if (retryCount >= maxRetries)
                            throw;

                        // Пауза перед повторной попыткой
                        System.Threading.Thread.Sleep(1000 * retryCount);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Другая ошибка: " + ex.Message);
                        throw;
                    }
                }

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
                System.Threading.Thread.Sleep(200);
            }

            Console.WriteLine(string.Format("Получено {0} операций из API", allOperations.Count));
            return allOperations;
        }

        /// <summary>
        /// Получение одной страницы операций с правильной кодировкой
        /// </summary>
        private List<BankOperationDto> GetOperationsPage(DateTime startDate, DateTime endDate, int page, int limit, int offset)
        {
            try
            {
                // Формируем URL запроса
                string url = string.Format(
                    "{0}/money/api/v1/Registry?StartDate={1}&EndDate={2}&OperationSource=1&OperationType=102&Limit={3}&Offset={4}",
                    _baseUrl,
                    startDate.ToString("yyyy.MM.dd"),
                    endDate.ToString("yyyy.MM.dd"),
                    limit,
                    offset);

                // Создаем WebRequest
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("md-api-key", _apiKey);
                request.Accept = "application/json";
                request.Timeout = 30000;

                // Получаем ответ
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Console.WriteLine("Получен ответ. Статус: " + response.StatusCode);
                    Console.WriteLine("Кодировка ответа: " + response.CharacterSet);

                    // Используем UTF-8 по умолчанию, но пытаемся определить кодировку из заголовков
                    Encoding encoding = Encoding.UTF8;

                    if (!string.IsNullOrEmpty(response.CharacterSet))
                    {
                        try
                        {
                            // Пробуем получить кодировку из заголовка
                            string charset = response.CharacterSet.ToLower();

                            // Нормализуем названия кодировок
                            if (charset.Contains("utf-8") || charset.Contains("utf8"))
                                encoding = Encoding.UTF8;
                            else if (charset.Contains("windows-1251") || charset.Contains("cp1251"))
                                encoding = Encoding.GetEncoding(1251);
                            else if (charset.Contains("koi8-r") || charset.Contains("koi8r"))
                                encoding = Encoding.GetEncoding(20866);
                            else
                                encoding = Encoding.GetEncoding(response.CharacterSet);

                            Console.WriteLine("Определена кодировка: " + encoding.EncodingName);
                        }
                        catch
                        {
                            encoding = Encoding.UTF8;
                            Console.WriteLine("Не удалось определить кодировку, используем UTF-8");
                        }
                    }

                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream, encoding))
                    {
                        string content = reader.ReadToEnd();
                        var responseText = JObject.Parse(content);
                        Console.WriteLine("Получено " + content.Length + " символов ответа");

                        // Проверяем наличие кириллицы в ответе
                        bool hasCyrillic = content.Contains("Р") || content.Contains("р") ||
                                           content.Contains("С") || content.Contains("с");
                        Console.WriteLine("Ответ содержит кириллицу: " + hasCyrillic);

                        // Парсим JSON
                        return ParseApiResponse(responseText.First.First.ToString());
                    }
                }
            }
            catch (WebException webEx)
            {
                Console.WriteLine(string.Format("Ошибка при запросе к API: {0}", webEx.Message));
                throw;
            }
        }

        /// <summary>
        /// Парсинг ответа API
        /// </summary>
        private List<BankOperationDto> ParseApiResponse(string content)
        {
            try
            {
                Console.WriteLine("Начинаем парсинг ответа API...");

                // Пробуем десериализовать как обернутый ответ {"data": {...}}
                var wrapperResponse = new RegistryApiWrapperResponse();

                wrapperResponse.Data = new RegistryApiResponse();
                wrapperResponse.Data.ResourceList = JsonConvert.DeserializeObject<List<BankOperationDto>>(content);

                if (wrapperResponse != null && wrapperResponse.Data != null && wrapperResponse.Data.ResourceList != null)
                {
                    Console.WriteLine(string.Format("Успешно распарсено через обертку 'data': {0} операций",
                        wrapperResponse.Data.ResourceList.Count));
                    return wrapperResponse.Data.ResourceList;
                }

                Console.WriteLine("Парсинг через обертку 'data' не удался, пробуем прямой парсинг...");

                // Пробуем прямой парсинг (старый формат)
                var directResponse = JsonConvert.DeserializeObject<RegistryApiResponse>(content);

                if (directResponse != null && directResponse.ResourceList != null)
                {
                    Console.WriteLine(string.Format("Успешно распарсено напрямую: {0} операций",
                        directResponse.ResourceList.Count));
                    return directResponse.ResourceList;
                }
                return new List<BankOperationDto>();
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine("Ошибка парсинга JSON: " + jsonEx.Message);
                Console.WriteLine("Первые 500 символов ответа: " +
                                  (content.Length > 500 ? content.Substring(0, 500) + "..." : content));
                return new List<BankOperationDto>();
            }
        }

        /// <summary>
        /// Получение информации о файле выписки
        /// </summary>
        public StatementMetadata GetStatementInfo()
        {
            return _fileService.GetStatementInfo();
        }
    }
}
