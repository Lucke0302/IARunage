using System;

namespace Runage.Utils;

public static class InstinctHelper
{
    private static readonly float[] InstintosPacifico = { -200f, -300f, 150f, 200f, 400f, -200f };
    private static readonly float[] InstintosNeutro   = { 0f, 0f, 0f, 0f, 0f, 0f };
    private static readonly float[] InstintosCaotico  = { 300f, 400f, -50f, 0f, -300f, 500f };

    public static float[] CalcularInstintos(float viesValor)
    {
        float[] playerInstintos = new float[6];

        for (int i = 0; i < 6; i++)
        {
            if (viesValor <= 0.5f)
            {
                float t = viesValor / 0.5f; 
                playerInstintos[i] = InstintosPacifico[i] + (InstintosNeutro[i] - InstintosPacifico[i]) * t;
            }
            else
            {
                float t = (viesValor - 0.5f) / 0.5f; 
                playerInstintos[i] = InstintosNeutro[i] + (InstintosCaotico[i] - InstintosNeutro[i]) * t;
            }
        }
        return playerInstintos;
    }
}