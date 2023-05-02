using System.Net;
using System.Text;
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
    public string name;

    public PlayerInfoReq()
    {
        packetId = (ushort)PacketID.PlayerInfoReq;
    }
    
    public override ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
            
        ushort count = 0;
        bool success = true;

        Span<byte> s = new(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), packetId);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), playerId);
        count += sizeof(long);
        
        // string
        ushort nameLen = (ushort)Encoding.Unicode.GetBytes(name, 0, name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
        count += sizeof(ushort);
        count += nameLen;
        
        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;
        
        return SendBufferHelper.Close(count);
    }

    public override void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        count += sizeof(ushort);
        playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
        count += sizeof(long);
        
        // string
        ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
        count += sizeof(ushort);
        name = Encoding.Unicode.GetString(s.Slice(count, nameLen));
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
                    Console.WriteLine($"PlayerInfoReq: {p.playerId} {p.name}");
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
