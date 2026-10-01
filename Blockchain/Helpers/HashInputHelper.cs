using System.Text;

namespace Blockchain.Helpers;

public static class HashInputHelper
{
    public static byte[] GetBytes(string message)
    {
        byte[] input = Encoding.UTF8.GetBytes(message);
        Console.WriteLine(string.Join(" ", input));
        Console.WriteLine("UTF-8 baitai HEX formatu: " + string.Join(" ", input.Select(b => b.ToString("X2"))));

        for (int i = 0; i < input.Length; i++)
        {
            int hNumber = i % 8;
            Console.WriteLine($"Baitas {i} ({input[i]:X2}) → H{hNumber}");
        }

        return input;
    }
}
