using System;
using System.Collections.Generic;
using Runage.Models;
using Runage.Utils;

namespace Runage.Engines;

public static class ForjaMassiva
{
    public static async Task IniciarAsync(float viesValor, ILogger logger, IProgressReporter? progress = null)
    {
        logger.Log("==================================================");
        logger.Log(" FORJA MASSIVA (TREINAMENTO REAL DE TRANSFERÊNCIA) ");
        logger.Log("==================================================\n");

        var dicionarioMobs = ObterTodosOsMobs();
        float tempPlayer = 15.0f;
        float[] instintosPlayer = InstinctHelper.CalcularInstintos(viesValor);

        logger.Log("[FASE 1] Forjando o Cérebro Global (Sobrevivência Básica)...");
        QAgent playerGlobal = new QAgent(tempPlayer, instintosPlayer);
        TreinarGlobal(playerGlobal, dicionarioMobs, viesValor, sims: 10, episodios: 6000, logger);
        
        float[][] pesosGlobais = playerGlobal.ExportarPesos();
        await DataHandler.SalvarPesosPlayerAsync(viesValor, "pesos_Global.json", pesosGlobais);
        logger.Log(">> Cérebro Global salvo com sucesso!\n");

        logger.Log("[FASE 2] Iniciando Especialização e Mente Coletiva NPC...\n");
        int mobIndex = 0;
        int totalMobs = dicionarioMobs.Count;
        foreach (var kvp in dicionarioMobs)
        {
            PerfilMob mobAtual = kvp.Value;
            logger.Log($"Especializando contra: {mobAtual.Nome}...");

            QAgent playerEspecializado = new QAgent(tempPlayer, instintosPlayer);
            playerEspecializado.ImportarPesos(pesosGlobais);
            playerEspecializado.DefinirExploracao(0.6f); 

            QAgent mobColmeia = new QAgent(mobAtual.Temperatura, mobAtual.InstintosBase);

            TreinarEspecializado(playerEspecializado, mobColmeia, mobAtual, viesValor, sims: 10, episodios: 6000, logger);

            await DataHandler.SalvarPesosPlayerAsync(viesValor, $"pesos_{mobAtual.Nome.Replace(" ", "_")}.json", playerEspecializado.ExportarPesos());
            await DataHandler.SalvarPesosNPCAsync(mobAtual.Nome, viesValor, mobColmeia.ExportarPesos());
            
            logger.Log($">> Conhecimento Tático e Colmeia salvos para {mobAtual.Nome}.\n");

            mobIndex++;
            progress?.ReportProgress((float)mobIndex / totalMobs * 100f, $"Processando {mobAtual.Nome}...");
        }

        logger.LogSuccess("==================================================");
        logger.LogSuccess(" FORJA CONCLUÍDA! O ecossistema está pronto para o jogo.");
        logger.LogSuccess("==================================================");
    }

    private static void TreinarGlobal(QAgent player, Dictionary<int, PerfilMob> mobs, float vies, int sims, int episodios, ILogger logger)
    {
        var listaMobs = new List<PerfilMob>(mobs.Values);
        Random rnd = new();

        int totalVitorias = 0;
        int totalDerrotas = 0;
        int totalEmpates = 0;

        for (int sim = 1; sim <= sims; sim++)
        {
            int andarMin = 1;
            int andarMax = sim * 10;
            
            logger.Log($"   -> Simulação {sim}/{sims} (Andares {andarMin}-{andarMax})...");

            for (int ep = 1; ep <= episodios; ep++)
            {
                PerfilMob mobSorteado = listaMobs[rnd.Next(listaMobs.Count)];
                
                int andarSorteado = rnd.Next(andarMin, andarMax + 1);
                float pMultiplicador = 1.0f + ((andarSorteado - 1) * 0.20f);
                float mMultiplicador = 1.0f + (andarSorteado * 0.05f);

                int resultado = ExecutarEpisodio(player, null, mobSorteado, vies, pMultiplicador, andarSorteado, mMultiplicador);
                
                if (resultado == 1) totalVitorias++;
                else if (resultado == -1) totalDerrotas++;
                else totalEmpates++;
            }
            float pisoAtual = 0.2f - ((sim - 1f) / (sims - 1f) * 0.15f);
            player.ForcarAmadurecimento(pisoAtual);
        }

        int totalLutas = sims * episodios;
        logger.LogInfo($"\n[ESTATÍSTICAS DO TREINO GLOBAL]");
        logger.LogInfo($"Vitórias: {(totalVitorias / (float)totalLutas):P2} | Derrotas: {(totalDerrotas / (float)totalLutas):P2} | Empates/Fugas: {(totalEmpates / (float)totalLutas):P2}\n");
    }

