using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Blockchain.Services;

namespace Blockchain.Experiments;

public static class GuessingExperiments
{
    public static int Run()
    {
        Directory.CreateDirectory("results-v0.2");

        // Tik bandymo paruošimas žino pasirinktą PIN.
        const string targetInput = "1234";
        byte[] targetBytes = Encoding.UTF8.GetBytes(targetInput);

        // Druską sukuriame vieną kartą ir išsaugome,
        // kad kitoms algoritmo versijoms naudotume tą pačią.
        const string saltPath = "results-v0.2/guessing-salt.hex";

        byte[] salt;

        if (File.Exists(saltPath))
        {
            salt = Convert.FromHexString(File.ReadAllText(saltPath).Trim());
        }
        else
        {
            salt = RandomNumberGenerator.GetBytes(16);
            File.WriteAllText(saltPath, Convert.ToHexString(salt));
        }

        string plainTargetHash = ComputeHex(targetBytes);
        string saltedTargetHash = ComputeHex(Combine(targetBytes, salt));

        var rows = new List<string>
        {
            "mode,runs,attempts_per_run,total_attempts,avg_ms,min_ms,max_ms,matches,matching_candidates"
        };

        var rawRows = new List<string>
        {
            "mode,run,attempts,elapsed_ms,matches,matching_candidates"
        };

        // Paieška gauna hash ir druską, bet ne pasirinktą PIN.
        rows.Add(Search("without_salt", plainTargetHash, [], rawRows));
        rows.Add(Search("public_salt", saltedTargetHash, salt, rawRows));

        File.WriteAllLines("results-v0.2/guessing-summary.csv", rows);
        File.WriteAllLines("results-v0.2/guessing-raw.csv", rawRows);

        File.WriteAllLines(
            "results-v0.2/guessing-metadata.txt",
            [
                "Candidates: 0000–9999",
                "Measured runs per mode: 10",
                "Candidates per run: 10000; total per mode: 100000",
                "avg_ms is the mean time of one complete 10000-candidate search",
                "Warmup: 1000 hashes per mode, excluded from timing",
                "Candidate encoding: UTF-8, four ASCII digits",
                $"Target input used in preparation: {targetInput}",
                $"Target hash without salt: {plainTargetHash}",
                $"Target hash with salt: {saltedTargetHash}",
                $"Public salt HEX: {Convert.ToHexString(salt)}",
                $"Salt length: {salt.Length} bytes",
                "Construction: H(input bytes || raw salt bytes)",
                "Salt is fixed for all candidates of this target",
                "All candidates are checked; search does not stop at first match",
                "Timing includes candidate formatting, encoding and hashing",
                $"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}",
                $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}"
            ]
        );

        Console.WriteLine("\nRezultatai: results-v0.2/guessing-summary.csv ir results-v0.2/guessing-raw.csv");

        return 0;
    }

    private static string Search(
        string mode,
        string targetHash,
        byte[] salt,
        List<string> rawRows)
    {
        const int runCount = 10;

        // Apšilimas neįtraukiamas į dešimt matuojamų paleidimų.
        for (int i = 0; i < 1000; i++)
            ComputeHex(Combine(Encoding.UTF8.GetBytes("0000"), salt));

        var times = new List<double>();
        List<string>? firstMatches = null;

        for (int run = 1; run <= runCount; run++)
        {
            var matches = new List<string>();
            int attempts = 0;
            var stopwatch = Stopwatch.StartNew();

            for (int number = 0; number <= 9999; number++)
            {
                string candidate = number.ToString("D4", CultureInfo.InvariantCulture);
                byte[] candidateBytes = Encoding.UTF8.GetBytes(candidate);
                string candidateHash = ComputeHex(Combine(candidateBytes, salt));
                attempts++;

                if (candidateHash == targetHash)
                    matches.Add(candidate);
            }

            stopwatch.Stop();
            double milliseconds = stopwatch.Elapsed.TotalMilliseconds;
            times.Add(milliseconds);

            // Laikai gali skirtis, tačiau rasti kandidatai turi sutapti.
            if (firstMatches is null)
                firstMatches = matches;
            else if (!firstMatches.SequenceEqual(matches))
                throw new InvalidOperationException("Tarp paleidimų pasikeitė rasti kandidatai.");

            rawRows.Add($"{mode},{run},{attempts},{Number(milliseconds)}," +
                        $"{matches.Count},{string.Join(";", matches)}");
        }

        double average = times.Average();
        double minimum = times.Min();
        double maximum = times.Max();
        string candidates = string.Join(";", firstMatches!);

        Console.WriteLine(
            $"\nRežimas: {mode}\n" +
            $"Matuotų paleidimų: {runCount}\n" +
            "Bandymų viename paleidime: 10000\n" +
            $"Bandymų iš viso: {runCount * 10000}\n" +
            $"Vidutinis vieno paleidimo laikas: {Number(average)} ms\n" +
            $"Mažiausias laikas: {Number(minimum)} ms\n" +
            $"Didžiausias laikas: {Number(maximum)} ms\n" +
            $"Sutampantys kandidatai kiekviename paleidime: {candidates}"
        );

        return $"{mode},{runCount},10000,{runCount * 10000}," +
               $"{Number(average)},{Number(minimum)},{Number(maximum)}," +
               $"{firstMatches!.Count},{candidates}";
    }

    private static string Number(double value) =>
        value.ToString("F6", CultureInfo.InvariantCulture);

    private static byte[] Combine(byte[] input, byte[] salt)
    {
        byte[] combined = new byte[input.Length + salt.Length];

        input.CopyTo(combined, 0);
        salt.CopyTo(combined, input.Length);

        return combined;
    }

    private static string ComputeHex(byte[] input)
    {
        uint[] hash = InputDistributorService.Compute(input);

        return string.Concat(
            hash.Select(value => value.ToString("X8"))
        );
    }
}