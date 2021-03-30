using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Mail;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Globalization;
using System.Threading;
using stock_quote_alert.Converters;
using Newtonsoft.Json;

namespace StockQuoteAlert
{
    public class Program
    {
        public static readonly HttpClient client = new();

        public static SmtpConfig config;

        public static MailMessage mailMessage;

        public static SmtpClient smtpClient;

        public static string stockSymbol = string.Empty;

        public static decimal sellPrice = decimal.Zero;

        public static decimal buyPrice = decimal.Zero;

        public static readonly CancellationTokenSource source = new();

        public static readonly CancellationToken token = source.Token;

        public static async Task<decimal?> GetPrice(string url)
        {
            using HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            Stock stock = JsonConvert.DeserializeObject<Stock>(json, new StockConverter());

            return stock.Results.Price;
        }

        public static void SendEmail(string message, bool buyEmail)
        {
            mailMessage = new(config.from, config.to)
            {
                Subject = buyEmail ? "Alerta de Ações - Compra" : "Alerta de Ações - Venda",
                Body = message
            };

            Parallel.Invoke(() => smtpClient.Send(mailMessage));
        }

        public static void InitializeSmtp(string path)
        {
            if (path is null)
            {
                throw new Exception("Please inform email-config path.");
            }

            //Lendo dados de configuração do servidor smtp.
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile(path, optional: true, reloadOnChange: true)
                            .Build();
            config = builder.Get<SmtpConfig>();

            if(config is null)
            {
                throw new Exception("Invalid email-config json.");
            }

            smtpClient = new(config.server)
            {
                EnableSsl = config.ssl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Port = config.port,
                UseDefaultCredentials = config.defaultCredentials,
                Credentials = new NetworkCredential(config.from, config.password)
            };
        }

        public static void ValidateInput(string[] args)
        {
            if (args is null)
            {
                throw new Exception("Please enter with the stock symbol, sell price and buy price.");
            }

            if (args.Length != 3)
            {
                throw new Exception("Please enter with the stock symbol, sell price and buy price.");
            }

            if (args[0].Length != 5)
            {
                throw new Exception("Please enter with a valid stock symbol.");
            }
            else
            {
                stockSymbol = args[0];
            }

            if (decimal.TryParse(args[1], NumberStyles.Any, new CultureInfo("en-US"), out decimal resultSell))
            {
                sellPrice = resultSell;
            }
            else
            {
                throw new Exception("Please enter with a valid sell price.");
            }

            if (decimal.TryParse(args[2], NumberStyles.Any, new CultureInfo("en-US"), out decimal resultBuy))
            {
                buyPrice = resultBuy;
            }
            else
            {
                throw new Exception("Please enter with a valid buy price.");
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                ValidateInput(args);
                InitializeSmtp("email-config.json");
                await Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async Task Run()
        {
            decimal? stockPrice;
            decimal percentageVariation;
            string message;

            var checkEmailInterval = Task.Run(async delegate
            {
                await Task.Delay(config.intervaloMinutosEmail * 60000, token);
            });

            Console.WriteLine("Press Ctrl+C to exit program.");

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Program shutting down...");
                source.Cancel();
                eventArgs.Cancel = true;
            };

            try
            {
                while (!token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();

                    stockPrice = await GetPrice($"https://api.hgbrasil.com/finance/stock_price?format=json&key=86a75eb7&symbol={stockSymbol}");

                    if (stockPrice == null)
                    {
                        throw new Exception("API not available, try again later.");
                    }

                    if (stockPrice >= sellPrice)
                    {
                        percentageVariation = Math.Round(((stockPrice ?? 0) / sellPrice - 1) * 100, 2);
                        message = $"The stock {stockSymbol} price is {string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C2}", stockPrice)}, {percentageVariation}% above the sell price! Time to sell!";
                        SendEmail(message, false);
                        Console.WriteLine($"{DateTime.Now} - Sell email sent!");
                    }
                    else if (stockPrice <= buyPrice)
                    {
                        percentageVariation = Math.Round(((stockPrice ?? 0) / sellPrice - 1) * 100, 2);
                        message = $"The stock {stockSymbol} price is {string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C2}", stockPrice)}, {percentageVariation}% below the buy price! Time to buy!";
                        SendEmail(message, true);
                        Console.WriteLine($"{DateTime.Now} - Buy email sent!");
                    }

                    //Backoff between emails
                    checkEmailInterval.Wait(token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                smtpClient.Dispose();
                mailMessage.Dispose();
                source.Dispose();
            }
        }
    }
}
