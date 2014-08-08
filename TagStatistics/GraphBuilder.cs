using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TagStatistics
{
    class Node
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "group")]
        public int Group { get; set; }
    }

    class Link
    {
        [JsonProperty(PropertyName = "source")]
        public int Source { get; set; }

        [JsonProperty(PropertyName = "target")]
        public int Target { get; set; }

        [JsonProperty(PropertyName = "value")]
        public int Value { get; set; }
    }

    class Graph
    {
        [JsonProperty(PropertyName = "nodes")]
        public Node[] Nodes { get; set; }

        [JsonProperty(PropertyName = "links")]
        public List<Link> Links { get; set; }
    }

    class GraphBuilder
    {
        public Graph Build(Dictionary<string, int> tags, Dictionary<string, Dictionary<string, int>> relations)
        {
            int magic = 10000;

            // Prunes the tree to remove edges with little weight.
            foreach (var relation in relations)
            {
                foreach (var node in relation.Value.Keys.Where(k => relation.Value[k] < magic).ToList())
                {
                    relation.Value.Remove(node);
                }
            }

            // Prunes the tree to remove nodes with no edges.
            foreach (var node in relations.Keys.Where(k => relations[k].Count == 0).ToList())
            {
                relations.Remove(node);
            }

            // Builds the tags based on the pruned tree.
            tags.Clear();
            foreach (var relation in relations)
            {
                int val;
                if (!tags.TryGetValue(relation.Key, out val))
                    tags[relation.Key] = tags.Count;

                foreach (var target in relation.Value)
                {
                    if (!tags.TryGetValue(target.Key, out val))
                        tags[target.Key] = tags.Count;
                }
            }

            // Builds the graph json object for d3.
            var graph = new Graph();

            graph.Nodes = new Node[tags.Count];
            graph.Links = new List<Link>();

            foreach (var tag in tags)
            {
                var node = new Node();

                node.Name = tag.Key;
                node.Group = 1;

                graph.Nodes[tag.Value] = node;
            }

            foreach (var relation in relations)
            {
                foreach (var node in relation.Value)
                {
                    var link = new Link();

                    link.Source = tags[relation.Key];
                    link.Target = tags[node.Key];
                    link.Value = (int)Math.Sqrt(node.Value / magic);

                    graph.Links.Add(link);
                }
            }

            return graph;
        }
    }
}
