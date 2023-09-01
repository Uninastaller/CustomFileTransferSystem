using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Model
{
    public static class JsonValidator
    {
        public static bool ValidateJson<T>(string jsonString)
        {
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(jsonString);
            }
            catch (JsonException)
            {
                return false;
            }

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                var ignoreAttribute = prop.GetCustomAttribute<JsonIgnoreAttribute>();
                if (ignoreAttribute != null)
                {
                    continue; // Skip properties marked as [JsonIgnore]
                }

                // Optional field
                if (!doc.RootElement.TryGetProperty(prop.Name, out JsonElement element) && Nullable.GetUnderlyingType(prop.PropertyType) != null)
                {
                    continue; // Skip nullable properties that don't exist in the JSON
                }
                else if (!doc.RootElement.TryGetProperty(prop.Name, out element))
                {
                    return false; // Property doesn't exist
                }

                // This is a simplified check. You might want to extend this for more types.
                if ((element.ValueKind == JsonValueKind.String && prop.PropertyType != typeof(string)) ||
                    (element.ValueKind == JsonValueKind.Number && !IsNumericType(prop.PropertyType)) ||
                    (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False) && prop.PropertyType != typeof(bool))
                {
                    return false; // Property type mismatch
                }
            }

            return true;
        }

        // Helper method to check if a type is numeric
        private static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
    }
}
