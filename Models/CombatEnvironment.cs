using System;

namespace Runage.Models;

public class CombatEnvironment
{
    public float PlayerHP = 100f;
    public float MobHP = 50f;
    public float Distancia = 3f;
    public float AuraPlayer = 0f; 
    public bool IsGameOver = false;

    private int turnosPacificos = 0;
    private int turnosPacificosTotal = 0;
    private bool alguemAtacou = false;
    private int playerCooldownRuna = 0; 
    private int playerBuffFogo = 0;     
    private int playerDeathCount;
    private PerfilMob perfilMob;
    private float playerVies;
    private float mobHpInicial;
    private Random rnd = new();

    public CombatEnvironment(int deathCount, PerfilMob perfil, float vies)
    {
        playerDeathCount = deathCount;
        perfilMob = perfil;
        playerVies = vies; 
        
        MobHP = perfil.HpBase;
        mobHpInicial = perfil.HpBase;
    }

    private float CalcularDanoOrganico(float baseDamage)
    {
        float variacao = (float)(rnd.NextDouble() * 0.4 + 0.8); 
        bool critico = rnd.NextDouble() < 0.1; 
        return baseDamage * variacao * (critico ? 2.0f : 1.0f);
    }

    public float[] GetFeatures() => [ 
        1.0f - (Math.Clamp(Distancia, 0f, 3f) / 3.0f), 
        PlayerHP / 100f, 
        MobHP / mobHpInicial,                                 
        (Math.Clamp(AuraPlayer, -100f, 100f) + 100f) / 200f, 
        turnosPacificos / 6.0f,
        (playerCooldownRuna == 0) ? 1.0f : 0.0f 
    ];

