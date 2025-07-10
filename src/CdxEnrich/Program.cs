using System.CommandLine;
using CdxEnrich.Actions;
using CdxEnrich.ClearlyDefined;
using CdxEnrich.ClearlyDefined.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            var inputFileArg = new Argument<string>(
                name: "input file",
                description: "The path to a CycloneDX SBOM to enrich."
            );

            var outputFileOption = new Option<string>(
                aliases: ["-o", "--output-file"],
                description: "Path to save the enriched SBOM. Leave blank to overwrite the input file."
            );

            var configFileOption = new Option<IEnumerable<string>>(
                aliases: ["-c", "--config-files"],
                description: "Path to one or more configuration files."
            )
            { AllowMultipleArgumentsPerToken = true };


            var outputFileFormatOption = new Option<CycloneDXFormatOption>(
                aliases: ["-of", "--output-format"],
                description: "Specify the output file format.",
                getDefaultValue: () => CycloneDXFormatOption.Auto
            );

            var inputFileFormatOption = new Option<CycloneDXFormatOption>(
                aliases: ["-if", "--input-format"],
                description: "Specify the input file format, if necessary.",
                getDefaultValue: () => CycloneDXFormatOption.Auto
            );

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var runner = serviceProvider.GetRequiredService<IRunner>();
            // Create the root command and add options
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

            var result = rootCommand.Invoke(args);
            return result;
        }

        internal static void ConfigureServices(ServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IRunner, Runner>();
            serviceCollection.AddTransient<IReplaceAction, ReplaceLicenseByBomRef>();
            serviceCollection.AddTransient<IReplaceAction, ReplaceLicensesByUrl>();
            
            serviceCollection.AddTransient<IReplaceAction, ReplaceLicenseByClearlyDefined>();
            serviceCollection.AddTransient<IClearlyDefinedClient, ClearlyDefinedClient>();
            
            serviceCollection.AddTransient<ILicenseResolver, LicenseResolver>();
            serviceCollection.AddTransient<ResolveLicenseRuleFactory>();
            
            serviceCollection.AddLogging(x => 
            {
                x.AddConsole();
                x.SetMinimumLevel(LogLevel.Information);
            });
        }
    }
}
