using System;
using System.Reflection;

namespace Modeel.Model
{
    public enum SocketMessageFlag
    {
        // Client => Server
        [StringValue("a.A")]
        FILE_REQUEST,

        [StringValue("a.B")]
        FILE_PART_REQUEST,

        // Server => Client
        [StringValue("b.A")]
        REJECT,

        [StringValue("b.B")]
        ACCEPT,

        [StringValue("b.C")]
        FILE_PART
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class StringValueAttribute : Attribute
    {
        public string StringValue { get; private set; }

        public StringValueAttribute(string stringValue)
        {
            StringValue = stringValue;
        }
    }

    public static class EnumExtensions
    {
        public static string GetStringValue(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo? fieldInfo = type.GetField(value.ToString());

            StringValueAttribute[]? stringValueAttributes = fieldInfo?.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];

            if (stringValueAttributes?.Length > 0)
                return stringValueAttributes[0].StringValue;

            return value.ToString();
        }
    }
}
