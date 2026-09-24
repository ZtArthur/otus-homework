using System.Text;
using FastPaymentIdemCache.Models;

namespace FastPaymentIdemCache.Tests;

public class UserPaymentOperationBinarySerializerTests
{
    [Fact]
    public void serialize_to_binary_writes_transaction_id_date_and_user_id()
    {
        var operation = new UserPaymentOperation
        {
            TransactionId = Guid.NewGuid(),
            TransactionDate = DateTime.UtcNow,
            UserId = long.MaxValue
        };

        using var ms = new MemoryStream();
        operation.SerializeToBinary(ms);
        ms.Position = 0;

        var restored = DeserializeFromBinary(ms);

        Assert.Equal(operation.TransactionId, restored.TransactionId);
        Assert.Equal(operation.TransactionDate, restored.TransactionDate);
        Assert.Equal(operation.UserId, restored.UserId);
    }

    // Ручная десериализация живёт только в тестах: генератор эмитит лишь запись,
    // продакшен GET отдаёт сохранённые байты как есть (десериализация — точка
    // улучшения для замеров на этапе оптимизации).
    private static UserPaymentOperation DeserializeFromBinary(Stream stream)
    {
        using var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        return new UserPaymentOperation
        {
            TransactionId = new Guid(br.ReadBytes(16)),
            TransactionDate = DateTime.FromBinary(br.ReadInt64()),
            UserId = br.ReadInt64()
        };
    }
}
