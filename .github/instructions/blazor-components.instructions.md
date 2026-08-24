---
description: "Use when creating or modifying Blazor components. Keeps component UI markup and C# logic separated into matching .razor and .razor.cs files."
applyTo: "**/*.razor, **/*.razor.cs"
---
# Blazor component structure

- Every Blazor component must consist of a matching `.razor` file and `.razor.cs` code-behind file.
- Keep UI markup, layout, and rendering declarations in the `.razor` file.
- Keep component logic, parameters, lifecycle methods, and other C# code in the `.razor.cs` file.
- The `.razor.cs` file must declare the matching component as a `partial` class.
