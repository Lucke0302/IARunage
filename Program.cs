using System;
using Runage.Engines;

Console.Clear();
Console.WriteLine("=====================================");
Console.WriteLine(" === PROJETO RUNAGE: HUB CENTRAL ===");
Console.WriteLine("=====================================\n");

Console.WriteLine("Selecione o Módulo de Operação:");
Console.WriteLine("[1] Arena de Testes (Laboratório de Balanceamento)");
Console.WriteLine("[2] Forja Massiva (Treinamento Real de IA)");
Console.WriteLine("[3] Manopla de Prodígios (Modo Sobrevivência - 100 Andares)");
Console.WriteLine("[4] Varredura Global (Testar todos os Mobs em sequência)");
Console.Write("Escolha: ");
int modulo = int.Parse(Console.ReadLine() ?? "1");

Console.Write("\nDefina o Viés do Player (0.0 a 1.0): ");
float viesValor = float.Parse((Console.ReadLine() ?? "0.5").Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
viesValor = Math.Clamp(viesValor, 0f, 1f);

switch (modulo)
{
    case 1:
        ArenaDeTestes.Iniciar(viesValor);
        break;
    case 2:
        ForjaMassiva.Iniciar(viesValor);
        break;
    case 3:
        ModoSobrevivencia.Iniciar(viesValor);
        break;
    case 4:
        ArenaDeTestes.ExecutarTesteGlobal(viesValor);
        break;
    default:
        Console.WriteLine("Módulo inválido.");
        break;
}