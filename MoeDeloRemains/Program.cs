using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MoeDeloRemains.DTO.Mony;
using MoeDeloRemains.ORP;
using MoeDeloRemains.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeDeloRemains
{
    public class GoodRepresentation
    {
        public int Id { get; set; }// (integer) : Идентификатор товара(материала) ,
        public int NomenclatureId { get; set; } //(integer) : Идентификатор группы товаров(материалов) ,
        public string Name { get; set; }  //(string) : Наименование товара(материала) ,
        public String Modification { get; set; }// (string, optional) : Модификация товара(материала) ,
        public String Article { get; set; } //: Артикул ,
        public string UnitOfMeasurement { get; set; } // (string) : Единица измерения,
        public int Nds { get; set; } // (integer): Ставка НДС: -1 - без НДС, 2 - 0%, 3 - 10%, 4 - 18% = ['0', '10', '18', '20', '110', '118', '120', '-1'],
        public float SalePrice { get; set; } // (number) : Цена за единицу товара(материала) ,
        public float? MinSalePrice { get; set; } //(number, optional) : Минимальная цена за единицу товара(материала) ,
        public int Type { get; set; } // (integer) : Тип 0 - Товар, 1 - Материал = ['0', '1', '-1'],
        public int NdsPositionType { get; set; } // (integer) : Тип начисления НДС 1 - НДС не начисляется, 2 - НДС начисляется "сверху", 3 - НДС включен в цену(в т. ч.) = ['1', '2', '3'],
        public int SubcontoId { get; set; } //(integer),
        public string Producer { get; set; } //(string, optional) : Производитель товара,
        public int ProductSubType { get; set; } //(integer, optional): Тип товара: 0 - материал/товар, 1 - комплект, 2 - готовая продукция, 3 - товар на комиссии = ['0', '1', '2', '3'],
        public float? ProductMinimum { get; set; }  //(number, optional) : Минимальный остаток на складе,
    }
    public class ProductRemainsRequest
    {
        public int[] Ids { get; set; }
        public string MaxDate { get; set; }
    }
    public class GoodRemainsCollection
    {
        public int ProductId { get; set; } //(integer, optional) : id продукта,
        public GoodRemainsRepresentation[] GoodRemains { get; set; }//(Array[GoodRemainsRepresentation], optional): коллекция содержащая данные по остаткам для товара
    }
    public class GoodRemainsRepresentation
    {
        public int StockId { get; set; } //(integer, optional) : Номер склада,
        public float Remains { get; set; } //(number, optional): Остаток товара на складе
    }
    class Program
    {
        private static string ApiKey = "544cc416-8f6c-4e4e-9732-89faeb7e156b";
        private static string MainUrl = "https://restapi.moedelo.org";

        static void Main(string[] args)
        {

            GetStatement();
            //FixRemainsGoods();

            //FixPsnKassa("2023.01.01", "2023.03.31");
            //FixPsnOrp("2023.04.01", "2023.06.30"); //перед использование открыть все возвраты за период для пересохранения иначе они теряют связи с орп
        }

        public static void GetStatement()
        {
            try
            {
                Console.WriteLine("=== Сервис получения банковской выписки ===");

                // Настройки (используем ваш API ключ из примера)
                string apiKey = "544cc416-8f6c-4e4e-9732-89faeb7e156b";
                string storagePath = @"C:\1\MoeDeloStatements";

                // Создаем сервис
                BankStatementService statementService = new BankStatementService(apiKey, storagePath: storagePath);

                // Получаем информацию о существующей выписке
                StatementMetadata existingInfo = statementService.GetStatementInfo();
                if (existingInfo != null)
                {
                    Console.WriteLine("\nНайдена существующая выписка:");
                    Console.WriteLine(string.Format("  Период: с {0} по {1}",
                        existingInfo.FirstOperationDate.ToString("dd.MM.yyyy"),
                        existingInfo.LastOperationDate.ToString("dd.MM.yyyy")));
                    Console.WriteLine(string.Format("  Операций: {0}", existingInfo.OperationCount));
                    Console.WriteLine(string.Format("  Последнее обновление: {0}",
                        existingInfo.LastUpdated.ToString("dd.MM.yyyy HH:mm")));
                }
                else
                {
                    Console.WriteLine("\nСуществующая выписка не найдена. Будет создана новая.");
                }

                // Получаем выписку за последний год
                Console.WriteLine("\nЗапрашиваем выписку за последний год...");
                var operations = statementService.GetBankStatement(1);

                // Выводим статистику
                Console.WriteLine("\n=== Результат ===");
                Console.WriteLine(string.Format("Всего операций: {0}", operations.Count));

                if (operations.Count > 0)
                {
                    DateTime firstDate = operations[0].Date;
                    DateTime lastDate = operations[operations.Count - 1].Date;

                    decimal totalAmount = 0;
                    foreach (var op in operations)
                    {
                        totalAmount += op.Sum;
                    }

                    Console.WriteLine(string.Format("Период: с {0} по {1}",
                        firstDate.ToString("dd.MM.yyyy"),
                        lastDate.ToString("dd.MM.yyyy")));
                    Console.WriteLine(string.Format("Общая сумма списаний: {0:N2} руб.", totalAmount));

                    // Вывод последних 5 операций
                    Console.WriteLine("\nПоследние 5 операций:");
                    int startIndex = Math.Max(0, operations.Count - 5);
                    for (int i = startIndex; i < operations.Count; i++)
                    {
                        var op = operations[i];
                        Console.WriteLine(string.Format("  {0} - {1} - {2} - {3:N2} руб.",
                            op.Date.ToString("dd.MM.yyyy"),
                            op.Number,
                            op.Contractor.Name,
                            op.Sum));
                    }
                }

                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("\nОшибка: {0}", ex.Message));
                Console.ReadKey();
            }
        }
        public static void FixPsnKassa(string startDate, string endDate)
        {
            var docs = GetKassaOperations(startDate, endDate);
            Console.Write("Получено - " + docs.Count.ToString() + " Кассовых поступлений");
            Console.WriteLine();

            int patentParse = 0;
            string patent = "";
            foreach (var item in docs)
            {
                Console.Write(item.DocumentBaseId + " - " + item.Description);
                switch (patentParse)
                {
                    case 0:
                        patent = "7500220011204";
                        break;
                    case 1:
                        patent = "7500220011204";
                        break;
                    case 2:
                        patent = "7500220011204";
                        break;
                    case 3:
                        patent = "7500220011361";
                        break;
                    case 4:
                        patent = "7500220011355";
                        break;
                    case 5:
                        patent = "7500220011204";
                        break;
                }

                patentParse++;
                if (patentParse > 5)
                {
                    patentParse = 0;
                }

                if (ChangeKassaOperation(item,patent))
                {
                    Console.Write(" - ПСН");
                }
                else
                {
                    Console.Write(" - ОШИБКА!");
                }
                Console.WriteLine();
            }
            Console.ReadKey();
        }
        public static bool ChangeKassaOperation(OperationResponseDto oper, string patent)
        {
            using (var httpClient = new HttpClient())
            {
                var curOrp = GetKassaOperation(oper.Source.Id.ToString(), oper.DocumentBaseId.ToString());
                curOrp.СonsideredInPatents = new List<СonsideredInPatent>(new СonsideredInPatent[]{new СonsideredInPatent()
                {
                    PatentNumber = patent,
                    PatentSum = curOrp.Sum-(curOrp.PayByCard.HasValue ? curOrp.PayByCard.Value : 0)
                }});

                string address = MainUrl + "/accounting/api/v1/cashier/" + oper.Source.Id + "/retailRevenue/" + oper.DocumentBaseId;
                HttpResponseMessage responseMessage;

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);

                var jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(curOrp);
                var content = new StringContent(jsonReq.ToString(), Encoding.UTF8, "application/json");

                responseMessage = httpClient.PutAsync(address, content).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                return responseMessage.IsSuccessStatusCode;
            }
        }
        public static RetailRevenueRepresentation GetKassaOperation(string kassaId, string id)
        {
            using (var httpClient = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var address = MainUrl + "/accounting/api/v1/cashier/" + kassaId + "/retailRevenue/" + id;

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);
                var request = new HttpRequestMessage(HttpMethod.Get, address);

                var responseMessage = httpClient.SendAsync(request).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                var allContracts = JsonConvert.DeserializeObject<RetailRevenueRepresentation>(responseText.ToString());
                return allContracts;
            }
        }
        public static List<OperationResponseDto> GetKassaOperations(string startDate, string endDate)
        {
            using (var httpClient = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var address = MainUrl + "/money/api/v1/Registry?Limit=1000&StartDate=" + startDate + "&EndDate=" + endDate + "&TaxationSystemType=1&OperationTypes=47";

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);
                var request = new HttpRequestMessage(HttpMethod.Get, address);

                var responseMessage = httpClient.SendAsync(request).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                var allContracts = JsonConvert.DeserializeObject<List<OperationResponseDto>>(responseText.First.First.ToString());
                return allContracts;
            }
        }
        public static void FixPsnOrp(string startDate, string endDate)
        {
            var orp = GetOrp(startDate, endDate);
            Console.Write("Получено - " + orp.Count.ToString() + " ОРП");
            Console.WriteLine();

            foreach (var item in orp)
            {
                Console.Write(item.Number+" - ");

                if (ChangeOrpToPsn(item.Id.ToString()))
                {
                    Console.Write(" ПСН - ОК");
                }
                else
                {
                    Console.Write(" ПСН - ОШИБКА!");
                }
                Console.WriteLine();
            }

            Console.ReadKey();
        }
        public static bool ChangeOrpToPsn(string id)
        {
            using (var httpClient = new HttpClient())
            {
                var curOrp = GetOrp(id);

                var address = MainUrl + "/accounting/api/v1/sales/retailreport/"+id;
                HttpResponseMessage responseMessage;

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);

                var jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(new RetailReportModel(curOrp));
                var content = new StringContent(jsonReq.ToString(), Encoding.UTF8, "application/json");

                responseMessage = httpClient.PutAsync(address, content).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                return responseMessage.IsSuccessStatusCode;
            }
        }
        public static RetailReportRepresentation GetOrp(string id)
        {
            using (var httpClient = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var address = MainUrl + "/accounting/api/v1/sales/retailreport/" + id;

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);
                var request = new HttpRequestMessage(HttpMethod.Get, address);

                var responseMessage = httpClient.SendAsync(request).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                var allContracts = JsonConvert.DeserializeObject<RetailReportRepresentation>(responseText.ToString());
                return allContracts;
            }
        }
        public static List<RetailReportCollectionItemRepresentation> GetOrp(string startDate, string endDate)
        {
            using (var httpClient = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var address = MainUrl + "/accounting/api/v1/sales/retailreport?pageNo=1&pageSize=5000&docAfterDate="+startDate+"&docBeforeDate="+endDate;

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);
                var request = new HttpRequestMessage(HttpMethod.Get, address);

                var responseMessage = httpClient.SendAsync(request).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                var allContracts = JsonConvert.DeserializeObject<RetailReportRepresentationCollection>(responseText.ToString());
                return allContracts.ResourceList;
            }
        }
        public static void FixRemainsGoods()
        {
            var goods = GetGoods("");
            var remains = GetRemainsGood(goods.Select(g => g.Id).ToArray());

            remains = remains.Where(r => r.GoodRemains.Where(g => g.Remains < 0).Count() > 0).ToList();

            List<PurchasesWaybillRepresentation> ttns = new List<PurchasesWaybillRepresentation>();
            int ttncount = 1;
            foreach (var remain in remains)
            {
                foreach (var good in remain.GoodRemains)
                {
                    if (good.Remains < 0)
                    {
                        var curttn = ttns.SingleOrDefault(t => t.StockId == good.StockId);
                        if (curttn != null)
                        {
                            var currgood = curttn.Items.SingleOrDefault(g => g.StockProductId == remain.ProductId);
                            if (currgood != null)
                            {
                                currgood.Count = currgood.Count - good.Remains;
                                ttns.SingleOrDefault(t => t.StockId == good.StockId).Items
                                    .SingleOrDefault(g => g.StockProductId == remain.ProductId).Count = currgood.Count;
                            }
                            else
                            {
                                var mdgood = GetGood(remain.ProductId);
                                ttns.SingleOrDefault(t => t.StockId == good.StockId).Items.Add(new PurchasesWaybillItemRepresentation()
                                {
                                    Count = good.Remains * -1,
                                    Price = 1,
                                    StockProductId = remain.ProductId,
                                    Name = mdgood.Name,
                                    NdsType = -1,
                                    Unit = mdgood.UnitOfMeasurement
                                });
                                Console.WriteLine(mdgood.Name);
                            }
                        }
                        else
                        {
                            var newitem = new PurchasesWaybillRepresentation();
                            newitem.Number = "ost" + ttncount.ToString();
                            newitem.StockId = good.StockId;

                            var mdgood = GetGood(remain.ProductId);
                            newitem.Items.Add(new PurchasesWaybillItemRepresentation()
                            {
                                Count = good.Remains * -1,
                                Price = 1,
                                StockProductId = remain.ProductId,
                                Name = mdgood.Name,
                                NdsType = -1,
                                Unit = mdgood.UnitOfMeasurement
                            });
                            ttns.Add(newitem);
                            Console.WriteLine(mdgood.Name);
                            ttncount++;
                        }
                    }
                }
            }
            ttns.ForEach(t => CreatTTN(t));
        }
        public static List<GoodRepresentation> GetGoods(string findText)
        {
            using (var httpClient = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var address = MainUrl + "/stock/api/v1/good?pageNo=1&pageSize=5000";

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);
                var request = new HttpRequestMessage(HttpMethod.Get, address);

                var responseMessage = httpClient.SendAsync(request).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                var allContracts = JsonConvert.DeserializeObject<List<GoodRepresentation>>(responseText.First.First.ToString());
                return allContracts;
            }
        }
        public static GoodRepresentation GetGood(int number)
        {
            using (var httpClient = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var address = MainUrl + "/stock/api/v1/good/"+number.ToString();

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);
                var request = new HttpRequestMessage(HttpMethod.Get, address);

                var responseMessage = httpClient.SendAsync(request).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                var allContracts = JsonConvert.DeserializeObject<GoodRepresentation>(responseText.ToString());
                return allContracts;
            }
        }
        public static List<GoodRemainsCollection> GetRemainsGood(int[] Ids)
        {
            using (var httpClient = new HttpClient())
            {
                var address = MainUrl + "/stock/api/v1/good/remains";
                HttpResponseMessage responseMessage;

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);

                ProductRemainsRequest request = new ProductRemainsRequest()
                {
                    Ids = Ids,
                    MaxDate = "2023-12-31"
                };

                var jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonReq.ToString(), Encoding.UTF8, "application/json");

                responseMessage = httpClient.PostAsync(address, content).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                return JsonConvert.DeserializeObject<List<GoodRemainsCollection>>(responseText.First.First.ToString());
            }
        }
        public static bool CreatTTN(PurchasesWaybillRepresentation item)
        {
            using (var httpClient = new HttpClient())
            {
                var address = MainUrl + "/accounting/api/v1/purchases/waybill";
                HttpResponseMessage responseMessage;

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("md-api-key", ApiKey);

                var jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(item);
                var content = new StringContent(jsonReq.ToString(), Encoding.UTF8, "application/json");

                responseMessage = httpClient.PostAsync(address, content).Result;
                var responseText = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);

                return responseMessage.IsSuccessStatusCode;
            }
        }
    }
}
