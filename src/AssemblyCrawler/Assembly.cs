﻿using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;


/* NOTES

// Get 


        // Any CPU = x86 + COMIMAGE_FLAGS_ILONLY will give the flag

        (dwManagedImageFlags &COMIMAGE_FLAGS_ILONLY) != 0;


        // How you get the core header

        if (image_optional_header.h32.NumberOfRvaAndSizes > IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR)
        {
            *ManagedCode = image_optional_header.h32.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size ? TRUE : FALSE;
            if (*ManagedCode)
            {
                fSucc = ReadCORHeader(hImage,
                                      image_optional_header.h32.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress,
                                      image_optional_header.h32.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size,
                                      image_section_headers,
                                      section_count,
                                      &ich);
            }
        }
*/

namespace AssemblyCrawler
{
    internal class AssemblyInfo
    {
        public readonly Lazy<string> FName;
        public readonly Lazy<string> AName;
        public readonly Lazy<Version> AssemblyVersion;
        public readonly Lazy<Version> FileVersion;
        public Lazy<ulong> FileSize;
        public Lazy<string> PublicKeyToken;
        public Lazy<string> FrameworkVersion;

        public Lazy<bool> IsManaged;

        public string Path => file.DirectoryName;

        private Lazy<AssemblyName> assemblyName;

        private FileInfo file { get; set; }

        public AssemblyInfo(FileInfo file)
        {
            this.file = file;

            FileVersion = new Lazy<Version>(() =>
            {
                if (Version.TryParse(FileVersionInfo.GetVersionInfo(file.FullName)?.FileVersion, out Version? fileVersion) && fileVersion is not null)
                {
                    return fileVersion;
                }

                // Placeholder
                return new Version("0.0.999.9");
            });

            FileSize = new Lazy<ulong>(() => (ulong)file.Length);

            this.assemblyName = new Lazy<AssemblyName>(GetAssemblyName);

            this.FName = new Lazy<string>(() => file.Name.ToLowerInvariant());

            this.AName = new Lazy<string>(() => assemblyName.Value?.ToString() ?? String.Empty);
            this.AssemblyVersion = new Lazy<Version>(GetAssemblyVersion);
            this.PublicKeyToken = new Lazy<string>(GetPublicKeyToken);
            this.IsManaged = new Lazy<bool>(() => this.IsManagedAssembly());
            this.FrameworkVersion = new Lazy<string>(() => this.InitializeFramework());
        }

        private AssemblyName GetAssemblyName()
        {
            if (this.IsManaged.Value)
            {
                try
                {
                    return AssemblyName.GetAssemblyName(file.FullName);
                }
                catch
                { }
            }
            return default;
        }

        private Version GetAssemblyVersion()
        {
            return assemblyName.Value?.Version ?? new Version("0.0.0.0");
        }

        private string InitializeFramework()
        {
            //Concord\src\StandaloneSetup\AssemblyReferenceChecker\ImportedAssembly.cs
            using var stream = File.OpenRead(file.FullName);
            using var reader = new PEReader(stream);

            if (!reader.HasMetadata)
            {
                return String.Empty;
            }

            var metadataReader = reader.GetMetadataReader();

            foreach (var handle in metadataReader.CustomAttributes)
            {
                try
                {
                    // look for System.Runtime.Versioning.TargetFrameworkAttribute
                    // var attributeHandle = metadataReader.CustomAttributes.Cast<CustomAttributeHandle>().SingleOrDefault(metadataReader.IsTargetFrameworkMonikerAttribute);
                    var attribute = metadataReader.GetCustomAttribute(handle);
                    var metadataVersion = metadataReader.MetadataVersion;
                    if (attribute.Constructor.Kind != HandleKind.MemberReference)
                    {
                        continue;
                    }
                    // Throws InvalidCastException
                    var attributeConstructor = metadataReader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    var attributeType = metadataReader.GetTypeReference((TypeReferenceHandle)attributeConstructor.Parent);
                    var attributeName = metadataReader.GetString(attributeType.Name);

                    if (attributeName == "TargetFrameworkAttribute")
                    {
                        // "\u0001\0\u0019.NETStandard,Version=v2.0\u0001\0T\u000e\u0014FrameworkDisplayName\u0011.NET Standard 2.0"
                        var value = metadataReader.GetBlobReader(attribute.Value);
                        _ = value.ReadByte();
                        _ = value.ReadByte();
                        var targetFramework = value.ReadSerializedString();
                        try
                        {
                            _ = value.ReadByte();
                            _ = value.ReadByte();
                            _ = value.ReadByte();
                            _ = value.ReadByte();
                            var label = value.ReadSerializedString();
                            var displayName = value.ReadSerializedString();
                            if (!string.IsNullOrEmpty(displayName))
                            {
                                return displayName;
                            }
                        }
                        catch { }

                        return targetFramework ?? "Unknown";
                    }
                }
                catch { }
            }
            return "Unknown";
        }

