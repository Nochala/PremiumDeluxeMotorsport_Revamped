using System;
using System.IO;

namespace PDMCD4
{
    public static class CFGRead
    {
        public static string ReadCfgValue(string key, string file)
        {
            string[] lines = File.ReadAllLines(file);

            foreach (string line in lines)
            {
                if (line.StartsWith(key, StringComparison.Ordinal))
                {
                    string temp = line.Substring(key.Length + 1);
                    return temp.Replace("\"", string.Empty);
                }
            }

            return null;
        }

        public static void WriteCfgValue(string key, string value, string file)
        {
            string ext = Path.GetExtension(file);
            string tmp = file.Replace(ext, ".tmp");
            bool found = false;

            using (var sr = new StreamReader(file))
            using (var wr = new StreamWriter(tmp))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (line.StartsWith(key, StringComparison.Ordinal))
                    {
                        line = string.Format("{0} \"{1}\"", key, value);
                        found = true;
                    }

                    wr.WriteLine(line);
                }

                if (!found)
                {
                    wr.WriteLine(string.Format("{0} \"{1}\"", key, value));
                }
            }

            File.Delete(file);
            File.Move(tmp, file);
        }
    }
}
