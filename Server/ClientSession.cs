using System.Net;
using ServerCore;

namespace Server;

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

class ClientSession : PacketSession
{
    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnConnected: {endPoint}");

        // Packet packet = new() { size = 100, packetId = 10 };
        //
        // ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        // byte[] buffer = BitConverter.GetBytes(packet.size);
        // byte[] buffer2 = BitConverter.GetBytes(packet.packetId);
        // Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
        // Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
        // ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);
        //
        // Send(sendBuff);
        Thread.Sleep(5000);
        
        Disconnect();
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        ushort count = 0;
        
        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        switch ((PacketID)id)
        {
            case PacketID.PlayerInfoReq:
                {
                    PlayerInfoReq p = new PlayerInfoReq();
                    p.Read(buffer);
                    Console.WriteLine($"PlayerInfoReq: {p.playerId}");
                }
                break;
        }
        
        Console.WriteLine($"RecvPacketId: {id}, Size {size}");
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnDisconnected: {endPoint}");
    }

    public override void OnSend(int numOfBytes)
    {
        Console.WriteLine($"Transferred bytes: {numOfBytes}");
    }
}
