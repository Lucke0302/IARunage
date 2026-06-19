using System.Runtime.CompilerServices;

namespace Runage.Models;

/// <summary>
/// Par de recompensas (Player, Mob) — substitui float[] no hot path.
/// Struct stack-only: zero alocação.
/// </summary>
public readonly struct Reward
{
    public readonly float Player;
    public readonly float Mob;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Reward(float player, float mob)
    {
        Player = player;
        Mob = mob;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out float player, out float mob)
    {
        player = Player;
        mob = Mob;
    }
}
