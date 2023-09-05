namespace BoostBotV2.Api.Extensions
{
    public static class KeyGenerator
    {
        private static readonly Random Random = new();
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public static string GenerateKey(int length = 16)
        {
            return new string(Enumerable.Repeat(Characters, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}