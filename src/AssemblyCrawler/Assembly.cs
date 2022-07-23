using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        public Lazy<bool> IsManaged;

        public string Path => file.DirectoryName;

        private Lazy<AssemblyName> assemblyName;
        private bool disposedValue;

        private FileInfo file { get; set; }

        public AssemblyInfo(FileInfo file)
        {
            this.file = file;
            FileVersion = new Lazy<Version>(() => new Version(FileVersionInfo.GetVersionInfo(file.FullName)?.FileVersion?.Split(' ')[0] ?? "0.0.999.9"));
            FileSize = new Lazy<ulong>(() => (ulong)file.Length);

            this.assemblyName = new Lazy<AssemblyName>(GetAssemblyName);

            this.FName = new Lazy<string>(() => file.Name.ToLowerInvariant());

            this.AName = new Lazy<string>(() => assemblyName.Value.ToString());
            this.AssemblyVersion = new Lazy<Version>(GetAssemblyVersion);
            this.PublicKeyToken = new Lazy<string>(GetPublicKeyToken);
            this.IsManaged = new Lazy<bool>(() => this.IsManagedAssembly());
        }

        private AssemblyName GetAssemblyName()
        {
            return AssemblyName.GetAssemblyName(file.FullName);
        }

        private Version GetAssemblyVersion()
        {
            return assemblyName.Value.Version;
        }

        private string GetPublicKey()
        {
            return HexByteArrayToString(assemblyName.Value.GetPublicKey());
        }

        private string GetPublicKeyToken()
        {
            return HexByteArrayToString(assemblyName.Value.GetPublicKeyToken());
        }

        private static string HexByteArrayToString(byte[] b)
        {
            if (b != null && b.Any())
            {
                return BitConverter.ToString(b).Replace("-", "").ToLowerInvariant();
            }
            return string.Empty;
        }

        public override int GetHashCode()
        {
            var hash = FName.Value.GetHashCode() ^ AssemblyVersion.Value.GetHashCode() ^ FileVersion.Value.GetHashCode() ^ FileSize.Value.GetHashCode();

            if (!string.IsNullOrWhiteSpace(PublicKeyToken.Value))
                return hash ^ PublicKeyToken.Value.GetHashCode();

            return hash;
        }
    }
}
