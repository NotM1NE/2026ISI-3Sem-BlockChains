using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Blockchain.Services;

namespace Blockchain.Experiments;

public static class PerformanceExperiments
{
    private static ulong checksum;

    public static int Run()
    {
        const int sampleCount = 5;

        string path = Path.Combine(
            "test-data",
            "konstitucija.txt"
        );

        if (!File.Exists(path))
        {
            Console.Error.WriteLine(
                $"Nerastas failas: {path}"
            );

            return 1;
        }

        // Failas perskaitomas prieš matavimus.
        byte[] completeFile = File.ReadAllBytes(path);

        List<byte[]> inputs = CreateInputs(completeFile);

        Directory.CreateDirectory("results");

        var rawRows = new List<string>
        {
            "lines,bytes,repetitions,sample,total_ms,ms_per_hash"
        };

        var summaryRows = new List<string>
        {
            "lines,bytes,repetitions,samples," +
            "min_ms,avg_ms,max_ms,megabytes_per_second"
        };

        foreach (byte[] input in inputs)
        {
            int lineCount = CountLines(input);

            // Apšilimas: pirmas vykdymas nematuojamas.
            for (int i = 0; i < 10; i++)
            {
                Consume(InputDistributorService.Compute(input));
            }

            int repetitions = FindRepetitions(input);

            var measurements = new List<double>();

            for (int sample = 1; sample <= sampleCount; sample++)
            {
                ulong localChecksum = 0;

                var stopwatch = Stopwatch.StartNew();

                for (int repetition = 0;
                     repetition < repetitions;
                     repetition++)
                {
                    uint[] hash =
                        InputDistributorService.Compute(input);

                    localChecksum ^= hash[repetition % hash.Length];
                }

                stopwatch.Stop();

                checksum ^= localChecksum;

                double totalMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds;

                double millisecondsPerHash =
                    totalMilliseconds / repetitions;

                measurements.Add(millisecondsPerHash);

                rawRows.Add(
                    $"{lineCount},{input.Length},{repetitions}," +
                    $"{sample},{Number(totalMilliseconds)}," +
                    $"{Number(millisecondsPerHash)}"
                );
            }

            double minimum = measurements.Min();
            double average = measurements.Average();
            double maximum = measurements.Max();

            double seconds = average / 1000.0;
            double megabytes = input.Length / 1_000_000.0;

            double megabytesPerSecond =
                seconds > 0
                    ? megabytes / seconds
                    : 0;

            summaryRows.Add(
                $"{lineCount},{input.Length},{repetitions}," +
                $"{sampleCount},{Number(minimum)}," +
                $"{Number(average)},{Number(maximum)}," +
                $"{Number(megabytesPerSecond)}"
            );

            Console.WriteLine(
                $"Eilučių: {lineCount}, " +
                $"baitų: {input.Length}, " +
                $"vidurkis: {Number(average)} ms, " +
                $"greitis: {Number(megabytesPerSecond)} MB/s"
            );
        }

        File.WriteAllLines(
            "results/performance-raw.csv",
            rawRows
        );

        File.WriteAllLines(
            "results/performance-summary.csv",
            summaryRows
        );

        File.WriteAllLines(
            "results/performance-metadata.txt",
            [
                $"Samples per input: {sampleCount}",
                $"Runtime: {RuntimeInformation.FrameworkDescription}",
                $"OS: {RuntimeInformation.OSDescription}",
                $"Architecture: {RuntimeInformation.ProcessArchitecture}",
                $"Processor count: {Environment.ProcessorCount}",
                $"File: {path}",
                $"File bytes: {completeFile.Length}",
                "Configuration: Release",
                "File reading and input preparation are not timed",
                $"Checksum: {checksum}"
            ]
        );

        Console.WriteLine(
            "\nRezultatai: results/performance-*.csv"
        );

        return 0;
    }

    private static List<byte[]> CreateInputs(byte[] file)
    {
        var lineEndPositions = new List<int>();

        for (int i = 0; i < file.Length; i++)
        {
            if (file[i] == (byte)'\n')
            {
                lineEndPositions.Add(i + 1);
            }
        }

        // Jeigu paskutinė eilutė neturi \n.
        if (file.Length > 0 &&
            (lineEndPositions.Count == 0 ||
             lineEndPositions[^1] != file.Length))
        {
            lineEndPositions.Add(file.Length);
        }

        var inputs = new List<byte[]>();

        int requestedLines = 1;

        while (requestedLines <= lineEndPositions.Count)
        {
            int byteCount =
                lineEndPositions[requestedLines - 1];

            inputs.Add(file[..byteCount]);

            if (requestedLines >
                lineEndPositions.Count / 2)
            {
                break;
            }

            requestedLines *= 2;
        }

        // Visas failas pridedamas, jeigu paskutinis testas
        // dar neapėmė viso failo.
        if (inputs.Count == 0 ||
            inputs[^1].Length != file.Length)
        {
            inputs.Add(file);
        }

        return inputs;
    }

    private static int FindRepetitions(byte[] input)
    {
        int repetitions = 1;

        while (true)
        {
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < repetitions; i++)
            {
                Consume(InputDistributorService.Compute(input));
            }

            stopwatch.Stop();

            // Mažoms įvestims vienas vykdymas per trumpas,
            // todėl viename matavime atliekame daugiau kartojimų.
            if (stopwatch.ElapsedMilliseconds >= 50)
            {
                return repetitions;
            }

            repetitions *= 2;
        }
    }

    private static int CountLines(byte[] input)
    {
        if (input.Length == 0)
        {
            return 0;
        }

        int lines = input.Count(value => value == (byte)'\n');

        if (input[^1] != (byte)'\n')
        {
            lines++;
        }

        return lines;
    }

    private static void Consume(uint[] hash)
    {
        checksum ^= hash[0];
    }

    private static string Number(double value)
    {
        return value.ToString(
            "F6",
            CultureInfo.InvariantCulture
        );
    }
}