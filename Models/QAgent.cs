using System;
using System.Runtime.CompilerServices;

namespace Runage.Models;

/// <summary>
/// Agente Q-Learning com aproximação linear de função.
/// Otimizado para ZERO alocação no hot path (mobile-first):
/// - Pesos em array flat [NUM_ACOES * NUM_FEATURES] para acesso contíguo.
/// - Buffers de Q-values reutilizáveis (pré-alocados), sem new float[] no hot path.
/// - FeatureVector (struct) em vez de float[].
/// </summary>
public sealed class QAgent
{
    public const int NUM_ACOES = 8;
    public const int NUM_FEATURES = 8;

    private readonly float[] _pesosFlat = new float[NUM_ACOES * NUM_FEATURES];

    private const float LearningRate = 0.05f;
    private const float DiscountFactor = 0.95f;
    private const float DecaimentoExploracao = 0.9995f;
    private const float PisoExploracao = 0.08f;

    private float _taxaExploracao = 1.0f;
    private readonly float _temperatura;

    private static readonly Random Rnd = Random.Shared;

    private static readonly int[] AcoesMob = { 0, 1, 2, 3, 4 };
    private static readonly int[] TodasAsAcoes = { 0, 1, 2, 3, 4, 5, 6, 7 };

    // Buffers reutilizáveis (pré-alocados) — eliminam new float[] no hot path
    private readonly float[] _qsBuffer = new float[NUM_ACOES];
    private readonly float[] _probBuffer = new float[NUM_ACOES];

    public QAgent(float temperatura, float[]? instintosBase = null)
        {
            if (temperatura < 0.01f)
                throw new ArgumentOutOfRangeException(
                    nameof(temperatura), temperatura,
                    "Temperatura deve ser >= 0.01f para evitar overflow numérico no softmax.");

            _temperatura = temperatura;

            if (instintosBase != null)
            {
                int limit = Math.Min(instintosBase.Length, NUM_ACOES);
                for (int a = 0; a < limit; a++)
                    _pesosFlat[a * NUM_FEATURES + 5] = instintosBase[a];
            }
        }

    public Span<float> PesosSpan() => new(_pesosFlat);

    public void ForcarAmadurecimento(float pisoExploracao = 0.2f)
    {
        _taxaExploracao = Math.Max(pisoExploracao, _taxaExploracao - 0.05f);
    }

    public void DefinirExploracao(float nivel) => _taxaExploracao = nivel;

    // -------------------- HOT PATH --------------------

    public int EscolherAcao(in FeatureVector features, int[]? acoesPermitidas = null)
    {
        int[] acoes = acoesPermitidas ?? TodasAsAcoes;
        int count = acoes.Length;

        if (Rnd.NextDouble() < _taxaExploracao)
            return acoes[Rnd.Next(count)];

        float maxQ = float.MinValue;
        for (int i = 0; i < count; i++)
        {
            float q = CalcularQ(acoes[i], features);
            _qsBuffer[i] = q;
            if (q > maxQ) maxQ = q;
        }

        float somaExp = 0f;
        float invTemp = 1f / _temperatura;
        for (int i = 0; i < count; i++)
        {
            float p = MathF.Exp((_qsBuffer[i] - maxQ) * invTemp);
            _probBuffer[i] = p;
            somaExp += p;
        }

        double roleta = Rnd.NextDouble() * somaExp;
        float acum = 0f;
        for (int i = 0; i < count; i++)
        {
            acum += _probBuffer[i];
            if (roleta <= acum) return acoes[i];
        }

        return ObterMelhorAcao(features, acoes, count, _qsBuffer);
    }

