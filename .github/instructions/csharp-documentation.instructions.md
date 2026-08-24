---
description: "Require XML documentation for all non-private C# types and members."
applyTo: "**/*.cs"
---
# C# XML documentation

- Every non-private type and member must have appropriate XML documentation.
- Document public, protected, internal, protected internal, and private protected types and members.
- Use a `<summary>` element to describe the purpose and behavior of each documented type or member.
- Document parameters with `<param>` elements and return values with `<returns>` when applicable.
- Document exceptions with `<exception>` when callers may reasonably need to handle them.
- Keep XML documentation accurate and update it when the API or behavior changes.
- Do not add XML documentation to private implementation details unless it improves clarity.
