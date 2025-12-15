using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoeDeloRemains.DTO.Accounting;

namespace MoeDeloRemains.Services
{
    /// <summary>
    /// Сервис для работы с файлами счетов
    /// </summary>
    public class BillFileService
    {
        private readonly string _storagePath;

        /// <summary>
        /// Конструктор
        /// </summary>
        public BillFileService(string storagePath = null)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bills");
            }
            else
            {
                _storagePath = storagePath;
            }

            // Создаем директорию если не существует
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            Console.WriteLine("BillFileService инициализирован");
            Console.WriteLine("Путь для сохранения: " + _storagePath);
        }

        /// <summary>
        /// Сохранить счета в файл (перезаписывает существующий)
        /// </summary>
        public void SaveBillsToFile(List<BillDto> bills, DateTime startDate, DateTime endDate)
        {
            try
            {
                string fileName = $"bills_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(_storagePath, fileName);

                var dataToSave = new
                {
                    ExportDate = DateTime.Now,
                    StartDate = startDate,
                    EndDate = endDate,
                    BillsCount = bills.Count,
                    Bills = bills
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(dataToSave, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);

                Console.WriteLine($"Счета сохранены в файл: {filePath}");
                Console.WriteLine($"Сохранено счетов: {bills.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении счетов в файл: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Получить информацию о последнем сохраненном файле
        /// </summary>
        public string GetLastFileInfo()
        {
            try
            {
                var files = Directory.GetFiles(_storagePath, "bills_*.json");
                if (files.Length == 0)
                {
                    return "Файлы счетов не найдены";
                }

                var lastFile = files.OrderByDescending(f => f).First();
                var fileInfo = new FileInfo(lastFile);

                return $"Последний файл: {fileInfo.Name}, Размер: {fileInfo.Length} байт, Изменен: {fileInfo.LastWriteTime}";
            }
            catch (Exception ex)
            {
                return $"Ошибка при получении информации о файлах: {ex.Message}";
            }
        }

        /// <summary>
        /// Очистить старые файлы (оставляет только последние 5)
        /// </summary>
        public void CleanOldFiles()
        {
            try
            {
                var files = Directory.GetFiles(_storagePath, "bills_*.json");
                if (files.Length > 5)
                {
                    var filesToDelete = files.OrderBy(f => f).Take(files.Length - 5);
                    foreach (var file in filesToDelete)
                    {
                        File.Delete(file);
                        Console.WriteLine($"Удален старый файл: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при очистке старых файлов: {ex.Message}");
            }
        }
    }
}