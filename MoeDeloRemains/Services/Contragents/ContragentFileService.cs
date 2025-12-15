// [file name]: ContragentFileService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoeDeloRemains.DTO.Contragents;
using MoeDeloRemains.DTO.Mony;
using Newtonsoft.Json;

namespace MoeDeloRemains.Services.Contragents
{
    /// <summary>
    /// Сервис для работы с файлами контрагентов
    /// </summary>
    public class ContragentFileService
    {
        private readonly string _storagePath;

        /// <summary>
        /// Конструктор сервиса
        /// </summary>
        /// <param name="storagePath">Путь для сохранения файлов (null = текущая директория)</param>
        public ContragentFileService(string storagePath = null)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                _storagePath = Directory.GetCurrentDirectory();
            }
            else
            {
                _storagePath = storagePath;
                if (!Directory.Exists(_storagePath))
                {
                    Directory.CreateDirectory(_storagePath);
                }
            }

            Console.WriteLine("Сервис файлов контрагентов инициализирован");
            Console.WriteLine("Путь сохранения: " + _storagePath);
        }

        /// <summary>
        /// Получить путь к файлу контрагентов
        /// </summary>
        /// <returns>Полный путь к файлу</returns>
        public string GetContragentsFilePath()
        {
            string fileName = "contragents_data.json";
            return Path.Combine(_storagePath, fileName);
        }

        /// <summary>
        /// Проверить существование файла контрагентов
        /// </summary>
        /// <returns>True если файл существует</returns>
        public bool ContragentsFileExists()
        {
            string filePath = GetContragentsFilePath();
            return File.Exists(filePath);
        }

        /// <summary>
        /// Загрузить контрагентов из файла
        /// </summary>
        /// <returns>Список контрагентов</returns>
        public List<ContragentDto> LoadContragentsFromFile()
        {
            try
            {
                string filePath = GetContragentsFilePath();

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("Файл контрагентов не найден: " + filePath);
                    return new List<ContragentDto>();
                }

                string json = File.ReadAllText(filePath);
                var contragents = JsonConvert.DeserializeObject<List<ContragentDto>>(json);

                Console.WriteLine($"Загружено {contragents.Count} контрагентов из файла");
                return contragents ?? new List<ContragentDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке контрагентов из файла: {ex.Message}");
                return new List<ContragentDto>();
            }
        }

        /// <summary>
        /// Сохранить контрагентов в файл
        /// </summary>
        /// <param name="contragents">Список контрагентов для сохранения</param>
        public void SaveContragentsToFile(List<ContragentDto> contragents)
        {
            try
            {
                string filePath = GetContragentsFilePath();

                // Сортируем по имени для удобства
                var sortedContragents = contragents
                    .OrderBy(c => c.Name)
                    .ToList();

                string json = JsonConvert.SerializeObject(sortedContragents, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Console.WriteLine($"Сохранено {sortedContragents.Count} контрагентов в файл: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении контрагентов в файл: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Получить уникальные ID контрагентов из выписки
        /// </summary>
        /// <param name="statementFilePath">Путь к файлу выписки</param>
        /// <returns>Список уникальных ID контрагентов</returns>
        public List<string> GetUniqueContragentIdsFromStatement(string statementFilePath)
        {
            try
            {
                if (!File.Exists(statementFilePath))
                {
                    Console.WriteLine($"Файл выписки не найден: {statementFilePath}");
                    return new List<string>();
                }

                Console.WriteLine($"Чтение файла выписки: {statementFilePath}");

                // Читаем JSON из файла
                string json = File.ReadAllText(statementFilePath);

                // Десериализуем как массив BankOperationDto
                var operations = JsonConvert.DeserializeObject<List<BankOperationDto>>(json);

                if (operations == null || operations.Count == 0)
                {
                    Console.WriteLine("В файле выписки нет операций или формат файла неверный");
                    return new List<string>();
                }

                Console.WriteLine($"Прочитано {operations.Count} операций из файла выписки");

                // Используем HashSet для автоматического удаления дубликатов
                HashSet<string> uniqueContragentIds = new HashSet<string>();
                int operationsWithContractor = 0;
                int operationsWithoutContractor = 0;
                int invalidIdFormat = 0;

                foreach (var operation in operations)
                {
                    // Проверяем наличие ContractorDto
                    if (operation.Contractor != null && !string.IsNullOrEmpty(operation.Contractor.Id))
                    {
                        string contractorIdString = operation.Contractor.Id;

                        try
                        {
                            // Пытаемся преобразовать string в string
                            string contractorId = contractorIdString;

                            // Добавляем в HashSet (дубликаты игнорируются автоматически)
                            if (uniqueContragentIds.Add(contractorId))
                            {
                                Console.WriteLine($"  Найден контрагент: ID={contractorId}, Name='{operation.Contractor.Name}'");
                            }
                            operationsWithContractor++;
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine($"  ВНИМАНИЕ: Некорректный формат ID контрагента: '{contractorIdString}'");
                            invalidIdFormat++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  Ошибка при обработке ID контрагента '{contractorIdString}': {ex.Message}");
                            invalidIdFormat++;
                        }
                    }
                    else
                    {
                        operationsWithoutContractor++;
                    }
                }

                // Преобразуем HashSet обратно в List
                List<string> result = new List<string>(uniqueContragentIds);

                Console.WriteLine($"\n=== Статистика по операциям ===");
                Console.WriteLine($"  Всего операций: {operations.Count}");
                Console.WriteLine($"  Операций с контрагентом: {operationsWithContractor}");
                Console.WriteLine($"  Операций без контрагента: {operationsWithoutContractor}");
                Console.WriteLine($"  Некорректных ID: {invalidIdFormat}");
                Console.WriteLine($"  Уникальных ID контрагентов: {result.Count}");

                // Логируем ID контрагентов для отладки
                if (result.Count > 0)
                {
                    Console.WriteLine("\nСписок уникальных ID контрагентов:");
                    foreach (var id in result)
                    {
                        Console.WriteLine($"  - {id}");
                    }
                }
                else
                {
                    Console.WriteLine("\nВНИМАНИЕ: Не найдено ни одного ID контрагента!");

                    // Для отладки: выводим структуру первых 3 операций
                    int sampleCount = Math.Min(3, operations.Count);
                    Console.WriteLine($"\nСтруктура первых {sampleCount} операций для проверки:");

                    for (int i = 0; i < sampleCount; i++)
                    {
                        var op = operations[i];
                        Console.WriteLine($"  Операция #{i + 1}:");
                        Console.WriteLine($"    Id: {op.Id}");
                        Console.WriteLine($"    Date: {op.Date}");
                        Console.WriteLine($"    Sum: {op.Sum}");

                        if (op.Contractor != null)
                        {
                            Console.WriteLine($"    Contractor.Id: '{op.Contractor.Id}' (тип: {op.Contractor.Id?.GetType()?.Name})");
                            Console.WriteLine($"    Contractor.Name: '{op.Contractor.Name}'");
                            Console.WriteLine($"    Contractor.Type: {op.Contractor.Type}");
                        }
                        else
                        {
                            Console.WriteLine($"    Contractor: null");
                        }
                        Console.WriteLine();
                    }
                }

                return result;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Ошибка парсинга JSON файла выписки: {jsonEx.Message}");

                // Пытаемся прочитать часть файла для диагностики
                try
                {
                    string fileContent = File.ReadAllText(statementFilePath);
                    int sampleLength = Math.Min(500, fileContent.Length);
                    string sample = fileContent.Substring(0, sampleLength);
                    Console.WriteLine($"Первые {sampleLength} символов файла:\n{sample}");

                    // Также покажем последние 200 символов
                    if (fileContent.Length > 200)
                    {
                        Console.WriteLine($"\nПоследние 200 символов файла:\n{fileContent.Substring(fileContent.Length - 200)}");
                    }
                }
                catch (Exception readEx)
                {
                    Console.WriteLine($"Не удалось прочитать файл для диагностики: {readEx.Message}");
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла выписки: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Сохранить операции с контрагентами в отдельный файл
        /// </summary>
        /// <param name="operations">Список операций с данными контрагентов</param>
        public void SaveOperationsWithContragents(List<BankOperationWithContragentDto> operations)
        {
            try
            {
                string fileName = $"operations_with_contragents_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(_storagePath, fileName);

                var enrichedOperations = operations.Select(op => new
                {
                    Operation = op,
                    ContragentName = op.Contragent?.Name ?? "Неизвестно",
                    ContragentInn = op.Contragent?.Inn ?? "Неизвестно",
                    ContragentLoaded = op.ContragentLoaded,
                    ErrorMessage = op.ContragentErrorMessage
                }).ToList();

                string json = JsonConvert.SerializeObject(enrichedOperations, Formatting.Indented);
                File.WriteAllText(filePath, json);

                int loadedCount = operations.Count(op => op.ContragentLoaded);
                int failedCount = operations.Count - loadedCount;

                Console.WriteLine($"Сохранено {operations.Count} операций с контрагентами в файл: {filePath}");
                Console.WriteLine($"Успешно загружено контрагентов: {loadedCount}");
                Console.WriteLine($"Не удалось загрузить: {failedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении операций с контрагентами: {ex.Message}");
                throw;
            }
        }
    }
}