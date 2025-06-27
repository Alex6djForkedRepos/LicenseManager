using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Standard.Licensing;

namespace LicenseManager_12noon.UnitTests;

[TestClass]
public class CliArgumentParserTest
{
	[TestMethod]
	public void TestParseBasicArguments()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test.private", result.PrivateFilePath);
		Assert.AreEqual("test.lic", result.LicenseFilePath);
		Assert.IsFalse(result.HelpRequested);
	}

	[TestMethod]
	public void TestParseShortArguments()
	{
		// Arrange
		string[] args = ["-p", "test.private", "-l", "test.lic"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test.private", result.PrivateFilePath);
		Assert.AreEqual("test.lic", result.LicenseFilePath);
	}

	[TestMethod]
	public void TestParseAllArguments()
	{
		// Arrange
		string[] args = [
			"--private", "test.private",
			"--license", "test.lic",
			"--type", "Trial",
			"--quantity", "5",
			"--expiration-days", "30",
			"--product-version", "2.1.0",
			"--product-publish-date", "2023-12-01"
		];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test.private", result.PrivateFilePath);
		Assert.AreEqual("test.lic", result.LicenseFilePath);
		Assert.AreEqual(LicenseType.Trial, result.LicenseType);
		Assert.AreEqual(5, result.Quantity);
		Assert.AreEqual(30, result.ExpirationDays);
		Assert.AreEqual("2.1.0", result.ProductVersion);
		Assert.AreEqual(new DateOnly(2023, 12, 1), result.ProductPublishDate);
	}

	[TestMethod]
	public void TestParseHelp()
	{
		// Arrange
		string[] args = ["--help"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.IsTrue(result.HelpRequested);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void TestParseInvalidLicenseType()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--type", "Invalid"];

		// Act
		CliArgumentParser.Parse(args);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void TestParseInvalidQuantity()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--quantity", "0"];

		// Act
		CliArgumentParser.Parse(args);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void TestParseNegativeExpirationDays()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--expiration-days", "-1"];

		// Act
		CliArgumentParser.Parse(args);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void TestParseMissingPrivateValue()
	{
		// Arrange
		string[] args = ["--private"];

		// Act
		CliArgumentParser.Parse(args);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void TestParseUnknownArgument()
	{
		// Arrange
		string[] args = ["--unknown", "value"];

		// Act
		CliArgumentParser.Parse(args);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void TestValidateRequiredPrivateFile()
	{
		// Arrange
		var parser = new CliArgumentParser
		{
			LicenseFilePath = "test.lic"
		};

		// Act
		parser.Validate();
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void TestValidateRequiredLicenseFile()
	{
		// Arrange
		var parser = new CliArgumentParser
		{
			PrivateFilePath = "test.private"
		};

		// Act
		parser.Validate();
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void TestValidateMutuallyExclusiveExpirationOptions()
	{
		// Arrange
		var parser = new CliArgumentParser
		{
			PrivateFilePath = "test.private",
			LicenseFilePath = "test.lic",
			ExpirationDays = 30,
			ExpirationDate = DateTime.Now.AddDays(30)
		};

		// Act
		parser.Validate();
	}

	[TestMethod]
	public void TestApplyOverrides()
	{
		// Arrange
		var manager = new CoreLicenseManager
		{
			StandardOrTrial = LicenseType.Standard,
			Quantity = 1,
			ExpirationDays = 0,
			Version = "1.0.0"
		};

		var parser = new CliArgumentParser
		{
			LicenseType = LicenseType.Trial,
			Quantity = 5,
			ExpirationDays = 30,
			ProductVersion = "2.1.0",
			ProductPublishDate = new DateOnly(2023, 12, 1)
		};

		// Act
		parser.ApplyOverrides(manager);

		// Assert
		Assert.AreEqual(LicenseType.Trial, manager.StandardOrTrial);
		Assert.AreEqual(5, manager.Quantity);
		Assert.AreEqual(30, manager.ExpirationDays);
		Assert.AreEqual("2.1.0", manager.Version);
		Assert.AreEqual(new DateOnly(2023, 12, 1), manager.PublishDate);
	}

	[TestMethod]
	public void TestApplyExpirationDateOverride()
	{
		// Arrange
		var manager = new CoreLicenseManager();
		var expirationDate = DateTime.UtcNow.Date.AddDays(45);
		
		var parser = new CliArgumentParser
		{
			ExpirationDate = expirationDate
		};

		// Act
		parser.ApplyOverrides(manager);

		// Assert
		Assert.AreEqual(expirationDate, manager.ExpirationDateUTC);
		Assert.AreEqual(45, manager.ExpirationDays);
	}
}