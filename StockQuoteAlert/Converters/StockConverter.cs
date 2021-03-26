using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace stock_quote_alert.Converters
{
    class StockConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Stock);
        }
         
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Stock stock = new();

            //Loading object
            JObject rootObject = JObject.Load(reader);

            //Getting results token
            JToken stockRootToken = rootObject["results"];
            if (stockRootToken is null || !stockRootToken.Any())
                throw new JsonReaderException("Cannot find property with stock name. Bad Json input.");

            //Populating stock but results will be null because it comes with the stock symbol as dynamic property
            stockRootToken = stockRootToken.FirstOrDefault();
            serializer.Populate(rootObject.CreateReader(), stock);

            //Repopulating results with the stock information
            stockRootToken = stockRootToken.FirstOrDefault();
            serializer.Populate(stockRootToken.CreateReader(), stock.Results);

            return stock;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite => false;
    }
}
