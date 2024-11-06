Imports FEAFIPLib.wsfexv1
imports System.Globalization
Imports System.Threading

Public Class BIWSFEXV1

    Private Const dateFormat As String = "yyyyMMdd"
    Private mErrorCode As Integer
    Private mErrorDesc As String
    Private mCUIT As ULong
    Private mAuthRequest As New wsfexv1.ClsFEXAuthRequest
    Private mFECAERequest As wsfexv1.ClsFEXRequest
    Private mFECAEResponse As wsfexv1.FEXResponseAuthorize
    Private Const URLWSAA = "https://wsaa.afip.gov.ar/ws/services/LoginCms"
    Private Const URLWSAA_HOMO = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms"
    Private Const URLWSW = "https://servicios1.afip.gov.ar/wsfexv1/service.asmx"
    Private Const URLWSW_HOMO = "https://wswhomo.afip.gov.ar/wsfexv1/service.asmx"
    Private mClient As wsfexv1.Service
    Private mModoProduccion As Boolean


    Sub New()
        'Dim newCulture As CultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture.Clone
        'newCulture.DateTimeFormat.ShortDatePattern = "yyyyMMdd"
        'newCulture.DateTimeFormat.DateSeparator = ""
        'Thread.CurrentThread.CurrentCulture = newCulture
        System.Net.ServicePointManager.SecurityProtocol = Net.SecurityProtocolType.Tls Or Net.SecurityProtocolType.Tls11 Or Net.SecurityProtocolType.Tls12 Or Net.SecurityProtocolType.Tls
    End Sub

    Private Function getClient() As wsfexv1.Service
        If mClient Is Nothing Then

            '   Dim binding = New BasicHttpBinding()
            '   binding.Security.Mode = BasicHttpSecurityMode.Transport

            mClient = New wsfexv1.Service
            If mModoProduccion Then
                mClient.Url = URLWSW
            Else
                mClient.Url = URLWSW_HOMO
            End If

        End If
        Return mClient
    End Function

    Public Function login(ByVal certificadoPFX As String, ByVal password As String) As Boolean
        Dim loginTicket As LoginTicket = New LoginTicket
        Try
            Dim url
            If mModoProduccion Then
                url = URLWSAA
            Else
                url = URLWSAA_HOMO
            End If
            loginTicket.ObtenerLoginTicketResponse("wsfex", url, certificadoPFX, password)
            mAuthRequest = New wsfexv1.ClsFEXAuthRequest
            mAuthRequest.Token = loginTicket.Token
            mAuthRequest.Sign = loginTicket.Sign
            mAuthRequest.Cuit = loginTicket.CUIT
            Return True
        Catch e As Exception
            mErrorCode = -1
            mErrorDesc = e.Message
            Return False
        End Try
    End Function

    Public ReadOnly Property ErrorCode As Integer
        Get
            Return mErrorCode
        End Get
    End Property

    Public ReadOnly Property ErrorDesc As String
        Get
            Return mErrorDesc
        End Get
    End Property

    Public Property ModoProduccion As Boolean
        Get
            Return mModoProduccion
        End Get
        Set(ByVal value As Boolean)
            mModoProduccion = value
            'mModoProduccion = False
            'Interaction.MsgBox("La siguiente dll esta habilitada en modo Demo. Para obtener la licencia en produccion contacte a contacto@bitingenieria.com.ar")
        End Set
    End Property

    Public Function recuperaLastCMP(ByVal ptoVta As Integer, ByVal tipoComprobante As Integer, ByRef nroComprobante As ULong)
        Dim auth As wsfexv1.ClsFEX_LastCMP = New wsfexv1.ClsFEX_LastCMP
        auth.Cbte_Tipo = tipoComprobante
        auth.Cuit = mAuthRequest.Cuit
        auth.Pto_venta = ptoVta
        auth.Sign = mAuthRequest.Sign
        auth.Token = mAuthRequest.Token
        Dim result As wsfexv1.FEXResponseLast_CMP = getClient.FEXGetLast_CMP(auth)
        If isError(result.FEXErr) Then
            Return False
        Else
            nroComprobante = result.FEXResult_LastCMP.Cbte_nro
            Return True
        End If
    End Function

    Public Function recuperaLastID(ByRef Id As Long)
        Dim result As wsfexv1.FEXResponse_LastID = getClient.FEXGetLast_ID(mAuthRequest)
        If isError(result.FEXErr) Then
            Return False
        Else
            Id = result.FEXResultGet.Id
            Return True
        End If
    End Function

    Public Sub reset()
        mFECAERequest = New wsfexv1.ClsFEXRequest
    End Sub

    Public Sub agregaFactura(ByVal Id As Double, ByVal Fecha_cbte As Date, ByVal Tipo_cbte As Integer, ByVal Punto_vta As Integer, ByVal Cbte_nro As Double, ByVal Tipo_expo As Integer, ByVal Permiso_existente As String, ByVal Dst_cmp As Integer, ByVal Cliente As String, ByVal Cuit_pais_cliente As Double, ByVal Domicilio_cliente As String, ByVal Id_impositivo As String, ByVal Moneda_Id As String, ByVal Moneda_ctz As Double, ByVal Obs_comerciales As String, ByVal Imp_total As Double, ByVal Obs As String, ByVal Forma_pago As String, ByVal Incoterms As String, ByVal Incoterms_Ds As String, ByVal Idioma_cbte As Integer)
        If mFECAERequest Is Nothing Then
            reset()
        End If
        With mFECAERequest
            .Cbte_nro = Cbte_nro
            .Cbte_Tipo = Tipo_cbte
            .Cliente = Cliente
            .Cuit_pais_cliente = Cuit_pais_cliente
            .Domicilio_cliente = Domicilio_cliente
            .Dst_cmp = Dst_cmp
            .Fecha_cbte = Fecha_cbte.Date.ToString(dateFormat)
            .Forma_pago = Forma_pago
            .Id = Id
            .Id_impositivo = Id_impositivo
            .Idioma_cbte = Idioma_cbte
            .Imp_total = Imp_total
            .Incoterms = Incoterms
            .Incoterms_Ds = Incoterms_Ds
            .Moneda_ctz = Moneda_ctz
            .Moneda_Id = Moneda_Id
            .Obs = Obs
            .Obs_comerciales = Obs_comerciales
            .Permiso_existente = Permiso_existente
            .Punto_vta = Punto_vta
            .Tipo_expo = Tipo_expo

        End With
    End Sub

    Public Sub agregaItem(ByVal Pro_codigo As String, ByVal Pro_ds As String, ByVal Pro_qty As Double, ByVal Pro_umed As Integer, ByVal Pro_precio_uni As Double, ByVal Pro_total_item As Double, ByVal Pro_bonificacion As Double)
        If Not mFECAERequest Is Nothing Then
            If mFECAERequest.Items Is Nothing Then
                ReDim Preserve mFECAERequest.Items(0)
            Else
                ReDim Preserve mFECAERequest.Items(mFECAERequest.Items.Length)
            End If
            mFECAERequest.Items(mFECAERequest.Items.GetUpperBound(0)) = New FEAFIPLib.wsfexv1.Item
            With mFECAERequest.Items(mFECAERequest.Items.GetUpperBound(0))
                .Pro_bonificacion = Pro_bonificacion
                .Pro_codigo = Pro_codigo
                .Pro_ds = Pro_ds
                .Pro_precio_uni = Pro_precio_uni
                .Pro_qty = Pro_qty
                .Pro_total_item = Pro_total_item
                .Pro_umed = Pro_umed
            End With
        End If
    End Sub

    Public Sub agregaPermiso(ByVal Id_permiso As String, ByVal Dst_merc As Integer)

        If Not mFECAERequest Is Nothing Then
            If mFECAERequest.Permisos Is Nothing Then
                ReDim Preserve mFECAERequest.Permisos(0)
            Else
                ReDim Preserve mFECAERequest.Permisos(mFECAERequest.Permisos.Length)
            End If
            mFECAERequest.Permisos(mFECAERequest.Permisos.GetUpperBound(0)) = New FEAFIPLib.wsfexv1.Permiso
            With mFECAERequest.Permisos(mFECAERequest.Permisos.GetUpperBound(0))
                .Dst_merc = Dst_merc
                .Id_permiso = Id_permiso
            End With
        End If
    End Sub

    Public Sub agregaCompAsoc(ByVal Cbte_tipo As Integer, ByVal Cbte_punto_vta As Integer, ByVal Cbte_nro As Double, ByVal Cbte_cuit As Double)
        If Not mFECAERequest Is Nothing Then
            If mFECAERequest.Cmps_asoc Is Nothing Then
                ReDim Preserve mFECAERequest.Cmps_asoc(0)
            Else
                ReDim Preserve mFECAERequest.Cmps_asoc(mFECAERequest.Cmps_asoc.Length)
            End If
            mFECAERequest.Cmps_asoc(mFECAERequest.Cmps_asoc.GetUpperBound(0)) = New FEAFIPLib.wsfexv1.Cmp_asoc
            With mFECAERequest.Cmps_asoc(mFECAERequest.Cmps_asoc.GetUpperBound(0))
                .Cbte_cuit = Cbte_cuit
                .Cbte_nro = Cbte_nro
                .Cbte_punto_vta = Cbte_punto_vta
                .Cbte_tipo = Cbte_tipo
            End With
        End If
    End Sub

    Public Function autorizar()
        If Not mFECAERequest Is Nothing Then
            mFECAEResponse = getClient.FEXAuthorize(mAuthRequest, mFECAERequest)
            If isError(mFECAEResponse.FEXErr) Then
                Return False
            End If

        End If
        Return True
    End Function

    Private Function isError(ByVal err As wsfexv1.ClsFEXErr)
        If (err Is Nothing) OrElse (err.ErrCode = 0) Then
            Return False
        Else
            mErrorCode = err.ErrCode
            mErrorDesc = err.ErrMsg
            Return True
        End If
    End Function

    Public Sub autorizarRespuesta(ByRef Cae As String, ByRef Fch_venc_Cae As DateTime, ByRef Resultado As String, ByRef Reproceso As String)
        If Not mFECAEResponse Is Nothing Then
            Cae = mFECAEResponse.FEXResultAuth.Cae
            DateTime.TryParseExact(mFECAEResponse.FEXResultAuth.Fch_venc_Cae, dateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, Fch_venc_Cae)
            Resultado = mFECAEResponse.FEXResultAuth.Resultado
            Reproceso = mFECAEResponse.FEXResultAuth.Reproceso
        End If
    End Sub

    Public Function autorizarRespuestaObs() As String
        Return mFECAEResponse.FEXResultAuth.Motivos_Obs
    End Function
End Class
