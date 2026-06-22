using System;
using System.Collections.Generic;
using Runage.Models;
using Runage.Utils;

namespace Runage.Engines;

public static class ArenaDeTestes
{
    public static void Iniciar(float viesValor, ILogger logger, IProgressReporter? progress = null)
    {
        Console.Clear();
        logger.Log("==================================================");
        logger.Log(" ARENA DE TESTES (LABORATÓRIO DE BALANCEAMENTO) ");
        logger.Log("==================================================\n");

        // O laboratório tem o próprio dicionário para alterar atributos sem medo
        var dicionarioMobs = ObterMobsLaboratorio();

        logger.Log("Escolha o Mob para testar contra o Player:");
        foreach (var kvp in dicionarioMobs)
        {
            logger.Log($"[{kvp.Key}] {kvp.Value.Nome} (Arquétipo: {kvp.Value.Arquetipo.Nome})");
        }
        
        Console.Write("\nID do Mob: ");
        int mobId = int.Parse(Console.ReadLine() ?? "1");

        Console.Write("Quantidade de Simulações (Ex: 5): ");
        int numSimulacoes = int.Parse(Console.ReadLine() ?? "5");

        Console.Write("Episódios por Simulação (Ex: 5000): ");
        int episodios = int.Parse(Console.ReadLine() ?? "5000");

        ExecutarTreinoIsolado(dicionarioMobs[mobId], numSimulacoes, episodios, viesValor, logger);
    }

    private static void ExecutarTreinoIsolado(PerfilMob mobEscolhido, int numSimulacoes, int episodiosPorSimulacao, float viesValorParam, ILogger logger)
    {
        Console.Clear();
        logger.Log("==================================================");
        logger.Log($" INICIANDO TESTE ISOLADO: PLAYER VS {mobEscolhido.Nome.ToUpper()}");
        logger.Log("==================================================\n");

        // Temperatura fixa corrigida para evitar que o Caótico sofra
        float tempPlayer = 15.0f;
        float[] instintosPlayerParam = InstinctHelper.CalcularInstintos(viesValorParam);

        // Agentes instanciados completamente zerados
        QAgent player = new QAgent(tempPlayer, instintosPlayerParam); 
        QAgent mob = new QAgent(mobEscolhido.Temperatura, mobEscolhido.InstintosBase);

        int maxTurnosPorLuta = 100;

        for (int sim = 1; sim <= numSimulacoes; sim++)
        {
            int playerMortes = 0;
            int mobMortes = 0;
            int empatesPacificos = 0;
            float auraFinalPlayer = 0f;

            for (int ep = 1; ep <= episodiosPorSimulacao; ep++)
            {
                CombatEnvironment env = new CombatEnvironment(mobEscolhido, viesValorParam);
                bool lutaAtiva = true;
                int turnosAtuais = 0;

                                while (lutaAtiva && turnosAtuais < maxTurnosPorLuta)
                {
                    turnosAtuais++;
                    FeatureVector estado = env.GetFeatures();

                    int acaoPlayer = player.EscolherAcao(estado);
                    int acaoMob = mob.EscolherAcao(estado);

                    Reward recompensas = env.ResolverTick(acaoPlayer, acaoMob);
                    FeatureVector novoEstado = env.GetFeatures();

                    player.Aprender(estado, acaoPlayer, recompensas.Player, novoEstado);
                    mob.Aprender(estado, acaoMob, recompensas.Mob, novoEstado);

                    if (env.IsGameOver)
                    {
                        lutaAtiva = false;
                        auraFinalPlayer = env.AuraPlayer;

                        if (env.PlayerHP <= 0) playerMortes++;
                        if (env.MobHP <= 0) mobMortes++;
                        if (env.PlayerHP > 0 && env.MobHP > 0) empatesPacificos++;
                    }
                }

                // Estouro de limite de turnos conta como empate
                if (!env.IsGameOver && turnosAtuais >= maxTurnosPorLuta)
                {
                    auraFinalPlayer = env.AuraPlayer;
                    empatesPacificos++;
                }
            }

            logger.LogInfo($"=== FIM DA SIMULAÇÃO {sim} ===");
            logger.Log($"Mortes do Player: {playerMortes}");
            logger.Log($"Mortes do Mob:    {mobMortes}");
            logger.Log($"Empates Pacíficos:{empatesPacificos}");
            
            if (auraFinalPlayer >= 20)
                logger.LogSuccess($"Aura Final Player: {auraFinalPlayer:F1}\n");
            else if (auraFinalPlayer <= -20)
                logger.LogError($"Aura Final Player: {auraFinalPlayer:F1}\n");
            else
                logger.Log($"Aura Final Player: {auraFinalPlayer:F1}\n");

            // Decaimento de exploração contínuo
            float progresso = (numSimulacoes > 1) ? (float)(sim - 1) / (numSimulacoes - 1) : 1f;
            float pisoAtual = 0.2f - (progresso * 0.15f);

            player.ForcarAmadurecimento(pisoAtual);
            mob.ForcarAmadurecimento(pisoAtual);
        }

        logger.LogWarning("\n[ARENA] Teste concluído! Nenhum peso foi salvo no disco. Este é apenas um ambiente de laboratório.");
        Console.WriteLine("\nPressione ENTER para voltar ao Menu Principal...");
        Console.ReadLine();
    }

