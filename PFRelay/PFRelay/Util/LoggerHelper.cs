using System;
using Dalamud.Plugin;

namespace PFRelay.Util
{
    public static class LoggerHelper
    {
        public static void LogError(string message, Exception ex = null,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            string fileName = System.IO.Path.GetFileName(filePath);
            string exceptionMessage = ex?.Message ?? "No exception provided";
            string stackTrace = ex?.StackTrace ?? "No stack trace available";

            Service.PluginLog.Error($"[{fileName}::{memberName} (Line {lineNumber})] {message}: {exceptionMessage}");
            Service.PluginLog.Error(stackTrace);
        }

        public static void LogDebug(string message,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            string fileName = System.IO.Path.GetFileName(filePath);
            Service.PluginLog.Debug($"[{fileName}::{memberName} (Line {lineNumber})] {message}");
        }
    }
}
