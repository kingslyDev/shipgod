using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace ShipmentFinishGood.Utilities;

public static class CompressionHelper
{
    public static string ToCompressedBase64<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen:true))
        {
            gz.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public static T? FromCompressedBase64<T>(string b64)
    {
        try
        {
            var data = Convert.FromBase64String(b64);
            using var inMs = new MemoryStream(data);
            using var gz = new GZipStream(inMs, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            gz.CopyTo(outMs);
            var json = Encoding.UTF8.GetString(outMs.ToArray());
            return JsonSerializer.Deserialize<T>(json);
        }
        catch { return default; }
    }
}
