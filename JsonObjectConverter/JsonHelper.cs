namespace JsonObjectConverter;

using System.Text;

public class JsonHelper
{
	public string Line
	{
		get; set;
	}

	public string MainLine
	{
		get; set;
	}

	public List<Tuple<string, string>> SubLines { get; set; } = [];

	public JsonHelper(string line, StringBuilder mainLine, List<Tuple<string, StringBuilder>> subLine)
	{
		Line = line;
		MainLine = mainLine.ToString();

		foreach (Tuple<string, StringBuilder> sb in subLine)
		{
			SubLines.Add(new Tuple<string, string>(sb.Item1, sb.Item2.ToString()));
		}
	}
}
