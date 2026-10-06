using System.Collections.Immutable;
using System.Text;

namespace PropStruct.Input;

/// <summary>
/// Reads the original's <c>.dat</c> formulation format (BOOT.md, "The original's reading order", source
/// lines 93-133 and 245-248).
/// </summary>
public static class DatFile
{
    /// <summary>Reads <paramref name="path"/> as a <see cref="Formulation"/>.</summary>
    /// <param name="path">The file to read.</param>
    /// <param name="readPocketFormingFractions">Whether to also read the trailing <c>SFR</c> line (menu [12]).</param>
    /// <exception cref="FileNotFoundException"><paramref name="path"/> does not exist.</exception>
    /// <exception cref="FormulationFormatException">The file is not in the original's format.</exception>
    public static Formulation Read(string path, bool readPocketFormingFractions = false)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"formulation file not found: {path}", path);
        }

        using var stream = File.OpenRead(path);
        return Parse(stream, Path.GetFileNameWithoutExtension(path), readPocketFormingFractions);
    }

    /// <summary>Reads <paramref name="stream"/> as a <see cref="Formulation"/> named <paramref name="name"/>.</summary>
    /// <exception cref="FormulationFormatException">The stream is not in the original's format.</exception>
    public static Formulation Parse(Stream stream, string name, bool readPocketFormingFractions = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(name);

        // Skipped records are never decoded (BOOT.md, "Skipped records are never decoded"); the byte-to-char
        // mapping below (Latin-1, one byte per char, every code point valid) reproduces every byte of every
        // record exactly, whichever of the archive's encodings produced its label text. The numeric records
        // this reader interprets use only ASCII digits, signs, letters and separators, which read identically
        // in every one of those encodings.
        using var reader = new StreamReader(stream, Encoding.Latin1, detectEncodingFromByteOrderMarks: false);
        var text = reader.ReadToEnd();
        var records = new DatRecordReader(text);

        records.SkipRecord();
        var header = records.ReadValues(4, requireIntegral: false);
        records.SkipRecord();
        var ak = records.ReadValues(4, requireIntegral: false);
        records.SkipRecord();
        var (counts, countLines) = records.ReadValuesWithLines(6, requireIntegral: true);

        var gsv = ToInt32(counts[5], countLines[5]);
        if (gsv != 2)
        {
            throw new FormulationFormatException($"GSV must be 2, was {gsv}", countLines[5]);
        }

        var nmm = ToInt32(counts[0], countLines[0]);
        var sizeLawCode = ToInt32(counts[1], countLines[1]);
        var cycles = Math.Max(ToInt32(counts[2], countLines[2]), 1); // KXX < 1 normalized to 1 (source lines 136-143)
        var particlesPerCycle = ToInt32(counts[3], countLines[3]);
        var generatorWarmup = counts[4];

        records.SkipRecord();
        var massShares = records.ReadValues(nmm, requireIntegral: false);
        records.SkipRecord();
        var bounds = records.ReadValues(2 * nmm, requireIntegral: false);

        var fractions = ImmutableArray.CreateBuilder<OxidizerFraction>(nmm);
        for (var i = 0; i < nmm; i++)
        {
            fractions.Add(new OxidizerFraction(massShares[i], new Length(bounds[2 * i]), new Length(bounds[2 * i + 1])));
        }

        ImmutableArray<int>? pocketFormingFractions = null;
        if (readPocketFormingFractions)
        {
            records.SkipRecord();
            var (sfr, sfrLines) = records.ReadValuesWithLines(nmm, requireIntegral: true);
            var sfrBuilder = ImmutableArray.CreateBuilder<int>(nmm);
            for (var i = 0; i < nmm; i++)
            {
                sfrBuilder.Add(ToInt32(sfr[i], sfrLines[i]));
            }

            pocketFormingFractions = sfrBuilder.MoveToImmutable();
        }

        return new Formulation(
            name,
            OxidizerDensity: header[0],
            PropellantDensity: header[1],
            OxidizerMassFraction: header[2],
            MetalMassFraction: header[3],
            Ak1: ak[0],
            Ak2: ak[1],
            Ak3: ak[2],
            Ak4: ak[3],
            SizeLawCode: sizeLawCode,
            Cycles: cycles,
            ParticlesPerCycle: particlesPerCycle,
            GeneratorWarmup: generatorWarmup,
            Fractions: fractions.MoveToImmutable(),
            PocketFormingFractions: pocketFormingFractions);
    }

    private static int ToInt32(double value, int lineNumber)
    {
        try
        {
            return checked((int)value);
        }
        catch (OverflowException exception)
        {
            throw new FormulationFormatException($"'{value}' does not fit an integer item", lineNumber, exception);
        }
    }
}
