using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MoeDeloRemains.DTO.Accounting;
using MoeDeloRemains.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeDeloRemains.Services
{
    /// <summary>
    /// Сервис для работы со счетами
    /// </summary>
    public class BillService
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly BillFileService _fileService;

        /// <summary>
        /// Конструктор сервиса
        /// </summary>
        public BillService(string apiKey, string baseUrl = "https://restapi.moedelo.org", string storagePath = null)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException("apiKey");

            _apiKey = apiKey;
            _baseUrl = baseUrl.TrimEnd('/');
            _fileService = new BillFileService(storagePath);

            // Инициализируем SSL настройки
            SslHelper.InitializeSslSettings();

            Console.WriteLine("BillService инициализирован");
            Console.WriteLine("Базовый URL: " + _baseUrl);
        }

        /// <summary>
        /// Получить все счета за период
        /// </summary>
        public List<BillDto> GetBills(DateTime startDate, DateTime endDate)
        {
            try
            {
                Console.WriteLine($"Начало получения счетов за период: с {startDate:yyyy-MM-dd} по {endDate:yyyy-MM-dd}");

                // Получаем все счета из API
                List<BillDto> allBills = GetAllBillsFromApi(startDate, endDate);

                if (allBills.Count == 0)
                {
                    Console.WriteLine("Счета за указанный период не найдены");
                    return new List<BillDto>();
                }

                // Сохраняем в файл (перезаписываем)
                _fileService.SaveBillsToFile(allBills, startDate, endDate);

                // Очищаем старые файлы
                _fileService.CleanOldFiles();

                Console.WriteLine($"Получено счетов: {allBills.Count}");
                return allBills;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении счетов: {ex.Message}");
                Console.WriteLine("StackTrace: " + ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Получить счета за последний год
        /// </summary>
        public List<BillDto> GetBillsLastYear()
        {
            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate.AddYears(-1);
            return GetBills(startDate, endDate);
        }

        /// <summary>
        /// Получить все счета из API с пагинацией
        /// </summary>
        private List<BillDto> GetAllBillsFromApi(DateTime startDate, DateTime endDate)
        {
            List<BillDto> allBills = new List<BillDto>();
            int currentPage = 1;
            bool hasMorePages = true;
            int maxRetries = 3;

            while (hasMorePages)
            {
                Console.WriteLine($"Запрос страницы {currentPage}...");

                bool success = false;
                int retryCount = 0;
                List<BillDto> bills = new List<BillDto>();

                // Повторные попытки при ошибках
                while (!success && retryCount < maxRetries)
                {
                    try
                    {
                        bills = GetBillsPage(startDate, endDate, currentPage, 100, (currentPage - 1) * 100);
                        success = true;
                    }
                    catch (WebException webEx)
                    {
                        retryCount++;
                        Console.WriteLine($"Ошибка WebException (попытка {retryCount}/{maxRetries}): {webEx.Message}");

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

                if (bills.Count > 0)
                {
                    allBills.AddRange(bills);
                    currentPage++;
                }
                else
                {
                    hasMorePages = false;
                }

                // Если получено меньше запрошенного лимита
                if (bills.Count < 100)
                {
                    hasMorePages = false;
                }

                // Небольшая задержка между запросами
                System.Threading.Thread.Sleep(300);
            }

            Console.WriteLine($"Получено {allBills.Count} счетов из API");
            return allBills;
        }

        /// <summary>
        /// Получение одной страницы счетов
        /// </summary>
        private List<BillDto> GetBillsPage(DateTime startDate, DateTime endDate, int page, int limit, int offset)
        {
            try
            {
                // Формируем URL запроса для счетов
                string url = string.Format(
                    "{0}/accounting/api/v1/sales/bill?docAfterDate={1}&docBeforeDate={2}&pageNo={3}&pageSize=100",
                    _baseUrl,
                    startDate.ToString("yyyy.MM.dd"),
                    endDate.ToString("yyyy.MM.dd"),
                    page);

                Console.WriteLine($"URL запроса: {url}");

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

                    // Используем UTF-8
                    Encoding encoding = Encoding.UTF8;

                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream, encoding))
                    {
                        string content = reader.ReadToEnd();
                        var responseText = JObject.Parse(content);
                        Console.WriteLine("Получено " + content.Length + " символов ответа");

                        // Парсим JSON ответ
                        return ParseApiResponse(responseText.ToString());
                    }
                }
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"Ошибка при запросе к API: {webEx.Message}");
                
                // Получаем детали ошибки
                if (webEx.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)webEx.Response)
                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        string errorText = reader.ReadToEnd();
                        Console.WriteLine("Детали ошибки: " + errorText);
                    }
                }
                
                throw;
            }
        }

        /// <summary>
        /// Парсинг ответа API счетов
        /// </summary>
        private List<BillDto> ParseApiResponse(string content)
        {
            try
            {
                Console.WriteLine("Начинаем парсинг ответа API счетов...");

                // Пробуем десериализовать как обернутый ответ {"data": {...}}
                try
                {
                    var wrapperResponse = JsonConvert.DeserializeObject<BillApiResponse>(content);
                    if (wrapperResponse != null && wrapperResponse.ResourceList != null)
                    {
                        Console.WriteLine($"Успешно распарсено через обертку 'data': {wrapperResponse.ResourceList.Count} счетов");
                        return wrapperResponse.ResourceList;
                    }
                }
                catch (JsonException) { }

                // Пробуем прямой парсинг
                try
                {
                    var directResponse = JsonConvert.DeserializeObject<BillApiResponse>(content);
                    if (directResponse != null && directResponse.ResourceList != null)
                    {
                        Console.WriteLine($"Успешно распарсено напрямую: {directResponse.ResourceList.Count} счетов");
                        return directResponse.ResourceList;
                    }
                }
                catch (JsonException) { }

                // Пробуем десериализовать напрямую как список
                try
                {
                    var list = JsonConvert.DeserializeObject<List<BillDto>>(content);
                    if (list != null)
                    {
                        Console.WriteLine($"Успешно распарсено как список: {list.Count} счетов");
                        return list;
                    }
                }
                catch (JsonException) { }

                Console.WriteLine("Не удалось распарсить ответ API счетов");
                return new List<BillDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка парсинга JSON: " + ex.Message);
                Console.WriteLine("Первые 500 символов ответа: " +
                                  (content.Length > 500 ? content.Substring(0, 500) + "..." : content));
                return new List<BillDto>();
            }
        }

        /// <summary>
        /// Получить информацию о сохраненных файлах
        /// </summary>
        public string GetFileInfo()
        {
            return _fileService.GetLastFileInfo();
        }
    }
}