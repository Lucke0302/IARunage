namespace Runage.Models;

public class RegistroSimulacao
{
    public int Simulacao { get; set; }
    public string Mob { get; set; } = string.Empty;
    public float ViesPlayer { get; set; }
    public float[][] PesosPlayer { get; set; } = [];
    public float[][] PesosMob { get; set; } = [];
}