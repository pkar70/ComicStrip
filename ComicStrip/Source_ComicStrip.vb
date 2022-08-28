Public Class Source_ComicStrip
    Inherits Source_Base

    Protected Overrides ReadOnly Property SRC_URL_PREFIX As String = "https://www.gocomics.com/"

    Public Overrides Async Function CreateNewChannel(sUrl As String) As Task(Of JedenChannel)
        'dociągnij potrzebne dane, wypełniając JedenChannel

        Dim oNew As JedenChannel = New JedenChannel
        oNew.bEnabled = True


        ' przetworzenie URL do postaci skróconej (bez about, dat, etc.)
        '       https://www.gocomics.com/citizendog/about
        ' dzis: https://www.gocomics.com/citizendog/2020/05/22
        ' min:  https://www.gocomics.com/citizendog
        '       0123456789012345678901234567890123456789
        '       ..........11111111112222222222333333333

        Dim iInd As Integer = sUrl.IndexOf("/", 10)
        If iInd < 5 Then Return Nothing

        iInd = sUrl.IndexOf("/", iInd + 2)

        If iInd > 0 Then sUrl = sUrl.Substring(0, iInd)

        oNew.sUrl = sUrl

        ' teraz nazwa katalogu - domyslnie nazwa kanalu
        iInd = sUrl.IndexOf("/", 10)
        oNew.sDirName = sUrl.Substring(iInd + 1)

        Dim sPage As String = Await HttpPageAsync(oNew.sUrl & "/about", "getting about", False)
        Dim sTmp As String

        If sPage = "" Then
            DialogBox("Sorry, cannot get channel about data")
            Return Nothing
        End If

        ' full name
        iInd = sPage.IndexOf("<h1")
        If iInd > 0 Then
            iInd = sPage.IndexOf(">About ", iInd)
            sTmp = sPage.Substring(iInd + ">About ".Length)
            iInd = sTmp.IndexOf("<")
            oNew.sFullName = sTmp.Substring(0, iInd)
        Else
            oNew.sFullName = oNew.sDirName  ' default
        End If


        ' indeksy
        iInd = sPage.IndexOf("data-link=""comic")
        If iInd < 10 Then
            DialogBox("Sorry, don't see link to current strip")
            Return Nothing
        End If

        iInd = sPage.IndexOf("href", iInd) + "href=""/".Length
        sTmp = sPage.Substring(iInd)
        iInd = sTmp.IndexOf("""")
        sUrl = "https://www.gocomics.com/" & sTmp.Substring(0, iInd)

        ' dociągamy dane dotyczące statystyki (indeksy)
        sPage = Await HttpPageAsync(sUrl, "getting stats", False)
        If sPage = "" Then
            DialogBox("Sorry, cannot get channel stats data")
            Return Nothing
        End If

        iInd = sPage.IndexOf("data-start=") + "data-start='".Length
        oNew.sIdFirstPicture = sPage.Substring(iInd, 10)    ' mogloby byc bardziej elastyczne, ale po co :)
        oNew.sIdGapStart = oNew.sIdFirstPicture

        iInd = sPage.IndexOf("data-end=") + "data-end='".Length
        oNew.sIdLastDownload = sPage.Substring(iInd, 10)    ' mogloby byc bardziej elastyczne, ale po co :)
        oNew.sIdGapStop = oNew.sIdLastDownload

        iInd = sPage.IndexOf("item-comic-image")
        iInd = sPage.IndexOf("src=", iInd)
        sTmp = sPage.Substring(iInd + "src='".Length)
        iInd = sTmp.IndexOf("""")
        sTmp = sTmp.Substring(0, iInd)

        Await DownloadPictureAsync(oNew, oNew.sIdLastDownload.Replace("/", "-"), sTmp)

        ' oraz pierwszy - tez konieczny do uproszczenia nawigacji
        sUrl = oNew.sUrl & "/" & oNew.sIdFirstPicture
        sPage = Await HttpPageAsync(sUrl, "getting stats", False)
        If sPage = "" Then
            Await DialogBoxAsync("cannot get first picture?")
        Else
            iInd = sPage.IndexOf("item-comic-image")
            iInd = sPage.IndexOf("src=", iInd)
            sTmp = sPage.Substring(iInd + "src='".Length)
            iInd = sTmp.IndexOf("""")
            sTmp = sTmp.Substring(0, iInd)

            Await DownloadPictureAsync(oNew, oNew.sIdFirstPicture.Replace("/", "-"), sTmp)
        End If

        Return oNew
    End Function


    Private Function ConvertId2Date(sString As String) As DateTimeOffset
        ' zakładam, że na wejściu jest yyyy/mm/dd (z URL) albo yyyy-mm-dd (z nazwy pliku)
        Dim iYear, iMonth, iDay As Integer
        iYear = sString.Substring(0, 4)
        iMonth = sString.Substring(5, 2)
        iDay = sString.Substring(8, 2)

        Dim oDateCurr As DateTime = New DateTime(iYear, iMonth, iDay)

        Return oDateCurr
    End Function

    Public Overrides Async Function FillNawigacja(oChannel As JedenChannel, sFileName As String, uiFirst As Button, uiPrev As Button, uiCalendar As CalendarDatePicker, uiNext As Button, uiLast As Button) As Task

        ' uiCalendar
        uiCalendar.Date = ConvertId2Date(sFileName)
        uiCalendar.MinDate = ConvertId2Date(oChannel.sIdFirstPicture)
        uiCalendar.MaxDate = ConvertId2Date(oChannel.sIdLastDownload)
        uiCalendar.IsEnabled = True

        uiFirst.IsEnabled = Not uiCalendar.Date = uiCalendar.MinDate
        uiLast.IsEnabled = Not uiCalendar.Date = uiCalendar.MaxDate
        uiPrev.IsEnabled = Not uiCalendar.Date = uiCalendar.MinDate
        uiNext.IsEnabled = Not uiCalendar.Date = uiCalendar.MaxDate

        ' first
        Dim dymek As ToolTip = New ToolTip
        dymek.Content = oChannel.sIdFirstPicture
        ToolTipService.SetToolTip(uiFirst, dymek)

        ' last
        dymek = New ToolTip
        dymek.Content = oChannel.sIdLastDownload
        ToolTipService.SetToolTip(uiLast, dymek)

        ' prev, next - wedle sFileName, szukamy po prostu kolejnych
        Dim oFold As Windows.Storage.StorageFolder = Await GetPicFolder(oChannel)
        If oFold Is Nothing Then Return

        Dim oGapDateStart As DateTimeOffset = ConvertId2Date(oChannel.sIdGapStart)
        Dim oGapDateStop As DateTimeOffset = ConvertId2Date(oChannel.sIdGapStop)

        Dim oTempDate As DateTimeOffset = uiCalendar.Date
        While oTempDate > uiCalendar.MinDate
            oTempDate = oTempDate.AddDays(-1)
            If oTempDate > oGapDateStart AndAlso oTempDate < oGapDateStop Then
                oTempDate = ConvertId2Date(oChannel.sIdGapStart)
            End If

            If Await oFold.TryGetItemAsync(oTempDate.ToString("yyyy-MM-dd")) IsNot Nothing Then
                dymek = New ToolTip
                dymek.Content = oTempDate.ToString("yyyy-MM-dd")
                ToolTipService.SetToolTip(uiPrev, dymek)
                Exit While
            End If

        End While

        oTempDate = uiCalendar.Date
        While oTempDate < uiCalendar.MaxDate
            oTempDate = oTempDate.AddDays(1)
            If oTempDate > oGapDateStart AndAlso oTempDate < oGapDateStop Then
                oTempDate = ConvertId2Date(oChannel.sIdGapStop)
            End If
            If Await oFold.TryGetItemAsync(oTempDate.ToString("yyyy-MM-dd")) IsNot Nothing Then
                dymek = New ToolTip
                dymek.Content = oTempDate.ToString("yyyy-MM-dd")
                ToolTipService.SetToolTip(uiNext, dymek)
                Exit While
            End If
        End While


    End Function

    ''' <summary>
    ''' zwraca liczbę obrazków, albo -1 error, -2 error na początku (zapewne brak DNS jeszcze)
    ''' </summary>
    Protected Overrides Async Function DownloadBatchPics(uiProgBar As ProgressBar, oChannel As JedenChannel, bFillHist As Boolean, iMaxBatchSize As Integer, bShowMsg As Boolean) As Task(Of Integer)
        Dim iNewCnt As Integer = 0
        Dim sPage As String
        Dim iInd As Integer
        Dim sTmp, sNextUrl As String

        ' nie ma sensu próbować, jeśli dzisiaj się już ściagało
        ' (a trzeba częściej niż co 24 godziny, bo jak będzie komputer wyłączony, to może nie sprawdzać przez serię dni)

        If Not bFillHist AndAlso ConvertId2Date(oChannel.sIdLastDownload).AddHours(23) > DateTimeOffset.Now Then Return 0

        If uiProgBar IsNot Nothing Then
            uiProgBar.Minimum = 0
            uiProgBar.Maximum = iMaxBatchSize
            uiProgBar.Value = 0
            uiProgBar.Visibility = Visibility.Visible
        End If

        sNextUrl = oChannel.sUrl & "/"
        If bFillHist Then

            If oChannel.sIdGapStart > oChannel.sIdGapStop Then Return 0

            If oChannel.sIdGapStart = "" Then
                sNextUrl = sNextUrl & oChannel.sIdFirstPicture.Replace("-", "/")
            Else
                sNextUrl = sNextUrl & oChannel.sIdGapStart.Replace("-", "/")
            End If
        Else
            sNextUrl = sNextUrl & oChannel.sIdLastDownload.Replace("-", "/")
        End If

        sPage = Await HttpPageAsync(sNextUrl, "downloading newer files (before loop) - " & sNextUrl, bShowMsg)
        If sPage = "" Then
            ' MakeToast("Requested page:", sNextUrl) ' bo już za dużo tych toastów błędów.
            Return -2    ' przeciez powinno byc, bo juz raz sie to sciagnęło!
        End If

        While iMaxBatchSize > iNewCnt
            ' URL dla nastepnego
            iInd = sPage.IndexOf("gc-calendar-nav__next")
            If iInd < 10 Then Return -1   ' błąd - niespodziewana składnia strony

            iInd = sPage.IndexOf("href=", iInd) + "href=".Length
            sNextUrl = sPage.Substring(iInd)
            iInd = sNextUrl.IndexOf(sPage.Substring(iInd, 1), 1)   ' w ten sposób obojętnie czy będzie ' czy " ...
            sNextUrl = sNextUrl.Substring(0, iInd)
            ' niby jest do wyboru:
            ' <a role="button" href="/citizendog/2020/05/24" class="fa btn btn-outline-secondary btn-circle fa-caret-right sm " title=""></a>
            ' albo
            ' <a role="button" href='' class="fa btn btn-outline-secondary btn-circle fa-caret-right sm disabled" title=""></a>

            If sNextUrl.Length < 5 Then Exit While

            ' pomijam /citizendog/
            iInd = sNextUrl.IndexOf("/", 2)
            sNextUrl = sNextUrl.Substring(iInd + 1)


            ' strona nastepnego obrazka
            sPage = Await HttpPageAsync(oChannel.sUrl & "/" & sNextUrl, "downloading newer files (in loop)", bShowMsg)
            If sPage = "" Then
                MakeToast("Requested page:", sNextUrl)
                Return -1    ' przeciez powinno byc, skoro odczytalismy ten link!
            End If

            ' link do obrazka
            iInd = sPage.IndexOf("item-comic-image")
            iInd = sPage.IndexOf("src=", iInd)
            sTmp = sPage.Substring(iInd + "src='".Length)
            iInd = sTmp.IndexOf("""")
            sTmp = sTmp.Substring(0, iInd)

            If Not Await DownloadPictureAsync(oChannel, sNextUrl.Replace("/", "-"), sTmp) Then Exit While

            iNewCnt = iNewCnt + 1
            If uiProgBar IsNot Nothing Then uiProgBar.Value = iNewCnt

            If bFillHist Then
                oChannel.sIdGapStart = sNextUrl
            Else
                oChannel.sIdLastDownload = sNextUrl
            End If

            ' i z przed chwilą ściągniętą stroną - próbuj następne
        End While

        If uiProgBar IsNot Nothing Then uiProgBar.Visibility = Visibility.Collapsed

        Return iNewCnt
    End Function

    Public Overrides Function GetUrlMain(sFileName As String) As String
        ' zwraca samą nazwe pliku wewnatrz channel.sUrl
        Return "/" & sFileName.Replace("-", "/")
    End Function
End Class
