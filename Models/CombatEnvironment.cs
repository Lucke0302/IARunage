using System;
using System.Runtime.CompilerServices;

namespace Runage.Models;

/// <summary>
/// Ambiente de combate turn-based.
/// Otimizado para ZERO alocação no hot path: GetFeatures() retorna FeatureVector (struct),
/// ResolverTick retorna Reward (struct).
/// </summary>
public class CombatEnvironment
{
    public float PlayerHP = 100f;
    public float MobHP = 50f;
    public bool CombateMortal = false;
    public float PlayerMultiplicador = 1.0f;
    public float Distancia = 3f;
    public float AuraPlayer = 0f;
    public bool IsGameOver = false;

    public string CausaMortePlayer { get; private set; } = "Nenhuma";
    public float DanoSofridoPlayer { get; private set; } = 0f;
    public bool PlayerMorreuPorAutoDano { get; private set; } = false;

    private int turnosPacificos = 0;
    private int turnosPacificosTotal = 0;
    private bool alguemAtacou = false;
    private int playerCooldownRuna = 0;
    private int playerBuffFogo = 0;
    private int mobStunturnos = 0;
    private readonly PerfilMob perfilMob;
    private readonly float playerVies;
    private readonly float mobHpInicial;
    private float playerMaxHP;
    private readonly float mobMaxHP;
    private static readonly Random rnd = Random.Shared;

    private bool oportunistaFaseRecuo = false;
    private int oportunistaTurnosRecuo = 0;
    private readonly float mobDanoPesadoNormalizado;

    public CombatEnvironment(PerfilMob perfil, float vies)
    {
        perfilMob = perfil;
        playerVies = vies;

        MobHP = perfil.HpBase;
        mobHpInicial = perfil.HpBase;
        mobMaxHP = perfil.HpBase;

        float danoPesadoReal = perfil.DanoPesado * perfil.Arquetipo.MultiplicadorDano;
        mobDanoPesadoNormalizado = Math.Clamp(danoPesadoReal / 150f, 0f, 1f);

        float fatorHP = (vies < 0.5f) ? (1.5f - vies) : 1.0f;
        float fatorDano = (vies > 0.5f) ? (0.5f + vies) : 1.0f;

        playerMaxHP = 100f * fatorHP;
        PlayerHP = playerMaxHP;
        PlayerMultiplicador = fatorDano;
    }

