using FastPaymentIdemCache.Storage;

namespace FastPaymentIdemCache.Tests;

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
        // Ключ читателей намеренно без значения: тест проверяет статистику блокировки
        // и счётчиков под конкурентной нагрузкой, путь чтения (cache miss) на счётчики
        // не влияет и от формата хранения значений не зависит.
        var writeKey = "write_item";
        var readKey = "read_item";

        var readerTasks = Enumerable.Range(start: 0, readerTasksCount)
            .Select(_ =>
                    {
                        return Task.Run(() =>
                        {
                            for (var i = 0; i < readCount; i++)
                            {
                                store.Get(readKey);
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
                                store.Set(writeKey, new());
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