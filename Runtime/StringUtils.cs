using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class StringUtils
{
    public static string SecondsToString(float seconds)
    {
        string timeText = "";
        var timeSpan = System.TimeSpan.FromSeconds(seconds);
        if (timeSpan.Days != 0)
            timeText += timeSpan.Days + "d ";
        if (timeSpan.Hours != 0)
            timeText += timeSpan.Hours + "h ";
        if (timeSpan.Minutes != 0)
            timeText += timeSpan.Minutes + "m ";
        if (timeSpan.Seconds != 0 && timeSpan.Days == 0)
            timeText += timeSpan.Seconds + "s ";
        return timeText.Trim();
    }

    public static string GetHotkeyString(KeyCode[] keyCodes, string separator, string noneKeyCodeString)
    {
        string hotKeyText = "";

        Dictionary<KeyCode, string> keyCodeShortNames = new Dictionary<KeyCode, string> {
            { KeyCode.LeftControl, "Ctrl" },
            { KeyCode.RightControl, "Ctrl" },
            { KeyCode.LeftShift, "Shift" },
            { KeyCode.RightShift, "Shift" },
            { KeyCode.LeftAlt, "Alt" },
            { KeyCode.RightAlt, "Alt" },
            { KeyCode.Escape, "Esc" },
            { KeyCode.Return, "Enter" },
            { KeyCode.Backspace, "Backspace" },
            { KeyCode.Delete, "Del" },
            { KeyCode.Alpha0, "0" },
            { KeyCode.Alpha1, "1" },
            { KeyCode.Alpha2, "2" },
            { KeyCode.Alpha3, "3" },
            { KeyCode.Alpha4, "4" },
            { KeyCode.Alpha5, "5" },
            { KeyCode.Alpha6, "6" },
            { KeyCode.Alpha7, "7" },
            { KeyCode.Alpha8, "8" },
            { KeyCode.Alpha9, "9" },
            { KeyCode.Keypad0, "Num 0" },
            { KeyCode.Keypad1, "Num 1" },
            { KeyCode.Keypad2, "Num 2" },
            { KeyCode.Keypad3, "Num 3" },
            { KeyCode.Keypad4, "Num 4" },
            { KeyCode.Keypad5, "Num 5" },
            { KeyCode.Keypad6, "Num 6" },
            { KeyCode.Keypad7, "Num 7" },
            { KeyCode.Keypad8, "Num 8" },
            { KeyCode.Keypad9, "Num 9" },
            { KeyCode.KeypadDivide, "Num /" },
            { KeyCode.KeypadMultiply, "Num *" },
            { KeyCode.KeypadEnter, "Num Enter" },
            { KeyCode.KeypadMinus, "Num -/" },
            { KeyCode.KeypadPlus, "Num +/" },
            { KeyCode.Mouse2, "Mouse middle" },
        };

        if (keyCodes[0] == KeyCode.None)
        {
            hotKeyText = noneKeyCodeString;
        }
        else
        {
            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (keyCodeShortNames.TryGetValue(keyCodes[i], out string shortName))
                {
                    hotKeyText += shortName;
                }
                else
                {
                    hotKeyText += keyCodes[i].ToString();
                }

                if (i != keyCodes.Length - 1)
                    hotKeyText += separator;
            }
        }

        return hotKeyText;
    }

    public static string CamelCaseToNormalText(string camelCaseString)
    {
        if (string.IsNullOrEmpty(camelCaseString))
            return camelCaseString;

        StringBuilder result = new StringBuilder();
        result.Append(camelCaseString[0]);

        for (int i = 1; i < camelCaseString.Length; i++)
        {
            char c = camelCaseString[i];
            if (char.IsUpper(c))
            {
                result.Append(' ');
                result.Append(char.ToLower(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
