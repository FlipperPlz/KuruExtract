using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using KuruExtract.RV.Config;
using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;

#pragma warning disable CS0618

public sealed class PBOFile : IDisposable {

    private readonly Dictionary<PBOEntry, long> _pboEntries = new();
    private readonly RVBinaryReader _pboReader;
    private readonly string _pboPath;

    public string PBOPrefix => PBOProperties!["prefix"];
    public List<PBOEntry> PBOEntries => _pboEntries.Keys.ToList();
    public Dictionary<string, string> PBOProperties { get; set; } = new();
    
    private bool _disposed = false;

    
    public PBOFile(string filename, Stream stream) 
    {
        _pboPath = filename;
        _pboReader = new RVBinaryReader(stream);
        
        _pboReader.ReadAsciiZ();
        if(_pboReader.ReadInt32() != BitConverter.ToInt32(Encoding.ASCII.GetBytes("sreV"))) 
            throw new Exception("Woah, version entry should be the first in all PBOs. Report this to developer");
        _pboReader.BaseStream.Position += 16;

        
        string propertyName;
        do 
        {
            propertyName = _pboReader.ReadAsciiZ();
            if (propertyName == "") break;

            var value = _pboReader.ReadAsciiZ();
            PBOProperties.Add(propertyName, value);

        }
        while (propertyName != "");
        
        do 
        {
            var entry = PBOEntry.GetEntryMeta(_pboReader);
            if(entry != PBOEntry.EmptyEntry) _pboEntries.Add(entry, 0);
        }
        while (_pboReader.PeekBytes(21).Sum(b =>  b) != 0);

        _pboReader.BaseStream.Position += 21;

        foreach (var pboEntry in PBOEntries) {
            _pboEntries[pboEntry] = _pboReader.BaseStream.Position;
            _pboReader.BaseStream.Position += pboEntry.PackedDataSize;
        }  

    }
    
    public void ExtractEntry(PBOEntry entry, string destination) 
    {
        var fileName = entry.EntryName.Replace("config.bin", "config.cpp");
        var path = Path.Combine(destination, fileName);
        var dir = Path.GetDirectoryName(path);
        if (dir != null)
            Directory.CreateDirectory(dir);

        using var targetFile = File.Create(path);
        using var source = GetEntryData(entry);

        using (var reader = new BinaryReader(source)) 
        {
            if (reader.BaseStream.Length >= 4 && reader.ReadByte() == '\0' && reader.ReadByte() == 'r' && reader.ReadByte() == 'a' &&
                reader.ReadByte() == 'P') 
            {
                reader.BaseStream.Position = 0;
                using var writer = new StreamWriter(targetFile);
                var param = new ParamFile(source);
                writer.Write(param.ToString());
                return;
            }
            reader.BaseStream.Position = 0;
            source.CopyTo(targetFile);
        }
    }
    
    public MemoryStream GetEntryData(PBOEntry entry) {
        var ogPos = _pboReader.Position;

        _pboReader.Position = _pboEntries[entry];

        var stream = new MemoryStream(entry.IsCompressed()
            ? RVLZSS.Decompress(_pboReader.ReadBytes(entry.PackedDataSize), entry.OriginalDataSize)
            : _pboReader.ReadBytes(entry.PackedDataSize));
        
        _pboReader.Position = ogPos;
        return stream;
    }

    public void Dispose() {
        if(_disposed) return;
        _pboReader.Dispose();
        _disposed = true;
    }
}

#pragma warning restore CS0618