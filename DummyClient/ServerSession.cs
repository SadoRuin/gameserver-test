using System.Net;
using System.Text;
using ServerCore;

namespace DummyClient;
public abstract class Packet
{
    public ushort size;
    public ushort packetId;

    public abstract ArraySegment<byte> Write();
    public abstract void Read(ArraySegment<byte> s);
}

class PlayerInfoReq : Packet
{
    public long playerId;

    public PlayerInfoReq()
    {
        packetId = (ushort)PacketID.PlayerInfoReq;
    }
    
    public override ArraySegment<byte> Write()
    {
        ArraySegment<byte> s = SendBufferHelper.Open(4096);
            
        ushort count = 0;
        bool success = true;
            
        count += 2;
        success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), packetId);
        count += 2;
        success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), playerId);
        count += 8;
        success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), count);

        if (success == false)
            return null;
        
        return SendBufferHelper.Close(12);
    }

    public override void Read(ArraySegment<byte> s)
    {
        ushort count = 0;
        
        // ushort size = BitConverter.ToUInt16(s.Array, s.Offset);
        count += 2;
        // ushort id = BitConverter.ToUInt16(s.Array, s.Offset + count);
        count += 2;
        playerId = BitConverter.ToInt64(s.Array, s.Offset + count);
        count += 8;
    }
}

public enum PacketID
{
    PlayerInfoReq = 1,
    PlayerInfoOk = 2,
}

class ServerSession : Session
{
    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnConnected: {endPoint}");
        
        PlayerInfoReq packet = new() { playerId = 1001};
        
        // 보낸다
        // for (int i = 0; i < 5; i++)
        {
            ArraySegment<byte> s = packet.Write();

            if(s != null)
                Send(s);
        }
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnDisconnected: {endPoint}");
    }

    public override int OnRecv(ArraySegment<byte> buffer)
    {
        string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        Console.WriteLine($"[From Server] {recvData}");
        return buffer.Count;
    }

    public override void OnSend(int numOfBytes)
    {
        Console.WriteLine($"Transferred bytes: {numOfBytes}");
    }
}