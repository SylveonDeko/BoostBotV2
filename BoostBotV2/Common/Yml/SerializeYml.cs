using YamlDotNet.Serialization;

namespace BoostBotV2.Common.Yml;

public class SerializeYml
{
    public static void Serialize(Credentials creds, string? setPath = null)
    {
        var serializer = new SerializerBuilder()
            .Build();
        var stringResult = serializer.Serialize(creds);
        var path = setPath ?? Path.Combine(Directory.GetCurrentDirectory(), "creds.yml");
        File.WriteAllText(Path.Combine(path), stringResult);
    }
}