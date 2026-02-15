using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CycloneDX.Models;

namespace CdxEnrich.Actions
{
    public static class ReplaceLicenseByBomRef
    {
        static readonly string moduleName = nameof(ReplaceLicenseByBomRef);

        public static Component? GetComponentByBomRef(Bom bom, string bomRef)
        {
            return
                bom.Components.Find(comp => comp.BomRef == bomRef);
        }

        private static Result<ConfigRoot> MustNotHaveIdAndNameSet(ConfigRoot config)
        {
            if (config.ReplaceLicenseByBomRef?.Exists(rec => rec.Name != null && rec.Id != null) == true)
            {
                return InvalidConfigError.Create<ConfigRoot>(moduleName, "One entry must have either Id or Name. Not both.");
            }
            else
            {
                return new Ok<ConfigRoot>(config);
            }
        }

        private static Result<ConfigRoot> MustHaveEitherIdOrName(ConfigRoot config)
        {
            if (config.ReplaceLicenseByBomRef?.Exists(rec => rec.Name == null && rec.Id == null) == true)
            {
                return InvalidConfigError.Create<ConfigRoot>(moduleName, "One entry must have either Id or Name.");
            }
            else
            {
                return new Ok<ConfigRoot>(config);
            }
        }

        private static Result<ConfigRoot> BomRefMustNotBeNullOrEmpty(ConfigRoot config)
        {
            if (config.ReplaceLicenseByBomRef?.Exists(rec => string.IsNullOrEmpty(rec.Ref)) == true)
            {
                return InvalidConfigError.Create<ConfigRoot>(moduleName, "BomRef must be set and cannot be an emtpy string.");
            }
            else
            {
                return new Ok<ConfigRoot>(config);
            }
        }

        public static Result<ConfigRoot> CheckConfig(ConfigRoot config)
        {
            return
                MustHaveEitherIdOrName(config)
                .Bind(MustNotHaveIdAndNameSet)
                .Bind(BomRefMustNotBeNullOrEmpty);
        }

        public static InputTuple Execute(InputTuple inputs)
        {
            inputs.Config.ReplaceLicenseByBomRef?
                   .Where(rep => rep.Ref != null)
                   .ToList()
                   .ForEach(rep =>
                   {
                       var comp = GetComponentByBomRef(inputs.Bom, rep.Ref!);
                       
                       comp?.Licenses = [
                           new LicenseChoice
                           {
                               License = new License()
                               {
                                   Name = rep.Name,
                                   Id = rep.Id
                               }
                           },
                       ];
                   });

            return inputs;
        }
    }
}