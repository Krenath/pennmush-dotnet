namespace PennMUSH.Core.Help;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class HelpStore
{
    private readonly Dictionary<string, string> _topics = new(StringComparer.OrdinalIgnoreCase);

    public int TopicCount => _topics.Count;

    public void LoadFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        var files = Directory.GetFiles(directoryPath, "*.hlp", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            LoadFromFile(file);
        }
    }

    public void LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath);
        var currentTopics = new List<string>();
        var currentBody = new List<string>();

        void FlushCurrent()
        {
            if (currentTopics.Count > 0 && currentBody.Count > 0)
            {
                string text = string.Join("\r\n", currentBody).TrimEnd();
                foreach (var t in currentTopics)
                {
                    _topics[t] = text;
                }
            }
            currentTopics.Clear();
            currentBody.Clear();
        }

        foreach (var rawLine in lines)
        {
            if (rawLine.StartsWith('&'))
            {
                FlushCurrent();
                string header = rawLine.Substring(1).Trim();
                var topics = header.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var topic in topics)
                {
                    currentTopics.Add(topic);
                }
            }
            else
            {
                currentBody.Add(rawLine);
            }
        }

        FlushCurrent();
    }

    public string? Lookup(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = "help";
        }

        string trimmed = topic.Trim();

        // 1. Exact match
        if (_topics.TryGetValue(trimmed, out var text))
        {
            return text;
        }

        // 2. Prefix match
        var prefixMatches = _topics.Keys.Where(k => k.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)).ToList();
        if (prefixMatches.Count == 1)
        {
            return _topics[prefixMatches[0]];
        }
        else if (prefixMatches.Count > 1)
        {
            return $"Here are the entries matching '{trimmed}':\r\n" + string.Join(", ", prefixMatches.Take(20));
        }

        return null;
    }
}
