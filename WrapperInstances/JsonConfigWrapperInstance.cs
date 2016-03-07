using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Aragas.Core.Wrappers;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class ConfigIgnoreContractResolver : DefaultContractResolver
    {
        private Type AttributeToIgnore { get; } = typeof (ConfigIgnoreAttribute);

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var list = type.GetProperties()
                        .Where(x => x.GetCustomAttributes().All(a => a.GetType() != AttributeToIgnore))
                        .Select(p => new JsonProperty()
                        {
                            PropertyName = p.Name,
                            PropertyType = p.PropertyType,
                            Readable = true,
                            Writable = true,
                            ValueProvider = CreateMemberValueProvider(p)
                        }).ToList();

            return list;
        }
    }

    public class JsonConfigWrapperInstance : IConfigWrapper
    {
        public string FileExtension => "json";

        private JsonSerializerSettings Settings { get; }


        public JsonConfigWrapperInstance()
        {
            Settings = new JsonSerializerSettings
            {
                ContractResolver = new ConfigIgnoreContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new JsonConverter[]{ new StringEnumConverter() }
            };
        }

        public string Serialize<T>(T target)
        {
            try { return JsonConvert.SerializeObject(target, Settings); }
            catch (JsonWriterException ex) { throw new ConfigSerializingException(string.Empty, ex); }
        }
        public T Deserialize<T>(string value)
        {
            try { return JsonConvert.DeserializeObject<T>(value, Settings); }
            catch (JsonReaderException ex) { throw new ConfigDeserializingException(string.Empty, ex); }
        }
        public void PopulateObject<T>(string value, T target)
        {
            try { JsonConvert.PopulateObject(value, target, Settings); }
            catch (JsonReaderException ex) { throw new ConfigDeserializingException(string.Empty, ex); }
        }
    }

    public class JsonConfigFactoryInstance : IConfigFactory
    {

        public IConfigWrapper Create() => new JsonConfigWrapperInstance();
    }
}
