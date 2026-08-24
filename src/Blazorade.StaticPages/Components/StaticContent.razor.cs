using Microsoft.AspNetCore.Components;

namespace Blazorade.StaticPages.Components;

/// <summary>
/// Marks the portion of a reusable component that may be included in generated static output.
/// </summary>
public partial class StaticContent
{
	/// <summary>
	/// Gets or sets a value indicating whether the content should be rendered when the app is running in the browser.
	/// The content is rendered during static generation regardless of this value.
	/// </summary>
	[Parameter]
	public bool RenderInBrowser { get; set; } = true;

}
