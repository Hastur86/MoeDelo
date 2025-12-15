using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MoeDeloRemains.DTO.Accounting;
using MoeDeloRemains.DTO.Contragents;
using MoeDeloRemains.Services.Contragents;
using MoeDeloRemains.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeDeloRemains.Services
{
    /// <summary>
    /// Сервис для обогащения счетов данными о контрагентах
    /// </summary>
    public class BillEnrichmentService
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly BillService _billService;
        private readonly ContragentService _contragentService;
        private readonly BillEnrichmentFileService _fileService;

        public BillEnrichmentService(string apiKey, BillService billService, ContragentService contragentService, string storagePath = null)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _apiKey = apiKey;
            _baseUrl = "https://restapi.moedelo.org";
            _billService = billService ?? throw new ArgumentNullException(nameof(billService));
            _contragentService = contragentService ?? throw new ArgumentNullException(nameof(contragentService));
            _fileService = new BillEnrichmentFileService(storagePath);

            SslHelper.InitializeSslSettings();

            Console.WriteLine("BillEnrichmentService инициализирован");
            Console.WriteLine("Базовый URL: " + _baseUrl);
        }

        /// <summary>
        /// Основной метод: получить обогащенные счета
        /// </summary>
        public List<EnrichedBillDto> GetEnrichedBills()
        {
            try
            {
                Console.WriteLine("Начало получения обогащенных счетов...");

                // 1. Найти последний файл счетов
                string latestBillFile = _fileService.GetLatestBillFile();
                if (string.IsNullOrEmpty(latestBillFile))
                {
                    Console.WriteLine("Файлы счетов не найдены. Запустите сначала BillService.GetBills()");
                    return new List<EnrichedBillDto>();
                }

                Console.WriteLine($"Найден файл счетов: {Path.GetFileName(latestBillFile)}");

                // 2. Получить ID счетов из файла
                List<int> billIds = _fileService.ExtractBillIdsFromFile(latestBillFile);
                if (billIds.Count == 0)
                {
                    Console.WriteLine("В файле счетов не найдены ID счетов");
                    return new List<EnrichedBillDto>();
                }

                Console.WriteLine($"Извлечено {billIds.Count} ID счетов");

                // 3. Получить детальную информацию о счетах через API
                Console.WriteLine("Загрузка детальной информации о счетах из API...");
                List<BillDetailDto> billDetails = GetBillDetailsFromApi(billIds);

                if (billDetails.Count == 0)
                {
                    Console.WriteLine("Не удалось загрузить детальную информацию о счетах");
                    return new List<EnrichedBillDto>();
                }

                Console.WriteLine($"Загружено {billDetails.Count} детализированных счетов");

                // 4. Загрузить контрагентов из кэша
                Console.WriteLine("Загрузка контрагентов из кэша...");
                List<ContragentDto> cachedContragents = _contragentService.GetAllCachedContragents();
                Dictionary<string, ContragentDto> contragentCache = cachedContragents.ToDictionary(c => c.Id);
                Console.WriteLine($"Загружено {contragentCache.Count} контрагентов из кэша");

                // 5. Обогатить счета данными контрагентов
                Console.WriteLine("Обогащение счетов данными контрагентов...");
                List<EnrichedBillDto> enrichedBills = EnrichBillsWithContragents(billDetails, contragentCache);

                // 6. Сохранить результат
                _fileService.SaveEnrichedBills(enrichedBills);

                // 7. Вывести статистику
                PrintEnrichmentStatistics(enrichedBills);

                Console.WriteLine("Получение обогащенных счетов завершено успешно");
                return enrichedBills;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении обогащенных счетов: {ex.Message}");
                Console.WriteLine("StackTrace: " + ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Получить детальную информацию о счетах из API
        /// </summary>
        private List<BillDetailDto> GetBillDetailsFromApi(List<int> billIds)
        {
            List<BillDetailDto> allBillDetails = new List<BillDetailDto>();
            int maxIdsPerRequest = 100; // Лимит API

            // Разбиваем на группы по 100 ID
            var idGroups = SplitIntoChunks(billIds, maxIdsPerRequest);
            int totalGroups = idGroups.Count;

            Console.WriteLine($"Разделено на {totalGroups} запросов к API (по {maxIdsPerRequest} ID в каждом)");

            for (int i = 0; i < totalGroups; i++)
            {
                var idGroup = idGroups[i];
                Console.WriteLine($"[{i + 1}/{totalGroups}] Запрос деталей для {idGroup.Count} счетов...");

                try
                {
                    List<BillDetailDto> groupResult = GetBillDetailsBatch(idGroup);
                    allBillDetails.AddRange(groupResult);
                    Console.WriteLine($"  Успешно загружено: {groupResult.Count} счетов");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Ошибка при запросе группы счетов: {ex.Message}");
                }

                // Задержка между запросами
                if (i < totalGroups - 1)
                {
                    Thread.Sleep(300);
                }
            }

            return allBillDetails;
        }

        /// <summary>
        /// Получить детали счетов для группы ID
        /// </summary>
        private List<BillDetailDto> GetBillDetailsBatch(List<int> billIds)
        {
            try
            {
                string url = $"{_baseUrl}/accounting/api/v1/sales/bill/byIds";

                var requestBody = billIds ;
                string jsonBody = JsonConvert.SerializeObject(requestBody);

                Console.WriteLine($"  Отправка POST запроса на {url}");
                Console.WriteLine($"  Тело запроса (первые 100 символов): {jsonBody.Substring(0, Math.Min(100, jsonBody.Length))}...");

                // Создаем WebRequest
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Headers.Add("md-api-key", _apiKey);
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Timeout = 60000; // 60 секунд

                // Отправляем тело запроса
                byte[] data = Encoding.UTF8.GetBytes(jsonBody);
                request.ContentLength = data.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                }

                // Получаем ответ
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"  Получен ответ. Статус: {response.StatusCode}, Content-Length: {response.ContentLength}");

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            string content = reader.ReadToEnd();
                            var billDetails = JsonConvert.DeserializeObject<List<BillDetailDto>>(content);
                            return billDetails ?? new List<BillDetailDto>();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  Неожиданный статус ответа: {response.StatusCode}");
                        return new List<BillDetailDto>();
                    }
                }
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"  WebException: {webEx.Message}");

                if (webEx.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)webEx.Response)
                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        string errorText = reader.ReadToEnd();
                        Console.WriteLine($"  Детали ошибки: {errorText}");
                    }
                }

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Ошибка: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Обогатить счета данными контрагентов
        /// </summary>
        private List<EnrichedBillDto> EnrichBillsWithContragents(
            List<BillDetailDto> billDetails,
            Dictionary<string, ContragentDto> contragentCache)
        {
            List<EnrichedBillDto> enrichedBills = new List<EnrichedBillDto>();

            int successfullyEnriched = 0;
            int contragentNotFound = 0;
            int contragentIdMismatch = 0;
            List<int> missingContragentIds = new List<int>();

            Console.WriteLine($"Начало обогащения {billDetails.Count} счетов...");

            foreach (var billDetail in billDetails)
            {
                var enrichedBill = new EnrichedBillDto
                {
                    BillDetail = billDetail,
                    Contragent = null,
                    ContragentLoaded = false,
                    ContragentErrorMessage = string.Empty
                };

                try
                {
                    // Преобразуем KontragentId (int) в string для поиска в кэше
                    string kontragentIdString = billDetail.KontragentId.ToString();

                    if (contragentCache.ContainsKey(kontragentIdString))
                    {
                        // Контрагент найден в кэше
                        enrichedBill.Contragent = contragentCache[kontragentIdString];
                        enrichedBill.ContragentLoaded = true;
                        successfullyEnriched++;

                        if (successfullyEnriched <= 5) // Логируем первые 5 для примера
                        {
                            Console.WriteLine($"  Счет #{billDetail.Id}: найден контрагент '{enrichedBill.Contragent.Name}' (ИНН: {enrichedBill.Contragent.Inn})");
                        }
                    }
                    else
                    {
                        // Контрагент не найден в кэше
                        enrichedBill.ContragentLoaded = false;
                        enrichedBill.ContragentErrorMessage = $"Контрагент с ID {kontragentIdString} не найден в кэше";
                        missingContragentIds.Add(billDetail.KontragentId.Value);
                        contragentNotFound++;

                        if (contragentNotFound <= 3) // Логируем первые 3 для примера
                        {
                            Console.WriteLine($"  Счет #{billDetail.Id}: контрагент с ID {kontragentIdString} не найден в кэше");
                        }
                    }
                }
                catch (Exception ex)
                {
                    enrichedBill.ContragentLoaded = false;
                    enrichedBill.ContragentErrorMessage = $"Ошибка при обработке контрагента: {ex.Message}";
                    contragentIdMismatch++;
                }

                enrichedBills.Add(enrichedBill);
            }

            // 6. Загружаем недостающих контрагентов из API
            if (missingContragentIds.Count > 0)
            {
                Console.WriteLine($"\nНайдено {missingContragentIds.Count} счетов с отсутствующими контрагентами в кэше");
                Console.WriteLine("Загрузка недостающих контрагентов из API...");

                int loadedFromApi = 0;
                foreach (var kontragentId in missingContragentIds.Distinct())
                {
                    try
                    {
                        string kontragentIdString = kontragentId.ToString();
                        Console.WriteLine($"  Загрузка контрагента {kontragentIdString} из API...");

                        var contragent = _contragentService.GetContragent(kontragentIdString);
                        if (contragent != null)
                        {
                            // Добавляем в кэш
                            contragentCache[kontragentIdString] = contragent;
                            loadedFromApi++;

                            // Обновляем соответствующие счета
                            foreach (var enrichedBill in enrichedBills)
                            {
                                if (enrichedBill.BillDetail.KontragentId == kontragentId && !enrichedBill.ContragentLoaded)
                                {
                                    enrichedBill.Contragent = contragent;
                                    enrichedBill.ContragentLoaded = true;
                                    enrichedBill.ContragentErrorMessage = string.Empty;
                                }
                            }

                            Console.WriteLine($"    Успешно: {contragent.Name} (ИНН: {contragent.Inn})");
                        }
                        else
                        {
                            Console.WriteLine($"    Не удалось загрузить контрагента {kontragentIdString}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    Ошибка при загрузке контрагента {kontragentId}: {ex.Message}");
                    }

                    // Задержка между запросами
                    Thread.Sleep(200);
                }

                Console.WriteLine($"Загружено {loadedFromApi} контрагентов из API");

                // Обновляем статистику
                successfullyEnriched += loadedFromApi;
                contragentNotFound -= loadedFromApi;
            }

            Console.WriteLine($"\n=== Статистика обогащения счетов ===");
            Console.WriteLine($"Всего счетов: {billDetails.Count}");
            Console.WriteLine($"Успешно обогащено: {successfullyEnriched}");
            Console.WriteLine($"Контрагенты не найдены (всего): {contragentNotFound}");
            Console.WriteLine($"Ошибки сопоставления ID: {contragentIdMismatch}");

            return enrichedBills;
        }

        /// <summary>
        /// Разделить список на части
        /// </summary>
        private List<List<int>> SplitIntoChunks(List<int> source, int chunkSize)
        {
            var chunks = new List<List<int>>();
            for (int i = 0; i < source.Count; i += chunkSize)
            {
                chunks.Add(source.GetRange(i, Math.Min(chunkSize, source.Count - i)));
            }
            return chunks;
        }

        /// <summary>
        /// Вывести статистику обогащения
        /// </summary>
        private void PrintEnrichmentStatistics(List<EnrichedBillDto> enrichedBills)
        {
            var successfullyEnriched = enrichedBills.Count(b => b.ContragentLoaded);
            var failedEnriched = enrichedBills.Count - successfullyEnriched;

            Console.WriteLine("\n=== ИТОГИ ОБОГАЩЕНИЯ СЧЕТОВ ===");
            Console.WriteLine($"Всего обработано счетов: {enrichedBills.Count}");
            Console.WriteLine($"С данными контрагента: {successfullyEnriched}");
            Console.WriteLine($"Без данных контрагента: {failedEnriched}");

            if (successfullyEnriched > 0)
            {
                Console.WriteLine("\nПримеры успешно обогащенных счетов:");
                int exampleCount = 0;
                foreach (var bill in enrichedBills)
                {
                    if (bill.ContragentLoaded && exampleCount < 3)
                    {
                        Console.WriteLine($"  Счет #{bill.BillDetail.Number} от {bill.BillDetail.DocDate:dd.MM.yyyy}");
                        Console.WriteLine($"    Контрагент: {bill.Contragent.Name} (ИНН: {bill.Contragent.Inn})");
                        Console.WriteLine($"    Сумма: {bill.BillDetail.Sum} руб., Оплачено: {bill.BillDetail.PaidSum} руб.");
                        Console.WriteLine($"    Статус: {GetStatusText(bill.BillDetail.Status.Value)}");
                        Console.WriteLine();
                        exampleCount++;
                    }
                }
            }

            if (failedEnriched > 0)
            {
                Console.WriteLine("\nСчета без данных контрагента (первые 5):");
                int exampleCount = 0;
                foreach (var bill in enrichedBills)
                {
                    if (!bill.ContragentLoaded && exampleCount < 5)
                    {
                        Console.WriteLine($"  Счет #{bill.BillDetail.Id} (KontragentId: {bill.BillDetail.KontragentId}): {bill.ContragentErrorMessage}");
                        exampleCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Получить текстовое описание статуса счета
        /// </summary>
        private string GetStatusText(int status)
        {
            switch (status)
            {
                case 4: return "Неоплачен";
                case 5: return "Частично оплачен";
                case 6: return "Оплачен";
                default: return $"Неизвестный ({status})";
            }
        }
    }
}