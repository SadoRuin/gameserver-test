using System.Net;
using System.Net.Sockets;

namespace ServerCore;

public class Listener
{
    private Socket _listenSocket;
    private Func<Session> _sessionFactory;

    public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
    {
        // 문지기
        _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _sessionFactory += sessionFactory;
        
        // 문지기 교육
        _listenSocket.Bind(endPoint);
            
        // 영업 시작
        // backlog : 최대 대기수
        _listenSocket.Listen(10);

        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
        args.Completed += OnAcceptCompleted;
        RegisterAccept(args);
    }

    void RegisterAccept(SocketAsyncEventArgs args)
    {
        args.AcceptSocket = null;
        
        bool pending = _listenSocket.AcceptAsync(args);
        if(pending == false)
            OnAcceptCompleted(null, args);
        
    }

    void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            Session session = _sessionFactory.Invoke();
            session.Start(args.AcceptSocket);
            session.OnConnected(args.AcceptSocket.RemoteEndPoint);
        }
        else
            Console.WriteLine(args.SocketError.ToString());
        
        RegisterAccept(args);
    }
}