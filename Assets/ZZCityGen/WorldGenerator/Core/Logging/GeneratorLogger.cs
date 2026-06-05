using System;
using System.IO;
using UnityEngine;

namespace ZZCityGen.WorldGenerator.Core.Logging
{
    public enum LogLevel { Info, Warning, Error }

    public static class GeneratorLogger
    {
        public static LogLevel Level = LogLevel.Info;
        private static string logFolder => Path.Combine(Application.dataPath, "ZZCityGen/WorldGenerator/GeneratedData/Logs");

        public static void Info(string tag, string message)
        {
            if (Level <= LogLevel.Info) Trace(LogLevel.Info, tag, message);
        }

        public static void Warn(string tag, string message)
        {
            if (Level <= LogLevel.Warning) Trace(LogLevel.Warning, tag, message);
        }

        public static void Error(string tag, string message)
        {
            if (Level <= LogLevel.Error) Trace(LogLevel.Error, tag, message);
        }

        private static void Trace(LogLevel level, string tag, string message)
        {
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{tag}] {message}";
            switch (level)
            {
                case LogLevel.Info: Debug.Log(text); break;
                case LogLevel.Warning: Debug.LogWarning(text); break;
                case LogLevel.Error: Debug.LogError(text); break;
            }
            try
            {
                Directory.CreateDirectory(logFolder);
                File.AppendAllText(Path.Combine(logFolder, "generator.log"), text + Environment.NewLine);
            }
            catch (Exception)
            {
                // swallow file IO errors to avoid breaking editor runtime
            }
        }
    }
}