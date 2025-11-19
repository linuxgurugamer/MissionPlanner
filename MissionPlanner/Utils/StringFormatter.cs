using System;
using System.Text;
using System.Text.RegularExpressions;

public static class StringFormatter
{
    public static string BeautifyName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 1. Capitalize first character
        var sb = new StringBuilder(input.Trim());
        sb[0] = char.ToUpper(sb[0]);

        string result = sb.ToString();

        // 2. Replace underscores with spaces and capitalize the following letter
        //    Example: "solar_panels_active" → "Solar Panels_active"
        result = Regex.Replace(result, @"_(\w)", m => " " + char.ToUpper(m.Groups[1].Value[0]));

        // 3. Find capital letters following lowercase letters and insert a space before them
        //    Example: "SolarPanels" → "Solar Panels"
        result = Regex.Replace(result, @"([a-z])([A-Z])", "$1 $2");

        return result;
    }
}
