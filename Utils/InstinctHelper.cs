using System;

namespace Runage.Utils;

public static class InstinctHelper
{
    public static float[] CalcularInstintos(float viesValor)
    {
        float[] instintosPacifico = { -200f, -300f, 150f, 200f, 400f, -200f };
        float[] instintosNeutro   = { 0f, 0f, 0f, 0f, 0f, 0f };
        float[] instintosCaotico  = { 300f, 400f, -50f, 0f, -300f, 500f };
        float[] playerInstintos = new float[6];

        for (int i = 0; i < 6; i++)
        {
            if (viesValor <= 0.5f)
            {
                float t = viesValor / 0.5f; 
                playerInstintos[i] = instintosPacifico[i] + (instintosNeutro[i] - instintosPacifico[i]) * t;
            }
            else
            {
                float t = (viesValor - 0.5f) / 0.5f; 
                playerInstintos[i] = instintosNeutro[i] + (instintosCaotico[i] - instintosNeutro[i]) * t;
            }
        }
        return playerInstintos;
    }
}