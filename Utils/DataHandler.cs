using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Runage.Utils;

public static class DataHandler
{
    private static readonly string BaseDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
    private static readonly JsonSerializerOptions jsonOptions = new() 
    { 
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals 
    };

    public static void SalvarPesosPlayer(float vies, string nomeArquivo, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, nomeArquivo);
        File.WriteAllText(path, JsonSerializer.Serialize(pesos, jsonOptions));
    }

    public static void SalvarPesosNPC(string nomeNpc, float viesPlayer, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "NPC", nomeNpc.Replace(" ", "_"), $"Vies_{viesPlayer:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "pesos_Colmeia.json");
        File.WriteAllText(path, JsonSerializer.Serialize(pesos, jsonOptions));
    }
}