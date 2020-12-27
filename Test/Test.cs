using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Open.Nat;

namespace Test
{
    //FUNCTIONEAZA XDDDDDDDDDDD PERFECT
    class Test
    {
        static void Main(string[] args)
        {
            Console.WriteLine(GetLocalIPAddress());
            program().Wait();
        }

        public static async Task program()
        {
            NatDiscoverer discoverer = new NatDiscoverer();
            NatDevice device = await discoverer.DiscoverDeviceAsync();
            await device.DeletePortMapAsync(new Mapping(Protocol.Tcp, 12750, 12750, "Windows Defender"));
            Console.WriteLine(await device.GetExternalIPAsync());
            Console.WriteLine("DONE");
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
