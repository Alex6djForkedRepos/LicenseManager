using LicenseManager_12noon.Client;
using Standard.Licensing;
using Standard.Licensing.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace LicenseManager_12noon;

/// <summary>
/// Core license manager functionality without WPF dependencies for CLI usage.
/// </summary>
public class CoreLicenseManager
{
	public const string FileExtension_License = ".lic";
	public const string FileExtension_PrivateKey = ".private";

	private const string ELEMENT_NAME_ROOT = "private";
	private const string ATTRIBUTE_NAME_VERSION = "version";

	private const string ELEMENT_NAME_ID = "id";

	private const string ELEMENT_NAME_SECRET = "secret";
	private const string ELEMENT_NAME_PASSPHRASE = "passphrase";
	private const string ELEMENT_NAME_PRIVATEKEY = "private-key";

	private const string ELEMENT_NAME_APP = "application";
	private const string ELEMENT_NAME_PUBLICKEY = "public-key";
	private const string ELEMENT_NAME_PRODUCT_ID = "product-id";

	private const string ELEMENT_NAME_CUSTOMER = "customer";
	private const string ELEMENT_NAME_NAME = "name";
	private const string ELEMENT_NAME_EMAIL = "email";
	private const string ELEMENT_NAME_COMPANY = "company";

	private const string ELEMENT_NAME_PRODUCT = "product";
	private const string ELEMENT_NAME_PRODUCT_NAME = "product-name";
	private const string ELEMENT_NAME_VERSION = "version";
	private const string ELEMENT_NAME_PUBLISH_DATE = "publish-date-utc";

	private const string ELEMENT_NAME_PRODUCT_FEATURES = "product-features";
	private const string ELEMENT_NAME_FEATURE = "feature";
	private const string ATTRIBUTE_NAME_NAME = "name";
	private const string ATTRIBUTE_NAME_VALUE = "value";

	private const string ELEMENT_NAME_LICENSE = "license";
	private const string ELEMENT_NAME_STANDARD_OR_TRIAL = "standard-or-trial";
	private const string ELEMENT_NAME_EXPIRATION_DATE = "expiration-date";
	private const string ELEMENT_NAME_EXPIRATION_DAYS = "expiration-days";
	private const string ELEMENT_NAME_QUANTITY = "quantity";

	private const string ELEMENT_NAME_LICENSE_ATTRIBUTES = "license-attributes";
	private const string ELEMENT_NAME_ATTRIBUTE = "attribute";

	private const string ELEMENT_NAME_PATHASSEMBLY = "path-assembly";

	private const string ProductFeature_Name_Product = "Product";
	private const string ProductFeature_Name_Version = "Version";
	private const string ProductFeature_Name_PublishDate = "Publish Date";

	private const string Attribute_Name_ProductIdentity = "Product Identity";
	private const string Attribute_Name_AssemblyIdentity = "Assembly Identity";
	private const string Attribute_Name_ExpirationDays = "Expiration Days";

	private readonly LicenseFile _licenseFile = new();

	// Core properties
	public LicenseType StandardOrTrial { get; set; } = LicenseType.Standard;
	public DateTime ExpirationDateUTC { get; set; } = DateTime.MaxValue.Date;
	public int ExpirationDays { get; set; }
	public int Quantity { get; set; } = 1;

	public Guid Id { get; set; } = Guid.NewGuid();
	public string Product { get; set; } = string.Empty;
	public string Version { get; set; } = string.Empty;
	public DateOnly? PublishDate { get; set; }

	public Dictionary<string, string> ProductFeatures { get; set; } = [];
	public Dictionary<string, string> LicenseAttributes { get; set; } = [];

	// Properties that cannot be overridden via CLI
	public string Passphrase { get; private set; } = string.Empty;
	public string KeyPrivate { get; private set; } = string.Empty;
	public string KeyPublic { get; private set; } = string.Empty;
	public string ProductId { get; private set; } = string.Empty;
	public string Name { get; private set; } = string.Empty;
	public string Email { get; private set; } = string.Empty;
	public string Company { get; private set; } = string.Empty;

	public string PathAssembly { get; set; } = string.Empty;
	public bool IsLockedToAssembly { get; set; } = false;

