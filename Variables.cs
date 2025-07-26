namespace PartyMod
{
    public class Variables
    {
        // ulong
        public static ulong clientId;

        // Il2CppArrayBase<byte>
        public static UnhollowerBaseLib.Il2CppArrayBase<byte> lastByteReceived = null;

        // Dictionary
        public static Dictionary<ulong, float> connectedModUsers = [];

        //List
        public static List<ulong> detectedModUsers = [];
    }
}
