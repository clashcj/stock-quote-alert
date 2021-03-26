using Microsoft.VisualStudio.TestTools.UnitTesting;
using stock_quote_alert;
using System;
using System.Net.Mail;

namespace stock_quote_alterts.Tests
{
    [TestClass]
    public class StockQuoteAlertTests
    {
        private Exception result;

        public static SmtpConfig InvalidSmtpConfig()
        {
            return new SmtpConfig() { 
                defaultCredentials = false,
                to = "gjorgec@gmail.com",
                from = "invalidgmail.com",
                server = "smtp.gmail.com",
                intervaloMinutosEmail = 1,
                password = "vlyzxygrcauadqlr",
                port = 587,
                ssl = true
            };
        }

        [TestMethod]
        public void ShouldThrowExceptionIfArgumentsAreInvalid()
        {
            result = Assert.ThrowsException<Exception>(() => Program.ValidateInput(null));
            Assert.AreEqual(result.Message, "Please enter with the stock symbol, sell price and buy price.");
            result = Assert.ThrowsException<Exception>(() => Program.ValidateInput(Array.Empty<string>()));
            Assert.AreEqual(result.Message, "Please enter with the stock symbol, sell price and buy price.");
            result = Assert.ThrowsException<Exception>(() => Program.ValidateInput(new string[] { "PETR4" }));
            Assert.AreEqual(result.Message, "Please enter with the stock symbol, sell price and buy price.");
            result = Assert.ThrowsException<Exception>(() => Program.ValidateInput(new string[] { "PETR4", "23.00" }));
            Assert.AreEqual(result.Message, "Please enter with the stock symbol, sell price and buy price.");
            result = Assert.ThrowsException<Exception>(() => Program.ValidateInput(new string[] { "PETR4", "Not a number", "Not a number" }));
            Assert.AreEqual(result.Message, "Please enter with a valid sell price.");
            result = Assert.ThrowsException<Exception>(() => Program.ValidateInput(new string[] { "PETR4", "23.00", "Not a number" }));
            Assert.AreEqual(result.Message, "Please enter with a valid buy price.");
            result = Assert.ThrowsException<Exception>(() => Program.ValidateInput(new string[] { "Not a valid symbol", "Not a number", "Not a number" }));
            Assert.AreEqual(result.Message, "Please enter with a valid stock symbol.");
        }

        [TestMethod]
        public void ShouldThrowExceptionIfEmailConfigIsInvalid()
        {
            result = Assert.ThrowsException<Exception>(() => Program.InitializeSmtp("Not a valid path"));
            Assert.AreEqual(result.Message, "Invalid email-config json.");
            result = Assert.ThrowsException<Exception>(() => Program.InitializeSmtp(null));
            Assert.AreEqual(result.Message, "Please inform email-config path.");
        }

        [TestMethod]
        public void ShouldThrowExceptionSendEmail()
        {
            Assert.ThrowsException<NullReferenceException>(() => Program.SendEmail(null, false));
            Program.config = InvalidSmtpConfig();
            Assert.ThrowsException<FormatException>(() => Program.SendEmail("teste", false));
            Program.config.from = null;
            Assert.ThrowsException<ArgumentNullException>(() => Program.SendEmail("teste", false));
            Program.config.from = string.Empty;
            Assert.ThrowsException<ArgumentException>(() => Program.SendEmail("teste", false));
            Program.config.from = "validemail@gmail.com";
            Program.config.port = 0; //invalid port
            Assert.ThrowsException<AggregateException>(() => Program.SendEmail("teste", false));
            Program.client.Dispose();
            Assert.ThrowsException<AggregateException>(() => Program.SendEmail("teste", false));
        }

        [TestMethod]
        public void ShouldThrowExceptionGetPrice()
        {
            Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await Program.GetPrice("invalidUri"));
            Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await Program.GetPrice(null));
        }

        [TestMethod]

        public void ShouldReturnPrice()
        {
            Assert.IsInstanceOfType(Program.GetPrice("https://api.hgbrasil.com/finance/stock_price?key=86a75eb7%20&symbol=b3sa3").GetAwaiter().GetResult(), typeof(decimal));
        }
    }
}
