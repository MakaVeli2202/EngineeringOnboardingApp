using System.Collections.Generic;
using System.Linq;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.ViewModels;

public class ResourcesViewModel : BaseViewModel
{
    private readonly AppSession _session = AppSession.Shared;

    public List<string> Categories { get; }

    public ResourcesViewModel()
    {
        Categories = _session.Resources
            .Where(r => !string.IsNullOrWhiteSpace(r.Category))
            .Select(r => r.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public IReadOnlyList<ResourceLink> GetAll()
        => _session.Resources;

    public IReadOnlyList<ResourceLink> GetByCategory(string category)
        => _session.Resources.Where(r => r.Category == category).ToList();
}
