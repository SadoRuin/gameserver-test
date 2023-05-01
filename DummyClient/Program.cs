using System.Net;
using ServerCore;

namespace DummyClient;

class Program
{
    static void Main(string[] args)
    {
        // DNS (Domain Name Server)
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress ipAddr = ipHost.AddressList[0];
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

        Connector connector = new();
        
        connector.Connect(endPoint, () => new ServerSession());

        while (true)
        {
            try
            {
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            
            Thread.Sleep(100);
        }
        
    }
}
