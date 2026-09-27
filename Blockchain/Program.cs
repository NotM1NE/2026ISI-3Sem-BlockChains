using Blockchain.Helpers;
using Blockchain.Services;

namespace Blockchain;

public class Program
{
    static void Main(string[] args)
    {
        Console.Write("Iveskite teksta: ");
        string text = Console.ReadLine() ?? "";

        var input = HashInputHelper.GetBytes(text);
        Console.WriteLine(Convert.ToHexString(input));

        Console.WriteLine("---HASH Avalanche----");
        
        var avalanche = InputDistributorService.Compute(input);
        Console.WriteLine(string.Join(" ", avalanche));
    }
}