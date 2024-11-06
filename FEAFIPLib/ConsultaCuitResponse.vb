Imports System.Web.Script.Serialization
Public Class ConsultaCuitResponse
    Private CondicionesIVA As String() = {"Monotributo", "Responsable Inscripto", "IVA Exento", "Consumidor Final"}
    Private _response As serviceA5.personaReturn
    Private Function FindTax(ByVal TaxId As Integer) As Boolean
        FindTax = False
        If Not (_response.datosRegimenGeneral.impuesto Is Nothing) Then
            For I As Integer = 0 To _response.datosRegimenGeneral.impuesto.Length - 1
                If (_response.datosRegimenGeneral.impuesto(I).idImpuesto = TaxId) Then
                    FindTax = True
                    Exit Function
                End If
            Next
        End If
    End Function

    Sub New(ByVal responseObj As serviceA5.personaReturn)
        _response = responseObj
    End Sub

    Public ReadOnly Property idPersona() As Long
        Get
            idPersona = _response.datosGenerales.idPersona
        End Get
    End Property
    Public ReadOnly Property tipoPersona() As String
        Get
            If _response.datosGenerales Is Nothing Then
                tipoPersona = ""
            Else
                tipoPersona = _response.datosGenerales.tipoPersona
            End If
        End Get
    End Property
    Public ReadOnly Property nombre() As String
        Get
            If _response.errorConstancia Is Nothing Then
                If tipoPersona = "FISICA" Then
                    nombre = _response.datosGenerales.apellido + " " + _response.datosGenerales.nombre
                Else
                    nombre = _response.datosGenerales.razonSocial
                End If
            Else
                nombre = _response.errorConstancia.apellido + " " + _response.errorConstancia.nombre
            End If
        End Get
    End Property
    Public ReadOnly Property tipoDocumento() As String
        Get
            tipoDocumento = "NO DISPONIBLE"
        End Get
    End Property
    Public ReadOnly Property numeroDocumento() As String
        Get
            numeroDocumento = "NO DISPONIBLE"
        End Get
    End Property
    Public ReadOnly Property domicilioFiscal_direccion() As String
        Get
            Try
                domicilioFiscal_direccion = _response.datosGenerales.domicilioFiscal.direccion
            Catch
                domicilioFiscal_direccion = ""
            End Try
        End Get
    End Property
    Public ReadOnly Property domicilioFiscal_localidad() As String
        Get
            Try
                domicilioFiscal_localidad = _response.datosGenerales.domicilioFiscal.localidad
            Catch
                domicilioFiscal_localidad = ""
            End Try
        End Get
    End Property
    Public ReadOnly Property domicilioFiscal_codPostal() As String
        Get
            Try
                domicilioFiscal_codPostal = _response.datosGenerales.domicilioFiscal.codPostal
            Catch
                domicilioFiscal_codPostal = ""
            End Try
        End Get
    End Property
    Public ReadOnly Property domicilioFiscal_idProvincia() As Integer
        Get
            Try
                domicilioFiscal_idProvincia = _response.datosGenerales.domicilioFiscal.idProvincia
            Catch
                domicilioFiscal_idProvincia = 0
            End Try
        End Get
    End Property
    Public ReadOnly Property condicionIVA()
        Get
            condicionIVA = 3
            If Not (_response.datosMonotributo Is Nothing) Then
                condicionIVA = 0 ' Monotributo
            ElseIf FindTax(30) Then
                condicionIVA = 1 ' Responsable inscripto
            ElseIf FindTax(32) Then
                condicionIVA = 2 ' IVA Exento
            End If
        End Get
    End Property
    Public ReadOnly Property condicionIVADesc()
        Get
            condicionIVADesc = CondicionesIVA(condicionIVA)
        End Get
    End Property


End Class
