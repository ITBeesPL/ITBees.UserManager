public class LanaguageParser
{
    public static string ParseAcceptLanguageHeader(string acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return null;
        }

        string[] languages = acceptLanguage.Split(',');
        if (languages.Length > 0)
        {
            return languages[0].Split(';')[0].Substring(0, 2);
        }

        return null;
    }
}