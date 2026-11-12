using System.Globalization;
using System.Text.Json;

namespace WMBankPortal.Application.Features.Applications.Validators;

public static class DynamicFormValidator
{
    public const int MaxPayloadChars = 10_000;
    public const int MaxTextLength = 200;

    /// <summary>Returns an error message, or null if the values match the schema.</summary>
    public static string? Validate(string schemaJson, string valuesJson)
    {
        if (valuesJson.Length > MaxPayloadChars) return "Form payload is too large.";
        try
        {
            using var schema = JsonDocument.Parse(schemaJson);
            using var values = JsonDocument.Parse(valuesJson);
            if (values.RootElement.ValueKind != JsonValueKind.Object)
                return "Form values must be a JSON object.";

            var known = new HashSet<string>();
            foreach (var field in schema.RootElement.EnumerateArray())
            {
                var id = field.GetProperty("id").GetString()!;
                known.Add(id);
                var label = field.TryGetProperty("label", out var l) ? l.GetString() : id;
                var required = field.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
                var type = field.GetProperty("type").GetString();

                var missing = !values.RootElement.TryGetProperty(id, out var value)
                    || value.ValueKind == JsonValueKind.Null
                    || (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString()));
                if (missing)
                {
                    if (required) return $"'{label}' is required.";
                    continue;
                }

                switch (type)
                {
                    case "number":
                        if (!TryGetDecimal(value, out var num)) return $"'{label}' must be a number.";
                        if (field.TryGetProperty("min", out var min) && num < min.GetDecimal())
                            return $"'{label}' must be at least {min.GetDecimal()}.";
                        if (field.TryGetProperty("max", out var max) && num > max.GetDecimal())
                            return $"'{label}' must be at most {max.GetDecimal()}.";
                        break;

                    case "select":
                        var chosen = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
                        var options = field.GetProperty("options").EnumerateArray().Select(o => o.GetString());
                        if (!options.Contains(chosen)) return $"'{label}' has an invalid selection.";
                        break;

                    default: // text
                        if (value.ValueKind != JsonValueKind.String) return $"'{label}' must be text.";
                        if (value.GetString()!.Length > MaxTextLength)
                            return $"'{label}' must be at most {MaxTextLength} characters.";
                        break;
                }
            }

            foreach (var prop in values.RootElement.EnumerateObject())
                if (!known.Contains(prop.Name)) return $"Unexpected field '{prop.Name}'.";

            return null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException
                                    or KeyNotFoundException or FormatException)
        {
            return "Form values do not match the product schema.";
        }
    }

    private static bool TryGetDecimal(JsonElement e, out decimal d)
    {
        if (e.ValueKind == JsonValueKind.Number) return e.TryGetDecimal(out d);
        if (e.ValueKind == JsonValueKind.String)
            return decimal.TryParse(e.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out d);
        d = 0;
        return false;
    }
}