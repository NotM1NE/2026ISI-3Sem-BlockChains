using System.Text;
using Blockchain.Services;

namespace Blockchain.Experiments;

public static class CorrectnessExperiments
{
    public static int Run()
    {
        const int seed = 2026;
        const string alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        var random = new Random(seed);

        var cases = new List<(string Name, string Text)>
        {
            ("empty", ""),
            ("one_byte_a", "a"),
            ("one_byte_b", "b"),
            ("short_text", "Domas"),
            ("repeated_text", new string('a', 2048)),
            ("utf8_text", "Ąžuolas 🌳"),
            ("leading_space", " Domas"),
            ("trailing_space", "Domas "),
            ("with_newline", "Domas\n"),
            ("order_ab", "ab"),
            ("order_ba", "ba"),
            ("repeated_pattern",
                string.Concat(Enumerable.Repeat("ab", 1024)))
        };

        // Atsitiktinės įvestys ir jų kopijos su vienu pakeistu baitu.
        foreach (int length in new[] { 1024, 2048, 4096 })
        {
            char[] original = new char[length];

            for (int i = 0; i < original.Length; i++)
            {
                original[i] = alphabet[random.Next(alphabet.Length)];
            }

            cases.Add(($"random_{length}", new string(original)));

            int[] positions = [0, length / 2, length - 1];
            string[] positionNames = ["start", "middle", "end"];

            for (int i = 0; i < positions.Length; i++)
            {
                char[] changed = (char[])original.Clone();
                int position = positions[i];

                int oldIndex = alphabet.IndexOf(changed[position]);

                changed[position] =
                    alphabet[(oldIndex + 1) % alphabet.Length];

                cases.Add((
                    $"random_{length}_changed_{positionNames[i]}",
                    new string(changed)
                ));
            }
        }

        Directory.CreateDirectory("results");
        Directory.CreateDirectory("test-data/correctness");

        var rows = new List<string>
        {
            "test,bytes,hash,format_ok,repeat_ok,aba_ok,file_ok"
        };

        bool allPassed = true;

        foreach (var test in cases)
        {
            Console.WriteLine($"\nTESTAS: {test.Name}");

            byte[] input = Encoding.UTF8.GetBytes(test.Text);

            // Apskaičiuojame A hash.
            string first = ComputeHex(input);

            bool formatOk =
                first.Length == 64 &&
                first.All(c => "0123456789ABCDEF".Contains(c));

            // Dar kartą skaičiuojame A.
            string repeated = ComputeHex(input);
            bool repeatOk = first == repeated;

            // Skaičiuojame B, tada vėl A.
            ComputeHex(Encoding.UTF8.GetBytes("other input"));

            string afterOtherInput = ComputeHex(input);
            bool abaOk = first == afterOtherInput;

            // Tuos pačius baitus įrašome į failą ir perskaitome.
            string filePath = Path.Combine(
                "test-data",
                "correctness",
                test.Name + ".txt"
            );

            File.WriteAllBytes(filePath, input);

            byte[] fileInput = File.ReadAllBytes(filePath);
            string fileHash = ComputeHex(fileInput);

            bool fileOk =
                input.SequenceEqual(fileInput) &&
                first == fileHash;

            Console.WriteLine($"Baitų: {input.Length}");
            Console.WriteLine($"Hash: {first}");

            ShowCheck("64 HEX simboliai", formatOk);
            ShowCheck("Pakartota įvestis → tas pats hash", repeatOk);
            ShowCheck("A → B → A: A rezultatai sutampa", abaOk);
            ShowCheck("Tekstas ir failas sutampa", fileOk);

            allPassed &= formatOk && repeatOk && abaOk && fileOk;

            rows.Add(
                $"{test.Name},{input.Length},{first}," +
                $"{formatOk},{repeatOk},{abaOk},{fileOk}"
            );
        }

        string resultsPath = "results/correctness.csv";
        string baselinePath = "results/correctness-baseline.csv";

        File.WriteAllLines(resultsPath, rows);

        Console.WriteLine("\nPALYGINIMAS SU ANKSTESNIU PALEIDIMU:");

        if (File.Exists(baselinePath))
        {
            string[] previousRows = File.ReadAllLines(baselinePath);

            bool previousRunOk = previousRows.SequenceEqual(rows);

            ShowCheck(
                "Rezultatai sutampa su išsaugotu paleidimu",
                previousRunOk
            );

            allPassed &= previousRunOk;
        }
        else if (allPassed)
        {
            File.WriteAllLines(baselinePath, rows);

            Console.WriteLine(
                "Išsaugoti pirmojo paleidimo rezultatai. " +
                "Paleisk --test dar kartą, kad juos palygintum."
            );
        }
        else
        {
            Console.WriteLine(
                "Pirmojo paleidimo rezultatai palyginimui " +
                "neišsaugoti, nes yra nepraėjusių patikrinimų."
            );
        }

        Console.WriteLine(
            $"\nPatikrinta įvesčių: {cases.Count}"
        );

        Console.WriteLine(
            allPassed
                ? "Visi atlikti patikrinimai: PASS"
                : "Yra nepraėjusių patikrinimų: FAIL"
        );

        Console.WriteLine($"Rezultatai: {resultsPath}");

        return allPassed ? 0 : 1;
    }

    private static string ComputeHex(byte[] input)
    {
        uint[] hash = InputDistributorService.Compute(input);

        return string.Concat(
            hash.Select(value => value.ToString("X8"))
        );
    }

    private static void ShowCheck(string description, bool passed)
    {
        string status = passed ? "PASS" : "FAIL";

        Console.WriteLine($"  [{status}] {description}");
    }
}