using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace TagStatistics
{
    public class PostParser
    {
        XmlReader reader;
        StreamWriter writer;

        public PostParser(XmlReader reader, StreamWriter writer)
        {
            this.reader = reader;
            this.writer = writer;
        }

        public void ProcessPost(Dictionary<string, int> tags, Dictionary<DateTime, int> days, Dictionary<string, Dictionary<string, int>> relations)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (reader.Name == "row")
                    {
                        while (reader.MoveToNextAttribute())
                        {
                            switch (reader.Name)
                            {
                                case "CreationDate":
                                    // Updates the date dimension table.
                                    DateTime dt = Convert.ToDateTime(reader.Value);
                                    DateTime mon = dt.AddDays(DayOfWeek.Monday - dt.DayOfWeek);
                                    int dayval;
                                    if (days.TryGetValue(mon.Date, out dayval))
                                    {
                                        writer.Write(dayval);
                                        writer.Write(',');
                                    }
                                    else
                                    {
                                        writer.Write(days.Count);
                                        writer.Write(',');
                                        days[mon.Date] = days.Count;
                                    }
                                    break;

                                case "Tags":
                                    var list = Regex.Matches(reader.Value, @"[\+\-#._:A-Za-z0-9]+").Cast<Match>().Select(m => m.Value).ToList();

                                    if (list.Count > 0)
                                    {
                                        list.Sort();

                                        // Updates the tag dimension table and writes the indexes to the fact file.
                                        var builder = new StringBuilder();
                                        foreach (var tag in list)
                                        {
                                            if (builder.Length > 0)
                                                builder.Append(';');
                                            int tagval;
                                            if (tags.TryGetValue(tag, out tagval))
                                            {
                                                builder.Append(tagval);
                                            }
                                            else
                                            {
                                                builder.Append(tags.Count);
                                                tags[tag] = tags.Count;
                                            }
                                        }
                                        writer.Write(builder);

                                        // Updates relation graph.
                                        for (int i = 0; i < list.Count; i++)
                                        {
                                            for (int j = i + 1; j < list.Count; j++)
                                            {
                                                Dictionary<string, int> neighbors;
                                                if (relations.TryGetValue(list[i], out neighbors))
                                                {
                                                    int weight;
                                                    if (neighbors.TryGetValue(list[j], out weight))
                                                    {
                                                        neighbors[list[j]] = weight + 1;
                                                    }
                                                    else
                                                    {
                                                        neighbors[list[j]] = 1;
                                                    }
                                                }
                                                else
                                                {
                                                    neighbors = new Dictionary<string, int>();
                                                    neighbors[list[j]] = 1;
                                                    relations[list[i]] = neighbors;
                                                }
                                            }
                                        }
                                    }

                                    break;
                            }
                        }
                    }

                    reader.MoveToElement();
                    if (reader.IsEmptyElement)
                        writer.WriteLine();

                    break;

                case XmlNodeType.EndElement:
                    writer.WriteLine();
                    break;
            }
        }

    }
}
