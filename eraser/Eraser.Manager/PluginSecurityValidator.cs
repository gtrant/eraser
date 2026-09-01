/* 
 * Enhanced Plugin Security Validator for Eraser
 * Copyright 2025 The Eraser Project
 * 
 * This file is part of Eraser.
 * 
 * Eraser is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 * 
 * Eraser is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * 
 * A copy of the GNU General Public License can be found at
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

using Eraser.Plugins;
using Eraser.Util;

namespace Eraser.Manager
{
	/// <summary>
	/// Enhanced security validator for plugin assemblies.
	/// </summary>
	public static class PluginSecurityValidator
	{
		/// <summary>
		/// Performs comprehensive security validation of a plugin.
		/// </summary>
		/// <param name="plugin">The plugin to validate.</param>
		/// <returns>True if the plugin passes all security checks.</returns>
		public static bool ValidatePluginSecurity(PluginInfo plugin)
		{
			if (plugin?.Assembly == null)
			{
				LogSecurityEvent(SecurityEventType.PluginValidationFailed, 
					"Plugin or assembly is null", null, SecuritySeverity.High);
				return false;
			}

			try
			{
				string assemblyPath = plugin.Assembly.Location;
				if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
				{
					LogSecurityEvent(SecurityEventType.PluginValidationFailed,
						$"Plugin assembly not found: {assemblyPath}", plugin.Assembly.FullName, SecuritySeverity.High);
					return false;
				}

				// 1. Verify strong name signature
				if (!ValidateStrongNameSignature(assemblyPath, plugin.Assembly.FullName))
					return false;

				// 2. Verify Authenticode signature
				if (!ValidateAuthenticodeSignature(assemblyPath, plugin.Assembly.FullName))
					return false;

				// 3. Validate certificate trust chain
				if (!ValidateCertificateTrust(assemblyPath, plugin.Assembly.FullName))
					return false;

				// 4. Scan for suspicious code patterns
				if (!ValidateCodeSafety(plugin.Assembly))
					return false;

				// 5. Validate plugin metadata and permissions
				if (!ValidatePluginMetadata(plugin))
					return false;

				LogSecurityEvent(SecurityEventType.PluginValidationSuccess,
					"Plugin passed all security validations", plugin.Assembly.FullName, SecuritySeverity.Information);

				return true;
			}
			catch (Exception ex)
			{
				LogSecurityEvent(SecurityEventType.PluginValidationError,
					$"Exception during plugin validation: {ex.Message}", 
					plugin.Assembly?.FullName, SecuritySeverity.High);
				return false;
			}
		}

		/// <summary>
		/// Validates the strong name signature of an assembly.
		/// </summary>
		/// <param name="assemblyPath">Path to the assembly.</param>
		/// <param name="assemblyName">Name of the assembly for logging.</param>
		/// <returns>True if the signature is valid.</returns>
		private static bool ValidateStrongNameSignature(string assemblyPath, string assemblyName)
		{
			try
			{
				if (!Security.VerifyStrongName(assemblyPath))
				{
					LogSecurityEvent(SecurityEventType.StrongNameValidationFailed,
						"Strong name signature verification failed", assemblyName, SecuritySeverity.High);
					return false;
				}

				// Verify the assembly has a public key
				Assembly assembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
				byte[] publicKey = assembly.GetName().GetPublicKey();
				
				if (publicKey == null || publicKey.Length == 0)
				{
					LogSecurityEvent(SecurityEventType.StrongNameValidationFailed,
						"Assembly does not have a public key", assemblyName, SecuritySeverity.High);
					return false;
				}

				// For core plugins, verify they have the same public key as the main assembly
				var mainAssemblyKey = Assembly.GetExecutingAssembly().GetName().GetPublicKey();
				if (IsCorePlugin(assemblyName) && !ArraysEqual(publicKey, mainAssemblyKey))
				{
					LogSecurityEvent(SecurityEventType.StrongNameValidationFailed,
						"Core plugin does not have matching public key", assemblyName, SecuritySeverity.Critical);
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				LogSecurityEvent(SecurityEventType.StrongNameValidationError,
					$"Error validating strong name: {ex.Message}", assemblyName, SecuritySeverity.High);
				return false;
			}
		}

		/// <summary>
		/// Validates the Authenticode signature of an assembly.
		/// </summary>
		/// <param name="assemblyPath">Path to the assembly.</param>
		/// <param name="assemblyName">Name of the assembly for logging.</param>
		/// <returns>True if the signature is valid.</returns>
		private static bool ValidateAuthenticodeSignature(string assemblyPath, string assemblyName)
		{
			try
			{
				if (!Security.VerifyAuthenticode(assemblyPath))
				{
					LogSecurityEvent(SecurityEventType.AuthenticodeValidationFailed,
						"Authenticode signature verification failed", assemblyName, SecuritySeverity.High);
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				LogSecurityEvent(SecurityEventType.AuthenticodeValidationError,
					$"Error validating Authenticode signature: {ex.Message}", assemblyName, SecuritySeverity.High);
				return false;
			}
		}

		/// <summary>
		/// Validates the certificate trust chain for the assembly.
		/// </summary>
		/// <param name="assemblyPath">Path to the assembly.</param>
		/// <param name="assemblyName">Name of the assembly for logging.</param>
		/// <returns>True if the certificate chain is trusted.</returns>
		private static bool ValidateCertificateTrust(string assemblyPath, string assemblyName)
		{
			try
			{
				X509Certificate2 cert = new X509Certificate2(assemblyPath);
				X509Chain chain = new X509Chain();
				
				// Configure chain policy for thorough validation
				chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
				chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
				chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
				chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);

				bool isValid = chain.Build(cert);

				if (!isValid)
				{
					StringBuilder errorDetails = new StringBuilder();
					foreach (X509ChainStatus status in chain.ChainStatus)
					{
						errorDetails.AppendLine($"- {status.Status}: {status.StatusInformation}");
					}

					LogSecurityEvent(SecurityEventType.CertificateValidationFailed,
						$"Certificate chain validation failed:\n{errorDetails}", assemblyName, SecuritySeverity.High);
					
					return false;
				}

				// Additional validation: check for known trusted publishers
				if (!IsFromTrustedPublisher(cert))
				{
					LogSecurityEvent(SecurityEventType.CertificateValidationWarning,
						"Assembly is not from a known trusted publisher", assemblyName, SecuritySeverity.Medium);
					// Note: This doesn't fail validation, just logs a warning
				}

				return true;
			}
			catch (Exception ex)
			{
				LogSecurityEvent(SecurityEventType.CertificateValidationError,
					$"Error validating certificate trust: {ex.Message}", assemblyName, SecuritySeverity.High);
				return false;
			}
		}

		/// <summary>
		/// Scans the assembly for suspicious code patterns.
		/// </summary>
		/// <param name="assembly">The assembly to scan.</param>
		/// <returns>True if no suspicious patterns are found.</returns>
		private static bool ValidateCodeSafety(Assembly assembly)
		{
			try
			{
				// Get all types in the assembly
				Type[] types = assembly.GetTypes();

				foreach (Type type in types)
				{
					// Check for suspicious type names
					if (ContainsSuspiciousTypeName(type.FullName))
					{
						LogSecurityEvent(SecurityEventType.SuspiciousCodeDetected,
							$"Suspicious type name detected: {type.FullName}", assembly.FullName, SecuritySeverity.High);
						return false;
					}

					// Check methods for suspicious patterns
					MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | 
						BindingFlags.Static | BindingFlags.Instance);

					foreach (MethodInfo method in methods)
					{
						if (ContainsSuspiciousMethodName(method.Name))
						{
							LogSecurityEvent(SecurityEventType.SuspiciousCodeDetected,
								$"Suspicious method name detected: {type.FullName}.{method.Name}", 
								assembly.FullName, SecuritySeverity.High);
							return false;
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				LogSecurityEvent(SecurityEventType.CodeSafetyValidationError,
					$"Error scanning assembly for suspicious code: {ex.Message}", assembly.FullName, SecuritySeverity.Medium);
				// Allow loading if we can't scan (might be obfuscated legitimate assembly)
				return true;
			}
		}

		/// <summary>
		/// Validates plugin metadata and manifest.
		/// </summary>
		/// <param name="plugin">The plugin to validate.</param>
		/// <returns>True if metadata is valid.</returns>
		private static bool ValidatePluginMetadata(PluginInfo plugin)
		{
			try
			{
				// Validate plugin GUID
				if (plugin.AssemblyInfo.Guid == Guid.Empty)
				{
					LogSecurityEvent(SecurityEventType.PluginMetadataValidationFailed,
						"Plugin has empty GUID", plugin.Assembly.FullName, SecuritySeverity.Medium);
					return false;
				}

				// Validate version information
				if (plugin.AssemblyInfo.Version == null)
				{
					LogSecurityEvent(SecurityEventType.PluginMetadataValidationFailed,
						"Plugin has no version information", plugin.Assembly.FullName, SecuritySeverity.Low);
				}

				// Check for duplicate GUIDs (this should be checked at the host level)
				// Note: This would require access to the plugin registry

				return true;
			}
			catch (Exception ex)
			{
				LogSecurityEvent(SecurityEventType.PluginMetadataValidationError,
					$"Error validating plugin metadata: {ex.Message}", plugin.Assembly.FullName, SecuritySeverity.Medium);
				return false;
			}
		}

		/// <summary>
		/// Checks if the assembly is a core plugin.
		/// </summary>
		/// <param name="assemblyName">Name of the assembly.</param>
		/// <returns>True if this is a core plugin.</returns>
		private static bool IsCorePlugin(string assemblyName)
		{
			string[] corePlugins = { "Eraser.DefaultPlugins" };
			
			foreach (string corePlugin in corePlugins)
			{
				if (assemblyName.Contains(corePlugin))
					return true;
			}
			
			return false;
		}

		/// <summary>
		/// Checks if the certificate is from a trusted publisher.
		/// </summary>
		/// <param name="certificate">The certificate to check.</param>
		/// <returns>True if from a trusted publisher.</returns>
		private static bool IsFromTrustedPublisher(X509Certificate2 certificate)
		{
			// List of trusted publisher subjects
			string[] trustedPublishers = {
				"The Eraser Project",
				"Eraser Development Team"
			};

			string subject = certificate.Subject;
			foreach (string trustedPublisher in trustedPublishers)
			{
				if (subject.Contains(trustedPublisher))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Checks for suspicious type names.
		/// </summary>
		/// <param name="typeName">The type name to check.</param>
		/// <returns>True if the name appears suspicious.</returns>
		private static bool ContainsSuspiciousTypeName(string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
				return false;

			string[] suspiciousPatterns = {
				"Backdoor", "Trojan", "Keylogger", "Rootkit", "Malware",
				"Inject", "Hook", "Stealer", "Dropper", "Payload",
				"Shell", "Remote", "RAT", "Bot", "Crypto"
			};

			string upperTypeName = typeName.ToUpperInvariant();
			foreach (string pattern in suspiciousPatterns)
			{
				if (upperTypeName.Contains(pattern.ToUpperInvariant()))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Checks for suspicious method names.
		/// </summary>
		/// <param name="methodName">The method name to check.</param>
		/// <returns>True if the name appears suspicious.</returns>
		private static bool ContainsSuspiciousMethodName(string methodName)
		{
			if (string.IsNullOrEmpty(methodName))
				return false;

			string[] suspiciousPatterns = {
				"InjectCode", "HookAPI", "InstallBackdoor", "StealData",
				"BypassSecurity", "DisableAntivirus", "HideProcess",
				"KeyLog", "ScreenCapture", "NetworkSniff"
			};

			string upperMethodName = methodName.ToUpperInvariant();
			foreach (string pattern in suspiciousPatterns)
			{
				if (upperMethodName.Contains(pattern.ToUpperInvariant()))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Compares two byte arrays for equality.
		/// </summary>
		/// <param name="array1">First array.</param>
		/// <param name="array2">Second array.</param>
		/// <returns>True if arrays are equal.</returns>
		private static bool ArraysEqual(byte[] array1, byte[] array2)
		{
			if (array1 == null && array2 == null)
				return true;
			if (array1 == null || array2 == null)
				return false;
			if (array1.Length != array2.Length)
				return false;

			for (int i = 0; i < array1.Length; i++)
			{
				if (array1[i] != array2[i])
					return false;
			}

			return true;
		}

		/// <summary>
		/// Logs a security event with appropriate severity.
		/// </summary>
		/// <param name="eventType">Type of security event.</param>
		/// <param name="message">Event message.</param>
		/// <param name="assemblyName">Assembly name (optional).</param>
		/// <param name="severity">Event severity.</param>
		private static void LogSecurityEvent(SecurityEventType eventType, string message, 
			string assemblyName, SecuritySeverity severity)
		{
			LogLevel logLevel = severity switch
			{
				SecuritySeverity.Critical => LogLevel.Fatal,
				SecuritySeverity.High => LogLevel.Error,
				SecuritySeverity.Medium => LogLevel.Warning,
				SecuritySeverity.Low => LogLevel.Notice,
				SecuritySeverity.Information => LogLevel.Information,
				_ => LogLevel.Information
			};

			string fullMessage = string.IsNullOrEmpty(assemblyName) 
				? $"[SECURITY] {eventType}: {message}"
				: $"[SECURITY] {eventType}: {message} (Assembly: {assemblyName})";

			Logger.Log(fullMessage,logLevel);

			// For critical and high severity events, also record in security audit trail
			if (severity >= SecuritySeverity.High)
			{
				RecordSecurityAuditEvent(eventType, message, assemblyName, severity);
			}
		}

		/// <summary>
		/// Records a security event in the audit trail.
		/// </summary>
		/// <param name="eventType">Type of security event.</param>
		/// <param name="message">Event message.</param>
		/// <param name="assemblyName">Assembly name (optional).</param>
		/// <param name="severity">Event severity.</param>
		private static void RecordSecurityAuditEvent(SecurityEventType eventType, string message, 
			string assemblyName, SecuritySeverity severity)
		{
			try
			{
				// This would integrate with a security audit trail system
				// For now, we'll just ensure it's logged with security context
				string auditMessage = $"SECURITY_AUDIT: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} | " +
					$"Event: {eventType} | Severity: {severity} | " +
					$"Assembly: {assemblyName ?? "Unknown"} | Message: {message}";

				Logger.Log(auditMessage, LogLevel.Notice);
			}
			catch (Exception ex)
			{
				// Never let audit trail recording prevent security validation
				Logger.Log($"Failed to record security audit event: {ex.Message}", LogLevel.Error);
			}
		}
	}

	/// <summary>
	/// Types of security events for plugin validation.
	/// </summary>
	public enum SecurityEventType
	{
		PluginValidationSuccess,
		PluginValidationFailed,
		PluginValidationError,
		StrongNameValidationFailed,
		StrongNameValidationError,
		AuthenticodeValidationFailed,
		AuthenticodeValidationError,
		CertificateValidationFailed,
		CertificateValidationWarning,
		CertificateValidationError,
		SuspiciousCodeDetected,
		CodeSafetyValidationError,
		PluginMetadataValidationFailed,
		PluginMetadataValidationError
	}

	/// <summary>
	/// Security event severity levels.
	/// </summary>
	public enum SecuritySeverity
	{
		Information = 0,
		Low = 1,
		Medium = 2,
		High = 3,
		Critical = 4
	}
}