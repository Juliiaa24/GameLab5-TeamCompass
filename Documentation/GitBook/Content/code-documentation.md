# Code Documentation

## Doxygen

The API documentation for Computer Learner is generated automatically from the project's C# source code using **Doxygen**.

It documents classes, methods, variables, parameters, namespaces and other code elements.

## Open the API Documentation

[Open Computer Learner API Documentation](https://juliiaa24.github.io/GameLab5-TeamCompass/doxygen/)

If the link is not available yet, open the repository's **Actions** tab and wait for the **Build and deploy Doxygen** workflow to finish successfully.

## How It Is Updated

The Doxygen documentation is regenerated automatically when changes are pushed to `main` that affect the source code or Doxygen configuration.

This keeps the API documentation connected to the source code without manually uploading generated HTML files.

## Documentation Comments

Use standard Doxygen-compatible C# comments for public classes and important members:

```csharp
/// <summary>
/// Handles interaction with a draggable desktop icon.
/// </summary>
public class DraggableIcon : MonoBehaviour
{
    /// <summary>
    /// Moves the icon according to the current drag position.
/// </summary>
    public void MoveIcon()
    {
    }
}
```
