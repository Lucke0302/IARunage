using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Threading.Tasks;

namespace Runage.Utils;

public static class DataHandler
{
    private static readonly string BaseDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
    private static readonly JsonSerializerOptions jsonOptions = new() 
    { 
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals 
    };

    private static string NormalizarNome(string nome)
    {
        string normalizado = nome.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace(" ", "_");
    }

    // --- VERSÕES ASSÍNCRONAS (recomendadas) ---

    public static async Task SalvarPesosPlayerAsync(float vies, string nomeArquivo, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, nomeArquivo);
        string json = JsonSerializer.Serialize(pesos, jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task SalvarPesosNPCAsync(string nomeNpc, float viesPlayer, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "NPC", NormalizarNome(nomeNpc), $"Vies_{viesPlayer:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "pesos_Colmeia.json");
        string json = JsonSerializer.Serialize(pesos, jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task<float[][]?> CarregarPesosPlayerAsync(float vies, string nomeArquivo)
    {
        string path = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}", nomeArquivo);
        if (!File.Exists(path)) return null;
        try
        {
            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<float[][]>(json, jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static async Task<float[][]?> CarregarPesosNPCAsync(string nomeNpc, float viesPlayer)
    {
        string path = Path.Combine(BaseDir, "NPC", NormalizarNome(nomeNpc), $"Vies_{viesPlayer:F1}", "pesos_Colmeia.json");
        if (!File.Exists(path)) return null;
        try
        {
            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<float[][]>(json, jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // --- VERSÕES SÍNCRONAS (obsoletas, mantidas para compatibilidade) ---

    [Obsolete("Use SalvarPesosPlayerAsync")]
    public static void SalvarPesosPlayer(float vies, string nomeArquivo, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, nomeArquivo);
        File.WriteAllText(path, JsonSerializer.Serialize(pesos, jsonOptions));
    }

    [Obsolete("Use SalvarPesosNPCAsync")]
    public static void SalvarPesosNPC(string nomeNpc, float viesPlayer, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "NPC", NormalizarNome(nomeNpc), $"Vies_{viesPlayer:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "pesos_Colmeia.json");
        File.WriteAllText(path, JsonSerializer.Serialize(pesos, jsonOptions));
    }

    [Obsolete("Use CarregarPesosPlayerAsync")]
    public static float[][]? CarregarPesosPlayer(float vies, string nomeArquivo)
    {
        string path = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}", nomeArquivo);
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<float[][]>(File.ReadAllText(path), jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [Obsolete("Use CarregarPesosNPCAsync")]
    public static float[][]? CarregarPesosNPC(string nomeNpc, float viesPlayer)
    {
        string path = Path.Combine(BaseDir, "NPC", NormalizarNome(nomeNpc), $"Vies_{viesPlayer:F1}", "pesos_Colmeia.json");
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<float[][]>(File.ReadAllText(path), jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}