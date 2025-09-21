﻿using System.Security.Claims;
using JobAPI.Contracts.Models.Companies.Requests;

namespace JobApi.Application.Interfaces;

public interface ICompanyCommandService
{
    Task CreateCompanyAsync(CreateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct);
}