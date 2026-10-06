namespace PropStruct.Cli;

/// <summary>The static help text (BOOT.md, "`--help`/`-h`, alone or after a verb, prints that verb's
/// usage to stdout and exits 0"). No formatting logic, no computed content: plain constants.</summary>
internal static class UsageText
{
    public const string General =
        """
        propstruct - Monte Carlo pocket-model simulator for composite solid propellants.

        Usage:
          propstruct run <file.dat> [run options] [model parameters]
          propstruct devices
          propstruct defaults
          propstruct --help | --version

        Run `propstruct run --help` for run options and model parameters, or
        `propstruct devices --help` / `propstruct defaults --help` for those verbs.
        """;

    public const string Run =
        """
        propstruct run <file.dat> [run options] [model parameters]

        Reads a formulation, runs the simulation, writes results.m (and, with --json,
        a JSON copy) and exits with a code describing the outcome.

        Run options (defaults are SimulationOptions'):
          --mode reference|batched          --accelerator auto|cpu|cuda
          --layout original|independent     --batch <n>          --seed <n>
          --precision binary64|original       (original: sequential only)
          --output <path>                   --json <path>        --quiet
          --attempts-per-launch <n>         --max-attempts-per-particle <n>
          --neighbour-budget <n>            --record-budget-bytes <n>
          --continued-streams                (batched, --batch 1 only)

        --precision reproduces the original's REAL*4 rounding loss, in its accumulators,
        its setup plane and its per-cycle plane (binary64 is the default and matches every
        prior run; `double` is still accepted as an older spelling of binary64);
        --precision original is refused with batched mode.

        Model parameters, the original's menu (canonical name, alias):
          --dmin <d>            --di <d> (--cell-size)      --dj <d> (--category-step)
          --eps <x> (--eps-dok) --k5 <x> (--alpha)          --nn-min <x> (--k6)
          --karmcoef <x> (--k7) --mkmcoef <x> (--k8)        --alfa <x> (--tail-probability)
          --nn-max <x>          --gdokns <x>                --sfr
          --ivar <n>            --eta <x>

        A length is handed to Input's Length as written: a value >= 0.1 is micrometres,
        any other value metres. Nothing is converted. Numbers use the invariant culture;
        the Fortran exponent form (10d-6) is refused. A flag given twice is an error.
        """;

    public const string Devices =
        """
        propstruct devices

        Lists the accelerators this process could bind to right now: the CPU
        accelerator always, and CUDA when it is available.
        """;

    public const string Defaults =
        """
        propstruct defaults

        Prints the fourteen model parameters of the original's menu, their canonical
        flag and aliases, and ModelParameters.Default's own value for each.
        """;

    public static string ForVerb(string? verb) => verb switch
    {
        "run" => Run,
        "devices" => Devices,
        "defaults" => Defaults,
        _ => General,
    };
}
