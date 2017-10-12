using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;

namespace FontRegAuto {
    public static class EmbeddedResource
    {
        public static Stream GetResource(string name, Assembly assembly = null)
        {
            Assembly assembly1 = assembly;
            if ((object)assembly1 == null)
                assembly1 = Assembly.GetCallingAssembly();
            Assembly assembly2 = assembly1;
            string name1 = ((IEnumerable<string>)assembly2.GetManifestResourceNames()).FirstOrDefault<string>((Func<string, bool>)(mrn => mrn.Contains(name)));
            if (name1 == null)
                throw new FileNotFoundException(string.Format("Could not find a resource that contains the name '{0}'", (object)name));
            return assembly2.GetManifestResourceStream(name1);
        }

        public static void ExportZipResource(DirectoryInfo dir, string resourcename, Assembly assembly = null)
        {
            if (dir == null)
                throw new ArgumentNullException(nameof(dir));
            if (!dir.Exists)
                dir.Create();
            EmbeddedResource.ExportZipResource(dir.FullName, resourcename, assembly);
        }

        public static void ExportZipResource(string dir, string resourcename, Assembly assembly = null)
        {
            new FastZip().ExtractZip(EmbeddedResource.GetResource(resourcename, assembly), dir, FastZip.Overwrite.Always, (FastZip.ConfirmOverwriteDelegate)(name => true), (string)null, (string)null, true, true);
        }
    }
}