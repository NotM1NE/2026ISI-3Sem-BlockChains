using System.Numerics;
using Blockchain.Common.Data;

namespace Blockchain.Services;

public static class InputDistributorService
{
    // Tie patys maišymo skaičiai kaip anksčiau, tik dešimtainiu užrašu.
    private const uint MixingOffset = 2_654_435_769u;
    private const uint FirstMultiplier = 2_246_822_507u;
    private const uint SecondMultiplier = 3_266_489_909u;

    public static uint[] Compute(byte[] input)
    {
        // 1. Pirmiausia atliekame tavo pradinį maišymą.
        uint[] h = ComputeV01(input);

        // 2. Į pirmas dvi vietas įmaišome įvesties ilgį baitais.
        ulong length = (ulong)input.LongLength;
        h[0] ^= (uint)length;
        h[1] ^= (uint)(length >> 32);

        // 3. Keturis kartus papildomai sumaišome visas aštuonias vietas.
        for (int round = 0; round < 4; round++)
        {
            for (int i = 0; i < h.Length; i++)
            {
                // Paimame kitą ir per keturias vietas nutolusį skaičių.
                int nextIndex = (i + 1) % h.Length;
                int oppositeIndex = (i + 4) % h.Length;
                uint next = h[nextIndex];
                uint opposite = h[oppositeIndex];

                // Dabartinę reikšmę susiejame su kitomis būsenos vietomis.
                uint value = h[i];
                value = unchecked(value + BitOperations.RotateLeft(next, 7));
                value = unchecked(value + MixingOffset + (uint)round);
                value ^= BitOperations.RotateLeft(opposite, 13);

                // Papildomas bitų maišymas: poslinkis, XOR ir daugyba.
                // Ši seka iš MurmurHash3 fmix32; šaltinis nurodytas README.
                value ^= value >> 16;
                value = unchecked(value * FirstMultiplier);
                value ^= value >> 13;
                value = unchecked(value * SecondMultiplier);
                value ^= value >> 16;

                // Išsaugome pakeistą reikšmę toje pačioje vietoje.
                h[i] = value;
            }
        }
        return h;
    }
    // Pradinis tavo algoritmas. v0.2 naudoja jį kaip pirmą etapą.
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
