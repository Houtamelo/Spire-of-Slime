using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using JetBrains.Annotations;

namespace Core.Combat.Scripts
{
    public static class CustomColorTranslator
    {
        private static readonly Hashtable HtmlSysColorTable;

        static CustomColorTranslator() =>
            HtmlSysColorTable = new Hashtable(26)
            {
                ["activeborder"] = Color.FromKnownColor(KnownColor.ActiveBorder),
                ["activecaption"] = Color.FromKnownColor(KnownColor.ActiveCaption),
                ["appworkspace"] = Color.FromKnownColor(KnownColor.AppWorkspace),
                ["background"] = Color.FromKnownColor(KnownColor.Desktop),
                ["buttonface"] = Color.FromKnownColor(KnownColor.Control),
                ["buttonhighlight"] = Color.FromKnownColor(KnownColor.ControlLightLight),
                ["buttonshadow"] = Color.FromKnownColor(KnownColor.ControlDark),
                ["buttontext"] = Color.FromKnownColor(KnownColor.ControlText),
                ["captiontext"] = Color.FromKnownColor(KnownColor.ActiveCaptionText),
                ["graytext"] = Color.FromKnownColor(KnownColor.GrayText),
                ["highlight"] = Color.FromKnownColor(KnownColor.Highlight),
                ["highlighttext"] = Color.FromKnownColor(KnownColor.HighlightText),
                ["inactiveborder"] = Color.FromKnownColor(KnownColor.InactiveBorder),
                ["inactivecaption"] = Color.FromKnownColor(KnownColor.InactiveCaption),
                ["inactivecaptiontext"] = Color.FromKnownColor(KnownColor.InactiveCaptionText),
                ["infobackground"] = Color.FromKnownColor(KnownColor.Info),
                ["infotext"] = Color.FromKnownColor(KnownColor.InfoText),
                ["menu"] = Color.FromKnownColor(KnownColor.Menu),
                ["menutext"] = Color.FromKnownColor(KnownColor.MenuText),
                ["scrollbar"] = Color.FromKnownColor(KnownColor.ScrollBar),
                ["threeddarkshadow"] = Color.FromKnownColor(KnownColor.ControlDarkDark),
                ["threedface"] = Color.FromKnownColor(KnownColor.Control),
                ["threedhighlight"] = Color.FromKnownColor(KnownColor.ControlLight),
                ["threedlightshadow"] = Color.FromKnownColor(KnownColor.ControlLightLight),
                ["window"] = Color.FromKnownColor(KnownColor.Window),
                ["windowframe"] = Color.FromKnownColor(KnownColor.WindowFrame),
                ["windowtext"] = Color.FromKnownColor(KnownColor.WindowText)
            };

        public static Color FromHtml([CanBeNull] string htmlColor)
        {
            Color c = Color.Empty;

            // empty color
            if ((htmlColor == null) || (htmlColor.Length == 0))
                return c;

            // #RRGGBB or #RGB
            if ((htmlColor[0] == '#') && ((htmlColor.Length == 7) || (htmlColor.Length == 4)))
            {
                if (htmlColor.Length == 7)
                {
                    c = Color.FromArgb(Convert.ToInt32(htmlColor.Substring(1, 2), 16),
                                       Convert.ToInt32(htmlColor.Substring(3, 2), 16),
                                       Convert.ToInt32(htmlColor.Substring(5, 2), 16));
                }
                else
                {
                    string r = Char.ToString(htmlColor[1]);
                    string g = Char.ToString(htmlColor[2]);
                    string b = Char.ToString(htmlColor[3]);

                    c = Color.FromArgb(Convert.ToInt32(r + r, 16),
                                       Convert.ToInt32(g + g, 16),
                                       Convert.ToInt32(b + b, 16));
                }
            }

            // special case. Html requires LightGrey, but .NET uses LightGray
            if (c.IsEmpty && String.Equals(htmlColor, "LightGrey", StringComparison.OrdinalIgnoreCase))
                c = Color.LightGray;

            // System color
            if (c.IsEmpty)
            {
                object o = HtmlSysColorTable[htmlColor.ToLower(CultureInfo.InvariantCulture)];
                if (o != null)
                    c = (Color)o;
            }

            // resort to type converter which will handle named colors
            if (c.IsEmpty)
                c = (Color)TypeDescriptor.GetConverter(typeof(Color)).ConvertFromString(htmlColor);

            return c;
        }
    }
}