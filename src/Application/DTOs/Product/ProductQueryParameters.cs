namespace ProductManagement.Application.DTOs.Product
{
    public record ProductQueryParameters(int PageNumber = 1, int PageSize = 20);
}
