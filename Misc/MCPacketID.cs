namespace MoreCommands.Misc;

public static class MCPacketID
{
    public enum C2S : byte
    {
        SuggestionsRequest = 0
    }
    
    public enum S2C : byte
    {
        OperatorPacket = 0,
        SuggestionsSend = 1
    }
}