Imports System
Imports System.Drawing
Imports System.Drawing.Text
Imports System.IO
Imports System.Windows.Forms

'==============================================================================
'  AppFonts
'  Loads the Inter typeface from the fonts/ folder that the build copies next
'  to the exe.  Falls back to "Segoe UI" gracefully if the files are missing.
'==============================================================================
Public Module AppFonts

    ' Kept alive for the process lifetime — disposing it would unload the fonts.
    Private _pfc As PrivateFontCollection

    ''' <summary>The font name to pass to New Font(...)</summary>
    Public ReadOnly Property InterName As String
        Get
            Return If(_pfc IsNot Nothing, "Inter", "Segoe UI Variable")
        End Get
    End Property

    ''' <summary>Mono fallback is unchanged — system font.</summary>
    Public ReadOnly Property MonoName As String
        Get
            Return "Cascadia Mono"
        End Get
    End Property

    ''' <summary>
    ''' Call once before Application.Run.  Loads Inter-Regular, Inter-SemiBold
    ''' and Inter-Bold from the fonts/ subfolder beside the exe.
    ''' </summary>
    Public Sub Load()
        Dim fontsDir As String = Path.Combine(AppContext.BaseDirectory, "fonts")
        Dim files() As String = {
            Path.Combine(fontsDir, "Inter-Regular.ttf"),
            Path.Combine(fontsDir, "Inter-SemiBold.ttf"),
            Path.Combine(fontsDir, "Inter-Bold.ttf")
        }

        ' All three files must exist — if any is missing, skip custom fonts.
        For Each f As String In files
            If Not File.Exists(f) Then
                Console.WriteLine($"[AppFonts] Missing: {f} — falling back to Segoe UI")
                Return
            End If
        Next

        Try
            _pfc = New PrivateFontCollection()
            For Each f As String In files
                _pfc.AddFontFile(f)
            Next
            Console.WriteLine("[AppFonts] Inter loaded successfully.")
        Catch ex As Exception
            Console.WriteLine($"[AppFonts] Failed to load Inter: {ex.Message}")
            _pfc = Nothing
        End Try
    End Sub

End Module


Module Program
    <STAThread>
    Sub Main()
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Load Inter (or fall back to Segoe UI) before any controls are created.
        AppFonts.Load()

        ' Set the process-wide default font so every nested container inherits it.
        Application.SetDefaultFont(New Font(AppFonts.InterName, 9, FontStyle.Regular))

        Application.Run(New Form1())
    End Sub
End Module
