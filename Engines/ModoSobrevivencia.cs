using System;
using System.Collections.Generic;
using Runage.Models;
using Runage.Utils;

namespace Runage.Engines;

public static class ModoSobrevivencia
{
    public static void Iniciar(float viesValor)
    {
        Console.Clear();
        Console.WriteLine("==================================================");
        Console.WriteLine(" MANOPLA DE PRODÍGIOS (MODO SOBREVIVÊNCIA) ");
        Console.WriteLine("==================================================\n");

        float[][]? pesosGlobais = DataHandler.CarregarPesosPlayer(viesValor, "pesos_Global.json");
        if (pesosGlobais == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERRO: Cérebro Global não encontrado! Execute a Forja Massiva [2] primeiro.");
            Console.ResetColor();
            Console.ReadLine();
            return;
        }

        int vidas = 3;
        int andarMaximo = 100;
        float playerMultiplicador = 1.0f; 
        var dicionarioMobs = ObterTodosOsMobs();
        Random rnd = new();

        Console.WriteLine($"Acessando matriz neural (Viés: {viesValor:F1})...\nO Desafio começou.\n");

        for (int andar = 1; andar <= andarMaximo; andar++)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== ANDAR {andar} ===");
            Console.ResetColor();

            int tensaoDoAndar = (andar % 10 == 0) ? 10 : andar % 10;
            float multiplicadorMob = 1.0f + (andar * 0.05f);
            
            if (tensaoDoAndar == 10) 
            {
                multiplicadorMob *= 1.20f; 
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("CUIDADO: Presença Colossal Detectada (Andar de Chefe).");
                Console.ResetColor();
            }

            for (int sala = 1; sala <= 5; sala++)
            {
                PerfilMob perfilBase = SortearNpc(tensaoDoAndar, dicionarioMobs, rnd);

                // Ações base 0-5 sempre disponíveis; Parry (7) desbloqueia no andar 5+,
                // Sobrecarga (6) desbloqueia no andar 10+
                List<int> acoesDesbloqueadas = new() { 0, 1, 2, 3, 4, 5 };
                if (andar >= 5) acoesDesbloqueadas.Add(7);
                if (andar >= 10) acoesDesbloqueadas.Add(6);
                int[] acoesPermitidas = acoesDesbloqueadas.ToArray();
                
                // Clona o mob para não poluir o dicionário base
                PerfilMob inimigo = new PerfilMob(perfilBase.Nome, perfilBase.Arquetipo, perfilBase.Temperatura, 
                                                  perfilBase.InstintosBase, perfilBase.RecompensaAbate, 
                                                  perfilBase.HpBase * multiplicadorMob, 
                                                  perfilBase.DanoLeve * multiplicadorMob, 
                                                  perfilBase.DanoPesado * multiplicadorMob);

                Console.Write($"Sala {sala}/5: {inimigo.Nome} -> ");

                Console.Write($"Sala {sala}/5: {inimigo.Nome} -> ");

                // Puxando o novo retorno de Tupla
                var resultado = ExecutarCombate(viesValor, pesosGlobais, inimigo, playerMultiplicador, acoesPermitidas);
                if (!resultado.sobreviveu)
                {
                    vidas--;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"MORTE! O {inimigo.Nome} foi letal. Vidas restantes: {vidas}");
                    Console.ResetColor();

                    if (vidas <= 0) 
                    {
                        Console.WriteLine($"\nFIM DE JOGO. O Player sucumbiu no Andar {andar}, Sala {sala}.");
                        Console.ReadLine();
                        return; 
                    }
                    sala--; // Repete a sala
                }
                else
                {
                    // Colore o console dinamicamente conforme a agressividade do desfecho
                    if (resultado.detalhe.Contains("Abatido"))
                        Console.ForegroundColor = ConsoleColor.DarkBlue; // Matou!
                    else if (resultado.detalhe.Contains("Exaustão"))
                        Console.ForegroundColor = ConsoleColor.DarkYellow; // Limite de turnos
                    else
                        Console.ForegroundColor = ConsoleColor.Green; // Fuga ou Paz

                    Console.WriteLine($"Vitória! ({resultado.detalhe})");
                    Console.ResetColor();
                    
                    playerMultiplicador += 0.04f; 
                }
            }
            Console.WriteLine($"Andar {andar} limpo! Status Base ampliado: {playerMultiplicador:F2}x\n");
        }
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nPARABÉNS! VOCÊ CONQUISTOU A MANOPLA DE PRODÍGIOS!");
        Console.ResetColor();
        Console.ReadLine();
    }

    private static (bool sobreviveu, string detalhe) ExecutarCombate(float viesValor, float[][] pesosGlobais, PerfilMob inimigo, float pMultiplicador, int[] acoesPermitidas)
    {
        QAgent player = new QAgent(15.0f, null);
        
        player.ImportarPesos(pesosGlobais);

        float[][]? pesosEspecificos = DataHandler.CarregarPesosPlayer(viesValor, $"pesos_{inimigo.Nome.Replace(" ", "_")}.json");
        if (pesosEspecificos != null) player.ImportarPesos(pesosEspecificos);

        player.DefinirExploracao(0.05f); 

        QAgent mob = new QAgent(inimigo.Temperatura, inimigo.InstintosBase);
        float[][]? pesosColmeia = DataHandler.CarregarPesosNPC(inimigo.Nome, viesValor);
        
        if (pesosColmeia != null) mob.ImportarPesos(pesosColmeia);
        mob.DefinirExploracao(0.05f);

        CombatEnvironment env = new CombatEnvironment(0, inimigo, viesValor);
        
        env.CombateMortal = true;
        // O construtor ja calculou HP e Multiplicador com base no vies.
        // pMultiplicador reflete o crescimento acumulado do player nos andares.
        env.PlayerMultiplicador *= pMultiplicador;
        env.PlayerHP            *= pMultiplicador;
        
        bool lutaAtiva = true;
        int turnosAtuais = 0;

        while (lutaAtiva && turnosAtuais < 1000)
        {
            turnosAtuais++;
            float[] estadoBase = env.GetFeatures();

            // Máscara ativada: Player só enxerga as ações que destravou
            int acaoPlayer = player.EscolherAcao(estadoBase, acoesPermitidas);
            int acaoMob = mob.EscolherAcao(estadoBase);

            float[] recompensas = env.ResolverTick(acaoPlayer, acaoMob);

            if (env.IsGameOver) lutaAtiva = false;
        }

        // NOVO: Avaliando como o combate terminou
        if (env.PlayerHP <= 0) 
            return (false, "Morto");
        
        if (env.MobHP <= 0) 
            return (true, "Inimigo Abatido ⚔️");
        
        if (turnosAtuais >= 1000) 
            return (true, "Sobreviveu por Exaustão de Tempo ⏳");
            
        return (true, "Resolução Pacífica ou Fuga 🕊️/🏃");
    }

    private static PerfilMob SortearNpc(int tensao, Dictionary<int, PerfilMob> dic, Random rnd)
    {
        List<int> idsPossiveis = new List<int>();

        if (tensao <= 3) idsPossiveis.AddRange(new[] { 1, 6 });       // Aldeão, Cervo
        else if (tensao <= 6) idsPossiveis.AddRange(new[] { 2, 4 });  // Lobo, Bandido
        else if (tensao <= 9) idsPossiveis.AddRange(new[] { 3, 7 });  // Guarda, Urso
        else idsPossiveis.AddRange(new[] { 5, 8 });                   // Chefe: Espírito, Cultista

        int idSorteado = idsPossiveis[rnd.Next(idsPossiveis.Count)];
        return dic[idSorteado];
    }

    private static Dictionary<int, PerfilMob> ObterTodosOsMobs()
    {
        return new Dictionary<int, PerfilMob>
        {
            // NOTA: os nomes aqui são usados tanto para exibição quanto para montar caminhos
            // de arquivo via DataHandler. O DataHandler normaliza os acentos internamente
            // antes de construir o path — veja NormalizarNome() em DataHandler.cs.
            { 1, new PerfilMob("Aldeão Assustado", Arquetipos.HumanoComum, 40.0f, new float[] { -50f, -100f, 50f, 300f, 300f, -100f }, 5f, 30f, 2f, 5f) },
            { 2, new PerfilMob("Lobo Faminto", Arquetipos.AnimalAgressivo, 80.0f, new float[] { 250f, 400f, 0f, 0f, -50f, 0f }, 150f, 50f, 8f, 20f) },
            { 3, new PerfilMob("Guarda Veterano", Arquetipos.HumanoForte, 20.0f, new float[] { 100f, 50f, 150f, 0f, 50f, 150f }, 500f, 800f, 15f, 35f) },
            { 4, new PerfilMob("Bandido Oportunista", Arquetipos.Oportunista, 60.0f, new float[] { 150f, 100f, 0f, 200f, -50f, 0f }, 250f, 60f, 10f, 25f) },
            { 5, new PerfilMob("Espírito Vingativo", Arquetipos.DefensorVingativo, 30.0f, new float[] { 50f, 0f, 200f, 50f, 100f, 300f }, 450f, 150f, 5f, 15f) },
            { 6, new PerfilMob("Cervo de Prodígios", Arquetipos.AnimalIndefeso, 70.0f, new float[] { -200f, -200f, 0f, 400f, 200f, -100f }, 150f, 15f, 1f, 3f) },
            { 7, new PerfilMob("Urso Territorial", Arquetipos.AnimalReativo, 30.0f, new float[] { 50f, 100f, 50f, 0f, 300f, 0f }, 250f, 120f, 20f, 50f) },
            { 8, new PerfilMob("Cultista Frenético", Arquetipos.Inimigo, 60.0f, new float[] { 300f, 150f, -50f, -100f, -200f, 200f }, 300f, 90f, 12f, 30f) }
        };
    }
}