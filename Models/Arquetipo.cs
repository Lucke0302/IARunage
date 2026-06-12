namespace Runage.Models;

public class Arquetipo
{
    public string Nome;

    // --- AGRESSÃO ---
    public float ChanceAgressaoInicial;
    public float ChanceAtaqueAposEmpate;

    // --- FUGA ---
    public float LimiarHpFuga;
    public float ChanceFugaHpBaixo;
    public float ChanceFugaPreventiva;
    public float ChanceFugaSeProvocado;

    // --- CICLO OPORTUNISTA ---
    public bool CicloOportunista;

    // --- COMBATE ---
    public float MultiplicadorDano;
    public bool FechaDistanciaAoAtacar;
    public float ChanceContraAtaque;
    public float MultiplicadorContraAtaque;

    public Arquetipo(string nome, float chanceAgressaoInicial, float chanceAtaqueAposEmpate,
        float limiarHpFuga, float chanceFugaHpBaixo, float chanceFugaPreventiva, float chanceFugaSeProvocado,
        bool cicloOportunista, float multiplicadorDano, bool fechaDistanciaAoAtacar,
        float chanceContraAtaque, float multiplicadorContraAtaque)
    {
        Nome = nome;
        ChanceAgressaoInicial = chanceAgressaoInicial;
        ChanceAtaqueAposEmpate = chanceAtaqueAposEmpate;
        LimiarHpFuga = limiarHpFuga;
        ChanceFugaHpBaixo = chanceFugaHpBaixo;
        ChanceFugaPreventiva = chanceFugaPreventiva;
        ChanceFugaSeProvocado = chanceFugaSeProvocado;
        CicloOportunista = cicloOportunista;
        MultiplicadorDano = multiplicadorDano;
        FechaDistanciaAoAtacar = fechaDistanciaAoAtacar;
        ChanceContraAtaque = chanceContraAtaque;
        MultiplicadorContraAtaque = multiplicadorContraAtaque;
    }
}

public static class Arquetipos
{
    public static readonly Arquetipo AnimalIndefeso = new(
        "Animal Indefeso",
        chanceAgressaoInicial: 0.05f, chanceAtaqueAposEmpate: 0.10f,
        limiarHpFuga: 0.5f, chanceFugaHpBaixo: 0.8f, chanceFugaPreventiva: 0.6f, chanceFugaSeProvocado: 0.7f,
        cicloOportunista: false, multiplicadorDano: 0.7f, fechaDistanciaAoAtacar: false,
        chanceContraAtaque: 0f, multiplicadorContraAtaque: 0f
    );

    public static readonly Arquetipo AnimalReativo = new(
        "Animal Reativo",
        chanceAgressaoInicial: 0f, chanceAtaqueAposEmpate: 0.30f,
        limiarHpFuga: 0.3f, chanceFugaHpBaixo: 0.4f, chanceFugaPreventiva: 0f, chanceFugaSeProvocado: 0.4f,
        cicloOportunista: false, multiplicadorDano: 1.0f, fechaDistanciaAoAtacar: false,
        chanceContraAtaque: 0f, multiplicadorContraAtaque: 0f
    );

    public static readonly Arquetipo AnimalAgressivo = new(
        "Animal Agressivo",
        chanceAgressaoInicial: 0.9f, chanceAtaqueAposEmpate: 0.85f,
        limiarHpFuga: 0.2f, chanceFugaHpBaixo: 0.5f, chanceFugaPreventiva: 0f, chanceFugaSeProvocado: 0f,
        cicloOportunista: false, multiplicadorDano: 1.8f, fechaDistanciaAoAtacar: true,
        chanceContraAtaque: 0f, multiplicadorContraAtaque: 0f
    );

    public static readonly Arquetipo HumanoComum = new(
        "Humano Comum",
        chanceAgressaoInicial: 0f, chanceAtaqueAposEmpate: 0f,
        limiarHpFuga: 0.7f, chanceFugaHpBaixo: 0.9f, chanceFugaPreventiva: 0f, chanceFugaSeProvocado: 0.9f,
        cicloOportunista: false, multiplicadorDano: 0.8f, fechaDistanciaAoAtacar: false,
        chanceContraAtaque: 0f, multiplicadorContraAtaque: 0f
    );

    public static readonly Arquetipo HumanoForte = new(
        "Humano++",
        chanceAgressaoInicial: 0f, chanceAtaqueAposEmpate: 0f,
        limiarHpFuga: 0.1f, chanceFugaHpBaixo: 0.3f, chanceFugaPreventiva: 0f, chanceFugaSeProvocado: 0.05f,
        cicloOportunista: false, multiplicadorDano: 1.4f, fechaDistanciaAoAtacar: false,
        chanceContraAtaque: 0f, multiplicadorContraAtaque: 0f
    );

    public static readonly Arquetipo Oportunista = new(
        "Oportunista",
        chanceAgressaoInicial: 0.6f, chanceAtaqueAposEmpate: 0.6f,
        limiarHpFuga: 0.3f, chanceFugaHpBaixo: 0.5f, chanceFugaPreventiva: 0f, chanceFugaSeProvocado: 0f,
        cicloOportunista: true, multiplicadorDano: 1.0f, fechaDistanciaAoAtacar: false,
        chanceContraAtaque: 0f, multiplicadorContraAtaque: 0f
    );

    public static readonly Arquetipo Inimigo = new(
        "Inimigo",
        chanceAgressaoInicial: 1.0f, chanceAtaqueAposEmpate: 0.85f,
        limiarHpFuga: 0.15f, chanceFugaHpBaixo: 0.3f, chanceFugaPreventiva: 0f, chanceFugaSeProvocado: 0f,
        cicloOportunista: false, multiplicadorDano: 1.3f, fechaDistanciaAoAtacar: false,
        chanceContraAtaque: 0f, multiplicadorContraAtaque: 0f
    );

    public static readonly Arquetipo DefensorVingativo = new(
        "Defensor Vingativo",
        chanceAgressaoInicial: 0f, chanceAtaqueAposEmpate: 0f,
        limiarHpFuga: 0.2f, chanceFugaHpBaixo: 0.2f, chanceFugaPreventiva: 0f, chanceFugaSeProvocado: 0.1f,
        cicloOportunista: false, multiplicadorDano: 1.3f, fechaDistanciaAoAtacar: false,
        chanceContraAtaque: 1.0f, multiplicadorContraAtaque: 0.5f
    );
}