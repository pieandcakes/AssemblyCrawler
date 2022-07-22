using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyCrawler
{
    internal class AssemblyInfo : IDisposable
    {
        public readonly Lazy<string> Name;
        public readonly Lazy<Version> AssemblyVersion;
        public readonly Lazy<Version> FileVersion;
        public Lazy<ulong> FileSize;
        public Lazy<string> PublicKeyToken;

        public string Path => file.DirectoryName;

        private Lazy<Assembly> assembly;
        private bool disposedValue;

        private FileInfo file { get; set; }

        public AssemblyInfo(FileInfo file)
        {
            this.file = file;
            Name = new Lazy<string>(() => file.Name.ToLowerInvariant());
            FileVersion = new Lazy<Version>(() => new Version(FileVersionInfo.GetVersionInfo(file.FullName)?.FileVersion?.Split(' ')[0] ?? "0.0.999.9"));
            FileSize = new Lazy<ulong>(() => (ulong)file.Length);

            this.assembly = new Lazy<Assembly>(GetAssembly);
            this.AssemblyVersion = new Lazy<Version>(GetAssemblyVersion);
            this.PublicKeyToken = new Lazy<string>(GetPublicKeyToken);
        }

        private Assembly GetAssembly()
        {
            return Assembly.LoadFile(file.FullName);
        }

        private Version GetAssemblyVersion()
        {
            return assembly.Value.GetName().Version;
        }

        private string GetPublicKey()
        {
            return HexByteArrayToString(assembly.Value.GetName().GetPublicKey());
        }

        private string GetPublicKeyToken()
        {
            return HexByteArrayToString(assembly.Value.GetName().GetPublicKeyToken());
        }

        private static string HexByteArrayToString(byte[] b)
        {
            if (b != null && b.Any())
            {
                return BitConverter.ToString(b).Replace("-", "").ToLowerInvariant();
            }
            return string.Empty;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.file != null) file = null;
                    if (this.assembly != null)
                    {
                        this.assembly = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AssemblyInfo()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override int GetHashCode()
        {
            var hash = Name.Value.GetHashCode() ^ AssemblyVersion.Value.GetHashCode() ^ FileVersion.Value.GetHashCode() ^ FileSize.Value.GetHashCode();

            if (!string.IsNullOrWhiteSpace(PublicKeyToken.Value))
                return hash ^ PublicKeyToken.Value.GetHashCode();

            return hash;
        }
    }
}
