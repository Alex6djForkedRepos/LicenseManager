using System;
using System.IO;

namespace LicenseManager_12noon;

/// <summary>
/// License Manager CLI - Create license files from .private files
/// </summary>
public static class Program
{
	/// <summary>
	/// Main entry point for the CLI application.
	/// </summary>
	/// <param name="args">Command line arguments</param>
	/// <returns>Exit code: 0 for success, 1 for failure</returns>
	public static int Main(string[] args)
	{
		try
		{
			// Parse command line arguments
			var parsedArgs = CliArgumentParser.Parse(args);
			
			// Show help if requested or no arguments provided
			if (parsedArgs.HelpRequested || args.Length == 0)
			{
				CliArgumentParser.ShowHelp();
				return 0;
			}
			
			// Validate arguments
			parsedArgs.Validate();
			
			// Create license manager and load private file
			var manager = new CoreLicenseManager();
			
			Console.WriteLine($"Loading private file: {parsedArgs.PrivateFilePath}");
			manager.LoadKeypair(parsedArgs.PrivateFilePath);
			
			Console.WriteLine($"Loaded license information for: {manager.Product}");
			Console.WriteLine($"Customer: {manager.Name} ({manager.Email})");
			Console.WriteLine($"Product Version: {manager.Version}");
			Console.WriteLine($"License Type: {manager.StandardOrTrial}");
			Console.WriteLine($"Quantity: {manager.Quantity}");
			Console.WriteLine($"Expiration Days: {manager.ExpirationDays}");
			
			// Apply CLI overrides
			parsedArgs.ApplyOverrides(manager);
			
			// Show what was overridden
			bool hasOverrides = parsedArgs.LicenseType.HasValue || parsedArgs.Quantity.HasValue ||
			                   parsedArgs.ExpirationDays.HasValue || parsedArgs.ExpirationDate.HasValue ||
			                   !string.IsNullOrEmpty(parsedArgs.ProductVersion) || parsedArgs.ProductPublishDate.HasValue;
			
			if (hasOverrides)
			{
				Console.WriteLine();
				Console.WriteLine("Applied CLI overrides:");
				if (parsedArgs.LicenseType.HasValue)
					Console.WriteLine($"  License Type: {manager.StandardOrTrial}");
				if (parsedArgs.Quantity.HasValue)
					Console.WriteLine($"  Quantity: {manager.Quantity}");
				if (parsedArgs.ExpirationDays.HasValue)
					Console.WriteLine($"  Expiration Days: {manager.ExpirationDays}");
				if (parsedArgs.ExpirationDate.HasValue)
					Console.WriteLine($"  Expiration Date: {manager.ExpirationDateUTC:yyyy-MM-dd}");
				if (!string.IsNullOrEmpty(parsedArgs.ProductVersion))
					Console.WriteLine($"  Product Version: {manager.Version}");
				if (parsedArgs.ProductPublishDate.HasValue)
					Console.WriteLine($"  Product Publish Date: {manager.PublishDate}");
			}
			
			// Create license file
			Console.WriteLine();
			Console.WriteLine($"Creating license file: {parsedArgs.LicenseFilePath}");
			manager.SaveLicenseFile(parsedArgs.LicenseFilePath);
			
			Console.WriteLine("License file created successfully!");
			return 0;
		}
		catch (ArgumentException ex)
		{
			Console.Error.WriteLine($"Error: {ex.Message}");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Use --help for usage information.");
			return 1;
		}
		catch (FileNotFoundException ex)
		{
			Console.Error.WriteLine($"Error: File not found - {ex.Message}");
			return 1;
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.Error.WriteLine($"Error: Access denied - {ex.Message}");
			return 1;
		}
		catch (IOException ex)
		{
			Console.Error.WriteLine($"Error: I/O error - {ex.Message}");
			return 1;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Unexpected error: {ex.Message}");
			Console.Error.WriteLine($"Details: {ex}");
			return 1;
		}
	}
}
