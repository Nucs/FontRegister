using System;
using System.IO;
using System.Linq;

namespace FontRegister.UnitTests;

internal class EmbeddedResourceHelper
{
    public static byte[] ReadEmbeddedResource(string resourceNameEndsWith)
    {
        var assembly = typeof(EmbeddedResourceHelper).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        var resourceName = resourceNames.FirstOrDefault(r => r.EndsWith(resourceNameEndsWith));

        if (resourceName == null)
            throw new ArgumentException($"Resource ending with '{resourceNameEndsWith}' not found in assembly.");

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly.");

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}