using System.Net;
using System.Text;
using ServerCore;

namespace Server;

class GameSession : Session
{
    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnConnected: {endPoint}");
        
        byte[] sendBuff = "Welcome to MMORPG Server !"u8.ToArray();
        Send(sendBuff);
            
        Thread.Sleep(1000);
            
        Disconnect();
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnDisconnected: {endPoint}");
    }

    public override void OnRecv(ArraySegment<byte> buffer)
    {
        string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        Console.WriteLine($"[From Client] {recvData}");
    }

    public override void OnSend(int numOfBytes)
    {
        Console.WriteLine($"Transferred bytes: {numOfBytes}");
    }
}

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
        
        _listener.Init(endPoint, () => new GameSession());
        Console.WriteLine("Listening...");

        while (true)
        {
            ;
        }
    }
}