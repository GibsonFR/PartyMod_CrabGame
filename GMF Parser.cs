namespace PartyMod
{
    public static class GMFParser
    {
        public static bool TryParseCommand(ReadOnlySpan<char> text, out string type, out ReadOnlySpan<char> args)
        {
            type = null;
            args = ReadOnlySpan<char>.Empty;

            if (!text.StartsWith("[GMF] ", StringComparison.Ordinal)) return false;

            text = text.Slice(6);
            int spaceIndex = text.IndexOf(' ');

            if (spaceIndex == -1)
            {
                type = text.ToString();
                return true;
            }

            type = text.Slice(0, spaceIndex).ToString();
            args = text.Slice(spaceIndex + 1);
            return true;
        }
        public static string[] ExtractArgs(ReadOnlySpan<char> args)
        {
            if (args.IsEmpty) return Array.Empty<string>();

            return args.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

    }

}



