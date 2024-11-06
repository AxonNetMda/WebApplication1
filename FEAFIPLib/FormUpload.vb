
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Net
Imports System.IO

Module FormUpload

    Private ReadOnly encoding As Encoding = encoding.UTF8

    Function MultipartFormDataPost(ByVal postUrl As String, ByVal userAgent As String, ByVal postParameters As Dictionary(Of String, Object)) As HttpWebResponse
        Dim formDataBoundary As String = String.Format("----------{0:N}", Guid.NewGuid())
        Dim contentType As String = "multipart/form-data; boundary=" & formDataBoundary
        Dim formData As Byte() = GetMultipartFormData(postParameters, formDataBoundary)
        Return PostForm(postUrl, userAgent, contentType, formData)
    End Function

    Private Function PostForm(ByVal postUrl As String, ByVal userAgent As String, ByVal contentType As String, ByVal formData As Byte()) As HttpWebResponse
        Dim request As HttpWebRequest = TryCast(WebRequest.Create(postUrl), HttpWebRequest)
        If request Is Nothing Then
            Throw New NullReferenceException("request is not a http request")
        End If

        request.Method = "POST"
        request.ContentType = contentType
        request.UserAgent = userAgent
        request.CookieContainer = New CookieContainer()
        request.ContentLength = formData.Length
        Using requestStream As Stream = request.GetRequestStream()
            requestStream.Write(formData, 0, formData.Length)
            requestStream.Close()
        End Using

        Return TryCast(request.GetResponse(), HttpWebResponse)
    End Function

    Private Function GetMultipartFormData(ByVal postParameters As Dictionary(Of String, Object), ByVal boundary As String) As Byte()
        Dim formDataStream As Stream = New System.IO.MemoryStream()
        Dim needsCLRF As Boolean = False
        For Each param As KeyValuePair(Of String, Object) In postParameters
            If needsCLRF Then formDataStream.Write(encoding.GetBytes(vbCrLf), 0, encoding.GetByteCount(vbCrLf))
            needsCLRF = True
            If TypeOf param.Value Is FileParameter Then
                Dim fileToUpload As FileParameter = CType(param.Value, FileParameter)
                Dim header As String = String.Format("--{0}" & vbCrLf & "Content-Disposition: form-data; name=""{1}""; filename=""{2}"";" & vbCrLf & "Content-Type: {3}" & vbCrLf & vbCrLf, boundary, param.Key, If(fileToUpload.FileName, param.Key), If(fileToUpload.ContentType, "application/octet-stream"))
                formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header))
                formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length)
            Else
                Dim postData As String = String.Format("--{0}" & vbCrLf & "Content-Disposition: form-data; name=""{1}""" & vbCrLf & vbCrLf & "{2}", boundary, param.Key, param.Value)
                formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData))
            End If
        Next

        Dim footer As String = vbCrLf & "--" & boundary & "--" & vbCrLf
        formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer))
        formDataStream.Position = 0
        Dim formData As Byte() = New Byte(formDataStream.Length - 1) {}
        formDataStream.Read(formData, 0, formData.Length)
        formDataStream.Close()
        Return formData
    End Function

    Public Class FileParameter

        Public File As Byte()

        Public FileName As String

        Public ContentType As String

        Public Sub New(ByVal Afile As Byte())
            file = Afile
        End Sub

        Public Sub New(ByVal Afile As Byte(), ByVal Afilename As String)
            file = Afile
            filename = Afilename
        End Sub

        Public Sub New(ByVal Afile As Byte(), ByVal Afilename As String, ByVal Acontenttype As String)
            File = Afile
            FileName = Afilename
            ContentType = Acontenttype
        End Sub
    End Class
End Module