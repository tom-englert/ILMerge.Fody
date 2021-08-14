// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable CollectionNeverQueried.Global
namespace Baml
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    internal class BamlElement
    {
        public BamlElement(BamlRecord header)
        {
            Header = header;
        }

        public BamlRecord Header { get; }
        public IList<BamlRecord> Body { get; } = new List<BamlRecord>();
        public IList<BamlElement> Children { get; } = new List<BamlElement>();

        public BamlElement? Parent { get; private set; }
        public BamlRecord? Footer { get; private set; }

        private static bool IsHeader(BamlRecord rec)
        {
            return rec.Type switch
            {
                BamlRecordType.ConstructorParametersStart => true,
                BamlRecordType.DocumentStart => true,
                BamlRecordType.ElementStart => true,
                BamlRecordType.KeyElementStart => true,
                BamlRecordType.NamedElementStart => true,
                BamlRecordType.PropertyArrayStart => true,
                BamlRecordType.PropertyComplexStart => true,
                BamlRecordType.PropertyDictionaryStart => true,
                BamlRecordType.PropertyListStart => true,
                BamlRecordType.StaticResourceStart => true,
                _ => false
            };
        }

        private static bool IsFooter(BamlRecord rec)
        {
            return rec.Type switch
            {
                BamlRecordType.ConstructorParametersEnd => true,
                BamlRecordType.DocumentEnd => true,
                BamlRecordType.ElementEnd => true,
                BamlRecordType.KeyElementEnd => true,
                BamlRecordType.PropertyArrayEnd => true,
                BamlRecordType.PropertyComplexEnd => true,
                BamlRecordType.PropertyDictionaryEnd => true,
                BamlRecordType.PropertyListEnd => true,
                BamlRecordType.StaticResourceEnd => true,
                _ => false
            };
        }

        private static bool IsMatch(BamlRecord header, BamlRecord footer)
        {
            return header.Type switch
            {
                BamlRecordType.ConstructorParametersStart => footer.Type == BamlRecordType.ConstructorParametersEnd,
                BamlRecordType.DocumentStart => footer.Type == BamlRecordType.DocumentEnd,
                BamlRecordType.KeyElementStart => footer.Type == BamlRecordType.KeyElementEnd,
                BamlRecordType.PropertyArrayStart => footer.Type == BamlRecordType.PropertyArrayEnd,
                BamlRecordType.PropertyComplexStart => footer.Type == BamlRecordType.PropertyComplexEnd,
                BamlRecordType.PropertyDictionaryStart => footer.Type == BamlRecordType.PropertyDictionaryEnd,
                BamlRecordType.PropertyListStart => footer.Type == BamlRecordType.PropertyListEnd,
                BamlRecordType.StaticResourceStart => footer.Type == BamlRecordType.StaticResourceEnd,
                BamlRecordType.ElementStart => footer.Type == BamlRecordType.ElementEnd,
                BamlRecordType.NamedElementStart => footer.Type == BamlRecordType.ElementEnd,
                _ => false
            };
        }

        public static BamlElement? Read(IList<BamlRecord> records)
        {
            Debug.Assert(records.Count > 0 && records[0].Type == BamlRecordType.DocumentStart);

            BamlElement? current = null;
            var stack = new Stack<BamlElement>();

            foreach (var record in records)
            {
                if (IsHeader(record))
                {
                    var prev = current;

                    current = new BamlElement(record);

                    if (prev != null)
                    {
                        prev.Children.Add(current);
                        current.Parent = prev;
                        stack.Push(prev);
                    }
                }
                else if (IsFooter(record))
                {
                    if (current == null)
                        throw new InvalidOperationException("Unexpected footer.");

                    // ReSharper disable once PossibleNullReferenceException
                    while (!IsMatch(current.Header, record))
                    {
                        // End record can be omitted (sometimes).
                        if (stack.Count > 0)
                            current = stack.Pop();
                    }
                    current.Footer = record;
                    if (stack.Count > 0)
                        current = stack.Pop();
                }
                else
                {
                    if (current == null)
                        throw new InvalidOperationException("Unexpected record.");

                    current.Body.Add(record);
                }
            }

            Debug.Assert(stack.Count == 0);
            return current;
        }
    }

    internal enum BamlRecordType : byte
    {
        ClrEvent = 0x13,
        Comment = 0x17,
        AssemblyInfo = 0x1c,
        AttributeInfo = 0x1f,
        ConstructorParametersStart = 0x2a,
        ConstructorParametersEnd = 0x2b,
        ConstructorParameterType = 0x2c,
        ConnectionId = 0x2d,
        ContentProperty = 0x2e,
        DefAttribute = 0x19,
        DefAttributeKeyString = 0x26,
        DefAttributeKeyType = 0x27,
        DeferableContentStart = 0x25,
        DefTag = 0x18,
        DocumentEnd = 0x2,
        DocumentStart = 0x1,
        ElementEnd = 0x4,
        ElementStart = 0x3,
        EndAttributes = 0x1a,
        KeyElementEnd = 0x29,
        KeyElementStart = 0x28,
        LastRecordType = 0x39,
        LineNumberAndPosition = 0x35,
        LinePosition = 0x36,
        LiteralContent = 0xf,
        NamedElementStart = 0x2f,
        OptimizedStaticResource = 0x37,
        PIMapping = 0x1b,
        PresentationOptionsAttribute = 0x34,
        ProcessingInstruction = 0x16,
        Property = 0x5,
        PropertyArrayEnd = 0xa,
        PropertyArrayStart = 0x9,
        PropertyComplexEnd = 0x8,
        PropertyComplexStart = 0x7,
        PropertyCustom = 0x6,
        PropertyDictionaryEnd = 0xe,
        PropertyDictionaryStart = 0xd,
        PropertyListEnd = 0xc,
        PropertyListStart = 0xb,
        PropertyStringReference = 0x21,
        PropertyTypeReference = 0x22,
        PropertyWithConverter = 0x24,
        PropertyWithExtension = 0x23,
        PropertyWithStaticResourceId = 0x38,
        RoutedEvent = 0x12,
        StaticResourceEnd = 0x31,
        StaticResourceId = 0x32,
        StaticResourceStart = 0x30,
        StringInfo = 0x20,
        Text = 0x10,
        TextWithConverter = 0x11,
        TextWithId = 0x33,
        TypeInfo = 0x1d,
        TypeSerializerInfo = 0x1e,
        XmlAttribute = 0x15,
        XmlnsProperty = 0x14
    }

    internal abstract class BamlRecord
    {
        public abstract BamlRecordType Type { get; }
        public long Position { get; internal set; }
        public abstract void Read(BamlBinaryReader reader);
        public abstract void Write(BamlBinaryWriter writer);

        public virtual void ReadDeferred(IList<BamlRecord> records, int index, IDictionary<long, BamlRecord> recordsByPosition) { }
        public virtual void WriteDeferred(IList<BamlRecord> records, int index, BamlBinaryWriter writer) { }

        protected static void NavigateTree(IList<BamlRecord> records, ref int index)
        {
            while (true)
            {
                switch (records[index].Type)
                {
                    case BamlRecordType.DefAttributeKeyString:
                    case BamlRecordType.DefAttributeKeyType:
                    case BamlRecordType.OptimizedStaticResource:
                        break;

                    case BamlRecordType.StaticResourceStart:
                        NavigateTree(records, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
                        break;

                    case BamlRecordType.KeyElementStart:
                        NavigateTree(records, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
                        break;

                    default:
                        return;
                }

                index++;
            }
        }

        private static void NavigateTree(IList<BamlRecord> records, BamlRecordType start, BamlRecordType end, ref int index)
        {
            index++;

            while (true) //Assume there always is a end
            {
                var recordType = records[index].Type;

                if (recordType == start)
                {
                    NavigateTree(records, start, end, ref index);
                }
                else if (recordType == end)
                {
                    return;
                }

                index++;
            }
        }
    }

    internal abstract class SizedBamlRecord : BamlRecord
    {
        public override void Read(BamlBinaryReader reader)
        {
            var pos = reader.BaseStream.Position;
            var size = reader.ReadEncodedInt();

            ReadData(reader, size - (int)(reader.BaseStream.Position - pos));
            Debug.Assert(reader.BaseStream.Position - pos == size);
        }

        private static int SizeofEncodedInt(int val)
        {
            if ((val & ~0x7F) == 0)
            {
                return 1;
            }
            if ((val & ~0x3FFF) == 0)
            {
                return 2;
            }
            if ((val & ~0x1FFFFF) == 0)
            {
                return 3;
            }
            if ((val & ~0xFFFFFFF) == 0)
            {
                return 4;
            }
            return 5;
        }

        public override void Write(BamlBinaryWriter writer)
        {
            var pos = writer.BaseStream.Position;
            WriteData(writer);
            var size = (int)(writer.BaseStream.Position - pos);
            size = SizeofEncodedInt(SizeofEncodedInt(size) + size) + size;
            writer.BaseStream.Position = pos;
            writer.WriteEncodedInt(size);
            WriteData(writer);
        }

        protected abstract void ReadData(BamlBinaryReader reader, int size);
        protected abstract void WriteData(BamlBinaryWriter writer);
    }

    internal class XmlnsPropertyRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.XmlnsProperty;

        public string Prefix { get; set; } = string.Empty;
        public string XmlNamespace { get; set; } = string.Empty;
        public ushort[] AssemblyIds { get; set; } = Array.Empty<ushort>();

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            Prefix = reader.ReadString();
            XmlNamespace = reader.ReadString();
            AssemblyIds = new ushort[reader.ReadUInt16()];
            for (var i = 0; i < AssemblyIds.Length; i++)
                AssemblyIds[i] = reader.ReadUInt16();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(Prefix);
            writer.Write(XmlNamespace);
            writer.Write((ushort)AssemblyIds.Length);
            foreach (var i in AssemblyIds)
                writer.Write(i);
        }
    }

    internal class PresentationOptionsAttributeRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PresentationOptionsAttribute;

        public string Value { get; set; } = string.Empty;
        public ushort NameId { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            Value = reader.ReadString();
            NameId = reader.ReadUInt16();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(Value);
            writer.Write(NameId);
        }
    }

    internal class PIMappingRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PIMapping;

        public string XmlNamespace { get; set; } = string.Empty;
        public string ClrNamespace { get; set; } = string.Empty;
        public ushort AssemblyId { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            XmlNamespace = reader.ReadString();
            ClrNamespace = reader.ReadString();
            AssemblyId = reader.ReadUInt16();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(XmlNamespace);
            writer.Write(ClrNamespace);
            writer.Write(AssemblyId);
        }
    }

    internal class AssemblyInfoRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.AssemblyInfo;

        public ushort AssemblyId { get; set; }
        public string AssemblyFullName { get; set; } = string.Empty;

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            AssemblyId = reader.ReadUInt16();
            AssemblyFullName = reader.ReadString();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(AssemblyId);
            writer.Write(AssemblyFullName);
        }
    }

    internal class PropertyRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.Property;

        public ushort AttributeId { get; set; }

        public string Value { get; set; } = string.Empty;

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            AttributeId = reader.ReadUInt16();
            Value = reader.ReadString();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(AttributeId);
            writer.Write(Value);
        }
    }

    internal class PropertyWithConverterRecord : PropertyRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyWithConverter;

        public ushort ConverterTypeId { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            base.ReadData(reader, size);
            ConverterTypeId = reader.ReadUInt16();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            base.WriteData(writer);
            writer.Write(ConverterTypeId);
        }
    }

    internal class PropertyCustomRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyCustom;

        public ushort AttributeId { get; set; }
        public ushort SerializerTypeId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            var pos = reader.BaseStream.Position;
            AttributeId = reader.ReadUInt16();
            SerializerTypeId = reader.ReadUInt16();
            Data = reader.ReadBytes(size - (int)(reader.BaseStream.Position - pos));
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(AttributeId);
            writer.Write(SerializerTypeId);
            writer.Write(Data);
        }
    }

    internal class DefAttributeRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.DefAttribute;

        public string Value { get; set; } = string.Empty;
        public ushort NameId { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            Value = reader.ReadString();
            NameId = reader.ReadUInt16();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(Value);
            writer.Write(NameId);
        }
    }

    internal class DefAttributeKeyStringRecord : SizedBamlRecord
    {
        private uint _position = 0xffffffff;

        public override BamlRecordType Type => BamlRecordType.DefAttributeKeyString;

        public ushort ValueId { get; set; }
        public bool Shared { get; set; }
        public bool SharedSet { get; set; }

        public BamlRecord? Record { get; set; }

        public override void ReadDeferred(IList<BamlRecord> records, int index, IDictionary<long, BamlRecord> recordsByPosition)
        {
            NavigateTree(records, ref index);

            Record = recordsByPosition[records[index].Position + _position];
        }

        public override void WriteDeferred(IList<BamlRecord> records, int index, BamlBinaryWriter writer)
        {
            if (Record == null)
                throw new InvalidOperationException("Invalid record state");

            NavigateTree(records, ref index);

            writer.BaseStream.Seek(_position, SeekOrigin.Begin);
            writer.Write((uint)(Record.Position - records[index].Position));
        }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            ValueId = reader.ReadUInt16();
            _position = reader.ReadUInt32();
            Shared = reader.ReadBoolean();
            SharedSet = reader.ReadBoolean();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(ValueId);
            _position = (uint)writer.BaseStream.Position;
            writer.Write((uint)0);
            writer.Write(Shared);
            writer.Write(SharedSet);
        }
    }

    internal class TypeInfoRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.TypeInfo;

        public ushort TypeId { get; set; }
        public ushort AssemblyId { get; set; }

        public string TypeFullName { get; set; } = string.Empty;

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            TypeId = reader.ReadUInt16();
            AssemblyId = reader.ReadUInt16();
            TypeFullName = reader.ReadString();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(AssemblyId);
            writer.Write(TypeFullName);
        }
    }

    internal class TypeSerializerInfoRecord : TypeInfoRecord
    {
        public override BamlRecordType Type => BamlRecordType.TypeSerializerInfo;

        public ushort SerializerTypeId { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            base.ReadData(reader, size);
            SerializerTypeId = reader.ReadUInt16();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            base.WriteData(writer);
            writer.Write(SerializerTypeId);
        }
    }

    internal class AttributeInfoRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.AttributeInfo;

        public ushort AttributeId { get; set; }
        public ushort OwnerTypeId { get; set; }
        public byte AttributeUsage { get; set; }
        public string Name { get; set; } = string.Empty;

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            AttributeId = reader.ReadUInt16();
            OwnerTypeId = reader.ReadUInt16();
            AttributeUsage = reader.ReadByte();
            Name = reader.ReadString();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(AttributeId);
            writer.Write(OwnerTypeId);
            writer.Write(AttributeUsage);
            writer.Write(Name);
        }
    }

    internal class StringInfoRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.StringInfo;

        public ushort StringId { get; set; }
        public string Value { get; set; } = string.Empty;

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            StringId = reader.ReadUInt16();
            Value = reader.ReadString();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(StringId);
            writer.Write(Value);
        }
    }

    internal class TextRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.Text;

        public string Value { get; set; } = string.Empty;

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            Value = reader.ReadString();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(Value);
        }
    }

    internal class TextWithConverterRecord : TextRecord
    {
        public override BamlRecordType Type => BamlRecordType.TextWithConverter;

        public ushort ConverterTypeId { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            base.ReadData(reader, size);
            ConverterTypeId = reader.ReadUInt16();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            base.WriteData(writer);
            writer.Write(ConverterTypeId);
        }
    }

    internal class TextWithIdRecord : TextRecord
    {
        public override BamlRecordType Type => BamlRecordType.TextWithId;

        public ushort ValueId { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            ValueId = reader.ReadUInt16();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(ValueId);
        }
    }

    internal class LiteralContentRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.LiteralContent;

        public string Value { get; set; } = string.Empty;
        public uint Reserved0 { get; set; }
        public uint Reserved1 { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            Value = reader.ReadString();
            Reserved0 = reader.ReadUInt32();
            Reserved1 = reader.ReadUInt32();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(Value);
            writer.Write(Reserved0);
            writer.Write(Reserved1);
        }
    }

    internal class RoutedEventRecord : SizedBamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.RoutedEvent;

        public string Value { get; set; } = string.Empty;
        public ushort AttributeId { get; set; }

        protected override void ReadData(BamlBinaryReader reader, int size)
        {
            AttributeId = reader.ReadUInt16();
            Value = reader.ReadString();
        }

        protected override void WriteData(BamlBinaryWriter writer)
        {
            writer.Write(Value);
            writer.Write(AttributeId);
        }
    }

    internal class DocumentStartRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.DocumentStart;

        public bool LoadAsync { get; set; }
        public uint MaxAsyncRecords { get; set; }
        public bool DebugBaml { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            LoadAsync = reader.ReadBoolean();
            MaxAsyncRecords = reader.ReadUInt32();
            DebugBaml = reader.ReadBoolean();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(LoadAsync);
            writer.Write(MaxAsyncRecords);
            writer.Write(DebugBaml);
        }
    }

    internal class DocumentEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.DocumentEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class ElementStartRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.ElementStart;

        public ushort TypeId { get; set; }
        public byte Flags { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            TypeId = reader.ReadUInt16();
            Flags = reader.ReadByte();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Flags);
        }
    }

    internal class ElementEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.ElementEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class KeyElementStartRecord : DefAttributeKeyTypeRecord
    {
        public override BamlRecordType Type => BamlRecordType.KeyElementStart;
    }

    internal class KeyElementEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.KeyElementEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class ConnectionIdRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.ConnectionId;

        public uint ConnectionId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            ConnectionId = reader.ReadUInt32();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(ConnectionId);
        }
    }

    internal class PropertyWithExtensionRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyWithExtension;

        public ushort AttributeId { get; set; }
        public ushort Flags { get; set; }
        public ushort ValueId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            AttributeId = reader.ReadUInt16();
            Flags = reader.ReadUInt16();
            ValueId = reader.ReadUInt16();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(AttributeId);
            writer.Write(Flags);
            writer.Write(ValueId);
        }
    }

    internal class PropertyTypeReferenceRecord : PropertyComplexStartRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyTypeReference;

        public ushort TypeId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            base.Read(reader);
            TypeId = reader.ReadUInt16();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(TypeId);
        }
    }

    internal class PropertyStringReferenceRecord : PropertyComplexStartRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyStringReference;

        public ushort StringId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            base.Read(reader);
            StringId = reader.ReadUInt16();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(StringId);
        }
    }

    internal class PropertyWithStaticResourceIdRecord : StaticResourceIdRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyWithStaticResourceId;

        public ushort AttributeId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            AttributeId = reader.ReadUInt16();
            base.Read(reader);
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(AttributeId);
            base.Write(writer);
        }
    }

    internal class ContentPropertyRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.ContentProperty;

        public ushort AttributeId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            AttributeId = reader.ReadUInt16();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(AttributeId);
        }
    }

    internal class DefAttributeKeyTypeRecord : ElementStartRecord
    {
        private uint _position = 0xffffffff;

        public override BamlRecordType Type => BamlRecordType.DefAttributeKeyType;

        public bool Shared { get; set; }
        public bool SharedSet { get; set; }

        public BamlRecord? Record { get; set; }

        public override void ReadDeferred(IList<BamlRecord> records, int index, IDictionary<long, BamlRecord> recordsByPosition)
        {
            NavigateTree(records, ref index);

            Record = recordsByPosition[records[index].Position + _position];
        }

        public override void WriteDeferred(IList<BamlRecord> records, int index, BamlBinaryWriter writer)
        {
            if (Record == null)
                throw new InvalidOperationException("Invalid record state");

            NavigateTree(records, ref index);

            writer.BaseStream.Seek(_position, SeekOrigin.Begin);
            writer.Write((uint)(Record.Position - records[index].Position));
        }

        public override void Read(BamlBinaryReader reader)
        {
            base.Read(reader);
            _position = reader.ReadUInt32();
            Shared = reader.ReadBoolean();
            SharedSet = reader.ReadBoolean();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            base.Write(writer);
            _position = (uint)writer.BaseStream.Position;
            writer.Write((uint)0);
            writer.Write(Shared);
            writer.Write(SharedSet);
        }
    }

    internal class PropertyListStartRecord : PropertyComplexStartRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyListStart;
    }

    internal class PropertyListEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyListEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class PropertyDictionaryStartRecord : PropertyComplexStartRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyDictionaryStart;
    }

    internal class PropertyDictionaryEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyDictionaryEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class PropertyArrayStartRecord : PropertyComplexStartRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyArrayStart;
    }

    internal class PropertyArrayEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyArrayEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class PropertyComplexStartRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyComplexStart;

        public ushort AttributeId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            AttributeId = reader.ReadUInt16();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(AttributeId);
        }
    }

    internal class PropertyComplexEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.PropertyComplexEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class ConstructorParametersStartRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.ConstructorParametersStart;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class ConstructorParametersEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.ConstructorParametersEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class ConstructorParameterTypeRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.ConstructorParameterType;

        public ushort TypeId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            TypeId = reader.ReadUInt16();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(TypeId);
        }
    }

    internal class DeferableContentStartRecord : BamlRecord
    {
        private long pos;
        internal uint size = 0xffffffff;

        public override BamlRecordType Type => BamlRecordType.DeferableContentStart;

        public BamlRecord? Record { get; set; }

        public override void ReadDeferred(IList<BamlRecord> records, int index, IDictionary<long, BamlRecord> recordsByPosition)
        {
            Record = recordsByPosition[pos + size];
        }

        public override void WriteDeferred(IList<BamlRecord> records, int index, BamlBinaryWriter writer)
        {
            if (Record == null)
                throw new InvalidOperationException("Invalid record state");

            writer.BaseStream.Seek(pos, SeekOrigin.Begin);
            writer.Write((uint)(Record.Position - (pos + 4)));
        }

        public override void Read(BamlBinaryReader reader)
        {
            size = reader.ReadUInt32();
            pos = reader.BaseStream.Position;
        }

        public override void Write(BamlBinaryWriter writer)
        {
            pos = writer.BaseStream.Position;
            writer.Write((uint)0);
        }
    }

    internal class StaticResourceStartRecord : ElementStartRecord
    {
        public override BamlRecordType Type => BamlRecordType.StaticResourceStart;
    }

    internal class StaticResourceEndRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.StaticResourceEnd;

        public override void Read(BamlBinaryReader reader) { }
        public override void Write(BamlBinaryWriter writer) { }
    }

    internal class StaticResourceIdRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.StaticResourceId;

        public ushort StaticResourceId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            StaticResourceId = reader.ReadUInt16();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(StaticResourceId);
        }
    }

    internal class OptimizedStaticResourceRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.OptimizedStaticResource;

        public byte Flags { get; set; }
        public ushort ValueId { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            Flags = reader.ReadByte();
            ValueId = reader.ReadUInt16();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(Flags);
            writer.Write(ValueId);
        }
    }

    internal class LineNumberAndPositionRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.LineNumberAndPosition;

        public uint LineNumber { get; set; }
        public uint LinePosition { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            LineNumber = reader.ReadUInt32();
            LinePosition = reader.ReadUInt32();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(LineNumber);
            writer.Write(LinePosition);
        }
    }

    internal class LinePositionRecord : BamlRecord
    {
        public override BamlRecordType Type => BamlRecordType.LinePosition;

        public uint LinePosition { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            LinePosition = reader.ReadUInt32();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(LinePosition);
        }
    }

    internal class NamedElementStartRecord : ElementStartRecord
    {
        public override BamlRecordType Type => BamlRecordType.NamedElementStart;

        public string? RuntimeName { get; set; }

        public override void Read(BamlBinaryReader reader)
        {
            TypeId = reader.ReadUInt16();
            RuntimeName = reader.ReadString();
        }

        public override void Write(BamlBinaryWriter writer)
        {
            writer.Write(TypeId);
            if (RuntimeName != null)
            {
                writer.Write(RuntimeName);
            }
        }
    }

    internal class BamlBinaryReader : BinaryReader
    {
        public BamlBinaryReader(Stream stream)
            : base(stream)
        {
        }

        public int ReadEncodedInt()
        {
            return Read7BitEncodedInt();
        }
    }

    internal class BamlBinaryWriter : BinaryWriter
    {
        public BamlBinaryWriter(Stream stream)
            : base(stream)
        {
        }

        public void WriteEncodedInt(int val)
        {
            Write7BitEncodedInt(val);
        }
    }

    internal static class Baml
    {
        private static readonly byte[] _signature =
        {
            0x0C, 0x00, 0x00, 0x00, // strlen

            (byte)'M', 0x00,
            (byte)'S', 0x00,
            (byte)'B', 0x00,
            (byte)'A', 0x00,
            (byte)'M', 0x00,
            (byte)'L', 0x00,

            0x00, 0x00, 0x60, 0x00, // reader version
            0x00, 0x00, 0x60, 0x00, // updater version
            0x00, 0x00, 0x60, 0x00, // writer version
        };

        public static IList<BamlRecord> ReadDocument(Stream stream)
        {
            var reader = new BamlBinaryReader(stream);

            var rawSignature = reader.ReadBytes(_signature.Length);

            if (!rawSignature.SequenceEqual(_signature))
                throw new NotSupportedException("Invalid signature");

            var records = new List<BamlRecord>();
            var recordsByPosition = new Dictionary<long, BamlRecord>();

            while (stream.Position < stream.Length)
            {
                var pos = stream.Position;
                var type = (BamlRecordType)reader.ReadByte();

                var record = BamlRecordFromType(type);

                record.Position = pos;
                record.Read(reader);

                records.Add(record);
                recordsByPosition.Add(pos, record);
            }

            for (var i = 0; i < records.Count; i++)
            {
                records[i]?.ReadDeferred(records, i, recordsByPosition);
            }

            return records;
        }

        public static void WriteDocument(IList<BamlRecord> records, Stream stream)
        {
            var writer = new BamlBinaryWriter(stream);

            writer.Write(_signature);

            foreach (var record in records)
            {
                record.Position = stream.Position;
                writer.Write((byte)record.Type);
                record.Write(writer);
            }

            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                record.WriteDeferred(records, i, writer);
            }
        }

        private static BamlRecord BamlRecordFromType(BamlRecordType type)
        {
            return type switch
            {
                BamlRecordType.AssemblyInfo => new AssemblyInfoRecord(),
                BamlRecordType.AttributeInfo => new AttributeInfoRecord(),
                BamlRecordType.ConstructorParametersStart => new ConstructorParametersStartRecord(),
                BamlRecordType.ConstructorParametersEnd => new ConstructorParametersEndRecord(),
                BamlRecordType.ConstructorParameterType => new ConstructorParameterTypeRecord(),
                BamlRecordType.ConnectionId => new ConnectionIdRecord(),
                BamlRecordType.ContentProperty => new ContentPropertyRecord(),
                BamlRecordType.DefAttribute => new DefAttributeRecord(),
                BamlRecordType.DefAttributeKeyString => new DefAttributeKeyStringRecord(),
                BamlRecordType.DefAttributeKeyType => new DefAttributeKeyTypeRecord(),
                BamlRecordType.DeferableContentStart => new DeferableContentStartRecord(),
                BamlRecordType.DocumentEnd => new DocumentEndRecord(),
                BamlRecordType.DocumentStart => new DocumentStartRecord(),
                BamlRecordType.ElementEnd => new ElementEndRecord(),
                BamlRecordType.ElementStart => new ElementStartRecord(),
                BamlRecordType.KeyElementEnd => new KeyElementEndRecord(),
                BamlRecordType.KeyElementStart => new KeyElementStartRecord(),
                BamlRecordType.LineNumberAndPosition => new LineNumberAndPositionRecord(),
                BamlRecordType.LinePosition => new LinePositionRecord(),
                BamlRecordType.LiteralContent => new LiteralContentRecord(),
                BamlRecordType.NamedElementStart => new NamedElementStartRecord(),
                BamlRecordType.OptimizedStaticResource => new OptimizedStaticResourceRecord(),
                BamlRecordType.PIMapping => new PIMappingRecord(),
                BamlRecordType.PresentationOptionsAttribute => new PresentationOptionsAttributeRecord(),
                BamlRecordType.Property => new PropertyRecord(),
                BamlRecordType.PropertyArrayEnd => new PropertyArrayEndRecord(),
                BamlRecordType.PropertyArrayStart => new PropertyArrayStartRecord(),
                BamlRecordType.PropertyComplexEnd => new PropertyComplexEndRecord(),
                BamlRecordType.PropertyComplexStart => new PropertyComplexStartRecord(),
                BamlRecordType.PropertyCustom => new PropertyCustomRecord(),
                BamlRecordType.PropertyDictionaryEnd => new PropertyDictionaryEndRecord(),
                BamlRecordType.PropertyDictionaryStart => new PropertyDictionaryStartRecord(),
                BamlRecordType.PropertyListEnd => new PropertyListEndRecord(),
                BamlRecordType.PropertyListStart => new PropertyListStartRecord(),
                BamlRecordType.PropertyStringReference => new PropertyStringReferenceRecord(),
                BamlRecordType.PropertyTypeReference => new PropertyTypeReferenceRecord(),
                BamlRecordType.PropertyWithConverter => new PropertyWithConverterRecord(),
                BamlRecordType.PropertyWithExtension => new PropertyWithExtensionRecord(),
                BamlRecordType.PropertyWithStaticResourceId => new PropertyWithStaticResourceIdRecord(),
                BamlRecordType.RoutedEvent => new RoutedEventRecord(),
                BamlRecordType.StaticResourceEnd => new StaticResourceEndRecord(),
                BamlRecordType.StaticResourceId => new StaticResourceIdRecord(),
                BamlRecordType.StaticResourceStart => new StaticResourceStartRecord(),
                BamlRecordType.StringInfo => new StringInfoRecord(),
                BamlRecordType.Text => new TextRecord(),
                BamlRecordType.TextWithConverter => new TextWithConverterRecord(),
                BamlRecordType.TextWithId => new TextWithIdRecord(),
                BamlRecordType.TypeInfo => new TypeInfoRecord(),
                BamlRecordType.TypeSerializerInfo => new TypeSerializerInfoRecord(),
                BamlRecordType.XmlnsProperty => new XmlnsPropertyRecord(),
                _ => throw new NotSupportedException("Unsupported record type: " + type)
            };
        }
    }
}