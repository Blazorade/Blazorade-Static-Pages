using Blazorade.Core.Components;
using Microsoft.AspNetCore.Components;

namespace Blazorade.StaticPages.Components;

public partial class StaticPage : BlazoradeComponentBase
{
    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public DateTime? Date { get; set; }

    [Parameter]
    public bool IncludeInSitemap { get; set; } = true;
}