    private static void TreinarEspecializado(QAgent player, QAgent mob, PerfilMob perfil, float vies, int sims, int episodios, ILogger logger)
    {
        Random rnd = new();
        
        int totalVitorias = 0;
        int totalDerrotas = 0;
        int totalEmpates = 0;

        for (int sim = 1; sim <= sims; sim++)
        {
            int andarMin = 1;
            int andarMax = sim * 10;

            for (int ep = 1; ep <= episodios; ep++)
            {
                int andarSorteado = rnd.Next(andarMin, andarMax + 1);
                float pMultiplicador = 1.0f + ((andarSorteado - 1) * 0.20f);
                float mMultiplicador = 1.0f + (andarSorteado * 0.05f);

                int resultado = ExecutarEpisodio(player, mob, perfil, vies, pMultiplicador, andarSorteado, mMultiplicador);
                
                if (resultado == 1) totalVitorias++;
                else if (resultado == -1) totalDerrotas++;
                else totalEmpates++;
            }
            float pisoAtual = 0.2f - ((sim - 1f) / (sims - 1f) * 0.15f);
            player.ForcarAmadurecimento(pisoAtual);
            mob.ForcarAmadurecimento(pisoAtual);
        }

        int totalLutas = sims * episodios;
        logger.LogWarning($"   -> Stats vs {perfil.Nome}: Vitórias: {(totalVitorias / (float)totalLutas):P2} | Derrotas: {(totalDerrotas / (float)totalLutas):P2} | Empates: {(totalEmpates / (float)totalLutas):P2}");
    }

    private static readonly int[][] AcoesPorAndar;

    static ForjaMassiva()
    {
        const int andarLimite = 100;
        AcoesPorAndar = new int[andarLimite + 1][];
        for (int andar = 0; andar <= andarLimite; andar++)
        {
            var lista = new List<int>(6) { 0, 1, 2, 3, 4, 5 };
            if (andar >= 5) lista.Add(7);
            if (andar >= 10) lista.Add(6);
            AcoesPorAndar[andar] = lista.ToArray();
        }
    }

    private static int[] ObterAcoesPorAndar(int andar)
    {
        int idx = Math.Clamp(andar, 0, AcoesPorAndar.Length - 1);
        return AcoesPorAndar[idx];
    }

    private static int ExecutarEpisodio(QAgent player, QAgent? mobAgente, PerfilMob perfil, float vies, float pMultiplicador, int andar, float mMultiplicador = 1.0f)
    {
        CombatEnvironment env = CombatEnvironmentPool.Get(perfil, vies, mMultiplicador);
        try
        {
            env.CombateMortal = true;
            env.AplicarMultiplicadorPlayer(pMultiplicador);

            int[] acoesPermitidas = ObterAcoesPorAndar(andar);
            const int maxTurnos = 500;
            int turno = 0;
            bool ativo = true;

            while (ativo && turno < maxTurnos)
            {
                turno++;
                FeatureVector estado = env.GetFeatures();

                int acaoPlayer = player.EscolherAcao(estado, acoesPermitidas);
                int acaoMob = mobAgente?.EscolherAcaoMob(estado) ?? 0;

                Reward recompensas = env.ResolverTick(acaoPlayer, acaoMob);
                FeatureVector novoEstado = env.GetFeatures();

                player.Aprender(estado, acaoPlayer, recompensas.Player, novoEstado, acoesPermitidas);
                mobAgente?.Aprender(estado, acaoMob, recompensas.Mob, novoEstado);

                if (env.IsGameOver) ativo = false;
            }

            if (env.PlayerHP <= 0) return -1;
            if (env.MobHP <= 0) return 1;
            return 0;
        }
        finally
        {
            CombatEnvironmentPool.Return(env);
        }
    }

    private static Dictionary<int, PerfilMob> ObterTodosOsMobs()
    {
        return new Dictionary<int, PerfilMob>
        {
            { 1, new PerfilMob("Aldeão Assustado", Arquetipos.HumanoComum, 40.0f, new float[] { -50f, -100f, 50f, 300f, 300f, -100f }, 5f, 30f, 2f, 5f, false) },
            { 2, new PerfilMob("Lobo Faminto", Arquetipos.AnimalAgressivo, 80.0f, new float[] { 250f, 400f, 0f, 0f, -50f, 0f }, 150f, 50f, 8f, 20f, false) },
            { 3, new PerfilMob("Guarda Veterano", Arquetipos.HumanoForte, 20.0f, new float[] { 100f, 50f, 150f, 0f, 50f, 150f }, 500f, 800f, 12f, 30f, false) },
            { 4, new PerfilMob("Bandido Oportunista", Arquetipos.Oportunista, 60.0f, new float[] { 150f, 100f, 0f, 200f, -50f, 0f }, 250f, 60f, 10f, 25f, false) },
            { 5, new PerfilMob("Espirito Vingativo", Arquetipos.DefensorVingativo, 30.0f, new float[] { 50f, 0f, 200f, 50f, 100f, 300f }, 450f, 150f, 5f, 15f, false) },
            { 6, new PerfilMob("Cervo de Prodigios", Arquetipos.AnimalIndefeso, 70.0f, new float[] { -200f, -200f, 0f, 400f, 200f, -100f }, 150f, 15f, 1f, 3f, true) },
            { 7, new PerfilMob("Urso Territorial", Arquetipos.AnimalReativo, 30.0f, new float[] { 50f, 100f, 50f, 0f, 300f, 0f }, 250f, 120f, 20f, 50f, false) },
            { 8, new PerfilMob("Cultista Frenetico", Arquetipos.Inimigo, 60.0f, new float[] { 300f, 150f, -50f, -100f, -200f, 200f }, 300f, 90f, 12f, 30f, false) }
        };
    }
}