public static class CurrencyRules
{
    public static int GetDecimalPlaces(string currency) => currency.ToUpperInvariant() switch
    {
        "IRR" or "IRT" => 0,
        "USD" or "EUR" => 2, 
        _ => 2
    };
}
