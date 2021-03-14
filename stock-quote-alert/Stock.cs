
public class Stock
{
    public string by { get; set; }
    public bool valid_key { get; set; }
    public Results results { get; set; }
    public float execution_time { get; set; }
    public bool from_cache { get; set; }
}

public class Results
{
    public Ativo nomeAtivo { get; set; }
}

public class Ativo
{
    public string symbol { get; set; }
    public string name { get; set; }
    public string company_name { get; set; }
    public string document { get; set; }
    public string description { get; set; }
    public string website { get; set; }
    public string region { get; set; }
    public string currency { get; set; }
    public Market_Time market_time { get; set; }
    public double market_cap { get; set; }
    public decimal price { get; set; }
    public double change_percent { get; set; }
    public string updated_at { get; set; }
}

public class Market_Time
{
    public string open { get; set; }
    public string close { get; set; }
    public int timezone { get; set; }
}
