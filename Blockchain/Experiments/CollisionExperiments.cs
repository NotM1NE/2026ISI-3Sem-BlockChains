using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Blockchain.Services;

namespace Blockchain.Experiments;

public static class CollisionExperiments
{
    public static int Run()
    {
        const int seed = 2026;
        const int pairsPerLength = 100_000;

        const string alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        int[] lengths = [10, 100, 500, 1000];

        var random = new Random(seed);

        Directory.CreateDirectory("results");

        var summary = new List<string>
        {
            "length,pairs,total_inputs,unique_inputs," +
            "pair_collisions,all_collisions,collision_groups"
        };

        var examples = new List<string>
        {
            "length,hash,input_a,input_b"
        };

        bool foundCollision = false;

        foreach (int length in lengths)
        {
            Console.WriteLine(
                $"Kolizijų paieška: ilgis {length}, " +
                $"{pairsPerLength} porų..."
            );

            int pairCollisions = 0;
            int allCollisions = 0;

            var uniqueInputs = new HashSet<string>();
            var hashes = new Dictionary<string, string>();
            var collisionHashes = new HashSet<string>();

            for (int pair = 0; pair < pairsPerLength; pair++)
            {
                string inputA = RandomText(
                    random,
                    alphabet,
                    length
                );

                string inputB;

                do
                {
                    inputB = RandomText(
                        random,
                        alphabet,
                        length
                    );
                }
                while (inputA == inputB);

                string hashA = ComputeHex(inputA);
                string hashB = ComputeHex(inputB);

                if (hashA == hashB)
                {
                    pairCollisions++;
                }

                CheckInput(
                    inputA,
                    hashA,
                    length,
                    uniqueInputs,
                    hashes,
                    collisionHashes,
                    examples,
                    ref allCollisions
                );

                CheckInput(
                    inputB,
                    hashB,
                    length,
                    uniqueInputs,
                    hashes,
                    collisionHashes,
                    examples,
                    ref allCollisions
                );
            }

            foundCollision |= allCollisions > 0;

            summary.Add(
                $"{length},{pairsPerLength}," +
                $"{pairsPerLength * 2},{uniqueInputs.Count}," +
                $"{pairCollisions},{allCollisions}," +
                $"{collisionHashes.Count}"
            );

            Console.WriteLine(
                $"Ilgis {length}: porinių kolizijų " +
                $"{pairCollisions}, bendrų kolizijų " +
                $"{allCollisions}"
            );
        }

        File.WriteAllLines(
            "results/collision-summary.csv",
            summary
        );

        File.WriteAllLines(
            "results/collision-examples.csv",
            examples
        );

        File.WriteAllLines(
            "results/collision-metadata.txt",
            [
                $"Seed: {seed}",
                $"Alphabet: {alphabet}",
                $"Pairs per length: {pairsPerLength}",
                $"Lengths: {string.Join(",", lengths)}",
                $"Runtime: {RuntimeInformation.FrameworkDescription}",
                $"OS: {RuntimeInformation.OSDescription}",
                $"Architecture: {RuntimeInformation.ProcessArchitecture}",
                "Only distinct input values are compared",
                "Each pair contains two different input values"
            ]
        );

        Console.WriteLine(
            foundCollision
                ? "Aptikta bent viena kolizija."
                : "Kolizijų šiame bandyme neaptikta."
        );

        Console.WriteLine(
            "Rezultatai: results/collision-summary.csv"
        );

        RunStructuredTests();

        return 0;
    }

    private static void CheckInput(
        string input,
        string hash,
        int length,
        HashSet<string> uniqueInputs,
        Dictionary<string, string> hashes,
        HashSet<string> collisionHashes,
        List<string> examples,
        ref int allCollisions)
    {
        // Jeigu tokia pati įvestis jau buvo, jos antrą kartą
        // neskaičiuojame kaip naujo bandymo.
        if (!uniqueInputs.Add(input))
        {
            return;
        }

        if (hashes.TryGetValue(hash, out string? previousInput))
        {
            if (previousInput != input)
            {
                allCollisions++;
                collisionHashes.Add(hash);

                if (examples.Count <= 20)
                {
                    examples.Add(
                        $"{length},{hash}," +
                        $"\"{previousInput}\",\"{input}\""
                    );
                }
            }
        }
        else
        {
            hashes[hash] = input;
        }
    }

    private static string RandomText(
        Random random,
        string alphabet,
        int length)
    {
        char[] characters = new char[length];

        for (int i = 0; i < characters.Length; i++)
        {
            characters[i] =
                alphabet[random.Next(alphabet.Length)];
        }

        return new string(characters);
    }

    private static string ComputeHex(string text)
    {
        byte[] input = Encoding.UTF8.GetBytes(text);
        uint[] hash = InputDistributorService.Compute(input);

        return string.Concat(
            hash.Select(value => value.ToString("X8"))
        );
    }
    private static void RunStructuredTests()
    {
        string[] inputs =
        [
            "",
        "a",
        "b",

        // Tie patys simboliai, skirtinga tvarka.
        "ab",
        "ba",
        "abc",
        "acb",
        "bac",
        "bca",
        "cab",
        "cba",

        // Pasikartojantys simboliai ir šablonai.
        new string('a', 100),
        new string('a', 101),
        new string('b', 100),
        string.Concat(Enumerable.Repeat("ab", 50)),
        string.Concat(Enumerable.Repeat("ba", 50)),
        new string('a', 50) + new string('b', 50),
        new string('b', 50) + new string('a', 50),

        // Tarpai ir eilučių pabaigos.
        "Domas",
        " Domas",
        "Domas ",
        "Domas\n",
        "Domas\r\n"
        ];

        var hashes = new Dictionary<string, List<string>>();

        var results = new List<string>
    {
        "input_utf8_hex,bytes,hash"
    };

        var collisions = new List<string>
    {
        "hash,input_a_utf8_hex,input_b_utf8_hex"
    };

        int collisionPairs = 0;

        foreach (string input in inputs.Distinct())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            string inputHex = Convert.ToHexString(bytes);
            string hash = ComputeHex(input);

            results.Add($"{inputHex},{bytes.Length},{hash}");

            if (!hashes.TryGetValue(hash, out var previousInputs))
            {
                previousInputs = new List<string>();
                hashes.Add(hash, previousInputs);
            }

            foreach (string previous in previousInputs)
            {
                string previousHex = Convert.ToHexString(
                    Encoding.UTF8.GetBytes(previous)
                );

                collisions.Add($"{hash},{previousHex},{inputHex}");
                collisionPairs++;
            }

            previousInputs.Add(input);
        }

        File.WriteAllLines(
            "results/collision-structured-inputs.csv",
            results
        );

        File.WriteAllLines(
            "results/collision-structured-examples.csv",
            collisions
        );

        int collisionGroups = hashes.Values.Count(
            group => group.Count > 1
        );

        Console.WriteLine(
            $"\nStruktūruotos įvestys: {inputs.Distinct().Count()}, " +
            $"kolizinių porų: {collisionPairs}, " +
            $"kolizijų grupių: {collisionGroups}"
        );
    }
}