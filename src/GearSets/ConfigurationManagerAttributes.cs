// Read by ConfigurationManager through reflection, matched by type name only. Declaring it
// here means no dependency on any particular ConfigurationManager build.
#pragma warning disable 0649
internal sealed class ConfigurationManagerAttributes
{
    public int? Order;
    public bool? IsAdvanced;
}
