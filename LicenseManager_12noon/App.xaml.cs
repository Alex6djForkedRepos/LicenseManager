using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace LicenseManager_12noon;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	[LibraryImport("kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool AttachConsole(int dwProcessId);
	private const int ATTACH_PARENT_PROCESS = -1;

	[LibraryImport("kernel32.dll", EntryPoint = "FreeConsole", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool FreeConsole();


	/// <summary>
	/// Application startup handler that determines whether to run in CLI or GUI mode.
	/// </summary>
	/// <param name="e">Startup event arguments containing command line arguments</param>
	protected override void OnStartup(StartupEventArgs e)
	{
		// Check if command line arguments were passed
		if (e.Args.Length > 0)
		{
			AttachConsole(ATTACH_PARENT_PROCESS);

			// Run in CLI mode
			int exitCode = RunCliMode(e.Args);

			Console.Out.Flush();
			FreeConsole();

			Shutdown(exitCode);
			return;
		}

		base.OnStartup(e);

		// Run in GUI mode - create and show the main window
		Window? window = Activator.CreateInstance(typeof(MainWindow), nonPublic: true) as Window;
		window?.Show();
	}

	/// <summary>
	/// Execute the CLI functionality.
	/// </summary>
	/// <param name="args">Command line arguments</param>
	/// <returns>Exit code: 0 for success, 1 for failure</returns>
	private static int RunCliMode(string[] args)
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
			LicenseManager manager = new();

			Console.WriteLine($"Loading private file: {parsedArgs.PrivateFilePath}");
			manager.LoadKeypair(parsedArgs.PrivateFilePath);

			Console.WriteLine($"Loaded license information for: {manager.Product}");
			Console.WriteLine($"Product Version: {manager.Version}");
			// Display product features if any
			if (manager.ProductFeatures.Count > 0)
			{
				Console.WriteLine($"Product Features:");
				foreach (var feature in manager.ProductFeatures)
				{
					Console.WriteLine($"  {feature.Key} = {feature.Value}");
				}
			}

			Console.WriteLine($"Customer: {manager.Name} ({manager.Email})");
			Console.WriteLine($"License Type: {manager.StandardOrTrial}");
			Console.WriteLine($"Quantity: {manager.Quantity}");
			Console.WriteLine($"Expiration Days: {manager.ExpirationDays}");

			// Display license attributes if any
			if (manager.LicenseAttributes.Count > 0)
			{
				Console.WriteLine($"License Attributes:");
				foreach (var attribute in manager.LicenseAttributes)
				{
					Console.WriteLine($"  {attribute.Key} = {attribute.Value}");
				}
			}

			// Display lock file information if present
			if (manager.IsLockedToAssembly && !string.IsNullOrEmpty(manager.PathAssembly))
			{
				Console.WriteLine($"Lock File: {manager.PathAssembly}");
			}

			// Apply CLI overrides
			parsedArgs.ApplyOverrides(manager);

			// Show what was overridden
			bool hasOverrides = !string.IsNullOrEmpty(parsedArgs.ProductVersion) || parsedArgs.ProductPublishDate.HasValue ||
										(parsedArgs.ProductFeatures.Count > 0) ||
										parsedArgs.LicenseType.HasValue || parsedArgs.Quantity.HasValue ||
										parsedArgs.ExpirationDays.HasValue ||	parsedArgs.ExpirationDate.HasValue ||
										(parsedArgs.LicenseAttributes.Count > 0) ||
										!string.IsNullOrEmpty(parsedArgs.LockPath);

			if (hasOverrides)
			{
				Console.WriteLine();
				Console.WriteLine("Applied CLI overrides:");
				if (!string.IsNullOrEmpty(parsedArgs.ProductVersion))
				{
					Console.WriteLine($"  Product Version: {manager.Version}");
				}
				if (parsedArgs.ProductPublishDate.HasValue)
				{
					Console.WriteLine($"  Product Publish Date: {manager.PublishDate}");
				}
				if (parsedArgs.ProductFeatures.Count > 0)
				{
					Console.WriteLine($"  Product Features:");
					foreach (var feature in parsedArgs.ProductFeatures)
					{
						Console.WriteLine($"    {feature.Key} = {feature.Value}");
					}
				}
				if (parsedArgs.LicenseType.HasValue)
				{
					Console.WriteLine($"  License Type: {manager.StandardOrTrial}");
				}
				if (parsedArgs.Quantity.HasValue)
				{
					Console.WriteLine($"  Quantity: {manager.Quantity}");
				}
				if (parsedArgs.ExpirationDays.HasValue)
				{
					Console.WriteLine($"  Expiration Days: {manager.ExpirationDays}");
				}
				if (parsedArgs.ExpirationDate.HasValue)
				{
					Console.WriteLine($"  Expiration Date: {manager.ExpirationDateUTC:yyyy-MM-dd}");
				}
				if (parsedArgs.LicenseAttributes.Count > 0)
				{
					Console.WriteLine($"  License Attributes:");
					foreach (var attribute in parsedArgs.LicenseAttributes)
					{
						Console.WriteLine($"    {attribute.Key} = {attribute.Value}");
					}
				}
				if (!string.IsNullOrEmpty(parsedArgs.LockPath))
				{
					Console.WriteLine($"  Lock File: {manager.PathAssembly}");
				}
			}

			 // Save keypair if requested
			if (parsedArgs.SaveKeypair)
			{
				Console.WriteLine();
				Console.WriteLine($"Saving keypair file: {parsedArgs.PrivateFilePath}");
				manager.SaveKeypair(parsedArgs.PrivateFilePath);
				Console.WriteLine("Keypair file saved successfully.");
			}

			// Create license file if requested
			if (!string.IsNullOrWhiteSpace(parsedArgs.LicenseFilePath))
			{
				Console.WriteLine();
				Console.WriteLine($"Creating license file: {parsedArgs.LicenseFilePath}");
				manager.SaveLicenseFile(parsedArgs.LicenseFilePath);
				Console.WriteLine("License file created successfully.");
			}

			Debug.Assert(parsedArgs.SaveKeypair || !string.IsNullOrWhiteSpace(parsedArgs.LicenseFilePath));
			// If neither --license nor --save was specified, error (should be caught by validation)
			if (!parsedArgs.SaveKeypair && string.IsNullOrWhiteSpace(parsedArgs.LicenseFilePath))
			{
				Console.Error.WriteLine("Error: Either --license or --save must be specified.");
				return 1;
			}

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

