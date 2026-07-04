using CodeBridge.Flow;

namespace CodeBridge.Flow.Execution;

internal static class FlowGraphSorter
{
    public static IReadOnlyList<FlowNode> Sort(
        FlowDocument document,
        IReadOnlyDictionary<string, FlowNode>? nodesById = null)
    {
        nodesById ??= document.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);

        var incomingCounts = document.Nodes.ToDictionary(node => node.Id, _ => 0, StringComparer.OrdinalIgnoreCase);
        var outgoing = document.Nodes.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var connection in document.Connections)
        {
            if (!nodesById.ContainsKey(connection.FromNodeId) || !nodesById.ContainsKey(connection.ToNodeId))
                continue;

            outgoing[connection.FromNodeId].Add(connection.ToNodeId);
            incomingCounts[connection.ToNodeId]++;
        }

        var queue = new Queue<FlowNode>(
            document.Nodes.Where(node => incomingCounts[node.Id] == 0));

        var sorted = new List<FlowNode>();

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            sorted.Add(node);

            foreach (var targetNodeId in outgoing[node.Id])
            {
                incomingCounts[targetNodeId]--;
                if (incomingCounts[targetNodeId] == 0)
                    queue.Enqueue(nodesById[targetNodeId]);
            }
        }

        if (sorted.Count != document.Nodes.Count)
            throw new InvalidOperationException("Flow graph contains a cycle. Cycles are not supported by the MVP runtime.");

        return sorted;
    }
}
