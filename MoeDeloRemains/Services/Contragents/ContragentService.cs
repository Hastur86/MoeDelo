// [file name]: ContragentService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using MoeDeloRemains.DTO.Contragents;
using MoeDeloRemains.DTO.Mony;
using MoeDeloRemains.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeDeloRemains.Services.Contragents
{
    /// <summary>
    /// Сервис для работы с контрагентами через API "Моё Дело"
    /// </summary>
    public class ContragentService
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ContragentFileService _fileService;

        /// <summary>
        /// Конструктор сервиса
        /// </summary>
        public ContragentService(string apiKey, string baseUrl = "https://restapi.moedelo.org",
                                string storagePath = null)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException("apiKey");

            _apiKey = apiKey;
            _baseUrl = baseUrl.TrimEnd('/');
            _fileService = new ContragentFileService(storagePath);

            // Инициализируем SSL настройки (используем тот же фикс)
            SslHelper.InitializeSslSettings();

            Console.WriteLine("Сервис контрагентов инициализирован");
            Console.WriteLine("Базовый URL: " + _baseUrl);
        }

        /// <summary>
        /// Основной метод: получить данные о контрагентах из выписки
        /// </summary>
        /// <param name="statementFilePath">Путь к файлу банковской выписки</param>
        /// <returns>Список операций с данными контрагентов</returns>
        public List<BankOperationWithContragentDto> GetContragentsFromStatement(string statementFilePath)
        {
            try
            {
                Console.WriteLine("Начало обработки контрагентов из выписки");
                Console.WriteLine("Путь к файлу выписки: " + statementFilePath);

                // 1. Получаем уникальные ID контрагентов из выписки
                List<string> contragentIds = _fileService.GetUniqueContragentIdsFromStatement(statementFilePath);

                if (contragentIds.Count == 0)
                {
                    Console.WriteLine("В выписке не найдены контрагенты");
                    return new List<BankOperationWithContragentDto>();
                }

                Console.WriteLine($"Найдено {contragentIds.Count} уникальных контрагентов для загрузки");

                // 2. Загружаем существующие контрагенты из кэша
                List<ContragentDto> cachedContragents = _fileService.LoadContragentsFromFile();
                Dictionary<string, ContragentDto> contragentCache = new Dictionary<string, ContragentDto>();
                foreach (var contragent in cachedContragents)
                {
                    contragentCache[contragent.Id] = contragent;
                }

                Console.WriteLine($"Загружено {contragentCache.Count} контрагентов из кэша");

                // 3. Определяем, каких контрагентов нужно загрузить из API
                List<string> idsToLoad = new List<string>();
                foreach (var id in contragentIds)
                {
                    if (!contragentCache.ContainsKey(id))
                    {
                        idsToLoad.Add(id);
                    }
                }

                Console.WriteLine($"Требуется загрузить из API: {idsToLoad.Count} контрагентов");

                // 4. Загружаем контрагентов из API
                if (idsToLoad.Count > 0)
                {
                    List<ContragentDto> loadedContragents = LoadContragentsFromApi(idsToLoad);

                    // Добавляем загруженных контрагентов в кэш
                    foreach (var contragent in loadedContragents)
                    {
                        contragentCache[contragent.Id] = contragent;
                    }

                    // Обновляем файл кэша
                    _fileService.SaveContragentsToFile(new List<ContragentDto>(contragentCache.Values));
                }

                // 5. Загружаем операции из выписки и обогащаем их данными контрагентов
                List<BankOperationWithContragentDto> enrichedOperations =
                    EnrichOperationsWithContragents(statementFilePath, contragentCache);

                // 6. Сохраняем обогащенные операции в отдельный файл
                _fileService.SaveOperationsWithContragents(enrichedOperations);

                Console.WriteLine("Обработка контрагентов завершена успешно");
                return enrichedOperations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке контрагентов: {ex.Message}");
                Console.WriteLine("StackTrace: " + ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Загрузить контрагентов из API
        /// </summary>
        private List<ContragentDto> LoadContragentsFromApi(List<string> contragentIds)
        {
            List<ContragentDto> loadedContragents = new List<ContragentDto>();
            int total = contragentIds.Count;

            Console.WriteLine($"Начинаем загрузку {total} контрагентов из API...");

            for (int i = 0; i < total; i++)
            {
                string contragentId = contragentIds[i];

                try
                {
                    Console.WriteLine($"[{i + 1}/{total}] Загрузка контрагента {contragentId}...");

                    ContragentDto contragent = GetContragentById(contragentId);

                    if (contragent != null)
                    {
                        loadedContragents.Add(contragent);
                        Console.WriteLine($"  Успешно: {contragent.Name} (ИНН: {contragent.Inn})");
                    }
                    else
                    {
                        Console.WriteLine($"  Не удалось загрузить контрагента {contragentId}");
                    }
                }
                catch (WebException webEx)
                {
                    HttpWebResponse response = webEx.Response as HttpWebResponse;
                    if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                    {
                        Console.WriteLine($"  Контрагент {contragentId} не найден (404)");
                    }
                    else
                    {
                        Console.WriteLine($"  Ошибка при загрузке контрагента {contragentId}: {webEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Ошибка при загрузке контрагента {contragentId}: {ex.Message}");
                }

                // Задержка между запросами, чтобы не перегружать API
                if (i < total - 1)
                {
                    Thread.Sleep(100); // 100 мс задержка
                }
            }

            Console.WriteLine($"Загружено {loadedContragents.Count} из {total} контрагентов");
            return loadedContragents;
        }

        /// <summary>
        /// Получить контрагента по ID через API
        /// </summary>
        private ContragentDto GetContragentById(string contragentId)
        {
            try
            {
                // Формируем URL запроса согласно документации API
                string url = $"{_baseUrl}/kontragents/api/v1/kontragent/{contragentId}";

                // Создаем WebRequest
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("md-api-key", _apiKey);
                request.Accept = "application/json";
                request.Timeout = 30000; // 30 секунд таймаут

                // Получаем ответ
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // Используем UTF-8 для чтения ответа
                        using (Stream responseStream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            string content = reader.ReadToEnd();
                            var apiResponse = JsonConvert.DeserializeObject<ContragentDto>(content);

                            if (apiResponse != null)
                            {
                                return apiResponse;
                            }
                        }
                    }
                }

                return null;
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"WebException при запросе контрагента {contragentId}: {webEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запросе контрагента {contragentId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Обогатить операции данными контрагентов
        /// </summary>
        private List<BankOperationWithContragentDto> EnrichOperationsWithContragents(
            string statementFilePath,
            Dictionary<string, ContragentDto> contragentCache)
        {
            try
            {
                Console.WriteLine("Начало обогащения операций данными контрагентов...");

                // Загружаем выписку как массив BankOperationDto
                string json = File.ReadAllText(statementFilePath);
                var operations = JsonConvert.DeserializeObject<List<BankOperationDto>>(json);

                if (operations == null || operations.Count == 0)
                {
                    Console.WriteLine("Не удалось загрузить операции из файла выписки");
                    return new List<BankOperationWithContragentDto>();
                }

                Console.WriteLine($"Обнаружено {operations.Count} операций для обогащения");

                // Создаем список обогащенных операций
                List<BankOperationWithContragentDto> enrichedOperations = new List<BankOperationWithContragentDto>();

                int enrichedCount = 0;
                int notFoundCount = 0;
                int noContractorCount = 0;
                int invalidIdCount = 0;

                foreach (var operation in operations)
                {
                    // Создаем обогащенную операцию - нужно скопировать все свойства
                    // Используем сериализацию/десериализацию для копирования
                    var enrichedOperation = JsonConvert.DeserializeObject<BankOperationWithContragentDto>(
                        JsonConvert.SerializeObject(operation));

                    // Инициализируем дополнительные поля
                    enrichedOperation.Contragent = null;
                    enrichedOperation.ContragentLoaded = false;
                    enrichedOperation.ContragentErrorMessage = string.Empty;

                    // Проверяем наличие контрагента в операции
                    if (operation.Contractor != null && !string.IsNullOrEmpty(operation.Contractor.Id))
                    {
                        string contractorIdString = operation.Contractor.Id;

                        try
                        {
                            string contractorId = contractorIdString;

                            if (contragentCache.ContainsKey(contractorId))
                            {
                                // Нашли контрагента в кэше
                                enrichedOperation.Contragent = contragentCache[contractorId];
                                enrichedOperation.ContragentLoaded = true;
                                enrichedCount++;

                                if (enrichedCount <= 10) // Логируем только первые 10 для примера
                                {
                                    Console.WriteLine($"  Операция #{operation.Id}: найден контрагент '{enrichedOperation.Contragent.Name}'");
                                }
                            }
                            else
                            {
                                // Контрагент не найден в кэше
                                enrichedOperation.ContragentLoaded = false;
                                enrichedOperation.ContragentErrorMessage = $"Контрагент с ID {contractorId} не найден в кэше";
                                notFoundCount++;

                                if (notFoundCount <= 5) // Логируем только первые 5 для примера
                                {
                                    Console.WriteLine($"  Операция #{operation.Id}: контрагент с ID {contractorId} не найден в кэше");
                                }
                            }
                        }
                        catch (FormatException)
                        {
                            // Некорректный формат GUID
                            enrichedOperation.ContragentLoaded = false;
                            enrichedOperation.ContragentErrorMessage = $"Некорректный формат ID контрагента: '{contractorIdString}'";
                            invalidIdCount++;

                            if (invalidIdCount <= 3) // Логируем только первые 3 для примера
                            {
                                Console.WriteLine($"  Операция #{operation.Id}: некорректный формат ID '{contractorIdString}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            enrichedOperation.ContragentLoaded = false;
                            enrichedOperation.ContragentErrorMessage = $"Ошибка обработки ID контрагента: {ex.Message}";
                            invalidIdCount++;
                        }
                    }
                    else
                    {
                        // В операции нет информации о контрагенте
                        enrichedOperation.ContragentLoaded = false;
                        enrichedOperation.ContragentErrorMessage = "В операции не указан контрагент";
                        noContractorCount++;
                    }

                    enrichedOperations.Add(enrichedOperation);
                }

                Console.WriteLine($"\n=== Итоги обогащения операций ===");
                Console.WriteLine($"Всего операций: {operations.Count}");
                Console.WriteLine($"Успешно обогащено: {enrichedCount}");
                Console.WriteLine($"Контрагенты не найдены в кэше: {notFoundCount}");
                Console.WriteLine($"Операций без контрагента: {noContractorCount}");
                Console.WriteLine($"Некорректных ID контрагентов: {invalidIdCount}");

                // Выводим примеры обогащенных операций
                if (enrichedCount > 0)
                {
                    Console.WriteLine("\nПримеры успешно обогащенных операций:");
                    int exampleCount = 0;
                    foreach (var op in enrichedOperations)
                    {
                        if (op.ContragentLoaded && exampleCount < 5)
                        {
                            Console.WriteLine($"  #{op.Id} от {op.Date:dd.MM.yyyy}: {op.Contragent.Name} (ИНН: {op.Contragent.Inn}) - {op.Sum} руб.");
                            exampleCount++;
                        }
                    }
                }

                return enrichedOperations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обогащении операций: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new List<BankOperationWithContragentDto>();
            }
        }

        /// <summary>
        /// Получить контрагента по ID (публичный метод для внешнего использования)
        /// </summary>
        public ContragentDto GetContragent(string contragentId)
        {
            return GetContragentById(contragentId);
        }

        /// <summary>
        /// Получить всех контрагентов из кэша
        /// </summary>
        public List<ContragentDto> GetAllCachedContragents()
        {
            return _fileService.LoadContragentsFromFile();
        }
    }
}