namespace GearSets.Core
{
    internal static class Loc
    {
        /// <summary>Localize a token such as "$item_pickaxe_iron"; plain text passes through.</summary>
        public static string T(string token)
        {
            if (string.IsNullOrEmpty(token))
                return "";
            return Localization.instance != null ? Localization.instance.Localize(token) : token;
        }
    }
}
