using System.Runtime.CompilerServices;

namespace Runage.Models;

/// <summary>
/// Vetor de 8 features como struct — ZERO alocação de heap.
/// Substitui float[] para eliminar garbage collection no hot path do Q-Learning.
/// </summary>
public readonly struct FeatureVector
{
    // Expoe cada feature como campo público readonly (acesso direto, sem property overhead)
    public readonly float F0;
    public readonly float F1;
    public readonly float F2;
    public readonly float F3;
    public readonly float F4;
    public readonly float F5;
    public readonly float F6;
    public readonly float F7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FeatureVector(
        float f0, float f1, float f2, float f3,
        float f4, float f5, float f6, float f7)
    {
        F0 = f0;
        F1 = f1;
        F2 = f2;
        F3 = f3;
        F4 = f4;
        F5 = f5;
        F6 = f6;
        F7 = f7;
    }

    /// <summary>
    /// Acessa feature por índice (0-7). Para compatibilidade com loops genéricos.
    /// </summary>
    public float this[int index] => index switch
    {
        0 => F0,
        1 => F1,
        2 => F2,
        3 => F3,
        4 => F4,
        5 => F5,
        6 => F6,
        7 => F7,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