    public static void ExecutarTesteGlobal(float viesValor, ILogger logger, IProgressReporter? progress = null)
    {
        var dicionarioMobs = ObterMobsLaboratorio();
        
        logger.Log("\n=== INICIANDO VARREDURA GLOBAL DE BALANCEAMENTO ===");
        logger.Log("Cada mob passará por 5 simulações de 5000 episódios.\n");

        int mobIndex = 0;
        int totalMobs = dicionarioMobs.Count;
        foreach (var mob in dicionarioMobs.Values)
        {
            logger.LogWarning($"\n>>> TESTANDO MOB: {mob.Nome.ToUpper()} <<<");
            
            // Chamada direta para o motor de treino com os parâmetros fixos
            ExecutarTreinoIsolado(mob, 5, 5000, viesValor, logger);

            mobIndex++;
            progress?.ReportProgress((float)mobIndex / totalMobs * 100f, $"Testando {mob.Nome}...");
        }

        logger.Log("\nVarredura Global Concluída!");
        Console.ReadLine();
    }

    private static Dictionary<int, PerfilMob> ObterMobsLaboratorio()
    {
        // Centralizamos os mobs aqui. Fica fácil vir no laboratório, buffar o HP do Urso 
        // para 1000f só para testar e não quebrar o jogo principal.
        return new Dictionary<int, PerfilMob>
        {
            { 1, new PerfilMob("Aldeão Assustado", Arquetipos.HumanoComum, 40.0f, 
                new float[] { -50f, -100f, 50f, 300f, 300f, -100f }, 5f, 30f, 2f, 5f, false) },
            { 2, new PerfilMob("Lobo Faminto", Arquetipos.AnimalAgressivo, 80.0f, 
                new float[] { 250f, 400f, 0f, 0f, -50f, 0f }, 150f, 50f, 8f, 20f, false) },
            { 3, new PerfilMob("Guarda Veterano", Arquetipos.HumanoForte, 20.0f, 
                new float[] { 100f, 50f, 150f, 0f, 50f, 150f }, 500f, 800f, 12f, 30f, false) },
            { 4, new PerfilMob("Bandido Oportunista", Arquetipos.Oportunista, 60.0f, 
                new float[] { 150f, 100f, 0f, 200f, -50f, 0f }, 250f, 60f, 10f, 25f, false) },
            { 5, new PerfilMob("Espírito Vingativo", Arquetipos.DefensorVingativo, 30.0f, 
                new float[] { 50f, 0f, 200f, 50f, 100f, 300f }, 450f, 150f, 5f, 15f, false) },
            { 6, new PerfilMob("Cervo de Prodígios", Arquetipos.AnimalIndefeso, 70.0f, 
                new float[] { -200f, -200f, 0f, 400f, 200f, -100f }, 150f, 15f, 1f, 3f, true) }, // <-- Sturdy ativado!
            { 7, new PerfilMob("Urso Territorial", Arquetipos.AnimalReativo, 30.0f, 
                new float[] { 50f, 100f, 50f, 0f, 300f, 0f }, 250f, 120f, 20f, 50f, false) },
            { 8, new PerfilMob("Cultista Frenético", Arquetipos.Inimigo, 60.0f, 
                new float[] { 300f, 150f, -50f, -100f, -200f, 200f }, 300f, 90f, 12f, 30f, false) }
        };
    }
}