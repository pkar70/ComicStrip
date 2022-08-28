﻿NotInheritable Class App
    Inherits Application

    'Public Sub App()
    '    ' ustawienie theme w zależności od godziny
    '    Dim bNoc As Boolean = False
    '    If DateTime.Now.Hour > 20 Then bNoc = True
    '    If DateTime.Now.Hour < 9 Then bNoc = True
    '    RequestedTheme = If(bNoc, ApplicationTheme.Dark, ApplicationTheme.Light)
    'End Sub

#Region "autogenerated"

    ' obsluga lokalnych komend
    Private Async Function AppServiceLocalCommand(sCommand As String) As Task(Of String)
        Return ""
    End Function

    ' RemoteSystems, Timer
    Protected Overrides Async Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)
        moTaskDeferal = args.TaskInstance.GetDeferral() ' w pkarmodule.App

        Dim bNoComplete As Boolean = False
        Dim bObsluzone As Boolean = False

        If args.TaskInstance.Task.Name.Contains("Timer") Then
            Await GetAllFeedsAsync(Nothing)
            bObsluzone = True
        End If

        ' lista komend danej aplikacji
        Dim sLocalCmds As String = "add CHANNEL" & vbTab & "dodanie kanału"

        ' zwroci false gdy to nie jest RemoteSystem; gdy true, to zainicjalizowało odbieranie
        If Not bObsluzone Then bNoComplete = RemSysInit(args, sLocalCmds)

        If Not bNoComplete Then moTaskDeferal.Complete()

    End Sub


    ' CommandLine, Toasts
    Protected Overrides Async Sub OnActivated(args As IActivatedEventArgs)
        ' to jest m.in. dla Toast i tak dalej?

        ' próba czy to commandline
        If args.Kind = ActivationKind.CommandLineLaunch Then

            Dim commandLine As CommandLineActivatedEventArgs = TryCast(args, CommandLineActivatedEventArgs)
            Dim operation As CommandLineActivationOperation = commandLine?.Operation
            Dim strArgs As String = operation?.Arguments

            If Not String.IsNullOrEmpty(strArgs) Then
                Await ObsluzCommandLine(strArgs)
                Window.Current.Close()
                Return
            End If
        End If

        ' jesli nie cmdline (a np. toast), albo cmdline bez parametrow, to pokazujemy okno
        Dim rootFrame As Frame = OnLaunchFragment(args.PreviousExecutionState)

        If args.Kind = ActivationKind.ToastNotification Then
            rootFrame.Navigate(GetType(MainPage))
        End If

        Window.Current.Activate()

    End Sub

    Protected Function OnLaunchFragment(aes As ApplicationExecutionState) As Frame
        Dim mRootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If mRootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            mRootFrame = New Frame()

            AddHandler mRootFrame.NavigationFailed, AddressOf OnNavigationFailed

            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler mRootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

            ' Place the frame in the current Window
            Window.Current.Content = mRootFrame
        End If

        Return mRootFrame
    End Function

    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
        Dim RootFrame As Frame = OnLaunchFragment(e.PreviousExecutionState)

        If e.PrelaunchActivated = False Then
            If rootFrame.Content Is Nothing Then
                ' When the navigation stack isn't restored navigate to the first page,
                ' configuring the new page by passing required information as a navigation
                ' parameter
                rootFrame.Navigate(GetType(MainPage), e.Arguments)
            End If

            ' Ensure the current window is active
            Window.Current.Activate()
        End If
    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub
#End Region

    Public Shared Async Function GetAllFeedsAsync(oTB As TextBlock) As Task
        Dim _kanaly As ObservableCollection(Of JedenChannel)
        Dim oFile As Windows.Storage.StorageFile = Await GetPicFile("", "channels.json", False)
        If oFile Is Nothing Then
            MakeToast("Error: empty channel list at timer!")
        Else
            Dim sToastNews As String = ""
            Dim sToastErrors As String = ""
            Dim iNewsCount As Integer = 0
            Dim bChannelsDirty As Boolean = False
            Dim bFirst As Boolean = True    ' dla rezygnacji przy błędzie ściągania (niby DNS error)
            'Dim bNoDNS As Boolean = False

            Dim sTxt As String = Await oFile.ReadAllTextAsync()
            _kanaly = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObservableCollection(Of JedenChannel)))

            For Each oChannel As JedenChannel In _kanaly
                If Not oChannel.bEnabled Then Continue For

                For Each oSrc As Source_Base In App.gaSrc
                    If oSrc.IsUrlSupported(oChannel.sUrl) Then
                        Dim iRet As Integer = Await oSrc.DownloadCurrentAsync(oChannel, False)
                        If iRet = -2 AndAlso bFirst Then
                            ' na ściąganiu poprzednio istniejącego obrazka - zapewne więc nie ma jeszcze DNSu
                            MakeToast("Chyba nie ma DNS, rezygnuję z tego Timera")
                            'bNoDNS = True
                            'Exit For
                            Return
                        End If
                        bFirst = False
                        If iRet < 0 Then
                            sToastErrors = sToastErrors & oChannel.sFullName & vbCrLf
                        ElseIf iRet > 0 Then
                            sToastNews = sToastNews & oChannel.sFullName & " (" & iRet & ")" & vbCrLf
                            iNewsCount += iRet
                            bChannelsDirty = True
                        End If

                        Exit For
                    End If
                Next

                'If bNoDNS Then Exit For
            Next

            ' przygotowanie informacji do pokazania
            Dim sToast As String = ""
            If sToastErrors <> "" Then sToast = "ERRORs:" & vbCrLf & sToastErrors & vbCrLf
            If sToastNews <> "" Then
                If sToast = "" Then
                    sToast = sToastNews
                Else
                    sToast = sToast & "News:" & sToastNews
                End If
            End If

            If sToast <> "" Then
                If oTB Is Nothing Then
                    MakeToast(sToast)
                Else
                    DialogBox(sToast)   ' podczas gdy sobie odklikuję, to on zapisuje
                End If
            End If

            If oTB IsNot Nothing Then
                oTB.Text = iNewsCount & " new pics"
            End If

            SetSettingsString("lastRun", DateTime.Now.ToString("yyyy.MM.dd HH:mm"))

            If Not bChannelsDirty Then Return

            sTxt = Newtonsoft.Json.JsonConvert.SerializeObject(_kanaly)
            Await oFile.WriteAllTextAsync(sTxt)

        End If

    End Function


    'Protected Overrides Async Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)
    '    Dim oTimerDeferal As Background.BackgroundTaskDeferral
    '    oTimerDeferal = args.TaskInstance.GetDeferral()

    '    If args.TaskInstance.Task.Name = "ComicStripTimer" Then
    '        Await GetAllFeedsAsync(Nothing)
    '    End If

    '    oTimerDeferal.Complete()
    'End Sub


    'Protected Overrides Sub OnActivated(e As IActivatedEventArgs)
    '    Dim rootFrame As Frame

    '    rootFrame = TryCast(Window.Current.Content, Frame)

    '    ' Do not repeat app initialization when the Window already has content,
    '    ' just ensure that the window is active

    '    If rootFrame Is Nothing Then
    '        ' Create a Frame to act as the navigation context and navigate to the first page
    '        rootFrame = New Frame()

    '        AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

    '        ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
    '        AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
    '        AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

    '        ' Place the frame in the current Window
    '        Window.Current.Content = rootFrame
    '    End If

    '    rootFrame.Navigate(GetType(MainPage))

    '    Window.Current.Activate()
    'End Sub


    Public Shared gaSrc As Source_Base() = {
        New Source_ComicStrip
    }


End Class