    public void AplicarMultiplicadorPlayer(float multiplicador)
    {
        PlayerMultiplicador *= multiplicador;
        float newMax = playerMaxHP * multiplicador;
        PlayerHP = Math.Min(newMax, PlayerHP * multiplicador);
        playerMaxHP = newMax;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalcularDanoOrganico(float baseDamage)
    {
        float variacao = (float)(rnd.NextDouble() * 0.4 + 0.8);
        bool critico = rnd.NextDouble() < 0.1;
        return baseDamage * variacao * (critico ? 2.0f : 1.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FeatureVector GetFeatures()
    {
        float hpPlayerNorm = Math.Clamp(PlayerHP / playerMaxHP, 0f, 1f);
        float hpMobNorm = Math.Clamp(MobHP / mobMaxHP, 0f, 1f);

        return new FeatureVector(
            f0: 1.0f - (Math.Clamp(Distancia, 0f, 3f) / 3.0f),
            f1: hpPlayerNorm,
            f2: hpMobNorm,
            f3: (Math.Clamp(AuraPlayer, -100f, 100f) + 100f) / 200f,
            f4: Math.Clamp(turnosPacificos / 6.0f, 0f, 1f),
            f5: (playerCooldownRuna == 0) ? 1.0f : 0.0f,
            f6: (mobStunturnos > 0) ? 1.0f : 0.0f,
            f7: mobDanoPesadoNormalizado
        );
    }

    public Reward ResolverTick(int acaoPlayer, int acaoMob)
    {
                alguemAtacou = false;
        bool ataqueOcorreuNoTurno = false;

        float rPlayer = 0f, rMob = 0f;
        float mobHpAnterior = MobHP;
        bool ultimoDanoFoiAutoDano = false;
        float danoM = 0f;
        
        if (playerCooldownRuna > 0) playerCooldownRuna--;
        if (playerBuffFogo > 0) playerBuffFogo--;

        bool playerAtacouBase = (acaoPlayer == 0 || acaoPlayer == 1);
        bool playerUsouRuna = (acaoPlayer == 5);
        bool playerUsouSobrecarga = (acaoPlayer == 6);
        bool playerAgressivo = playerAtacouBase || playerUsouRuna || playerUsouSobrecarga;
        
        bool mobTentouAtacar = (acaoMob == 0 || acaoMob == 1 || acaoMob == 5);
        bool playerUsouParry = (acaoPlayer == 7);
        var arq = perfilMob.Arquetipo;

        if (mobStunturnos > 0)
        {
            mobStunturnos--;
            acaoMob = 4;
            mobTentouAtacar = false;
        }

        // PARRY
        if (playerUsouParry && Distancia == 0)
        {
            if (mobTentouAtacar)
            {
                rPlayer += 60f;
                rMob -= 30f;
                mobStunturnos = 1;
                mobTentouAtacar = false;
                ataqueOcorreuNoTurno = true;
            }
            else
            {
                rPlayer -= 15f;
            }
        }

        // INSTINTO FORÇADO
        float fatorProximidade = arq.CicloOportunista ? 1.0f : (1.0f - (Distancia / 3.0f));
        float chanceRealDeAtaque = arq.ChanceAgressaoInicial * fatorProximidade;

        if (!alguemAtacou && rnd.NextDouble() < chanceRealDeAtaque)
        {
            if (!mobTentouAtacar) 
            {
                acaoMob = rnd.NextDouble() < 0.7 ? 0 : 1; 
                mobTentouAtacar = true;
                ataqueOcorreuNoTurno = true;
            }
        }
        else if (!alguemAtacou && mobTentouAtacar)
        {
            if (rnd.NextDouble() < arq.ChanceFugaPreventiva)
                acaoMob = 3; 
            else
                acaoMob = rnd.NextDouble() < 0.5 ? 3 : 4; 
            mobTentouAtacar = false;
        }

        // CICLO OPORTUNISTA
        if (arq.CicloOportunista && ataqueOcorreuNoTurno)
        {
            if (oportunistaFaseRecuo)
            {
                oportunistaTurnosRecuo++;
                if (oportunistaTurnosRecuo >= rnd.Next(1, 3))
                {
                    oportunistaFaseRecuo = false;
                    oportunistaTurnosRecuo = 0;
                    acaoMob = rnd.NextDouble() < 0.6 ? 0 : 1;
                    mobTentouAtacar = true;
                }
                else
                {
                    acaoMob = 3;
                    mobTentouAtacar = false;
                }
            }
            else if (mobTentouAtacar && rnd.NextDouble() < 0.45f)
            {
                oportunistaFaseRecuo = true;
                oportunistaTurnosRecuo = 0;
            }
        }

        // SOBRECARGA
        if (playerUsouSobrecarga)
        {
            float poderAntiTanque = 50f + (MobHP * 0.10f); 
            float danoSobrecarga = CalcularDanoOrganico(poderAntiTanque) * PlayerMultiplicador;
            float custoHp = 10f * PlayerMultiplicador;
            PlayerHP -= custoHp; 
            ultimoDanoFoiAutoDano = true;
            
            // Penalidade extra se o mob já estiver com HP baixo (anti-overkill)
            if (MobHP < mobHpInicial * 0.3f)
            {
                rPlayer -= 100f * playerVies;
            }

            if (acaoMob == 3)
            { 
                rPlayer -= (custoHp * 2.5f);
                rPlayer -= 10f; 
                rMob += 40f; 
            } 
            else 
            { 
                float hpMobAntesSobrecarga = MobHP;
                MobHP -= danoSobrecarga; 
                float danoEfetivo = Math.Min(danoSobrecarga, hpMobAntesSobrecarga);
                float overkill = Math.Max(0f, danoSobrecarga - hpMobAntesSobrecarga);
                rPlayer += (danoEfetivo - overkill - custoHp) * playerVies; 
                rMob -= 80f; 
                float fraquezaMob = Math.Clamp(1.0f - (mobHpInicial / 200f), 0f, 1f);
                rPlayer -= 300f * fraquezaMob;
            }
        }

        // SISTEMA MORAL
        if (!alguemAtacou)
        {
            if (playerAgressivo)
            {
                AuraPlayer -= 50f; 
                float recompensaAgressao = -1500f * (1.0f - playerVies);
                rPlayer += recompensaAgressao;
                alguemAtacou = true;
                ataqueOcorreuNoTurno = true;

                if (mobTentouAtacar && rnd.NextDouble() < arq.ChanceFugaSeProvocado)
                {
                    acaoMob = 3;
                    mobTentouAtacar = false;
                }
            }
            else if (mobTentouAtacar)
            {
                AuraPlayer += 15f; 
                alguemAtacou = true;
                ataqueOcorreuNoTurno = true;
                float punicaoMob = -2500f * (1.0f - arq.ChanceAgressaoInicial);
                rMob += punicaoMob;
            }
        }

        // PAZ
        bool playerPacifico = (acaoPlayer == 2 || acaoPlayer == 3 || acaoPlayer == 4 || acaoPlayer == 7);
        bool mobPacifico = (acaoMob == 2 || acaoMob == 3 || acaoMob == 4);

        if (perfilMob.ResisteHitKill && mobHpAnterior >= mobHpInicial && MobHP <= 0)
            MobHP = 1f;

        if (PlayerHP <= 0 || MobHP <= 0) IsGameOver = true;

        if (!IsGameOver && playerPacifico && mobPacifico)
        {
            turnosPacificos++;
            turnosPacificosTotal++;

            float recompensaPacifica = Math.Max(0f, 20f - (turnosPacificosTotal * 1.5f));
            float fatorPaz = Math.Clamp(1.0f - playerVies, 0f, 1f);
            rPlayer += recompensaPacifica * fatorPaz + (-10f) * (1f - fatorPaz); 
            rMob += recompensaPacifica; 

            bool travaAtivada = CombateMortal && (alguemAtacou || arq.ChanceAgressaoInicial >= 0.8f);

            if (CombateMortal && MobHP < (mobHpInicial * arq.LimiarHpFuga) && Distancia >= 3 && acaoMob == 3)
            {
                IsGameOver = true;
                float fatorFugaParcial = Math.Clamp(1.0f - playerVies, 0f, 1f);
                rPlayer += perfilMob.RecompensaAbate * 0.8f * fatorFugaParcial; 
                return new Reward(rPlayer, rMob);
            }

            if (turnosPacificos >= 6 && !travaAtivada) 
            {
                IsGameOver = true;
                float bonusEmpateFinal;
                if (playerVies <= 0.5f)
                    bonusEmpateFinal = Math.Max(0f, 120f - (turnosPacificosTotal * 4.0f));
                else
                {
                    float t = (playerVies - 0.5f) / 0.5f;
                    float bonusBase = Math.Max(0f, 120f - (turnosPacificosTotal * 4.0f));
                    bonusEmpateFinal = bonusBase + (-200f - bonusBase) * t;
                }
                rPlayer += bonusEmpateFinal;
                rMob += 120f * (1.0f - arq.ChanceAgressaoInicial);
                return new Reward(rPlayer, rMob);
            }
        }
        else if (!IsGameOver)
        {
            turnosPacificos = 0; 
        }

        if (mobPacifico && arq.ChanceAgressaoInicial >= 0.6f)
            rMob -= 30f;

        if (mobTentouAtacar && !playerAgressivo && arq.ChanceAgressaoInicial < 0.3f)
        {
            float grauDefensivo = 1.0f - (arq.ChanceAgressaoInicial / 0.3f);
            rMob -= 800f * grauDefensivo;
        }
        if (acaoPlayer == 4 && !mobPacifico)
            rPlayer -= turnosPacificosTotal * 0.8f;

        if (!IsGameOver && playerPacifico && MobHP >= mobHpInicial * 0.5f)
        {
            float pressaoInacao = 8f * (MobHP / mobHpInicial) * playerVies;
            rPlayer -= pressaoInacao;
        }

        if (turnosPacificos > 0 && rnd.NextDouble() < arq.ChanceAtaqueAposEmpate)
        {
            acaoMob = 0; 
            mobTentouAtacar = true;
        }

        // MOVIMENTAÇÃO
        if (acaoPlayer == 3) Distancia = Math.Max(0, Distancia - 1f);
        else if (acaoPlayer == 4) Distancia = Math.Min(3f, Distancia + 1f);

        if (acaoMob == 3) Distancia = Math.Max(0, Distancia - 1f);
        else if (acaoMob == 4) Distancia = Math.Min(3f, Distancia + 1f);

        if (arq.FechaDistanciaAoAtacar && mobTentouAtacar && Distancia == 1)
            Distancia = 0; 

        if (MobHP < (mobHpInicial * arq.LimiarHpFuga) && rnd.NextDouble() < arq.ChanceFugaHpBaixo)
        {
            acaoMob = 3; 
            mobTentouAtacar = false;
            Distancia = Math.Min(3f, Distancia + 1f);
        }

        // RUNA
        if (playerUsouRuna)
        {
            playerCooldownRuna = 5;
            playerBuffFogo = 3;
            float danoRuna = CalcularDanoOrganico(20f) * PlayerMultiplicador;
            if (acaoMob == 3) { rPlayer -= 10f; rMob += 40f; } 
            else { 
                float hpMobAntesRuna = MobHP;
                MobHP -= danoRuna;
                float overkillRuna = Math.Max(0f, danoRuna - hpMobAntesRuna);
                float fraquezaMobRuna = Math.Clamp(1.0f - (mobHpInicial / 200f), 0f, 1f);
                rPlayer -= 80f * fraquezaMobRuna * Math.Clamp(overkillRuna / Math.Max(1f, hpMobAntesRuna), 0f, 1f);
                rPlayer += 55f * playerVies; 
                rMob -= 60f; 
            }
        }

        // RESOLUÇÃO DE DANO
        if (Distancia > 0)
        {
            if (playerAtacouBase) rPlayer -= 5f; 
            if (mobTentouAtacar) rMob -= 5f;
        }
        else
        {
            if (playerAtacouBase)
            {
                if (acaoPlayer == 1) rPlayer -= 15f; 

                float baseDano = (acaoPlayer == 0) ? 10f : 30f;
                if (playerBuffFogo > 0) baseDano *= 1.5f; 
                float danoP = CalcularDanoOrganico(baseDano) * PlayerMultiplicador;

                if (acaoMob == 3) { rPlayer -= 10f; rMob += 20f; } 
                else if (acaoMob == 2) 
                {
                    if (acaoPlayer == 1)
                    {
                        MobHP -= danoP;
                        rPlayer += (danoP * 2f) * playerVies;
                        rPlayer += 20f;
                        rMob -= 60f;
                    }
                    else
                    {
                        rPlayer -= 10f;
                        rMob += 10f;
                        if (rnd.NextDouble() < arq.ChanceContraAtaque)
                        {
                            float danoContra = CalcularDanoOrganico(danoP * arq.MultiplicadorContraAtaque);
                            PlayerHP -= danoContra;
                            ultimoDanoFoiAutoDano = false;
                            rPlayer -= danoContra * 1.2f;
                        }
                    }
                } 
                else { 
                    MobHP -= danoP; 
                    rPlayer += (danoP * 2f) * playerVies; 
                    rMob -= 40f; 
                }
            }

            if (mobTentouAtacar)
            {
                float baseDano = (acaoMob == 0 || acaoMob == 5) ? perfilMob.DanoLeve : perfilMob.DanoPesado;
                baseDano *= arq.MultiplicadorDano;
                danoM = CalcularDanoOrganico(baseDano);

                if (acaoPlayer == 3) { rMob -= 10f; rPlayer += 20f; } 
                else if (acaoPlayer == 2) { rMob -= 10f; rPlayer += 10f; } 
                else 
                { 
                    PlayerHP -= danoM; 
                    ultimoDanoFoiAutoDano = false;
                    float multiplicadorSadismo = 1.0f + arq.ChanceAgressaoInicial; 
                    rMob += (danoM * 2f) * multiplicadorSadismo; 
                    rPlayer -= (danoM * 2.5f); 
                }
            }
        }

        if (perfilMob.ResisteHitKill && mobHpAnterior >= mobHpInicial && MobHP <= 0)
            MobHP = 1f;

        if (PlayerHP <= 0 || MobHP <= 0) IsGameOver = true;

        if (PlayerHP <= 0) 
        { 
            float pesoMorte = 500f + (500f * playerVies);
            if (ultimoDanoFoiAutoDano) 
            {
                pesoMorte *= 3.0f;
                CausaMortePlayer = "Auto-dano (Sobrecarga)";
                PlayerMorreuPorAutoDano = true;
                DanoSofridoPlayer = 10f * PlayerMultiplicador; // custo da Sobrecarga
            }
            else
            {
                CausaMortePlayer = "Dano do mob";
                PlayerMorreuPorAutoDano = false;
                DanoSofridoPlayer = danoM;
            }
            rPlayer -= pesoMorte; 
            rMob += 200f; 
        }

        float danoCausadoNesteTurno = mobHpAnterior - MobHP;
        if (danoCausadoNesteTurno > 0 && playerVies > 0.1f) 
        {
            float percentualRoubo = 0.15f * playerVies;
            float curaVampirica = danoCausadoNesteTurno * percentualRoubo;
            PlayerHP = Math.Min(playerMaxHP, PlayerHP + curaVampirica);
        }

        if (MobHP <= 0) 
        {
            rMob -= 150f; 
            rPlayer += (perfilMob.RecompensaAbate * playerVies); 
            float curaAbate = 20f * playerVies * PlayerMultiplicador;
            PlayerHP = Math.Min(playerMaxHP, PlayerHP + curaAbate); 
        }

        return new Reward(rPlayer, rMob);
    }
}