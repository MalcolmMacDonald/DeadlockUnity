using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using ValveFileImporter.ValveResourceFormat.KeyValues;
using KVValueType = ValveFileImporter.ValveResourceFormat.KeyValues.KVValueType;

namespace ValveFileImporter.ValveResourceFormat.Resource
{
    public partial class BinaryKV3
    {
        public const int MAGIC4 = 0x4B563304; // KV3\x04

        /// <inheritdoc />
        public void Serialize(Stream stream)
        {
            if (Data == null)
            {
                throw new InvalidOperationException("No data to serialize");
            }

            using var context = new SerializationContext();

            context.Bytes4Writer.Write(0xDEADBEEF); // string count, will be updated

            WriteObject(Data, context);

            context.Bytes4.Position = 0;
            context.Bytes4Writer.Write(context.Strings.Count);

            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

            writer.Write(MAGIC4);
            writer.Write(Format.Id.ToByteArray());
            writer.Write(0); // 0 = no compression
            writer.Write((ushort)0); // compressionDictionaryId
            writer.Write((ushort)0); // compressionFrameSize
            writer.Write((int)context.Bytes1.Length);
            writer.Write((int)context.Bytes4.Length / 4);
            writer.Write((int)context.Bytes8.Length / 8);

            var countTypesOffset = stream.Position;
            writer.Write(0); // countTypes, will be overwritten
            writer.Write((ushort)0); // countObjects
            writer.Write((ushort)0); // countArrays

            var sizeUncompressedTotalOffset = stream.Position;
            writer.Write(0xDEADBEEF); // uncompressed size, will be overwritten
            writer.Write(0xDEADBEEF); // compressed size, will be overwritten

            writer.Write(context.BinaryBlobLengths.Count);
            writer.Write((int)context.BinaryBlobs.Length);
            writer.Write((int)context.Bytes2.Length / 2);
            writer.Write(0); // sizeBlockCompressedSizesBytes

            var start = stream.Position;
            var countTypes = WriteData(writer, context);

            // If there are no binary blobs, write the trailer in the main buffer
            if (context.BinaryBlobLengths.Count == 0)
            {
                writer.Write(0xFFEEDD00);
            }

            var end = stream.Position;

            // Go back and write uncompressed size (only the main data, including trailer when no binary blobs)
            var dataSize = (uint)(end - start);
            stream.Position = countTypesOffset;
            writer.Write(countTypes);
            stream.Position = sizeUncompressedTotalOffset;
            writer.Write(dataSize);
            writer.Write(dataSize);
            stream.Position = end;

            if (context.BinaryBlobLengths.Count > 0)
            {
                context.BinaryBlobs.WriteTo(writer.BaseStream);

                // Finish with a trailer after binary blobs
                writer.Write(0xFFEEDD00);
            }
        }

        private void WriteObject(KVObject obj, SerializationContext context)
        {
            Debug.Assert(!obj.IsArray); // The reader does not support non-object roots properly.

            WriteType(context, KV3BinaryNodeType.OBJECT);
            context.Bytes4Writer.Write(obj.Properties.Count);

            foreach (var property in obj.Properties)
            {
                WriteProperty(property.Key, property.Value, context);
            }
        }

        private void WriteProperty(string name, KVValue value, SerializationContext context)
        {
            context.Bytes4Writer.Write(context.GetStringId(name));
            WriteValueRecursive(value, context);
        }

