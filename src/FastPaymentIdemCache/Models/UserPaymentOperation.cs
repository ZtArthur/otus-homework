using Generators.BinarySerializer;

namespace FastPaymentIdemCache.Models;

[GenerateBinarySerializer]
public partial class UserPaymentOperation
{
    public Guid TransactionId { get; set; }

    public DateTime TransactionDate { get; set; }

    public long UserId { get; set; }
}
