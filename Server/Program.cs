using System.Net;
using System.Text;
using ServerCore;

namespace Server;

class Program
{
    private static Listener _listener = new();

    
    static void Main(string[] args)
    {
        // DNS (Domain Name Server)
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress ipAddr = ipHost.AddressList[0];
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);
        
        _listener.Init(endPoint, () => new ClientSession());
        Console.WriteLine("Listening...");

        while (true)
        {
            ;
        }
    }
}