        private void WriteValueRecursive(KVValue value, SerializationContext context)
        {
            if (value.Type == KVValueType.Boolean)
            {
                if ((bool)value.Value)
                {
                    WriteType(context, KV3BinaryNodeType.BOOLEAN_TRUE, value.Flag);
                }
                else
                {
                    WriteType(context, KV3BinaryNodeType.BOOLEAN_FALSE, value.Flag);
                }

                return;
            }

            if (value.Type == KVValueType.Int64)
            {
                var writeValue = Convert.ToInt64(value.Value, CultureInfo.InvariantCulture);

                if (writeValue == 0)
                {
                    WriteType(context, KV3BinaryNodeType.INT64_ZERO, value.Flag);
                    return;
                }

                if (writeValue == 1)
                {
                    WriteType(context, KV3BinaryNodeType.INT64_ONE, value.Flag);
                    return;
                }

                WriteType(context, KV3BinaryNodeType.INT64, value.Flag);
                context.Bytes8Writer.Write(writeValue);
                return;
            }

            if (value.Type == KVValueType.FloatingPoint64)
            {
                var writeValue = Convert.ToDouble(value.Value, CultureInfo.InvariantCulture);

                if (writeValue == 0.0)
                {
                    WriteType(context, KV3BinaryNodeType.DOUBLE_ZERO, value.Flag);
                    return;
                }

                if (writeValue == 1.0)
                {
                    WriteType(context, KV3BinaryNodeType.DOUBLE_ONE, value.Flag);
                    return;
                }

                WriteType(context, KV3BinaryNodeType.DOUBLE, value.Flag);
                context.Bytes8Writer.Write(writeValue);
                return;
            }


            var nodeType = GetKV3BinaryNodeType(value);
            WriteType(context, nodeType, value.Flag);

            switch (value.Type)
            {
                case KVValueType.Null:
                    break;
                case KVValueType.Int16:
                    context.Bytes2Writer.Write(Convert.ToInt16(value.Value, CultureInfo.InvariantCulture));
                    break;
                case KVValueType.UInt16:
                    context.Bytes2Writer.Write(Convert.ToUInt16(value.Value, CultureInfo.InvariantCulture));
                    break;
                case KVValueType.Int32:
                    context.Bytes4Writer.Write(Convert.ToInt32(value.Value, CultureInfo.InvariantCulture));
                    break;
                case KVValueType.UInt32:
                    context.Bytes4Writer.Write(Convert.ToUInt32(value.Value, CultureInfo.InvariantCulture));
                    break;
                case KVValueType.UInt64:
                    context.Bytes8Writer.Write(Convert.ToUInt64(value.Value, CultureInfo.InvariantCulture));
                    break;
                case KVValueType.FloatingPoint:
                    context.Bytes4Writer.Write(Convert.ToSingle(value.Value, CultureInfo.InvariantCulture));
                    break;
                case KVValueType.String:
                    context.Bytes4Writer.Write(context.GetStringId((string)value.Value));
                    break;
                case KVValueType.BinaryBlob:
                    var bytes = (byte[])value.Value;
                    context.BinaryBlobLengths.Add(bytes.Length);
                    if (bytes.Length > 0)
                    {
                        context.BinaryBlobsWriter.Write(bytes);
                    }

                    break;
                case KVValueType.Collection:
                    context.Bytes4Writer.Write(((KVObject)value.Value).Properties.Count);

                    foreach (var property in ((KVObject)value.Value).Properties)
                    {
                        WriteProperty(property.Key, property.Value, context);
                    }

                    break;
                case KVValueType.Array:
                    context.Bytes4Writer.Write(((KVObject)value.Value).Properties.Count);

                    foreach (var property in ((KVObject)value.Value).Properties)
                    {
                        WriteValueRecursive(property.Value, context);
                    }

                    break;
                default:
                    throw new NotSupportedException($"Unsupported value type: {value.Type}");
            }
        }

        private static KV3BinaryNodeType GetKV3BinaryNodeType(KVValue value)
        {
            return value.Type switch
            {
                KVValueType.Null => KV3BinaryNodeType.NULL,
                //KVValueType.Boolean => KV3BinaryNodeType.BOOLEAN,
                KVValueType.Int16 => KV3BinaryNodeType.INT16,
                KVValueType.UInt16 => KV3BinaryNodeType.UINT16,
                KVValueType.Int32 => KV3BinaryNodeType.INT32,
                KVValueType.UInt32 => KV3BinaryNodeType.UINT32,
                //KVValueType.Int64 => KV3BinaryNodeType.INT64,
                KVValueType.UInt64 => KV3BinaryNodeType.UINT64,
                KVValueType.FloatingPoint => KV3BinaryNodeType.FLOAT,
                //KVValueType.FloatingPoint64 => KV3BinaryNodeType.DOUBLE,
                KVValueType.String => KV3BinaryNodeType.STRING,
                KVValueType.BinaryBlob => KV3BinaryNodeType.BINARY_BLOB,
                KVValueType.Array => KV3BinaryNodeType.ARRAY,
                KVValueType.Collection => KV3BinaryNodeType.OBJECT,
                _ => throw new NotSupportedException($"Unsupported value type: {value.Type}")
            };
        }

        private static void WriteType(SerializationContext context, KV3BinaryNodeType type, KVFlag flag = KVFlag.None)
        {
            if (flag != KVFlag.None)
            {
                context.TypesWriter.Write((byte)((byte)type | 0x80));
                context.TypesWriter.Write((byte)flag);
            }
            else
            {
                context.TypesWriter.Write((byte)type);
            }
        }

