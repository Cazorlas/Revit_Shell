using System;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using RevitShell.RevitAddin.Requests;

namespace RevitShell.RevitAddin;

/// <summary>Schedules a shell open request after Revit initializes.</summary>
public sealed class OpenRequestApplication : IExternalApplication
{
    private ExternalEvent? _externalEvent;

    /// <summary>Registers an external event only when a valid shell request exists.</summary>
    /// <param name="application">The starting Revit application.</param>
    /// <returns>A successful startup result.</returns>
    public Result OnStartup(UIControlledApplication application)
    {
        var path = Environment.GetEnvironmentVariable(OpenRequestFile.EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(path) || !OpenRequestFile.TryRead(path!, out var request))
        {
            return Result.Succeeded;
        }

        _externalEvent = ExternalEvent.Create(new OpenRequestHandler(request!));
        application.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
        return Result.Succeeded;
    }

    private void OnApplicationInitialized(object? sender, ApplicationInitializedEventArgs args)
    {
        // Opening a document is forbidden inside this event handler; defer it to Execute.
        _externalEvent?.Raise();
    }

    /// <summary>Releases the external event when Revit shuts down.</summary>
    /// <param name="application">The closing Revit application.</param>
    /// <returns>A successful shutdown result.</returns>
    public Result OnShutdown(UIControlledApplication application)
    {
        application.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
        _externalEvent?.Dispose();
        _externalEvent = null;
        return Result.Succeeded;
    }
}
