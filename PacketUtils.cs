using System.Buffers;

namespace PartyMod
{
    public static class PacketUtils
    {
        private const float DuplicatePacketTimeout = 0.1f;
        private static byte[] _lastPacketHash;
        private static float _lastPacketTime;

        private const int HeaderSize = 20;
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

        public static bool TryParsePacket(Packet packet, out ReadOnlyMemory<byte> payload, out ulong senderId)
        {
            payload = default;
            senderId = 0;

            try
            {
                var list = packet.field_Private_List_1_Byte_0;
                if (list.Count < HeaderSize) return false;

                var rentedBuffer = BufferPool.Rent(list.Count);
                for (int i = 0; i < list.Count; i++) rentedBuffer[i] = list[i];

                int payloadSize = BitConverter.ToInt32(rentedBuffer, 16);
                if (payloadSize <= 0 || payloadSize > list.Count - HeaderSize)
                {
                    BufferPool.Return(rentedBuffer);
                    return false;
                }

                senderId = BitConverter.ToUInt64(rentedBuffer, 8);

                var payloadData = new byte[payloadSize];
                Array.Copy(rentedBuffer, HeaderSize, payloadData, 0, payloadSize);
                payload = new ReadOnlyMemory<byte>(payloadData);

                BufferPool.Return(rentedBuffer);
                return true;
            }
            catch
            {
                return false;
            }
        }


        public static string DecodeUTF8(ReadOnlyMemory<byte> data) => Encoding.UTF8.GetString(data.Span);

        public static bool IsDuplicatePacket(ReadOnlyMemory<byte> current)
        {
            float now = Time.realtimeSinceStartup;

            var span = current.Span;
            bool isSamePacket = _lastPacketHash != null && _lastPacketHash.AsSpan().SequenceEqual(span);
            bool isWithinTimeout = (now - _lastPacketTime) < DuplicatePacketTimeout;

            if (isSamePacket && isWithinTimeout)
                return true;

            _lastPacketHash = span.ToArray();
            _lastPacketTime = now;
            return false;
        }
    }
}
