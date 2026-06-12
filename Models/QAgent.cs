using System;

namespace Runage.Models;

public class QAgent
{
    private const int NUM_ACOES = 6; 
    private const int NUM_FEATURES = 6; 
    private float[,] pesos = new float[NUM_ACOES, NUM_FEATURES];
    private float learningRate = 0.05f; 
    private float discountFactor = 0.95f; 
    private float taxaExploracao = 1.0f;  
    private float decaimentoExploracao = 0.999f; 
    private float temperatura;
    private Random rnd = new();

    public QAgent(float temp, float[]? instintosBase = null)
    {
        temperatura = temp;
        if (instintosBase != null)
        {
            for (int a = 0; a < instintosBase.Length; a++)
            {
                if (a < NUM_ACOES) pesos[a, 5] = instintosBase[a]; 
            }
        }
    }

    public void ForcarAmadurecimento(float pisoExploracao = 0.2f) 
    { 
        taxaExploracao = Math.Max(pisoExploracao, taxaExploracao - 0.05f); 
    }

    // Usado pelo gerador procedural para travar o agente no modo combate experiente
    public void DefinirExploracao(float nivel)
    {
        taxaExploracao = nivel;
    }

    public int EscolherAcao(float[] features)
    {
        if (rnd.NextDouble() < taxaExploracao) return rnd.Next(0, NUM_ACOES); 

        float[] qs = new float[NUM_ACOES];
        float maxQ = float.MinValue;
        
        for (int i = 0; i < NUM_ACOES; i++) 
        {
            qs[i] = CalcularQ(i, features);
            if (qs[i] > maxQ) maxQ = qs[i]; 
        }

        float somaExp = 0f;
        float[] probabilidades = new float[NUM_ACOES];
        
        for (int i = 0; i < NUM_ACOES; i++) 
        {
            probabilidades[i] = (float)Math.Exp((qs[i] - maxQ) / temperatura);
            somaExp += probabilidades[i];
        }

        double roleta = rnd.NextDouble() * somaExp;
        float acumulado = 0f;
        
        for (int i = 0; i < NUM_ACOES; i++) 
        {
            acumulado += probabilidades[i];
            if (roleta <= acumulado) return i; 
        }
        
        return ObterMelhorAcao(features); 
    }

    private int ObterMelhorAcao(float[] features)
    {
        int melhorAcao = 0;
        float maxQ = float.MinValue;
        for (int a = 0; a < NUM_ACOES; a++)
        {
            float q = CalcularQ(a, features);
            if (q > maxQ) { maxQ = q; melhorAcao = a; }
        }
        return melhorAcao;
    }

    private float CalcularQ(int acao, float[] features)
    {
        float q = 0;
        for (int i = 0; i < NUM_FEATURES; i++) q += pesos[acao, i] * features[i];
        return q;
    }

    public void Aprender(float[] estadoAntigo, int acao, float recompensa, float[] estadoNovo)
    {
        float maxQFuturo = CalcularQ(ObterMelhorAcao(estadoNovo), estadoNovo);
        float qAtual = CalcularQ(acao, estadoAntigo);
        float erroTD = recompensa + (discountFactor * maxQFuturo) - qAtual;

        for (int i = 0; i < NUM_FEATURES; i++)
        {
            float variacaoPeso = learningRate * erroTD * estadoAntigo[i];
            pesos[acao, i] = Math.Clamp(pesos[acao, i] + variacaoPeso, -10000f, 10000f); 
        }

        if (taxaExploracao > 0.15f) taxaExploracao *= decaimentoExploracao;
    }

    public float[][] ExportarPesos()
    {
        float[][] export = new float[NUM_ACOES][];
        for (int i = 0; i < NUM_ACOES; i++)
        {
            export[i] = new float[NUM_FEATURES];
            for (int j = 0; j < NUM_FEATURES; j++) export[i][j] = pesos[i, j];
        }
        return export;
    }

    public void ImportarPesos(float[][] pesosCarregados)
    {
        for (int i = 0; i < NUM_ACOES; i++)
        {
            for (int j = 0; j < NUM_FEATURES; j++)
            {
                pesos[i, j] = pesosCarregados[i][j];
            }
        }
    }
}