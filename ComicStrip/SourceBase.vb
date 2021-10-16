Public MustInherit Class Source_Base

    Protected MustOverride ReadOnly Property SRC_URL_PREFIX As String

    Public Function IsUrlSupported(sUrl As String) As Boolean
        Return sUrl.ToLower.StartsWith(SRC_URL_PREFIX)
    End Function

    Public Function IsUrlSupported(oChannel As JedenChannel) As Boolean
        Return IsUrlSupported(oChannel.sUrl)
    End Function

    Public MustOverride Async Function CreateNewChannel(sUrl As String) As Task(Of JedenChannel)
    Public MustOverride Async Function FillNawigacja(oChannel As JedenChannel, sFileName As String, uiFirst As Button, uiPrev As Button, uiCalendar As CalendarDatePicker, uiNext As Button, uiLast As Button) As Task
    Protected MustOverride Async Function DownloadBatchPics(uiProgBar As ProgressBar, oChannel As JedenChannel, bFillHist As Boolean, iMaxBatchSize As Integer, bShowMsg As Boolean) As Task(Of Integer)

    Public Async Function DownloadNextHistoryBatchAsync(uiProgBar As ProgressBar, oChannel As JedenChannel, iBatchSize As Integer) As Task(Of Integer)
        If Not IsUrlSupported(oChannel) Then Return -1

        Return Await DownloadBatchPics(uiProgBar, oChannel, True, iBatchSize, True)
    End Function

    Public Async Function DownloadCurrentAsync(oChannel As JedenChannel, bShowMsg As Boolean) As Task(Of Integer)
        If Not IsUrlSupported(oChannel) Then Return -1

        Return Await DownloadBatchPics(Nothing, oChannel, False, 50, bShowMsg)
    End Function

    Public Async Function DeleteFileAsync(oChannel As JedenChannel, oDate As DateTimeOffset) As Task
        If Not IsUrlSupported(oChannel) Then Return

        Dim sPicName As String = oDate.ToString("yyyy-MM-dd")
        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile(oChannel.sDirName, sPicName, True)
        If oFile Is Nothing Then Return

        Await oFile.DeleteAsync
    End Function

    Public Async Function FindTextAsync(oChannel As JedenChannel) As Task(Of List(Of JedenNewPicture))
        Dim retList As List(Of JedenNewPicture) = New List(Of JedenNewPicture)

        If Not IsUrlSupported(oChannel) Then Return retList ' skoro to nie nasze, dajemy EMPTY listę

    End Function

    Protected Async Function DownloadPictureAsync(oChannel As JedenChannel, sPicName As String, sUrl As String) As Task(Of Boolean)

        Dim oResp As Windows.Web.Http.HttpResponseMessage
        Dim oHttp As Windows.Web.Http.HttpClient = New Windows.Web.Http.HttpClient
        oResp = Await oHttp.GetAsync(New Uri(sUrl))
        If oResp.StatusCode > 290 Then Return ""

        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile(oChannel.sDirName, sPicName, True)
        If oFile Is Nothing Then Return False

        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync()
        Await oResp.Content.WriteToStreamAsync(oStream.AsOutputStream)
        oStream.Flush()
        oStream.Dispose()

        Return True
    End Function

    Public MustOverride Function GetUrlMain(sFileName As String) As String

    Public Function GetUrl(oChannel As JedenChannel, sFileName As String) As String
        If Not IsUrlSupported(oChannel) Then Return ""
        Return oChannel.sUrl & GetUrlMain(sFileName)

    End Function


End Class
