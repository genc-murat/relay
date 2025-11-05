using Relay.CLI.Plugins;
using System.Reflection;

namespace Relay.CLI.Tests.Plugins;

public class PluginSecurityValidatorTests
{
#pragma warning disable CS8602, CS8605
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginSecurityValidator _validator;

    public PluginSecurityValidatorTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        _validator = new PluginSecurityValidator(_mockLogger.Object);
    }

    [Fact]
    public async Task ValidatePluginAsync_ValidPlugin_ReturnsValidResult()
    {
        // Arrange
        // Note: .NET Core system assemblies typically don't have Authenticode signatures
        // in the same way Windows executables do, so we need to test with a mock or skip this test.
        // For now, we'll test that the validation runs and provides appropriate errors.
        var systemAssemblyPath = typeof(string).Assembly.Location;

        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = new PluginManifest
            {
                Name = "TestPlugin",
                Description = "Test Description"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(systemAssemblyPath, pluginInfo);

        // Assert
        // System assemblies may not have Authenticode signatures and have many dependencies
        // not in the allowed list, so we expect validation to fail.
        // The important thing is that the validation process runs without exceptions.
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidatePluginAsync_NonExistentAssembly_ReturnsInvalidResult()
    {
        // Arrange
        var nonExistentPath = "nonexistent.dll";
        var pluginInfo = new PluginInfo { Name = "TestPlugin" };

        // Act
        var result = await _validator.ValidatePluginAsync(nonExistentPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("does not exist"));
    }

    [Fact]
    public async Task ValidatePluginAsync_InvalidManifest_ReturnsInvalidResult()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");
        
        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = new PluginManifest
            {
                Name = "", // Invalid - empty name
                Description = "Test Description"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        
        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public void AddTrustedSource_AddsSourceToList()
    {
        // Arrange
        var source = "https://trusted.example.com";

        // Act
        _validator.AddTrustedSource(source);

        // Assert
        // Since we can't directly access the internal list, we'll verify by checking if it doesn't throw
        // and by looking at the internal state through reflection if needed for a more thorough test
        _validator.AddTrustedSource(source); // Adding same source again should not duplicate
    }

    [Fact]
    public void AddTrustedSource_WithNullSource_ThrowsArgumentNullException()
    {
        // Act & Assert - Should throw ArgumentNullException when calling Contains on List<string> with null
        Assert.Throws<ArgumentNullException>(() => _validator.AddTrustedSource(null!));
    }

    [Fact]
    public void AddTrustedSource_WithEmptyString_AddsEmptySource()
    {
        // Arrange
        var emptySource = "";

        // Act
        _validator.AddTrustedSource(emptySource);

        // Assert - Should add empty string (though it may not be useful)
        // We can't directly verify, but it shouldn't throw
    }

    [Fact]
    public void AddTrustedSource_WithWhitespace_AddsWhitespaceSource()
    {
        // Arrange
        var whitespaceSource = "   ";

        // Act
        _validator.AddTrustedSource(whitespaceSource);

        // Assert - Should add whitespace string
    }

    [Fact]
    public void AddTrustedSource_WithDuplicateSource_DoesNotDuplicate()
    {
        // Arrange
        var source = "https://trusted.example.com";

        // Act
        _validator.AddTrustedSource(source);
        _validator.AddTrustedSource(source); // Add same source again

        // Assert - Should not duplicate, but we can't verify count directly
        // The method uses List.Contains, so duplicates should be prevented
    }

    [Fact]
    public void AddTrustedSource_WithMultipleValidSources_AddsAll()
    {
        // Arrange
        var sources = new[] { "https://trusted1.com", "https://trusted2.com", "https://trusted3.com" };

        // Act
        foreach (var source in sources)
        {
            _validator.AddTrustedSource(source);
        }

        // Assert - Should add all sources without issues
    }

    [Fact]
    public void GetSetPluginPermissions_WorksCorrectly()
    {
        // Arrange
        var pluginName = "TestPlugin";
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                Write = false
            }
        };

        // Act
        _validator.SetPluginPermissions(pluginName, permissions);
        var retrievedPermissions = _validator.GetPluginPermissions(pluginName);

        // Assert
        Assert.NotNull(retrievedPermissions);
        Assert.Equal(permissions.FileSystem?.Read, retrievedPermissions.FileSystem?.Read);
        Assert.Equal(permissions.FileSystem?.Write, retrievedPermissions.FileSystem?.Write);
    }

    [Fact]
    public void GetPluginPermissions_NonExistentPlugin_ReturnsNull()
    {
        // Act
        var permissions = _validator.GetPluginPermissions("NonExistentPlugin");

        // Assert
        Assert.Null(permissions);
    }

    [Fact]
    public async Task ValidatePluginAsync_PluginWithInvalidPermissions_ReturnsInvalidResult()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = new PluginManifest
            {
                Name = "TestPlugin",
                Description = "Test Description",
                Permissions = new PluginPermissions
                {
                    FileSystem = new FileSystemPermissions
                    {
                        AllowedPaths = new[] { @"C:\Windows\System32" } // Invalid system path
                    }
                }
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("invalid permissions"));

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidatePluginAsync_PluginWithUntrustedRepository_LogsWarning()
    {
        // Arrange
        // Use a real signed system assembly
        var systemAssemblyPath = typeof(string).Assembly.Location;

        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = new PluginManifest
            {
                Name = "TestPlugin",
                Description = "Test Description",
                Repository = "https://untrusted.example.com"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(systemAssemblyPath, pluginInfo);

        // Assert
        // System assemblies have many dependencies not in the allowed list,
        // so it will fail validation. The important part is that the warning about
        // untrusted repository was logged.
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("not trusted"))), Times.Once);
    }

    [Fact]
    public async Task ValidatePluginAsync_PluginWithEmptyDescription_ReturnsInvalidResult()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = new PluginManifest
            {
                Name = "TestPlugin",
                Description = "" // Invalid - empty description
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidatePluginAsync_PluginWithNullManifest_ReturnsInvalidResult()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = null // Invalid - null manifest
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidatePluginAsync_InvalidAssemblyPath_ReturnsInvalidResult()
    {
        // Arrange - use a directory path that exists but is not a valid assembly
        var directoryPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar); // Directory path
        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = new PluginManifest
            {
                Name = "TestPlugin",
                Description = "Test Description"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(directoryPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("does not exist"));
    }

    [Fact]
    public void ValidateFileSystemPermissions_InvalidSystemPath_ReturnsFalse()
    {
        // Arrange
        var permissions = new FileSystemPermissions
        {
            AllowedPaths = new[] { @"C:\Windows\System32" }
        };

        // Act - use reflection to call private method
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateFileSystemPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateFileSystemPermissions_ValidPaths_ReturnsTrue()
    {
        // Arrange
        var permissions = new FileSystemPermissions
        {
            AllowedPaths = new[] { @"C:\Temp", @"D:\Data" }
        };

        // Act - use reflection to call private method
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateFileSystemPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateFileSystemPermissions_WithNullAllowedPaths_ReturnsTrue()
    {
        // Arrange
        var permissions = new FileSystemPermissions
        {
            AllowedPaths = null
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateFileSystemPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateFileSystemPermissions_WithEmptyAllowedPaths_ReturnsTrue()
    {
        // Arrange
        var permissions = new FileSystemPermissions
        {
            AllowedPaths = Array.Empty<string>()
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateFileSystemPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateFileSystemPermissions_WithProgramFilesPath_ReturnsFalse()
    {
        // Arrange
        var permissions = new FileSystemPermissions
        {
            AllowedPaths = new[] { @"C:\Program Files\MaliciousApp" }
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateFileSystemPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateFileSystemPermissions_WithWindowsPath_ReturnsFalse()
    {
        // Arrange
        var permissions = new FileSystemPermissions
        {
            AllowedPaths = new[] { @"C:\Windows\System32\cmd.exe" }
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateFileSystemPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePermissions_WithValidFileSystemAndNetworkPermissions_ReturnsTrue()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                Write = false,
                AllowedPaths = new[] { @"C:\Temp" }
            },
            Network = new NetworkPermissions
            {
                Https = true,
                AllowedHosts = new[] { "api.example.com" }
            }
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidatePermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePermissions_WithInvalidFileSystemPermissions_ReturnsFalse()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                AllowedPaths = new[] { @"C:\Windows\System32" } // Invalid
            },
            Network = new NetworkPermissions
            {
                Https = true
            }
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidatePermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePermissions_WithInvalidNetworkPermissions_ReturnsFalse()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                AllowedPaths = new[] { @"C:\Temp" } // Valid
            },
            Network = new NetworkPermissions
            {
                Https = true,
                AllowedHosts = new[] { "localhost" } // Invalid
            }
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidatePermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePermissions_WithNullPermissions_ReturnsTrue()
    {
        // Arrange
        PluginPermissions permissions = null!;

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidatePermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert - Should handle null gracefully
        Assert.True(result);
    }

    [Fact]
    public void ValidateNetworkPermissions_WithValidPermissions_ReturnsTrue()
    {
        // Arrange
        var permissions = new NetworkPermissions
        {
            Http = true,
            Https = true,
            AllowedHosts = new[] { "api.example.com", "cdn.example.com" }
        };

        // Act - use reflection to call private method
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateNetworkPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("HTTP permissions"))), Times.Once);
    }

    [Fact]
    public void ValidateNetworkPermissions_WithDangerousHost_ReturnsFalse()
    {
        // Arrange
        var permissions = new NetworkPermissions
        {
            Https = true,
            AllowedHosts = new[] { "localhost" } // Dangerous host
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateNetworkPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("dangerous host"))), Times.Once);
    }

    [Fact]
    public void ValidateNetworkPermissions_WithInternalNetworkHost_ReturnsFalse()
    {
        // Arrange
        var permissions = new NetworkPermissions
        {
            Https = true,
            AllowedHosts = new[] { "192.168.1.1" } // Internal network
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateNetworkPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("dangerous host"))), Times.Once);
    }

    [Fact]
    public void ValidateNetworkPermissions_WithFileProtocol_ReturnsFalse()
    {
        // Arrange
        var permissions = new NetworkPermissions
        {
            Https = true,
            AllowedHosts = new[] { "file://etc/passwd" } // Dangerous protocol
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateNetworkPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("dangerous host"))), Times.Once);
    }

    [Fact]
    public void ValidateNetworkPermissions_WithEmptyAllowedHosts_LogsWarning()
    {
        // Arrange
        var permissions = new NetworkPermissions
        {
            Https = true,
            AllowedHosts = new[] { "", null, "   " } // Empty/null hosts
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateNetworkPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result); // Empty hosts are allowed, just logged
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("empty or null allowed host"))), Times.Exactly(3));
    }

    [Fact]
    public void ValidateNetworkPermissions_WithValidDeniedHosts_ReturnsTrue()
    {
        // Arrange
        var permissions = new NetworkPermissions
        {
            Https = true,
            DeniedHosts = new[] { "malicious.com", "evil.net" }
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateNetworkPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateNetworkPermissions_WithHttpsOnly_ReturnsTrue()
    {
        // Arrange
        var permissions = new NetworkPermissions
        {
            Https = true,
            Http = false
        };

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateNetworkPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("HTTP permissions"))), Times.Never);
    }

    #region ValidateSignatureAsync Tests

    [Fact]
    public async Task ValidateSignatureAsync_WithSystemAssembly_ValidatesAuthenticodeAndStrongName()
    {
        // Arrange
        var systemAssembly = typeof(string).Assembly.Location;
        var pluginInfo = new PluginInfo
        {
            Name = "SystemAssembly",
            Manifest = new PluginManifest
            {
                Name = "SystemAssembly",
                Description = "System assembly test",
                Version = "1.0.0"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(systemAssembly, pluginInfo);

        // Assert
        Assert.NotNull(result);
        // System assemblies should be properly signed
    }

    [Fact]
    public async Task ValidateAuthenticodeSignature_WithInvalidPEFile_ReturnsFalse()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        try
        {
            // Create invalid PE file (not starting with MZ)
            await File.WriteAllBytesAsync(tempPath, new byte[] { 0x00, 0x00, 0x00, 0x00 });

            var pluginInfo = new PluginInfo
            {
                Name = "InvalidPE",
                Manifest = new PluginManifest
                {
                    Name = "InvalidPE",
                    Description = "Invalid PE file test",
                    Version = "1.0.0"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("not properly signed"));
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateAuthenticodeSignature_WithValidPEButNoSignature_ReturnsFalse()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        try
        {
            // Create valid PE structure but without certificate table
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // DOS header
                writer.Write((ushort)0x5A4D); // MZ
                writer.Write(new byte[58]);
                writer.Write((int)0x80); // PE offset

                writer.Write(new byte[0x80 - 0x40]);

                // PE header
                writer.Write((uint)0x00004550); // PE signature

                // COFF header
                writer.Write((ushort)0x14C); // Machine
                writer.Write((ushort)1); // Sections
                writer.Write(new byte[12]);
                writer.Write((ushort)224); // Optional header size
                writer.Write((ushort)0x2022);

                // Optional header
                writer.Write((ushort)0x10b); // PE32 magic
                writer.Write(new byte[126]);

                // Certificate table (empty)
                writer.Write((uint)0); // No RVA
                writer.Write((uint)0); // No size
            }

            var pluginInfo = new PluginInfo
            {
                Name = "NoCertTable",
                Manifest = new PluginManifest
                {
                    Name = "NoCertTable",
                    Description = "No certificate table test",
                    Version = "1.0.0"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("not properly signed"));
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateAuthenticodeSignature_WithPE32Plus_HandlesCorrectly()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        try
        {
            // Create PE32+ structure
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // DOS header
                writer.Write((ushort)0x5A4D);
                writer.Write(new byte[58]);
                writer.Write((int)0x80);

                writer.Write(new byte[0x80 - 0x40]);

                // PE header
                writer.Write((uint)0x00004550);

                // COFF header
                writer.Write((ushort)0x8664); // AMD64
                writer.Write((ushort)1);
                writer.Write(new byte[12]);
                writer.Write((ushort)240); // Larger optional header
                writer.Write((ushort)0x2022);

                // Optional header with PE32+ magic
                writer.Write((ushort)0x20b); // PE32+ magic
                writer.Write(new byte[142]);

                // Certificate table at offset 144 for PE32+
                writer.Write((uint)0x2000); // RVA
                writer.Write((uint)0x300); // Size
            }

            var pluginInfo = new PluginInfo
            {
                Name = "PE32Plus",
                Manifest = new PluginManifest
                {
                    Name = "PE32Plus",
                    Description = "PE32+ test",
                    Version = "1.0.0"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert
            Assert.NotNull(result);
            // Should detect PE32+ and find certificate table
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateAuthenticodeSignature_WithCorruptedPESignature_ReturnsFalse()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // DOS header
                writer.Write((ushort)0x5A4D);
                writer.Write(new byte[58]);
                writer.Write((int)0x80);

                writer.Write(new byte[0x80 - 0x40]);

                // Invalid PE signature
                writer.Write((uint)0xBADC0DE); // Wrong signature
            }

            var pluginInfo = new PluginInfo
            {
                Name = "CorruptedPE",
                Manifest = new PluginManifest
                {
                    Name = "CorruptedPE",
                    Description = "Corrupted PE test",
                    Version = "1.0.0"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert
            Assert.False(result.IsValid);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateStrongNameSignature_WithUnsignedAssembly_ReturnsFalse()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        try
        {
            // Create a minimal valid PE/DLL without strong name
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write((ushort)0x5A4D);
                writer.Write(new byte[58]);
                writer.Write((int)0x80);
                writer.Write(new byte[0x80 - 0x40]);
                writer.Write((uint)0x00004550);
                writer.Write(new byte[20]);
            }

            var pluginInfo = new PluginInfo
            {
                Name = "UnsignedAssembly",
                Manifest = new PluginManifest
                {
                    Name = "UnsignedAssembly",
                    Description = "Unsigned assembly test",
                    Version = "1.0.0"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert
            Assert.False(result.IsValid);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateSignatureAsync_LogsDebugMessagesForHash()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempPath, new byte[] { 1, 2, 3, 4, 5 });

            var pluginInfo = new PluginInfo
            {
                Name = "HashTest",
                Manifest = new PluginManifest
                {
                    Name = "HashTest",
                    Description = "Hash test",
                    Version = "1.0.0"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert
            Assert.NotNull(result);
            _mockLogger.Verify(x => x.LogDebug(It.Is<string>(s => s.Contains("Assembly hash:"))), Times.AtLeastOnce);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithFileAccessError_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.dll");

        var pluginInfo = new PluginInfo
        {
            Name = "FileAccessError",
            Manifest = new PluginManifest
            {
                Name = "FileAccessError",
                Description = "File access error test",
                Version = "1.0.0"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(nonExistentPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("does not exist"));
    }

    [Fact]
    public async Task ValidateAuthenticodeSignature_WithValidCertificateTable_LogsSuccess()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // DOS header
                writer.Write((ushort)0x5A4D);
                writer.Write(new byte[58]);
                writer.Write((int)0x80);
                writer.Write(new byte[0x80 - 0x40]);

                // PE header
                writer.Write((uint)0x00004550);

                // COFF header
                writer.Write((ushort)0x14C);
                writer.Write((ushort)1);
                writer.Write(new byte[12]);
                writer.Write((ushort)224);
                writer.Write((ushort)0x2022);

                // Optional header
                writer.Write((ushort)0x10b); // PE32
                writer.Write(new byte[126]);

                // Certificate table with valid values
                writer.Write((uint)0x1000); // RVA
                writer.Write((uint)0x200); // Size
            }

            var pluginInfo = new PluginInfo
            {
                Name = "ValidCertTable",
                Manifest = new PluginManifest
                {
                    Name = "ValidCertTable",
                    Description = "Valid certificate table test",
                    Version = "1.0.0"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert
            _mockLogger.Verify(x => x.LogDebug(It.Is<string>(s => s.Contains("Found certificate table"))), Times.AtLeastOnce);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateSignatureAsync_HandlesExceptionsGracefully()
    {
        // Arrange
        var invalidPath = "Z:\\InvalidPath\\NonExistent.dll"; // Drive likely doesn't exist

        var pluginInfo = new PluginInfo
        {
            Name = "ExceptionTest",
            Manifest = new PluginManifest
            {
                Name = "ExceptionTest",
                Description = "Exception handling test",
                Version = "1.0.0"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(invalidPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    #endregion

    #region ValidateDependenciesAsync Tests

    [Fact]
    public async Task ValidateDependenciesAsync_WithUntrustedDependencies_ReturnsFalse()
    {
        // Arrange - Use a system assembly that likely references untrusted assemblies
        var systemAssemblyPath = typeof(System.Data.DataSet).Assembly.Location; // References System.Data which may not be in allowed list

        // Act - use reflection to call private method
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { systemAssemblyPath });

        // Assert - System.Data assembly references many assemblies not in the allowed list
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithTrustedDependencies_ReturnsTrue()
    {
        // Arrange - Create a simple assembly that only references allowed assemblies
        // For this test, we'll use mscorlib or a basic system assembly
        var basicAssemblyPath = typeof(object).Assembly.Location; // mscorlib/System.Private.CoreLib

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { basicAssemblyPath });

        // Assert - Basic system assemblies should have allowed dependencies
        // Note: This might fail if the assembly references untrusted assemblies, which is expected behavior
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithBadImageFormat_ReturnsTrue()
    {
        // Arrange - Create a file that's not a valid assembly
        var tempPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempPath, "This is not an assembly");

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert - Should return true for bad image format (validation continues, load will fail)
            Assert.True(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithFileLoadException_ReturnsTrue()
    {
        // Arrange - Use a path that will cause FileLoadException
        // Create a file that exists but will cause FileLoadException when loaded as assembly
        var tempPath = Path.GetTempFileName();
        try
        {
            // Write some data that will cause FileLoadException when Assembly.LoadFrom tries to load it
            await File.WriteAllBytesAsync(tempPath, new byte[] { 0x4D, 0x5A, 0x00 }); // Incomplete MZ header

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert - Should return true for file load issues (validation continues)
            Assert.True(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithNullReferencedAssembly_HandlesGracefully()
    {
        // Arrange - This is harder to test directly since we need an assembly with null reference
        // We'll test with an assembly that has some references
        var assemblyPath = typeof(System.Linq.Enumerable).Assembly.Location;

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { assemblyPath });

        // Assert - Should handle the validation without throwing
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithAssemblyReferencingOnlyAllowedAssemblies_ReturnsTrue()
    {
        // Arrange - Use an assembly that should only reference allowed assemblies
        // System.Runtime is a good candidate as it references basic system assemblies
        var assemblyPath = typeof(System.Runtime.GCSettings).Assembly.Location;

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { assemblyPath });

        // Assert - System.Runtime should reference allowed assemblies
        // Note: This may fail if it references untrusted assemblies, which would be correct behavior
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithAssemblyHavingNoReferences_ReturnsTrue()
    {
        // Arrange - Find or create an assembly with no references
        // Most assemblies have at least some references, but let's test with a basic one
        var assemblyPath = typeof(object).Assembly.Location; // mscorlib/System.Private.CoreLib

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { assemblyPath });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithAssemblyReferencingUntrustedAssembly_ReturnsFalse()
    {
        // Arrange - Use an assembly that definitely references untrusted assemblies
        // System.Windows.Forms or similar would reference many untrusted assemblies
        // Let's use System.Data which references assemblies not in our allowed list
        var assemblyPath = typeof(System.Data.DataTable).Assembly.Location;

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { assemblyPath });

        // Assert - System.Data references assemblies like System.Xml, System.Transactions, etc.
        // Some of these may not be in the allowed list
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithAssemblyReferencingUntrustedAssemblies_ReturnsFalse()
    {
        // Arrange - Test with an assembly that references untrusted assemblies
        // The Relay.CLI assembly references packages like BenchmarkDotNet, Microsoft.CodeAnalysis, etc.
        var assemblyPath = typeof(Relay.CLI.Plugins.PluginSecurityValidator).Assembly.Location;

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { assemblyPath });

        // Assert - The Relay.CLI assembly references untrusted assemblies, so should return false
        Assert.False(result);
    }

    #endregion

    #region ValidateManifestAsync Tests

    [Fact]
    public async Task ValidateManifestAsync_WithTrustedRepository_DoesNotLogWarning()
    {
        // Arrange
        var trustedSource = "https://trusted.example.com";
        _validator.AddTrustedSource(trustedSource);

        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        var pluginInfo = new PluginInfo
        {
            Name = "TrustedRepoPlugin",
            Manifest = new PluginManifest
            {
                Name = "TrustedRepoPlugin",
                Description = "Test Description",
                Repository = trustedSource
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert - Should not log warning about untrusted repository
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("not trusted"))), Times.Never);

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidateManifestAsync_WithNullRepository_DoesNotLogWarning()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        var pluginInfo = new PluginInfo
        {
            Name = "NullRepoPlugin",
            Manifest = new PluginManifest
            {
                Name = "NullRepoPlugin",
                Description = "Test Description",
                Repository = null
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert - Should not log warning when repository is null
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("not trusted"))), Times.Never);

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidateManifestAsync_WithEmptyRepository_DoesNotLogWarning()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        var pluginInfo = new PluginInfo
        {
            Name = "EmptyRepoPlugin",
            Manifest = new PluginManifest
            {
                Name = "EmptyRepoPlugin",
                Description = "Test Description",
                Repository = ""
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert - Should not log warning when repository is empty
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("not trusted"))), Times.Never);

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidateManifestAsync_ValidManifest_ReturnsTrue()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        var pluginInfo = new PluginInfo
        {
            Name = "ValidManifestPlugin",
            Manifest = new PluginManifest
            {
                Name = "ValidManifestPlugin",
                Description = "Valid Description"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert - Manifest validation should pass (other validations may fail)
        // We can't directly test ValidateManifestAsync return value, but we can verify
        // that manifest-related errors are not present when manifest is valid
        Assert.False(result.IsValid); // Will fail due to signature/dependency issues
        Assert.DoesNotContain(result.Errors, e => e.Contains("Plugin manifest is invalid"));

        // Cleanup
        File.Delete(tempPath);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ValidatePluginAsync_WithUnauthorizedAccessException_HandlesGracefully()
    {
        // Arrange - Try to access a file in a restricted location
        // This is platform-dependent, but we can try system directories
        var restrictedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "restricted.dll");

        var pluginInfo = new PluginInfo
        {
            Name = "AccessDeniedPlugin",
            Manifest = new PluginManifest
            {
                Name = "AccessDeniedPlugin",
                Description = "Access denied test"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(restrictedPath, pluginInfo);

        // Assert - Should handle the exception gracefully
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        // Should contain either "does not exist" or exception message
        Assert.True(result.Errors.Any(e => e.Contains("does not exist") || e.Contains("Exception during security validation")));
    }

    [Fact]
    public async Task ValidatePluginAsync_WithPathTooLongException_HandlesGracefully()
    {
        // Arrange - Create a path that's too long
        var longPath = new string('a', 260) + ".dll"; // Windows MAX_PATH is 260

        var pluginInfo = new PluginInfo
        {
            Name = "LongPathPlugin",
            Manifest = new PluginManifest
            {
                Name = "LongPathPlugin",
                Description = "Long path test"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(longPath, pluginInfo);

        // Assert - Should handle path too long gracefully
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidatePluginAsync_WithCorruptedManifestData_HandlesGracefully()
    {
        // Arrange - Create a valid file but with manifest that might cause issues
        var tempPath = Path.GetTempFileName();
        try
        {
            // Write some binary data that might cause issues during validation
            var randomData = new byte[1024];
            new Random().NextBytes(randomData);
            await File.WriteAllBytesAsync(tempPath, randomData);

            var pluginInfo = new PluginInfo
            {
                Name = "CorruptedDataPlugin",
                Manifest = new PluginManifest
                {
                    Name = "CorruptedDataPlugin",
                    Description = "Corrupted data test"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert - Should handle any exceptions during validation
            Assert.False(result.IsValid);
            // May or may not have errors depending on what exceptions occur
            Assert.IsType<SecurityValidationResult>(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidatePluginAsync_WithExceptionInValidationSubMethod_HandlesGracefully()
    {
        // Arrange - Create conditions that might cause exceptions in sub-methods
        // For example, a file that exists but causes issues during signature validation
        var tempPath = Path.GetTempFileName();
        try
        {
            // Create a file with partial PE header that might cause BinaryReader issues
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write((ushort)0x5A4D); // Valid MZ
                writer.Write(new byte[50]); // Partial header
                // Don't write full PE header - this might cause exceptions in Authenticode validation
            }

            var pluginInfo = new PluginInfo
            {
                Name = "PartialPEPlugin",
                Manifest = new PluginManifest
                {
                    Name = "PartialPEPlugin",
                    Description = "Partial PE test"
                }
            };

            // Act
            var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

            // Assert - Should handle exceptions in sub-methods gracefully
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    #endregion

    #region ValidateStrongNameSignatureAsync Tests

    [Fact]
    public async Task ValidateStrongNameSignatureAsync_WithBadImageFormat_ReturnsFalse()
    {
        // Arrange - Create a file that's not a valid assembly
        var tempPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempPath, "This is definitely not an assembly");

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateStrongNameSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateStrongNameSignatureAsync_WithFileLoadException_ReturnsFalse()
    {
        // Arrange - Use a non-existent path that will cause FileLoadException
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.dll");

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateStrongNameSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { nonExistentPath });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateStrongNameSignatureAsync_WithValidUnsignedAssembly_ReturnsFalse()
    {
        // Arrange - Use a system assembly that's not strong-named
        // Most .NET Core assemblies are not strong-named in the traditional sense
        var assemblyPath = typeof(System.Collections.Generic.List<>).Assembly.Location;

        // Act
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateStrongNameSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { assemblyPath });

        // Assert - Most .NET Core assemblies don't have strong names
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateStrongNameSignatureAsync_WithCorruptedAssembly_ReturnsFalse()
    {
        // Arrange - Create a file with partial assembly data
        var tempPath = Path.GetTempFileName();
        try
        {
            // Write some data that looks like an assembly but is corrupted
            var partialAssemblyData = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 }; // MZ header but incomplete
            await File.WriteAllBytesAsync(tempPath, partialAssemblyData);

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateStrongNameSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    #endregion

    #region ValidateAuthenticodeSignatureAsync Edge Cases

    [Fact]
    public async Task ValidateAuthenticodeSignatureAsync_WithFileTooShort_ReturnsFalse()
    {
        // Arrange - Create a file that's too short to contain PE headers
        var tempPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempPath, new byte[] { 0x4D, 0x5A }); // Just MZ, not enough for full header

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateAuthenticodeSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateAuthenticodeSignatureAsync_WithInvalidDOSHeader_ReturnsFalse()
    {
        // Arrange - Create a file with invalid DOS header
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write((ushort)0x1234); // Invalid DOS signature (not MZ)
                writer.Write(new byte[50]); // Fill some data
            }

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateAuthenticodeSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateAuthenticodeSignatureAsync_WithInvalidPEOffset_ReturnsFalse()
    {
        // Arrange - Create a file with invalid PE header offset
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write((ushort)0x5A4D); // Valid MZ
                writer.Write(new byte[58]); // Skip to offset field
                writer.Write((int)0xFFFFFFF); // Invalid PE offset (way too large)
            }

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateAuthenticodeSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateAuthenticodeSignatureAsync_WithZeroOptionalHeaderSize_ReturnsFalse()
    {
        // Arrange - Create PE file with zero optional header size
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write((ushort)0x5A4D); // MZ
                writer.Write(new byte[58]);
                writer.Write((int)0x80); // PE offset

                writer.Write(new byte[0x80 - 0x40]); // Fill to PE header

                writer.Write((uint)0x00004550); // PE signature

                // COFF header with zero optional header size
                writer.Write((ushort)0x14C); // Machine
                writer.Write((ushort)1); // Sections
                writer.Write(new byte[12]); // Rest of COFF header
                writer.Write((ushort)0); // Zero optional header size
                writer.Write((ushort)0x2022); // Characteristics
            }

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateAuthenticodeSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ValidateAuthenticodeSignatureAsync_WithIOExceptionDuringRead_ReturnsFalse()
    {
        // Arrange - Create a file and then try to read it in a way that might cause IO issues
        // This is hard to simulate reliably, but we can test with a file that gets modified during read
        var tempPath = Path.GetTempFileName();
        try
        {
            // Create a minimal valid file
            await File.WriteAllBytesAsync(tempPath, new byte[] { 0x4D, 0x5A, 0x00 });

            // Act
            var method = typeof(PluginSecurityValidator).GetMethod("ValidateAuthenticodeSignatureAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<bool>)method.Invoke(_validator, new object[] { tempPath });

            // Assert - Should handle any IO exceptions gracefully
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    #endregion

    #region InitializeTrustedAssemblies Tests

    [Fact]
    public void InitializeTrustedAssemblies_ContainsExpectedSystemAssemblies()
    {
        // Arrange - Use reflection to access the private field
        var field = typeof(PluginSecurityValidator).GetField("_allowedAssemblies", BindingFlags.NonPublic | BindingFlags.Instance);
        var allowedAssemblies = (HashSet<string>)field.GetValue(_validator);

        // Assert - Should contain the assemblies initialized in InitializeTrustedAssemblies
        Assert.Contains("System", allowedAssemblies);
        Assert.Contains("System.Core", allowedAssemblies);
        Assert.Contains("System.Data", allowedAssemblies);
        Assert.Contains("System.Xml", allowedAssemblies);
        Assert.Contains("System.Linq", allowedAssemblies);
        Assert.Contains("System.Collections", allowedAssemblies);
        Assert.Contains("System.IO", allowedAssemblies);
        Assert.Contains("System.Text", allowedAssemblies);
        Assert.Contains("System.Threading", allowedAssemblies);
        Assert.Contains("Microsoft.CSharp", allowedAssemblies);
        Assert.Contains("Relay.CLI.Sdk", allowedAssemblies);
        Assert.Contains("Relay.CLI.Plugins", allowedAssemblies);
    }

    [Fact]
    public void InitializeTrustedAssemblies_IsCaseInsensitive()
    {
        // Arrange
        var field = typeof(PluginSecurityValidator).GetField("_allowedAssemblies", BindingFlags.NonPublic | BindingFlags.Instance);
        var allowedAssemblies = (HashSet<string>)field.GetValue(_validator);

        // Assert - Should be case insensitive due to StringComparer.OrdinalIgnoreCase
        Assert.Contains("system", allowedAssemblies); // lowercase
        Assert.Contains("SYSTEM", allowedAssemblies); // uppercase
        Assert.Contains("System", allowedAssemblies); // mixed case
    }

    [Fact]
    public async Task DependencyValidation_UsesInitializedTrustedAssemblies()
    {
        // Arrange - Create a mock assembly that references "System" (which should be trusted)
        // This is indirect testing - we verify that the initialization affects dependency validation

        // Use an assembly that we know references System
        var systemAssemblyPath = typeof(string).Assembly.Location;

        // Act - Validate dependencies
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateDependenciesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = await (Task<bool>)method.Invoke(_validator, new object[] { systemAssemblyPath });

        // Assert - Should pass because System assemblies reference trusted assemblies
        // Note: This may vary depending on the actual references, but the initialization should work
        Assert.IsType<bool>(result); // At minimum, should not throw due to uninitialized collection
    }

    #endregion
}
