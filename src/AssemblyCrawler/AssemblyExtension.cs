﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyCrawler
{
    internal static class AssemblyExtension
    {
        public static Dictionary<int, List<AssemblyInfo>> SortByHashCode(this List<AssemblyInfo> assemblyDetail)
        {
            Dictionary<int, List<AssemblyInfo>> samesies = new Dictionary<int, List<AssemblyInfo>>();

            foreach (var item in assemblyDetail.OrderByDescending(a => string.Concat(a.AssemblyVersion.Value.ToString(), a.FileVersion.Value.ToString())))
            {
                //Console.WriteLine($"{item.Path}");
                //Console.WriteLine($"  av:'{item.AssemblyVersion.Value}'");
                //Console.WriteLine($"  fv:'{item.FileVersion.Value}'");
                //Console.WriteLine($"  sz:'{item.FileSize.Value.ToString("N0")}'");

                int hc = item.GetHashCode();
                if (!samesies.ContainsKey(hc)) samesies[hc] = new List<AssemblyInfo>();
                samesies[hc].Add(item);
            }



            return samesies;
        }

        public static bool IsManagedAssembly(this AssemblyInfo assembly)
        {
            string path = Path.Combine(assembly.Path, assembly.FName.Value);

            return CheckIsManagedAssembly(path);
        }

        private static bool CheckIsManagedAssembly(string fileName)
        {
            using Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            using BinaryReader binaryReader = new(fileStream);
            if (fileStream.Length < 64)
            {
                return false;
            }

            //PE Header starts @ 0x3C (60). Its a 4 byte header.
            fileStream.Position = 0x3C;
            uint peHeaderPointer = binaryReader.ReadUInt32();
            if (peHeaderPointer == 0)
            {
                peHeaderPointer = 0x80;
            }

            // Ensure there is at least enough room for the following structures:
            //     24 byte PE Signature & Header
            //     28 byte Standard Fields         (24 bytes for PE32+)
            //     68 byte NT Fields               (88 bytes for PE32+)
            // >= 128 byte Data Dictionary Table
            if (peHeaderPointer > fileStream.Length - 256)
            {
                return false;
            }

            // Check the PE signature.  Should equal 'PE\0\0'.
            fileStream.Position = peHeaderPointer;
            uint peHeaderSignature = binaryReader.ReadUInt32();
            if (peHeaderSignature != 0x00004550)
            {
                return false;
            }

            // skip over the PEHeader fields
            fileStream.Position += 20;

            const ushort PE32 = 0x10b;
            const ushort PE32Plus = 0x20b;

            // Read PE magic number from Standard Fields to determine format.
            var peFormat = binaryReader.ReadUInt16();
            if (peFormat != PE32 && peFormat != PE32Plus)
            {
                return false;
            }

            // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
            // When this is non-zero then the file contains CLI data otherwise not.
            ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
            fileStream.Position = dataDictionaryStart;

            uint cliHeaderRva = binaryReader.ReadUInt32();
            if (cliHeaderRva == 0)
            {
                return false;
            }

            return true;
        }
    }
}