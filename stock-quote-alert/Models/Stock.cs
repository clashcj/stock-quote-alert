using Newtonsoft.Json;

public class Stock
{
    public string By { get; set; }

    [JsonProperty("valid_key")]
    public bool ValidKey { get; set; }

    public Results Results { get; set; }

    [JsonProperty("execution_time")]
    public float ExecutionTime { get; set; }

    [JsonProperty("from_cache")]
    public bool FromCache { get; set; }
}

public class Results
{
    public string Symbol { get; set; }

    public string Name { get; set; }

    [JsonProperty("company_name")]
    public string CompanyName { get; set; }

    public string Document { get; set; }

    public string Description { get; set; }

    public string Website { get; set; }

    public string Region { get; set; }

    public string Currency { get; set; }

    [JsonProperty("market_time")]
    public Market_Time MarketTime { get; set; }

    [JsonProperty("market_cap")]
    public double MarketCap { get; set; }

    public decimal Price { get; set; }

    [JsonProperty("change_percent")]
    public double ChangePercent { get; set; }

    [JsonProperty("updated_at")]
    public string UpdatedAt { get; set; }
}

public class Market_Time
{
    public string Open { get; set; }
    
    public string Close { get; set; }
    
    public int Timezone { get; set; }
}
