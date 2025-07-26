namespace PartyMod
{
    public class GMFPatches
    {
        [HarmonyPatch(typeof(ServerHandle), nameof(ServerHandle.Ping))]
        [HarmonyPrefix]
        public static bool OnServerHandlePing(ulong __0, Packet __1)
        {    
            if (!PacketUtility.TryParsePacket(__1, out var mem, out var senderId)) return true;
            if (PacketUtility.IsDuplicatePacket(mem)) return false;
            
            string payload = PacketUtility.DecodeUTF8(mem);

            if (!GMFParser.TryParseCommand(payload.AsSpan(), out var type, out var argsSpan)) return true;
            string[] args = GMFParser.ExtractArgs(argsSpan);

            if (type == "chat") CustomChatboxUtility.HandleGMFChatPacket(args);
            else if (type == "party") HandleGMFPartyPacket(args, senderId);
            else if (type == "ping" && args.Length > 0 && ulong.TryParse(args[0], out var pingId)) ModUserSyncManager.HandlePingPacket(pingId);

            return false;
        }

        [HarmonyPatch(typeof(ClientHandle), nameof(ClientHandle.ReceiveSerializedDrop))]
        [HarmonyPrefix]
        public static bool OnClientHandleReceiveSerializedDrop(Packet __0)
        {
            if (!PacketUtility.TryParsePacket(__0, out var mem, out var senderId)) return true;
            if (PacketUtility.IsDuplicatePacket(mem)) return false;

            string payload = PacketUtility.DecodeUTF8(mem);

            if (!GMFParser.TryParseCommand(payload.AsSpan(), out var type, out var argsSpan)) return true;
            string[] args = GMFParser.ExtractArgs(argsSpan);

            if (type == "chat") CustomChatboxUtility.HandleGMFChatPacket(args);
            else if (type == "party") HandleGMFPartyPacket(args, senderId);
            else if (type == "ping" && args.Length > 0 && ulong.TryParse(args[0], out var pingId)) ModUserSyncManager.HandlePingPacket(pingId);
            

            return false;
        }
    }
}
