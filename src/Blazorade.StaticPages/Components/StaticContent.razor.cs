using Blazorade.StaticPages.StaticGeneration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blazorade.StaticPages.Components;

/// <summary>
/// Marks the portion of a reusable component that may be included in generated static output.
/// </summary>
public partial class StaticContent
{
	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	private bool IsStaticGeneration =>
		Services.GetService<StaticPageRenderContext>()?.IsStaticGeneration == true;
}
