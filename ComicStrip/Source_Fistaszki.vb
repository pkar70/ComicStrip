'Public Class Source_Fistaszki
'    Inherits Source_Base

'    Protected Overrides ReadOnly Property SRC_URL_PREFIX As String = "https://www.peanuts.com/comics/"

'    Public Overrides Function CreateNewChannel(sUrl As String) As Task(Of JedenChannel)
'        'dociągnij potrzebne dane, wypełniając JedenChannel

'        Dim oNew As JedenChannel = New JedenChannel
'        oNew.bEnabled = True

'        ' first:  August 29th, 1987


'        ' przetworzenie URL do postaci skróconej (bez about, dat, etc.)
'        '       https://www.gocomics.com/citizendog/about
'        ' dzis: https://www.gocomics.com/citizendog/2020/05/22
'        ' min:  https://www.gocomics.com/citizendog
'        '       0123456789012345678901234567890123456789
'        '       ..........11111111112222222222333333333

'        Dim iInd As Integer = sUrl.IndexOf("/", 10)
'        If iInd < 5 Then Return Nothing

'        iInd = sUrl.IndexOf("/", iInd + 2)

'        If iInd > 0 Then sUrl = sUrl.Substring(0, iInd)

'        oNew.sUrl = sUrl

'        ' teraz nazwa katalogu - domyslnie nazwa kanalu
'        iInd = sUrl.IndexOf("/", 10)
'        oNew.sDirName = sUrl.Substring(iInd + 1)

'        Dim sPage As String = Await HttpPageAsync(oNew.sUrl & "/about", "getting about", False)
'        Dim sTmp As String

'        If sPage = "" Then
'            DialogBox("Sorry, cannot get channel about data")
'            Return Nothing
'        End If

'        ' full name
'        iInd = sPage.IndexOf("<h1")
'        If iInd > 0 Then
'            iInd = sPage.IndexOf(">About ", iInd)
'            sTmp = sPage.Substring(iInd + ">About ".Length)
'            iInd = sTmp.IndexOf("<")
'            oNew.sFullName = sTmp.Substring(0, iInd)
'        Else
'            oNew.sFullName = oNew.sDirName  ' default
'        End If


'        ' indeksy
'        iInd = sPage.IndexOf("data-link=""comic")
'        If iInd < 10 Then
'            DialogBox("Sorry, don't see link to current strip")
'            Return Nothing
'        End If

'        iInd = sPage.IndexOf("href", iInd) + "href=""/".Length
'        sTmp = sPage.Substring(iInd)
'        iInd = sTmp.IndexOf("""")
'        sUrl = "https://www.gocomics.com/" & sTmp.Substring(0, iInd)

'        ' dociągamy dane dotyczące statystyki (indeksy)
'        sPage = Await HttpPageAsync(sUrl, "getting stats", False)
'        If sPage = "" Then
'            DialogBox("Sorry, cannot get channel stats data")
'            Return Nothing
'        End If

'        iInd = sPage.IndexOf("data-start=") + "data-start='".Length
'        oNew.sIdFirstPicture = sPage.Substring(iInd, 10)    ' mogloby byc bardziej elastyczne, ale po co :)
'        oNew.sIdGapStart = oNew.sIdFirstPicture

'        iInd = sPage.IndexOf("data-end=") + "data-end='".Length
'        oNew.sIdLastDownload = sPage.Substring(iInd, 10)    ' mogloby byc bardziej elastyczne, ale po co :)
'        oNew.sIdGapStop = oNew.sIdLastDownload

'        iInd = sPage.IndexOf("item-comic-image")
'        iInd = sPage.IndexOf("src=", iInd)
'        sTmp = sPage.Substring(iInd + "src='".Length)
'        iInd = sTmp.IndexOf("""")
'        sTmp = sTmp.Substring(0, iInd)

'        Await DownloadPictureAsync(oNew, oNew.sIdLastDownload.Replace("/", "-"), sTmp)

'        ' oraz pierwszy - tez konieczny do uproszczenia nawigacji
'        sUrl = oNew.sUrl & "/" & oNew.sIdFirstPicture
'        sPage = Await HttpPageAsync(sUrl, "getting stats", False)
'        If sPage = "" Then
'            Await DialogBox("cannot get first picture?")
'        Else
'            iInd = sPage.IndexOf("item-comic-image")
'            iInd = sPage.IndexOf("src=", iInd)
'            sTmp = sPage.Substring(iInd + "src='".Length)
'            iInd = sTmp.IndexOf("""")
'            sTmp = sTmp.Substring(0, iInd)

'            Await DownloadPictureAsync(oNew, oNew.sIdFirstPicture.Replace("/", "-"), sTmp)
'        End If

'        Return oNew
'    End Function

'    Public Overrides Function FillNawigacja(oChannel As JedenChannel, sFileName As String, uiFirst As Button, uiPrev As Button, uiCalendar As CalendarDatePicker, uiNext As Button, uiLast As Button) As Task
'        Throw New NotImplementedException()
'    End Function

'    Public Overrides Function GetUrlMain(sFileName As String) As String
'        Throw New NotImplementedException()
'    End Function

'    Protected Overrides Function DownloadBatchPics(uiProgBar As ProgressBar, oChannel As JedenChannel, bFillHist As Boolean, iMaxBatchSize As Integer, bShowMsg As Boolean) As Task(Of Integer)
'        Throw New NotImplementedException()
'    End Function
'End Class
