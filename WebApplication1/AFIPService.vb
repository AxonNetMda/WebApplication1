Imports System.Net
Imports System.Xml
Imports FEAFIPLib
Imports FEAFIPLib.Wsaa

Public Class AFIPService
    Private wsfev1 As New BIWSFEV1()

    ''' <summary>
    ''' Realiza el login en el servicio de facturación de AFIP utilizando el certificado.
    ''' </summary>
    ''' <param name="rutaCertificado">Ruta del archivo .pfx del certificado.</param>
    ''' <param name="passwordCertificado">Contraseña del certificado.</param>
    ''' <param name="modoProduccion">Indica si se usa el entorno de producción.</param>
    ''' <returns>True si el login es exitoso; de lo contrario, False.</returns>
    Public Function LoginAFIP(rutaCertificado As String, passwordCertificado As String, modoProduccion As Boolean) As Boolean
        Try
            wsfev1.ModoProduccion = modoProduccion
            If wsfev1.login(rutaCertificado, passwordCertificado) Then
                Return True
            Else
                Console.WriteLine("Error en login: LoginAFIP LoginAFIP" & wsfev1.ErrorDesc)
                Return False
            End If
        Catch ex As Exception
            Console.WriteLine("Excepción en el login: LoginAFIP LoginAFIP" & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Genera y autoriza una factura en AFIP.
    ''' </summary>
    ''' <param name="ptoVta">Punto de venta.</param>
    ''' <param name="tipoComprobante">Tipo de comprobante (por ejemplo, 11 para factura electrónica).</param>
    ''' <param name="importe">Importe total de la factura.</param>
    ''' <returns>Mensaje de éxito o error.</returns>
    Public Function CrearYAutorizarFactura(ptoVta As Integer, tipoComprobante As Integer, importe As Decimal) As String
        Try
            Dim nroComprobante As Integer

            ' Obtener el último número de comprobante autorizado en el punto de venta
            If wsfev1.recuperaLastCMP(ptoVta, tipoComprobante, nroComprobante) Then
                nroComprobante += 1 ' Incrementar para el próximo comprobante

                ' Configuración de la factura
                wsfev1.reset()
                Dim fecha As Date = Date.Now
                wsfev1.agregaFactura(1, 99, 0, nroComprobante, nroComprobante, fecha, importe, 0, importe, 0, Nothing, Nothing, Nothing, "PES", 1)

                ' Enviar la solicitud de autorización a AFIP
                If wsfev1.autorizar(ptoVta, tipoComprobante) Then
                    Dim cae As String = ""
                    Dim vencimiento As DateTime
                    Dim resultado As String = ""

                    ' Obtener los datos de autorización
                    wsfev1.autorizarRespuesta(0, cae, vencimiento, resultado)
                    If resultado = "A" Then
                        Return "Autorización exitosa. CAE: " & cae & ", Vencimiento: " & vencimiento
                    Else
                        Return "Observación de AFIP: " & wsfev1.autorizarRespuestaObs(0)
                    End If
                Else
                    Return "Error en la autorización: " & wsfev1.ErrorDesc
                End If
            Else
                Return "Error al obtener último comprobante: " & wsfev1.ErrorDesc
            End If
        Catch ex As Exception
            Return "Error: " & ex.Message
        End Try
    End Function
End Class
