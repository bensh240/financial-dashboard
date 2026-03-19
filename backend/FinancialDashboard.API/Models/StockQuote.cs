using System.Text.Json.Serialization;

namespace FinancialDashboard.API.Models;

/// <summary>Maps to Finnhub /quote response</summary>
public class FinnhubQuote
{
    [JsonPropertyName("c")]  public decimal CurrentPrice { get; set; }
    [JsonPropertyName("d")]  public decimal Change { get; set; }
    [JsonPropertyName("dp")] public decimal PercentChange { get; set; }
    [JsonPropertyName("h")]  public decimal High { get; set; }
    [JsonPropertyName("l")]  public decimal Low { get; set; }
    [JsonPropertyName("o")]  public decimal Open { get; set; }
    [JsonPropertyName("pc")] public decimal PreviousClose { get; set; }
    [JsonPropertyName("t")]  public long Timestamp { get; set; }
}

/// <summary>Maps to Finnhub /stock/profile2 response</summary>
public class FinnhubProfile
{
    [JsonPropertyName("name")]    public string? Name { get; set; }
    [JsonPropertyName("ticker")]  public string? Ticker { get; set; }
    [JsonPropertyName("exchange")]public string? Exchange { get; set; }
    [JsonPropertyName("ipo")]     public string? Ipo { get; set; }
    [JsonPropertyName("marketCapitalization")] public decimal? MarketCap { get; set; }
    [JsonPropertyName("shareOutstanding")]     public decimal? ShareOutstanding { get; set; }
    [JsonPropertyName("logo")]    public string? Logo { get; set; }
    [JsonPropertyName("weburl")]  public string? WebUrl { get; set; }
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("currency")]public string? Currency { get; set; }
    [JsonPropertyName("industry")]public string? Industry { get; set; }
}

/// <summary>Maps to Finnhub /search result item</summary>
public class FinnhubSearchResult
{
    [JsonPropertyName("description")]   public string? Description   { get; set; }
    [JsonPropertyName("displaySymbol")] public string? DisplaySymbol { get; set; }
    [JsonPropertyName("symbol")]        public string? Symbol        { get; set; }
    [JsonPropertyName("type")]          public string? Type          { get; set; }
}

public class FinnhubSearchResponse
{
    [JsonPropertyName("count")]  public int Count { get; set; }
    [JsonPropertyName("result")] public List<FinnhubSearchResult>? Result { get; set; }
}

/// <summary>Maps to Finnhub /stock/candle response (premium only – kept for reference)</summary>
public class FinnhubCandle
{
    [JsonPropertyName("c")] public List<decimal>? Close { get; set; }
    [JsonPropertyName("h")] public List<decimal>? High { get; set; }
    [JsonPropertyName("l")] public List<decimal>? Low { get; set; }
    [JsonPropertyName("o")] public List<decimal>? Open { get; set; }
    [JsonPropertyName("t")] public List<long>? Timestamps { get; set; }
    [JsonPropertyName("v")] public List<decimal>? Volume { get; set; }
    [JsonPropertyName("s")] public string? Status { get; set; }
}

/// <summary>Yahoo Finance /v8/finance/chart response models</summary>
public class YahooChartResponse
{
    [JsonPropertyName("chart")] public YahooChart? Chart { get; set; }
}
public class YahooChart
{
    [JsonPropertyName("result")] public List<YahooChartResult>? Result { get; set; }
    [JsonPropertyName("error")]  public object? Error { get; set; }
}
public class YahooChartResult
{
    [JsonPropertyName("timestamp")]  public List<long>? Timestamps { get; set; }
    [JsonPropertyName("indicators")] public YahooIndicators? Indicators { get; set; }
}
public class YahooIndicators
{
    [JsonPropertyName("quote")] public List<YahooQuoteData>? Quote { get; set; }
}
public class YahooQuoteData
{
    [JsonPropertyName("close")]  public List<decimal?>? Close  { get; set; }
    [JsonPropertyName("open")]   public List<decimal?>? Open   { get; set; }
    [JsonPropertyName("high")]   public List<decimal?>? High   { get; set; }
    [JsonPropertyName("low")]    public List<decimal?>? Low    { get; set; }
    [JsonPropertyName("volume")] public List<long?>?    Volume { get; set; }
}
