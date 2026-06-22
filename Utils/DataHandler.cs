using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
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
    private static readonly ConcurrentDictionary<string, float[][]> _cachePesos = new();
    private static readonly byte[] HmacKey = Encoding.UTF8.GetBytes("?wOu8}uKa?8b7pD9");

    public static void LimparCache() { _cachePesos.Clear(); }

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

    private static string CalcularHash(string conteudo)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(conteudo));
        return Convert.ToHexStringLower(bytes);
    }

    private static byte[] CalcularHMAC(byte[] dados)
    {
        using var hmac = new HMACSHA256(HmacKey);
        return hmac.ComputeHash(dados);
    }

    private const int BIN_HEADER_VERSION = 1;

    public static async Task SalvarPesosBinario(string filePath, float[][] pesos)
    {
        string dir = Path.GetDirectoryName(filePath)!;
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        int rows = pesos.Length;
        int cols = rows > 0 ? pesos[0].Length : 0;

        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
        {
            writer.Write(BIN_HEADER_VERSION);
            writer.Write(rows);
            writer.Write(cols);

            for (int i = 0; i < rows; i++)
            {
                var row = pesos[i];
                for (int j = 0; j < cols; j++)
                {
                    writer.Write(row[j]);
                }
            }
        }

        byte[] dataBytes = ms.ToArray();
        byte[] hash = CalcularHMAC(dataBytes);

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fileStream, Encoding.UTF8, false))
        {
            writer.Write(dataBytes);
            writer.Write(hash);
        }

        // Gera .json equivalente para debug
        string jsonPath = Path.ChangeExtension(filePath, ".json");
        string json = JsonSerializer.Serialize(pesos, jsonOptions);
        await File.WriteAllTextAsync(jsonPath, json);

        string hashJsonPath = jsonPath + ".hash";
        await File.WriteAllTextAsync(hashJsonPath, Convert.ToHexStringLower(hash));
    }

    public static async Task<float[][]?> CarregarPesosBinario(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            int expectedHashLen = 32; // HMACSHA256 output length
            int dataLen = fileBytes.Length - expectedHashLen;

            if (dataLen <= 0) return null;

            // Extrai e valida hash
            byte[] hashSalvo = new byte[expectedHashLen];
            Array.Copy(fileBytes, dataLen, hashSalvo, 0, expectedHashLen);
            byte[] dataBytes = new byte[dataLen];
            Array.Copy(fileBytes, dataBytes, dataLen);
            byte[] hashCalculado = CalcularHMAC(dataBytes);
            if (!CryptographicOperations.FixedTimeEquals(hashSalvo, hashCalculado))
                return null;

            // Lê cabeçalho e dados
            using var ms = new MemoryStream(dataBytes);
            using var reader = new BinaryReader(ms, Encoding.UTF8);

            int version = reader.ReadInt32();
            if (version != BIN_HEADER_VERSION)
                return null;

            int rows = reader.ReadInt32();
            int cols = reader.ReadInt32();

            if (rows <= 0 || cols <= 0)
                return null;

            var pesos = new float[rows][];
            for (int i = 0; i < rows; i++)
            {
                var row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = reader.ReadSingle();
                }
                pesos[i] = row;
            }

            return pesos;
        }
        catch (EndOfStreamException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static async Task SalvarPesosPlayerAsync(float vies, string nomeArquivo, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}");
        Directory.CreateDirectory(dir);
        string nomeBin = Path.ChangeExtension(nomeArquivo, ".bin");
        string path = Path.Combine(dir, nomeBin);
        await SalvarPesosBinario(path, pesos);
    }

    public static async Task SalvarPesosNPCAsync(string nomeNpc, float viesPlayer, float[][] pesos)
    {
        string dir = Path.Combine(BaseDir, "NPC", NormalizarNome(nomeNpc), $"Vies_{viesPlayer:F1}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "pesos_Colmeia.bin");
        await SalvarPesosBinario(path, pesos);
    }

    public static async Task<float[][]?> CarregarPesosPlayerAsync(float vies, string nomeArquivo)
    {
        string dir = Path.Combine(BaseDir, "Player", $"Vies_{vies:F1}");
        string cacheKeyJson = Path.Combine(dir, nomeArquivo);
        string cacheKeyBin = Path.Combine(dir, Path.ChangeExtension(nomeArquivo, ".bin"));

        // Tenta cache primeiro
        if (_cachePesos.TryGetValue(cacheKeyBin, out var cachedBin)) return cachedBin;
        if (_cachePesos.TryGetValue(cacheKeyJson, out var cachedJson)) return cachedJson;

        // Tenta binário primeiro
        string binPath = Path.Combine(dir, Path.ChangeExtension(nomeArquivo, ".bin"));
        if (File.Exists(binPath))
        {
            var dados = await CarregarPesosBinario(binPath);
            if (dados != null)
            {
                _cachePesos[cacheKeyBin] = dados;
                return dados;
            }
        }

        // Fallback para JSON
        string jsonPath = Path.Combine(dir, nomeArquivo);
        if (!File.Exists(jsonPath)) return null;
        try
        {
            string json = await File.ReadAllTextAsync(jsonPath);

            string hashPath = jsonPath + ".hash";
            if (File.Exists(hashPath))
            {
                string hashEsperado = await File.ReadAllTextAsync(hashPath);
                string hashReal = CalcularHash(json);
                if (!string.Equals(hashEsperado, hashReal, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            var dados = JsonSerializer.Deserialize<float[][]>(json, jsonOptions);
            if (dados != null) _cachePesos[cacheKeyJson] = dados;
            return dados;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static async Task<float[][]?> CarregarPesosNPCAsync(string nomeNpc, float viesPlayer)
    {
        string dir = Path.Combine(BaseDir, "NPC", NormalizarNome(nomeNpc), $"Vies_{viesPlayer:F1}");
        string cacheKeyBin = Path.Combine(dir, "pesos_Colmeia.bin");
        string cacheKeyJson = Path.Combine(dir, "pesos_Colmeia.json");

        // Tenta cache primeiro
        if (_cachePesos.TryGetValue(cacheKeyBin, out var cachedBin)) return cachedBin;
        if (_cachePesos.TryGetValue(cacheKeyJson, out var cachedJson)) return cachedJson;

        // Tenta binário primeiro
        string binPath = Path.Combine(dir, "pesos_Colmeia.bin");
        if (File.Exists(binPath))
        {
            var dados = await CarregarPesosBinario(binPath);
            if (dados != null)
            {
                _cachePesos[cacheKeyBin] = dados;
                return dados;
            }
        }

        // Fallback para JSON
        string jsonPath = Path.Combine(dir, "pesos_Colmeia.json");
        if (!File.Exists(jsonPath)) return null;
        try
        {
            string json = await File.ReadAllTextAsync(jsonPath);

            string hashPath = jsonPath + ".hash";
            if (File.Exists(hashPath))
            {
                string hashEsperado = await File.ReadAllTextAsync(hashPath);
                string hashReal = CalcularHash(json);
                if (!string.Equals(hashEsperado, hashReal, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            var dados = JsonSerializer.Deserialize<float[][]>(json, jsonOptions);
            if (dados != null) _cachePesos[cacheKeyJson] = dados;
            return dados;
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