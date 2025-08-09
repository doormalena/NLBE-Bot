using System;
using System.Collections.Generic;
using System.Text;

namespace JsonObjectConverter
{
    public class JsonHelper
    {
        public string line { get; set; } = string.Empty;
        public string mainLine { get; set; } = string.Empty;
        public List<Tuple<string, string>> subLines { get; set; } = new List<Tuple<string, string>>();

        public JsonHelper(string line, StringBuilder mainLine, List<Tuple<string, StringBuilder>> subLine)
        {
            this.line = line;
            this.mainLine = mainLine.ToString();
            foreach (Tuple<string, StringBuilder> sb in subLine)
            {
                this.subLines.Add(new Tuple<string, string>(sb.Item1, sb.Item2.ToString()));
            }
        }
    }
}
