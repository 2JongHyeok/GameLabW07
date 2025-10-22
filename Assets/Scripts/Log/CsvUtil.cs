using System.Collections.Generic;
using System.IO;
using System.Text;


public static class CsvUtil
{
    static readonly UTF8Encoding NoBom = new UTF8Encoding(false);


    public static StreamWriter Open(string path, string[] header)
    {
        bool writeHeader = !File.Exists(path);
        var sw = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read), NoBom);
        if (writeHeader) sw.WriteLine(Join(header));
        return sw;
    }


    public static string Join(IEnumerable<string> cols)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (var raw in cols)
        {
            if (!first) sb.Append(',');
            first = false;
            var s = raw ?? string.Empty;
            bool needQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            if (needQuote)
            {
                sb.Append('"');
                sb.Append(s.Replace("\"", "\"\""));
                sb.Append('"');
            }
            else sb.Append(s);
        }
        return sb.ToString();
    }
}