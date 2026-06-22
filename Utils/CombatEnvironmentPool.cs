using System.Collections.Concurrent;
using System.Collections.Generic;
using Runage.Models;

namespace Runage.Utils;

public static class CombatEnvironmentPool
{
    private const int Capacity = 16;
    private static readonly ConcurrentBag<CombatEnvironment> _pool = new();

    public static CombatEnvironment Get(PerfilMob perfil, float vies, float multiplicadorMob = 1.0f)
    {
        if (_pool.TryTake(out var env))
        {
            env.Reset(perfil, vies, multiplicadorMob);
            return env;
        }

        return new CombatEnvironment(perfil, vies, multiplicadorMob);
    }

    public static void Return(CombatEnvironment env)
    {
        _pool.Add(env);
    }
}