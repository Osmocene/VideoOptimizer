using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VideoOptimizer
{
    public static class voSave
    {
        public static string FileInSameFolder(string asthispath, string newfilename)
        {
            return Path.Combine(Path.GetDirectoryName(asthispath), newfilename);
        }
        public static string ConventionalFilepath(string path, string filename)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), filename);
        }
        public static void CreateOrganizingFolder(string path)
        {
            Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)));
        }
        public static void SerializableObject<T>(T serializableObject, string filename)
        {
            string contents = Newtonsoft.Json.JsonConvert.SerializeObject(serializableObject, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filename, contents);
        }
        public static T DeserializeObject<T>(string filename)
        {
            Type type = typeof(T);
            T outobj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
            return outobj;
        }

    }
}
