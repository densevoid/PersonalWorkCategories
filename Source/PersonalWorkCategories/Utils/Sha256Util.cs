using System.Security.Cryptography;
using System.Text;

namespace HandyUI_PersonalWorkCategories.Utils;

internal class Sha256Util
{
    internal static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256   
        using var sha256Hash = SHA256.Create();
        // ComputeHash - returns byte array  
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

        // Convert byte array to a string   
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }
}