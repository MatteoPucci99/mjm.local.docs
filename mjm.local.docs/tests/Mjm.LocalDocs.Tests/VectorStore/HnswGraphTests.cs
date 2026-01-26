using Mjm.LocalDocs.Infrastructure.VectorStore.Hnsw;

namespace Mjm.LocalDocs.Tests.VectorStore;

/// <summary>
/// Unit tests for <see cref="HnswGraph"/>.
/// </summary>
public sealed class HnswGraphTests
{
    #region Helper Methods

    private static ReadOnlyMemory<float> CreateRandomVector(int dimensions = 128, int seed = 42)
    {
        var random = new Random(seed);
        var vector = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = (float)random.NextDouble();
        }
        // Normalize
        var magnitude = Math.Sqrt(vector.Sum(v => v * v));
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] /= (float)magnitude;
        }
        return vector;
    }

    #endregion

    #region Basic Operations Tests

    [Fact]
    public void Add_SingleVector_CountIsOne()
    {
        // Arrange
        var graph = new HnswGraph();

        // Act
        graph.Add("id-1", CreateRandomVector());

        // Assert
        Assert.Equal(1, graph.Count);
    }

    [Fact]
    public void Add_MultipleVectors_CountIsCorrect()
    {
        // Arrange
        var graph = new HnswGraph();

        // Act
        for (int i = 0; i < 100; i++)
        {
            graph.Add($"id-{i}", CreateRandomVector(seed: i));
        }

        // Assert
        Assert.Equal(100, graph.Count);
    }

    [Fact]
    public void Add_DuplicateId_UpdatesExisting()
    {
        // Arrange
        var graph = new HnswGraph();
        graph.Add("id-1", CreateRandomVector(seed: 1));

        // Act
        graph.Add("id-1", CreateRandomVector(seed: 2));

        // Assert
        Assert.Equal(1, graph.Count);
    }

    [Fact]
    public void Remove_ExistingVector_ReturnsTrue()
    {
        // Arrange
        var graph = new HnswGraph();
        graph.Add("id-1", CreateRandomVector());

        // Act
        var result = graph.Remove("id-1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Remove_NonExistingVector_ReturnsFalse()
    {
        // Arrange
        var graph = new HnswGraph();

        // Act
        var result = graph.Remove("non-existing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_ExistingId_ReturnsTrue()
    {
        // Arrange
        var graph = new HnswGraph();
        graph.Add("id-1", CreateRandomVector());

        // Assert
        Assert.True(graph.Contains("id-1"));
    }

    [Fact]
    public void Contains_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var graph = new HnswGraph();

        // Assert
        Assert.False(graph.Contains("non-existing"));
    }

    [Fact]
    public void GetAllIds_ReturnsAllIds()
    {
        // Arrange
        var graph = new HnswGraph();
        var expectedIds = new[] { "id-1", "id-2", "id-3" };
        foreach (var id in expectedIds)
        {
            graph.Add(id, CreateRandomVector(seed: id.GetHashCode()));
        }

        // Act
        var actualIds = graph.GetAllIds().ToList();

        // Assert
        Assert.Equal(expectedIds.Length, actualIds.Count);
        Assert.All(expectedIds, id => Assert.Contains(id, actualIds));
    }

    #endregion

    #region Search Tests

    [Fact]
    public void Search_EmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = new HnswGraph();

        // Act
        var results = graph.Search(CreateRandomVector(), 10);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_SingleVector_FindsIt()
    {
        // Arrange
        var graph = new HnswGraph();
        var vector = CreateRandomVector();
        graph.Add("id-1", vector);

        // Act
        var results = graph.Search(vector, 10);

        // Assert
        Assert.Single(results);
        Assert.Equal("id-1", results[0].Id);
        Assert.True(results[0].Distance < 0.01, "Self-distance should be ~0");
    }

    [Fact]
    public void Search_MultipleVectors_ReturnsCorrectCount()
    {
        // Arrange
        var graph = new HnswGraph();
        for (int i = 0; i < 100; i++)
        {
            graph.Add($"id-{i}", CreateRandomVector(seed: i));
        }

        // Act
        var results = graph.Search(CreateRandomVector(seed: 42), 10);

        // Assert
        Assert.Equal(10, results.Count);
    }

    [Fact]
    public void Search_ResultsOrderedByDistance()
    {
        // Arrange
        var graph = new HnswGraph();
        for (int i = 0; i < 50; i++)
        {
            graph.Add($"id-{i}", CreateRandomVector(seed: i));
        }

        // Act
        var results = graph.Search(CreateRandomVector(seed: 25), 10);

        // Assert
        for (int i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].Distance <= results[i].Distance,
                $"Results should be ordered by distance. Index {i - 1} ({results[i - 1].Distance}) > Index {i} ({results[i].Distance})");
        }
    }

    [Fact]
    public void Search_DeletedVectors_NotReturned()
    {
        // Arrange
        var graph = new HnswGraph();
        graph.Add("id-1", CreateRandomVector(seed: 1));
        graph.Add("id-2", CreateRandomVector(seed: 2));
        graph.Add("id-3", CreateRandomVector(seed: 3));

        // Act
        graph.Remove("id-2");
        var results = graph.Search(CreateRandomVector(seed: 2), 10);

        // Assert
        Assert.DoesNotContain(results, r => r.Id == "id-2");
    }

    [Fact]
    public void Search_LimitGreaterThanCount_ReturnsAll()
    {
        // Arrange
        var graph = new HnswGraph();
        for (int i = 0; i < 5; i++)
        {
            graph.Add($"id-{i}", CreateRandomVector(seed: i));
        }

        // Act
        var results = graph.Search(CreateRandomVector(), 100);

        // Assert
        Assert.Equal(5, results.Count);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void SerializeDeserialize_PreservesData()
    {
        // Arrange
        var graph = new HnswGraph();
        for (int i = 0; i < 20; i++)
        {
            graph.Add($"id-{i}", CreateRandomVector(seed: i));
        }

        // Act
        var data = graph.Serialize();
        var newGraph = new HnswGraph();
        newGraph.Deserialize(data);

        // Assert
        Assert.Equal(graph.Count, newGraph.Count);
        Assert.All(graph.GetAllIds(), id => Assert.True(newGraph.Contains(id)));
    }

    [Fact]
    public void SerializeDeserialize_SearchStillWorks()
    {
        // Arrange
        var graph = new HnswGraph();
        var queryVector = CreateRandomVector(seed: 42);
        graph.Add("query", queryVector);
        for (int i = 0; i < 20; i++)
        {
            graph.Add($"id-{i}", CreateRandomVector(seed: i));
        }

        // Act
        var data = graph.Serialize();
        var newGraph = new HnswGraph();
        newGraph.Deserialize(data);

        var results = newGraph.Search(queryVector, 5);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal("query", results[0].Id);
        Assert.True(results[0].Distance < 0.01, "Self-search should return near-zero distance");
    }

    [Fact]
    public void SerializeDeserialize_ExcludesDeletedNodes()
    {
        // Arrange
        var graph = new HnswGraph();
        graph.Add("keep-1", CreateRandomVector(seed: 1));
        graph.Add("delete-1", CreateRandomVector(seed: 2));
        graph.Add("keep-2", CreateRandomVector(seed: 3));
        graph.Remove("delete-1");

        // Act
        var data = graph.Serialize();
        var newGraph = new HnswGraph();
        newGraph.Deserialize(data);

        // Assert
        Assert.Equal(2, newGraph.Count);
        Assert.True(newGraph.Contains("keep-1"));
        Assert.True(newGraph.Contains("keep-2"));
        Assert.False(newGraph.Contains("delete-1"));
    }

    #endregion

    #region Performance/Scale Tests

    [Fact]
    public void Search_LargeDataset_ReturnsInReasonableTime()
    {
        // Arrange
        var graph = new HnswGraph(maxConnections: 16, efConstruction: 100);
        const int vectorCount = 1000;
        
        for (int i = 0; i < vectorCount; i++)
        {
            graph.Add($"id-{i}", CreateRandomVector(seed: i));
        }

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = graph.Search(CreateRandomVector(seed: 500), 10, efSearch: 50);
        sw.Stop();

        // Assert
        Assert.Equal(10, results.Count);
        // HNSW should be fast - way under 100ms for 1000 vectors
        Assert.True(sw.ElapsedMilliseconds < 100, 
            $"Search took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    #endregion
}
