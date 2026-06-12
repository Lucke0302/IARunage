using System;

namespace Runage.Models;

public class CombatEnvironment
{
    public float PlayerHP = 100f;
    public float MobHP = 50f;
    public bool CombateMortal = false;
    public float PlayerMultiplicador = 1.0f;
    public float Distancia = 3f;
    public float AuraPlayer = 0f; 
    public bool IsGameOver = false;

    private int turnosPacificos = 0;
    private int turnosPacificosTotal = 0;
    private bool alguemAtacou = false;
    private int playerCooldownRuna = 0; 
    private int playerBuffFogo = 0;     
    private int mobStunturnos = 0;
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
        Math.Clamp(PlayerHP / (100f * Math.Max(1f, PlayerMultiplicador)), 0f, 1f), 
        Math.Clamp(MobHP / Math.Max(1f, mobHpInicial), 0f, 1f),                                 
        (Math.Clamp(AuraPlayer, -100f, 100f) + 100f) / 200f, 
        Math.Clamp(turnosPacificos / 6.0f, 0f, 1f), 
        (playerCooldownRuna == 0) ? 1.0f : 0.0f,
        (mobStunturnos > 0) ? 1.0f : 0.0f
    ];

    public float[] ResolverTick(int acaoPlayer, int acaoMob)
    {
        float[] recompensas = { 0f, 0f }; 

        float mobHpAnterior = MobHP;
        bool ultimoDanoFoiAutoDano = false;
        
        if (playerCooldownRuna > 0) playerCooldownRuna--;
        if (playerBuffFogo > 0) playerBuffFogo--;

        bool playerAtacouBase = (acaoPlayer == 0 || acaoPlayer == 1);
        bool playerUsouRuna = (acaoPlayer == 5);
        bool playerUsouSobrecarga = (acaoPlayer == 6);
        bool playerAgressivo = playerAtacouBase || playerUsouRuna || playerUsouSobrecarga;
        
        bool mobTentouAtacar = (acaoMob == 0 || acaoMob == 1 || acaoMob == 5);
        bool playerUsouParry = (acaoPlayer == 7);
        var arq = perfilMob.Arquetipo;

        // Stun: se o mob foi atordoado no turno anterior, ele perde a ação deste turno
        if (mobStunturnos > 0)
        {
            mobStunturnos--;
            acaoMob = 4;
            mobTentouAtacar = false;
        }

        // --- PARRY ---
        // Bloqueio perfeito: se o mob estava de fato atacando, anula o dano e o atordoa
        // por 1 turno. Se o mob não atacou, o parry "erra" e o player fica vulnerável
        // (pequena punição), pra não ser estritamente dominante sobre Defesa normal.
        if (playerUsouParry && Distancia == 0)
        {
            if (mobTentouAtacar)
            {
                recompensas[0] += 60f;
                recompensas[1] -= 30f;
                mobStunturnos = 1;
                mobTentouAtacar = false; // anula o ataque deste turno também
            }
            else
            {
                recompensas[0] -= 15f;
            }
        }

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

        if (playerUsouSobrecarga)
        {
            float poderAntiTanque = 50f + (MobHP * 0.10f); 
            float danoSobrecarga = CalcularDanoOrganico(poderAntiTanque) * PlayerMultiplicador;
            
            float custoHp = 10f * PlayerMultiplicador;
            PlayerHP -= custoHp; 
            ultimoDanoFoiAutoDano = true;
            
            if (acaoMob == 3)
            { 
                // Mob esquivou: só o custo conta, sem dano nem overkill
                recompensas[0] -= (custoHp * 2.5f);
                recompensas[0] -= 10f; 
                recompensas[1] += 40f; 
            } 
            else 
            { 
                float hpMobAntesSobrecarga = MobHP;
                MobHP -= danoSobrecarga; 

                // Dano efetivo é limitado ao HP que o mob de fato tinha (overkill não conta).
                // Recompensa = dano efetivo - overkill - custo próprio em HP.
                // Contra mobs com pouco HP (Cervo), o overkill cancela o ganho e o
                // custo próprio domina, deixando a Sobrecarga claramente não-lucrativa.
                float danoEfetivo = Math.Min(danoSobrecarga, hpMobAntesSobrecarga);
                float overkill = Math.Max(0f, danoSobrecarga - hpMobAntesSobrecarga);

                recompensas[0] += (danoEfetivo - overkill - custoHp) * playerVies; 
                recompensas[1] -= 80f; 
            }
        }

        // --- SISTEMA MORAL DO PLAYER ---
        if (!alguemAtacou)
        {
            if (playerAgressivo)
            {
                AuraPlayer -= 50f; 
                
                float recompensaAgressao = -1500f * (1.0f - playerVies);
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
                
                // Impede que a IA de um Aldeão/Guarda decida virar um assassino só por pontos.
                float punicaoMob = -1000f * (1.0f - arq.ChanceAgressaoInicial);
                recompensas[1] += punicaoMob;
            }
        }

        // --- AVALIAÇÃO DE PAZ E PASSIVIDADE ---
        bool playerPacifico = (acaoPlayer == 2 || acaoPlayer == 3 || acaoPlayer == 4 || acaoPlayer == 7);
        bool mobPacifico = (acaoMob == 2 || acaoMob == 3 || acaoMob == 4);

        // Morte é sempre checada primeiro: se alguém já morreu (dano de Sobrecarga/Runa
        // resolvido antes deste bloco), o combate termina aqui e a recompensa de abate
        // é processada no final da função, nunca pelos returns de empate abaixo.
        if (PlayerHP <= 0 || MobHP <= 0) IsGameOver = true;

        if (!IsGameOver && playerPacifico && mobPacifico)
        {
            turnosPacificos++;
            turnosPacificosTotal++;

            float recompensaPacifica = Math.Max(0f, 20f - (turnosPacificosTotal * 1.5f));
            float fatorPaz = Math.Clamp(1.0f - playerVies, 0f, 1f);
            
            recompensas[0] += recompensaPacifica * fatorPaz + (-10f) * (1f - fatorPaz); 
            recompensas[1] += recompensaPacifica; 

            bool travaAtivada = CombateMortal && (alguemAtacou || arq.ChanceAgressaoInicial >= 0.8f);

            if (CombateMortal && MobHP < (mobHpInicial * arq.LimiarHpFuga) && Distancia >= 3 && acaoMob == 3)
            {
                IsGameOver = true;
                // Recompensa por deixar o mob fugir ferido, sem matar de fato:
                // inversamente proporcional ao viés. Pacífico (vies baixo) valoriza
                // evitar o combate final e recebe quase o loot completo; Caótico
                // (vies alto) não se contenta com isso e recebe bem menos.
                float fatorFugaParcial = Math.Clamp(1.0f - playerVies, 0f, 1f);
                recompensas[0] += perfilMob.RecompensaAbate * 0.8f * fatorFugaParcial; 
                return recompensas;
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
                recompensas[0] += bonusEmpateFinal;
                
                // Mobs agressivos ganham menos ao empatar
                recompensas[1] += 120f * (1.0f - arq.ChanceAgressaoInicial);
                return recompensas;
            }
        }
        else if (!IsGameOver)
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
            float danoRuna = CalcularDanoOrganico(20f) * PlayerMultiplicador;
            
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
                float danoP = CalcularDanoOrganico(baseDano) * PlayerMultiplicador;

                if (acaoMob == 3) { recompensas[0] -= 10f; recompensas[1] += 20f; } 
                else if (acaoMob == 2) 
                { 
                    recompensas[0] -= 10f; 
                    recompensas[1] += 10f; 

                    if (rnd.NextDouble() < arq.ChanceContraAtaque)
                    {
                        float danoContra = CalcularDanoOrganico(danoP * arq.MultiplicadorContraAtaque);
                        PlayerHP -= danoContra;
                        ultimoDanoFoiAutoDano = false;
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
                    ultimoDanoFoiAutoDano = false;
                    float multiplicadorSadismo = 1.0f + arq.ChanceAgressaoInicial; 
                    recompensas[1] += (danoM * 2f) * multiplicadorSadismo; 
                    
                    // O agente perde pontos proporcionais ao estrago que sofreu.
                    recompensas[0] -= (danoM * 2.5f); 
                }
            }
        }

        if (PlayerHP <= 0 || MobHP <= 0) IsGameOver = true;

        if (PlayerHP <= 0) 
        { 
            float pesoMorte = 500f + (500f * playerVies);

            // Morte "kamikaze": se o golpe final que zerou o HP foi auto-dano da
            // Sobrecarga (e não dano do mob), a punição é multiplicada — o agente
            // se matou com a própria habilidade, o que é muito pior que morrer em combate.
            if (ultimoDanoFoiAutoDano) pesoMorte *= 3.0f;

            recompensas[0] -= pesoMorte; 
            recompensas[1] += 200f; 
        }

        float danoCausadoNesteTurno = mobHpAnterior - MobHP;
        
        if (danoCausadoNesteTurno > 0 && playerVies > 0.1f) 
        {
            float percentualRoubo = 0.15f * playerVies;
            float curaVampirica = danoCausadoNesteTurno * percentualRoubo;
            
            PlayerHP = Math.Min(100f * PlayerMultiplicador, PlayerHP + curaVampirica);
        }

        if (MobHP <= 0) 
        {
            recompensas[1] -= 150f; 
            recompensas[0] += (perfilMob.RecompensaAbate * playerVies); 
            
            float curaAbate = 20f * playerVies * PlayerMultiplicador;
            PlayerHP = Math.Min(100f * PlayerMultiplicador, PlayerHP + curaAbate); 
        }

        return recompensas;
    }
}