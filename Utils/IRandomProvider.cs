namespace Runage.Utils
{
    public interface IRandomProvider
    {
        double NextDouble();
        int Next(int maxValue);
        int Next(int minValue, int maxValue);
    }
}