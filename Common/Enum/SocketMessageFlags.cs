using System;
using System.Reflection;

namespace Common.Enum
{
    public enum SocketMessageFlag
    {
        // Client => Server
        [StringValue("a.A")]
        FILE_REQUEST,

        [StringValue("a.B")]
        FILE_PART_REQUEST,

        [StringValue("a.C")]
        OFFERING_FILES_REQUEST,

        [StringValue("a.D")]
        NODE_LIST_REQUEST,



        // Server => Client
        [StringValue("b.A")]
        REJECT,

        [StringValue("b.B")]
        ACCEPT,

        [StringValue("b.C")]
        FILE_PART,

        [StringValue("b.D")]
        OFFERING_FILE,

        [StringValue("b.E")]
        NODE_LIST,




        [StringValue("e.F")]
        END_OF_MESSAGE,

        [StringValue("e.G")]
        END_OF_MESSAGE_GROUP,


        // pBFT
        [StringValue("c.A")]
        PBFT_REQUEST,

        [StringValue("c.B")]
        PBFT_PRE_PREPARE,

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
        public static string GetStringValue(this System.Enum value)
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
