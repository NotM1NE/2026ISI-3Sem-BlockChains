using System.Numerics;
using Blockchain.Common.Data;

namespace Blockchain.Services;

public static class InputDistributorService
{
    public static uint[] Compute(byte[] input)
    {
        var h = HashInitialState.Create();
        int start = 0;

        for (int i = 0; i < input.Length; i++)
        {
            // Console.WriteLine($"------[{i}]-----");

            // for (int j = 0; j < h.Length; j++)
            //     Console.WriteLine($"H[{j}] = {h[j]}");

            uint mixed = unchecked(h[start] + input[i]);
            mixed = unchecked(mixed * 7u);
            h[start] = BitOperations.RotateLeft(mixed, 5);

            int previous = start;
            //avalanche effect - maziausiai 50%
            for (int step = 1; step < h.Length; step++)
            {
                int current = previous + 1;

                if (current == h.Length)
                    current = 0;

                uint mixedWithPrevious = unchecked(h[current] + h[previous] * 3u);

                h[current] = BitOperations.RotateLeft(mixedWithPrevious, 11);
                previous = current;
            }

            start++;

            if (start == h.Length)
                start = 0;
        }

        return h;
    }
}
