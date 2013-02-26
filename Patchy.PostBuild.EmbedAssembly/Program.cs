using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.IO;
using System.IO.Compression;

namespace Patchy.PostBuild.EmbedAssembly
{
    // Uses Mono.Cecil to embed assemblies into the installer
    class Program
    {
        static void Main(string[] args)
        {
            // Usage: Patchy.PostBuild.EmbedAssembly <target assembly> <assemblies...>
            AssemblyDefinition target;
            using (var stream = File.OpenRead(args[0]))
                target = AssemblyDefinition.ReadAssembly(stream);
            for (int i = 1; i < args.Length; i++)
            {
                var data = File.ReadAllBytes(args[i]);
                target.MainModule.Resources.Add(new EmbeddedResource(Path.GetFileName(args[i]), ManifestResourceAttributes.Public, data));
            }
            using (var stream = File.Create(args[0]))
            {
                var gStream = new GZipStream(stream, CompressionMode.Compress);
                target.Write(gStream);
            }
        }
    }
}
