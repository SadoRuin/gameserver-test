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
    public string name;

    public struct SkillInfo
    {
        public int id;
        public short level;
        public float duration;

        public bool Write()
        {
            return true;
        }
    }

    public List<SkillInfo> skills = new();

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
        
        // skill list
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)skills.Count);
        count += sizeof(ushort);
        foreach (SkillInfo skill in skills)
        {
            
        }
        
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

class ServerSession : Session
{
    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnConnected: {endPoint}");
        
        PlayerInfoReq packet = new() { playerId = 1001, name = "ABCD" };
        
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