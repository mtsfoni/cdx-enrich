using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CycloneDX.Models;

namespace CdxEnrich.Actions
{
    public static class ReplaceLicensesByUrl
    {
        static readonly string moduleName = nameof(ReplaceLicensesByUrl);

        private static IEnumerable<CycloneDX.Models.License> FindLicensesByUrl(Bom bom, string Url)
        {
            return
                bom.Components
                    .Where(comp => comp.Licenses?.Count > 0)
                    .SelectMany(comp => comp.Licenses)
                    .Where(lic => lic.License?.Url == Url)
                    .Select(lic => lic.License);
        }

        private static Result<ConfigRoot> MustNotHaveIdAndNameSet(ConfigRoot config)
        {
            if (config.ReplaceLicensesByURL?.Exists(rec => rec.Name != null && rec.Id != null) == true)
            {
                return InvalidConfigError.Create<ConfigRoot>(moduleName, "One entry must have either Id or Name. Not both.");
            }
            else
            {
                return new Ok<ConfigRoot>(config);
            }
        }

        private static Result<ConfigRoot> MustHaveEitherIdorName(ConfigRoot config)
        {
            if (config.ReplaceLicensesByURL?.Exists(rec => rec.Name == null && rec.Id == null) == true)
            {
                return InvalidConfigError.Create<ConfigRoot>(moduleName, "One entry must have either Id or Name.");
            }
            else
            {
                return new Ok<ConfigRoot>(config);
            }
        }

        private static Result<ConfigRoot> BomRefMustNotBeNullorEmpty(ConfigRoot config)
        {
            if (config.ReplaceLicensesByURL?.Exists(rec => string.IsNullOrEmpty(rec.URL)) == true)
            {
                return InvalidConfigError.Create<ConfigRoot>(moduleName, "Url must be set and cannot be an emtpy string.");
            }
            else
            {
                return new Ok<ConfigRoot>(config);
            }
        }

        public static Result<ConfigRoot> CheckConfig(ConfigRoot config)
        {
            return
                MustHaveEitherIdorName(config)
                .Bind(MustNotHaveIdAndNameSet)
                .Bind(BomRefMustNotBeNullorEmpty);
        }

        public static InputTuple Execute(InputTuple inputs)
        {
            inputs.Config.ReplaceLicensesByURL?
                .Where(rep => rep.URL != null)
                .ToList()
                .ForEach(rep =>
                {
                    var licensesRaw = FindLicensesByUrl(inputs.Bom, rep.URL!);

                    foreach (var license in licensesRaw)
                    {
                        license.Id = rep.Id;
                        license.Name = rep.Name;
                    }
                });

            return inputs;
        }

    }
}
