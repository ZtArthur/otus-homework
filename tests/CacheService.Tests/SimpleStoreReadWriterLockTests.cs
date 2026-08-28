using CacheService.Storage;

namespace CacheService.Tests;

public class SimpleStoreReadWriterLockTests
{
    [Theory]
    [InlineData(5, 5, 50, 50)]
    [InlineData(25, 25, 150, 150)]
    public async Task should_return_correct_read_write_statistics(int readerTasksCount, int writerTasksCount, int readCount, int writeCount)
    {
        using var store = new SimpleStore();
        var expectedReadCount = readerTasksCount * readCount;
        var expectedWriteCount = writerTasksCount * writeCount;
        var key = "item";

        var readerTasks = Enumerable.Range(start: 0, readerTasksCount)
            .Select(_ =>
                {
                    return Task.Run(() =>
                        {
                            for (var i = 0; i < readCount; i++)
                            {
                                store.Get(key);
                            }
                        }
                    );
                }
            )
            .ToArray();

        var writerTasks = Enumerable.Range(start: 0, writerTasksCount)
            .Select(_ =>
                {
                    return Task.Run(() =>
                        {
                            for (var i = 0; i < writeCount; i++)
                            {
                                store.Set(key, new());
                            }
                        }
                    );
                }
            )
            .ToArray();

        await Task.WhenAll([..readerTasks, ..writerTasks]);

        var statistics = store.GetStatistics();

        Assert.Equal(expectedReadCount, statistics.getCount);
        Assert.Equal(expectedWriteCount, statistics.setCount);
    }
}