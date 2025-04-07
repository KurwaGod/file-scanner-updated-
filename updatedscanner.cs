using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TextFormatAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: TextFormatAnalyzer <filepath> [output filepath] [words to find...]");
                return;
            }

            string inputPath = args[0];
            string outputPath = args.Length > 1 ? args[1] : null;
            string[] wordsToFind = args.Length > 2 ? args.Skip(2).ToArray() : new string[0];

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: File {inputPath} not found.");
                return;
            }

            try
            {
                var analyzer = new TextAnalyzer();
                var result = analyzer.AnalyzeFile(inputPath, wordsToFind);

                if (outputPath != null)
                {
                    File.WriteAllText(outputPath, result);
                    Console.WriteLine($"Analysis saved to {outputPath}");
                }
                else
                {
                    Console.WriteLine(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    public class TextAnalyzer
    {
        private readonly Dictionary<string, string> _fileSignatures = new Dictionary<string, string>
        {
            { "%PDF", "PDF" },
            { "PK", "ZIP/DOCX/XLSX/PPTX" },
            { "<!DOCTYPE html", "HTML" },
            { "<?xml", "XML" },
            { "{", "JSON" },
            { "\\documentclass", "LaTeX" },
            { "---", "YAML" },
            { "BEGIN:VCALENDAR", "iCalendar" },
            { "<?php", "PHP" },
            { "#!/", "Script" }
        };

        public string AnalyzeFile(string filePath, string[] wordsToFind)
        {
            var fileInfo = new FileInfo(filePath);
            var extension = fileInfo.Extension.ToLowerInvariant();
            string fileType = DetermineFileType(filePath, extension);
            
            var content = File.ReadAllText(filePath);
            var charFrequency = GetCharacterFrequency(content);
            var wordMatches = FindWords(content, wordsToFind);

            var result = new StringBuilder();
            result.AppendLine($"File Analysis: {filePath}");
            result.AppendLine($"File Type: {fileType}");
            result.AppendLine($"File Size: {fileInfo.Length} bytes");
            result.AppendLine();

            result.AppendLine("Character Analysis:");
            foreach (var pair in charFrequency.OrderByDescending(p => p.Value).Take(10))
            {
                result.AppendLine($"  '{pair.Key}': {pair.Value} occurrences");
            }
            result.AppendLine();

            if (wordsToFind.Length > 0)
            {
                result.AppendLine("Word Matches:");
                foreach (var word in wordsToFind)
                {
                    int count = wordMatches.ContainsKey(word) ? wordMatches[word] : 0;
                    result.AppendLine($"  '{word}': {count} occurrences");
                }
            }

            return result.ToString();
        }

        private string DetermineFileType(string filePath, string extension)
        {
            try
            {
                // Read first 1024 bytes to detect file signature
                byte[] buffer = new byte[1024];
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(buffer, 0, buffer.Length);
                }
                
                string header = Encoding.ASCII.GetString(buffer);

                foreach (var signature in _fileSignatures)
                {
                    if (header.StartsWith(signature.Key))
                    {
                        return signature.Value;
                    }
                }

                // Check for CSV
                if (header.Contains(",") && Regex.IsMatch(header, @"[^,]+,[^,]+"))
                {
                    return "CSV";
                }

                // Check for TSV
                if (header.Contains("\t") && Regex.IsMatch(header, @"[^\t]+\t[^\t]+"))
                {
                    return "TSV";
                }

                // Default to extension-based detection
                switch (extension)
                {
                    case ".txt": return "Plain Text";
                    case ".md": return "Markdown";
                    case ".json": return "JSON";
                    case ".xml": return "XML";
                    case ".csv": return "CSV";
                    case ".html": return "HTML";
                    case ".htm": return "HTML";
                    case ".css": return "CSS";
                    case ".js": return "JavaScript";
                    case ".py": return "Python";
                    case ".java": return "Java";
                    case ".cs": return "C#";
                    case ".c": return "C";
                    case ".cpp": return "C++";
                    case ".h": return "Header";
                    case ".sh": return "Shell Script";
                    case ".bat": return "Batch File";
                    case ".ps1": return "PowerShell";
                    case ".sql": return "SQL";
                    case ".yaml": return "YAML";
                    case ".yml": return "YAML";
                    case ".ini": return "INI";
                    case ".config": return "Configuration";
                    case ".log": return "Log File";
                    case ".rtf": return "Rich Text Format";
                    default: return "Unknown";
                }
            }
            catch (Exception)
            {
                return "Unknown/Unreadable";
            }
        }

        private Dictionary<char, int> GetCharacterFrequency(string content)
        {
            var frequency = new Dictionary<char, int>();
            foreach (char c in content)
            {
                if (frequency.ContainsKey(c))
                {
                    frequency[c]++;
                }
                else
                {
                    frequency[c] = 1;
                }
            }
            return frequency;
        }

        private Dictionary<string, int> FindWords(string content, string[] wordsToFind)
        {
            var result = new Dictionary<string, int>();
            if (wordsToFind.Length == 0) return result;

            foreach (var word in wordsToFind)
            {
                int count = Regex.Matches(content, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase).Count;
                result[word] = count;
            }
            return result;
        }
    }
}
