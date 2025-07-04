using System;
using System.Collections.Generic;

namespace Shared;

public static class MultilineTextToDictionary
{
	/// <summary>
	/// Converts a multiline string with key=value pairs to a Dictionary.
	/// Each line should be in the format "key=value".
	/// </summary>
	/// <param name="text">The multiline string to convert.</param>
	/// <returns>A Dictionary with the parsed key-value pairs.</returns>
	public static Dictionary<string, string> ConvertTextToDictionary(string text)
	{
		Dictionary<string, string> dictionary = [];

		if (string.IsNullOrWhiteSpace(text))
		{
			return dictionary;
		}

		string[] lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		foreach (string line in lines)
		{
			string trimmedLine = line.Trim();
			if (string.IsNullOrEmpty(trimmedLine))
			{
				continue;
			}

			string key;
			string val;
			// Find first equals sign
			int equalsIndex = trimmedLine.IndexOf('=');
			if (equalsIndex == 0)
			{
				// Equals sign is the first character, so key is empty; skip this line
				continue;
			}
			else if (equalsIndex > 0) // Only allow if key is not empty
			{
				key = trimmedLine[..equalsIndex].Trim();
				val = (equalsIndex < trimmedLine.Length - 1)
						  ? trimmedLine[(equalsIndex + 1)..].Trim()
						  : string.Empty;
			}
			else
			{
				// No equals sign, treat the whole line as a key with an empty value
				key = trimmedLine;
				val = string.Empty;
			}

			dictionary[key] = val;
		}

		return dictionary;
	}

	/// <summary>
	/// Compare two dictionaries for equality
	/// </summary>
	public static bool DictionariesEqual(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
	{
		if (dict1.Count != dict2.Count)
		{
			return false;
		}

		foreach (var kvp in dict1)
		{
			if (!dict2.TryGetValue(kvp.Key, out string? value) || (value != kvp.Value))
			{
				return false;
			}
		}

		return true;
	}
}
