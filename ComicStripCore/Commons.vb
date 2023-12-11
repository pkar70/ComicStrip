Imports pkar

Module Commons
    Public Async Function GetPicRootDirAsync() As Task(Of Windows.Storage.StorageFolder)
        Try
            Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.KnownFolders.PicturesLibrary
            oFold = Await oFold.CreateFolderAsync("ComicStrips", Windows.Storage.CreationCollisionOption.OpenIfExists)
            Return oFold
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Async Function GetPicFolder(sFolderName As String) As Task(Of Windows.Storage.StorageFolder)
        Dim oFold As Windows.Storage.StorageFolder = Await GetPicRootDirAsync()
        If oFold Is Nothing Then Return Nothing

        If String.IsNullOrEmpty(sFolderName) Then Return oFold

        Try
            oFold = Await oFold.CreateFolderAsync(sFolderName, Windows.Storage.CreationCollisionOption.OpenIfExists)
            Return oFold
        Catch ex As Exception
            Return Nothing
        End Try

    End Function

    Public Async Function GetPicFolder(oChannel As JedenChannel) As Task(Of Windows.Storage.StorageFolder)
        Return Await GetPicFolder(oChannel.sDirName)
    End Function

    Public Async Function GetPicFile(sFolderName As String, sFileName As String, bCreate As Boolean) As Task(Of Windows.Storage.StorageFile)
        Dim oFold As Windows.Storage.StorageFolder = Await GetPicFolder(sFolderName)
        If oFold Is Nothing Then Return Nothing

        Dim oFile As Windows.Storage.StorageFile
        If bCreate Then
            oFile = Await oFold.CreateFileAsync(sFileName, Windows.Storage.CreationCollisionOption.ReplaceExisting)
        Else
            oFile = Await oFold.TryGetItemAsync(sFileName)
        End If

        Return oFile

    End Function

    Public Function CreateChannelToolTip(oItem As JedenChannel) As String

        Dim sTxt As String = oItem.sFullName & vbCrLf & vbCrLf &
                "From URL : " & oItem.sUrl & vbCrLf &
                "To folder: " & oItem.sDirName & vbCrLf & vbCrLf &
                "First: " & oItem.sIdFirstPicture & vbCrLf &
                "Current: " & oItem.sIdLastDownload & vbCrLf & vbCrLf

        If oItem.sIdGapStart > oItem.sIdGapStop Then
            sTxt = sTxt & "(bez gap)"
        Else
            sTxt = sTxt & "Pics gap: " & vbCrLf &   ' ewentualnie licznik tu dać
                "from: " & oItem.sIdGapStart & " +1" & vbCrLf &
                "to: " & oItem.sIdGapStop & " -1"
        End If

        Return sTxt

    End Function

    Public Sub FillChannelToolTips(oChannelsList As ObservableCollection(Of JedenChannel))
        If oChannelsList Is Nothing Then Return

        For Each oItem In oChannelsList
            oItem.sTooltip = CreateChannelToolTip(oItem)
        Next
    End Sub


End Module


' bo to teraz jest w pkarmodule
' a teraz nawet w Nuget
#If False Then
Module Extensions
    <Extension()>
    Public Async Function WriteAllTextAsync(ByVal oFile As Windows.Storage.StorageFile, sTxt As String) As Task
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        Dim oWriter As Windows.Storage.Streams.DataWriter = New Windows.Storage.Streams.DataWriter(oStream.AsOutputStream)
        oWriter.WriteString(sTxt)
        Await oWriter.FlushAsync()
        Await oWriter.StoreAsync()
        oWriter.Dispose()
        'oStream.Flush()
        'oStream.Dispose()
    End Function

    <Extension()>
    Public Async Function WriteAllTextToFileAsync(ByVal oFold As Windows.Storage.StorageFolder, sFileName As String, sTxt As String, Optional oOption As Windows.Storage.CreationCollisionOption = Windows.Storage.CreationCollisionOption.FailIfExists) As Task
        Dim oFile As Windows.Storage.StorageFile = Await oFold.CreateFileAsync(sFileName, oOption)
        If oFile Is Nothing Then Return

        Await oFile.WriteAllTextAsync(sTxt)
    End Function

    <Extension()>
    Public Async Function ReadAllTextAsync(ByVal oFile As Windows.Storage.StorageFile) As Task(Of String)
        ' zamiast File.ReadAllText(oFile.Path)
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Dim oReader As Windows.Storage.Streams.DataReader = New Windows.Storage.Streams.DataReader(oStream.AsInputStream)
        Dim iSize As Integer = oStream.Length
        Await oReader.LoadAsync(iSize)
        Dim sTxt As String = oReader.ReadString(iSize)
        oReader.Dispose()
        oStream.Dispose()
        Return sTxt
    End Function

    <Extension()>
    Public Async Function ReadAllTextFromFileAsync(ByVal oFold As Windows.Storage.StorageFolder, sFileName As String) As Task(Of String)
        Dim oFile As Windows.Storage.StorageFile = Await oFold.TryGetItemAsync(sFileName)
        If oFile Is Nothing Then Return Nothing
        Return Await oFile.ReadAllTextAsync
    End Function

End Module
#End If