using System.Diagnostics;
using JobBoard.Application.Actions.Companies.Activate;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Messaging;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.Monolith.Contracts.Companies;
using JobBoard.Monolith.Tests.Unit.Application.Decorators;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class ActivateCompanyCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityFactory _activityFactory;
    private readonly IUnitOfWorkEvents _unitOfWorkEvents;
    private readonly ActivateCompanyCommandHandler _sut;

    public ActivateCompanyCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork, ITransactionDbContext>();
        _companyRepository = Substitute.For<ICompanyRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _activityFactory = Substitute.For<IActivityFactory>();
        _unitOfWorkEvents = Substitute.For<IUnitOfWorkEvents>();

        var changeTracker = new StubDbContext().ChangeTracker;
        ((ITransactionDbContext)_unitOfWork).ChangeTracker.Returns(changeTracker);

        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);

        var handlerContext = Substitute.For<IHandlerContext>();
        handlerContext.UnitOfWork.Returns(_unitOfWork);
        handlerContext.OutboxPublisher.Returns(Substitute.For<IOutboxPublisher>());
        handlerContext.MetricsService.Returns(Substitute.For<IMetricsService>());
        handlerContext.UnitOfWorkEvents.Returns(_unitOfWorkEvents);
        handlerContext.LoggerFactory.Returns(Substitute.For<ILoggerFactory>());

        _sut = new ActivateCompanyCommandHandler(handlerContext, _activityFactory, _companyRepository, _userRepository);
    }

    private static CompanyCreatedModel CreateModel() => new()
    {
        CompanyName = "Acme Corp",
        CompanyUId = Guid.NewGuid(),
        CompanyEmail = "info@acme.com",
        Auth0CompanyId = "auth0|company-123",
        Auth0UserId = "auth0|user-456",
        UserUId = Guid.NewGuid(),
        CreatedBy = "system"
    };

    private Company CreateTestCompany()
    {
        return Company.Create(new CompanyInput(
            InternalId: 1,
            Id: Guid.NewGuid(),
            Name: "Acme Corp",
            Email: "info@acme.com",
            Status: "Provisioning",
            IndustryId: 1));
    }

    private User CreateTestUser()
    {
        return User.Create("John", "Doe", "john@acme.com", null, Guid.NewGuid(), 1);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCompanyStatusToActive()
    {
        var model = CreateModel();
        var company = CreateTestCompany();
        var user = CreateTestUser();

        _companyRepository.GetCompanyById(model.CompanyUId, Arg.Any<CancellationToken>())
            .Returns(company);
        _userRepository.FindUserByUIdAsync(model.UserUId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new ActivateCompanyCommand(model) { UserId = "user-123" };
        await _sut.HandleAsync(command, CancellationToken.None);

        company.Status.ShouldBe("Active");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCompanyExternalId()
    {
        var model = CreateModel();
        var company = CreateTestCompany();
        var user = CreateTestUser();

        _companyRepository.GetCompanyById(model.CompanyUId, Arg.Any<CancellationToken>())
            .Returns(company);
        _userRepository.FindUserByUIdAsync(model.UserUId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new ActivateCompanyCommand(model) { UserId = "user-123" };
        await _sut.HandleAsync(command, CancellationToken.None);

        company.ExternalId.ShouldBe("auth0|company-123");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetUserExternalId()
    {
        var model = CreateModel();
        var company = CreateTestCompany();
        var user = CreateTestUser();

        _companyRepository.GetCompanyById(model.CompanyUId, Arg.Any<CancellationToken>())
            .Returns(company);
        _userRepository.FindUserByUIdAsync(model.UserUId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new ActivateCompanyCommand(model) { UserId = "user-123" };
        await _sut.HandleAsync(command, CancellationToken.None);

        user.ExternalId.ShouldBe("auth0|user-456");
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveChanges()
    {
        var model = CreateModel();
        SetupDefaultEntities(model);

        var command = new ActivateCompanyCommand(model) { UserId = "user-123" };
        await _sut.HandleAsync(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync("user-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldEnqueueUnitOfWorkEvent()
    {
        var model = CreateModel();
        SetupDefaultEntities(model);

        var command = new ActivateCompanyCommand(model) { UserId = "user-123" };
        await _sut.HandleAsync(command, CancellationToken.None);

        _unitOfWorkEvents.Received(1).Enqueue(Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnitValue()
    {
        var model = CreateModel();
        SetupDefaultEntities(model);

        var command = new ActivateCompanyCommand(model) { UserId = "user-123" };
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.ShouldBe(JobBoard.Application.Interfaces.Configurations.Unit.Value);
    }

    [Fact]
    public async Task HandleAsync_ShouldStartActivity()
    {
        var model = CreateModel();
        SetupDefaultEntities(model);

        var command = new ActivateCompanyCommand(model) { UserId = "user-123" };
        await _sut.HandleAsync(command, CancellationToken.None);

        _activityFactory.Received(1).StartActivity("ActivateCompany", ActivityKind.Internal, Arg.Any<ActivityContext>());
    }

    private void SetupDefaultEntities(CompanyCreatedModel model)
    {
        _companyRepository.GetCompanyById(model.CompanyUId, Arg.Any<CancellationToken>())
            .Returns(CreateTestCompany());
        _userRepository.FindUserByUIdAsync(model.UserUId, Arg.Any<CancellationToken>())
            .Returns(CreateTestUser());
    }
}
