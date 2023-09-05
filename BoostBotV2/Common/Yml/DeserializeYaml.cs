using YamlDotNet.Serialization;

namespace BoostBotV2.Common.Yml;

public class DeserializeYaml
{
    public static Credentials CredsDeserialize(bool api = false)
    {
        string path;
        path = Path.Combine(Directory.GetCurrentDirectory(), api ? "../BoostBotV2/creds.yml" : "creds.yml");
        var file = File.ReadAllText(path);
        var deserializer = new Deserializer();
        var creds = deserializer.Deserialize<Credentials>(file);
        return creds;
    }
}