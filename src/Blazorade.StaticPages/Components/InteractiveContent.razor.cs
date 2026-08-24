using Blazorade.StaticPages.StaticGeneration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blazorade.StaticPages.Components;

/// <summary>
/// Marks a subtree that must be omitted from generated static output.
/// </summary>
public partial class InteractiveContent
{
	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	private bool ShouldRenderContent =>
		Services.GetService<StaticPageRenderContext>()?.IsStaticGeneration != true;
}
