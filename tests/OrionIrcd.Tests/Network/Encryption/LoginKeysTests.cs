using OrionIrcd.Network.Encryption;

namespace OrionIrcd.Tests.Network.Encryption;

public class LoginKeysTests
{
    [Fact]
    public void GetKeys_ConcurrentCalls_DoNotCorruptCache()
    {
        // Regression: previous implementation used a non-thread-safe Dictionary,
        // which could throw or return wrong values under concurrent access.
        const int threadCount = 16;
        const int iterations = 500;
        var errors = 0;

        var threads = new Thread[threadCount];

        for (var t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            threads[t] = new(
                () =>
                {
                    try
                    {
                        for (var i = 0; i < iterations; i++)
                        {
                            var major = threadIndex % 8;
                            var minor = i % 4;
                            var revision = (threadIndex + i) % 16;
                            var keys = LoginKeys.GetKeys(major, minor, revision);

                            // Re-query and assert determinism.
                            var keys2 = LoginKeys.GetKeys(major, minor, revision);

                            if (keys.Key1 != keys2.Key1 || keys.Key2 != keys2.Key2)
                            {
                                Interlocked.Increment(ref errors);
                            }
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref errors);
                    }
                }
            );
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Assert.Equal(0, errors);
    }

    [Fact]
    public void GetKeys_DifferentVersions_ReturnDifferentValues()
    {
        var v7 = LoginKeys.GetKeys(7, 0, 1);
        var v6 = LoginKeys.GetKeys(6, 0, 1);

        Assert.NotEqual(v7.Key1, v6.Key1);
    }

    [Fact]
    public void GetKeys_SameVersion_ReturnsSameValues()
    {
        var first = LoginKeys.GetKeys(7, 0, 1);
        var second = LoginKeys.GetKeys(7, 0, 1);

        Assert.Equal(first.Key1, second.Key1);
        Assert.Equal(first.Key2, second.Key2);
    }

    [Fact]
    public void LegacyKeys_ReturnsPrecomputedSpan()
    {
        var keys = LoginKeys.LegacyKeys;

        Assert.False(keys.IsEmpty);
    }
}
