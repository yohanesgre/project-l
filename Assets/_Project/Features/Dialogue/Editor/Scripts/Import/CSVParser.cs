using System;
using System.Collections.Generic;
using System.Text;
using MyGame.Features.Dialogue.Models;
using UnityEngine;

namespace MyGame.Features.Dialogue.Editor.Import
{
    /// <summary>
    /// Utility class for parsing CSV data from Google Sheets into DialogueEntry objects.
    /// Handles quoted fields, multiline text, and proper CSV escaping.
    /// </summary>
    public static class CSVParser
    {
        // Expected column headers (case-insensitive)
        private static readonly string[] ExpectedHeaders = 
        {
            "Text_ID", "Scene_ID", "Order", "Speaker", "Expression", 
            "Dialogue_Text", "Event_Tag", "Next_ID", "Notes"
        };

        /// <summary>
        /// Parses CSV content into a list of DialogueEntry objects.
        /// </summary>
        /// <param name="csvContent">Raw CSV string from Google Sheets.</param>
        /// <returns>List of parsed dialogue entries.</returns>
        public static List<DialogueEntry> Parse(string csvContent)
        {
            var entries = new List<DialogueEntry>();
            var lines = ParseCSVLines(csvContent);

            if (lines.Count == 0)
            {
                Debug.LogWarning("[CSVParser] Empty CSV content");
                return entries;
            }

            // Parse header row
            var headers = ParseCSVRow(lines[0]);
            var headerMap = BuildHeaderMap(headers);

            // Parse data rows
            for (int i = 1; i < lines.Count; i++)
            {
                var values = ParseCSVRow(lines[i]);
                if (values.Length == 0 || IsEmptyRow(values)) continue;

                var entry = MapRowToEntry(headerMap, values);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            Debug.Log($"[CSVParser] Parsed {entries.Count} dialogue entries");
            return entries;
        }

        /// <summary>
        /// Splits CSV content into lines, handling multiline quoted fields.
        /// </summary>
        private static List<string> ParseCSVLines(string csvContent)
        {
            var lines = new List<string>();
            var currentLine = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in csvContent)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    currentLine.Append(c);
                }
                else if (c == '\n' && !inQuotes)
                {
                    var line = currentLine.ToString().Trim('\r');
                    if (!string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                    }
                    currentLine.Clear();
                }
                else
                {
                    currentLine.Append(c);
                }
            }

            // Add last line if not empty
            var lastLine = currentLine.ToString().Trim('\r');
            if (!string.IsNullOrEmpty(lastLine))
            {
                lines.Add(lastLine);
            }

            return lines;
        }

        /// <summary>
        /// Parses a single CSV row into an array of field values.
        /// Handles quoted fields with embedded commas and escaped quotes.
        /// </summary>
        private static string[] ParseCSVRow(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // Check for escaped quote ("")
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString().Trim());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            // Add last value
            values.Add(currentValue.ToString().Trim());

            return values.ToArray();
        }

        /// <summary>
        /// Builds a map from column name to index.
        /// </summary>
        private static Dictionary<string, int> BuildHeaderMap(string[] headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim();
                if (!string.IsNullOrEmpty(header))
                {
                    map[header] = i;
                }
            }

            // Log missing expected headers
            foreach (var expected in ExpectedHeaders)
            {
                if (!map.ContainsKey(expected))
                {
                    Debug.LogWarning($"[CSVParser] Missing expected column: {expected}");
                }
            }

            return map;
        }

        /// <summary>
        /// Maps a row of values to a DialogueEntry using the header map.
        /// </summary>
        private static DialogueEntry MapRowToEntry(Dictionary<string, int> headerMap, string[] values)
        {
            var entry = new DialogueEntry();

            entry.TextID = GetValue(headerMap, values, "Text_ID");
            entry.SceneID = GetValue(headerMap, values, "Scene_ID");
            entry.Speaker = GetValue(headerMap, values, "Speaker");
            entry.Expression = GetValue(headerMap, values, "Expression");
            entry.DialogueText = GetValue(headerMap, values, "Dialogue_Text");
            entry.EventTag = GetValue(headerMap, values, "Event_Tag");
            entry.NextID = GetValue(headerMap, values, "Next_ID");
            entry.Notes = GetValue(headerMap, values, "Notes");

            // Parse Order as int
            var orderStr = GetValue(headerMap, values, "Order");
            if (int.TryParse(orderStr, out int order))
            {
                entry.Order = order;
            }

            // Skip entries without TextID
            if (string.IsNullOrEmpty(entry.TextID))
            {
                return null;
            }

            return entry;
        }

        /// <summary>
        /// Gets a value from the row by column name.
        /// </summary>
        private static string GetValue(Dictionary<string, int> headerMap, string[] values, string columnName)
        {
            if (headerMap.TryGetValue(columnName, out int index) && index < values.Length)
            {
                return values[index];
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if a row is effectively empty (all values are whitespace).
        /// </summary>
        private static bool IsEmptyRow(string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
