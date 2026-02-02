using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Mjm.LocalDocs.Infrastructure.VectorStore.Hnsw;

/// <summary>
/// Hierarchical Navigable Small World (HNSW) graph for approximate nearest neighbor search.
/// This is a simplified, self-contained implementation optimized for embedding search.
/// </summary>
/// <remarks>
/// Based on the paper: "Efficient and robust approximate nearest neighbor search using 
/// Hierarchical Navigable Small World graphs" (https://arxiv.org/abs/1603.09320)
/// </remarks>
public sealed class HnswGraph
{
    private readonly int _maxConnections; // M parameter
    private readonly int _maxConnectionsAtLevel0; // M0 = 2 * M
    private readonly int _efConstruction;
    private readonly double _levelMultiplier; // 1 / ln(M)

    private readonly List<HnswNode> _nodes = [];
    private readonly ConcurrentDictionary<string, int> _idToIndex = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Random _random;

    private int _entryPointIndex = -1;
    private int _maxLevel = -1;

    /// <summary>
    /// Creates a new HNSW graph.
    /// </summary>
    /// <param name="maxConnections">Maximum connections per node (M parameter). Default: 16.</param>
    /// <param name="efConstruction">Size of dynamic candidate list during construction. Default: 200.</param>
    /// <param name="seed">Random seed for reproducibility. Default: 42.</param>
    public HnswGraph(int maxConnections = 16, int efConstruction = 200, int seed = 42)
    {
        _maxConnections = maxConnections;
        _maxConnectionsAtLevel0 = maxConnections * 2;
        _efConstruction = efConstruction;
        _levelMultiplier = 1.0 / Math.Log(maxConnections);
        _random = new Random(seed);
    }

    /// <summary>
    /// Number of active (non-deleted) nodes in the graph.
    /// </summary>
    public int Count => _idToIndex.Count;

