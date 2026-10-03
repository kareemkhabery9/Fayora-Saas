using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;

namespace Fayora.Application.Features.CompanyModule.Queries.GetMyCompanyProfile;

public record GetMyCompanyProfileQuery : IQuery<Result<CompanyProfileResult>>;
