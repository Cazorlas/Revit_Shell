using System;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitShell.RevitAddin.Requests;

namespace RevitShell.RevitAddin;

/// <summary>Opens the requested model from a Revit external event.</summary>
public sealed class OpenRequestHandler : IExternalEventHandler
{
    private readonly OpenRequest _request;

    /// <summary>Initializes the handler for one model request.</summary>
    /// <param name="request">The shell request to execute.</param>
    public OpenRequestHandler(OpenRequest request)
    {
        _request = request;
    }

    /// <summary>Gets the external event display name.</summary>
    /// <returns>The shell open request name.</returns>
    public string GetName() => "Revit Shell open request";

    /// <summary>Creates a local copy when requested and opens the chosen model.</summary>
    /// <param name="app">The active Revit application.</param>
    public void Execute(UIApplication app)
    {
        try
        {
            var path = _request.ModelPath;
            var options = new OpenOptions
            {
                DetachFromCentralOption = _request.Mode == OpenRequestMode.Detach
                    ? DetachFromCentralOption.DetachAndPreserveWorksets
                    : DetachFromCentralOption.DoNotDetach
            };

            if (_request.Mode == OpenRequestMode.CreateLocal)
            {
                var local = LocalModelNaming.GetLocalPath(path, app.Application.Username,
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                if (File.Exists(local))
                {
                    File.Move(local, LocalModelNaming.GetBackupPath(local, DateTime.Now));
                }

                WorksharingUtils.CreateNewLocal(ModelPathUtils.ConvertUserVisiblePathToModelPath(path),
                    ModelPathUtils.ConvertUserVisiblePathToModelPath(local));
                path = local;
            }

            app.OpenAndActivateDocument(ModelPathUtils.ConvertUserVisiblePathToModelPath(path), options, false);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Revit Shell", _request.Mode + " failed for " +
                Path.GetFileName(_request.ModelPath) + ":\n" + ex.Message);
        }
    }
}
