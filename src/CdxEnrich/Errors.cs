using CdxEnrich.FunctionalHelpers;

namespace CdxEnrich
{

    public class UnspecificError : IErrorType
    {
        public string ErrorMessage => "This Error shouldn't be used.";

        public static Error<TData> Create<TData>()
        {
            return new Error<TData>(new UnspecificError());
        }
    }

    public class InvalidFileFormatError : IErrorType
    {
        public string ErrorMessage => "Input file is neither XML nor JSON.";

        public static Error<TData> Create<TData>()
        {
            return new Error<TData>(new InvalidFileFormatError());
        }
    }

    public class YamlDeserializationError(string message) : IErrorType
    {
        static readonly string baseErrorMessage = "Error when Deserializing the Config-File: {0}";

        public string ErrorMessage => string.Format(baseErrorMessage, message);

        public static Error<TData> Create<TData>(string message)
        {
            return new Error<TData>(new YamlDeserializationError(message));
        }
    }

    public class BomDeserializationError(string fileFormat) : IErrorType
    {
        static readonly string baseErrorMessage = "Couldn't parse input file as {0}";

        public string ErrorMessage => string.Format(baseErrorMessage, fileFormat);

        public static Error<TData> Create<TData>(string fileFormat)
        {
            return new Error<TData>(new BomDeserializationError(fileFormat));
        }
    }

    public class BomSerializationError(string exceptionMessage) : IErrorType
    {
        static readonly string baseErrorMessage = "Error when serializing the bom. This is likely an error in the CycloneDX library.{0}{1}";

        public string ErrorMessage => string.Format(baseErrorMessage, Environment.NewLine, exceptionMessage);

        public static Error<TData> Create<TData>(string exceptionMessage)
        {
            return new Error<TData>(new BomSerializationError(exceptionMessage));
        }
    }

    public class InvalidConfigError(string configSection, string desciption) : IErrorType
    {
        static readonly string baseErrorMessage = "Config has invalid content in section {0}: {1}{2}";

        public string ErrorMessage => string.Format(baseErrorMessage, configSection, Environment.NewLine, desciption);

        public static Error<TData> Create<TData>(string configSection, string desciption)
        {
            return new Error<TData>(new InvalidConfigError(configSection, desciption));
        }
    }

}