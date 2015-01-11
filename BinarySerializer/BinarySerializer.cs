﻿using System;
using System.Collections.Generic;
using System.IO;
using BinarySerialization.Graph.TypeGraph;

namespace BinarySerialization
{
    /// <summary>
    ///     Declaratively serializes and deserializes an object, or a graph of connected objects, in binary format.
    ///     <seealso cref="IgnoreAttribute" />
    ///     <seealso cref="FieldOrderAttribute" />
    ///     <seealso cref="SerializeAsEnumAttribute" />
    ///     <seealso cref="FieldOffsetAttribute" />
    ///     <seealso cref="FieldLengthAttribute" />
    ///     <seealso cref="FieldCountAttribute" />
    ///     <seealso cref="SubtypeAttribute" />
    ///     <seealso cref="SerializeWhenAttribute" />
    ///     <seealso cref="SerializeUntilAttribute" />
    ///     <seealso cref="ItemLengthAttribute" />
    ///     <seealso cref="ItemSerializeUntilAttribute" />
    ///     <seealso cref="IBinarySerializable" />
    /// </summary>
    public class BinarySerializer
    {
        private readonly Dictionary<Type, RootTypeNode> _graphCache = new Dictionary<Type, RootTypeNode>();
        private readonly object _graphCacheLock = new object();

        private const Endianness DefaultEndianness = Endianness.Little;
        private Endianness _endianness;

        /// <summary>
        /// Constructs an instance of BinarySerializer.
        /// </summary>
        public BinarySerializer()
        {
            Endianness = DefaultEndianness;
        }

        /// <summary>
        ///     The default <see cref="Endianness" /> to use during serialization or deserialization.
        ///     This property can be updated dynamically during serialization or deserialization.
        /// </summary>
        public Endianness Endianness
        {
            get { return _endianness; }

            set
            {
                foreach (RootTypeNode graph in _graphCache.Values)
                    graph.Endianness = value;

                _endianness = value;
            }
        }

        /// <summary>
        ///     Occurrs after a member has been serialized.
        /// </summary>
        public event EventHandler<MemberSerializedEventArgs> MemberSerialized;

        /// <summary>
        ///     Occurrs after a member has been deserialized.
        /// </summary>
        public event EventHandler<MemberSerializedEventArgs> MemberDeserialized;

        /// <summary>
        ///     Occurrs before a member has been serialized.
        /// </summary>
        public event EventHandler<MemberSerializingEventArgs> MemberSerializing;

        /// <summary>
        ///     Occurrs before a member has been deserialized.
        /// </summary>
        public event EventHandler<MemberSerializingEventArgs> MemberDeserializing;


        private RootTypeNode GetGraph(Type valueType)
        {
            RootTypeNode graph;
            if (_graphCache.TryGetValue(valueType, out graph))
                return graph;

            lock (_graphCacheLock)
            {
                if (_graphCache.TryGetValue(valueType, out graph))
                    return graph;

                graph = new RootTypeNode(valueType) {Endianness = Endianness};
                graph.MemberSerializing += OnMemberSerializing;
                graph.MemberSerialized += OnMemberSerialized;
                graph.MemberDeserializing += OnMemberDeserializing;
                graph.MemberDeserialized += OnMemberDeserialized;
                _graphCache.Add(valueType, graph);

                return graph;
            }
        }

        /// <summary>
        ///     Serializes the object, or graph of objects with the specified top (root), to the given stream.
        /// </summary>
        /// <param name="stream">The stream to which the graph is to be serialized.</param>
        /// <param name="value">The object at the root of the graph to serialize.</param>
        /// <param name="context">An optional serialization context.</param>
        public void Serialize(Stream stream, object value, object context = null)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (value == null)
                return;

            RootTypeNode graph = GetGraph(value.GetType());

            // graph.SerializationContext = context;

            var valueGraph = graph.Serialize(null, value);

            valueGraph.Bind();

            valueGraph.Serialize(stream);
        }

        /// <summary>
        ///     Calculates the serialized length of the object.
        /// </summary>
        /// <param name="value">The object.</param>
        /// <param name="context">An optional context.</param>
        /// <returns>The length of the specified object graph when serialized.</returns>
        public long SizeOf(object value, object context = null)
        {
            var nullStream = new NullStream();
            Serialize(nullStream, value, context);
            return nullStream.Length;
        }

        /// <summary>
        ///     Deserializes the specified stream into an object graph.
        /// </summary>
        /// <typeparam name="T">The type of the root of the object graph.</typeparam>
        /// <param name="stream">The stream from which to deserialize the object graph.</param>
        /// <param name="context">An optional serialization context.</param>
        /// <returns>The deserialized object graph.</returns>
        public T Deserialize<T>(Stream stream, object context = null)
        {
            RootTypeNode graph = GetGraph(typeof (T));
            //graph.SerializationContext = context;

            //graph.Deserialize(new StreamLimiter(stream));

            //return (T) graph.Value;

            throw new NotImplementedException();
        }

        /// <summary>
        ///     Deserializes the specified stream into an object graph.
        /// </summary>
        /// <typeparam name="T">The type of the root of the object graph.</typeparam>
        /// <param name="data">The byte array from which to deserialize the object graph.</param>
        /// <param name="context">An optional serialization context.</param>
        /// <returns>The deserialized object graph.</returns>
        public T Deserialize<T>(byte[] data, object context = null)
        {
            return Deserialize<T>(new MemoryStream(data), context);
        }


        private void OnMemberSerialized(object sender, MemberSerializedEventArgs e)
        {
            EventHandler<MemberSerializedEventArgs> handler = MemberSerialized;
            if (handler != null)
                handler(sender, e);
        }

        private void OnMemberDeserialized(object sender, MemberSerializedEventArgs e)
        {
            EventHandler<MemberSerializedEventArgs> handler = MemberDeserialized;
            if (handler != null)
                handler(sender, e);
        }

        private void OnMemberSerializing(object sender, MemberSerializingEventArgs e)
        {
            EventHandler<MemberSerializingEventArgs> handler = MemberSerializing;
            if (handler != null)
                handler(sender, e);
        }

        private void OnMemberDeserializing(object sender, MemberSerializingEventArgs e)
        {
            EventHandler<MemberSerializingEventArgs> handler = MemberDeserializing;
            if (handler != null)
                handler(sender, e);
        }
    }
}