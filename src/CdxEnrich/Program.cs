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

            var verboseOption = new Option<bool>("--verbose", "-v")
            {
                Description = "Enable verbose output (show informational messages)."
            };

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var runner = serviceProvider.GetRequiredService<IRunner>();
            
            // Create the root command and add options
            var rootCommand = new RootCommand("A .NET tool for enriching CycloneDX Bill-of-Materials (BOM) with predefined data.");
            
            rootCommand.Add(inputFileArg);
            rootCommand.Add(inputFileFormatOption);
            rootCommand.Add(outputFileOption);
            rootCommand.Add(outputFileFormatOption);
            rootCommand.Add(configFileOption);
            rootCommand.Add(verboseOption);

            rootCommand.SetAction(parseResult =>
            {
                // Set verbose mode before running
                Log.SetVerbose(parseResult.GetValue(verboseOption));
                
                return runner.Enrich(
                    parseResult.GetValue(inputFileArg) ?? "",
                    parseResult.GetValue(inputFileFormatOption),
                    parseResult.GetValue(outputFileOption) ?? "",
                    parseResult.GetValue(configFileOption) ?? new List<string>(),
                    parseResult.GetValue(outputFileFormatOption));
            });

            return rootCommand.Parse(args).Invoke();
        }

        internal static void ConfigureServices(ServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IRunner, Runner>();
            serviceCollection.AddReplaceLicenseByClearlyDefined();
        }
    }
}