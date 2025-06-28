using LicenseManager_12noon.Client;
using Standard.Licensing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace LicenseManager_12noon;

/// <summary>
/// CLI argument parser and validator for License Manager.
/// </summary>
public class CliArgumentParser
{
	public string PrivateFilePath { get; set; } = string.Empty;
	public string LicenseFilePath { get; set; } = string.Empty;

	// Optional overrides
	public LicenseType? LicenseType { get; set; }
	public int? Quantity { get; set; }
	public int? ExpirationDays { get; set; }
	public DateTime? ExpirationDate { get; set; }
	public string? ProductVersion { get; set; }
	public DateOnly? ProductPublishDate { get; set; }
	public string? LockPath { get; set; }
	public Dictionary<string, string> ProductFeatures { get; set; } = [];
	public Dictionary<string, string> LicenseAttributes { get; set; } = [];

	public bool HelpRequested { get; set; }

	/// <summary>
	/// Parse command line arguments.
	/// </summary>
	/// <param name="args">Command line arguments</param>
	/// <returns>Parsed arguments</returns>
	/// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
	public static CliArgumentParser Parse(string[] args)
	{
		var parser = new CliArgumentParser();

		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i].ToLowerInvariant())
			{
				case "--private":
				case "-p":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --private argument");
					}
					parser.PrivateFilePath = args[++i];
					break;

				case "--license":
				case "-l":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --license argument");
					}
					parser.LicenseFilePath = args[++i];
					break;

				case "--type":
				case "-t":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --type argument");
					}
					string typeValue = args[++i];
					if (!Enum.TryParse<LicenseType>(typeValue, true, out var licenseType))
					{
						throw new ArgumentException($"Invalid license type '{typeValue}'. Valid values: Standard, Trial");
					}
					parser.LicenseType = licenseType;
					break;

				case "--quantity":
				case "-q":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --quantity argument");
					}
					if (!int.TryParse(args[++i], out var quantity) || quantity < 1)
					{
						throw new ArgumentException("Quantity must be a positive integer");
					}
					parser.Quantity = quantity;
					break;

				case "--expiration-days":
				case "-dy":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --expiration-days argument");
					}
					if (!int.TryParse(args[++i], out var expirationDays) || expirationDays < 0)
					{
						throw new ArgumentException("Expiration days must be zero or a positive integer");
					}
					parser.ExpirationDays = expirationDays;
					break;

				case "--expiration-date":
				case "-dt":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --expiration-date argument");
					}
					if (!DateTime.TryParse(args[++i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var expirationDate))
					{
						throw new ArgumentException("Invalid expiration-date format. Use YYYY-MM-DD format");
					}
					parser.ExpirationDate = expirationDate;
					break;

				case "--product-version":
				case "-v":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --product-version argument");
					}
					parser.ProductVersion = args[++i];
					break;

				case "--product-publish-date":
				case "-pd":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --product-publish-date argument");
					}
					if (!DateOnly.TryParse(args[++i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var publishDate))
					{
						throw new ArgumentException("Invalid product-publish date format. Use YYYY-MM-DD format");
					}
					parser.ProductPublishDate = publishDate;
					break;

				case "--lock":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --lock argument");
					}
					parser.LockPath = args[++i];
					break;

				case "--product-features":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --product-features argument");
					}
					ParseKeyValuePairs(args[++i], parser.ProductFeatures, "product features");
					break;

				case "--license-attributes":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --license-attributes argument");
					}
					ParseKeyValuePairs(args[++i], parser.LicenseAttributes, "license attributes");
					break;

				case "--help":
				case "-h":
				case "/?":
					parser.HelpRequested = true;
					break;

				default:
					throw new ArgumentException($"Unknown argument: {args[i]}");
			}
		}

		return parser;
	}

	/// <summary>
	/// Parse key=value pairs from a string.
	/// </summary>
	/// <param name="input">Input string containing space-separated key=value pairs</param>
	/// <param name="dictionary">Dictionary to add the parsed pairs to</param>
	/// <param name="argumentName">Name of the argument for error messages</param>
	/// <exception cref="ArgumentException">Thrown when parsing fails</exception>
	private static void ParseKeyValuePairs(string input, Dictionary<string, string> dictionary, string argumentName)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return;
		}

		string[] pairs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//TODO? Use MultilineTextToDictionary to parse key=value
