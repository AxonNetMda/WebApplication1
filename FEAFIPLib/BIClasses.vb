Imports FEAFIPLib.wsfev1
imports System.Globalization
Imports System.Threading
Imports System.Net
Imports System.Xml
Imports System.Xml.Serialization


Public Class BIWSFEV1

    Private PROVINCIAS() = {"Ciudad Autónoma de Buenos Aires", "Buenos Aires", "Catamara", "Córdoba", "Corrientes", "Entre Ríos", "Jujuy", "Mendoza", "La Rioja", "Salta", "San Juan", "San Luis", "Santa Fe", "Santiago del Estero", "Tucumán", "Chaco", "Chubut", "Formosa", "Misiones", "Neuquén", "La Pampa", "Río Negro", "Santa Cruz", "Tierra del Fuego"}

    Private Const dateFormat As String = "yyyyMMdd"
    Private mErrorCode As Integer
    Private mErrorDesc As String
    Private mCUIT As ULong
    Private mAuthRequest As New wsfev1.FEAuthRequest
    Private mFECAERequest As wsfev1.FECAERequest
    Private mFECAEResponse As wsfev1.FECAEResponse
    Private Const URLWSAA = "https://wsaa.afip.gov.ar/ws/services/LoginCms"
    Private Const URLWSAA_HOMO = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms"
    Private Const URLWSW = "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
    Private Const URLWSW_HOMO = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx"
    Private Const URLWSServiceA5 = "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5"
    Private Const URLWSServiceA5_HOMO = "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5"
    Private mClient As wsfev1.Service
    Private mModoProduccion As Boolean
    Private mCAEAInformarResponse As FECAEAResponse
    Private mCertificado As String
    Private mPAssword As String

    Sub New()
        'Dim newCulture As CultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture.Clone
        'newCulture.DateTimeFormat.ShortDatePattern = "yyyyMMdd"
        'newCulture.DateTimeFormat.DateSeparator = ""
        'Thread.CurrentThread.CurrentCulture = newCulture
        System.Net.ServicePointManager.SecurityProtocol = Net.SecurityProtocolType.Tls Or Net.SecurityProtocolType.Tls11 Or Net.SecurityProtocolType.Tls12 Or Net.SecurityProtocolType.Tls
    End Sub

    Private Function getClient() As wsfev1.Service
        If mClient Is Nothing Then

            '           Dim binding = New BasicHttpBinding()
            '           binding.Security.Mode = BasicHttpSecurityMode.Transport

            mClient = New wsfev1.Service
            If mModoProduccion Then
                mClient.Url = URLWSW
            Else
                mClient.Url = URLWSW_HOMO
            End If
        End If
        Return mClient
    End Function

    Public Function login(ByVal certificadoPFX As String, ByVal password As String, Optional ByVal servicio As String = "wsfe") As Boolean
        mCertificado = certificadoPFX
        mPAssword = password
        Dim loginTicket As LoginTicket = New LoginTicket
        Try
            Dim url
            If mModoProduccion Then
                url = URLWSAA
            Else
                url = URLWSAA_HOMO
            End If
            loginTicket.ObtenerLoginTicketResponse(servicio, url, certificadoPFX, password)
            mAuthRequest = New wsfev1.FEAuthRequest
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

    Private Function TrialExpired()
        Return False
        'Dim Today As Date
        'Today = Date.Now()
        'Dim ExpDate As New Date(2019, 3, 31)
        'Dim Expired As Boolean = Today.Date > ExpDate.Date
        'If Expired Then
        '    mModoProduccion = False
        'End If
        'Return Expired
    End Function

    Public Property ModoProduccion() As Boolean
        Get
            Return mModoProduccion
        End Get
        Set(ByVal value As Boolean)
            mModoProduccion = value
            If TrialExpired() Then
                Interaction.MsgBox("La siguiente dll esta habilitada en modo Demo. Para obtener la licencia en produccion contacte a contacto@bitingenieria.com.ar")
            End If
        End Set
    End Property

    Public Function recuperaLastCMP(ByVal ptoVta As Integer, ByVal tipoComprobante As Integer, ByRef nroComprobante As ULong)
        Dim result As wsfev1.FERecuperaLastCbteResponse = getClient.FECompUltimoAutorizado(mAuthRequest, ptoVta, tipoComprobante)
        If isError(result.Errors) Then
            Return False
        Else
            nroComprobante = result.CbteNro
            Return True
        End If
    End Function

    Public Sub reset()
        mFECAERequest = New wsfev1.FECAERequest
        mFECAERequest.FeCabReq = New wsfev1.FECAECabRequest
        Dim detail As New List(Of wsfev1.FECAEDetRequest)
        mFECAERequest.FeDetReq = detail.ToArray()
    End Sub

    Public Sub agregaFactura(ByVal concepto As Integer, ByVal docTipo As Integer, ByVal docNro As ULong, ByVal cbteDesde As ULong, ByVal cbteHasta As ULong, ByVal cbteFch As Date, ByVal impTotal As Double, ByVal impTotalConc As Double, ByVal impNeto As Double, ByVal impOpEx As Double, ByVal FchServDesde As Nullable(Of Date), ByVal FchServHasta As Nullable(Of Date), ByVal FchVtoPago As Nullable(Of Date), ByVal monId As String, ByVal monCotiz As Double)
        If mFECAERequest Is Nothing Then
            reset()
        End If
        ReDim Preserve mFECAERequest.FeDetReq(mFECAERequest.FeDetReq.Length)
        mFECAERequest.FeDetReq(mFECAERequest.FeDetReq.Length - 1) = New wsfev1.FECAEDetRequest
        With mFECAERequest.FeDetReq(mFECAERequest.FeDetReq.Length - 1)
            .CbteDesde = cbteDesde
            .CbteFch = cbteFch.Date.ToString(dateFormat)
            .CbteHasta = cbteHasta
            .Concepto = concepto
            .DocNro = docNro
            .DocTipo = docTipo
            If (FchServDesde.HasValue) Then
                .FchServDesde = FchServDesde.Value.Date.ToString(dateFormat)
            End If
            If (FchServHasta.HasValue) Then
                .FchServHasta = FchServHasta.Value.Date.ToString(dateFormat)
            End If
            If (FchVtoPago.HasValue) Then
                .FchVtoPago = FchVtoPago.Value.Date.ToString(dateFormat)
            End If
            .ImpNeto = impNeto
            .ImpOpEx = impOpEx
            .ImpTotal = impTotal
            .ImpTotConc = impTotalConc
            .MonCotiz = monCotiz
            .MonId = monId
        End With
        mFECAERequest.FeCabReq.CantReg = mFECAERequest.FeDetReq.Length
    End Sub

    Public Sub agregaIVA(ByVal id As Integer, ByVal baseImp As Double, ByVal importe As Double)
        If mFECAERequest.FeDetReq.Length > 0 Then
            Dim lDetRequest As FECAEDetRequest = mFECAERequest.FeDetReq(mFECAERequest.FeDetReq.GetUpperBound(0))
            If lDetRequest.Iva Is Nothing Then
                ReDim Preserve lDetRequest.Iva(0)
            Else
                ReDim Preserve lDetRequest.Iva(lDetRequest.Iva.Length)
            End If
            lDetRequest.Iva(lDetRequest.Iva.GetUpperBound(0)) = New wsfev1.AlicIva
            With lDetRequest.Iva(lDetRequest.Iva.GetUpperBound(0))
                .BaseImp = baseImp
                .Id = id
                .Importe = importe
            End With
            Dim decImp As Decimal = lDetRequest.ImpIVA + importe
            lDetRequest.ImpIVA = decImp
        End If
    End Sub

    Public Sub agregaTributo(ByVal id As Integer, ByVal desc As String, ByVal baseImp As Double, ByVal alic As Double, ByVal importe As Double)
        If mFECAERequest.FeDetReq.Length > 0 Then
            Dim lDetRequest As FECAEDetRequest = mFECAERequest.FeDetReq(mFECAERequest.FeDetReq.GetUpperBound(0))
            If lDetRequest.Tributos Is Nothing Then
                ReDim Preserve lDetRequest.Tributos(0)
            Else
                ReDim Preserve lDetRequest.Tributos(lDetRequest.Tributos.Length)
            End If
            lDetRequest.Tributos(lDetRequest.Tributos.GetUpperBound(0)) = New wsfev1.Tributo
            With lDetRequest.Tributos(lDetRequest.Tributos.GetUpperBound(0))
                .Alic = alic
                .BaseImp = baseImp
                .Desc = desc
                .Id = id
                .Importe = importe
            End With
            Dim decImp As Decimal = lDetRequest.ImpTrib + importe
            lDetRequest.ImpTrib = decImp
        End If
    End Sub

    Public Sub agregaOpcional(ByVal id As Integer, ByVal valor As String)
        If mFECAERequest.FeDetReq.Length > 0 Then
            Dim lDetRequest As FECAEDetRequest = mFECAERequest.FeDetReq(mFECAERequest.FeDetReq.GetUpperBound(0))
            If lDetRequest.Opcionales Is Nothing Then
                ReDim Preserve lDetRequest.Opcionales(0)
            Else
                ReDim Preserve lDetRequest.Opcionales(lDetRequest.Opcionales.Length)
            End If
            lDetRequest.Opcionales(lDetRequest.Opcionales.GetUpperBound(0)) = New wsfev1.Opcional
            With lDetRequest.Opcionales(lDetRequest.Opcionales.GetUpperBound(0))
                .Id = id
                .Valor = valor
            End With
        End If

    End Sub

    Public Sub agregaCbteAsoc(ByVal tipo As Integer, ByVal ptoVta As Integer, ByVal nro As ULong, ByVal cuit As ULong, ByVal cbteFch As Nullable(Of Date))
        If mFECAERequest.FeDetReq.Length > 0 Then
            Dim lDetRequest As FECAEDetRequest = mFECAERequest.FeDetReq(mFECAERequest.FeDetReq.GetUpperBound(0))
            If lDetRequest.CbtesAsoc Is Nothing Then
                ReDim Preserve lDetRequest.CbtesAsoc(0)
            Else
                ReDim Preserve lDetRequest.CbtesAsoc(lDetRequest.CbtesAsoc.Length)
            End If
            lDetRequest.CbtesAsoc(lDetRequest.CbtesAsoc.GetUpperBound(0)) = New FEAFIPLib.wsfev1.CbteAsoc
            With lDetRequest.CbtesAsoc(lDetRequest.CbtesAsoc.GetUpperBound(0))
                .Nro = nro
                .PtoVta = ptoVta
                .Tipo = tipo
                If cuit > 0 Then
                    .Cuit = cuit
                End If
                If cbteFch.HasValue Then
                    .CbteFch = cbteFch.Value.ToString(dateFormat)
                End If
            End With
        End If
    End Sub

    Public Function autorizar(ByVal ptoVta As Integer, ByVal tipoComp As Integer)

        mCAEAInformarResponse = Nothing
        If Not mFECAERequest Is Nothing Then
            mFECAERequest.FeCabReq.PtoVta = ptoVta
            mFECAERequest.FeCabReq.CbteTipo = tipoComp
            mFECAEResponse = getClient.FECAESolicitar(mAuthRequest, mFECAERequest)
            If isError(mFECAEResponse.Errors) Then
                Return False
            End If

        End If
        Return True
    End Function

    Private Function isError(ByVal err As wsfev1.Err())
        If (err Is Nothing) OrElse (err.Length = 0) Then
            Return False
        Else
            mErrorCode = err(0).Code
            mErrorDesc = err(0).Msg
            Return True
        End If
    End Function

    Public Sub autorizarRespuesta(ByVal index As Integer, ByRef cae As String, ByRef vencimiento As DateTime, ByRef resultado As String)
        If Not mFECAEResponse Is Nothing AndAlso mFECAEResponse.FeDetResp.Length > index Then
            cae = mFECAEResponse.FeDetResp(index).CAE
            DateTime.TryParseExact(mFECAEResponse.FeDetResp(index).CAEFchVto, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, vencimiento)
            resultado = mFECAEResponse.FeCabResp.Resultado
        ElseIf Not mCAEAInformarResponse Is Nothing AndAlso mCAEAInformarResponse.FeDetResp.Length > index Then
            cae = mCAEAInformarResponse.FeDetResp(index).CAEA
            DateTime.TryParseExact(mCAEAInformarResponse.FeCabResp.FchProceso, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, vencimiento)
            resultado = mCAEAInformarResponse.FeCabResp.Resultado
        End If
    End Sub
    Public Function autorizarRespuestaObs(ByVal index As Integer) As String
        If Not mFECAEResponse Is Nothing AndAlso mFECAEResponse.FeDetResp.Length > index AndAlso Not mFECAEResponse.FeDetResp(index).Observaciones Is Nothing Then
            If mFECAEResponse.FeDetResp(index).Observaciones.Length > 0 Then
                Return mFECAEResponse.FeDetResp(index).Observaciones(0).Msg
            End If
        ElseIf Not mCAEAInformarResponse Is Nothing AndAlso mCAEAInformarResponse.FeDetResp.Length > index AndAlso Not mCAEAInformarResponse.FeDetResp(index).Observaciones Is Nothing Then
            If mCAEAInformarResponse.FeDetResp(index).Observaciones.Length > 0 Then
                Return mCAEAInformarResponse.FeDetResp(index).Observaciones(0).Msg
            End If
        End If
        Return ""
    End Function
    Public Function CAEASolicitar(ByVal Periodo As Integer, ByVal Orden As Integer, ByRef CAE As String, ByRef FchVigDesd As Date, ByRef FchVigHasta As Date, ByRef FchTopeInf As Date, ByRef FchProceso As Date) As Boolean
        Dim response As FECAEAGetResponse = getClient.FECAEASolicitar(mAuthRequest, Periodo, Orden)

        Dim Result As Boolean = Not isError(response.Errors)
        If Result Then
            CAE = response.ResultGet.CAEA
            FchVigDesd = Date.ParseExact(response.ResultGet.FchVigDesde, dateFormat, Nothing)
            FchVigHasta = Date.ParseExact(response.ResultGet.FchVigHasta, dateFormat, Nothing)
            FchTopeInf = Date.ParseExact(response.ResultGet.FchTopeInf, dateFormat, Nothing)
            FchProceso = Date.ParseExact(response.ResultGet.FchProceso, dateFormat, Nothing)
        End If

        Return Result
    End Function
    Public Function CAEAConsultar(ByVal Periodo As Integer, ByVal Orden As Integer, ByRef CAE As String, ByRef FchVigDesd As Date, ByRef FchVigHasta As Date, ByRef FchTopeInf As Date, ByRef FchProceso As Date) As Boolean
        Dim response As FECAEAGetResponse = getClient.FECAEAConsultar(mAuthRequest, Periodo, Orden)

        Dim Result As Boolean = Not isError(response.Errors)
        If Result Then
            CAE = response.ResultGet.CAEA
            FchVigDesd = Date.ParseExact(response.ResultGet.FchVigDesde, dateFormat, Nothing)
            FchVigHasta = Date.ParseExact(response.ResultGet.FchVigHasta, dateFormat, Nothing)
            FchTopeInf = Date.ParseExact(response.ResultGet.FchTopeInf, dateFormat, Nothing)
            FchProceso = Date.ParseExact(response.ResultGet.FchProceso.Substring(0, 8), dateFormat, Nothing)
        End If

        Return Result
    End Function

    Public Function CAEAInformar(ByVal ptoVenta As Integer, ByVal CbteTipo As Integer, ByVal CAE As String) As Boolean

        mFECAEResponse = Nothing

        Dim Req As FECAEARequest = New FECAEARequest
        Req.FeCabReq = New FECAEACabRequest
        Req.FeDetReq = (New List(Of FECAEADetRequest)).ToArray()

        Req.FeCabReq.CantReg = mFECAERequest.FeDetReq.Length
        Req.FeCabReq.PtoVta = ptoVenta
        Req.FeCabReq.CbteTipo = CbteTipo

        For I As Integer = 0 To mFECAERequest.FeDetReq.Length - 1

            ReDim Preserve Req.FeDetReq(Req.FeDetReq.Length)
            Dim lDetalle As FECAEADetRequest = New FECAEADetRequest
            Req.FeDetReq(Req.FeDetReq.Length - 1) = lDetalle

            lDetalle.CAEA = CAE
            lDetalle.Concepto = mFECAERequest.FeDetReq(I).Concepto
            lDetalle.DocTipo = mFECAERequest.FeDetReq(I).DocTipo
            lDetalle.DocNro = mFECAERequest.FeDetReq(I).DocNro
            lDetalle.CbteDesde = mFECAERequest.FeDetReq(I).CbteDesde
            lDetalle.CbteHasta = mFECAERequest.FeDetReq(I).CbteHasta
            lDetalle.CbteFch = mFECAERequest.FeDetReq(I).CbteFch
            lDetalle.ImpTotal = mFECAERequest.FeDetReq(I).ImpTotal
            lDetalle.ImpTotConc = mFECAERequest.FeDetReq(I).ImpTotConc
            lDetalle.ImpNeto = mFECAERequest.FeDetReq(I).ImpNeto
            lDetalle.ImpOpEx = mFECAERequest.FeDetReq(I).ImpOpEx
            lDetalle.ImpTrib = mFECAERequest.FeDetReq(I).ImpTrib
            lDetalle.ImpIVA = mFECAERequest.FeDetReq(I).ImpIVA
            lDetalle.FchServDesde = mFECAERequest.FeDetReq(I).FchServDesde
            lDetalle.FchServHasta = mFECAERequest.FeDetReq(I).FchServHasta
            lDetalle.FchVtoPago = mFECAERequest.FeDetReq(I).FchVtoPago
            lDetalle.MonId = mFECAERequest.FeDetReq(I).MonId
            lDetalle.MonCotiz = mFECAERequest.FeDetReq(I).MonCotiz

            If Not IsNothing(mFECAERequest.FeDetReq(I).CbtesAsoc) Then

                For J As Integer = 0 To mFECAERequest.FeDetReq(I).CbtesAsoc.Length - 1

                    Dim lCompAsoc As CbteAsoc = New CbteAsoc
                    ReDim Preserve lDetalle.CbtesAsoc(lDetalle.CbtesAsoc.Length)
                    lDetalle.CbtesAsoc(lDetalle.CbtesAsoc.Length - 1) = lCompAsoc


                    lCompAsoc.Tipo = mFECAERequest.FeDetReq(I).CbtesAsoc(J).Tipo
                    lCompAsoc.PtoVta = mFECAERequest.FeDetReq(I).CbtesAsoc(J).PtoVta
                    lCompAsoc.Nro = mFECAERequest.FeDetReq(I).CbtesAsoc(J).Nro
                Next
            End If

            If Not IsNothing(mFECAERequest.FeDetReq(I).Tributos Is Nothing) Then

                lDetalle.Tributos = (New List(Of Tributo)).ToArray()

                For J As Integer = 0 To mFECAERequest.FeDetReq(I).Tributos.Length - 1

                    Dim lTributo As Tributo = New Tributo
                    ReDim Preserve lDetalle.Tributos(lDetalle.Tributos.Length)
                    lDetalle.Tributos(lDetalle.Tributos.Length - 1) = lTributo

                    lTributo.Id = mFECAERequest.FeDetReq(I).Tributos(J).Id
                    lTributo.Desc = mFECAERequest.FeDetReq(I).Tributos(J).Desc
                    lTributo.BaseImp = mFECAERequest.FeDetReq(I).Tributos(J).BaseImp
                    lTributo.Alic = mFECAERequest.FeDetReq(I).Tributos(J).Alic
                    lTributo.Importe = mFECAERequest.FeDetReq(I).Tributos(J).Importe
                Next
            End If

            If Not IsNothing(mFECAERequest.FeDetReq(I).Iva) Then

                lDetalle.Iva = (New List(Of AlicIva)).ToArray()

                For J As Integer = 0 To mFECAERequest.FeDetReq(I).Iva.Length - 1

                    Dim lIva As AlicIva = New AlicIva
                    ReDim Preserve lDetalle.Iva(lDetalle.Iva.Length)
                    lDetalle.Iva(lDetalle.Iva.Length - 1) = lIva
                    lIva.Id = mFECAERequest.FeDetReq(I).Iva(J).Id
                    lIva.BaseImp = mFECAERequest.FeDetReq(I).Iva(J).BaseImp
                    lIva.Importe = mFECAERequest.FeDetReq(I).Iva(J).Importe
                Next
            End If

            If Not IsNothing(mFECAERequest.FeDetReq(I).Opcionales) Then

                lDetalle.Opcionales = (New List(Of Opcional)).ToArray()

                For J As Integer = 0 To mFECAERequest.FeDetReq(I).Opcionales.Length - 1

                    Dim lOpcional As Opcional = New Opcional
                    ReDim Preserve lDetalle.Opcionales(lDetalle.Opcionales.Length)
                    lDetalle.Opcionales(lDetalle.Opcionales.Length - 1) = lOpcional
                    lOpcional.Id = mFECAERequest.FeDetReq(I).Opcionales(J).Id
                    lOpcional.Valor = mFECAERequest.FeDetReq(I).Opcionales(J).Valor
                Next
            End If
        Next

        mCAEAInformarResponse = getClient().FECAEARegInformativo(mAuthRequest, Req)
        If isError(mCAEAInformarResponse.Errors) Then
            Return False
        End If

        Return True
    End Function

    Public Function CAEASinMovimientoConsultar(ByVal PtoVta As Integer, ByVal CAEA As String, ByRef Resultado As String) As Boolean
        Resultado = ""


        Dim response As FECAEASinMovConsResponse = getClient().FECAEASinMovimientoConsultar(mAuthRequest, CAEA, PtoVta)

        If Not isError(response.Errors) Then

            For I As Integer = 0 To response.ResultGet.Length - 1
                If Resultado <> "" Then
                    Resultado = Resultado + Chr(10)
                End If
                Resultado = Resultado + String.Format("{0}m, {1}, {2}", response.ResultGet(I).CAEA, response.ResultGet(I).FchProceso, response.ResultGet(I).PtoVta)
            Next

            Return True
        End If

        Return False
    End Function

    Public Function CAEASinMovimientoInformar(ByVal PtoVta As Integer, ByVal CAEA As String, ByRef Resultado As String) As Boolean
        Dim response As FECAEASinMovResponse = getClient().FECAEASinMovimientoInformar(mAuthRequest, PtoVta, CAEA)

        If Not isError(response.Errors) Then

            Resultado = response.Resultado

            Return True
        End If

        Return False
    End Function

    Public Function CmpConsultar(ByVal Tipo_cbte As Integer, ByVal Punto_vta As Integer, ByVal nro As Long, ByRef Cbte As FECompConsultaResponse) As Boolean
        Dim request As FECompConsultaReq = New FECompConsultaReq

        request.CbteTipo = Tipo_cbte
        request.PtoVta = Punto_vta
        request.CbteNro = nro


        Dim response As FECompConsultaResponse = getClient().FECompConsultar(mAuthRequest, request)

        If Not isError(response.Errors) Then

            Cbte = response

            Return True
        End If

        Return False
    End Function

    Private Function InternoConsultaCUIT(ByVal CUITConsulta As Long, ByRef response As ConsultaCuitResponse) As Boolean
        Try

            System.Net.ServicePointManager.ServerCertificateValidationCallback = AddressOf AcceptAllCertifications
            Dim awsClient As serviceA5.PersonaServiceA5 = New serviceA5.PersonaServiceA5

            If mModoProduccion Then
                awsClient.Url = URLWSServiceA5
            Else
                awsClient.Url = URLWSServiceA5_HOMO
            End If


            Dim awsresult As serviceA5.personaReturn = awsClient.getPersona(mAuthRequest.Token, mAuthRequest.Sign, mAuthRequest.Cuit, CUITConsulta)
            response = New ConsultaCuitResponse(awsresult)
            InternoConsultaCUIT = True
        Catch e As Exception
            mErrorCode = -1
            mErrorDesc = e.Message
        End Try


        Try
        Catch e As Exception
            mErrorCode = -1
            mErrorDesc = e.Message
            Return False
        End Try
    End Function

    Public Function AcceptAllCertifications(ByVal sender As Object, ByVal certification As System.Security.Cryptography.X509Certificates.X509Certificate, ByVal chain As System.Security.Cryptography.X509Certificates.X509Chain, ByVal sslPolicyErrors As System.Net.Security.SslPolicyErrors) As Boolean
        Return True
    End Function

    Private Function AddChecksum(ByVal Prefix As Integer, ByVal DNI As Double) As Long

        Dim DNIStr As String
        Dim Serie As Integer
        Dim I As Integer
        Dim Acc As Integer
        Dim Modulo As Integer

        DNIStr = Prefix.ToString & Double.Parse(DNI)
        Serie = 2
        Acc = 0
        For I = DNIStr.Length - 1 To 0 Step -1

            Acc = Acc + Integer.Parse(DNIStr.Chars(I)) * Serie
            If Serie = 7 Then
                Serie = 2
            Else
                Serie = Serie + 1
            End If
        Next
        Modulo = 11 - (Acc Mod 11)
        If Modulo = 11 Then
            Modulo = 0
        End If
        AddChecksum = DNIStr + Modulo.ToString
    End Function

    Public Function ConsultaCUIT(ByVal CUITConsulta As Long, ByRef response As ConsultaCuitResponse) As Boolean
        Dim CuitStr As String
        CuitStr = Long.Parse(CUITConsulta)

        If CuitStr.Length < 11 Then

            ConsultaCUIT = InternoConsultaCUIT(AddChecksum(20, CUITConsulta), response)
            If Not ConsultaCUIT Then
                ConsultaCUIT = InternoConsultaCUIT(AddChecksum(27, CUITConsulta), response)
            End If
            If Not ConsultaCUIT Then
                ConsultaCUIT = InternoConsultaCUIT(AddChecksum(23, CUITConsulta), response)
            End If
            If Not ConsultaCUIT Then
                ConsultaCUIT = InternoConsultaCUIT(AddChecksum(24, CUITConsulta), response)
            End If
        Else
            ConsultaCUIT = InternoConsultaCUIT(CUITConsulta, response)
        End If

    End Function
End Class
