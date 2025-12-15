using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoeDeloRemains.DTO.Accounting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeDeloRemains.Services
{
    /// <summary>
    /// Сервис для работы с файлами обогащенных счетов
    /// </summary>
    public class BillEnrichmentFileService
    {
        private readonly string _storagePath;

        public BillEnrichmentFileService(string storagePath = null)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                // Используем ту же папку, что и BillService
                _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bills");
            }
            else
            {
                _storagePath = storagePath;
            }

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            Console.WriteLine("BillEnrichmentFileService инициализирован");
            Console.WriteLine("Путь для сохранения: " + _storagePath);
        }

        /// <summary>
        /// Найти последний файл счетов
        /// </summary>
        public string GetLatestBillFile()
        {
            try
            {
                var files = Directory.GetFiles(_storagePath, "bills_*.json");
                if (files.Length == 0)
                {
                    Console.WriteLine("Файлы счетов не найдены в папке: " + _storagePath);
                    return null;
                }

                var latestFile = files.OrderByDescending(f => f).First();
                Console.WriteLine($"Найден последний файл счетов: {Path.GetFileName(latestFile)}");
                return latestFile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске файлов счетов: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Извлечь ID счетов из файла
        /// </summary>
        public List<int> ExtractBillIdsFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл не найден: {filePath}");
                    return new List<int>();
                }

                Console.WriteLine($"Чтение файла счетов: {Path.GetFileName(filePath)}");
                string json = File.ReadAllText(filePath);

                // Парсим JSON чтобы получить список счетов
                var jsonObject = JObject.Parse(json);
                var billsArray = jsonObject["Bills"] as JArray;

                if (billsArray == null || !billsArray.Any())
                {
                    Console.WriteLine("В файле не найдены счета (Bills)");
                    return new List<int>();
                }

                List<int> billIds = new List<int>();
                foreach (var bill in billsArray)
                {
                    var id = bill["Id"]?.Value<int>();
                    if (id.HasValue)
                    {
                        billIds.Add(id.Value);
                    }
                }

                Console.WriteLine($"Извлечено {billIds.Count} ID счетов");
                return billIds;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Ошибка парсинга JSON файла счетов: {jsonEx.Message}");
                return new List<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла счетов: {ex.Message}");
                return new List<int>();
            }
        }

        /// <summary>
        /// Сохранить обогащенные счета в файл
        /// </summary>
        public void SaveEnrichedBills(List<EnrichedBillDto> enrichedBills)
        {
            try
            {
                string fileName = $"enriched_bills_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(_storagePath, fileName);

                var dataToSave = new
                {
                    ExportDate = DateTime.Now,
                    TotalBills = enrichedBills.Count,
                    BillsWithContragent = enrichedBills.Count(b => b.ContragentLoaded),
                    BillsWithoutContragent = enrichedBills.Count(b => !b.ContragentLoaded),
                    EnrichedBills = enrichedBills.Select(b => new
                    {
                        BillId = b.BillDetail.Id,
                        BillNumber = b.BillDetail.Number,
                        BillDate = b.BillDetail.DocDate,
                        BillSum = b.BillDetail.Sum,
                        BillStatus = b.BillDetail.Status,
                        ContragentName = b.Contragent?.Name ?? "Неизвестно",
                        ContragentInn = b.Contragent?.Inn ?? "Неизвестно",
                        ContragentLoaded = b.ContragentLoaded,
                        ErrorMessage = b.ContragentErrorMessage
                    })
                };

                string json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Console.WriteLine($"Обогащенные счета сохранены в файл: {filePath}");
                Console.WriteLine($"Сохранено счетов: {enrichedBills.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении обогащенных счетов: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Очистить старые файлы обогащенных счетов (оставляет последние 3)
        /// </summary>
        public void CleanOldEnrichedFiles()
        {
            try
            {
                var files = Directory.GetFiles(_storagePath, "enriched_bills_*.json");
                if (files.Length > 3)
                {
                    var filesToDelete = files.OrderBy(f => f).Take(files.Length - 3);
                    foreach (var file in filesToDelete)
                    {
                        File.Delete(file);
                        Console.WriteLine($"Удален старый файл обогащенных счетов: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при очистке старых файлов обогащенных счетов: {ex.Message}");
            }
        }
    }
}