namespace ShipmentFinishGood.Utilities;

public static class CountryNormalizer
{
    // Normalize country names by trimming and collapsing internal whitespace
    public static string Normalize(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return string.Empty;
        var parts = country.Trim().Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    public static string NormalizeOrUnknown(string? country)
    {
        var c = Normalize(country);
        return string.IsNullOrEmpty(c) ? "Unknown" : c;
    }
}
