
Imports Windows.ApplicationModel.Background

Public NotInheritable Class MainPage
    Inherits Page

    Private _kanaly As ObservableCollection(Of JedenChannel)
    Private _oChannel As JedenChannel
    Private _sPicShownPath As String

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

    Private Async Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        ProgresywnyRing(True)
        Await App.GetAllFeedsAsync(uiMsg)
        ProgresywnyRing(False)

    End Sub

    Public Shared Sub UnregisterTriggers()
        For Each oTask As KeyValuePair(Of Guid, IBackgroundTaskRegistration) In BackgroundTaskRegistration.AllTasks
            If oTask.Value.Name = "ComicStripTimer" Then oTask.Value.Unregister(True)
        Next
    End Sub
    Public Shared Async Function RegisterTriggers() As Task

        ' na pewno musza byc usuniete
        UnregisterTriggers()

        Dim oBAS As BackgroundAccessStatus
        oBAS = Await BackgroundExecutionManager.RequestAccessAsync()


        Dim builder As BackgroundTaskBuilder = New BackgroundTaskBuilder

        If oBAS = BackgroundAccessStatus.AlwaysAllowed Or oBAS = BackgroundAccessStatus.AllowedSubjectToSystemPolicy Then
            builder.SetTrigger(New TimeTrigger(GetSettingsInt("TimerInterval", 60 * 2), False))
            builder.Name = "ComicStripTimer"
            builder.Register()
        End If

    End Function
    Private Async Sub uiClockRead_Click(sender As Object, e As RoutedEventArgs)
        If uiClockRead.IsChecked Then
            Await RegisterTriggers()
        Else
            UnregisterTriggers()
        End If
        SetSettingsBool("autoRead", uiClockRead.IsChecked)
    End Sub


    Private Async Sub uiOpenExpl_Click(sender As Object, e As RoutedEventArgs)
        Dim oFold As Windows.Storage.StorageFolder = Await GetPicRootDirAsync()
        Windows.System.Launcher.LaunchFolderAsync(oFold)
    End Sub

    Private Sub uiSetup_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(ComicChannels))
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiClockRead.IsChecked = GetSettingsBool("autoRead")
        uiLastRun.Text = GetSettingsString("lastRun")

        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile("", "channels.json", False)
        If oFile Is Nothing Then
            _kanaly = New ObservableCollection(Of JedenChannel)
            Await DialogBox("Empty channel list")
        Else
            Dim sTxt As String = Await oFile.ReadAllTextAsync()
            _kanaly = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObservableCollection(Of JedenChannel)))
        End If

        FillChannelToolTips(_kanaly)

        uiChannelsList.ItemsSource = From c In _kanaly ' Where c.bEnabled = True

    End Sub

    Private Async Sub uiChannel_Click(sender As Object, e As TappedRoutedEventArgs)
        Dim oMFI As Grid = sender
        Dim oItem As JedenChannel = oMFI.DataContext

        uiChannelName.Text = oItem.sFullName
        _oChannel = oItem

        ProgresywnyRing(True)

        If oItem.bEnabled Then
            Await ChangePicture(oItem, oItem.sIdLastDownload)
        Else
            Await ChangePicture(oItem, oItem.sIdFirstPicture)
        End If

        ProgresywnyRing(False)

    End Sub

    Public Async Function SaveChannelsAsync() As Task(Of Boolean)
        If _kanaly.Count < 1 Then Return False

        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile("", "channels.json", True)
        If oFile Is Nothing Then
            Await DialogBox("Nie mogę dostać oFile do zapisania kanałów")
            Return False
        End If
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(_kanaly)
        Await oFile.WriteAllTextAsync(sTxt)

        Return True
    End Function


    Private Async Sub uiDisableThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JedenChannel = oMFI.DataContext
        If Not Await DialogBoxYN("Zablokować kanał '" & oItem.sFullName & "' ?") Then Return

        oItem.bEnabled = False
        SaveChannelsAsync()   ' tak, bez await, niech sobie to robi w tle
        uiChannelsList.ItemsSource = From c In _kanaly Where c.bEnabled = True
    End Sub

    Private Sub uiShowDetailsThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JedenChannel = oMFI.DataContext
        DialogBox(oItem.sTooltip)
    End Sub

    Private Async Sub uiGetBatch_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JedenChannel = oMFI.DataContext

        If oItem.sIdGapStart > oItem.sIdGapStop Then
            DialogBox("Ale tu już nie ma gap'a...")
            Return
        End If

        Dim sGapStart As String = oItem.sIdGapStart
        ProgresywnyRing(True)

        For Each oChann In App.gaSrc
            If oChann.IsUrlSupported(oItem.sUrl) Then
                Dim iRet As Integer = Await oChann.DownloadNextHistoryBatchAsync(uiProgBar, oItem, 30)
                If iRet < 0 Then
                    DialogBox("nie udalo się ściągnąć paczki z historii...")
                Else
                    DialogBox("Ściągnąłem " & iRet & " obrazków, pooglądaj sobie") ' nie czekaj
                    oItem.sTooltip = CreateChannelToolTip(oItem)
                    Await SaveChannelsAsync() ' czekaj - żeby nie było równoległego sięgania do dysku
                    ChangePicture(oItem, sGapStart) ' nie czekaj - kontynuacja w tle
                End If
                Exit For
            End If
        Next

        ProgresywnyRing(False)

    End Sub


    Private Sub uiPic_Tapped(sender As Object, e As RoutedEventArgs)
        Dim oResize As Stretch = uiFullPicture.Stretch
        Select Case oResize
            Case Stretch.Uniform
                uiFullPicture.Stretch = Stretch.None
            Case Stretch.None
                uiFullPicture.Stretch = Stretch.Uniform
        End Select
    End Sub

    Private Sub uiPicDelFromMenu_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Async Function ChangePicture(oChannel As JedenChannel, sIdFile As String) As Task
        ProgresywnyRing(True)

        If oChannel Is Nothing Then oChannel = _oChannel

        For Each oChann In App.gaSrc
            If oChann.IsUrlSupported(oChannel.sUrl) Then
                ' mamy oChann, teraz on ma się zająć wypełnianiem danych etc.
                Await oChann.FillNawigacja(oChannel, sIdFile, uiGoFirst, uiGoPrev, uiGoDate, uiGoNext, uiGoLast)
                Exit For
            End If
        Next

        Dim sFileName As String = sIdFile.Replace("/", "-")
        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile(oChannel.sDirName, sFileName, False)
        If oFile Is Nothing Then
            uiDelPic.IsEnabled = False
        Else
            Dim oImageSrc = New BitmapImage
            Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
            If oStream IsNot Nothing Then
                Await oImageSrc.SetSourceAsync(oStream.AsRandomAccessStream)
                oStream.Dispose()
                uiFullPicture.Source = oImageSrc
            End If
            uiDelPic.IsEnabled = True
            _sPicShownPath = oFile.Path
        End If

        ProgresywnyRing(False)

    End Function

    Private Sub uiGoPic_Click(sender As Object, e As RoutedEventArgs)
        Dim oDymek As ToolTip = ToolTipService.GetToolTip(sender)
        If oDymek IsNot Nothing Then ChangePicture(Nothing, oDymek.Content)
    End Sub

    Private Sub uiGoDate_DateChanged(sender As CalendarDatePicker, args As CalendarDatePickerDateChangedEventArgs) Handles uiGoDate.DateChanged
        ChangePicture(Nothing, sender.Date.Value.ToString("yyyy/MM/dd"))
    End Sub

    Private Async Sub uiDelPic_Click(sender As Object, e As RoutedEventArgs)

        If _oChannel Is Nothing Then Return

        For Each oChann In App.gaSrc
            If oChann.IsUrlSupported(_oChannel.sUrl) Then
                ' mamy oChann, teraz on ma się zająć wypełnianiem danych etc.
                Await oChann.DeleteFileAsync(_oChannel, uiGoDate.Date.Value)
                Exit For
            End If
        Next

        If uiGoNext.IsEnabled Then
            uiGoPic_Click(uiGoNext, Nothing)
        Else
            uiGoPic_Click(uiGoLast, Nothing)
        End If

    End Sub

    Private Sub uiGetPath_Click(sender As Object, e As RoutedEventArgs)
        'Dim oMFI As MenuFlyoutItem = sender
        'Dim oItem As JedenChannel = oMFI.DataContext
        ClipPut(_sPicShownPath)
        DialogBox("Path obrazka już w clipboard")
    End Sub
    Private Sub uiGetUrl_Click(sender As Object, e As RoutedEventArgs)

        Dim iInd As Integer = _sPicShownPath.LastIndexOf("\")
        Dim sFileName As String = _sPicShownPath.Substring(iInd + 1)

        For Each oChann In App.gaSrc
            If oChann.IsUrlSupported(_oChannel.sUrl) Then
                ClipPut(oChann.GetUrl(_oChannel, sFileName))
                DialogBox("URL obrazka już w clipboard")
                Exit For
            End If
        Next

    End Sub
End Class
