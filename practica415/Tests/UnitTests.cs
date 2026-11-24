using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using practica415.Controllers;
using practica415.Data;
using practica415.Models;
using practica415.Services;
using Xunit;

namespace practica415.Tests;

public sealed class UnitTests
{
    [Fact]
    public void ResetPassword_SavesNewPassword_AndShowsItTemporarily()
    {
        var clientId = Guid.NewGuid();
        string? savedPassword = null;
        var accounts = new Mock<IAccountService>();
        accounts.Setup(x => x.ResetClientPassword(clientId, It.IsAny<string>()))
            .Callback<Guid, string>((_, password) => savedPassword = password)
            .Returns((Guid id, string _) => new AccountResult(true, new WarehouseUser { Id = id, Email = "client@example.com" }));
        var controller = CreateController(accounts.Object);

        var result = controller.ResetPassword(clientId);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(FitnessController.Index), ((RedirectToActionResult)result).ActionName);
        Assert.NotNull(savedPassword);
        Assert.Equal(12, savedPassword!.Length);
        Assert.Equal(clientId.ToString(), controller.TempData["NewClientId"]);
        Assert.Equal(savedPassword, controller.TempData["TemporaryPassword"]);
        accounts.Verify(x => x.ResetClientPassword(clientId, savedPassword), Times.Once);
    }

    [Fact]
    public void ResetPassword_WhenClientDoesNotExist_ShowsError()
    {
        var accounts = new Mock<IAccountService>();
        accounts.Setup(x => x.ResetClientPassword(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(new AccountResult(false, Error: "Клиент не найден."));
        var controller = CreateController(accounts.Object);

        controller.ResetPassword(Guid.NewGuid());

        Assert.Equal("Клиент не найден.", controller.TempData["Error"]);
        Assert.Null(controller.TempData["TemporaryPassword"]);
    }

    [Fact]
    public void ResetPassword_IsPostOnly_AndRestrictedToStaff()
    {
        var method = typeof(FitnessController).GetMethod(nameof(FitnessController.ResetPassword))!;

        Assert.NotNull(method.GetCustomAttributes(typeof(HttpPostAttribute), true).SingleOrDefault());
        Assert.NotNull(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
        var authorization = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
        Assert.Equal("Manager,Admin", authorization.Roles);
    }

    [Fact]
    public void ClientForm_WithValidData_PassesValidation()
    {
        var model = new ClientFormViewModel
        {
            FullName = "Иван Иванов", Phone = "+373 60000000", Email = "ivan@example.com",
            Login = "ivan", Plan = "3months", StartDate = new DateTime(2026, 11, 23)
        };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void ClientForm_WithInvalidEmail_FailsValidation()
    {
        var model = new ClientFormViewModel
        {
            FullName = "Иван Иванов", Phone = "+373 60000000", Email = "not-an-email", Login = "ivan", Plan = "3months"
        };

        Assert.Contains(Validate(model), error => error.MemberNames.Contains(nameof(ClientFormViewModel.Email)));
    }

    [Fact]
    public void ClientForm_WithoutName_FailsValidation()
    {
        var model = new ClientFormViewModel
        {
            FullName = "", Phone = "+373 60000000", Email = "ivan@example.com", Login = "ivan", Plan = "1month"
        };

        Assert.Contains(Validate(model), error => error.MemberNames.Contains(nameof(ClientFormViewModel.FullName)));
    }

    [Fact]
    public void Reports_UsesProjectedRowsAndDatabaseSummary()
    {
        var warehouse = new Mock<IWarehouseService>();
        var accounts = new Mock<IAccountService>();
        warehouse.Setup(x => x.GetReportProducts()).Returns(new[]
        {
            new ReportProductRow("Протеин", "PRO-01", "Питание", 4, "шт.", 1, 250m)
        });
        warehouse.Setup(x => x.GetReportMovements(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns(new[] { new ReportMovementRow(DateTime.Today, MovementType.Receipt, "Протеин", 4, "шт.") });
        accounts.Setup(x => x.GetClientMembershipSummary())
            .Returns(new ClientMembershipSummary(12, 8, 2, 3));

        var controller = new ReportsController(warehouse.Object, accounts.Object);
        var result = Assert.IsType<ViewResult>(controller.Index(null, null));
        var model = Assert.IsType<ReportsViewModel>(result.Model);

        Assert.Equal(12, model.ClientCount);
        Assert.Equal(8, model.ActiveMemberships);
        Assert.Equal(2, model.ExpiringMemberships);
        Assert.Equal(3, model.DebtorCount);
        Assert.Equal(1000m, model.StockValue);
        Assert.Single(model.Products);
        Assert.Single(model.Movements);
        warehouse.Verify(x => x.GetProducts(), Times.Never);
        warehouse.Verify(x => x.GetMovements(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
        accounts.Verify(x => x.GetClients(), Times.Never);
    }

    private static List<ValidationResult> Validate(object model)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), errors, validateAllProperties: true);
        return errors;
    }

    private static FitnessController CreateController(IAccountService accounts)
    {
        var httpContext = new DefaultHttpContext();
        var provider = new Mock<ITempDataProvider>();
        provider.Setup(x => x.LoadTempData(It.IsAny<HttpContext>())).Returns(new Dictionary<string, object>());
        return new FitnessController(accounts)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, provider.Object)
        };
    }
}
