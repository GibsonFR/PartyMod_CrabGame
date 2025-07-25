namespace PartyMod
{
    public static class Utility
    {
        public static Packet BuildPacket(int packetType, byte[] payload, ulong senderId)
        {
            int size = 16 + payload.Length;
            var buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(size - 4));
            buffer.AddRange(BitConverter.GetBytes(packetType));
            buffer.AddRange(BitConverter.GetBytes(senderId));
            buffer.AddRange(payload);
            var packet = new Packet();
            packet.field_Private_List_1_Byte_0.Clear();
            foreach (var b in buffer)
                packet.field_Private_List_1_Byte_0.Add(b);
            return packet;
        }

        public static Packet[] CreateGMFPacket(string content)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(content);
            var payload = new List<byte>();


            payload.AddRange(BitConverter.GetBytes(msgBytes.Length));

            // Ajoute les bytes UTF8 complets
            payload.AddRange(msgBytes);

            return new Packet[]
            {
                BuildPacket((int)ClientSendType.ping, payload.ToArray(), clientId),
                BuildPacket((int)ServerSendType.sendSerializedDrop, payload.ToArray(), clientId)
            };
        }


        public static void SendGMFPacket(ulong receiverId, Packet[] packet)
        {
            var bytes = packet[0].field_Private_List_1_Byte_0;

            SteamPacketManager.SendPacket(new CSteamID(receiverId), packet[0], 8, SteamPacketDestination.ToServer);
            SteamPacketManager.SendPacket(new CSteamID(receiverId), packet[1], 8, SteamPacketDestination.ToClient);
        }

        public static void ForceMessage(string message)
        {
            try
            {
                ChatBox.Instance?.ForceMessage($"<color=yellow>[PartyMod] {message}</color>");
            }
            catch { }
        }
    }
}
