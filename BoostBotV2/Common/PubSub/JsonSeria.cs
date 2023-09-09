using System.Text.Json;
using Mewdeko.Common.PubSub;

namespace BoostBotV2.Common.PubSub;

public class JsonSeria : ISeria
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
    };

    public byte[] Serialize<T>(T data)
        => JsonSerializer.SerializeToUtf8Bytes(data, _serializerOptions);

    public T? Deserialize<T>(byte[]? data)
    {
        return data is null ? default : JsonSerializer.Deserialize<T>(data, _serializerOptions);
    }
}