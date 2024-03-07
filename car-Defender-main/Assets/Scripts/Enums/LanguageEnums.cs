using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LanguageEnums
{
    public enum LanguageId
    {
        English,
    }

    private readonly static string[] LanguageKey =
    {
        "en",
    };

    public static string GetLanguageDevice ()
    {
        var language = Application.systemLanguage;

        switch (language)
        {
            default:
                return GetLanguageKey (LanguageId.English);
        }
    }

    public static string GetLanguageSupportDefault ()
    {
        var language = Application.systemLanguage;

        switch (language)
        {
            default:
                return GetLanguageKey (LanguageId.English);
        }
    }

    public static string GetLanguageKey (LanguageId id)
    {
        return LanguageKey[(int) id];
    }

    public static LanguageId GetLanguageId (string language_code)
    {
        for (int i = 0; i < LanguageKey.Length; i++)
        {
            if (string.CompareOrdinal (LanguageKey[i], language_code) == 0)
            {
                return (LanguageId) i;
            }
        }

        return LanguageId.English;
    }
}