Imports System.Security.Cryptography.X509Certificates
Imports FEAFIPLib.Wsaa

Public Class FacturaCv01
    Inherits System.Web.UI.Page
    Private wsaa As New LoginCMSService()
    Private token As String = ""
    Private sign As String = ""


    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

    End Sub

    Protected Sub btnAgregar_Click(sender As Object, e As EventArgs)
        Dim afipService As New AFIPService()
        Dim rutaCertificado As String = Server.MapPath("~/App_Data/axontest2024.pfx")
        Dim passwordCertificado As String = "axontest"
        Dim modoProduccion As Boolean = False ' Cambiar a True para producción

        ' Realizar login con AFIP
        If afipService.LoginAFIP(rutaCertificado, passwordCertificado, modoProduccion) Then
            ' Crear y autorizar factura
            Dim resultado As String = afipService.CrearYAutorizarFactura(3, 11, 200.0)
            Response.Write(resultado)
        Else
            Response.Write("Error en el login con AFIP. 20241104")
        End If
        'Dim nroComprobante As ULong
        'Dim ptoVta As Integer = 3
        'Dim FechaComp As Date = Now
        'Dim cae As String = ""
        'Dim vencimiento As Date
        'Dim resultado As String = ""

        'Dim wsfev1 As FEAFIPLib.BIWSFEV1 = New FEAFIPLib.BIWSFEV1
        'wsfev1.ModoProduccion = True
        'Dim rutaTRA As String = Server.MapPath("~/App_Data/TRA.xml")
        'Dim rutaCertificado As String = Server.MapPath("~/App_Data/certificadotaboada.pfx")
        'Dim passwordCertificado As String = "patita40"
        'If wsfev1.login(rutaCertificado, "patita40") Then
        '    If wsfev1.recuperaLastCMP(ptoVta, 11, nroComprobante) Then
        '        nroComprobante += 1
        '        wsfev1.reset()
        '        wsfev1.agregaFactura(1, 99, 0, nroComprobante, nroComprobante, FechaComp, 200, 0, 200, 0, Nothing, Nothing, Nothing, "PES", 1)
        '        'wsfev1.agregaIVA(5, 200, 0)
        '        If wsfev1.autorizar(ptoVta, 11) Then
        '            wsfev1.autorizarRespuesta(0, cae, vencimiento, resultado)
        '            If resultado = "A" Then
        '                Response.Write("Felicitaciones! Si ve este mensaje instalo correctamente FEAFIP. CAE y Vencimiento: " + cae + " " + vencimiento)
        '            Else
        '                Response.Write(wsfev1.autorizarRespuestaObs(0))
        '            End If
        '        Else
        '            MsgBox(wsfev1.ErrorDesc)
        '        End If
        '    Else
        '        Response.Write(wsfev1.ErrorDesc)
        '    End If
        'Else
        '    Response.Write(wsfev1.ErrorDesc)
        'End If
    End Sub

    Protected Sub btnConstatar_Click(sender As Object, e As EventArgs)
        Dim wscdc As FEAFIPLib.WscdcClass = New FEAFIPLib.WscdcClass()
        Dim rutaCertificado As String = Server.MapPath("~/App_Data/certificadotaboada.pfx")
        Dim passwordCertificado As String = "patita40"
        wscdc.ModoProduccion = True
        If wscdc.login(rutaCertificado, passwordCertificado) Then
            If wscdc.ComprobanteConstatar("CAE", 20233273644, 3, 11, 13, "20241101", 200, "74444646477939", "99", "00000000") Then
                MsgBox("Comprobante constatado con éxito.")
            Else
                MsgBox(wscdc.ErrorDesc)
            End If
        Else
            MsgBox(wscdc.ErrorDesc)
        End If
    End Sub
    'Public Function Autenticar(rutaCertificado As String, passwordCertificado As String) As Boolean
    '    Try
    '        ' Leer el certificado
    '        Dim certificado As New X509Certificate2(rutaCertificado, passwordCertificado)

    '        ' Crear el archivo de acceso (TRA.xml)
    '        Dim tra As New LoginTicketRequest()
    '        tra.generarTRA("ws_sr_padron_a5") ' Servicio para el WSRC
    '        Dim traXml As String = tra.obtenerTRA() ' Obtener el TRA en formato XML

    '        ' Firmar el TRA con el certificado
    '        Dim cms As String = tra.firmarTRA(traXml, certificado)

    '        ' Enviar el CMS firmado al WSAA y obtener la respuesta
    '        Dim loginCMSResponse As LoginTicketResponse = wsaa.loginCms(cms)
    '        token = loginCMSResponse.token
    '        sign = loginCMSResponse.sign

    '        Return True
    '    Catch ex As Exception
    '        Console.WriteLine("Error en la autenticación: " & ex.Message)
    '        Return False
    '    End Try
    'End Function
End Class