using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Contoso.Infrastructure.Messaging
{
    public static class JsonSerializationSettings
    {

        public static JsonSerializerSettings Settings => new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
            Formatting = Formatting.Indented,
        };

    }
}