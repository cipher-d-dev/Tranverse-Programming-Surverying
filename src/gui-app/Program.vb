Imports System
Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Force Segoe UI 9pt as the application-wide default font so every
        ' control — including those inside nested TableLayoutPanels and
        ' FlowLayoutPanels — inherits it instead of the system default
        ' (usually Microsoft Sans Serif 8.25pt on older Windows builds).
        Application.SetDefaultFont(New System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Regular))

        Application.Run(New Form1())
    End Sub
End Module
