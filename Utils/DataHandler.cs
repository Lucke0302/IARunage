using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Runage.Utils;

public static class DataHandler
{
    private static readonly string BaseDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
    private static readonly JsonSerializerOptions jsonOptions = new() 
    { 
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals 
    };

    // Normaliza o nome para uso em paths de arquivo: remove acentos e substitui
    // espaços por underscore. Isso garante que "Espírito Vingativo" (ModoSobrevivencia)
    // e "Espirito Vingativo" (ForjaMassiva) apontem para o mesmo diretório em disco,
    // evitando que o player jogue sem os pesos especializados por divergência de nome.
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

    public static void SalvarPesosPlayer(float vies, string nomeArquivo, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, nomeArquivo);
        File.WriteAllText(path, JsonSerializer.Serialize(pesos, jsonOptions));
    }

    public static void SalvarPesosNPC(string nomeNpc, float viesPlayer, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "NPC", NormalizarNome(nomeNpc), $"Vies_{viesPlayer:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "pesos_Colmeia.json");
        File.WriteAllText(path, JsonSerializer.Serialize(pesos, jsonOptions));
    }

    public static float[][]? CarregarPesosPlayer(float vies, string nomeArquivo)
    {
        string path = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}", nomeArquivo);
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<float[][]>(File.ReadAllText(path), jsonOptions);
    }

    public static float[][]? CarregarPesosNPC(string nomeNpc, float viesPlayer)
    {
        string path = Path.Combine(BaseDir, "NPC", NormalizarNome(nomeNpc), $"Vies_{viesPlayer:F1}", "pesos_Colmeia.json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<float[][]>(File.ReadAllText(path), jsonOptions);
    }
}