    /// <summary>
    /// Adds a vector with the given ID to the graph.
    /// </summary>
    /// <param name="id">Unique identifier for the vector.</param>
    /// <param name="vector">The embedding vector.</param>
    public void Add(string id, ReadOnlyMemory<float> vector)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_idToIndex.ContainsKey(id))
            {
                // Update existing - remove and re-add
                RemoveInternal(id);
            }

            var nodeLevel = GetRandomLevel();
            var newNodeIndex = _nodes.Count;
            var newNode = new HnswNode(id, vector.ToArray(), nodeLevel, _maxConnections, _maxConnectionsAtLevel0);
            _nodes.Add(newNode);
            _idToIndex[id] = newNodeIndex;

            if (_entryPointIndex == -1)
            {
                // First node
                _entryPointIndex = newNodeIndex;
                _maxLevel = nodeLevel;
                return;
            }

            var entryPoint = _entryPointIndex;
            var currentMaxLevel = _maxLevel;

            // Search from top to the level of the new node
            for (int level = currentMaxLevel; level > nodeLevel; level--)
            {
                entryPoint = SearchLayerGreedy(newNode.Vector, entryPoint, level);
            }

            // Insert at each level from nodeLevel down to 0
            for (int level = Math.Min(nodeLevel, currentMaxLevel); level >= 0; level--)
            {
                var candidates = SearchLayer(newNode.Vector, entryPoint, _efConstruction, level);
                var neighbors = SelectNeighbors(newNode.Vector, candidates, GetMaxConnections(level), level);

                // Connect new node to neighbors
                newNode.SetNeighbors(level, neighbors.Select(n => n.Index).ToList());

                // Connect neighbors back to new node
                foreach (var neighbor in neighbors)
                {
                    var neighborNode = _nodes[neighbor.Index];
                    var neighborConnections = neighborNode.GetNeighbors(level).ToList();
                    neighborConnections.Add(newNodeIndex);

                    // Prune if too many connections
                    if (neighborConnections.Count > GetMaxConnections(level))
                    {
                        var pruned = SelectNeighbors(
                            neighborNode.Vector,
                            neighborConnections.Select(i => new SearchCandidate(i, Distance(neighborNode.Vector, _nodes[i].Vector))).ToList(),
                            GetMaxConnections(level),
                            level);
                        neighborNode.SetNeighbors(level, pruned.Select(n => n.Index).ToList());
                    }
                    else
                    {
                        neighborNode.SetNeighbors(level, neighborConnections);
                    }
                }

                if (candidates.Count > 0)
                {
                    entryPoint = candidates.MinBy(c => c.Distance)!.Index;
                }
            }

            // Update entry point if new node has higher level
            if (nodeLevel > _maxLevel)
            {
                _entryPointIndex = newNodeIndex;
                _maxLevel = nodeLevel;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes a vector by ID from the graph.
    /// </summary>
    /// <param name="id">The vector ID to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool Remove(string id)
    {
        _lock.EnterWriteLock();
        try
        {
            return RemoveInternal(id);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private bool RemoveInternal(string id)
    {
        if (!_idToIndex.TryGetValue(id, out var index))
            return false;

        var node = _nodes[index];
        node.IsDeleted = true;
        _idToIndex.TryRemove(id, out _);

        // Note: We use soft deletion. The node stays in the graph but is marked as deleted.
        // This is simpler than full removal and maintains index stability.
        // Periodic compaction could be added for production use.

        return true;
    }

    /// <summary>
    /// Searches for the K nearest neighbors of the query vector.
    /// </summary>
    /// <param name="query">The query vector.</param>
    /// <param name="k">Number of neighbors to return.</param>
    /// <param name="efSearch">Search expansion factor (higher = more accurate but slower). Default: 50.</param>
    /// <returns>List of (ID, distance) pairs ordered by distance ascending.</returns>
    public IReadOnlyList<(string Id, double Distance)> Search(ReadOnlyMemory<float> query, int k, int efSearch = 50)
    {
        _lock.EnterReadLock();
        try
        {
            if (_entryPointIndex == -1 || _nodes.Count == 0)
                return [];

            var querySpan = query.Span;
            var entryPoint = _entryPointIndex;

            // Traverse from top level to level 1
            for (int level = _maxLevel; level > 0; level--)
            {
                entryPoint = SearchLayerGreedy(querySpan, entryPoint, level);
            }

            // Search at level 0 with efSearch candidates
            var candidates = SearchLayer(querySpan, entryPoint, Math.Max(efSearch, k), 0);

            // Filter deleted nodes and return top k
            return candidates
                .Where(c => !_nodes[c.Index].IsDeleted)
                .OrderBy(c => c.Distance)
                .Take(k)
                .Select(c => (_nodes[c.Index].Id, c.Distance))
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Serializes the graph to a byte array.
    /// </summary>
    public byte[] Serialize()
    {
        _lock.EnterReadLock();
        try
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Header
            writer.Write(_maxConnections);
            writer.Write(_efConstruction);
            writer.Write(_entryPointIndex);
            writer.Write(_maxLevel);

            // Nodes (excluding deleted)
            var activeNodes = _nodes.Where(n => !n.IsDeleted).ToList();
            writer.Write(activeNodes.Count);

            foreach (var node in activeNodes)
            {
                writer.Write(node.Id);
                writer.Write(node.Level);
                writer.Write(node.Vector.Length);
                foreach (var v in node.Vector)
                {
                    writer.Write(v);
                }

                // Neighbors at each level
                for (int level = 0; level <= node.Level; level++)
                {
                    var neighbors = node.GetNeighbors(level).ToList();
                    writer.Write(neighbors.Count);
                    foreach (var n in neighbors)
                    {
                        // Write the ID instead of index for portability
                        writer.Write(_nodes[n].Id);
                    }
                }
            }

            return ms.ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Deserializes the graph from a byte array.
    /// </summary>
    public void Deserialize(byte[] data)
    {
        _lock.EnterWriteLock();
        try
        {
            _nodes.Clear();
            _idToIndex.Clear();

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            // Header (maxConnections and efConstruction are already set in constructor)
            var savedMaxConnections = reader.ReadInt32();
            var savedEfConstruction = reader.ReadInt32();
            _entryPointIndex = reader.ReadInt32();
            _maxLevel = reader.ReadInt32();

            // First pass: read nodes
            var nodeCount = reader.ReadInt32();
            var neighborIds = new List<List<List<string>>>(nodeCount);

            for (int i = 0; i < nodeCount; i++)
            {
                var id = reader.ReadString();
                var level = reader.ReadInt32();
                var vectorLength = reader.ReadInt32();
                var vector = new float[vectorLength];
                for (int j = 0; j < vectorLength; j++)
                {
                    vector[j] = reader.ReadSingle();
                }

                var node = new HnswNode(id, vector, level, _maxConnections, _maxConnectionsAtLevel0);
                _nodes.Add(node);
                _idToIndex[id] = i;

                // Read neighbor IDs (will resolve to indices in second pass)
                var nodeNeighborIds = new List<List<string>>();
                for (int l = 0; l <= level; l++)
                {
                    var count = reader.ReadInt32();
                    var levelNeighbors = new List<string>(count);
                    for (int n = 0; n < count; n++)
                    {
                        levelNeighbors.Add(reader.ReadString());
                    }
                    nodeNeighborIds.Add(levelNeighbors);
                }
                neighborIds.Add(nodeNeighborIds);
            }

            // Second pass: resolve neighbor IDs to indices
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                var nodeNeighborIds = neighborIds[i];
                for (int level = 0; level <= node.Level; level++)
                {
                    var indices = nodeNeighborIds[level]
                        .Where(nid => _idToIndex.ContainsKey(nid))
                        .Select(nid => _idToIndex[nid])
                        .ToList();
                    node.SetNeighbors(level, indices);
                }
            }

            // Update entry point index if it references a valid node
            if (_entryPointIndex >= 0 && _entryPointIndex < _nodes.Count)
            {
                // Entry point index should be valid
            }
            else if (_nodes.Count > 0)
            {
                // Find highest level node as entry point
                _entryPointIndex = 0;
                _maxLevel = _nodes[0].Level;
                for (int i = 1; i < _nodes.Count; i++)
                {
                    if (_nodes[i].Level > _maxLevel)
                    {
                        _maxLevel = _nodes[i].Level;
                        _entryPointIndex = i;
                    }
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets all IDs in the graph.
    /// </summary>
    public IEnumerable<string> GetAllIds()
    {
        _lock.EnterReadLock();
        try
        {
            return _nodes.Where(n => !n.IsDeleted).Select(n => n.Id).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Checks if the graph contains the given ID.
    /// </summary>
    public bool Contains(string id)
    {
        _lock.EnterReadLock();
        try
        {
            return _idToIndex.ContainsKey(id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private int GetRandomLevel()
    {
        var r = _random.NextDouble();
        return (int)(-Math.Log(r) * _levelMultiplier);
    }

    private int GetMaxConnections(int level) => level == 0 ? _maxConnectionsAtLevel0 : _maxConnections;

    private int SearchLayerGreedy(ReadOnlySpan<float> query, int entryPoint, int level)
    {
        var currentBest = entryPoint;
        var currentDistance = Distance(query, _nodes[entryPoint].Vector);

        bool improved;
        do
        {
            improved = false;
            foreach (var neighborIndex in _nodes[currentBest].GetNeighbors(level))
            {
                var neighborDistance = Distance(query, _nodes[neighborIndex].Vector);
                if (neighborDistance < currentDistance)
                {
                    currentBest = neighborIndex;
                    currentDistance = neighborDistance;
                    improved = true;
                }
            }
        } while (improved);

        return currentBest;
    }

    private List<SearchCandidate> SearchLayer(ReadOnlySpan<float> query, int entryPoint, int ef, int level)
    {
        var visited = new HashSet<int> { entryPoint };
        var candidates = new PriorityQueue<SearchCandidate, double>(); // min-heap
        var results = new PriorityQueue<SearchCandidate, double>(Comparer<double>.Create((a, b) => b.CompareTo(a))); // max-heap

        var entryDistance = Distance(query, _nodes[entryPoint].Vector);
        candidates.Enqueue(new SearchCandidate(entryPoint, entryDistance), entryDistance);
        results.Enqueue(new SearchCandidate(entryPoint, entryDistance), entryDistance);

        while (candidates.Count > 0)
        {
            var current = candidates.Dequeue();

            // Get the furthest result
            if (results.Count >= ef)
            {
                results.TryPeek(out var furthest, out _);
                if (current.Distance > furthest!.Distance)
                    break;
            }

            foreach (var neighborIndex in _nodes[current.Index].GetNeighbors(level))
            {
                if (visited.Contains(neighborIndex))
                    continue;

                visited.Add(neighborIndex);
                var neighborDistance = Distance(query, _nodes[neighborIndex].Vector);

                results.TryPeek(out var furthestResult, out _);
                if (results.Count < ef || neighborDistance < furthestResult!.Distance)
                {
                    candidates.Enqueue(new SearchCandidate(neighborIndex, neighborDistance), neighborDistance);
                    results.Enqueue(new SearchCandidate(neighborIndex, neighborDistance), neighborDistance);

                    if (results.Count > ef)
                    {
                        results.Dequeue();
                    }
                }
            }
        }

        // Extract results
        var resultList = new List<SearchCandidate>(results.Count);
        while (results.Count > 0)
        {
            resultList.Add(results.Dequeue());
        }
        return resultList;
    }

    private List<SearchCandidate> SelectNeighbors(
        ReadOnlySpan<float> nodeVector,
        List<SearchCandidate> candidates,
        int maxNeighbors,
        int level)
    {
        // Simple selection: take closest neighbors
        return candidates
            .OrderBy(c => c.Distance)
            .Take(maxNeighbors)
            .ToList();
    }

    /// <summary>
    /// Computes cosine distance (1 - cosine_similarity) between two vectors.
    /// Lower distance = more similar.
    /// </summary>
    private static double Distance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            return double.MaxValue;

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        var magnitude = Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB);
        if (magnitude == 0)
            return 1.0;

        var similarity = dotProduct / magnitude;
        return 1.0 - similarity; // Convert similarity to distance
    }

    private sealed record SearchCandidate(int Index, double Distance);
}

/// <summary>
/// Represents a node in the HNSW graph.
/// </summary>
internal sealed class HnswNode
{
    public string Id { get; }
    public float[] Vector { get; }
    public int Level { get; }
    public bool IsDeleted { get; set; }

    private readonly List<int>[] _neighbors;
    private readonly object _lock = new();

    public HnswNode(string id, float[] vector, int level, int maxConnections, int maxConnectionsAtLevel0)
    {
        Id = id;
        Vector = vector;
        Level = level;
        _neighbors = new List<int>[level + 1];
        for (int i = 0; i <= level; i++)
        {
            var capacity = i == 0 ? maxConnectionsAtLevel0 : maxConnections;
            _neighbors[i] = new List<int>(capacity);
        }
    }

    public IReadOnlyList<int> GetNeighbors(int level)
    {
        if (level > Level)
            return [];

        lock (_lock)
        {
            return _neighbors[level].ToList();
        }
    }

    public void SetNeighbors(int level, List<int> neighbors)
    {
        if (level > Level)
            return;

        lock (_lock)
        {
            _neighbors[level].Clear();
            _neighbors[level].AddRange(neighbors);
        }
    }
}
