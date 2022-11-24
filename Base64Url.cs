public static class Base64Url 
{
    public static string Encode(byte[] data) => Convert.ToBase64String(data).Replace("+", "-").Replace("/", "_").Replace("=", "");
    public static byte[] Decode(string enc) => 
        Convert.FromBase64String(
            enc.Replace("-", "+").Replace("_", "/").PadRight((int)(Math.Ceiling(enc.Length / 4.0) * 4), '=')
        );
}