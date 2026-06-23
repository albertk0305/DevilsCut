using System;
using System.Globalization;
using System.Text;

public class KoreanParticleFormatter : IFormatProvider, ICustomFormatter
{
    private static readonly KoreanParticleFormatter Instance = new KoreanParticleFormatter();

    public static string Format(string format, params object[] args)
    {
        if (format == null)
            return "";

        args = args ?? Array.Empty<object>();
        return string.Format(Instance, format, args);
    }

    public object GetFormat(Type formatType)
    {
        return formatType == typeof(ICustomFormatter) ? this : null;
    }

    public string Format(string format, object arg, IFormatProvider formatProvider)
    {
        string text = FormatArgument(format, arg);

        if (!TryGetParticle(format, text, out string particle))
            return text;

        if (!IsCurrentLanguageKorean())
            return text;

        return text + particle;
    }

    public static bool IsCurrentLanguageKorean()
    {
        return LocalizationManager.Instance != null
            && LocalizationManager.Instance.currentLanguage == LocalizationManager.Language.Korean;
    }

    private static string FormatArgument(string format, object arg)
    {
        if (arg == null)
            return "";

        if (!IsParticleFormat(format) && arg is IFormattable formattable)
            return formattable.ToString(format, CultureInfo.CurrentCulture);

        return arg.ToString();
    }

    private static bool TryGetParticle(string format, string text, out string particle)
    {
        particle = "";

        switch (format)
        {
            case "을를":
                particle = HasFinalConsonant(text) ? "을" : "를";
                return true;
            case "이가":
                particle = HasFinalConsonant(text) ? "이" : "가";
                return true;
            case "은는":
                particle = HasFinalConsonant(text) ? "은" : "는";
                return true;
            case "와과":
                particle = HasFinalConsonant(text) ? "과" : "와";
                return true;
            case "으로로":
                particle = HasFinalConsonant(text) && !HasFinalRieul(text) ? "으로" : "로";
                return true;
            case "아야":
                particle = HasFinalConsonant(text) ? "아" : "야";
                return true;
            case "이라라":
                particle = HasFinalConsonant(text) ? "이라" : "라";
                return true;
            default:
                return false;
        }
    }

    private static bool IsParticleFormat(string format)
    {
        return format == "을를"
            || format == "이가"
            || format == "은는"
            || format == "와과"
            || format == "으로로"
            || format == "아야"
            || format == "이라라";
    }

    private static bool HasFinalConsonant(string text)
    {
        char lastChar = GetLastMeaningfulChar(text);
        if (lastChar == '\0')
            return false;

        if (IsHangulSyllable(lastChar))
            return GetJong(lastChar) != 0;

        if (char.IsDigit(lastChar))
            return lastChar == '0'
                || lastChar == '1'
                || lastChar == '3'
                || lastChar == '6'
                || lastChar == '7'
                || lastChar == '8';

        return false;
    }

    private static bool HasFinalRieul(string text)
    {
        char lastChar = GetLastMeaningfulChar(text);
        return IsHangulSyllable(lastChar) && GetJong(lastChar) == 8;
    }

    private static bool IsHangulSyllable(char value)
    {
        return value >= 0xAC00 && value <= 0xD7A3;
    }

    private static int GetJong(char value)
    {
        return (value - 0xAC00) % 28;
    }

    private static char GetLastMeaningfulChar(string text)
    {
        string stripped = StripRichTextTags(text);
        for (int i = stripped.Length - 1; i >= 0; i--)
        {
            char value = stripped[i];
            if (char.IsWhiteSpace(value) || IsIgnoredTrailingPunctuation(value))
                continue;

            return value;
        }

        return '\0';
    }

    private static string StripRichTextTags(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0)
            return text ?? "";

        StringBuilder builder = new StringBuilder(text.Length);
        bool insideTag = false;
        for (int i = 0; i < text.Length; i++)
        {
            char value = text[i];
            if (value == '<')
            {
                insideTag = true;
                continue;
            }

            if (insideTag)
            {
                if (value == '>')
                    insideTag = false;

                continue;
            }

            builder.Append(value);
        }

        return builder.ToString();
    }

    private static bool IsIgnoredTrailingPunctuation(char value)
    {
        return value == '.'
            || value == ','
            || value == '!'
            || value == '?'
            || value == ':'
            || value == ';'
            || value == ')'
            || value == ']'
            || value == '}'
            || value == '"'
            || value == '\''
            || value == '”'
            || value == '’'
            || value == '。'
            || value == '、'
            || value == '！'
            || value == '？';
    }
}
