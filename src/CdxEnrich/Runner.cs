using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CdxEnrich.Serialization;
using CycloneDX.Models;
using CdxEnrich.Actions;


namespace CdxEnrich
{
    public class Runner(IEnumerable<IReplaceAction> replaceActions) : IRunner
    {
        public static Result<InputTuple> CombineBomAndConfig(Result<Bom> bom, Result<ConfigRoot> config)
        {

            if (bom is Ok<Bom> ok1 && config is Ok<ConfigRoot> ok2)
            {
                return new Ok<InputTuple>(new InputTuple(ok1.Data, ok2.Data));
            }

            if (bom is Failure failure1)
            {
                return new Error<InputTuple>(failure1);
            }

            return new Error<InputTuple>((Failure)config);
        }


        public int Enrich(string inputFilePath, CycloneDXFormatOption inputFormat, string outputFilePath, IEnumerable<string> configPaths, CycloneDXFormatOption outputFileFormat)
        {
            try
            {
                CycloneDXFormat explicitInputFormat;
                CycloneDXFormat explicitOutputFormat;


                explicitInputFormat = inputFormat switch
                {
                    CycloneDXFormatOption.Auto => Path.GetExtension(inputFilePath).ToLower() switch
                    {
                        ".xml" => CycloneDXFormat.XML,
                        ".json" => CycloneDXFormat.JSON,
                        _ => throw new InvalidOperationException("File format of the input file could not be detected automatically by its file extension. Please provide the format as an argument.")
                    },
                    CycloneDXFormatOption.XML => CycloneDXFormat.XML,
                    CycloneDXFormatOption.JSON => CycloneDXFormat.JSON,
                    _ => throw new ArgumentOutOfRangeException(nameof(inputFormat))
                };


                if (string.IsNullOrWhiteSpace(outputFilePath))
                {
                    outputFilePath = inputFilePath;
                    explicitOutputFormat = explicitInputFormat;
                }
                else
                {

                    explicitOutputFormat = outputFileFormat switch
                    {
                        CycloneDXFormatOption.Auto => Path.GetExtension(outputFilePath).ToLower() switch
                        {
                            ".xml" => CycloneDXFormat.XML,
                            ".json" => CycloneDXFormat.JSON,
                            _ => explicitInputFormat
                        },
                        CycloneDXFormatOption.XML => CycloneDXFormat.XML,
                        CycloneDXFormatOption.JSON => CycloneDXFormat.JSON,
                        _ => throw new ArgumentOutOfRangeException(nameof(outputFileFormat))
                    };
                }

                var inputContent = File.ReadAllText(inputFilePath);
                string currentBomAsString = inputContent;


                foreach (var configPath in configPaths)
                {
                    var configContent = File.ReadAllText(configPath);
                    Result<string> enrichResult = Enrich(currentBomAsString, explicitInputFormat, configContent, explicitOutputFormat);
                    //if there are multiple rounds, we need to fix the format
                    explicitInputFormat = explicitOutputFormat;

                    if (enrichResult is Ok<string> success)
                    {
                        currentBomAsString = success.Data;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("An error occurred when trying to enrich the bom");
                        Console.WriteLine(((Failure)enrichResult).ErrorMessage);
                        Console.ResetColor();
                        return 1;
                    }
                }

                File.WriteAllText(outputFilePath, currentBomAsString);
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An unexpected error occurred:");
                Console.WriteLine($"Error Type: {ex.GetType()}");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine("Please check the input and try again. If the problem persists, consult the documentation or contact support.");
                Console.ResetColor();
                return 1;
            }
        }

        public Result<string> Enrich(string inputFileContent, CycloneDXFormat inputFormat, string configFileContent, CycloneDXFormat outputFileFormat)
        {
            return CombineBomAndConfig(
                    BomSerialization.DeserializeBom(inputFileContent, inputFormat),
                    ConfigLoader.ParseConfig(configFileContent)
                        .AggregateBind(replaceActions, action => action.CheckConfig))
                .AggregateBind(replaceActions, action => action.CheckBomAndConfigCombination)
                .AggregateMap(replaceActions, action => action.Execute)
                .Bind(inputs => BomSerialization.SerializeBom(inputs, outputFileFormat));
        }
    }

    public interface IRunner
    {
        int Enrich(string inputFilePath, CycloneDXFormatOption inputFormat, string outputFilePath,
            IEnumerable<string> configPaths, CycloneDXFormatOption outputFileFormat);

        Result<string> Enrich(string inputFileContent, CycloneDXFormat inputFormat, string configFileContent,
            CycloneDXFormat outputFileFormat);
    }
}
