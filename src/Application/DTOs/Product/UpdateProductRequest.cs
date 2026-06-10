using System.Collections.Generic;

namespace ProductManagement.Application.DTOs.Product
{
    public record UpdateProductRequest(
        string ProductName,
        string ModifiedBy,
        ICollection<CreateItemRequest> Items
    );
}
