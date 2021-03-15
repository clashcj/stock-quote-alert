using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Mail;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Globalization;

namespace stock_quote_alert
{
    class Program
    {
        static readonly HttpClient client = new();

        private static string nomeAtivo = string.Empty;

        private static decimal precoVenda = decimal.Zero;

        private static decimal precoCompra = decimal.Zero;

        private static async Task<decimal?> GetPreco(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            int index = json.IndexOf(nomeAtivo);
            json = json[..index] + "nomeAtivo" + json[(index + nomeAtivo.Length)..];
            Stock acao = JsonSerializer.Deserialize<Stock>(json);

            return acao.results.nomeAtivo.price;
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Por favor entre com o nome do ativo, o preço de referência para venda e o preço de referência para compra.");
                    return;
                }

                nomeAtivo = args[0];
                precoVenda = Convert.ToDecimal(args[1], new CultureInfo("en-US"));
                precoCompra = Convert.ToDecimal(args[2], new CultureInfo("en-US"));
                Run().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async Task Run()
        {
            decimal? valorAtualAcao;
            decimal variacaoPorcentagem;

            //Lendo dados de configuração do servidor smtp.
            SmtpConfig config;
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("email-config.json", optional: true, reloadOnChange: true)
                            .Build();
            config = builder.Get<SmtpConfig>();
            SmtpClient smtpClient = new(config.server)
            {
                EnableSsl = config.ssl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Port = config.port,
                UseDefaultCredentials = config.defaultCredentials,
                Credentials = new NetworkCredential(config.from, config.password)
            };

            MailMessage mensagemCompra = new(config.from, config.to)
            {
                Subject = "Alerta de Ações - Compra"
            };

            MailMessage mensagemVenda = new(config.from, config.to)
            {
                Subject = "Alerta de Ações - Venda"
            };

            DateTime? dataUltimoEmailEnviado = null;

            Console.WriteLine("Pressione ESC para encerrar o programa");
            do
            {
                while (!Console.KeyAvailable)
                {
                    try
                    {
                        //Verifica se está no intervalo para envio de email para não ficar realizando muitas consultas a API que pode causar bloqueio da chave de consulta.
                        if (dataUltimoEmailEnviado is object && DateTime.Now.Subtract(dataUltimoEmailEnviado.Value).TotalMinutes <= config.intervaloMinutosEmail)
                        {
                            continue;
                        }

                        valorAtualAcao = await GetPreco($"https://api.hgbrasil.com/finance/stock_price?format=json&key=86a75eb7&symbol={nomeAtivo}");

                        if (valorAtualAcao != null)
                        {
                            if (valorAtualAcao >= precoVenda)
                            {
                                //Envia email de acordo com intervalo configurado (em minutos)
                                variacaoPorcentagem = Math.Round(((valorAtualAcao ?? 0) / precoVenda - 1) * 100, 2);
                                mensagemVenda.Body = $"O preço da ação {nomeAtivo} está em {string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C2}", valorAtualAcao)}, {variacaoPorcentagem}% acima do preço de venda configurado! Hora de vender!";
                                smtpClient.Send(mensagemVenda);
                                dataUltimoEmailEnviado = DateTime.Now;
                                Console.WriteLine($"{DateTime.Now} - Email de venda enviado!");
                            }
                            else if (valorAtualAcao <= precoCompra)
                            {
                                //Envia email de acordo com intervalo configurado (em minutos)
                                variacaoPorcentagem = Math.Round(((valorAtualAcao ?? 0) / precoCompra - 1) * 100, 2);
                                mensagemCompra.Body = $"O preço da ação {nomeAtivo} está em {string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C2}", valorAtualAcao)}, {variacaoPorcentagem}% abaixo do preço de compra configurado! Hora de comprar!";
                                smtpClient.Send(mensagemCompra);
                                dataUltimoEmailEnviado = DateTime.Now;
                                Console.WriteLine($"{DateTime.Now} - Email de compra enviado!");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            smtpClient.Dispose();
        }
    }
}