	/// <summary>
	/// Load keypair from .private file.
	/// </summary>
	/// <param name="pathKeypair">Path to .private file</param>
	/// <exception cref="FileNotFoundException">The keypair file does not exist.</exception>
	public void LoadKeypair(string pathKeypair)
	{
		ConvertOldPrivateFile(pathKeypair);

		XDocument xmlDoc = XDocument.Load(pathKeypair);

		XElement root = xmlDoc.Element(ELEMENT_NAME_ROOT)!;

		XElement secret = root.Element(ELEMENT_NAME_SECRET)!;
		Passphrase = secret.Element(ELEMENT_NAME_PASSPHRASE)!.Value;
		KeyPrivate = secret.Element(ELEMENT_NAME_PRIVATEKEY)!.Value;

		Id = (Guid)root.Element(ELEMENT_NAME_ID)!;

		XElement app = root.Element(ELEMENT_NAME_APP)!;
		KeyPublic = app.Element(ELEMENT_NAME_PUBLICKEY)!.Value;
		ProductId = app.Element(ELEMENT_NAME_PRODUCT_ID)!.Value;

		XElement customer = root.Element(ELEMENT_NAME_CUSTOMER)!;
		Name = customer.Element(ELEMENT_NAME_NAME)!.Value;
		Email = customer.Element(ELEMENT_NAME_EMAIL)!.Value;
		Company = customer.Element(ELEMENT_NAME_COMPANY)!.Value;

		XElement product = root.Element(ELEMENT_NAME_PRODUCT)!;
		Product = product.Element(ELEMENT_NAME_PRODUCT_NAME)!.Value;
		Version = product.Element(ELEMENT_NAME_VERSION)!.Value;
		string publishDate = product.Element(ELEMENT_NAME_PUBLISH_DATE)!.Value;
		PublishDate = string.IsNullOrEmpty(publishDate) ? null : DateOnly.Parse(publishDate, CultureInfo.InvariantCulture);

		ProductFeatures.Clear();
		XElement? productFeatures = root.Element(ELEMENT_NAME_PRODUCT_FEATURES);
		if (productFeatures is not null)
		{
			foreach (var feature in productFeatures.Elements(ELEMENT_NAME_FEATURE))
			{
				string? name = feature.Attribute(ATTRIBUTE_NAME_NAME)?.Value;
				string? value = feature.Attribute(ATTRIBUTE_NAME_VALUE)?.Value;
				if (!string.IsNullOrEmpty(name))
				{
					ProductFeatures[name] = value ?? string.Empty;
				}
			}
		}

		XElement license = root.Element(ELEMENT_NAME_LICENSE)!;
		StandardOrTrial = Enum.Parse<LicenseType>(license.Element(ELEMENT_NAME_STANDARD_OR_TRIAL)!.Value);
		ExpirationDays = int.Parse(license.Element(ELEMENT_NAME_EXPIRATION_DAYS)!.Value);
		ExpirationDateUTC = DateTime.Parse(license.Element(ELEMENT_NAME_EXPIRATION_DATE)!.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
		Quantity = int.Parse(license.Element(ELEMENT_NAME_QUANTITY)!.Value);

		LicenseAttributes.Clear();
		XElement? licenseAttributes = root.Element(ELEMENT_NAME_LICENSE_ATTRIBUTES);
		if (licenseAttributes is not null)
		{
			foreach (var attribute in licenseAttributes.Elements(ELEMENT_NAME_ATTRIBUTE))
			{
				string? name = attribute.Attribute(ATTRIBUTE_NAME_NAME)?.Value;
				string? value = attribute.Attribute(ATTRIBUTE_NAME_VALUE)?.Value;
				if (!string.IsNullOrEmpty(name))
				{
					LicenseAttributes[name] = value ?? string.Empty;
				}
			}
		}

		XElement? pathAssembly = root.Element(ELEMENT_NAME_PATHASSEMBLY);
		PathAssembly = pathAssembly?.Value ?? string.Empty;
	}

	/// <summary>
	/// Save license file to specified path.
	/// </summary>
	/// <param name="pathLicense">Full path to license file: MyApplication.lic</param>
	/// <exception cref="ArgumentException">Thrown when required properties are missing</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when properties have invalid values</exception>
	public void SaveLicenseFile(string pathLicense)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(Passphrase, nameof(Passphrase));
		ArgumentException.ThrowIfNullOrWhiteSpace(KeyPrivate, nameof(KeyPrivate));
		ArgumentException.ThrowIfNullOrWhiteSpace(KeyPublic, nameof(KeyPublic));

		if (Id == Guid.Empty)
		{
			throw new ArgumentOutOfRangeException(nameof(Id), "Id must be a valid GUID.");
		}
		ArgumentException.ThrowIfNullOrWhiteSpace(ProductId, nameof(ProductId));
		ArgumentException.ThrowIfNullOrWhiteSpace(Product, nameof(Product));
		ArgumentException.ThrowIfNullOrWhiteSpace(Version, nameof(Version));
		if (Quantity < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(Quantity), "License quantity must be one or more.");
		}
		// Expiration optional but cannot be negative
		if (ExpirationDays < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(ExpirationDays), "Expiration days must be zero (no expiry) or positive.");
		}

		ArgumentException.ThrowIfNullOrWhiteSpace(Name, nameof(Name));
		ArgumentException.ThrowIfNullOrWhiteSpace(Email, nameof(Email));
		// Company optional

		/// Create a hash to verify that this license is associated with the caller.
		/// Without this, an attacker could use the license file with ANY product
		/// (because we save the public key in the license file--which we do to
		/// prevent an attacker from substituting their own public key in the
		/// assembly and creating their own licenses).
		string identityProduct = SecureHash.ComputeSHA256Hash(CreateProductIdentity(ProductId, KeyPublic));

		// Optionally, tie the license file to only THIS instance of the calling assembly.
		string identityAssembly = (IsLockedToAssembly && !string.IsNullOrWhiteSpace(PathAssembly)) ? SecureHash.ComputeSHA256HashFile(PathAssembly) : string.Empty;

		ILicenseBuilder licenseBuilder =
			License.New()
			.WithUniqueIdentifier(Id)
			.As(StandardOrTrial);
		
		if (ExpirationDays > 0)
		{
			// ExpiresAt() converts passed date/time to UTC and assigns to Expiration property.
			// If we do this with LocalTime, the expiration date will be off by the time zone offset.
			licenseBuilder.ExpiresAt(DateTime.UtcNow.Date.AddDays(ExpirationDays));
		}
		
		licenseBuilder
			.WithMaximumUtilization(Quantity)
			.WithProductFeatures(
				new Dictionary<string, string>(ProductFeatures)
				{
					[ProductFeature_Name_Product] = Product,
					[ProductFeature_Name_Version] = Version,
					[ProductFeature_Name_PublishDate] = PublishDate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
				}
			)
			.LicensedTo(Name, Email, (Customer c) =>
			{
				if (!string.IsNullOrWhiteSpace(Company))
				{
					c.Company = Company;
				}
			})
			.WithAdditionalAttributes(
				new Dictionary<string, string>(LicenseAttributes)
				{
					[Attribute_Name_ProductIdentity] = identityProduct,
					[Attribute_Name_AssemblyIdentity] = identityAssembly,
					[Attribute_Name_ExpirationDays] = (ExpirationDays == 0) ? string.Empty : ExpirationDays.ToString(),
				}
			);
		
		// InvalidCipherTextException probably means you changed the passphrase and did not generate a new keypair.
		License license = licenseBuilder.CreateAndSignWithPrivateKey(KeyPrivate, Passphrase);

		// Note: This emits a license file in clear text with an encrypted signature.
		File.WriteAllText(pathLicense, license.ToString(), Encoding.UTF8);
	}

	/// <summary>
	/// Convert old private and license files to the new format.
	/// </summary>
	/// <remarks>
	/// Feb 2025: Delete when no longer needed.
	/// </remarks>
	/// <param name="pathKeypair"></param>
	private void ConvertOldPrivateFile(string pathKeypair)
	{
		XDocument xmlDocKeypair = XDocument.Load(pathKeypair);
		XElement root = xmlDocKeypair.Element(ELEMENT_NAME_ROOT)!;
		XAttribute? versionAttribute = root.Attribute(ATTRIBUTE_NAME_VERSION);
		if (versionAttribute is not null)
		{
			return;
		}

		try
		{
			// Load properties from old private key file.
			Passphrase = root.Element("passphrase")!.Value;
			KeyPrivate = root.Element("private-key")!.Value;
			KeyPublic = root.Element("public-key")!.Value;
			Id = (Guid)root.Element("id")!;
			ProductId = root.Element("product-id")!.Value;
			Product = root.Element("product")!.Value;
			Version = root.Element("version")!.Value;
			string publishDate = root.Element("publish-date-utc")!.Value;
			PublishDate = string.IsNullOrEmpty(publishDate) ? null : DateOnly.Parse(publishDate, CultureInfo.InvariantCulture);
			Name = root.Element("name")!.Value;
			Email = root.Element("email")!.Value;
			Company = root.Element("company")!.Value;
			StandardOrTrial = Enum.Parse<LicenseType>(root.Element("standard-or-trial")!.Value);
			ExpirationDays = int.Parse(root.Element("expiration-days")!.Value);
			ExpirationDateUTC = DateTime.Parse(root.Element("expiration-date")!.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
			Quantity = int.Parse(root.Element("quantity")!.Value);
			PathAssembly = root.Element("path-assembly")?.Value ?? string.Empty;
		}
		catch (Exception ex)
		{
			throw new InvalidDataException($"Failed to convert old private file format: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Creates a product identity string by combining productId and publicKey.
	/// </summary>
	/// <param name="productId">Product identifier</param>
	/// <param name="publicKey">Public key</param>
	/// <returns>Combined product identity string</returns>
	private static string CreateProductIdentity(string productId, string publicKey)
	{
		return $"{productId}||{publicKey}";
	}
}