    public float[] ResolverTick(int acaoPlayer, int acaoMob)
    {
        float[] recompensas = { 0f, 0f }; 
        
        if (playerCooldownRuna > 0) playerCooldownRuna--;
        if (playerBuffFogo > 0) playerBuffFogo--;

        bool playerAtacouBase = (acaoPlayer == 0 || acaoPlayer == 1);
        bool playerUsouRuna = (acaoPlayer == 5);
        bool playerAgressivo = playerAtacouBase || playerUsouRuna;
        
        bool mobTentouAtacar = (acaoMob == 0 || acaoMob == 1 || acaoMob == 5);
        var arq = perfilMob.Arquetipo;

        // --- INSTINTO FORÇADO DO MOB ---
        if (!alguemAtacou && rnd.NextDouble() < arq.ChanceAgressaoInicial)
        {
            if (!mobTentouAtacar) 
            {
                acaoMob = rnd.NextDouble() < 0.7 ? 0 : 1; 
                mobTentouAtacar = true;
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

        if (playerUsouRuna && playerCooldownRuna > 0)
        {
            recompensas[0] -= 20f; 
            playerUsouRuna = false;
            playerAgressivo = false;
        }

        // --- SISTEMA MORAL DO PLAYER ---
        if (!alguemAtacou)
        {
            if (playerAgressivo)
            {
                AuraPlayer -= 50f; 
                float recompensaAgressao;
                if (playerVies <= 0.5f)
                {
                    float t = playerVies / 0.5f;
                    recompensaAgressao = -1500f + (-500f - (-1500f)) * t; 
                }
                else
                {
                    float t = (playerVies - 0.5f) / 0.5f;
                    recompensaAgressao = -500f + (500f - (-500f)) * t; 
                }
                recompensas[0] += recompensaAgressao;
                alguemAtacou = true;

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
            }
        }

        // --- AVALIAÇÃO DE PAZ E PASSIVIDADE ---
        bool playerPacifico = (acaoPlayer == 2 || acaoPlayer == 3 || acaoPlayer == 4);
        bool mobPacifico = (acaoMob == 2 || acaoMob == 3 || acaoMob == 4);

        if (playerPacifico && mobPacifico)
        {
            turnosPacificos++;
            turnosPacificosTotal++;

            float recompensaPacifica = Math.Max(0f, 20f - (turnosPacificosTotal * 1.5f));
            float fatorPaz = Math.Clamp(1.0f - playerVies, 0f, 1f);
            
            recompensas[0] += recompensaPacifica * fatorPaz + (-10f) * (1f - fatorPaz); 
            recompensas[1] += recompensaPacifica; 

            if (turnosPacificos >= 6) 
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
                recompensas[0] += bonusEmpateFinal;
                
                // Mobs agressivos ganham menos ao empatar
                recompensas[1] += 120f * (1.0f - arq.ChanceAgressaoInicial);
                return recompensas;
            }
        }
        else
        {
            turnosPacificos = 0; 
        }

        // Punição por covardia para mobs hiper agressivos
        if (mobPacifico && arq.ChanceAgressaoInicial >= 0.6f)
            recompensas[1] -= 30f;

        if (acaoPlayer == 4 && !mobPacifico)
            recompensas[0] -= turnosPacificosTotal * 0.8f;

        if (turnosPacificos > 0 && rnd.NextDouble() < arq.ChanceAtaqueAposEmpate)
        {
            acaoMob = 0; 
            mobTentouAtacar = true;
        }

        // --- MOVIMENTAÇÃO ---
        if (acaoPlayer == 3 || acaoPlayer == 4) Distancia = Math.Max(0, Distancia - 1f); 
        if (acaoMob == 3 || acaoMob == 4) Distancia = Math.Max(0, Distancia - 1f);       

        if (arq.FechaDistanciaAoAtacar && mobTentouAtacar && Distancia == 1)
            Distancia = 0; 

        if (MobHP < (mobHpInicial * arq.LimiarHpFuga) && rnd.NextDouble() < arq.ChanceFugaHpBaixo)
        {
            acaoMob = 3; 
            mobTentouAtacar = false;
            Distancia = Math.Min(3f, Distancia + 1f);
        }

        // --- RESOLUÇÃO DE DANO E HABILIDADES ---
        if (playerUsouRuna)
        {
            playerCooldownRuna = 5;
            playerBuffFogo = 3;
            float danoRuna = CalcularDanoOrganico(20f);
            
            if (acaoMob == 3) { recompensas[0] -= 10f; recompensas[1] += 40f; } 
            else { 
                MobHP -= danoRuna; 
                recompensas[0] += 55f * playerVies; 
                recompensas[1] -= 60f; 
            }
        }

        if (Distancia > 0)
        {
            if (playerAtacouBase) recompensas[0] -= 5f; 
            if (mobTentouAtacar) recompensas[1] -= 5f;
        }
        else
        {
            if (playerAtacouBase)
            {
                if (acaoPlayer == 1) recompensas[0] -= 15f; 

                float baseDano = (acaoPlayer == 0) ? 10f : 30f;
                if (playerBuffFogo > 0) baseDano *= 1.5f; 
                float danoP = CalcularDanoOrganico(baseDano);

                if (acaoMob == 3) { recompensas[0] -= 10f; recompensas[1] += 20f; } 
                else if (acaoMob == 2) 
                { 
                    recompensas[0] -= 10f; 
                    recompensas[1] += 10f; 

                    if (rnd.NextDouble() < arq.ChanceContraAtaque)
                    {
                        float danoContra = CalcularDanoOrganico(danoP * arq.MultiplicadorContraAtaque);
                        PlayerHP -= danoContra;
                        recompensas[0] -= danoContra * 1.2f;
                    }
                } 
                else { 
                    MobHP -= danoP; 
                    recompensas[0] += (danoP * 2f) * playerVies; 
                    recompensas[1] -= 40f; 
                }
            }

            if (mobTentouAtacar)
            {
                float baseDano = (acaoMob == 0 || acaoMob == 5) ? perfilMob.DanoLeve : perfilMob.DanoPesado;
                baseDano *= arq.MultiplicadorDano;
                float danoM = CalcularDanoOrganico(baseDano);

                if (acaoPlayer == 3) { recompensas[1] -= 10f; recompensas[0] += 20f; } 
                else if (acaoPlayer == 2) { recompensas[1] -= 10f; recompensas[0] += 10f; } 
                else 
                { 
                    PlayerHP -= danoM; 
                    float multiplicadorSadismo = 1.0f + arq.ChanceAgressaoInicial; 
                    recompensas[1] += (danoM * 2f) * multiplicadorSadismo; 
                    recompensas[0] -= 40f; 
                }
            }
        }

        if (PlayerHP <= 0 || MobHP <= 0) IsGameOver = true;

        if (PlayerHP <= 0) 
        { 
            float pesoMorte = 500f + (500f * playerVies);
            recompensas[0] -= pesoMorte; 
            recompensas[1] += 200f; 
        }
        
        if (MobHP <= 0) 
        { 
            recompensas[1] -= 150f; 
            recompensas[0] += (perfilMob.RecompensaAbate * playerVies); 
        }

        return recompensas;
    }
}