using System.Numerics;
using Blockchain.Common.Data;

namespace Blockchain.Services;

public static class InputDistributorService
{
    // v0.1 is kept unchanged so both algorithms use exactly the same experiments.

    public static uint[] Compute(byte[] input)
    {
        uint[] h = ComputeV01(input);

        // Include all 64 bits of the byte length, including for an empty input.
        ulong length = (ulong)input.LongLength;
        h[0] ^= (uint)length;
        h[1] ^= (uint)(length >> 32);

        // Four complete passes allow even the last input byte to spread further.
        for (int round = 0; round < 4; round++)
        {
            for (int i = 0; i < h.Length; i++)
            {
                uint next = h[(i + 1) % h.Length];
                uint opposite = h[(i + 4) % h.Length];
                uint x = unchecked(h[i] + BitOperations.RotateLeft(next, 7)
                    + 0x9E3779B9u + (uint)round);
                x ^= BitOperations.RotateLeft(opposite, 13);
                // MurmurHash3 fmix32 mixing block (Austin Appleby, public domain); see README source.
                x ^= x >> 16;
                x = unchecked(x * 0x85EBCA6Bu);
                x ^= x >> 13;
                x = unchecked(x * 0xC2B2AE35u);
                h[i] = x ^ (x >> 16);
            }
        }
        return h;
    }
    public static uint[] ComputeV01(byte[] input)
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
            // Perduodame atnaujinto žodžio įtaką kitiems būsenos žodžiams.
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
