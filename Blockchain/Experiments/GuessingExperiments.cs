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
        Directory.CreateDirectory("results");

        // Tik bandymo paruošimas žino pasirinktą PIN.
        const string targetInput = "2026";
        byte[] targetBytes = Encoding.UTF8.GetBytes(targetInput);

        // Druską sukuriame vieną kartą ir išsaugome,
        // kad kitoms algoritmo versijoms naudotume tą pačią.
        const string saltPath = "results/guessing-salt.hex";

        byte[] salt;

        if (File.Exists(saltPath))
        {
            salt = Convert.FromHexString(
                File.ReadAllText(saltPath).Trim()
            );
        }
        else
        {
            salt = RandomNumberGenerator.GetBytes(16);
            File.WriteAllText(
                saltPath,
                Convert.ToHexString(salt)
            );
        }

        string plainTargetHash = ComputeHex(targetBytes);
        string saltedTargetHash = ComputeHex(
            Combine(targetBytes, salt)
        );

        var rows = new List<string>
        {
            "mode,attempts,elapsed_ms,matches,matching_candidates"
        };

        // Paieška gauna hash ir druską, bet ne pasirinktą PIN.
        rows.Add(Search("without_salt", plainTargetHash, []));
        rows.Add(Search("public_salt", saltedTargetHash, salt));

        File.WriteAllLines("results/guessing-summary.csv", rows);

        File.WriteAllLines(
            "results/guessing-metadata.txt",
            [
                "Candidates: 0000–9999",
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

        Console.WriteLine(
            "\nRezultatai: results/guessing-summary.csv"
        );

        return 0;
    }

    private static string Search(
        string mode,
        string targetHash,
        byte[] salt)
    {
        // Apšilimas prieš matavimą.
        for (int i = 0; i < 1000; i++)
        {
            ComputeHex(
                Combine(Encoding.UTF8.GetBytes("0000"), salt)
            );
        }

        var matches = new List<string>();
        int attempts = 0;

        var stopwatch = Stopwatch.StartNew();

        for (int number = 0; number <= 9999; number++)
        {
            string candidate = number.ToString(
                "D4",
                CultureInfo.InvariantCulture
            );

            byte[] candidateBytes = Encoding.UTF8.GetBytes(candidate);

            string candidateHash = ComputeHex(
                Combine(candidateBytes, salt)
            );

            attempts++;

            if (candidateHash == targetHash)
            {
                matches.Add(candidate);
            }
        }

        stopwatch.Stop();

        string elapsed = stopwatch.Elapsed.TotalMilliseconds
            .ToString("F6", CultureInfo.InvariantCulture);

        Console.WriteLine(
            $"\nRežimas: {mode}\n" +
            $"Bandymų: {attempts}\n" +
            $"Laikas: {elapsed} ms\n" +
            $"Sutampantys kandidatai: {string.Join("; ", matches)}"
        );

        return $"{mode},{attempts},{elapsed},{matches.Count}," +
               $"{string.Join(";", matches)}";
    }

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