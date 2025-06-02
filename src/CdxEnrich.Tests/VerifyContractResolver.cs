using System.Reflection;
using Argon;
using CdxEnrich.FunctionalHelpers;

namespace CdxEnrich.Tests
{
    internal class VerifyContractResolver : DefaultContractResolver
    {
        private readonly string[] fieldNamesToIgnore = [
            "LicensesSerialized",
            "NonNullableScope",
            "NonNullableModified",
            "NonNullableAcknowledgement",
            "Author_Xml"];
        private readonly Type interfaceType = typeof(Failure);

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyName == "Data")
            {
                if (interfaceType.IsAssignableFrom(property.DeclaringType))
                {
                    property.Ignored = true;
                }
            }

            if (fieldNamesToIgnore.Contains(property.PropertyName))
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
