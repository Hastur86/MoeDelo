using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MoeDeloRemains.Utils
{
    /// <summary>
    /// Утилиты для настройки SSL/TLS соединений
    /// </summary>
    public static class SslHelper
    {
        /// <summary>
        /// Инициализация настроек безопасности для работы с HTTPS
        /// </summary>
        public static void InitializeSslSettings()
        {
            try
            {
                // Принудительно включаем поддержку TLS 1.2 и TLS 1.1
                ServicePointManager.SecurityProtocol =
                    (SecurityProtocolType)3072 |  // TLS 1.2
                    (SecurityProtocolType)768 |   // TLS 1.1
                    SecurityProtocolType.Tls;     // TLS 1.0

                // Отключаем проверку сертификатов (для тестирования)
                ServicePointManager.ServerCertificateValidationCallback =
                    new RemoteCertificateValidationCallback(AcceptAllCertifications);

                // Настройка повторных попыток
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.DefaultConnectionLimit = 9999;

                Console.WriteLine("Настройки SSL инициализированы");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка инициализации SSL: " + ex.Message);
            }
        }

        /// <summary>
        /// Callback для принятия всех сертификатов
        /// </summary>
        private static bool AcceptAllCertifications(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true; // Принимаем все сертификаты
        }
    }
}