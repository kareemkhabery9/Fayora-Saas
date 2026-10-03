namespace Fayora.Domain.Common.Interfaces;

/// <summary>Marks data that is isolated to one FayoraTravel company.</summary>
public interface ICompanyOwned
{
    Guid CompanyId { get; }
}
