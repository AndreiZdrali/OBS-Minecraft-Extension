using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using Open.Nat;

namespace Server
{
    static class Utils
    {
        public static async Task OpenPort(Protocol protocol , int privatePort, int publicPort, string description)
        {
            NatDiscoverer discoverer = new NatDiscoverer();
            NatDevice device = await discoverer.DiscoverDeviceAsync();
            await device.CreatePortMapAsync(new Mapping(protocol, privatePort, publicPort, description));
        }

        public static string GetMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().ToLower();
            }
        }

        public static byte[] TrimBytes(byte[] input)
        {
            return input.TakeWhile((v, index) => input.Skip(index).Any(w => w != 0x00)).ToArray();
        }
    }
}
