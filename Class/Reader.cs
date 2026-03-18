using System;
using System.Collections.Generic;
using System.IO;

namespace PDMCD4
{
    public class Reader
    {
        private readonly string[] parms;
        private readonly string filePath;
        private readonly List<Line> lines = new List<Line>();

        public int Count => lines.Count;

        public Reader(string file, string[] parms)
        {
            this.parms = parms;
            filePath = file;
            ReadFile();
        }

        private void ReadFile()
        {
            using (var sr = new StreamReader(filePath))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line == null || line.StartsWith(";", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string strLine = line.Replace(Environment.NewLine, string.Empty);
                    for (int i = 0; i < parms.Length; i++)
                    {
                        if (i == 0)
                        {
                            strLine = strLine.Replace(parms[0], string.Empty);
                        }

                        strLine = strLine.Replace(parms[i], ",");
                    }

                    string[] values = strLine.Split(',');
                    var currentLine = new Line();

                    for (int i = 0; i < values.Length && i < parms.Length; i++)
                    {
                        string currentParameter = parms[i].Replace("[", string.Empty).Replace("]", string.Empty);
                        currentLine.AddParameter(currentParameter, values[i]);
                    }

                    lines.Add(currentLine);
                }
            }
        }

        public Line this[int index] => lines[index];
    }

    public class Line
    {
        private readonly Dictionary<string, string> row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string this[string key]
        {
            get
            {
                try
                {
                    return row[key];
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public void AddParameter(string parameter, string value)
        {
            row.Add(parameter, value);
        }

        public int ParameterCount => row.Count;

        public void ClearAllParameters()
        {
            row.Clear();
        }

        public void RemoveParameter(string parameter)
        {
            row.Remove(parameter);
        }
    }
}
