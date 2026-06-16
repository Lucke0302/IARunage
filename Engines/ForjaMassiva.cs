using System;
using System.Collections.Generic;
using System.Linq;
using Runage.Models;
using Runage.Utils;

namespace Runage.Engines;

public static class ForjaMassiva
{
    public static void Iniciar(float viesValor)
    {
        Console.Clear();
        Console.WriteLine("==================================================");
        Console.WriteLine(" FORJA MASSIVA (TREINAMENTO REAL DE TRANSFERÊNCIA) ");
        Console.WriteLine("==================================================\n");

        var dicionarioMobs = ObterTodosOsMobs();
        float tempPlayer = 15.0f;
        float[] instintosPlayer = CalcularInstintos(viesValor);

        Console.WriteLine("[FASE 1] Forjando o Cérebro Global (Sobrevivência Básica)...");
        QAgent playerGlobal = new QAgent(tempPlayer, instintosPlayer);
        TreinarGlobal(playerGlobal, dicionarioMobs, viesValor, sims: 10, episodios: 6000);
        
        // Salva a baseline do Player
        float[][] pesosGlobais = playerGlobal.ExportarPesos();
        DataHandler.SalvarPesosPlayer(viesValor, "pesos_Global.json", pesosGlobais);
        Console.WriteLine(">> Cérebro Global salvo com sucesso!\n");

        Console.WriteLine("[FASE 2] Iniciando Especialização e Mente Coletiva NPC...\n");
        foreach (var kvp in dicionarioMobs)
        {
            PerfilMob mobAtual = kvp.Value;
            Console.WriteLine($"Especializando contra: {mobAtual.Nome}...");

            //  O Player carrega a experiência Global que acabou de aprender
            QAgent playerEspecializado = new QAgent(tempPlayer, instintosPlayer);
            playerEspecializado.ImportarPesos(pesosGlobais);
            playerEspecializado.DefinirExploracao(0.6f); 

            // O Mob nasce do zero para aprender a focar ESSE viés de Player específico
            QAgent mobColmeia = new QAgent(mobAtual.Temperatura, mobAtual.InstintosBase);

            TreinarEspecializado(playerEspecializado, mobColmeia, mobAtual, viesValor, sims: 10, episodios: 6000);

            // Salva os cérebros finais
            DataHandler.SalvarPesosPlayer(viesValor, $"pesos_{mobAtual.Nome.Replace(" ", "_")}.json", playerEspecializado.ExportarPesos());
            DataHandler.SalvarPesosNPC(mobAtual.Nome, viesValor, mobColmeia.ExportarPesos());
            
            Console.WriteLine($">> Conhecimento Tático e Colmeia salvos para {mobAtual.Nome}.\n");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("==================================================");
        Console.WriteLine(" FORJA CONCLUÍDA! O ecossistema está pronto para o jogo.");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.ReadLine();
    }

    private static void TreinarGlobal(QAgent player, Dictionary<int, PerfilMob> mobs, float vies, int sims, int episodios)
    {
        var listaMobs = mobs.Values.ToList();
        Random rnd = new();

        int totalVitorias = 0;
        int totalDerrotas = 0;
        int totalEmpates = 0;

        for (int sim = 1; sim <= sims; sim++)
        {
            int andarMin = 1;
            int andarMax = sim * 10;
            
            Console.WriteLine($"   -> Simulação {sim}/{sims} (Andares {andarMin}-{andarMax})...");

            for (int ep = 1; ep <= episodios; ep++)
            {
                PerfilMob mobSorteado = listaMobs[rnd.Next(listaMobs.Count)];
                QAgent mobDummy = new QAgent(mobSorteado.Temperatura, mobSorteado.InstintosBase);
                
                int andarSorteado = rnd.Next(andarMin, andarMax + 1);
                
                // Progressão do Player
                float pMultiplicador = 1.0f + (andarSorteado * 0.20f);
                
                // Progressão do Inimigo
                float mMultiplicador = 1.0f + (andarSorteado * 0.05f);

                // Clona o mob com status bufados simulando os andares profundos
                PerfilMob mobTreino = new PerfilMob(
                    mobSorteado.Nome, mobSorteado.Arquetipo, mobSorteado.Temperatura, 
                    mobSorteado.InstintosBase, mobSorteado.RecompensaAbate, 
                    mobSorteado.HpBase * mMultiplicador, 
                    mobSorteado.DanoLeve * mMultiplicador, 
                    mobSorteado.DanoPesado * mMultiplicador
                );

                int resultado = ExecutarEpisodio(player, mobDummy, mobTreino, vies, pMultiplicador, andarSorteado);
                
                if (resultado == 1) totalVitorias++;
                else if (resultado == -1) totalDerrotas++;
                else totalEmpates++;
            }
            float pisoAtual = 0.2f - ((sim - 1f) / (sims - 1f) * 0.15f);
            player.ForcarAmadurecimento(pisoAtual);
        }

        int totalLutas = sims * episodios;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[ESTATÍSTICAS DO TREINO GLOBAL]");
        Console.WriteLine($"Vitórias: {(totalVitorias / (float)totalLutas):P2} | Derrotas: {(totalDerrotas / (float)totalLutas):P2} | Empates/Fugas: {(totalEmpates / (float)totalLutas):P2}\n");
        Console.ResetColor();
    }

    private static void TreinarEspecializado(QAgent player, QAgent mob, PerfilMob perfil, float vies, int sims, int episodios)
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
                
                // Progressão do Player
                float pMultiplicador = 1.0f + (andarSorteado * 0.20f);
                
                // Progressão do Inimigo
                float mMultiplicador = 1.0f + (andarSorteado * 0.05f);

                // Clona o mob com status bufados simulando os andares profundos
                PerfilMob mobTreino = new PerfilMob(
                    perfil.Nome, perfil.Arquetipo, perfil.Temperatura, 
                    perfil.InstintosBase, perfil.RecompensaAbate, 
                    perfil.HpBase * mMultiplicador, 
                    perfil.DanoLeve * mMultiplicador, 
                    perfil.DanoPesado * mMultiplicador
                );
                
                int resultado = ExecutarEpisodio(player, mob, mobTreino, vies, pMultiplicador, andarSorteado);
                
                if (resultado == 1) totalVitorias++;
                else if (resultado == -1) totalDerrotas++;
                else totalEmpates++;
            }
            float pisoAtual = 0.2f - ((sim - 1f) / (sims - 1f) * 0.15f);
            player.ForcarAmadurecimento(pisoAtual);
            mob.ForcarAmadurecimento(pisoAtual);
        }

        int totalLutas = sims * episodios;
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"   -> Stats vs {perfil.Nome}: Vitórias: {(totalVitorias / (float)totalLutas):P2} | Derrotas: {(totalDerrotas / (float)totalLutas):P2} | Empates: {(totalEmpates / (float)totalLutas):P2}");
        Console.ResetColor();
    }

    // Adicionado o parâmetro 'int andar'
    private static int ExecutarEpisodio(QAgent player, QAgent mob, PerfilMob perfil, float vies, float pMultiplicador, int andar)
    {
        CombatEnvironment env = new CombatEnvironment(0, perfil, vies);
        env.CombateMortal = true; 
        
        env.PlayerMultiplicador *= pMultiplicador;
        env.PlayerHP            *= pMultiplicador;

        List<int> acoesDesbloqueadas = new() { 0, 1, 2, 3, 4, 5 };
        if (andar >= 5) acoesDesbloqueadas.Add(7); 
        if (andar >= 10) acoesDesbloqueadas.Add(6);
        int[] acoesPermitidas = acoesDesbloqueadas.ToArray();
        
        bool lutaAtiva = true;
        int turnosAtuais = 0;

        while (lutaAtiva && turnosAtuais < 500) 
        {
            turnosAtuais++;
            float[] estadoBase = env.GetFeatures();

            int acaoPlayer = player.EscolherAcao(estadoBase, acoesPermitidas);
            int acaoMob = mob.EscolherAcao(estadoBase);

            float[] recompensas = env.ResolverTick(acaoPlayer, acaoMob);
            float[] novoEstado = env.GetFeatures();
            
            player.Aprender(estadoBase, acaoPlayer, recompensas[0], novoEstado);
            mob.Aprender(estadoBase, acaoMob, recompensas[1], novoEstado);

            if (env.IsGameOver) lutaAtiva = false;
        }

        if (env.PlayerHP <= 0) return -1;
        if (env.MobHP <= 0) return 1;
        return 0;
    }

    private static float[] CalcularInstintos(float viesValor)
    {
        float[] instintosPacifico = { -200f, -300f, 150f, 200f, 400f, -200f };
        float[] instintosNeutro   = { 0f, 0f, 0f, 0f, 0f, 0f };
        float[] instintosCaotico  = { 300f, 400f, -50f, 0f, -300f, 500f };
        float[] playerInstintos = new float[6];

        for (int i = 0; i < 6; i++)
        {
            if (viesValor <= 0.5f)
            {
                float t = viesValor / 0.5f; 
                playerInstintos[i] = instintosPacifico[i] + (instintosNeutro[i] - instintosPacifico[i]) * t;
            }
            else
            {
                float t = (viesValor - 0.5f) / 0.5f; 
                playerInstintos[i] = instintosNeutro[i] + (instintosCaotico[i] - instintosNeutro[i]) * t;
            }
        }
        return playerInstintos;
    }

    private static Dictionary<int, PerfilMob> ObterTodosOsMobs()
    {
        return new Dictionary<int, PerfilMob>
        {
            { 1, new PerfilMob("Aldeão Assustado", Arquetipos.HumanoComum, 40.0f, new float[] { -50f, -100f, 50f, 300f, 300f, -100f }, 5f, 30f, 2f, 5f) },
            { 2, new PerfilMob("Lobo Faminto", Arquetipos.AnimalAgressivo, 80.0f, new float[] { 250f, 400f, 0f, 0f, -50f, 0f }, 150f, 50f, 8f, 20f) },
            { 3, new PerfilMob("Guarda Veterano", Arquetipos.HumanoForte, 20.0f, new float[] { 100f, 50f, 150f, 0f, 50f, 150f }, 500f, 800f, 15f, 35f) },
            { 4, new PerfilMob("Bandido Oportunista", Arquetipos.Oportunista, 60.0f, new float[] { 150f, 100f, 0f, 200f, -50f, 0f }, 250f, 60f, 10f, 25f) },
            { 5, new PerfilMob("Espirito Vingativo", Arquetipos.DefensorVingativo, 30.0f, new float[] { 50f, 0f, 200f, 50f, 100f, 300f }, 450f, 150f, 5f, 15f) },
            { 6, new PerfilMob("Cervo de Prodigios", Arquetipos.AnimalIndefeso, 70.0f, new float[] { -200f, -200f, 0f, 400f, 200f, -100f }, 150f, 15f, 1f, 3f) },
            { 7, new PerfilMob("Urso Territorial", Arquetipos.AnimalReativo, 30.0f, new float[] { 50f, 100f, 50f, 0f, 300f, 0f }, 250f, 120f, 20f, 50f) },
            { 8, new PerfilMob("Cultista Frenetico", Arquetipos.Inimigo, 60.0f, new float[] { 300f, 150f, -50f, -100f, -200f, 200f }, 300f, 90f, 12f, 30f) }
        };
    }
}