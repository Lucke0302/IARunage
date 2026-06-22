using System;
using System.Collections.Generic;
using Runage.Models;
using Runage.Utils;

namespace Runage.Engines;

public static class ModoSobrevivencia
{
    // Cache estático de máscaras de ações — evita List + ToArray a cada sala
    private static readonly int[][] AcoesPorAndarCache;

    static ModoSobrevivencia()
    {
        const int maxAndar = 100;
        AcoesPorAndarCache = new int[maxAndar + 1][];
        for (int andar = 0; andar <= maxAndar; andar++)
        {
            var lista = new System.Collections.Generic.List<int>(6) { 0, 1, 2, 3, 4, 5 };
            if (andar >= 5) lista.Add(7);
            if (andar >= 10) lista.Add(6);
            AcoesPorAndarCache[andar] = lista.ToArray();
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static int[] ObterAcoes(int andar)
    {
        int idx = Math.Clamp(andar, 0, AcoesPorAndarCache.Length - 1);
        return AcoesPorAndarCache[idx];
    }

    public static async Task IniciarAsync(float viesValor, ILogger logger, IProgressReporter? progress = null)
    {
        logger.Log("==================================================");
        logger.Log(" MANOPLA DE PRODÍGIOS (MODO SOBREVIVÊNCIA) ");
        logger.Log("==================================================\n");

        float[][]? pesosGlobais = await DataHandler.CarregarPesosPlayerAsync(viesValor, "pesos_Global.json");
        if (pesosGlobais == null)
        {
            logger.LogError("ERRO: Cérebro Global não encontrado! Execute a Forja Massiva [2] primeiro.");
            return;
        }

        int vidas = 3;
        int andarMaximo = 100;
        float playerMultiplicador = 1.0f; 
        var dicionarioMobs = ObterTodosOsMobs();
        Random rnd = new();

        logger.Log($"Acessando matriz neural (Viés: {viesValor:F1})...\nO Desafio começou.\n");

        for (int andar = 1; andar <= andarMaximo; andar++)
        {
            logger.LogInfo($"=== ANDAR {andar} ===");

            int tensaoDoAndar = (andar % 10 == 0) ? 10 : andar % 10;
            float multiplicadorMob = 1.0f + (andar * 0.05f);
            
            if (tensaoDoAndar == 10) 
            {
                multiplicadorMob *= 1.20f; 
                logger.LogWarning("CUIDADO: Presença Colossal Detectada (Andar de Chefe).");
            }

            for (int sala = 1; sala <= 5; sala++)
            {
                PerfilMob perfilBase = SortearNpc(tensaoDoAndar, dicionarioMobs, rnd);

                    int[] acoesPermitidas = ObterAcoes(andar);
                
                logger.Log($"Sala {sala}/5: {perfilBase.Nome} -> ");

                // Puxando o novo retorno de Tupla
                var resultado = await ExecutarCombateAsync(viesValor, pesosGlobais, perfilBase, playerMultiplicador, acoesPermitidas, multiplicadorMob);
                if (!resultado.sobreviveu)
                {
                    vidas--;
                    logger.LogError($"MORTE! O {perfilBase.Nome} foi letal. Vidas restantes: {vidas}");

                    if (vidas <= 0) 
                    {
                        logger.Log($"\nFIM DE JOGO. O Player sucumbiu no Andar {andar}, Sala {sala}.");
                        return; 
                    }
                    sala--; // Repete a sala
                }
                else
                {
                    // Colore o console dinamicamente conforme a agressividade do desfecho
                    if (resultado.detalhe.Contains("Abatido"))
                        logger.LogWarning($"Sala {sala}/5: {perfilBase.Nome} -> Vitória! ({resultado.detalhe})");
                    else if (resultado.detalhe.Contains("Exaustão"))
                        logger.LogWarning($"Sala {sala}/5: {perfilBase.Nome} -> Vitória! ({resultado.detalhe})");
                    else
                        logger.LogSuccess($"Sala {sala}/5: {perfilBase.Nome} -> Vitória! ({resultado.detalhe})");
                    
                    playerMultiplicador += 0.04f; 
                }
            }
            logger.Log($"Andar {andar} limpo! Status Base ampliado: {playerMultiplicador:F2}x\n");
        }
        
        logger.LogWarning("\nPARABÉNS! VOCÊ CONQUISTOU A MANOPLA DE PRODÍGIOS!");
    }

    private static async Task<(bool sobreviveu, string detalhe)> ExecutarCombateAsync(
        float viesValor, 
        float[][] pesosGlobais, 
        PerfilMob inimigo, 
        float pMultiplicador, 
        int[] acoesPermitidas,
        float multiplicadorMob = 1.0f)
    {
        QAgent player = new QAgent(15.0f, null);
        player.ImportarPesos(pesosGlobais);

        // Carregamento assíncrono dos pesos específicos
        float[][]? pesosEspecificos = await DataHandler.CarregarPesosPlayerAsync(viesValor, $"pesos_{inimigo.Nome.Replace(" ", "_")}.json");
        if (pesosEspecificos != null) player.ImportarPesos(pesosEspecificos);

        player.DefinirExploracao(0.05f);

        QAgent mob = new QAgent(inimigo.Temperatura, inimigo.InstintosBase);
        // Carregamento assíncrono dos pesos da colmeia
        float[][]? pesosColmeia = await DataHandler.CarregarPesosNPCAsync(inimigo.Nome, viesValor);
        if (pesosColmeia != null) mob.ImportarPesos(pesosColmeia);
        mob.DefinirExploracao(0.05f);

        CombatEnvironment env = CombatEnvironmentPool.Get(inimigo, viesValor, multiplicadorMob);
        try
        {
        env.CombateMortal = true;
        env.AplicarMultiplicadorPlayer(pMultiplicador);
        
                
        const int maxTurnos = 1000;
        int turno = 0;
        bool ativo = true;

        while (ativo && turno < maxTurnos)
        {
            turno++;
            FeatureVector estado = env.GetFeatures();

            int acaoPlayer = player.EscolherAcao(estado, acoesPermitidas);
            int acaoMob = mob.EscolherAcaoMob(estado);

            env.ResolverTick(acaoPlayer, acaoMob);

            if (env.IsGameOver) ativo = false;
        }

        if (env.PlayerHP <= 0) 
        {
            string causa = env.CausaMortePlayer;
            float dano = env.DanoSofridoPlayer;
            string detalheMorte = $"Morto por {causa} (dano: {dano:F1})";
            return (false, detalheMorte);
        }
        
        if (env.MobHP <= 0) 
            return (true, "Inimigo Abatido ⚔️");
        
        if (turno >= 1000) 
            return (true, "Sobreviveu por Exaustão de Tempo ⏳");
            
        return (true, "Resolução Pacífica ou Fuga 🕊️/🏃");
        }
        finally
        {
            CombatEnvironmentPool.Return(env);
        }
    }

        private static PerfilMob SortearNpc(int tensao, Dictionary<int, PerfilMob> dic, Random rnd)
    {
        int[] idsPossiveis = tensao switch
        {
            <= 3 => new[] { 1, 6 },
            <= 6 => new[] { 2, 4 },
            <= 9 => new[] { 3, 7 },
            _    => new[] { 5, 8 }
        };

        return dic[idsPossiveis[rnd.Next(idsPossiveis.Length)]];
    }

   private static Dictionary<int, PerfilMob> ObterTodosOsMobs()
    {
        return new Dictionary<int, PerfilMob>
        {
            // NOTA: os nomes aqui são usados tanto para exibição quanto para montar caminhos
            // de arquivo via DataHandler. O DataHandler normaliza os acentos internamente
            // antes de construir o path — veja NormalizarNome() em DataHandler.cs.
            { 1, new PerfilMob("Aldeão Assustado", Arquetipos.HumanoComum, 40.0f, new float[] { -50f, -100f, 50f, 300f, 300f, -100f }, 5f, 30f, 2f, 5f, false) },
            { 2, new PerfilMob("Lobo Faminto", Arquetipos.AnimalAgressivo, 80.0f, new float[] { 250f, 400f, 0f, 0f, -50f, 0f }, 150f, 50f, 8f, 20f, false) },
            { 3, new PerfilMob("Guarda Veterano", Arquetipos.HumanoForte, 20.0f, new float[] { 100f, 50f, 150f, 0f, 50f, 150f }, 500f, 800f, 12f, 30f, false) },
            { 4, new PerfilMob("Bandido Oportunista", Arquetipos.Oportunista, 60.0f, new float[] { 150f, 100f, 0f, 200f, -50f, 0f }, 250f, 60f, 10f, 25f, false) },
            { 5, new PerfilMob("Espírito Vingativo", Arquetipos.DefensorVingativo, 30.0f, new float[] { 50f, 0f, 200f, 50f, 100f, 300f }, 450f, 150f, 5f, 15f, false) },
            { 6, new PerfilMob("Cervo de Prodígios", Arquetipos.AnimalIndefeso, 70.0f, new float[] { -200f, -200f, 0f, 400f, 200f, -100f }, 150f, 15f, 1f, 3f, true) }, // <-- Sturdy ativado!
            { 7, new PerfilMob("Urso Territorial", Arquetipos.AnimalReativo, 30.0f, new float[] { 50f, 100f, 50f, 0f, 300f, 0f }, 250f, 120f, 20f, 50f, false) },
            { 8, new PerfilMob("Cultista Frenético", Arquetipos.Inimigo, 60.0f, new float[] { 300f, 150f, -50f, -100f, -200f, 200f }, 300f, 90f, 12f, 30f, false) }
        };
    }
}