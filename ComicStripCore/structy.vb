﻿Imports Newtonsoft.Json
Imports pkar
Imports vblib

Public Class JedenChannel
    Inherits BaseStruct

    Public Property bEnabled As Boolean = True
    Public Property sUrl As String
    Public Property sFullName As String
    Public Property sDirName As String
    Public Property sIdFirstPicture As String
    Public Property sIdGapStart As String
    Public Property sIdGapStop As String
    Public Property sIdLastSeen As String
    Public Property sIdLastDownload As String

    <JsonIgnore>
    Public Property oIcon As BitmapImage = Nothing
    <JsonIgnore>
    Public Property sTooltip As String

End Class

Public Class JedenPictureData
    Inherits BaseStruct

    Public Property sFileName As String
    Public Property sDymki As String
    Public Property sDymkiPl As String
End Class


Public Class JedenNewPicture
    Inherits BaseStruct

    Public Property oChannel As JedenChannel
    Public Property oPicture As JedenPictureData
End Class
