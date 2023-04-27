using System.Net.Sockets;
using System.Text;

namespace ServerCore;

public class Session
{
    private Socket _socket;
    private int _disconnected = 0;

    private object _lock = new();
    private Queue<byte[]> _sendQueue = new();
    private List<ArraySegment<byte>> _pendingList = new();
    private SocketAsyncEventArgs _sendArgs = new(); 
    private SocketAsyncEventArgs _recvArgs = new();
    
    public void Start(Socket socket)
    {
        _socket = socket;
        _recvArgs.Completed += OnRecvCompleted;
        _recvArgs.SetBuffer(new byte[1024], 0, 1024);

        _sendArgs.Completed += OnSendCompleted;
        
        RegisterRecv();
    }

    public void Send(byte[] sendBuff)
    {
        lock (_lock)
        {
            _sendQueue.Enqueue(sendBuff);
            if(_pendingList.Count == 0)
                RegisterSend();
        }
        
    }

    public void Disconnect()
    {
        if (Interlocked.Exchange(ref _disconnected, 1) == 1)
            return;
        
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }

    #region 네트워크 통신

    void RegisterSend()
    {
        while (_sendQueue.Count > 0)
        {
            byte[] buff = _sendQueue.Dequeue();
            _pendingList.Add(new ArraySegment<byte>(buff , 0, buff.Length));
        }

        _sendArgs.BufferList = _pendingList;
        
        bool pending = _socket.SendAsync(_sendArgs);
        if(pending == false)
            OnSendCompleted(null, _sendArgs);
    }

    void OnSendCompleted(object sender, SocketAsyncEventArgs args)
    {
        lock (_lock)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    _sendArgs.BufferList = null;
                    _pendingList.Clear();

                    Console.WriteLine($"Transferred bytes: {_sendArgs.BytesTransferred}");
                    
                    if (_sendQueue.Count > 0)
                        RegisterSend();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnSendCompleted Failed {e}");
                }
            }
            else
            {
                Disconnect();
            }
        }
    }
    void RegisterRecv()
    {
        bool pending = _socket.ReceiveAsync(_recvArgs);
        if(pending == false)
            OnRecvCompleted(null, _recvArgs);
    }

    void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            try
            {
                string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                Console.WriteLine($"[From Client] {recvData}");
                RegisterRecv();
            }
            catch (Exception e)
            {
                Console.WriteLine($"OnRecvCompleted Failed {e}");
            }
        }
        else
        {
            Disconnect();
        }
    }
    #endregion
}