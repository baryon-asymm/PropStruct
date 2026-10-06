using PropStruct.Simulation;

namespace PropStruct.Cli;

/// <summary>
/// The whole grammar of the command line, hand-written (BOOT.md, "The parser is this node's own,
/// hand-written"). Never writes to the console and never throws for bad input: <see cref="Parse"/>
/// returns either a parsed command or a non-empty list of <see cref="ParseError"/>s, and
/// <see cref="Program"/> decides what to print and which exit code to use.
/// </summary>
internal static class CommandLine
{
    public static ParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return ParseResult.OfError(new ParseError(Diagnostic.NoVerbGiven, "no command given; try `propstruct --help`"));
        }

        if (args[0] is "--help" or "-h")
        {
            return ParseResult.OfCommand(new HelpCommand(null));
        }

        if (args[0] == "--version")
        {
            return ParseResult.OfCommand(new VersionCommand());
        }

        return args[0] switch
        {
            "run" => ParseRun(args, 1),
            "devices" => ParseNoArgumentVerb(args, 1, new DevicesCommand(), "devices"),
            "defaults" => ParseNoArgumentVerb(args, 1, new DefaultsCommand(), "defaults"),
            var verb => ParseResult.OfError(new ParseError(Diagnostic.UnknownVerb, $"unknown command `{verb}`; try `propstruct --help`")),
        };
    }

    private static ParseResult ParseNoArgumentVerb(string[] args, int startIndex, ParsedCommand command, string verbName)
    {
        if (startIndex < args.Length && args[startIndex] is "--help" or "-h" && startIndex == args.Length - 1)
        {
            return ParseResult.OfCommand(new HelpCommand(verbName));
        }

        if (startIndex < args.Length)
        {
            return ParseResult.OfError(new ParseError(Diagnostic.UnknownOption, $"unknown option `{args[startIndex]}`"));
        }

        return ParseResult.OfCommand(command);
    }

    private static ParseResult ParseRun(string[] args, int startIndex)
    {
        string? inputPath = null;
        var extraPositionals = new List<string>();
        var setProperties = new HashSet<string>(StringComparer.Ordinal);
        var modelValues = new Dictionary<string, object>();
        var optionValues = new Dictionary<string, object>();
        string? outputPath = null;
        string? jsonPath = null;
        var quiet = false;
        var errors = new List<ParseError>();
        var afterDoubleDash = false;

        for (var i = startIndex; i < args.Length; i++)
        {
            var token = args[i];

            if (!afterDoubleDash && token == "--")
            {
                afterDoubleDash = true;
                continue;
            }

            if (!afterDoubleDash && i == startIndex && token is "--help" or "-h")
            {
                return ParseResult.OfCommand(new HelpCommand("run"));
            }

            if (!afterDoubleDash && token.StartsWith("--", StringComparison.Ordinal))
            {
                var equals = token.IndexOf('=');
                var name = equals >= 0 ? token[..equals] : token;
                var inlineValue = equals >= 0 ? token[(equals + 1)..] : null;

                if (!FlagCatalog.TryResolve(name, out var flag))
                {
                    errors.Add(new ParseError(Diagnostic.UnknownOption, $"unknown option `{name}`"));
                    continue;
                }

                var key = flag.Owner + "." + flag.PropertyName;
                var duplicate = !setProperties.Add(key);
                if (duplicate)
                {
                    errors.Add(new ParseError(Diagnostic.DuplicateOption, $"`{flag.CanonicalFlag}` given twice"));
                }

                if (flag.Kind == FlagKind.Switch)
                {
                    if (inlineValue is not null)
                    {
                        errors.Add(new ParseError(Diagnostic.SwitchDoesNotTakeValue, $"`{flag.CanonicalFlag}` does not take a value"));
                        continue;
                    }

                    if (!duplicate)
                    {
                        StoreSwitch(flag, true, modelValues, optionValues, ref quiet);
                    }

                    continue;
                }

                string valueText;
                if (inlineValue is not null)
                {
                    valueText = inlineValue;
                }
                else
                {
                    if (i + 1 >= args.Length)
                    {
                        errors.Add(new ParseError(Diagnostic.MissingOptionValue, $"`{flag.CanonicalFlag}` expects a value"));
                        continue;
                    }

                    valueText = args[++i];
                }

                if (!FlagValueParser.TryParse(flag, valueText, out var parsed, out var parseError))
                {
                    errors.Add(parseError!);
                    continue;
                }

                if (!duplicate)
                {
                    StoreValue(flag, parsed, modelValues, optionValues, ref outputPath, ref jsonPath);
                }

                continue;
            }

            // A token starting with "-" that reached here was not consumed as a value-taking flag's
            // value (that branch reads its own next token directly, never looping back to this one) and
            // is not a recognized "--" flag either: before "--" ends the options, this is someone's typo
            // of a flag, not a file path (BOOT.md: "after `--` a path may begin with `-`" - the "after"
            // is load-bearing). Reviewed 2026-09-20: a bare "-x" used to fall through to the positional
            // branch below and be silently accepted as (or appended to) the file path.
            if (!afterDoubleDash && token.StartsWith('-') && token.Length > 1)
            {
                errors.Add(new ParseError(Diagnostic.UnknownOption, $"unknown option `{token}`"));
                continue;
            }

            if (inputPath is null)
            {
                inputPath = token;
            }
            else
            {
                extraPositionals.Add(token);
            }
        }

        if (extraPositionals.Count > 0)
        {
            errors.Add(new ParseError(Diagnostic.RunTooManyPositionalArguments, $"unexpected argument `{extraPositionals[0]}`"));
        }

        if (inputPath is null)
        {
            errors.Add(new ParseError(Diagnostic.RunMissingInputFile, "`run` requires a formulation file"));
        }

        var parameters = RunOptionsBuilder.BuildModelParameters(modelValues);
        var options = RunOptionsBuilder.BuildSimulationOptions(optionValues, parameters);

        if (SimulationOptionsValidator.IsContinuedStreamsCombinationInvalid(options))
        {
            errors.Add(new ParseError(
                Diagnostic.ContinuedStreamsRequiresBatchedBatchOne,
                "`--continued-streams` is only valid with `--mode batched --batch 1`"));
        }

        if (errors.Count > 0)
        {
            return ParseResult.OfErrors(errors);
        }

        var resolvedOutputPath = Path.GetFullPath(outputPath ?? "results.m");
        var resolvedJsonPath = jsonPath is null ? null : Path.GetFullPath(jsonPath);

        return ParseResult.OfCommand(new RunCommand(inputPath!, parameters, options, resolvedOutputPath, resolvedJsonPath, quiet));
    }

    private static void StoreSwitch(
        FlagDefinition flag, bool value, Dictionary<string, object> modelValues, Dictionary<string, object> optionValues, ref bool quiet)
    {
        switch (flag.Owner)
        {
            case FlagOwner.ModelParameters:
                modelValues[flag.PropertyName] = value;
                break;
            case FlagOwner.SimulationOptions:
                optionValues[flag.PropertyName] = value;
                break;
            case FlagOwner.Cli:
                if (flag.PropertyName == "Quiet")
                {
                    quiet = value;
                }

                break;
            default:
                break;
        }
    }

    private static void StoreValue(
        FlagDefinition flag,
        object value,
        Dictionary<string, object> modelValues,
        Dictionary<string, object> optionValues,
        ref string? outputPath,
        ref string? jsonPath)
    {
        switch (flag.Owner)
        {
            case FlagOwner.ModelParameters:
                modelValues[flag.PropertyName] = value;
                break;
            case FlagOwner.SimulationOptions:
                optionValues[flag.PropertyName] = value;
                break;
            case FlagOwner.Cli:
                switch (flag.PropertyName)
                {
                    case "OutputPath":
                        outputPath = (string)value;
                        break;
                    case "JsonPath":
                        jsonPath = (string)value;
                        break;
                    default:
                        break;
                }

                break;
            default:
                break;
        }
    }
}
