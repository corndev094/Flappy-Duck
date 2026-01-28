using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using UnityEngine;

public class EDebug
{
    private static EDebugSettings settings;

    static EDebug()
    {
        settings = Resources.Load<EDebugSettings>("EDebugSettings");
    }

    public static void Log(object message)
    {
        Debug.Log(FormatMessage(message));
    }

    public static void Log<T>(Expression<Func<T>> messageExpression)
    {
        if (messageExpression.Body is MemberExpression member)
        {
            var variableName = member.Member.Name;
            var value = messageExpression.Compile().Invoke();

            Debug.Log($"{variableName}: {FormatMessage(value)}");
        }
        else
        {
            // Fallback for expressions that are not member access
            var value = messageExpression.Compile().Invoke();
            Log(value);
        }
    }

    private static string FormatMessage(object message)
    {
        if (settings == null)
        {
            return message.ToString();
        }

        if (message is int)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(settings.intColor)}>{message}</color>";
        }
        else if (message is bool value)
        {
            if (value == true)
                return $"<color=#{ColorUtility.ToHtmlStringRGB(settings.boolTrueColor)}>{message}</color>";
            else
                return $"<color=#{ColorUtility.ToHtmlStringRGB(settings.boolFalseColor)}>❌ {message}</color>";
        }
        else if (message is string s)
        {
            var log = $"<color=#{ColorUtility.ToHtmlStringRGB(settings.stringColor)}>{s}</color>";
            string headingPattern = @"\[(\w*)\]";
            var headingMatches = Regex.Matches(s, headingPattern);
            if (headingMatches.Count > 0)
            {
                foreach (Match m in headingMatches)
                {
                    var colorizedHeading = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.yellow)}>{m.Value}</color>";
                    s = Regex.Replace(s, Regex.Escape(m.Value), colorizedHeading);
                }
                return s;
            }
            else
            {
                return log;
            }
        }
        else if (message is float)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(settings.floatColor)}>{message}</color>";
        }
        else if (message is System.Collections.IList list)
        {
            return $"List: <color=#{ColorUtility.ToHtmlStringRGB(settings.listColor)}>{string.Join(", ", list.Cast<object>())}</color>";
        }
        else
        {
            return message.ToString();
        }
    }
    
    public static void LogWarning(object message)
    {
        if (settings == null)
        {
            Debug.LogWarning(message);
            return;
        }
        Debug.LogWarning($"<color=#{ColorUtility.ToHtmlStringRGB(settings.warningColor)}>Waring: {message}</color>");
    }

    public static void LogError(object message)
    {
        if (settings == null)
        {
            Debug.LogError(message);
            return;
        }
        Debug.LogError($"<color=#{ColorUtility.ToHtmlStringRGB(settings.errorColor)}>❌ Error: {message}</color>");
    }
}