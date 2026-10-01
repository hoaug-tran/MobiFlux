using FluentValidation;
using MobiFlux.Application.Abstractions;

namespace MobiFlux.Application.Features.Proxies.GetProxySessions;

public sealed class GetProxySessionsQueryValidator : AbstractValidator<GetProxySessionsQuery>
{
    public GetProxySessionsQueryValidator(IApiRequestLimits limits)
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, limits.MaximumPageSize);
    }
}
