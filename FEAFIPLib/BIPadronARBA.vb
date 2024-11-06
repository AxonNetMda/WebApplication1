
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Net
Imports System.Xml
Imports System.Security.Cryptography
Imports System.IO

Public Class BIPadronARBA

    Public Class ConsultaAlicuotaRespuesta

        Public GrupoPercepcion As Integer

        Public GrupoRetencion As Integer

        Public AlicuotaPercepcion As Double

        Public AlicuotaRetencion As Double
    End Class

    Private Const URLPADRON_TEST As String = "http://dfe.test.arba.gov.ar/DomicilioElectronico/SeguridadCliente/dfeServicioConsulta.do"

    Private Const URLPADRON_PROD As String = "http://dfe.arba.gov.ar/DomicilioElectronico/SeguridadCliente/dfeServicioConsulta.do"

    Public ModoProduccion As Boolean

    Public user As String

    Public password As String

    Public ErrorDesc As String

    Public ConsultaRespuesta As ConsultaAlicuotaRespuesta

    Public Function GetFileContent(ByVal FechaDesde As String, ByVal FechaHasta As String, ByVal CUIT As Double) As String
        Dim doc As XmlDocument = New XmlDocument()
        Dim root As XmlElement = doc.CreateElement("CONSULTA-ALICUOTA")
        doc.AppendChild(root)
        root.AppendChild(doc.CreateElement("fechaDesde")).InnerText = FechaDesde
        root.AppendChild(doc.CreateElement("fechaHasta")).InnerText = FechaHasta
        root.AppendChild(doc.CreateElement("cantidadContribuyentes")).InnerText = "1"
        Dim contNode As XmlNode = root.AppendChild(doc.CreateElement("contribuyentes"))
        contNode.Attributes.Append(doc.CreateAttribute("class")).Value = "list"
        contNode.AppendChild(doc.CreateElement("contribuyente")).AppendChild(doc.CreateElement("cuitContribuyente")).InnerText = CUIT.ToString()
        Return doc.OuterXml.Trim()
    End Function

    Public Function GetFileName(ByVal FileContent As String) As String
        Dim md5Hasher As MD5 = MD5.Create()
        Dim data As Byte() = md5Hasher.ComputeHash(Encoding.[Default].GetBytes(FileContent))
        Dim sBuilder As StringBuilder = New StringBuilder()
        For i As Integer = 0 To data.Length - 1
            sBuilder.Append(data(i).ToString("x2"))
        Next

        Dim Filename As String = "DFEServicioConsulta_" & sBuilder.ToString() & ".xml"
        Return Filename
    End Function

    Public Function ConsultaAlicuota(ByVal FechaDesde As String, ByVal FechaHasta As String, ByVal CUIT As Double) As Boolean
        Dim parameters As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        parameters("user") = user
        parameters("password") = password
        Dim FileContent As String = GetFileContent(FechaDesde, FechaHasta, CUIT)
        Dim fileparam As FormUpload.FileParameter = New FormUpload.FileParameter(System.Text.Encoding.[Default].GetBytes(FileContent), GetFileName(FileContent))
        parameters("file") = fileparam
        Dim URL As String = If(ModoProduccion, URLPADRON_PROD, URLPADRON_TEST)
        Dim response As HttpWebResponse = FormUpload.MultipartFormDataPost(URL, "", parameters)
        If response.StatusCode = HttpStatusCode.OK Then
            Using stream As Stream = response.GetResponseStream()
                Dim reader As StreamReader = New StreamReader(stream, Encoding.[Default])
                Dim responseString As String = reader.ReadToEnd()
                Try
                    Dim document As XmlDocument = New XmlDocument()
                    document.LoadXml(responseString)
                    If document.SelectSingleNode("//DFEError") IsNot Nothing Then
                        ErrorDesc = document.SelectSingleNode("//DFEError//mensajeError").InnerText.Replace("<![CDATA[", "").Replace("]]/>", "")
                        Return False
                    Else
                        ConsultaRespuesta = New ConsultaAlicuotaRespuesta()
                        Double.TryParse(document.SelectSingleNode("//contribuyentes//contribuyente//alicuotaPercepcion").InnerText, ConsultaRespuesta.AlicuotaPercepcion)
                        Double.TryParse(document.SelectSingleNode("//contribuyentes//contribuyente//alicuotaRetencion").InnerText, ConsultaRespuesta.AlicuotaRetencion)
                        Integer.TryParse(document.SelectSingleNode("//contribuyentes//contribuyente//grupoPercepcion").InnerText, ConsultaRespuesta.GrupoPercepcion)
                        Integer.TryParse(document.SelectSingleNode("//contribuyentes//contribuyente//grupoRetencion").InnerText, ConsultaRespuesta.GrupoRetencion)
                        Return True
                    End If
                Catch e As Exception
                    ErrorDesc = e.Message
                    Return False
                End Try
            End Using
        End If

        Return False
    End Function
End Class