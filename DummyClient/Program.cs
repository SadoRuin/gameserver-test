using System.Net;
using System.Net.Sockets;
using System.Text;

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

        while (true)
        {
            // 휴대폰 설정
            Socket socket = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // 문지기한테 입장 문의
                socket.Connect(endPoint);
                Console.WriteLine($"Connected To {socket.RemoteEndPoint}");
                
                // 보낸다
                byte[] sendBuff = "Hello World!"u8.ToArray();
                int sendBytes = socket.Send(sendBuff);
                
                // 받는다
                byte[] recvBuff = new byte[1024];
                int recvBytes = socket.Receive(recvBuff);
                string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                Console.WriteLine($"[From Server] {recvData}");
                
                // 나간다
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            
            Thread.Sleep(100);
        }
        
    }
}
