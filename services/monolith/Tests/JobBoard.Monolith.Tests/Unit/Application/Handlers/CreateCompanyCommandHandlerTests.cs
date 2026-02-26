using JobBoard.Application.Actions.Companies.Create;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Messaging;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.IntegrationEvents;
using JobBoard.Monolith.Tests.Unit.Application.Decorators;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class CreateCompanyCommandHandlerTests
{
    private readonly IHandlerContext _handlerContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOutboxPublisher _outboxPublisher;
    private readonly IUnitOfWorkEvents _unitOfWorkEvents;
    private readonly CreateCompanyCommandHandler _sut;

    public CreateCompanyCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork, ITransactionDbContext>();
        _companyRepository = Substitute.For<ICompanyRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _outboxPublisher = Substitute.For<IOutboxPublisher>();
        _unitOfWorkEvents = Substitute.For<IUnitOfWorkEvents>();

        var changeTracker = new StubDbContext().ChangeTracker;
        ((ITransactionDbContext)_unitOfWork).ChangeTracker.Returns(changeTracker);

        _handlerContext = Substitute.For<IHandlerContext>();
        _handlerContext.UnitOfWork.Returns(_unitOfWork);
        _handlerContext.OutboxPublisher.Returns(_outboxPublisher);
        _handlerContext.MetricsService.Returns(Substitute.For<IMetricsService>());
        _handlerContext.UnitOfWorkEvents.Returns(_unitOfWorkEvents);
        _handlerContext.LoggerFactory.Returns(Substitute.For<ILoggerFactory>());

        _sut = new CreateCompanyCommandHandler(_handlerContext, _companyRepository, _userRepository);
    }

    private static CreateCompanyCommand CreateValidCommand() => new()
    {
        Name = "Acme Corp",
        CompanyEmail = "info@acme.com",
        CompanyWebsite = "https://acme.com",
        IndustryUId = Guid.NewGuid(),
        AdminFirstName = "John",
        AdminLastName = "Doe",
        AdminEmail = "john@acme.com",
        UserId = "user-123"
    };

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnCompanyDto()
    {
        var command = CreateValidCommand();
        var companyUId = Guid.NewGuid();
        var userUId = Guid.NewGuid();
        var userCompanyUId = Guid.NewGuid();

        _unitOfWork.GetNextValueFromSequenceAsync(typeof(Company), Arg.Any<CancellationToken>())
            .Returns((1, companyUId));
        _unitOfWork.GetNextValueFromSequenceAsync(typeof(User), Arg.Any<CancellationToken>())
            .Returns((2, userUId));
        _unitOfWork.GetNextValueFromSequenceAsync(typeof(UserCompany), Arg.Any<CancellationToken>())
            .Returns((3, userCompanyUId));
        _companyRepository.GetIndustryIdByUId(command.IndustryUId, Arg.Any<CancellationToken>())
            .Returns(10);

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Acme Corp");
        result.Email.ShouldBe("info@acme.com");
        result.Website.ShouldBe("https://acme.com");
        result.Status.ShouldBe("Provisioning");
        result.Id.ShouldBe(companyUId);
    }

    [Fact]
    public async Task HandleAsync_ShouldGetSequenceValuesForAllEntities()
    {
        var command = CreateValidCommand();
        SetupDefaultSequences();

        await _sut.HandleAsync(command, CancellationToken.None);

        await _unitOfWork.Received(1).GetNextValueFromSequenceAsync(typeof(Company), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GetNextValueFromSequenceAsync(typeof(User), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GetNextValueFromSequenceAsync(typeof(UserCompany), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldResolveIndustryId()
    {
        var command = CreateValidCommand();
        SetupDefaultSequences();

        await _sut.HandleAsync(command, CancellationToken.None);

        await _companyRepository.Received(1).GetIndustryIdByUId(command.IndustryUId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldAddCompanyToRepository()
    {
        var command = CreateValidCommand();
        SetupDefaultSequences();

        await _sut.HandleAsync(command, CancellationToken.None);

        await _companyRepository.Received(1).AddAsync(
            Arg.Is<Company>(c => c.Name == "Acme Corp" && c.Email == "info@acme.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldAddUserToRepository()
    {
        var command = CreateValidCommand();
        SetupDefaultSequences();

        await _sut.HandleAsync(command, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.FirstName == "John" && u.LastName == "Doe" && u.Email == "john@acme.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldAddUserCompanyToRepository()
    {
        var command = CreateValidCommand();
        SetupDefaultSequences();

        await _sut.HandleAsync(command, CancellationToken.None);

        await _userRepository.Received(1).AddCompanyUser(
            Arg.Any<UserCompany>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishIntegrationEvent()
    {
        var command = CreateValidCommand();
        SetupDefaultSequences();

        await _sut.HandleAsync(command, CancellationToken.None);

        await _outboxPublisher.Received(1).PublishAsync(
            Arg.Is<IIntegrationEvent>(e => e.EventType == "company.created.v1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveChanges()
    {
        var command = CreateValidCommand();
        SetupDefaultSequences();

        await _sut.HandleAsync(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync("user-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldEnqueueUnitOfWorkEvent()
    {
        var command = CreateValidCommand();
        SetupDefaultSequences();

        await _sut.HandleAsync(command, CancellationToken.None);

        _unitOfWorkEvents.Received(1).Enqueue(Arg.Any<Func<Task>>());
    }

    private void SetupDefaultSequences()
    {
        _unitOfWork.GetNextValueFromSequenceAsync(typeof(Company), Arg.Any<CancellationToken>())
            .Returns((1, Guid.NewGuid()));
        _unitOfWork.GetNextValueFromSequenceAsync(typeof(User), Arg.Any<CancellationToken>())
            .Returns((2, Guid.NewGuid()));
        _unitOfWork.GetNextValueFromSequenceAsync(typeof(UserCompany), Arg.Any<CancellationToken>())
            .Returns((3, Guid.NewGuid()));
        _companyRepository.GetIndustryIdByUId(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(10);
    }
}

