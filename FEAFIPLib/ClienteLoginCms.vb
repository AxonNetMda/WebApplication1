Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Xml
Imports System.Net
Imports System.Security
Imports System.Security.Cryptography
Imports System.Security.Cryptography.Pkcs
Imports System.Security.Cryptography.X509Certificates
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Globalization
Imports System.Web
Imports System.Data.SqlClient
Imports capaDatos.Conexion
Imports capaDatos
Public Class LoginTicket

    Public UniqueId As UInt32 ' Entero de 32 bits sin signo que identifica el requerimiento
    Public GenerationTime As DateTime ' Momento en que fue generado el requerimiento
    Public ExpirationTime As DateTime ' Momento en el que exoira la solicitud
    Public Service As String ' Identificacion del WSN para el cual se solicita el TA
    Public Sign As String ' Firma de seguridad recibida en la respuesta
    Public Token As String ' Token de seguridad recibido en la respuesta

    Public XmlLoginTicketRequest As XmlDocument = Nothing
    Public XmlLoginTicketResponse As XmlDocument = Nothing
    Public RutaDelCertificadoFirmante As String
    Public XmlStrLoginTicketRequestTemplate As String = "<loginTicketRequest><header><uniqueId></uniqueId><generationTime></generationTime><expirationTime></expirationTime></header><service></service></loginTicketRequest>"

    Private Shared _globalUniqueID As UInt32 = 0 ' OJO! NO ES THREAD-SAFE

    Private Function AssemblyDirectory() As String
        Dim codeBase As String = Reflection.Assembly.GetExecutingAssembly().CodeBase
        Dim uri As UriBuilder = New UriBuilder(codeBase)
        Dim lpath As String = HttpUtility.UrlDecode(uri.Path)
        Return Path.GetDirectoryName(lpath)
    End Function

    Private Function CacheFilename(ByVal CUIT As Double) As String
        'MsgBox(AssemblyDirectory() & "..\" & CUIT.ToString & ".cache")
        Return AssemblyDirectory() & "\" & CUIT.ToString & ".cache"

    End Function

    Public CUIT As ULong
    Public Function ObtenerLoginTicketResponse( _
    ByVal argServicio As String, _
    ByVal argUrlWsaa As String, _
    ByVal argRutaCertX509Firmante As String, _
    ByVal argPassword As String _
    ) As String

        Me.RutaDelCertificadoFirmante = argRutaCertX509Firmante

        Dim cmsFirmadoBase64 As String
        Dim loginTicketResponse As String
        Dim xmlNodoUniqueId As XmlNode
        Dim xmlNodoGenerationTime As XmlNode
        Dim xmlNodoExpirationTime As XmlNode
        Dim xmlNodoService As XmlNode
        Dim strPasswordSecureString As New SecureString

        For Each character As Char In argPassword.ToCharArray()
            strPasswordSecureString.AppendChar(character)
        Next
        strPasswordSecureString.MakeReadOnly()

        ' PASO 1: Genero el Login Ticket Request
        Try
            _globalUniqueID += 1

            XmlLoginTicketRequest = New XmlDocument()
            XmlLoginTicketRequest.LoadXml(XmlStrLoginTicketRequestTemplate)

            xmlNodoUniqueId = XmlLoginTicketRequest.SelectSingleNode("//uniqueId")
            xmlNodoGenerationTime = XmlLoginTicketRequest.SelectSingleNode("//generationTime")
            xmlNodoExpirationTime = XmlLoginTicketRequest.SelectSingleNode("//expirationTime")
            xmlNodoService = XmlLoginTicketRequest.SelectSingleNode("//service")

            xmlNodoGenerationTime.InnerText = DateTime.Now.AddMinutes(-60).ToString("s")
            xmlNodoExpirationTime.InnerText = DateTime.Now.AddHours(+12).ToString("s")
            xmlNodoUniqueId.InnerText = CStr(_globalUniqueID)
            xmlNodoService.InnerText = argServicio
            Me.Service = argServicio

        Catch excepcionAlGenerarLoginTicketRequest As Exception
            Throw New Exception("***Error GENERANDO el LoginTicketRequest : " + excepcionAlGenerarLoginTicketRequest.Message + excepcionAlGenerarLoginTicketRequest.StackTrace)
        End Try

        Dim certFirmante As X509Certificate2
        ' PASO 2: Firmo el Login Ticket Request
        Try

            certFirmante = CertificadosX509Lib.ObtieneCertificadoDesdeArchivo(RutaDelCertificadoFirmante, strPasswordSecureString)
            Dim subject As String() = certFirmante.Subject.Split(",")
            For Each element As String In subject
                If element.Trim().StartsWith("SERIALNUMBER=CUIT ") Then
                    CUIT = ULong.Parse(element.Replace("SERIALNUMBER=CUIT ", ""))
                End If
            Next
        Catch excepcionAlLeerCertificado As Exception
            Throw New Exception("***Error Leyendo el Certificado : " + excepcionAlLeerCertificado.Message)
        End Try

        Dim cacheKey As String = "N" & (argUrlWsaa & CUIT & argServicio).GetHashCode.ToString
        Dim cache As XmlDocument = New XmlDocument
        '**********************************************************************************



        ''*********************************************************************************
        'If capaDatos.loginWSASS().ObtenerXmlPorCuit(CUIT) Then
        '    cache.Load(CacheFilename(CUIT))
        'End If

        Try
            Dim token As String = ""
            Dim sign As String = ""
            Dim expirationDate As Date

            If Not cache.SelectSingleNode("//root//" & cacheKey) Is Nothing Then
                token = cache.SelectSingleNode("//root//" & cacheKey & "//" & "token").InnerText
                sign = cache.SelectSingleNode("//root//" & cacheKey & "//" & "sign").InnerText
                expirationDate = Date.Parse(cache.SelectSingleNode("//root//" & cacheKey & "//" & "expirationDate").InnerText)
                If (Not token.Equals("")) And (Now < expirationDate) Then
                    Me.ExpirationTime = expirationDate
                    Me.Token = token
                    Me.Sign = sign
                    Return ""
                End If
            End If

            ' Convierto el login ticket request a bytes, para firmar
            Dim EncodedMsg As Encoding = Encoding.UTF8
            Dim msgBytes As Byte() = EncodedMsg.GetBytes(XmlLoginTicketRequest.OuterXml)

            ' Firmo el msg y paso a Base64
            Dim encodedSignedCms As Byte() = CertificadosX509Lib.FirmaBytesMensaje(msgBytes, certFirmante)
            cmsFirmadoBase64 = Convert.ToBase64String(encodedSignedCms)

        Catch excepcionAlFirmar As Exception
            Throw New Exception("***Error FIRMANDO el LoginTicketRequest : " + excepcionAlFirmar.Message)
        End Try

        ' PASO 3: Invoco al WSAA para obtener el Login Ticket Response
        Try
            '        Dim binding = New BasicHttpBinding()
            '       binding.Security.Mode = BasicHttpSecurityMode.Transport

            Dim servicioWsaa As New Wsaa.LoginCMSService
            servicioWsaa.Url = argUrlWsaa
            loginTicketResponse = servicioWsaa.loginCms(cmsFirmadoBase64)

        Catch excepcionAlInvocarWsaa As Exception
            Throw New Exception("***Error INVOCANDO al servicio WSAA : " + excepcionAlInvocarWsaa.Message)
        End Try


        ' PASO 4: Analizo el Login Ticket Response recibido del WSAA
        Try
            XmlLoginTicketResponse = New XmlDocument()
            XmlLoginTicketResponse.LoadXml(loginTicketResponse)

            Me.UniqueId = UInt32.Parse(XmlLoginTicketResponse.SelectSingleNode("//uniqueId").InnerText)
            Me.GenerationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//generationTime").InnerText)
            Me.ExpirationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//expirationTime").InnerText)
            Me.Sign = XmlLoginTicketResponse.SelectSingleNode("//sign").InnerText
            Me.Token = XmlLoginTicketResponse.SelectSingleNode("//token").InnerText

            If cache.SelectSingleNode("//root") Is Nothing Then
                cache.RemoveAll()
                cache.AppendChild(cache.CreateElement("root"))
            End If
            Dim rootNode = cache.SelectSingleNode("//root")
            Dim keyNode = rootNode.SelectSingleNode("//" & cacheKey)
            If keyNode Is Nothing Then
                keyNode = rootNode.AppendChild(cache.CreateElement(cacheKey))
            End If
            If keyNode.SelectSingleNode("token") Is Nothing Then
                keyNode.AppendChild(cache.CreateElement("token"))
            End If
            If keyNode.SelectSingleNode("sign") Is Nothing Then
                keyNode.AppendChild(cache.CreateElement("sign"))
            End If
            If keyNode.SelectSingleNode("expirationDate") Is Nothing Then
                keyNode.AppendChild(cache.CreateElement("expirationDate"))
            End If
            keyNode.SelectSingleNode("token").InnerText = Me.Token
            keyNode.SelectSingleNode("sign").InnerText = Me.Sign
            keyNode.SelectSingleNode("expirationDate").InnerText = Me.ExpirationTime.ToString()
            loginWSASS.GuardarXmlPorCuit(CUIT, cache)
            'cache.Save(CacheFilename(CUIT))
        Catch excepcionAlAnalizarLoginTicketResponse As Exception
            Throw New Exception("***Error ANALIZANDO el LoginTicketResponse : " + excepcionAlAnalizarLoginTicketResponse.Message)
        End Try

        Return loginTicketResponse

    End Function

