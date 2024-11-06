Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Globalization
Imports System.Threading
Imports FEAFIPLib.ServiceReference1

Public Class WscdcClass
    Private Const dateFormat As String = "yyyyMMdd"
    Private mErrorCode As Integer
    Private mErrorDesc As String
    Private mAuthRequest As CmpAuthRequest
    Private Const URLWSAA As String = "https://wsaa.afip.gov.ar/ws/services/LoginCms"
    Private Const URLWSAA_HOMO As String = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms"
    Private Const URLWSW As String = "https://servicios1.afip.gov.ar/wscdc/service.asmx"
    Private Const URLWSW_HOMO As String = "https://wswhomo.afip.gov.ar/WSCDC/service.asmx"
    Private mModoProduccion As Boolean
    Private mCertificadoPFX As String
    Private mPassword As String

    Public ReadOnly Property ErrorCode() As Integer
        Get
            Return mErrorCode
        End Get
    End Property

    Public ReadOnly Property ErrorDesc() As String
        Get
            Return mErrorDesc
        End Get
    End Property

    Public Property ModoProduccion() As Boolean
        Get
            Return mModoProduccion
        End Get
        Set(ByVal value As Boolean)
            'mModoProduccion = false;
            'Interaction.MsgBox("La siguiente dll esta habilitada en modo Demo. Para obtener la licencia en produccion contacte a contacto@bitingenieria.com.ar");
            mModoProduccion = Value
        End Set
    End Property

    '            CultureInfo newCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
    '            newCulture.DateTimeFormat.ShortDatePattern = "yyyyMMdd";
    '            newCulture.DateTimeFormat.DateSeparator = "";
    '            Thread.CurrentThread.CurrentCulture = newCulture;
    Public Sub New()
    End Sub

    Public Function login(ByVal certificadoPFX As String, ByVal password As String) As Boolean
        Dim servicio As String = "wscdc"
        mCertificadoPFX = certificadoPFX
        mPassword = password
        Dim loginTicket As New LoginTicket()
        Try
            Dim url = If(mModoProduccion, URLWSAA, URLWSAA_HOMO)
            loginTicket.ObtenerLoginTicketResponse(servicio, url, certificadoPFX, password)
            mAuthRequest = New CmpAuthRequest()
            mAuthRequest.Token = loginTicket.Token
            mAuthRequest.Sign = loginTicket.Sign
            mAuthRequest.Cuit = CLng(loginTicket.CUIT)
            Return True
        Catch e As Exception
            mErrorCode = -1
            mErrorDesc = e.Message
            Return False
        End Try
    End Function

    Private Function getClient() As ServiceReference1.Service
        getClient = New ServiceReference1.Service()
        If mModoProduccion Then
            getClient.Url = URLWSW
        Else
            getClient.Url = URLWSW_HOMO
        End If
    End Function

    Public Function ComprobanteConstatar(ByVal CbteModo As String, ByVal CuitEmisor As Long, ByVal PtoVta As Integer, ByVal CbteTipo As Integer, ByVal CbteNro As Long, ByVal CbteFch As String, _
     ByVal ImpTotal As Double, ByVal CodAutorizacion As String, ByVal DocTipoReceptor As String, ByVal DocNroReceptor As String) As Boolean


        mErrorCode = 0
        mErrorDesc = ""

        Dim CmpReq As New CmpDatos()
        CmpReq.CbteModo = CbteModo
        CmpReq.CuitEmisor = CuitEmisor
        CmpReq.PtoVta = PtoVta
        CmpReq.CbteTipo = CbteTipo
        CmpReq.CbteNro = CbteNro
        CmpReq.CbteFch = CbteFch
        CmpReq.ImpTotal = ImpTotal
        CmpReq.CodAutorizacion = CodAutorizacion
        CmpReq.DocTipoReceptor = DocTipoReceptor
        CmpReq.DocNroReceptor = DocNroReceptor
        Dim wscdc As ServiceReference1.Service = getClient()
        Dim Resultado As CmpResponse = wscdc.ComprobanteConstatar(mAuthRequest, CmpReq)
        If Resultado.Errors IsNot Nothing AndAlso (Resultado.Errors.Length > 0) Then
            mErrorCode = Resultado.Errors(0).Code
            mErrorDesc = Resultado.Errors(0).Msg
        Else
            If Resultado.Resultado = "A" Then
                Return True
            ElseIf (Resultado.Observaciones IsNot Nothing) AndAlso (Resultado.Observaciones.Length > 0) Then
                mErrorCode = Resultado.Observaciones(0).Code
                mErrorDesc = Resultado.Observaciones(0).Msg
            Else
                mErrorCode = -1
                mErrorDesc = "Error Desconocido"
            End If
        End If
        Return False

    End Function



End Class