        public bool IsReadyToRun()
        {
            using var stream = File.OpenRead(file.FullName);
            using var reader = new PEReader(stream);
            
            // From Concord: https://devdiv.visualstudio.com/DevDiv/_git/Concord?path=/src/impl/Common/PEFile.cpp&version=GBmain&line=1761&lineEnd=1770&lineStartColumn=1&lineEndColumn=18&lineStyle=plain&_a=contents
            //DWORD dwSignature = 0;
            //HRESULT hr = ReadRVA(m_IMAGE_COR20_HEADER.ManagedNativeHeader.VirtualAddress, &dwSignature, sizeof(DWORD));
            //if (hr != S_OK)
            //{
            //    VSFAIL("failed to read ManagedNativeHeader Signature???");
            //}
            //else
            //{
            //    m_fManagedReadyToRun = dwSignature == 0x00525452; // 'RTR'
            //}

            return false;
        }

        private string GetPublicKey()
        {
            return HexByteArrayToString(assemblyName.Value?.GetPublicKey());
        }

        private string GetPublicKeyToken()
        {
            return HexByteArrayToString(assemblyName.Value?.GetPublicKeyToken());
        }

        private static string HexByteArrayToString(byte[]? b)
        {
            if (b != null && b.Any())
            {
                return BitConverter.ToString(b).Replace("-", "").ToLowerInvariant();
            }
            return string.Empty;
        }

        public override int GetHashCode()
        {
            var hash = FName.Value.GetStableHashCode() ^ AssemblyVersion.Value.ToString(4).GetStableHashCode() ^ FileVersion.Value.ToString(4).GetStableHashCode() ^ FileSize.Value.ToString("0x{0:X}").GetStableHashCode();

            if (!string.IsNullOrWhiteSpace(PublicKeyToken.Value))
                return hash ^ PublicKeyToken.Value.GetStableHashCode();

            return hash;
        }
    }

    internal static class MetadataReaderExtensions
    {
        public static bool IsTargetFrameworkMonikerAttribute(this MetadataReader metadataReader, CustomAttributeHandle handle)
        {
            if (handle.IsNil)
            {
                return false;
            }
            var customAttribute = metadataReader.GetCustomAttribute(handle);
            if (customAttribute.Constructor.Kind != HandleKind.MemberReference)
            {
                return false;
            }
            if (customAttribute.Parent.Kind == HandleKind.TypeReference)
            {
                return false;
            }

            // GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            var constructorRef = metadataReader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
            if (constructorRef.Parent.Kind != HandleKind.TypeReference)
            {
                return false;
            }
            var typeRef = metadataReader.GetTypeReference((TypeReferenceHandle)constructorRef.Parent);
            var typeRefName = metadataReader.GetString(typeRef.Name);
            var typeRefNamespace = metadataReader.GetString(typeRef.Namespace);
            return string.Equals(typeRefName, "TargetFrameworkAttribute", StringComparison.Ordinal)
                && string.Equals(typeRefNamespace, "System.Runtime.Versioning", StringComparison.Ordinal);
        }

        public static ImmutableArray<string> GetParameterValues(this MetadataReader metadataReader, CustomAttribute customAttribute)
        {
            if (customAttribute.Constructor.Kind != HandleKind.MemberReference)
            {
                throw new InvalidOperationException();
            }
            var ctor = metadataReader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
            var provider = new StringParameterValueTypeProvider(metadataReader, customAttribute.Value);
            var signature = ctor.DecodeMethodSignature<string, object>(provider, new GenericContext());
            return signature.ParameterTypes;
            //return default;
        }
    }

    internal class StringParameterValueTypeProvider : ISignatureTypeProvider<string, object>
    {
        public StringParameterValueTypeProvider(MetadataReader reader, BlobHandle handle )
        {

        }

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            throw new NotImplementedException();
        }

        public string GetByReferenceType(string elementType)
        {
            throw new NotImplementedException();
        }

        public string GetFunctionPointerType(MethodSignature<string> signature)
        {
            throw new NotImplementedException();
        }

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            throw new NotImplementedException();
        }

        public string GetGenericMethodParameter(object genericContext, int index)
        {
            throw new NotImplementedException();
        }

        public string GetGenericTypeParameter(object genericContext, int index)
        {
            throw new NotImplementedException();
        }

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
        {
            throw new NotImplementedException();
        }

        public string GetPinnedType(string elementType)
        {
            throw new NotImplementedException();
        }

        public string GetPointerType(string elementType)
        {
            throw new NotImplementedException();
        }

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            throw new NotImplementedException();
        }

        public string GetSZArrayType(string elementType)
        {
            throw new NotImplementedException();
        }

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }

        public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }
    }

    internal struct GenericContext
    {
    }

    internal static class StringExtension
    {
        public static int GetStableHashCode(this string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}