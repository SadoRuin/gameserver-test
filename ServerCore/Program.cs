using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore;

class Program
{
    private static Listener _listener = new();

    static void OnAcceptHandler(Socket clientSocket)
    {
        try
        {
            Session session = new();
            session.Start(clientSocket);

            byte[] sendBuff = "Welcome to MMORPG Server !"u8.ToArray();
            session.Send(sendBuff);
            
            Thread.Sleep(1000);
            
            session.Disconnect();
            session.Disconnect();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    
    static void Main(string[] args)
    {
        // DNS (Domain Name Server)
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress ipAddr = ipHost.AddressList[0];
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);
        
        _listener.Init(endPoint, OnAcceptHandler);
        Console.WriteLine("Listening...");

        while (true)
        {
            ;
        }
    }
}
