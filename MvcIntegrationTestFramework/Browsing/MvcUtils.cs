﻿using System;
using System.Text.RegularExpressions;
namespace MvcIntegrationTestFramework.Browsing
{
    public static class MvcUtils
    {
        public static string ExtractAntiForgeryToken(string htmlResponseText)
        {
            if (htmlResponseText == null)
            {
                throw new ArgumentNullException("htmlResponseText");
            }
            Match match = Regex.Match(htmlResponseText, "\\<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\" \\/\\>");
            if (!match.Success)
            {
                return null;
            }
            return match.Groups[1].Captures[0].Value;
        }
    }
}
