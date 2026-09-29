using System.Globalization;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>
    /// Number formatting for uGUI Text, mirroring the small subset of TMP's SetText syntax the screens use
    /// ("{0}", "{0:0}", "x{0}", "{0}/{1}"). Values are rounded to whole numbers. Call on events, not per frame.
    /// </summary>
    public static class TextExtensions
    {
        public static void SetText(this Text label, string format, float value)
        {
            label.text = string.Format(CultureInfo.InvariantCulture, Normalize(format), Round(value));
        }

        public static void SetText(this Text label, string format, float first, float second)
        {
            label.text = string.Format(CultureInfo.InvariantCulture, Normalize(format), Round(first), Round(second));
        }

        private static long Round(float value) => (long)System.Math.Round(value);

        private static string Normalize(string format) => format.Replace("{0:0}", "{0}");
    }
}
