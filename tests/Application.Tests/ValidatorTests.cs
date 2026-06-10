using System.Collections.Generic;
using FluentValidation.TestHelper;
using ProductManagement.Application.DTOs.Auth;
using ProductManagement.Application.DTOs.Product;
using ProductManagement.Application.Validators;
using Xunit;

namespace ProductManagement.Tests
{
    public class ValidatorTests
    {
        private readonly CreateProductRequestValidator _createProductValidator;
        private readonly UpdateProductRequestValidator _updateProductValidator;
        private readonly LoginRequestValidator _loginValidator;
        private readonly RegisterRequestValidator _registerValidator;
        private readonly ProductQueryParametersValidator _queryParametersValidator;

        public ValidatorTests()
        {
            _createProductValidator = new CreateProductRequestValidator();
            _updateProductValidator = new UpdateProductRequestValidator();
            _loginValidator = new LoginRequestValidator();
            _registerValidator = new RegisterRequestValidator();
            _queryParametersValidator = new ProductQueryParametersValidator();
        }

        [Fact]
        public void CreateProductRequestValidator_ValidRequest_Passes()
        {
            var model = new CreateProductRequest(
                "Valid Product",
                "Admin",
                new List<CreateItemRequest> { new CreateItemRequest(5) }
            );
            var result = _createProductValidator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void CreateProductRequestValidator_InvalidRequest_Fails()
        {
            var model = new CreateProductRequest(
                "",
                "Admin",
                new List<CreateItemRequest>() // Empty items
            );
            var result = _createProductValidator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.ProductName);
            result.ShouldHaveValidationErrorFor(x => x.Items);

            var modelWithInvalidItem = new CreateProductRequest(
                "Valid Product",
                "Admin",
                new List<CreateItemRequest> { new CreateItemRequest(0) } // Quantity 0 is invalid
            );
            var resultItem = _createProductValidator.TestValidate(modelWithInvalidItem);
            resultItem.ShouldHaveValidationErrorFor("Items[0].Quantity");
        }

        [Fact]
        public void UpdateProductRequestValidator_ValidRequest_Passes()
        {
            var model = new UpdateProductRequest(
                "Valid Product",
                "ModifiedUser",
                new List<CreateItemRequest> { new CreateItemRequest(5) }
            );
            var result = _updateProductValidator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void UpdateProductRequestValidator_InvalidRequest_Fails()
        {
            var model = new UpdateProductRequest(
                "",
                "ModifiedUser",
                new List<CreateItemRequest>()
            );
            var result = _updateProductValidator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.ProductName);
            result.ShouldHaveValidationErrorFor(x => x.Items);
        }

        [Fact]
        public void LoginRequestValidator_ValidRequest_Passes()
        {
            var model = new LoginRequest("test@example.com", "Password123!");
            var result = _loginValidator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void LoginRequestValidator_InvalidRequest_Fails()
        {
            var model = new LoginRequest("", "");
            var result = _loginValidator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void RegisterRequestValidator_ValidRequest_Passes()
        {
            var model = new RegisterRequest("username", "test@example.com", "Password123!");
            var result = _registerValidator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void RegisterRequestValidator_InvalidRequest_Fails()
        {
            // Empty inputs
            var model1 = new RegisterRequest("", "invalid-email", "pass");
            var result1 = _registerValidator.TestValidate(model1);
            result1.ShouldHaveValidationErrorFor(x => x.Username);
            result1.ShouldHaveValidationErrorFor(x => x.Email);
            result1.ShouldHaveValidationErrorFor(x => x.Password);

            // Password missing uppercase
            var model2 = new RegisterRequest("username", "test@example.com", "password123!");
            var result2 = _registerValidator.TestValidate(model2);
            result2.ShouldHaveValidationErrorFor(x => x.Password);

            // Password missing lowercase
            var model3 = new RegisterRequest("username", "test@example.com", "PASSWORD123!");
            var result3 = _registerValidator.TestValidate(model3);
            result3.ShouldHaveValidationErrorFor(x => x.Password);

            // Password missing digit
            var model4 = new RegisterRequest("username", "test@example.com", "Password!");
            var result4 = _registerValidator.TestValidate(model4);
            result4.ShouldHaveValidationErrorFor(x => x.Password);

            // Password missing special character
            var model5 = new RegisterRequest("username", "test@example.com", "Password123");
            var result5 = _registerValidator.TestValidate(model5);
            result5.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void ProductQueryParametersValidator_ValidRequest_Passes()
        {
            var model = new ProductQueryParameters { PageNumber = 1, PageSize = 10 };
            var result = _queryParametersValidator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ProductQueryParametersValidator_InvalidRequest_Fails()
        {
            var model = new ProductQueryParameters { PageNumber = 0, PageSize = 101 };
            var result = _queryParametersValidator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.PageNumber);
            result.ShouldHaveValidationErrorFor(x => x.PageSize);
        }
    }
}
