using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Aragas.Core.Wrappers;

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class YamlConfigWrapperInstance : IConfigWrapper
    {
        public string FileExtension => "yml";

        public string Serialize<T>(T target)
        {
            try
            {
                using (var stringWriter = new StringWriter())
                {
                    new YamlSerializer(SerializationOptions.EmitDefaults).Serialize(stringWriter, target);
                    return stringWriter.ToString();
                }
            }
            catch (YamlException ex) { throw new ConfigSerializingException(string.Empty, ex); }
        }
        public T Deserialize<T>(string value)
        {
            try { return (T) new Deserializer().Deserialize(new StringReader(value), typeof (T)); }
            catch (YamlException ex) { throw new ConfigDeserializingException(string.Empty, ex); }
        }
        public void PopulateObject<T>(string value, T target)
        {
            try
            {
                var source = (T) new Deserializer(new LambdaObjectFactory(Factory)).Deserialize(new StringReader(value), typeof(T));
                CopyAll(source, target);
            }
            catch (YamlException ex) { throw new ConfigDeserializingException(string.Empty, ex); }
        }
        
        private static object Factory(Type type)
        {
            if (type == typeof (string))
                return string.Empty;
            else if (type.HasDefaultConstructor())
                return Activator.CreateInstance(type);
            else
                return FormatterServices.GetUninitializedObject(type);
        }

        private static void CopyAll<T>(T source, T target)
        {
            var type = typeof(T);
            foreach (var sourceProperty in type.GetRuntimeProperties().Where(prop => prop.CanRead && prop.GetMethod.IsPublic && prop.CustomAttributes.All(att => att.AttributeType != typeof(YamlIgnoreAttribute))))
            {
                var targetProperty = type.GetRuntimeProperty(sourceProperty.Name);
                if(targetProperty.CanWrite)
                    targetProperty.SetValue(target, sourceProperty.GetValue(source, null), null);
            }
            foreach (var sourceField in type.GetRuntimeFields().Where(field => field.IsPublic && field.CustomAttributes.All(att => att.AttributeType != typeof(YamlIgnoreAttribute))))
            {
                var targetField = type.GetRuntimeField(sourceField.Name);
                targetField.SetValue(target, sourceField.GetValue(source));
            }
        }
    }

    public class YamlConfigFactoryInstance : IConfigFactory
    {
        public IConfigWrapper CreateConfig() => new YamlConfigWrapperInstance();
    }
}
