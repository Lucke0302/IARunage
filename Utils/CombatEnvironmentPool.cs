using System.Collections.Generic;
using Runage.Models;

namespace Runage.Utils;

public static class CombatEnvironmentPool
{
    private const int Capacity = 16;
    private static readonly object _lock = new();
    private static readonly Stack<CombatEnvironment> _pool = new(Capacity);

    public static CombatEnvironment Get(PerfilMob perfil, float vies)
    {
        lock (_lock)
        {
            if (_pool.Count > 0)
            {
                CombatEnvironment env = _pool.Pop();
                env.Reset(perfil, vies);
                return env;
            }
        }

        return new CombatEnvironment(perfil, vies);
    }

    public static void Return(CombatEnvironment env)
    {
        lock (_lock)
        {
            if (_pool.Count < Capacity)
                _pool.Push(env);
        }
    }
}