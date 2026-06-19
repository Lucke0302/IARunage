namespace Runage.Models;

public readonly struct PerfilMob
{
    public string Nome { get; }
    public Arquetipo Arquetipo { get; }
    public float Temperatura { get; }
    public float[] InstintosBase { get; }
    public float RecompensaAbate { get; }
    public float HpBase { get; }
    public float DanoLeve { get; }
    public float DanoPesado { get; }
    public bool ResisteHitKill { get; }

    public PerfilMob(string nome, Arquetipo arquetipo, float temp, float[] instintosBase,
                     float recompensaAbate, float hpBase, float danoLeve, float danoPesado,
                     bool resisteHitKill = false)
    {
        Nome = nome;
        Arquetipo = arquetipo;
        Temperatura = temp;
        InstintosBase = instintosBase;
        RecompensaAbate = recompensaAbate;
        HpBase = hpBase;
        DanoLeve = danoLeve;
        DanoPesado = danoPesado;
        ResisteHitKill = resisteHitKill;
    }
}
