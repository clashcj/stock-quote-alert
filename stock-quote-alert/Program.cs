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
        static HttpClient client = new HttpClient();

        static string nomeAtivo = "";

        static decimal precoVenda = 0;

        static decimal precoCompra = 0;

        static async Task<decimal?> GetPreco(string url)
        {
            Stock acao;
            string json;
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                json = await response.Content.ReadAsStringAsync();
                var index = json.IndexOf(nomeAtivo);
                json = json.Substring(0, index) + "nomeAtivo" + json.Substring(index + nomeAtivo.Length);
                acao = JsonSerializer.Deserialize<Stock>(json);
            }
            else
            {
                return null;
            }

            return acao.results.nomeAtivo.price;
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Por favor entre com o nome do ativo, o preço de referência para venda e o preço de referência para compra.");
                }
                else
                {
                    nomeAtivo = args[0];
                    precoVenda = Convert.ToDecimal(args[1]);
                    precoCompra = Convert.ToDecimal(args[2]);
                    Run().GetAwaiter().GetResult();
                }
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
            SmtpClient smtpClient = new SmtpClient(config.server);
            smtpClient.EnableSsl = config.ssl;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.Port = config.port;
            smtpClient.UseDefaultCredentials = config.defaultCredentials;
            smtpClient.Credentials = new NetworkCredential(config.from, config.password);
            MailMessage mensagemCompra = new MailMessage(config.from, config.to);
            MailMessage mensagemVenda = new MailMessage(config.from, config.to);
            mensagemCompra.Subject = "Alerta de Ações - Compra";
            mensagemVenda.Subject = "Alerta de Ações - Venda";
            DateTime? dataUltimoEmailEnviado = null ;

            Console.WriteLine("Pressione ESC para encerrar o programa");
            do
            {
                while (!Console.KeyAvailable)
                {
                    try
                    {
                        valorAtualAcao = await GetPreco($"https://api.hgbrasil.com/finance/stock_price?format=json&key=981c1cf3&symbol={nomeAtivo}");

                        if (valorAtualAcao != null)
                        {
                            if (valorAtualAcao >= precoVenda)
                            {
                                if (dataUltimoEmailEnviado == null ||
                                    (DateTime.Now - dataUltimoEmailEnviado.GetValueOrDefault()).TotalMinutes > config.intervaloMinutosEmail)
                                {
                                    //Envia email de acordo com intervalo configurado (em minutos)
                                    variacaoPorcentagem = Math.Round(((valorAtualAcao ?? 0) / precoVenda - 1) * 100, 2);
                                    mensagemVenda.Body = $"O preço da ação {nomeAtivo} está em {string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C2}", valorAtualAcao)}, {variacaoPorcentagem}% acima do preço de venda configurado! Hora de vender!";
                                    smtpClient.Send(mensagemVenda);
                                    dataUltimoEmailEnviado = DateTime.Now;
                                    Console.WriteLine($"{DateTime.Now} - Email de venda enviado!");
                                }
                            }
                            else if (valorAtualAcao <= precoCompra)
                            {
                                if (dataUltimoEmailEnviado == null ||
                                    (DateTime.Now - dataUltimoEmailEnviado.GetValueOrDefault()).TotalMinutes > config.intervaloMinutosEmail)
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
