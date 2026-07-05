' ==================================================================================
'  Entry point - needed when building/running with the .NET SDK directly
'  (dotnet build / dotnet run), instead of through the full Visual Studio IDE.
'  Visual Studio normally generates this file for you automatically; since we
'  are building with just the lightweight SDK, we add it ourselves.
' ==================================================================================

Imports System
Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New Form1())
    End Sub
End Module
