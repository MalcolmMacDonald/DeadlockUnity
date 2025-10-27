using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using ValveFileImporter.ValveResourceFormat.KeyValues;
using ValveFileImporter.ValveResourceFormat.Utils;

namespace ValveFileImporter.ValveResourceFormat.Resource
{
    public partial class BinaryKV3
    {
        public KVObject Data;

        public BinaryKV3(string assetPath)
        {
            using var fs = new FileStream(assetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(fs, Encoding.ASCII, true);

            var magic = reader.ReadUInt32();

            var version = magic & 0xFF;
            magic &= 0xFFFFFF00;

            var context = new Context
            {
                Version = (int)version
            };

            var formatData = reader.ReadBytes(16);
            Format = KV3IDLookup.GetByValue(new Guid(formatData));

            var compressionMethod = reader.ReadUInt32();
            ushort compressionDictionaryId = 0;
            ushort compressionFrameSize = 0;
            var countBytes1 = 0;
            var countBytes4 = 0;
            var countBytes8 = 0;
            var countTypes = 0;
            var countObjects = 0;
            var countArrays = 0;
            var sizeUncompressedTotal = 0;
            var sizeCompressedTotal = 0;
            var countBlocks = 0;
            var sizeBinaryBlobsBytes = 0;

            compressionDictionaryId = reader.ReadUInt16();
            compressionFrameSize = reader.ReadUInt16();
            countBytes1 = reader.ReadInt32();
            countBytes4 = reader.ReadInt32();
            countBytes8 = reader.ReadInt32();
            countTypes = reader.ReadInt32();
            countObjects = reader.ReadUInt16();
            countArrays = reader.ReadUInt16();
            sizeUncompressedTotal = reader.ReadInt32();
            sizeCompressedTotal = reader.ReadInt32();
            countBlocks = reader.ReadInt32();
            sizeBinaryBlobsBytes = reader.ReadInt32();

            var countBytes2 = reader.ReadInt32();
            var sizeBlockCompressedSizesBytes = reader.ReadInt32();

            var sizeUncompressedBuffer1 = 0;
            var sizeCompressedBuffer1 = 0;
            var sizeUncompressedBuffer2 = 0;
            var sizeCompressedBuffer2 = 0;
            var countBytes1_buffer2 = 0;
            var countBytes2_buffer2 = 0;
            var countBytes4_buffer2 = 0;
            var countBytes8_buffer2 = 0;
            var countObjects_buffer2 = 0;
            var countArrays_buffer2 = 0;

            sizeUncompressedBuffer1 = reader.ReadInt32();
            sizeCompressedBuffer1 = reader.ReadInt32();
            sizeUncompressedBuffer2 = reader.ReadInt32();
            sizeCompressedBuffer2 = reader.ReadInt32();
            countBytes1_buffer2 = reader.ReadInt32();
            countBytes2_buffer2 = reader.ReadInt32();
            countBytes4_buffer2 = reader.ReadInt32();
            countBytes8_buffer2 = reader.ReadInt32();
            var unk13 = reader.ReadInt32();
            countObjects_buffer2 = reader.ReadInt32();
            countArrays_buffer2 = reader.ReadInt32();
            var unk16 = reader.ReadInt32();

            Debug.Assert(sizeUncompressedTotal == sizeUncompressedBuffer1 + sizeUncompressedBuffer2);
            var buffer1Raw = ArrayPool<byte>.Shared.Rent(sizeUncompressedBuffer1);
            byte[] buffer2Raw = null;
            byte[] binaryBlobsRaw = null;
            //ZstdSharp.Decompressor zstdDecompressor = null;
            ArraySegment<byte> bufferWithBinaryBlobSizes = null;

            Debug.Assert(sizeCompressedBuffer1 == 0);
            {
                var buffer1Span = new ArraySegment<byte>(buffer1Raw, 0, sizeUncompressedBuffer1);

                reader.Read(buffer1Span);
                var buffer1 = new Buffers();

                var offset = 0;

                if (countBytes1 > 0)
                {
                    var end = offset + countBytes1;
                    buffer1.Bytes1 = buffer1Span[offset..end];
                    offset = end;
                }

                if (countBytes2 > 0)
                {
                    Align(ref offset, 2);

                    var end = offset + countBytes2 * 2;
                    buffer1.Bytes2 = buffer1Span[offset..end];
                    offset = end;
                }

                if (countBytes4 > 0)
                {
                    Align(ref offset, 4);

                    var end = offset + countBytes4 * 4;
                    buffer1.Bytes4 = buffer1Span[offset..end];
                    offset = end;
                }

                if (countBytes8 > 0)
                {
                    Align(ref offset, 8);

                    var end = offset + countBytes8 * 8;
                    buffer1.Bytes8 = buffer1Span[offset..end];
                    offset = end;
                }
                else if (version < 5)
                {
                    // For some reason V5 does not align this when empty, but earlier versions did
                    Align(ref offset, 8);
                }

                Debug.Assert(countBytes4 > 0); // should be guaranteed to be at least 1 for the strings count


                var countStrings = MemoryMarshal.Read<int>(buffer1.Bytes4);
                buffer1.Bytes4 = buffer1.Bytes4[sizeof(int)..];
                context.Strings = new string[countStrings];

                context.AuxiliaryBuffer = buffer1;

                var readStringBytes = 0;

                for (var i = 0; i < countStrings; i++)
                {
                    context.Strings[i] = ReadNullTermUtf8String(ref buffer1.Bytes1, ref readStringBytes);
                }

                Debug.Assert(buffer1Span.Count == offset);
            }

            buffer2Raw = ArrayPool<byte>.Shared.Rent(sizeUncompressedBuffer2);
            {
                var buffer2Span = new ArraySegment<byte>(buffer2Raw, 0, sizeUncompressedBuffer2);

                if (compressionMethod == 0) // uncompressed
                {
                    Debug.Assert(sizeCompressedBuffer2 == 0);

                    reader.Read(buffer2Span);
                }

                var buffer2 = new Buffers();
                context.Buffer = buffer2;

                var end = countObjects_buffer2 * sizeof(int);
                var offset = end;

                context.ObjectLengths = buffer2Span[..end];

                if (countBytes1_buffer2 > 0)
                {
                    end = offset + countBytes1_buffer2;
                    buffer2.Bytes1 = buffer2Span[offset..end];
                    offset = end;
                }

                if (countBytes2_buffer2 > 0)
                {
                    Align(ref offset, 2);

                    end = offset + countBytes2_buffer2 * 2;
                    buffer2.Bytes2 = buffer2Span[offset..end];
                    offset = end;
                }

                if (countBytes4_buffer2 > 0)
                {
                    Align(ref offset, 4);

                    end = offset + countBytes4_buffer2 * 4;
                    buffer2.Bytes4 = buffer2Span[offset..end];
                    offset = end;
                }

                if (countBytes8_buffer2 > 0)
                {
                    Align(ref offset, 8);

                    end = offset + countBytes8_buffer2 * 8;
                    buffer2.Bytes8 = buffer2Span[offset..end];
                    offset = end;
                }

                // Types in v5
                context.Types = buffer2Span[offset..(offset + countTypes)];
                offset += countTypes;

                if (countBlocks == 0)
                {
                    var trailer = MemoryMarshal.Read<uint>(buffer2Span[offset..]);
                    offset += 4;
                    UnexpectedMagicException.Assert(trailer == 0xFFEEDD00, trailer);
                }
                else
                {
                    bufferWithBinaryBlobSizes = buffer2Span[offset..];
                }
            }

            if (countBlocks > 0)
            {
                Debug.Assert(version >= 2);
                Debug.Assert(bufferWithBinaryBlobSizes != null);

                {
                    var end = countBlocks * sizeof(int);
                    context.BinaryBlobLengths = bufferWithBinaryBlobSizes[..end];
                    bufferWithBinaryBlobSizes = bufferWithBinaryBlobSizes[end..];

                    var trailer = MemoryMarshal.Read<uint>(bufferWithBinaryBlobSizes);
                    bufferWithBinaryBlobSizes = bufferWithBinaryBlobSizes[sizeof(int)..];
                    UnexpectedMagicException.Assert(trailer == 0xFFEEDD00, trailer);
                }

                if (compressionMethod == 0) // Uncompressed
                {
                    binaryBlobsRaw = ArrayPool<byte>.Shared.Rent(sizeBinaryBlobsBytes);
                    context.BinaryBlobs = new ArraySegment<byte>(binaryBlobsRaw, 0, sizeBinaryBlobsBytes);
                    reader.Read(context.BinaryBlobs);
                }
                else
                {
                    throw new UnexpectedMagicException("Unsupported compression method in block decoder", compressionMethod, nameof(compressionMethod));
                }

                {
                    var trailer = reader.ReadUInt32();
                    UnexpectedMagicException.Assert(trailer == 0xFFEEDD00, trailer);
                }
            }

            /*if (KVBlockType != BlockType.Undefined)
            {
                Debug.Assert(reader.BaseStream.Position == Offset + Size);
            }*/
            Data = ParseBinaryKV3(context, null, true);

            Debug.Assert(context.Types.Count == 0);
            Debug.Assert(context.ObjectLengths.Count == 0);
            Debug.Assert(context.BinaryBlobs.Count == 0);
            Debug.Assert(context.BinaryBlobLengths.Count == 0);
            Debug.Assert(context.Buffer.Bytes1.Count == 0);
            Debug.Assert(context.Buffer.Bytes2.Count == 0);
            Debug.Assert(context.Buffer.Bytes4.Count == 0);
            Debug.Assert(context.Buffer.Bytes8.Count == 0);

            if (version >= 5)
            {
                Debug.Assert(context.AuxiliaryBuffer.Bytes1.Count == 0);
                Debug.Assert(context.AuxiliaryBuffer.Bytes2.Count == 0);
                Debug.Assert(context.AuxiliaryBuffer.Bytes4.Count == 0);
                Debug.Assert(context.AuxiliaryBuffer.Bytes8.Count == 0);
            }

            ArrayPool<byte>.Shared.Return(buffer1Raw);

            if (buffer2Raw != null)
            {
                ArrayPool<byte>.Shared.Return(buffer2Raw);
            }

            if (binaryBlobsRaw != null)
            {
                ArrayPool<byte>.Shared.Return(binaryBlobsRaw);
            }
        }

        public KV3ID Format { get; }

        private static KVValueType ConvertBinaryOnlyKVType(KV3BinaryNodeType type)
        {
            // TODO: Why we are upcasting (u)int32 to 64
#pragma warning disable IDE0066 // Convert switch statement to expression
            switch (type)
            {
                case KV3BinaryNodeType.BOOLEAN:
                case KV3BinaryNodeType.BOOLEAN_TRUE:
                case KV3BinaryNodeType.BOOLEAN_FALSE:
                    return KVValueType.Boolean;
                case KV3BinaryNodeType.INT16:
                    return KVValueType.Int16;
                case KV3BinaryNodeType.UINT16:
                    return KVValueType.UInt16;
                case KV3BinaryNodeType.INT64:
                case KV3BinaryNodeType.INT32:
                case KV3BinaryNodeType.INT64_ZERO:
                case KV3BinaryNodeType.INT64_ONE:
                case KV3BinaryNodeType.INT32_AS_BYTE:
                    return KVValueType.Int64;
                case KV3BinaryNodeType.UINT64:
                case KV3BinaryNodeType.UINT32:
                    return KVValueType.UInt64;
                case KV3BinaryNodeType.FLOAT:
                    return KVValueType.FloatingPoint;
                case KV3BinaryNodeType.DOUBLE:
                case KV3BinaryNodeType.DOUBLE_ZERO:
                case KV3BinaryNodeType.DOUBLE_ONE:
                    return KVValueType.FloatingPoint64;
                case KV3BinaryNodeType.ARRAY:
                case KV3BinaryNodeType.ARRAY_TYPED:
                case KV3BinaryNodeType.ARRAY_TYPE_BYTE_LENGTH:
                case KV3BinaryNodeType.ARRAY_TYPE_AUXILIARY_BUFFER:
                    return KVValueType.Array;
                case KV3BinaryNodeType.OBJECT:
                    return KVValueType.Collection;
                case KV3BinaryNodeType.STRING:
                    return KVValueType.String;
                case KV3BinaryNodeType.BINARY_BLOB:
                    return KVValueType.BinaryBlob;
                case KV3BinaryNodeType.NULL:
                    return KVValueType.Null;
                default:
                    throw new NotImplementedException($"Unknown type {type}");
            }
#pragma warning restore IDE0066 // Convert switch statement to expression
        }

        private static KVValue MakeValue(KV3BinaryNodeType type, object data, KVFlag flag = KVFlag.None)
        {
            var realType = ConvertBinaryOnlyKVType(type);
            return new KVValue(realType, flag, data);
        }

        private static KVObject ReadBinaryValue(Context context, string name, KV3BinaryNodeType datatype, KVFlag flagInfo, KVObject parent)
        {
            // We don't support non-object roots properly, so this is a hack to handle "null" kv3
            if (datatype != KV3BinaryNodeType.OBJECT && parent == null)
            {
                name ??= "root";
                parent ??= new KVObject(name);
            }

            var buffer = context.Buffer;

            switch (datatype)
            {
                // Hardcoded values
                case KV3BinaryNodeType.NULL:
                    parent.AddProperty(name, MakeValue(datatype, null, flagInfo));
                    break;
                case KV3BinaryNodeType.BOOLEAN_TRUE:
                    parent.AddProperty(name, MakeValue(datatype, true, flagInfo));
                    break;
                case KV3BinaryNodeType.BOOLEAN_FALSE:
                    parent.AddProperty(name, MakeValue(datatype, false, flagInfo));
                    break;
                case KV3BinaryNodeType.INT64_ZERO:
                    parent.AddProperty(name, MakeValue(datatype, 0L, flagInfo));
                    break;
                case KV3BinaryNodeType.INT64_ONE:
                    parent.AddProperty(name, MakeValue(datatype, 1L, flagInfo));
                    break;
                case KV3BinaryNodeType.DOUBLE_ZERO:
                    parent.AddProperty(name, MakeValue(datatype, 0.0D, flagInfo));
                    break;
                case KV3BinaryNodeType.DOUBLE_ONE:
                    parent.AddProperty(name, MakeValue(datatype, 1.0D, flagInfo));
                    break;

                // 1 byte values
                case KV3BinaryNodeType.BOOLEAN:
                {
                    var value = buffer.Bytes1[0] == 1;
                    buffer.Bytes1 = buffer.Bytes1[1..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;
                // TODO: 22 might be INT32_AS_BYTE, and 23 is UINT32_AS_BYTE
                case KV3BinaryNodeType.INT32_AS_BYTE:
                {
                    Debug.Assert(context.Version >= 4);

                    var value = (int)buffer.Bytes1[0];
                    buffer.Bytes1 = buffer.Bytes1[1..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;

                // 2 byte values
                case KV3BinaryNodeType.INT16:
                {
                    Debug.Assert(context.Version >= 4);

                    var value = MemoryMarshal.Read<short>(buffer.Bytes2);
                    buffer.Bytes2 = buffer.Bytes2[sizeof(short)..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.UINT16:
                {
                    Debug.Assert(context.Version >= 4);

                    var value = MemoryMarshal.Read<ushort>(buffer.Bytes2);
                    buffer.Bytes2 = buffer.Bytes2[sizeof(ushort)..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;

                // 4 byte values
                case KV3BinaryNodeType.INT32:
                {
                    var value = MemoryMarshal.Read<int>(buffer.Bytes4);
                    buffer.Bytes4 = buffer.Bytes4[sizeof(int)..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.UINT32:
                {
                    var value = MemoryMarshal.Read<uint>(buffer.Bytes4);
                    buffer.Bytes4 = buffer.Bytes4[sizeof(uint)..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.FLOAT:
                {
                    Debug.Assert(context.Version >= 4);

                    var value = MemoryMarshal.Read<float>(buffer.Bytes4);
                    buffer.Bytes4 = buffer.Bytes4[sizeof(float)..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;

                // 8 byte values
                case KV3BinaryNodeType.INT64:
                {
                    var value = MemoryMarshal.Read<long>(buffer.Bytes8);
                    buffer.Bytes8 = buffer.Bytes8[sizeof(long)..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.UINT64:
                {
                    var value = MemoryMarshal.Read<ulong>(buffer.Bytes8);
                    buffer.Bytes8 = buffer.Bytes8[sizeof(ulong)..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.DOUBLE:
                {
                    var value = MemoryMarshal.Read<double>(buffer.Bytes8);
                    buffer.Bytes8 = buffer.Bytes8[sizeof(double)..];

                    parent.AddProperty(name, MakeValue(datatype, value, flagInfo));
                }
                    break;

                // Custom types
                case KV3BinaryNodeType.STRING:
                {
                    var id = MemoryMarshal.Read<int>(buffer.Bytes4);
                    buffer.Bytes4 = buffer.Bytes4[sizeof(int)..];

                    parent.AddProperty(name, MakeValue(datatype, id == -1 ? string.Empty : context.Strings[id], flagInfo));
                }
                    break;
                case KV3BinaryNodeType.BINARY_BLOB when context.Version < 2:
                {
                    var blockLength = MemoryMarshal.Read<int>(buffer.Bytes4);
                    buffer.Bytes4 = buffer.Bytes4[sizeof(int)..];
                    byte[] output;

                    if (blockLength > 0)
                    {
                        //MALCOLM: replacing because this isnt available in our version of C#
                        //output = [.. buffer.Bytes1[..blockLength]]; // explicit copy
                        output = buffer.Bytes1[..blockLength].ToArray();
                        buffer.Bytes1 = buffer.Bytes1[blockLength..];
                    }
                    else
                    {
                        //MALCOLM: replacing because this isnt available in our version of C#
                        //output = [];
                        output = Array.Empty<byte>();
                    }

                    parent.AddProperty(name, MakeValue(datatype, output, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.BINARY_BLOB:
                {
                    var blockLength = MemoryMarshal.Read<int>(context.BinaryBlobLengths);
                    context.BinaryBlobLengths = context.BinaryBlobLengths[sizeof(int)..];
                    byte[] output;

                    if (blockLength > 0)
                    {
                        //MALCOLM: replacing because this isnt available in our version of C#
                        //output = [.. context.BinaryBlobs[..blockLength]]; // explicit copy
                        output = context.BinaryBlobs[..blockLength].ToArray();
                        context.BinaryBlobs = context.BinaryBlobs[blockLength..];
                    }
                    else
                    {
                        //MALCOLM: replacing because this isnt available in our version of C#
                        //output = [];
                        output = Array.Empty<byte>();
                    }

                    parent.AddProperty(name, MakeValue(datatype, output, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.ARRAY:
                {
                    var arrayLength = MemoryMarshal.Read<int>(buffer.Bytes4);
                    buffer.Bytes4 = buffer.Bytes4[sizeof(int)..];

                    var array = new KVObject(name, true, arrayLength);

                    for (var i = 0; i < arrayLength; i++)
                    {
                        ParseBinaryKV3(context, array, true);
                    }

                    parent.AddProperty(name, MakeValue(datatype, array, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.ARRAY_TYPED:
                case KV3BinaryNodeType.ARRAY_TYPE_BYTE_LENGTH:
                {
                    int arrayLength;

                    if (datatype == KV3BinaryNodeType.ARRAY_TYPE_BYTE_LENGTH)
                    {
                        arrayLength = buffer.Bytes1[0];
                        buffer.Bytes1 = buffer.Bytes1[1..];
                    }
                    else
                    {
                        arrayLength = MemoryMarshal.Read<int>(buffer.Bytes4);
                        buffer.Bytes4 = buffer.Bytes4[sizeof(int)..];
                    }

                    var (subType, subFlagInfo) = ReadType(context);
                    var typedArray = new KVObject(name, true, arrayLength);

                    for (var i = 0; i < arrayLength; i++)
                    {
                        ReadBinaryValue(context, name, subType, subFlagInfo, typedArray);
                    }

                    parent.AddProperty(name, MakeValue(datatype, typedArray, flagInfo));
                }
                    break;
                case KV3BinaryNodeType.ARRAY_TYPE_AUXILIARY_BUFFER:
                {
                    Debug.Assert(context.Version >= 5);

                    var arrayLength = buffer.Bytes1[0];
                    buffer.Bytes1 = buffer.Bytes1[1..];

                    var (subType, subFlagInfo) = ReadType(context);
                    var typedArray = new KVObject(name, true, arrayLength);

                    // Swap the buffers and simply call read again instead of reimplementing the switch here
                    (context.AuxiliaryBuffer, context.Buffer) = (context.Buffer, context.AuxiliaryBuffer);

                    for (var i = 0; i < arrayLength; i++)
                    {
                        ReadBinaryValue(context, name, subType, subFlagInfo, typedArray);
                    }

                    (context.AuxiliaryBuffer, context.Buffer) = (context.Buffer, context.AuxiliaryBuffer);

                    parent.AddProperty(name, MakeValue(datatype, typedArray, flagInfo));
                }
                    break;

                case KV3BinaryNodeType.OBJECT:
                {
                    int objectLength;

                    if (context.Version >= 5)
                    {
                        objectLength = MemoryMarshal.Read<int>(context.ObjectLengths);
                        context.ObjectLengths = context.ObjectLengths[sizeof(int)..];
                    }
                    else
                    {
                        objectLength = MemoryMarshal.Read<int>(buffer.Bytes4);
                        buffer.Bytes4 = buffer.Bytes4[sizeof(int)..];
                    }

                    var newObject = new KVObject(name, false, objectLength);

                    for (var i = 0; i < objectLength; i++)
                    {
                        ParseBinaryKV3(context, newObject);
                    }

                    if (parent == null)
                    {
                        parent = newObject;
                    }
                    else
                    {
                        parent.AddProperty(name, MakeValue(datatype, newObject, flagInfo));
                    }
                }
                    break;
                default:
                    throw new UnexpectedMagicException($"Unknown KVType for field '{name}'", (int)datatype, nameof(datatype));
            }

            return parent;
        }

        private static KVObject ParseBinaryKV3(Context context, KVObject parent, bool inArray = false)
        {
            string name = null;
            if (!inArray)
            {
                var stringID = MemoryMarshal.Read<int>(context.Buffer.Bytes4);
                context.Buffer.Bytes4 = context.Buffer.Bytes4[sizeof(int)..];

                name = stringID == -1 ? string.Empty : context.Strings[stringID];
            }

            var (datatype, flagInfo) = ReadType(context);

            return ReadBinaryValue(context, name, datatype, flagInfo, parent);
        }


        private static (KV3BinaryNodeType Type, KVFlag Flag) ReadType(Context context)
        {
            var databyte = context.Types[0];
            context.Types = context.Types[1..];
            var flagInfo = KVFlag.None;

            if (context.Version >= 3)
            {
                if ((databyte & 0x80) > 0)
                {
                    databyte &= 0x3F; // Remove the flag bit

                    flagInfo = (KVFlag)context.Types[0];
                    context.Types = context.Types[1..];

                    if (flagInfo > KVFlag.MaxPersistedFlag)
                    {
                        throw new UnexpectedMagicException("Unexpected kv3 flag", (int)flagInfo, nameof(flagInfo));
                    }
                }
            }
            else if ((databyte & 0x80) > 0) // TODO: Valve's new code also checks for 0x40 even for old kv3 version
            {
                databyte &= 0x7F; // Remove the flag bit

                flagInfo = (KVFlag)context.Types[0];
                context.Types = context.Types[1..];

                if (((int)flagInfo & 4) > 0) // Multiline string
                {
                    Debug.Assert(databyte == (int)KV3BinaryNodeType.STRING);
                    flagInfo ^= (KVFlag)4;
                }

                // Strictly speaking there could be more than one flag set, but in practice it was seemingly never.
                // Valve's new code just sets whichever flag is highest, new kv3 version does not support multiple flags at once.
                flagInfo = (int)flagInfo switch
                {
                    0 => KVFlag.None,
                    1 => KVFlag.Resource,
                    2 => KVFlag.ResourceName,
                    8 => KVFlag.Panorama,
                    16 => KVFlag.SoundEvent,
                    32 => KVFlag.SubClass,
                    _ => throw new UnexpectedMagicException("Unexpected kv3 flag", (int)flagInfo, nameof(flagInfo))
                };
            }

            return ((KV3BinaryNodeType)databyte, flagInfo);
        }


        private static string ReadNullTermUtf8String(ref ArraySegment<byte> buffer, ref int offset)
        {
            var nullByte = buffer.AsSpan().IndexOf((byte)0);
            var str = buffer[..nullByte];
            buffer = buffer[(nullByte + 1)..];

            offset += nullByte + 1;

            return Encoding.UTF8.GetString(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Align(ref int offset, int alignment)
        {
            alignment -= 1;
            offset += alignment;
            offset &= ~alignment;
        }

        private class Context
        {
            public Buffers AuxiliaryBuffer;
            public ArraySegment<byte> BinaryBlobLengths;
            public ArraySegment<byte> BinaryBlobs;
            public Buffers Buffer;
            public ArraySegment<byte> ObjectLengths;
            public string[] Strings;
            public ArraySegment<byte> Types;
            public int Version;
        }

        private class Buffers
        {
            public ArraySegment<byte> Bytes1;
            public ArraySegment<byte> Bytes2;
            public ArraySegment<byte> Bytes4;
            public ArraySegment<byte> Bytes8;
        }
    }
}