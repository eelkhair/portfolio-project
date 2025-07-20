using System;
using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;

namespace JobApi.Application.Commands.Job;

public record CreateJobCommand(string Title) : ICommand<Guid>;