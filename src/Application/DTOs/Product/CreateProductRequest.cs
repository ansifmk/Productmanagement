using System.Collections.Generic;

namespace ProductManagement.Application.DTOs.Product
{
    public record CreateProductRequest(
        string ProductName,
        string CreatedBy,
        ICollection<CreateItemRequest> Items
    );
}
