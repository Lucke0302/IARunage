using Runage.Engines;
using Runage.Utils;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        ILogger logger = new ConsoleLogger();
        IProgressReporter? progress = null;

        Console.Clear();
        logger.Log("=====================================");
        logger.Log(" === PROJETO RUNAGE: HUB CENTRAL ===");
        logger.Log("=====================================\n");

        logger.Log("Selecione o Módulo de Operação:");
        logger.Log("[1] Arena de Testes (Laboratório de Balanceamento)");
        logger.Log("[2] Forja Massiva (Treinamento Real de IA)");
        logger.Log("[3] Manopla de Prodígios (Modo Sobrevivência - 100 Andares)");
        logger.Log("[4] Varredura Global (Testar todos os Mobs em sequência)");
        Console.Write("Escolha: ");
        int modulo = int.Parse(Console.ReadLine() ?? "1");

        Console.Write("\nDefina o Viés do Player (0.0 a 1.0): ");
        float viesValor = float.Parse((Console.ReadLine() ?? "0.5").Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
        viesValor = Math.Clamp(viesValor, 0f, 1f);

        switch (modulo)
        {
            case 1:
                Console.Clear();
                Console.Write("ID do Mob: ");
                int mobId = int.Parse(Console.ReadLine() ?? "1");
                Console.Write("Quantidade de Simulações (Ex: 5): ");
                int numSimulacoes = int.Parse(Console.ReadLine() ?? "5");
                Console.Write("Episódios por Simulação (Ex: 5000): ");
                int episodios = int.Parse(Console.ReadLine() ?? "5000");
                ArenaDeTestes.Iniciar(mobId, numSimulacoes, episodios, viesValor, logger, progress);
                Console.WriteLine("\nPressione ENTER para continuar...");
                Console.ReadLine();
                break;
            case 2:
                await ForjaMassiva.IniciarAsync(viesValor, logger, progress);
                Console.WriteLine("\nPressione ENTER para continuar...");
                Console.ReadLine();
                break;
            case 3:
                await ModoSobrevivencia.IniciarAsync(viesValor, logger, progress);
                Console.WriteLine("\nPressione ENTER para continuar...");
                Console.ReadLine();
                break;
            case 4:
                Console.Clear();
                Console.Write("Quantidade de Simulações (Ex: 5): ");
                int numSimulacoesGlobal = int.Parse(Console.ReadLine() ?? "5");
                Console.Write("Episódios por Simulação (Ex: 5000): ");
                int episodiosGlobal = int.Parse(Console.ReadLine() ?? "5000");
                ArenaDeTestes.ExecutarTesteGlobal(1, numSimulacoesGlobal, episodiosGlobal, viesValor, logger, progress);
                Console.WriteLine("\nPressione ENTER para continuar...");
                Console.ReadLine();
                break;
            default:
                logger.LogWarning("Módulo inválido.");
                break;
        }
    }
}