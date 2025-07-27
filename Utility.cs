namespace PartyMod
{
    public static class Utility
    {
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
