using UnityEngine;
using System.Diagnostics;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TMG_EditorTools
{
    public class DebugC
    {
        private static List<(string filePath, int lineNumber, string message)> openedScriptPositions = new List<(string, int, string)>();
        //*****REPLACED BY USING FORMAT MSG + DEBUG.LOG (FINDS SCRIPT WHEN DOUBLE CLICKED BC THIS DOESN'T)
        public static void Log(string message, Color color, bool bold = false, bool italic = false, int fontSize = 12)
        {
            string formattedMessage = FormatMessage(message, color, fontSize, bold, italic);
            UnityEngine.Debug.Log(formattedMessage);
            LogScriptInfo(message);
        }

        public static void LogWarning(string message, Color color, bool bold = false, bool italic = false, int fontSize = 12)
        {
            string formattedMessage = FormatMessage(message, color, fontSize, bold, italic);
            UnityEngine.Debug.LogWarning(formattedMessage);
            LogScriptInfo(message);
        }

        public static void LogError(string message, Color color, bool bold = false, bool italic = false, int fontSize = 12)
        {
            string formattedMessage = FormatMessage(message, color, fontSize, bold, italic);
            UnityEngine.Debug.LogError(formattedMessage);
            LogScriptInfo(message);
        }
        static void LogScriptInfo(string message)
        {
            StackTrace stackTrace = new StackTrace(true);
            // The frame at index 1 will be the caller of the Log* method
            StackFrame frame = stackTrace.GetFrame(2);
            string scriptFilePath = frame.GetFileName();
            int lineNumber = frame.GetFileLineNumber();


            UpdateScriptPosition(scriptFilePath, lineNumber, message);
        }
        //*********************************************************************************************************

        static void UpdateScriptPosition(string filePath, int lineNumber, string message)
        {
            string relativePath = GetRelativePath(filePath);

            if (!openedScriptPositions.Any(entry => entry.filePath == relativePath && entry.lineNumber == lineNumber && entry.message == message))//check if already exists
            {
                openedScriptPositions.Add((relativePath, lineNumber, message));

            }
        }

        static string GetRelativePath(string fullPath)
        {
            int assetsIndex = fullPath.IndexOf("Assets");
            if (assetsIndex >= 0)
            {
                return fullPath.Substring(assetsIndex);
            }
            else
            {
                // If "Assets" is not found, return the full path
                return fullPath;
            }
        }

        public static List<(string filePath, int lineNumber, string message)> GetOpenedScriptPositions()
        {
            return openedScriptPositions;
        }

        public static string FormatMessage(
            string message,
            Color color = default, // or Color.white
            int fontSize = 14,
            bool bold = false,
            bool italic = false)
        {
            // Your formatting logic here. For illustration, let's just build a string:
            string colorHex = ColorUtility.ToHtmlStringRGBA(color == default ? Color.white : color);

            // Example of constructing an HTML-like formatted string:
            // <color=#RRGGBBAA><size=xx><b><i>Your message</i></b></size></color>
            string formattedMessage = $"<color=#{colorHex}><size={fontSize}>";
            if (bold) formattedMessage += "<b>";
            if (italic) formattedMessage += "<i>";

            formattedMessage += message;

            if (italic) formattedMessage += "</i>";
            if (bold) formattedMessage += "</b>";
            formattedMessage += "</size></color>";

            return formattedMessage;
        }

    }
}