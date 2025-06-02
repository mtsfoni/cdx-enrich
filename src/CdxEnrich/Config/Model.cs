using CycloneDX.Models;

namespace CdxEnrich.Config
{
    public class InputTuple(Bom bom, ConfigRoot config)
    {
        public Bom Bom => bom;
        public ConfigRoot Config => config;
    }

    public class ConfigRoot
    {
        public List<LicenseByUrl>? ReplaceLicensesByURL { get; set; }
        public List<LicenseByBomRef>? ReplaceLicenseByBomRef { get; set; }
        public List<ReplaceLicenseByClearlyDefinedConfig>? ReplaceLicenseByClearlyDefined { get; set; }
    }

    public class LicenseByUrl
    {
        public string? URL { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public class LicenseByBomRef
    {
        public string? Ref { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
    
    public class ReplaceLicenseByClearlyDefinedConfig
    {
        public string? Ref { get; set; }
    }
}