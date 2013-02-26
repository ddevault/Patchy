using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.IO;
using System.IO.Compression;
using System.Threading;

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
                var memStream = new MemoryStream();
                using (var stream = File.OpenRead(args[i]))
                {
                    using (var gStream = new GZipStream(memStream, CompressionMode.Compress))
                        stream.CopyTo(gStream);
                }
                var data = memStream.ToArray();
                target.MainModule.Resources.Add(new EmbeddedResource(Path.GetFileName(args[i]), ManifestResourceAttributes.Public, data));
            }
            using (var stream = File.Create(args[0]))
                target.Write(stream);
        }
    }
}