    public int EscolherAcaoMob(in FeatureVector features) =>
        EscolherAcao(features, AcoesMob);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalcularQ(int acao, in FeatureVector features)
    {
        int baseIdx = acao * NUM_FEATURES;
        return _pesosFlat[baseIdx]     * features.F0
             + _pesosFlat[baseIdx + 1] * features.F1
             + _pesosFlat[baseIdx + 2] * features.F2
             + _pesosFlat[baseIdx + 3] * features.F3
             + _pesosFlat[baseIdx + 4] * features.F4
             + _pesosFlat[baseIdx + 5] * features.F5
             + _pesosFlat[baseIdx + 6] * features.F6
             + _pesosFlat[baseIdx + 7] * features.F7;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ObterMelhorAcao(in FeatureVector features, int[] acoes, int count, float[]? buffer = null)
    {
        int melhor = acoes[0];
        float maxQ = float.MinValue;

        if (buffer != null)
        {
            for (int i = 0; i < count; i++)
            {
                if (buffer[i] > maxQ) { maxQ = buffer[i]; melhor = acoes[i]; }
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                float q = CalcularQ(acoes[i], features);
                if (q > maxQ) { maxQ = q; melhor = acoes[i]; }
            }
        }
        return melhor;
    }

    public void Aprender(in FeatureVector estadoAntigo, int acao, float recompensa,
                         in FeatureVector estadoNovo, int[]? acoesPermitidas = null)
        {
        if (acao < 0 || acao >= NUM_ACOES)
            ThrowAcaoInvalida(acao);

        if (float.IsInfinity(recompensa) || float.IsNaN(recompensa))
            ThrowRecompensaInvalida(recompensa);

        if (NaoEhFinita(estadoAntigo) || NaoEhFinita(estadoNovo))
            return;

        int[] acoes = acoesPermitidas ?? TodasAsAcoes;
        int count = acoes.Length;

        // MaxQ futuro iterando sem alocar arrays
        float maxQFuturo = float.MinValue;
        for (int i = 0; i < count; i++)
        {
            float q = CalcularQ(acoes[i], estadoNovo);
            if (q > maxQFuturo) maxQFuturo = q;
        }

        float qAtual = CalcularQ(acao, estadoAntigo);
        float erroTD = recompensa + (DiscountFactor * maxQFuturo) - qAtual;

        // Unroll manual para 8 features
        int baseIdx = acao * NUM_FEATURES;
        float lr = LearningRate * erroTD;

        _pesosFlat[baseIdx]     = Math.Clamp(_pesosFlat[baseIdx]     + lr * estadoAntigo.F0, -10000f, 10000f);
        _pesosFlat[baseIdx + 1] = Math.Clamp(_pesosFlat[baseIdx + 1] + lr * estadoAntigo.F1, -10000f, 10000f);
        _pesosFlat[baseIdx + 2] = Math.Clamp(_pesosFlat[baseIdx + 2] + lr * estadoAntigo.F2, -10000f, 10000f);
        _pesosFlat[baseIdx + 3] = Math.Clamp(_pesosFlat[baseIdx + 3] + lr * estadoAntigo.F3, -10000f, 10000f);
        _pesosFlat[baseIdx + 4] = Math.Clamp(_pesosFlat[baseIdx + 4] + lr * estadoAntigo.F4, -10000f, 10000f);
        _pesosFlat[baseIdx + 5] = Math.Clamp(_pesosFlat[baseIdx + 5] + lr * estadoAntigo.F5, -10000f, 10000f);
        _pesosFlat[baseIdx + 6] = Math.Clamp(_pesosFlat[baseIdx + 6] + lr * estadoAntigo.F6, -10000f, 10000f);
        _pesosFlat[baseIdx + 7] = Math.Clamp(_pesosFlat[baseIdx + 7] + lr * estadoAntigo.F7, -10000f, 10000f);

        if (_taxaExploracao > PisoExploracao)
            _taxaExploracao *= DecaimentoExploracao;
    }

    // -------------------- Serialização --------------------

    public float[][] ExportarPesos()
    {
        var export = new float[NUM_ACOES][];
        for (int i = 0; i < NUM_ACOES; i++)
        {
            var linha = new float[NUM_FEATURES];
            int baseIdx = i * NUM_FEATURES;
            for (int j = 0; j < NUM_FEATURES; j++)
                linha[j] = _pesosFlat[baseIdx + j];
            export[i] = linha;
        }
        return export;
    }

    public void ImportarPesos(float[][] pesosCarregados)
    {
        if (pesosCarregados == null || pesosCarregados.Length != NUM_ACOES)
            throw new ArgumentException($"Esperado {NUM_ACOES} ações, recebido {pesosCarregados?.Length ?? 0}.");
        if (pesosCarregados[0].Length != NUM_FEATURES)
            throw new ArgumentException($"Esperado {NUM_FEATURES} features, recebido {pesosCarregados[0].Length}.");

                        for (int i = 0; i < NUM_ACOES; i++)
        {
            int baseIdx = i * NUM_FEATURES;
            for (int j = 0; j < NUM_FEATURES; j++)
                _pesosFlat[baseIdx + j] = pesosCarregados[i][j];
        }
    }

    // -------------------- Sanitização & Guardas --------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool NaoEhFinita(in FeatureVector f)
    {
        return float.IsNaN(f.F0)     || float.IsInfinity(f.F0)
            || float.IsNaN(f.F1)     || float.IsInfinity(f.F1)
            || float.IsNaN(f.F2)     || float.IsInfinity(f.F2)
            || float.IsNaN(f.F3)     || float.IsInfinity(f.F3)
            || float.IsNaN(f.F4)     || float.IsInfinity(f.F4)
            || float.IsNaN(f.F5)     || float.IsInfinity(f.F5)
            || float.IsNaN(f.F6)     || float.IsInfinity(f.F6)
            || float.IsNaN(f.F7)     || float.IsInfinity(f.F7);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowAcaoInvalida(int acao) =>
        throw new ArgumentOutOfRangeException(nameof(acao), acao,
            $"Ação deve estar no intervalo [0-{NUM_ACOES - 1}], recebido {acao}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowRecompensaInvalida(float recompensa) =>
        throw new ArgumentOutOfRangeException(nameof(recompensa), recompensa,
            "Recompensa não pode ser NaN ou Infinity.");
}