using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using com.LoanTek.Types;
using LoanTek.LoggingObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace com.LoanTek.API.Common
{
    public class Global : System.Web.HttpApplication
    {
        static Global()
        {
            JsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new WritablePropertiesOnlyResolver() //this is needed because to ensure any getter properties that have long running processes are not accessed
            };
            JsonSettings.NullValueHandling = NullValueHandling.Ignore;
            JsonSettings.Formatting = Formatting.None;
            JsonSettings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
            JsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            JsonSettings.Converters.Add(new IntegerConverter());
            JsonSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'

            JsonSettingsMin = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };  

            //new XmlWriterSettings().NamespaceHandling = NamespaceHandling.OmitDuplicates;
        }

        public static readonly JsonSerializerSettings JsonSettings;
        public static readonly JsonSerializerSettings JsonSettingsMin;

        class WritablePropertiesOnlyResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
                return props.Where(p => p.Writable).ToList();
            }
        }

        class IntegerConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(int));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JToken token = JToken.Load(reader);
                switch (token.Type)
                {
                    case JTokenType.Integer:
                        return token.ToObject<int>();
                    case JTokenType.Float:
                        return token.ToObject<int>();
                    case JTokenType.String:
                        return int.Parse(token.ToString()); 
                    case JTokenType.Boolean:
                        return NullSafe.NullSafeInteger(token, 0);
                    case JTokenType.Null:
                        return objectType == typeof(int?);
                    default:
                        throw new JsonSerializationException("Unexpected token type: " + token.Type);
                }
            }

            public override bool CanWrite => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public const string ARequestPropertyName = "ARequest";

        public static readonly string LocalServerName = Environment.MachineName;
        public static Types.Api.ApiStatusType ServerStatusType;
        public static Api ApiObject;

        public static Types.Api.ApiCategoryTypes ApiCategoryType;
        public static Types.Api.ApiForWhoTypes ApiForWhoType;

        public static Processing.DebugModeType DebugModeType = Processing.DebugModeType.None;
        public static int UseOnlyThisUserId = 0;


        private static readonly SimpleLogger errorLogger = new SimpleLogger(SimpleLogger.LogToType.DATABASE);
        public static void OutPrint(string msg, string fromNamespace, string fromClass, string fromMethod, SimpleLogger.LogLevelType logLevel = SimpleLogger.LogLevelType.ERROR)
        {
            OutPrint(msg, new SimpleLogger.LocationObject() { Namespace = fromNamespace, ClassName = fromClass, MethodName = fromMethod }, logLevel);
        }
        public static void OutPrint(string msg, SimpleLogger.LocationObject location, SimpleLogger.LogLevelType logLevel = SimpleLogger.LogLevelType.ERROR)
        {
            if (Debugger.IsAttached)
                errorLogger.LogTo = SimpleLogger.LogToType.DEBUG;
            errorLogger.Log(logLevel, msg, location);
        }
    }
}
