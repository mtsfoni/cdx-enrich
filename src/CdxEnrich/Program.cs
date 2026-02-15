using System.CommandLine;
using CdxEnrich.Actions;
using Microsoft.Extensions.DependencyInjection;

namespace CdxEnrich
{
    public enum CycloneDXFormat
    {
        XML,
        JSON
    }

    public enum CycloneDXFormatOption
    {
        XML,
        JSON,
        Auto
    }

    static class Program
    {
        static int Main(string[] args)
        {
            var inputFileArg = new Argument<string>("input file")
            {
                Description = "The path to a CycloneDX SBOM to enrich."
            };

            var outputFileOption = new Option<string>("--output-file", "-o")
            {
                Description = "Path to save the enriched SBOM. Leave blank to overwrite the input file."
            };

            var configFileOption = new Option<IEnumerable<string>>("--config-files", "-c")
            {
                Description = "Path to one or more configuration files.",
                AllowMultipleArgumentsPerToken = true
            };

            var outputFileFormatOption = new Option<CycloneDXFormatOption>("--output-format", "-of")
            {
                Description = "Specify the output file format.",
                DefaultValueFactory = _ => CycloneDXFormatOption.Auto
            };

            var inputFileFormatOption = new Option<CycloneDXFormatOption>("--input-format", "-if")
            {
                Description = "Specify the input file format, if necessary.",
                DefaultValueFactory = _ => CycloneDXFormatOption.Auto
            };

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var runner = serviceProvider.GetRequiredService<IRunner>();
            
            var rootCommand = new RootCommand();
            rootCommand.AddArgument(inputFileArg);
            rootCommand.AddOption(inputFileFormatOption);
            rootCommand.AddOption(outputFileOption);
            rootCommand.AddOption(outputFileFormatOption);
            rootCommand.AddOption(configFileOption);
            rootCommand.Description = "A .NET tool for enriching CycloneDX Bill-of-Materials (BOM) with predefined data.";
            rootCommand.SetHandler((context) =>
                context.ExitCode =
                    runner.Enrich
                        (context.ParseResult.GetValueForArgument(inputFileArg) ?? "",
                        context.ParseResult.GetValueForOption(inputFileFormatOption),
                        context.ParseResult.GetValueForOption(outputFileOption) ?? "",
                        context.ParseResult.GetValueForOption(configFileOption) ?? new List<string>(),
                        context.ParseResult.GetValueForOption(outputFileFormatOption))
            );

            return rootCommand.Parse(args).Invoke();
        }

        internal static void ConfigureServices(ServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging();
            serviceCollection.AddTransient<IRunner, Runner>();
            serviceCollection.AddTransient<IReplaceAction, ReplaceLicenseByBomRef>();
            serviceCollection.AddTransient<IReplaceAction, ReplaceLicensesByUrl>();
            serviceCollection.AddReplaceLicenseByClearlyDefined();
        }
    }
}