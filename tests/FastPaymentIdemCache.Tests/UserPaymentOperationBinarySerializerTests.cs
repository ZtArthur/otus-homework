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

        using var br = new BinaryReader(ms);
        var transactionId = new Guid(br.ReadBytes(16));
        var transactionDate = DateTime.FromBinary(br.ReadInt64());
        var userId = br.ReadInt64();

        Assert.Equal(operation.TransactionId, transactionId);
        Assert.Equal(operation.TransactionDate, transactionDate);
        Assert.Equal(operation.UserId, userId);
    }
}
