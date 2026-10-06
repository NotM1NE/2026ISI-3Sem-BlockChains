using Blockchain.Experiments;
using Blockchain.Helpers;
using Blockchain.Services;

namespace Blockchain;

public class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--test")
            return CorrectnessExperiments.Run();   

        
        if (args.Length == 1 && args[0] == "--avalanche")
            return AvalancheExperiments.Run();


        if (args.Length > 1)
        {
            Console.Error.WriteLine("Naudojimas: dotnet run -- [failo kelias]");
            return 1;
        }

        if (args.Length == 1 && args[0] == "--collisions")
            return CollisionExperiments.Run();

        if (args.Length == 1 && args[0] == "--performance")
            return PerformanceExperiments.Run();

        byte[] input;

        if (args.Length == 1)
        {
            Console.WriteLine("Režimas: failas");

            try
            {
                input = File.ReadAllBytes(args[0]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Nepavyko perskaityti failo: {ex.Message}");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("Režimas: tekstas");
            Console.Write("Įveskite tekstą: ");
            string text = Console.ReadLine() ?? "";

            input = HashInputHelper.GetBytes(text);
        }

        var hash = InputDistributorService.Compute(input);
        Console.WriteLine(string.Concat(hash.Select(h => h.ToString("X8"))));

        return 0;
    }
}