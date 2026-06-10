using System;
using System.Collections.Generic;

namespace ProductManagement.Application.DTOs.Product
{
    public record ProductDto(
        int Id,
        string ProductName,
        string CreatedBy,
        DateTime CreatedOn,
        string? ModifiedBy,
        DateTime? ModifiedOn,
        ICollection<ItemDto> Items
    );
}
