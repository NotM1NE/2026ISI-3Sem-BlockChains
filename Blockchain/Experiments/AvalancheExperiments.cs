using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Blockchain.Services;

namespace Blockchain.Experiments;

public static class AvalancheExperiments
{
    public static int Run()
    {
        const int seed = 2026;
        const int pairsPerLength = 25_000;

        const string alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        int[] lengths = [10, 100, 500, 1000];
        var random = new Random(seed);

        Directory.CreateDirectory("results");

        var measurements =
            new List<(int Length, int Bits, int Hex)>();

        using var raw = new StreamWriter(
            "results/avalanche-raw.csv"
        );

        raw.WriteLine(
            "length,pair,changed_position,old_char,new_char," +
            "hash_a,hash_b,changed_bits,bit_percent," +
            "changed_hex,hex_percent"
        );

        foreach (int length in lengths)
        {
            Console.WriteLine(
                $"Tikrinama: {length} simbolių, " +
                $"{pairsPerLength} porų..."
            );

            for (int pair = 0; pair < pairsPerLength; pair++)
            {
                // Sukuriame pirmąją įvestį.
                char[] original = new char[length];

                for (int i = 0; i < original.Length; i++)
                {
                    original[i] =
                        alphabet[random.Next(alphabet.Length)];
                }

                // Kopijoje pakeičiame tik vieną simbolį.
                char[] changed = (char[])original.Clone();

                int position = random.Next(length);
                char oldChar = changed[position];

                int oldIndex = alphabet.IndexOf(oldChar);

                // Poslinkis bent 1, todėl simbolis tikrai pasikeis.
                int offset = random.Next(1, alphabet.Length);

                char newChar = alphabet[
                    (oldIndex + offset) % alphabet.Length
                ];

                changed[position] = newChar;

                string hashA = ComputeHex(new string(original));
                string hashB = ComputeHex(new string(changed));

                // HEX paverčiame į tikruosius hash baitus.
                byte[] bytesA = Convert.FromHexString(hashA);
                byte[] bytesB = Convert.FromHexString(hashB);

                int changedBits = 0;

                for (int i = 0; i < bytesA.Length; i++)
                {
                    uint difference = (uint)(bytesA[i] ^ bytesB[i]);

                    changedBits += BitOperations.PopCount(difference);
                }

                int changedHex = 0;

                for (int i = 0; i < hashA.Length; i++)
                {
                    if (hashA[i] != hashB[i])
                    {
                        changedHex++;
                    }
                }

                double bitPercent = changedBits * 100.0 / 256;
                double hexPercent = changedHex * 100.0 / 64;

                measurements.Add((
                    length,
                    changedBits,
                    changedHex
                ));

                raw.WriteLine(
                    $"{length},{pair},{position},{oldChar},{newChar}," +
                    $"{hashA},{hashB},{changedBits},{Number(bitPercent)}," +
                    $"{changedHex},{Number(hexPercent)}"
                );
            }
        }

        var summary = new List<string>
        {
            "length,pairs,bit_min_percent,bit_max_percent," +
            "bit_avg_percent,hex_min_percent,hex_max_percent," +
            "hex_avg_percent"
        };

        var histogram = new List<string>
        {
            "length,changed_bits,bit_percent,count"
        };

        // 0 reiškia bendrą visų ilgių rezultatą.
        foreach (int length in lengths.Append(0))
        {
            var selected = measurements
                .Where(m => length == 0 || m.Length == length)
                .ToArray();

            string label = length == 0 ? "all" : length.ToString();

            double bitMin = selected.Min(m => m.Bits) * 100.0 / 256;
            double bitMax = selected.Max(m => m.Bits) * 100.0 / 256;
            double bitAvg = selected.Average(m => m.Bits) * 100.0 / 256;

            double hexMin = selected.Min(m => m.Hex) * 100.0 / 64;
            double hexMax = selected.Max(m => m.Hex) * 100.0 / 64;
            double hexAvg = selected.Average(m => m.Hex) * 100.0 / 64;

            summary.Add(
                $"{label},{selected.Length}," +
                $"{Number(bitMin)},{Number(bitMax)},{Number(bitAvg)}," +
                $"{Number(hexMin)},{Number(hexMax)},{Number(hexAvg)}"
            );

            int[] counts = new int[257];

            foreach (var measurement in selected)
            {
                counts[measurement.Bits]++;
            }

            for (int bits = 0; bits <= 256; bits++)
            {
                histogram.Add(
                    $"{label},{bits}," +
                    $"{Number(bits * 100.0 / 256)},{counts[bits]}"
                );
            }

            Console.WriteLine(
                $"{label}: porų {selected.Length}, " +
                $"bitų vidurkis {Number(bitAvg)} %, " +
                $"HEX vidurkis {Number(hexAvg)} %"
            );
        }

        File.WriteAllLines(
            "results/avalanche-summary.csv",
            summary
        );

        File.WriteAllLines(
            "results/avalanche-histogram.csv",
            histogram
        );

        File.WriteAllLines(
            "results/avalanche-metadata.txt",
            [
                $"Seed: {seed}",
                $"Alphabet: {alphabet}",
                $"Pairs per length: {pairsPerLength}",
                $"Lengths: {string.Join(",", lengths)}",
                $"Runtime: {RuntimeInformation.FrameworkDescription}",
                $"OS: {RuntimeInformation.OSDescription}",
                $"Architecture: {RuntimeInformation.ProcessArchitecture}",
                "Encoding: UTF-8",
                "Each pair differs by exactly one ASCII character",
                "Replacement character is selected uniformly from alternatives",
                "Input lengths in characters and bytes are equal"
            ]
        );

        Console.WriteLine(
            "\nRezultatai išsaugoti results/avalanche-*.csv"
        );

        return 0;
    }

    private static string ComputeHex(string text)
    {
        byte[] input = Encoding.UTF8.GetBytes(text);
        uint[] hash = InputDistributorService.Compute(input);

        return string.Concat(
            hash.Select(value => value.ToString("X8"))
        );
    }

    private static string Number(double value)
    {
        return value.ToString("F4", CultureInfo.InvariantCulture);
    }
}