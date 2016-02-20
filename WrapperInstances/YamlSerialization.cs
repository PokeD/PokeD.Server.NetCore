using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Aragas.Core.Wrappers;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.ObjectGraphVisitors;
using YamlDotNet.Serialization.TypeInspectors;
using YamlDotNet.Serialization.TypeResolvers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    internal static class YamlExtensions
    {
        public static bool HasDefaultConstructor(this Type type) => type.IsValueType || type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;

        public static Type GetImplementedGenericInterface(Type type, Type genericInterfaceType) => GetImplementedInterfaces(type).FirstOrDefault(interfacetype => interfacetype.IsGenericType() && interfacetype.GetGenericTypeDefinition() == genericInterfaceType);
        private static IEnumerable<Type> GetImplementedInterfaces(Type type)
        {
            if (type.IsInterface())
                yield return type;
            
            foreach (var implementedInterface in type.GetInterfaces())
                yield return implementedInterface;
        }

        public static PropertyInfo GetPublicProperty(this Type type, string name) => type.GetProperty(name);

        public static MethodInfo GetPublicInstanceMethod(this Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
    }


    internal sealed class GenericDictionaryToNonGenericAdapter : IDictionary
    {
        private object GenericDictionary { get; }
        private Type GenericDictionaryType { get; }
        private MethodInfo IndexerSetter { get; }

        public GenericDictionaryToNonGenericAdapter(object genericDictionary, Type genericDictionaryType)
        {
            GenericDictionary = genericDictionary;
            GenericDictionaryType = genericDictionaryType;

            IndexerSetter = genericDictionaryType.GetPublicProperty("Item").GetSetMethod();
        }

        public void Add(object key, object value) { throw new NotImplementedException(); }
        public void Clear() { throw new NotImplementedException(); }
        public bool Contains(object key) { throw new NotImplementedException(); }
        public IDictionaryEnumerator GetEnumerator() => new DictionaryEnumerator(GenericDictionary, GenericDictionaryType);
        public bool IsFixedSize { get { throw new NotImplementedException(); } }
        public bool IsReadOnly { get { throw new NotImplementedException(); } }
        public ICollection Keys { get { throw new NotImplementedException(); } }
        public void Remove(object key) { throw new NotImplementedException(); }
        public ICollection Values { get { throw new NotImplementedException(); } }
        public object this[object key] { get { throw new NotImplementedException(); } set { IndexerSetter.Invoke(GenericDictionary, new[] { key, value }); } }
        public void CopyTo(Array array, int index) { throw new NotImplementedException(); }
        public int Count { get { throw new NotImplementedException(); } }
        public bool IsSynchronized { get { throw new NotImplementedException(); } }
        public object SyncRoot { get { throw new NotImplementedException(); } }
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)GenericDictionary).GetEnumerator();

        private class DictionaryEnumerator : IDictionaryEnumerator
        {
            private IEnumerator Enumerator { get; }
            private MethodInfo GetKeyMethod { get; }
            private MethodInfo GetValueMethod { get; }

            public DictionaryEnumerator(object genericDictionary, Type genericDictionaryType)
            {
                var genericArguments = genericDictionaryType.GetGenericArguments();
                var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(genericArguments);

                GetKeyMethod = keyValuePairType.GetPublicProperty("Key").GetGetMethod();
                GetValueMethod = keyValuePairType.GetPublicProperty("Value").GetGetMethod();

                Enumerator = ((IEnumerable)genericDictionary).GetEnumerator();
            }

            public DictionaryEntry Entry => new DictionaryEntry(Key, Value);
            public object Key => GetKeyMethod.Invoke(Enumerator.Current, null);
            public object Value => GetValueMethod.Invoke(Enumerator.Current, null);
            public object Current => Entry;
            public bool MoveNext() => Enumerator.MoveNext();
            public void Reset() { Enumerator.Reset(); }
        }
    }

    internal class FullObjectGraphTraversalStrategy : IObjectGraphTraversalStrategy
    {
        protected YamlSerializer Serializer { get; }
        private int MaxRecursion { get; }
        private ITypeInspector TypeDescriptor { get; }
        private ITypeResolver TypeResolver { get; }
        private INamingConvention NamingConvention { get; }

        public FullObjectGraphTraversalStrategy(YamlSerializer serializer, ITypeInspector typeDescriptor, ITypeResolver typeResolver, int maxRecursion, INamingConvention namingConvention)
        {
            if (maxRecursion <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRecursion), maxRecursion, "MaxRecursion must be greater than 1");
            Serializer = serializer;

            if (typeDescriptor == null)
                throw new ArgumentNullException(nameof(typeDescriptor));
            TypeDescriptor = typeDescriptor;

            if (typeResolver == null)
                throw new ArgumentNullException(nameof(typeResolver));
            TypeResolver = typeResolver;

            MaxRecursion = maxRecursion;
            NamingConvention = namingConvention;
        }

        void IObjectGraphTraversalStrategy.Traverse(IObjectDescriptor graph, IObjectGraphVisitor visitor) { Traverse(graph, visitor, 0); }

        protected virtual void Traverse(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            if (++currentDepth > MaxRecursion)
                throw new InvalidOperationException("Too much recursion when traversing the object graph");

            if (!visitor.Enter(value))
                return;


            var typeCode = Type.GetTypeCode(value.Type);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.String:
                case TypeCode.Char:
                case TypeCode.DateTime:
                    visitor.VisitScalar(value);
                    break;

                case TypeCode.DBNull:
                    visitor.VisitScalar(new ObjectDescriptor(null, typeof(object), typeof(object)));
                    break;

                case TypeCode.Empty:
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "TypeCode.{0} is not supported.", typeCode));

                default:
                    if (value.Value == null || value.Type == typeof(TimeSpan))
                    {
                        visitor.VisitScalar(value);
                        break;
                    }

                    var underlyingType = Nullable.GetUnderlyingType(value.Type);
                    if (underlyingType != null)
                        Traverse(new ObjectDescriptor(value.Value, underlyingType, value.Type, value.ScalarStyle), visitor, currentDepth);
                    else
                        TraverseObject(value, visitor, currentDepth);
                    break;
            }
        }

        protected virtual void TraverseObject(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            if (typeof(IDictionary).IsAssignableFrom(value.Type))
            {
                TraverseDictionary(value, visitor, currentDepth, typeof(object), typeof(object));
                return;
            }

            var genericDictionaryType = YamlExtensions.GetImplementedGenericInterface(value.Type, typeof(IDictionary<,>));
            if (genericDictionaryType != null)
            {
                var adaptedDictionary = new GenericDictionaryToNonGenericAdapter(value.Value, genericDictionaryType);
                var genericArguments = genericDictionaryType.GetGenericArguments();
                TraverseDictionary(new ObjectDescriptor(adaptedDictionary, value.Type, value.StaticType, value.ScalarStyle), visitor, currentDepth, genericArguments[0], genericArguments[1]);
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(value.Type))
            {
                TraverseList(value, visitor, currentDepth);
                return;
            }

            TraverseProperties(value, visitor, currentDepth);
        }

        protected virtual void TraverseDictionary(IObjectDescriptor dictionary, IObjectGraphVisitor visitor, int currentDepth, Type keyType, Type valueType)
        {
            visitor.VisitMappingStart(dictionary, keyType, valueType);

            var isDynamic = dictionary.Type.FullName.Equals("System.Dynamic.ExpandoObject");
            foreach (DictionaryEntry entry in (IDictionary)dictionary.Value)
            {
                var keyString = isDynamic ? NamingConvention.Apply(entry.Key.ToString()) : entry.Key.ToString();
                var key = GetObjectDescriptor(keyString, keyType);
                var value = GetObjectDescriptor(entry.Value, valueType);

                if (visitor.EnterMapping(key, value))
                {
                    Traverse(key, visitor, currentDepth);
                    Traverse(value, visitor, currentDepth);
                }
            }

            visitor.VisitMappingEnd(dictionary);
        }

        private void TraverseList(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            var enumerableType = YamlExtensions.GetImplementedGenericInterface(value.Type, typeof(IEnumerable<>));
            var itemType = enumerableType != null ? enumerableType.GetGenericArguments()[0] : typeof(object);

            visitor.VisitSequenceStart(value, itemType);

            foreach (var item in (IEnumerable)value.Value)
                Traverse(GetObjectDescriptor(item, itemType), visitor, currentDepth);

            visitor.VisitSequenceEnd(value);
        }

        protected virtual void TraverseProperties(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            visitor.VisitMappingStart(value, typeof(string), typeof(object));

            foreach (var propertyDescriptor in TypeDescriptor.GetProperties(value.Type, value.Value))
            {
                var propertyValue = propertyDescriptor.Read(value.Value);

                if (visitor.EnterMapping(propertyDescriptor, propertyValue))
                {
                    Traverse(new ObjectDescriptor(propertyDescriptor.Name, typeof(string), typeof(string)), visitor, currentDepth);
                    Traverse(propertyValue, visitor, currentDepth);
                }
            }

            visitor.VisitMappingEnd(value);
        }

        private IObjectDescriptor GetObjectDescriptor(object value, Type staticType) => new ObjectDescriptor(value, TypeResolver.Resolve(staticType, value), staticType);
    }

    internal class RoundtripObjectGraphTraversalStrategy : FullObjectGraphTraversalStrategy
    {
        public RoundtripObjectGraphTraversalStrategy(YamlSerializer serializer, ITypeInspector typeDescriptor, ITypeResolver typeResolver, int maxRecursion)
            : base(serializer, typeDescriptor, typeResolver, maxRecursion, null) { }

        protected override void TraverseProperties(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            if (!value.Type.HasDefaultConstructor() && !Serializer.Converters.Any(c => c.Accepts(value.Type)))
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Type '{0}' cannot be deserialized because it does not have a default constructor or a type converter.", value.Type));

            base.TraverseProperties(value, visitor, currentDepth);
        }
    }

    internal static class YamlTypeConverters
    {
        public static IEnumerable<IYamlTypeConverter> BuiltInConverters { get; } = new IYamlTypeConverter[]
        {
            new GuidConverter(),
        };
    }


    internal sealed class ConfigIgnoreTypeInspector : TypeInspectorSkeleton
    {
        private ITypeInspector InnerTypeDescriptor { get; }

        public ConfigIgnoreTypeInspector(ITypeInspector innerTypeDescriptor) { InnerTypeDescriptor = innerTypeDescriptor; }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            return InnerTypeDescriptor.GetProperties(type, container)
                .Where(p => p.GetCustomAttribute<YamlIgnoreAttribute>() == null && p.GetCustomAttribute<ConfigIgnoreAttribute>() == null)
                .Select(p =>
                {
                    var descriptor = new PropertyDescriptor(p);

#pragma warning disable 0618 // 'YamlDotNet.Serialization.YamlAliasAttribute' is obsolete: 'Please use YamlMember instead'
                    var alias = p.GetCustomAttribute<YamlAliasAttribute>();
                    if (alias != null)
                    {
                        descriptor.Name = alias.Alias;
                    }
#pragma warning restore 0618 // 'YamlDotNet.Serialization.YamlAliasAttribute' is obsolete: 'Please use YamlMember instead'

                    var member = p.GetCustomAttribute<YamlMemberAttribute>();
                    if (member != null)
                    {
                        if (member.SerializeAs != null)
                            descriptor.TypeOverride = member.SerializeAs;

                        descriptor.Order = member.Order;
                        descriptor.ScalarStyle = member.ScalarStyle;

                        if (member.Alias != null)
                        {
                            if (alias != null)
                                throw new InvalidOperationException("Mixing YamlAlias(...) with YamlMember(Alias = ...) is an error. The YamlAlias attribute is obsolete and should be removed.");

                            descriptor.Name = member.Alias;
                        }
                    }

                    return (IPropertyDescriptor)descriptor;
                })
                .OrderBy(p => p.Order);
        }
    }

    internal sealed class YamlSerializer
    {
        internal IList<IYamlTypeConverter> Converters { get; }

        private SerializationOptions Options { get; }
        private INamingConvention NamingConvention { get; }
        private ITypeResolver TypeResolver { get; }

        public YamlSerializer(SerializationOptions options = SerializationOptions.None, INamingConvention namingConvention = null)
        {
            Options = options;
            NamingConvention = namingConvention ?? new NullNamingConvention();

            Converters = new List<IYamlTypeConverter>();
            foreach (var yamlTypeConverter in YamlTypeConverters.BuiltInConverters)
                Converters.Add(yamlTypeConverter);

            TypeResolver = IsOptionSet(SerializationOptions.DefaultToStaticType)
                ? (ITypeResolver)new StaticTypeResolver()
                : (ITypeResolver)new DynamicTypeResolver();
        }

        private bool IsOptionSet(SerializationOptions option) => (Options & option) != 0;

        public void RegisterTypeConverter(IYamlTypeConverter converter) { Converters.Add(converter); }

        public void Serialize(TextWriter writer, object graph) { Serialize(new Emitter(writer), graph); }
        public void Serialize(TextWriter writer, object graph, Type type) { Serialize(new Emitter(writer), graph, type); }
        public void Serialize(IEmitter emitter, object graph)
        {
            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));

            EmitDocument(emitter, new ObjectDescriptor(graph, graph?.GetType() ?? typeof(object), typeof(object)));
        }
        public void Serialize(IEmitter emitter, object graph, Type type)
        {
            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            EmitDocument(emitter, new ObjectDescriptor(graph, type, type));
        }

        private void EmitDocument(IEmitter emitter, IObjectDescriptor graph)
        {
            var traversalStrategy = CreateTraversalStrategy();
            var eventEmitter = CreateEventEmitter(emitter);
            var emittingVisitor = CreateEmittingVisitor(emitter, traversalStrategy, eventEmitter, graph);

            emitter.Emit(new StreamStart());
            emitter.Emit(new DocumentStart());

            traversalStrategy.Traverse(graph, emittingVisitor);

            emitter.Emit(new DocumentEnd(true));
            emitter.Emit(new StreamEnd());
        }

        private IObjectGraphVisitor CreateEmittingVisitor(IEmitter emitter, IObjectGraphTraversalStrategy traversalStrategy, IEventEmitter eventEmitter, IObjectDescriptor graph)
        {
            IObjectGraphVisitor emittingVisitor = new EmittingObjectGraphVisitor(eventEmitter);

            emittingVisitor = new CustomSerializationObjectGraphVisitor(emitter, emittingVisitor, Converters);

            if (!IsOptionSet(SerializationOptions.DisableAliases))
            {
                var anchorAssigner = new AnchorAssigner();
                traversalStrategy.Traverse(graph, anchorAssigner);

                emittingVisitor = new AnchorAssigningObjectGraphVisitor(emittingVisitor, eventEmitter, anchorAssigner);
            }

            if (!IsOptionSet(SerializationOptions.EmitDefaults))
                emittingVisitor = new DefaultExclusiveObjectGraphVisitor(emittingVisitor);

            return emittingVisitor;
        }

        private IEventEmitter CreateEventEmitter(IEmitter emitter)
        {
            var writer = new WriterEventEmitter(emitter);

            if (IsOptionSet(SerializationOptions.JsonCompatible))
                return new JsonEventEmitter(writer);
            else
                return new TypeAssigningEventEmitter(writer, IsOptionSet(SerializationOptions.Roundtrip));
        }

        private IObjectGraphTraversalStrategy CreateTraversalStrategy()
        {
            ITypeInspector typeDescriptor = new ReadablePropertiesTypeInspector(TypeResolver);
            if (IsOptionSet(SerializationOptions.Roundtrip))
                typeDescriptor = new ReadableAndWritablePropertiesTypeInspector(typeDescriptor);

            typeDescriptor = new NamingConventionTypeInspector(typeDescriptor, NamingConvention);
            typeDescriptor = new ConfigIgnoreTypeInspector(typeDescriptor);
            if (IsOptionSet(SerializationOptions.DefaultToStaticType))
                typeDescriptor = new CachedTypeInspector(typeDescriptor);

            if (IsOptionSet(SerializationOptions.Roundtrip))
                return new RoundtripObjectGraphTraversalStrategy(this, typeDescriptor, TypeResolver, 50);
            else
                return new FullObjectGraphTraversalStrategy(this, typeDescriptor, TypeResolver, 50, NamingConvention);
        }
    }
}
