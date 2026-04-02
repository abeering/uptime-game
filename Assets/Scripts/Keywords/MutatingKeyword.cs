using UnityEngine;

public class MutatingKeyword : IPacketKeyword
{
    public string KeywordId => "mutating";
    public string DisplayName => "Mutating";
    public string Description => "Changes its visible packet ID every few ticks, making it harder to track reliably.";
    
    private readonly int ticksPerMutation;
    private int ticksRemaining;

    public MutatingKeyword(int ticksPerMutation = 3)
    {
        this.ticksPerMutation = Mathf.Max(1, ticksPerMutation);
        ticksRemaining = this.ticksPerMutation;
    }

    public void OnTick(PacketView packet, KeywordContext context)
    {
        ticksRemaining--;

        if (ticksRemaining > 0)
            return;

        packet.SetVisiblePacketId(GenerateVisiblePacketId());
        ticksRemaining = ticksPerMutation;
    }

    public void OnScanned(PacketView packet, KeywordContext context)
    {
    }

    // might be fun to play with a "Weird" visible packet id in some other case 
    private string GenerateVisiblePacketId()
    {
        char letter = (char)Random.Range('a', 'z' + 1);
        int number = Random.Range(0, 10);

        return $"{letter}{number}";
    }
}