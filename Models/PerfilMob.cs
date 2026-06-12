namespace Runage.Models;

public class PerfilMob
{
    public string Nome { get; set; }
    public Arquetipo Arquetipo { get; set; }
    public float Temperatura { get; set; }
    public float[] InstintosBase { get; set; }
    public float RecompensaAbate { get; set; }
    public float HpBase { get; set; }
    public float DanoLeve { get; set; }
    public float DanoPesado { get; set; }

    public PerfilMob(string nome, Arquetipo arquetipo, float temp, float[] instintosBase, 
                     float recompensaAbate, float hpBase, float danoLeve, float danoPesado)
    {
        Nome = nome; 
        Arquetipo = arquetipo;
        Temperatura = temp; 
        InstintosBase = instintosBase;
        RecompensaAbate = recompensaAbate;
        HpBase = hpBase;
        DanoLeve = danoLeve;
        DanoPesado = danoPesado;
    }
}