End Class

Class CertificadosX509Lib

    Public Shared VerboseMode As Boolean = False

    ''' <summary>
    ''' Firma mensaje
    ''' </summary>
    ''' <param name="argBytesMsg">Bytes del mensaje</param>
    ''' <param name="argCertFirmante">Certificado usado para firmar</param>
    ''' <returns>Bytes del mensaje firmado</returns>
    ''' <remarks></remarks>
    Public Shared Function FirmaBytesMensaje(
    ByVal argBytesMsg As Byte(),
    ByVal argCertFirmante As X509Certificate2
    ) As Byte()
        Dim donde As String = ""
        Try
            ' Pongo el mensaje en un objeto ContentInfo (requerido para construir el obj SignedCms)
            Dim infoContenido As New ContentInfo(argBytesMsg)
            Dim cmsFirmado As New SignedCms(infoContenido)
            donde = " Pongo el mensaje en un objeto ContentInfo (requerido para construir el obj SignedCms)"

            ' Creo objeto CmsSigner que tiene las caracteristicas del firmante
            Dim cmsFirmante As New CmsSigner(argCertFirmante)
            cmsFirmante.IncludeOption = X509IncludeOption.EndCertOnly
            donde = "Creo objeto CmsSigner que tiene las caracteristicas del firmante"

            If VerboseMode Then
                Console.WriteLine("***Firmando bytes del mensaje...")
            End If
            ' Firmo el mensaje PKCS #7

            cmsFirmado.ComputeSignature(cmsFirmante)
            donde = "Firmo el mensaje PKCS #7"

            If VerboseMode Then
                Console.WriteLine("***OK mensaje firmado")
            End If
            donde = "Encodeo el mensaje PKCS #7"
            ' Encodeo el mensaje PKCS #7.
            Return cmsFirmado.Encode()
        Catch excepcionAlFirmar As Exception
            Throw New Exception("***Error al firmar: " & excepcionAlFirmar.Message & " LL " & donde)
            Return Nothing
        End Try
    End Function

    Public Shared Function ObtieneCertificadoDesdeArchivo( _
    ByVal argArchivo As String, _
    ByVal argPassword As SecureString _
    ) As X509Certificate2
        Dim objCert As New X509Certificate2
        Try
            If argPassword.IsReadOnly Then
                objCert.Import(My.Computer.FileSystem.ReadAllBytes(argArchivo), argPassword, X509KeyStorageFlags.PersistKeySet)
            Else
                objCert.Import(My.Computer.FileSystem.ReadAllBytes(argArchivo))
            End If
            Return objCert
        Catch excepcionAlImportarCertificado As Exception
            Throw New Exception(excepcionAlImportarCertificado.Message & " " & excepcionAlImportarCertificado.StackTrace)
            Return Nothing
        End Try
    End Function

End Class

Class ProgramaPrincipal


    ' Valores por defecto, globales en esta clase
    Const DEFAULT_URLWSAAWSDL As String = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms?WSDL"
    Const DEFAULT_SERVICIO As String = "wsfe"
    Const DEFAULT_CERTSIGNER As String = "certificadofenamar.p12"


    Public Shared Function AssemblyDirectory2() As String
        Dim codeBase As String = Reflection.Assembly.GetExecutingAssembly().CodeBase
        Dim uri As UriBuilder = New UriBuilder(codeBase)
        Dim lpath As String = HttpUtility.UrlDecode(uri.Path)
        Return Path.GetDirectoryName(lpath)
    End Function

    Public Shared Function Main(ByVal args As String()) As Integer

        Dim strUrlWsaaWsdl As String = DEFAULT_URLWSAAWSDL
        Dim strIdServicioNegocio As String = DEFAULT_SERVICIO
        Dim strRutaCertSigner As String = DEFAULT_CERTSIGNER
        Dim strPassword As String = "feafip"

        strIdServicioNegocio = "wsfe"
        strRutaCertSigner = AssemblyDirectory2() & "\certificadofenamar.p12" '"c:\certificado.pfx"

        ' Argumentos OK, entonces procesar normalmente...

        Dim objTicketRespuesta As LoginTicket
        Dim strTicketRespuesta As String

        Try


            objTicketRespuesta = New LoginTicket

            strTicketRespuesta = objTicketRespuesta.ObtenerLoginTicketResponse(strIdServicioNegocio, strUrlWsaaWsdl, strRutaCertSigner, strPassword)

        Catch excepcionAlObtenerTicket As Exception

            Console.WriteLine("***EXCEPCION AL OBTENER TICKET:")
            Console.WriteLine(excepcionAlObtenerTicket.Message)
            Return -10

        End Try
        Return 0
    End Function



End Class