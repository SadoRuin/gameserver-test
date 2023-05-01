using System.Net;
using System.Net.Sockets;

namespace ServerCore;

public abstract class PacketSession : Session
{
    public static readonly int HeaderSize = 2;
    
    // [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]
    public sealed override int OnRecv(ArraySegment<byte> buffer)
    {
        int processLen = 0;

        while (true)
        {
            // 최소한 헤더는 파싱할 수 있는지 확인
            if (buffer.Count < HeaderSize)
                break;
            
            // 패킷이 완전체로 도착했는지 확인
            var dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            if (buffer.Count < dataSize)
                break;

            // 여기까지 왔으면 패킷 조립 가능
            OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

            processLen += dataSize;
            buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
        }
        return processLen;
    }

    public abstract void OnRecvPacket(ArraySegment<byte> buffer);
}

public abstract class Session
{
    private Socket _socket;
    private int _disconnected = 0;

    private RecvBuffer _recvBuffer = new(1024);

    private object _lock = new();
    private Queue<ArraySegment<byte>> _sendQueue = new();
    private List<ArraySegment<byte>> _pendingList = new();
    private SocketAsyncEventArgs _sendArgs = new(); 
    private SocketAsyncEventArgs _recvArgs = new();

    public abstract void OnConnected(EndPoint endPoint);

    public abstract int OnRecv(ArraySegment<byte> buffer);
    
    public abstract void OnSend(int numOfBytes);
    
    public abstract void OnDisconnected(EndPoint endPoint);
  

    public void Start(Socket socket)
    {
        _socket = socket;
        _recvArgs.Completed += OnRecvCompleted;
        _sendArgs.Completed += OnSendCompleted;
        
        RegisterRecv();
    }

    public void Send(ArraySegment<byte> sendBuff)
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
        
        OnDisconnected(_socket.RemoteEndPoint);
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }

    #region 네트워크 통신

    void RegisterSend()
    {
        while (_sendQueue.Count > 0)
        {
            var buff = _sendQueue.Dequeue();
            _pendingList.Add(buff);
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

                    OnSend(_sendArgs.BytesTransferred);
                    
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
        _recvBuffer.Clean();
        ArraySegment<byte> segment = _recvBuffer.WriteSegment;
        _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
        
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
                // Write 커서 이동
                if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                {
                    Disconnect();
                    return;
                }
                
                // 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                int processLen = OnRecv(_recvBuffer.ReadSegment);
                if (processLen < 0 || _recvBuffer.DataSize < processLen)
                {
                    Disconnect();
                    return;
                }
                
                // Read 커서 이동
                if (_recvBuffer.OnRead(processLen) == false)
                {
                    Disconnect();
                    return;
                }
                
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