// join pairs with Newline
// call ConvertTextToDictionary()
		foreach (string pair in pairs)
		{
			int equalsIndex = pair.IndexOf('=');
			if (equalsIndex == -1)
			{
				throw new ArgumentException($"Invalid {argumentName} format: '{pair}'. Expected key=value format.");
			}

			string key = pair[..equalsIndex].Trim();
			string value = pair[(equalsIndex + 1)..].Trim();

			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException($"Invalid {argumentName} format: '{pair}'. Key cannot be empty.");
			}

			dictionary[key] = value;
		}
	}

	/// <summary>
	/// Validate parsed arguments.
	/// </summary>
	/// <exception cref="ArgumentException">Thrown when validation fails</exception>
	public void Validate()
	{
		if (HelpRequested)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(PrivateFilePath))
		{
			throw new ArgumentException("Private file path is required. Use --private or -p argument.");
		}

		if (string.IsNullOrWhiteSpace(LicenseFilePath))
		{
			throw new ArgumentException("License file path is required. Use --license or -l argument.");
		}

		if (!File.Exists(PrivateFilePath))
		{
			throw new ArgumentException($"Private file does not exist: {PrivateFilePath}");
		}

		if (File.Exists(LicenseFilePath))
		{
			throw new ArgumentException($"License file already exists and will not be overwritten: {LicenseFilePath}");
		}

		// Make sure directory exists for the license file
		string? licenseDir = Path.GetDirectoryName(LicenseFilePath);
		if (!string.IsNullOrEmpty(licenseDir) && !Directory.Exists(licenseDir))
		{
			throw new ArgumentException($"Directory does not exist for license file: {licenseDir}");
		}

		// Validate mutually exclusive expiration options
		if ((ExpirationDays.HasValue) && (ExpirationDate.HasValue))
		{
			throw new ArgumentException("Cannot specify both --expiration-days and --expiration-date. Use only one.");
		}

		// Validate lock file exists if specified
		if (!string.IsNullOrWhiteSpace(LockPath) && !File.Exists(LockPath))
		{
			throw new ArgumentException($"Lock file does not exist: {LockPath}");
		}

		// Validate product features
		foreach (var feature in ProductFeatures)
		{
			if (LicenseManager.IsReservedFeatureName(feature.Key))
			{
				throw new ArgumentException($"'{feature.Key}' is a reserved product feature name and cannot be used.");
			}
		}

		// Validate license attributes
		foreach (var attribute in LicenseAttributes)
		{
			if (LicenseManager.IsReservedAttributeName(attribute.Key))
			{
				throw new ArgumentException($"'{attribute.Key}' is a reserved license attribute name and cannot be used.");
			}
		}
	}

	/// <summary>
	/// Apply CLI overrides to the license manager.
	/// </summary>
	/// <param name="manager">License manager to apply overrides to</param>
	public void ApplyOverrides(LicenseManager manager)
	{
		if ((LicenseType.HasValue) && (manager.StandardOrTrial != LicenseType.Value))
		{
			manager.StandardOrTrial = LicenseType.Value;
		}

		if ((Quantity.HasValue) && (manager.Quantity != Quantity.Value))
		{
			manager.Quantity = Quantity.Value;
		}

		if ((ExpirationDays.HasValue) && (manager.ExpirationDays != ExpirationDays.Value))
		{
			manager.ExpirationDays = ExpirationDays.Value;
			// ExpirationDateUTC is automatically updated by the property change handler
		}
		else if ((ExpirationDate.HasValue) && (manager.ExpirationDateUTC != ExpirationDate.Value))
		{
			manager.ExpirationDateUTC = ExpirationDate.Value;
			manager.ExpirationDays = (int)(ExpirationDate.Value - MyNow.UtcNow().Date).TotalDays;
		}

		if ((!string.IsNullOrWhiteSpace(ProductVersion)) && (manager.Version != ProductVersion))
		{
			manager.Version = ProductVersion;
		}

		if ((ProductPublishDate.HasValue) && (manager.PublishDate != ProductPublishDate.Value))
		{
			manager.PublishDate = ProductPublishDate.Value;
		}

		// Apply lock path if specified
		if (!string.IsNullOrWhiteSpace(LockPath))
		{
			if (manager.PathAssembly != LockPath)
			{
				manager.PathAssembly = LockPath;
				manager.IsLockedToAssembly = true;
			}
		}

		// Apply product features if any specified
		if (ProductFeatures.Count > 0)
		{
			// Create a new dictionary with existing features plus new ones
			var newFeatures = new Dictionary<string, string>(manager.ProductFeatures);
			foreach (var feature in ProductFeatures)
			{
				newFeatures[feature.Key] = feature.Value;
			}
			manager.UpdateProductFeatures(newFeatures);
		}

		// Apply license attributes if any specified
		if (LicenseAttributes.Count > 0)
		{
			// Create a new dictionary with existing attributes plus new ones
			var newAttributes = new Dictionary<string, string>(manager.LicenseAttributes);
			foreach (var attribute in LicenseAttributes)
			{
				newAttributes[attribute.Key] = attribute.Value;
			}
			manager.UpdateLicenseAttributes(newAttributes);
		}
	}

	/// <summary>
	/// Show help text.
	/// </summary>
	public static void ShowHelp()
	{
		Console.WriteLine("License Manager CLI - Create license files from .private files");
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine("  licensemanager --private <path> --license <path> [options]");
		Console.WriteLine();
		Console.WriteLine("Required Arguments:");
		Console.WriteLine("  --private, -p <path>     Path to the .private file");
		Console.WriteLine("  --license, -l <path>     Path to the new .lic file (must not exist)");
		Console.WriteLine();
		Console.WriteLine("Optional Arguments:");
		Console.WriteLine("  --type, -t <type>                License type: Standard or Trial");
		Console.WriteLine("  --quantity, -q <number>          License quantity (positive integer)");
		Console.WriteLine("  --expiration-days, -dy <days>    Expiration in days (0 = no expiry)");
		Console.WriteLine("  --expiration-date, -dt <date>    Expiration date (YYYY-MM-DD format)");
		Console.WriteLine("  --product-version, -v <version>  Product version");
		Console.WriteLine("  --product-publish-date, -pd <date>  Product publish date (YYYY-MM-DD)");
		Console.WriteLine("  --lock <path>                    Lock license to a specific DLL or EXE file");
		Console.WriteLine("  --product-features <pairs>       Product features as key=value pairs");
		Console.WriteLine("  --license-attributes <pairs>     License attributes as key=value pairs");
		Console.WriteLine("  --help, -h                       Show this help");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  licensemanager -p my.private -l customer.lic");
		Console.WriteLine("  licensemanager -p my.private -l trial.lic --type Trial --expiration-days 30");
		Console.WriteLine("  licensemanager -p my.private -l enterprise.lic --quantity 100 --product-version 2.1.0");
		Console.WriteLine("  licensemanager -p my.private -l locked.lic --lock C:\\MyApp\\MyApp.exe");
		Console.WriteLine("  licensemanager -p my.private -l featured.lic --product-features \"Color=Blue Bird=Heron\"");
		Console.WriteLine("  licensemanager -p my.private -l attributed.lic --license-attributes \"Size=Large Color=Red\"");
		Console.WriteLine();
		Console.WriteLine("Notes:");
		Console.WriteLine("  - If the license file already exists, it will not be overwritten");
		Console.WriteLine("  - Cannot override: passphrase, keys, product name, customer info");
		Console.WriteLine("  - Either expiration-days or expiration-date can be specified, not both");
		Console.WriteLine("  - Key=value pairs should be space-separated: \"key1=value1 key2=value2\"");
		Console.WriteLine("  - Reserved feature names: Product, Version, Publish Date");
		Console.WriteLine("  - Reserved attribute names: Product Identity, Assembly Identity, Expiration Days");
	}
}
