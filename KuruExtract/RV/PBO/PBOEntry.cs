using System.IO.Compression;
using KuruExtract.RV.Config;
using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;

public enum PackingType 
{
    Uncompressed = 0x00000000,
    Compressed = 0x43707273
}

public sealed class PBOEntry 
{
    public static readonly PBOEntry EmptyEntry = new PBOEntry() 
    {
        EntryName = "",
        EntryMimeType = PackingType.Uncompressed,
        PackedDataSize = 0,
        OriginalDataSize = 0
    };

    private PBOEntry() 
    {
    }

    

    public PBOEntry(PackingType entryMimeType, string entryName, int storedDataSize, int originalDataSize) 
    {
        EntryMimeType = entryMimeType;
        EntryName = entryName;
        PackedDataSize = storedDataSize;
        OriginalDataSize = originalDataSize;
    }

    public PackingType EntryMimeType { get; private set; }
    public string EntryName { get; private set; }
    public int OriginalDataSize { get; private set; }
    public int PackedDataSize { get; private set; }

    public bool IsCompressed() => EntryMimeType == PackingType.Compressed;

    public static PBOEntry GetEntryMeta(RVBinaryReader reader) 
    {
        var entryName = reader.ReadAsciiZ();
        var mimeType = PackingType.Uncompressed;
        if (reader.ReadInt32() == (int)PackingType.Compressed) mimeType = PackingType.Compressed;
        var originalSize = reader.ReadInt32();
        reader.BaseStream.Position += 8;
        var dataLength = reader.ReadInt32();

        return new PBOEntry(mimeType, entryName, dataLength, originalSize);
    }

    
}