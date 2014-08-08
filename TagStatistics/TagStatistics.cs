using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace TagStatistics
{
    /// <summary>
    /// Normalizes the stackoverflow data dump is too large, we'll need to normalize the entities such that they can get
    /// imported into Power Pivot for visualization.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var tags = new Dictionary<string, int>();
            var days = new Dictionary<DateTime, int>();
            var relations = new Dictionary<string, Dictionary<string, int>>();

            if (args.Length != 2)
            {
                Console.WriteLine("Usage: TagStatistics StackOverflowXmlFile OutputDirectory");
                return;
            }

            // Normalizes the stack overflow dump file. The mapping between posts and tags, tag dimension and date dimension will
            // be stored in post file, tags dictionary and days dictionary respectively. Also, the relationships between tags
            // will be stored in relations graph.
            using (var writer = new StreamWriter(Path.Combine(args[1], "Posts.csv")))
            {
                using (var reader = XmlReader.Create(args[0]))
                {
                    int i = 0;
                    var parser = new PostParser(reader, writer);

                    while (reader.Read())
                    {
                        if (++i % 100 == 0)
                            Console.WriteLine("{0} posts proceeded", i);

                        parser.ProcessPost(tags, days, relations);

                        // Uncommnet the following for testing on a small set of data.
                        //if (i > 5000)
                        //    break;
                    }

                    Console.WriteLine("{0:D} posts proceeded in total", i);
                }
            }

            // Generates the tag dimension file.
            using (var writer = new StreamWriter(Path.Combine(args[1], "Tags.csv")))
            {
                foreach (var key in tags.Keys)
                {
                    writer.WriteLine("{0},{1}", tags[key], key);
                }
            }

            // Generates the tag dimension file.
            using (var writer = new StreamWriter(Path.Combine(args[1], "Days.csv")))
            {
                foreach (var key in days.Keys)
                {
                    writer.WriteLine("{0},{1}", days[key], key);
                }
            }

            // Generates the json object for d3 graph from relation graph.
            using (var writer = new StreamWriter(Path.Combine(args[1], "Graph.js")))
            {
                var serializer = new JsonSerializer();
                var builder = new GraphBuilder();

                using (var jswriter = new JsonTextWriter(writer))
                {
                    jswriter.Formatting = Newtonsoft.Json.Formatting.Indented;

                    serializer.Serialize(jswriter, builder.Build(tags, relations));
                }
            }
        }
    }
}