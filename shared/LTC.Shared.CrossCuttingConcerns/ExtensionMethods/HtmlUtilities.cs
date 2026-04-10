using Ganss.Xss;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTC.Shared.CrossCuttingConcerns.ExtensionMethods
{
    public static class HtmlUtilities
    {
        public static string? SanitizeHtml(string? text, params string[] allowedAttributes)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var sanitizer = new HtmlSanitizer();
            if (!allowedAttributes.IsNullOrEmpty())
                foreach (var allowedAttribute in allowedAttributes)
                    sanitizer.AllowedAttributes.Add(allowedAttribute);
            return sanitizer.Sanitize(text);
        }

        public static string? ClearFormatHtml(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var cleanHtml = SanitizeHtml(text);
            var doc = new HtmlDocument();
            doc.LoadHtml(cleanHtml);
            string plainText = doc.DocumentNode.InnerText;
            return plainText;
        }

    }
}
