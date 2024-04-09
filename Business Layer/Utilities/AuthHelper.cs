using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public class AuthHelper
    {

        public static string GenerateSHA256(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            using (SHA256 hashEngine = SHA256.Create())
            {
                byte[] hashedBytes = hashEngine.ComputeHash(bytes, 0, bytes.Length);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte stringByte in hashedBytes)
                {
                    string hex = stringByte.ToString("x2");
                    stringBuilder.Append(hex);
                }
                return stringBuilder.ToString();
            }
        }

    }
}
