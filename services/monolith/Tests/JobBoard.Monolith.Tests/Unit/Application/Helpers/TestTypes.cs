using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;

namespace JobBoard.Monolith.Tests.Unit.Application.Helpers;

public class TestCommand : BaseCommand<string>
{
    public string Name { get; set; } = string.Empty;
}

public class TestNoTransactionCommand : BaseCommand<string>, INoTransaction;

public class TestQuery : BaseQuery<string>;
