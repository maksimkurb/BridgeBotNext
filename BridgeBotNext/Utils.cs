using System.Security.Cryptography;

internal static class Utils
{
    /// <summary>
    ///     Generates random token
    ///     See https://blog.bitscry.com/2018/04/13/cryptographically-secure-random-string/
    /// </summary>
    /// <param name="length">Token length</param>
    /// <param name="chars">Allowed chars</param>
    /// <returns></returns>
    public static string GenerateCryptoRandomString(int length,
        string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890")
    {
        using (var crypto = new RNGCryptoServiceProvider())
        {
            var data = new byte[length];

            // If chars.Length isn't a power of 2 then there is a bias if we simply use the modulus operator. The first characters of chars will be more probable than the last ones.
            // buffer used if we encounter an unusable random byte. We will regenerate it in this buffer
            byte[] buffer = null;

            // Maximum random number that can be used without introducing a bias
            var maxRandom = byte.MaxValue - (byte.MaxValue + 1) % chars.Length;

            crypto.GetBytes(data);

            var result = new char[length];

            for (var i = 0; i < length; i++)
            {
                var value = data[i];

                while (value > maxRandom)
                {
                    if (buffer == null) buffer = new byte[1];

                    crypto.GetBytes(buffer);
                    value = buffer[0];
                }

                result[i] = chars[value % chars.Length];
            }

            return new string(result);
        }
    }
}