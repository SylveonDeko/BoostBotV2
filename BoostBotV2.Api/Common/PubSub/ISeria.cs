namespace BoostBotV2.Api.Common.PubSub;

public interface ISeria
{
    byte[] Serialize<T>(T data);
    T? Deserialize<T>(byte[] data);
}