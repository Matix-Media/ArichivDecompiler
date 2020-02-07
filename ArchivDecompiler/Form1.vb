Imports System.ComponentModel
Imports System.IO, Ionic.Zip

Public Class Form1
    Dim file_ As String
    Dim destination As String

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim parameter() As String = Environment.GetCommandLineArgs().ToArray
        If parameter.Count - 1 >= 1 Then 'Höher als 1 weil der index 0 der Pfad zum programm ist
            For i = 1 To parameter.Count - 1
                Select Case i
                    Case 1
                        file_ = parameter(i)
                    Case 2
                        destination = parameter(i)
                End Select
            Next
        End If
    End Sub

    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        tbFile.Text = file_
        If destination = "" AndAlso Not file_ = "" Then
            destination = Path.GetDirectoryName(file_) & "\" & Path.GetFileNameWithoutExtension(file_)
            tbdestination.Text = destination
        ElseIf Not destination = "" Then
            tbdestination.Text = destination
        ElseIf destination = "" Then
            tbdestination.Text = ""
        End If



    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim bfd As New OpenFileDialog
        bfd.Filter = "ZIP-Archiv|*.zip"
        bfd.Multiselect = False
        If bfd.ShowDialog = DialogResult.OK Then
            If File.Exists(bfd.FileName) Then
                tbFile.Text = bfd.FileName
                file_ = bfd.FileName
                If tbdestination.Text = "" Then
                    tbdestination.Text = Path.GetDirectoryName(bfd.FileName)
                End If
            End If
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim fbd As New FolderBrowserDialog
        If fbd.ShowDialog = DialogResult.OK Then
            Dim ff As String = fbd.SelectedPath
            If File.Exists(ff) Then
                tbdestination.Text = ff
                destination = ff
            End If
        End If
    End Sub

    ''' <summary>
    ''' wartet x Millisekunden (GUI-freundlich)
    ''' </summary>
    Public Sub WaitOnGUI(ByVal ms As Integer)
        ' timeout festlegen
        Dim timeOut As Date = Date.Now().AddMilliseconds(ms)
        ' bis timeout warten
        While timeOut > Date.Now
            Application.DoEvents() ' GUI Ereignisse erlauben
        End While
    End Sub

    Private TotalSize As ULong = 0

    Private ExtractedSize As ULong = 0

    Private LastBtsTransfered As ULong = 0

    Private BufferSize As ULong = 0

    Dim oa As Boolean = False
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        lvStatus.Items(0).SubItems(1).Text = "..."
        lvStatus.Items(1).SubItems(1).Text = "..."
        lvStatus.Items(2).SubItems(1).Text = "..."
        If File.Exists(tbFile.Text) AndAlso Directory.Exists(Path.GetDirectoryName(tbdestination.Text)) Then
            lvStatus.Items(0).SubItems(1).Text = "Abgeschlossen."
            GroupBox1.Enabled = False
            GroupBox2.Enabled = False
            Button3.Enabled = False
            ProgressBar1.Visible = True
            ProgressBar1.Value = 0
            If CheckBox1.Checked = True Then
                oa = True
            End If
            WaitOnGUI(100)
            BackgroundWorker1.RunWorkerAsync()
        Else

            With lvErrors.Items.Add("Der/Die angegebene/n Pfad/e ist/sind Falsch.")
                .ToolTipText = "Der/Die angegebene/n Pfad/e ist/sind Falsch."
            End With
        End If

    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork

        Try
            Dim ZipToUnpack As String = tbFile.Text
            Dim TargetDir As String = tbdestination.Text
            BeginInvoke(Sub() lvStatus.Items(1).SubItems(1).Text = "Extrahiere Archiv """ & ZipToUnpack & """ zu """ & TargetDir & """")
            Using zip1 As ZipFile = ZipFile.Read(ZipToUnpack)
                AddHandler zip1.ExtractProgress, AddressOf MyExtractProgress

                'reset these values each time you unzip a folder
                BufferSize = CULng(zip1.BufferSize)
                TotalSize = 0
                ExtractedSize = 0

                'get the total size in bytes to be extracted
                For Each ent As ZipEntry In zip1
                    TotalSize += CULng(ent.UncompressedSize)
                Next

                'if the total size is greater than Integer.MaxValue then divide it down by 1024 before setting the ProgressBar.Maximum
                If TotalSize >= Integer.MaxValue Then
                    BeginInvoke(Sub() ProgressBar1.Maximum = CInt(TotalSize / 1024))
                Else
                    BeginInvoke(Sub() ProgressBar1.Maximum = CInt(TotalSize))
                End If

                Dim e_ As ZipEntry
                ' here, we extract every entry, but we could extract    
                ' based on entry name, size, date, etc.   
                For Each e_ In zip1
                    BeginInvoke(Sub() lvStatus.Items(1).SubItems(1).Text = "Extrahiere Datei """ & e_.FileName & """...")
                    BeginInvoke(Sub() lvStatus.Items(1).ToolTipText = "Extrahiere Datei """ & e_.FileName & """...")

                    If File.Exists(TargetDir & "\" & e_.FileName.Replace("/", "\")) Then
                        If MsgBox("Wollen Sie folgende Datei Überschreiben? """ & TargetDir & "\" & e_.FileName.Replace("/", "\") & """", MsgBoxStyle.YesNo, "Überschreiben") = MsgBoxResult.Yes Then
                            e_.Extract(TargetDir, ExtractExistingFileAction.OverwriteSilently)
                        Else

                        End If
                    Else
                        e_.Extract(TargetDir, ExtractExistingFileAction.DoNotOverwrite)
                    End If

                Next
            End Using
            BeginInvoke(Sub() lvStatus.Items(1).SubItems(1).Text = "Abgeschlossen.")
            BeginInvoke(Sub() lvStatus.Items(2).SubItems(1).Text = """" & ZipToUnpack & """ Erfogreich Extrahiert.")

            BeginInvoke(Sub() GroupBox1.Enabled = True)
            BeginInvoke(Sub() GroupBox2.Enabled = True)
            BeginInvoke(Sub() Button3.Enabled = True)

            BeginInvoke(Sub() lvErrors.Items.Clear())
        Catch ex As Exception
            Invoke(Sub() lvErrors.Items.Add(ex.Message))

            BeginInvoke(Sub() GroupBox1.Enabled = True)
            BeginInvoke(Sub() GroupBox2.Enabled = True)
            BeginInvoke(Sub() Button3.Enabled = True)

            BeginInvoke(Sub() lvStatus.Items(0).SubItems(1).Text = "Fehler!")
            BeginInvoke(Sub() lvStatus.Items(1).SubItems(1).Text = "Fehler!")
            BeginInvoke(Sub() lvStatus.Items(2).SubItems(1).Text = "Fehler!")

        End Try
    End Sub


    Async Sub MyExtractProgress(ByVal sender As Object, ByVal e As ExtractProgressEventArgs)
        'if the reported BytesTransferred is greater than 0 then add the number of bytes that where transferred to the ExtractedSize variable
        'and set the LastBtsTransfered to the number of bytes that have been transferred so far. Else reset the LastBtsTransfered to 0.
        If e.BytesTransferred > 0 Then
            ExtractedSize += CULng(e.BytesTransferred - LastBtsTransfered)
            LastBtsTransfered = CULng(e.BytesTransferred)
        Else
            LastBtsTransfered = 0
        End If

        'again, if the total size is greater than Integer.MaxValue then divide the ExtractedSize by 1024 before setting the ProgressBar.Value
        If TotalSize >= Integer.MaxValue Then
            BeginInvoke(Sub() ProgressBar1.Value = CInt(ExtractedSize / 1024))

        Else
            BeginInvoke(Sub() ProgressBar1.Value = CInt(ExtractedSize))
        End If
    End Sub

    Private Sub BackgroundWorker1_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        Try
            If oa = True Then
                Dim porc As New ProcessStartInfo
                porc.Arguments = tbdestination.Text
                porc.FileName = Application.StartupPath & "\" & "AddWExplorer.exe"
                Process.Start(porc)
            End If
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
        ProgressBar1.Visible = False
    End Sub
End Class

