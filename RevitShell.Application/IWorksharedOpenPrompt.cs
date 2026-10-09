using RevitShell.Domain;

namespace RevitShell.Application;

/// <summary>Requests an opening choice for a workshared model.</summary>
public interface IWorksharedOpenPrompt
{
    /// <summary>Asks the user how to open the model.</summary>
    /// <param name="info">The inspected model information.</param>
    /// <returns>The user's opening choice.</returns>
    WorksharedOpenChoice Ask(RevitInfo info);
}
