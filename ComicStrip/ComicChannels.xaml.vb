Imports pkar.Uwp.Ext

Public NotInheritable Class ComicChannels
    Inherits Page

    Dim _kanaly As ObservableCollection(Of JedenChannel)
    Dim _oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder

    Private Sub ProgresywnyRing(sStart As Boolean)
        If sStart Then
            Dim dVal As Double
            dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
            uiProcesuje.Width = dVal
            uiProcesuje.Height = dVal

            uiProcesuje.Visibility = Visibility.Visible
            uiProcesuje.IsActive = True
        Else
            uiProcesuje.IsActive = False
            uiProcesuje.Visibility = Visibility.Collapsed
        End If
    End Sub



    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiVers.ShowAppVers(True)

        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile("", "channels.json", False)
        If oFile Is Nothing Then
            _kanaly = New ObservableCollection(Of JedenChannel)
        Else
            Dim sTxt As String = Await oFile.ReadAllTextAsync()
            _kanaly = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObservableCollection(Of JedenChannel)))
        End If

        FillChannelToolTips(_kanaly)

        uiListItems.ItemsSource = _kanaly
    End Sub

    Private Async Function SaveChannelsData() As Task
        'App._kanaly.Save()

        If _kanaly.Count < 1 Then
            If Not Await vblib.DialogBoxYNAsync("Pusta lista! Zapisać ją?") Then Return
        End If

        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(_kanaly)

        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile("", "channels.json", True)
        oFile.WriteAllTextAsync(sTxt) ' UTF-8, overwrite

    End Function

    Private Async Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        Await SaveChannelsData()
        Me.GoBack
    End Sub

    Private Async Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)
        Dim sUrl As String = Await Me.InputBoxAsync("Podaj link do pasków:")
        If sUrl = "" Then Return

        For Each oItem As JedenChannel In _kanaly
            If oItem.sUrl = sUrl Then
                Me.MsgBox("taki kanał już istnieje!")
                Return
            End If
        Next

        Dim bSupported As Boolean = False
        For Each oItem As Source_Base In App.gaSrc
            If oItem.IsUrlSupported(sUrl) Then
                bSupported = True
                Exit For
            End If
        Next

        If Not bSupported Then
            Me.MsgBox("nie umiem obsłużyć takiego kanału!")
            Return
        End If

        ProgresywnyRing(True)

        Dim oChannel As JedenChannel = Nothing
        For Each oItem As Source_Base In App.gaSrc
            If oItem.IsUrlSupported(sUrl) Then
                oChannel = Await oItem.CreateNewChannel(sUrl)
                Exit For
            End If
        Next


        ProgresywnyRing(False)

        If oChannel Is Nothing Then
            Me.MsgBox("Błąd dodawania kanału!")
            Return
        End If

        oChannel.sTooltip = CreateChannelToolTip(oChannel)

        _kanaly.Add(oChannel)

        Await SaveChannelsData()  ' zapisujemy!

        Dim oPict As JedenPictureData = New JedenPictureData
        oPict.sFileName = oChannel.sIdLastDownload.Replace("/", "-")
        Dim oPicList As List(Of JedenPictureData) = New List(Of JedenPictureData)
        oPicList.Add(oPict)

        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile(oChannel.sDirName, "pictures.json", True)
        If oFile Is Nothing Then Return

        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(oPicList)
        oFile.WriteAllTextAsync(sTxt) ' UTF-8, overwrite


    End Sub

    Private Sub uiDelChannel_Click(sender As Object, e As RoutedEventArgs)
        Me.MsgBox("unimplemented jeszcze")
    End Sub
End Class
