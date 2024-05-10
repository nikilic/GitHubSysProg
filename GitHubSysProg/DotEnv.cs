using System;
using System.IO;
using System.Collections.Generic;

public class DotEnv
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {path}");
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            string[] split = line.Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                throw new FormatException($"Invalid line format: {line}");
            }

            string key = split[0].Trim();
            string value = split[1].Trim();

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}