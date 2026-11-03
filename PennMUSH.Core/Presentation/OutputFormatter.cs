namespace PennMUSH.Core.Presentation;

using System;

public static class OutputFormatter
{
    public static string FormatOutput(string message, ClientCapabilities capabilities)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;

        if (capabilities.SupportsHtml)
        {
            // Direct HTML delivery directly into text stream
            return message;
        }

        // Fallback: Translate HTML to ANSI / plaintext
        return HtmlToAnsiTranslator.Translate(message, capabilities.ColorMode);
    }

    public static string BuildPuebloInit(string stylesheetUrl)
    {
        if (string.IsNullOrEmpty(stylesheetUrl))
        {
            return "</xch_muc_open><xch_page>\r\n";
        }
        return $"</xch_muc_open><xch_page><link rel=\"stylesheet\" href=\"{stylesheetUrl}\">\r\n";
    }
}
