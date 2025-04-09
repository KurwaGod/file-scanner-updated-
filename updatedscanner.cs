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
                Console.WriteLine($"Error processing file: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    public class TextAnalyzer
    {
        private readonly Dictionary<byte[], string> _fileSignatures = new Dictionary<byte[], string>(new ByteArrayComparer())
        {
            { new byte[] { 0x25, 0x50, 0x44, 0x46 }, "PDF" },                             // %PDF
            { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "ZIP/DOCX/XLSX/PPTX" },              // PK
            { new byte[] { 0x3C, 0x3F, 0x78, 0x6D, 0x6C }, "XML" },                       // <?xml
            { new byte[] { 0x3C, 0x21, 0x44, 0x4F, 0x43, 0x54, 0x59, 0x50, 0x45 }, "HTML" } // <!DOCTYPE
        };

        private readonly Dictionary<string, string> _textSignatures = new Dictionary<string, string>
        {
            { "<!DOCTYPE html", "HTML" },
            { "<?xml", "XML" },
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
            
            // Get file type using binary and text checks
            string fileType = DetermineFileType(filePath, extension);
            
            var result = new StringBuilder();
            result.AppendLine($"File Analysis: {filePath}");
            result.AppendLine($"File Type: {fileType}");
            result.AppendLine($"File Size: {fileInfo.Length} bytes");
            result.AppendLine();

            // Skip text analysis for known binary formats
            if (IsBinaryFileType(fileType))
            {
                result.AppendLine("Text analysis skipped for binary file type");
                return result.ToString();
            }

            try
            {
                string content = File.ReadAllText(filePath);
                
                var charFrequency = GetCharacterFrequency(content);
                result.AppendLine("Character Analysis:");
                foreach (var pair in charFrequency.OrderByDescending(p => p.Value).Take(10))
                {
                    string charRepresentation = CharToReadableString(pair.Key);
                    result.AppendLine($"  {charRepresentation}: {pair.Value} occurrences");
                }
                result.AppendLine();

                if (wordsToFind.Length > 0)
                {
                    var wordMatches = FindWords(content, wordsToFind);
                    result.AppendLine("Word Matches:");
                    foreach (var word in wordsToFind)
                    {
                        int count = wordMatches.ContainsKey(word) ? wordMatches[word] : 0;
                        result.AppendLine($"  '{word}': {count} occurrences");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"Error analyzing file content: {ex.Message}");
            }

            return result.ToString();
        }

        private string CharToReadableString(char c)
        {
            if (char.IsControl(c) || char.IsWhiteSpace(c))
            {
                switch (c)
                {
                    case '\n': return "'\\n' (newline)";
                    case '\r': return "'\\r' (carriage return)";
                    case '\t': return "'\\t' (tab)";
                    case ' ': return "' ' (space)";
                    default: return $"'\\u{(int)c:X4}' (control character)";
                }
            }
            return $"'{c}'";
        }

        private bool IsBinaryFileType(string fileType)
        {
            string[] binaryTypes = { "PDF", "ZIP/DOCX/XLSX/PPTX" };
            return binaryTypes.Contains(fileType);
        }

        private string DetermineFileType(string filePath, string extension)
        {
            try
            {
                // First, try to identify by binary signature for file type 
                string binaryType = DetectBinarySignature(filePath);
                if (!string.IsNullOrEmpty(binaryType))
                {
                    return binaryType;
                }

                // Then try to detect based on text signatures in indivudal file types 
                string textType = DetectTextSignature(filePath);
                if (!string.IsNullOrEmpty(textType))
                {
                    return textType;
                }

                // Use additional content checks for specific formats
                if (IsJson(filePath))
                {
                    return "JSON";
                }
                if (IsCsv(filePath))
                {
                    return "CSV";
                }
                if (IsTsv(filePath))
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

        private string DetectBinarySignature(string filePath)
        {
            try
            {
                // Read first bytes to detect file signature
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[Math.Min(16, fs.Length)];
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead == 0)
                        return "Empty File";

                    foreach (var signature in _fileSignatures)
                    {
                        byte[] pattern = signature.Key;
                        if (bytesRead >= pattern.Length && buffer.Take(pattern.Length).SequenceEqual(pattern))
                        {
                            return signature.Value;
                        }
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string DetectTextSignature(string filePath)
        {
            try
            {
                // read first few lines to detect text-based signatures (redundant?)
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string firstLines = string.Empty;
                    for (int i = 0; i < 5 && !reader.EndOfStream; i++)
                    {
                        firstLines += reader.ReadLine() + Environment.NewLine;
                    }

                    foreach (var signature in _textSignatures)
                    {
                        if (firstLines.Contains(signature.Key))
                        {
                            return signature.Value;
                        }
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool IsJson(string filePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string firstChar = string.Empty;
                    // Skip whitespace
                    while (!reader.EndOfStream)
                    {
                        char c = (char)reader.Read();
                        if (!char.IsWhiteSpace(c))
                        {
                            firstChar = c.ToString();
                            break;
                        }
                    }
                    
                    // Check for JSON starting characters
                    return firstChar == "{" || firstChar == "[";
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsCsv(string filePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    // Check first few lines for CSV structure
                    for (int i = 0; i < 3 && !reader.EndOfStream; i++)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            continue;
                            
                        // Check if line contains commas with sensible field separation
                        if (line.Contains(",") && line.Count(c => c == ',') >= 2)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsTsv(string filePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    // Check first few lines for TSV structure
                    for (int i = 0; i < 3 && !reader.EndOfStream; i++)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            continue;
                            
                        // Check if line contains tabs with sensible field separation
                        if (line.Contains("\t") && line.Count(c => c == '\t') >= 2)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
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

            // Convert to lowercase for case-insensitive search
            string normalizedContent = content.ToLowerInvariant();
            
            foreach (var word in wordsToFind)
            {
                string normalizedWord = word.ToLowerInvariant();
                
                // Use word boundary regex function 
                string pattern = $@"\b{Regex.Escape(normalizedWord)}\b";
                int count = Regex.Matches(normalizedContent, pattern).Count;
                
                result[word] = count;
            }
            return result;
        }
    }

    // Custom comparer for byte arrays in dictionary
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            if (x == null || y == null)
                return x == y;
            if (x.Length != y.Length)
                return false;
            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null)
                return 0;
            int hash = 17;
            foreach (byte b in obj)
            {
                hash = hash * 31 + b;
            }
            return hash;
        }
    }
}