        private static int WriteData(BinaryWriter writer, SerializationContext context)
        {
            // We're aligning inside of the compressed data block (even though we don't compress)
            var offset = (int)context.Bytes1.Length;

            if (context.Bytes1.Length > 0)
            {
                context.Bytes1.WriteTo(writer.BaseStream);
            }

            if (context.Bytes2.Length > 0)
            {
                AlignWriter(ref offset, writer, 2);
                context.Bytes2.WriteTo(writer.BaseStream);
                offset += (int)context.Bytes2.Length;
            }

            if (context.Bytes4.Length > 0)
            {
                AlignWriter(ref offset, writer, 4);
                context.Bytes4.WriteTo(writer.BaseStream);
                offset += (int)context.Bytes4.Length;
            }

            if (context.Bytes8.Length > 0)
            {
                AlignWriter(ref offset, writer, 8);
                context.Bytes8.WriteTo(writer.BaseStream);
                offset += (int)context.Bytes8.Length;
            }
            else
            {
                // For versions before 5, align even when empty
                AlignWriter(ref offset, writer, 8);
            }

            var stringsStartOffset = offset;

            foreach (var str in context.Strings)
            {
                var strBytes = Encoding.UTF8.GetBytes(str);
                writer.Write(strBytes);
                writer.Write((byte)0);
                offset += strBytes.Length + 1;
            }

            context.Types.WriteTo(writer.BaseStream);
            offset += (int)context.Types.Length;

            var typesEndOffset = offset - stringsStartOffset;

            if (context.BinaryBlobLengths.Count > 0)
            {
                foreach (var length in context.BinaryBlobLengths)
                {
                    writer.Write(length);
                }

                writer.Write(0xFFEEDD00);
            }

            return typesEndOffset;
        }

        private static void AlignWriter(ref int offset, BinaryWriter writer, int alignment)
        {
            var originalOffset = offset;
            Align(ref offset, alignment);
            var padding = offset - originalOffset;

            for (var i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }
        }


        private class SerializationContext : IDisposable
        {
            public readonly List<int> BinaryBlobLengths = new();
            public readonly MemoryStream BinaryBlobs = new();
            public readonly BinaryWriter BinaryBlobsWriter;
            public readonly MemoryStream Bytes1 = new();

            public readonly BinaryWriter Bytes1Writer;
            public readonly MemoryStream Bytes2 = new();
            public readonly BinaryWriter Bytes2Writer;
            public readonly MemoryStream Bytes4 = new();
            public readonly BinaryWriter Bytes4Writer;
            public readonly MemoryStream Bytes8 = new();

            public readonly BinaryWriter Bytes8Writer;

            // TODO: Remove the extra list
            public readonly Dictionary<string, int> StringMap = new();
            public readonly List<string> Strings = new();
            public readonly MemoryStream Types = new();
            public readonly BinaryWriter TypesWriter;

            public SerializationContext()
            {
                Bytes1Writer = new BinaryWriter(Bytes1, Encoding.UTF8, true);
                Bytes2Writer = new BinaryWriter(Bytes2, Encoding.UTF8, true);
                Bytes4Writer = new BinaryWriter(Bytes4, Encoding.UTF8, true);
                Bytes8Writer = new BinaryWriter(Bytes8, Encoding.UTF8, true);
                TypesWriter = new BinaryWriter(Types, Encoding.UTF8, true);
                BinaryBlobsWriter = new BinaryWriter(BinaryBlobs, Encoding.UTF8, true);
            }

            public void Dispose()
            {
                Bytes1Writer?.Dispose();
                Bytes2Writer?.Dispose();
                Bytes4Writer?.Dispose();
                Bytes8Writer?.Dispose();
                TypesWriter?.Dispose();
                BinaryBlobsWriter?.Dispose();
                Bytes1?.Dispose();
                Bytes2?.Dispose();
                Bytes4?.Dispose();
                Bytes8?.Dispose();
                Types?.Dispose();
                BinaryBlobs?.Dispose();
            }

            public int GetStringId(string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return -1;
                }

                if (!StringMap.TryGetValue(str, out var id))
                {
                    id = Strings.Count;
                    Strings.Add(str);
                    StringMap[str] = id;
                }

                return id;
            }
        }
    }
}