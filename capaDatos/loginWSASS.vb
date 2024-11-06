Imports System.Data.SqlClient
Imports System.Xml
Imports capaDatos.Conexion

Public Class loginWSASS
    Public Function ObtenerXmlPorCuit(cuit As Double) As XmlDocument
        Dim connectionString As String = "your_connection_string_here" ' Reemplaza con tu cadena de conexión
        Dim consultaSQL As String = "SELECT xml FROM loginticket WHERE cuit = @cuit"
        Dim xmlDocument As New XmlDocument()

        Using connection As New SqlConnection(conectar.Cadena)
            Dim command As New SqlCommand(consultaSQL, connection)
            command.Parameters.AddWithValue("@cuit", cuit)

            Try
                connection.Open()
                Dim resultadoXml As String = CStr(command.ExecuteScalar())

                ' Cargar el resultado en el XmlDocument
                If Not String.IsNullOrEmpty(resultadoXml) Then
                    xmlDocument.LoadXml(resultadoXml)
                Else
                    Throw New Exception("No se encontró el registro para el CUIT especificado.")
                End If

            Catch ex As Exception
                ' Manejo de excepciones (puedes personalizar este mensaje)
                Console.WriteLine("Error al obtener el XML: " & ex.Message)
                Return Nothing
            Finally
                connection.Close()
            End Try
        End Using

        Return xmlDocument
    End Function
    Public Shared Sub GuardarXmlPorCuit(cuit As Double, xmlDocument As XmlDocument)
        Dim xmlContent As String = xmlDocument.OuterXml

        Using connection As New SqlConnection(conectar.Cadena)
            ' Comprobar si el registro ya existe
            Dim checkQuery As String = "SELECT COUNT(*) FROM feafip_loginticket WHERE cuit = @cuit"
            Dim checkCommand As New SqlCommand(checkQuery, connection)
            checkCommand.Parameters.AddWithValue("@cuit", cuit)

            Try
                connection.Open()
                Dim exists As Boolean = CInt(checkCommand.ExecuteScalar()) > 0

                If exists Then
                    ' Actualizar el registro existente
                    Dim updateQuery As String = "UPDATE feafip_loginticket SET xlm = @xlm WHERE cuit = @cuit"
                    Dim updateCommand As New SqlCommand(updateQuery, connection)
                    updateCommand.Parameters.AddWithValue("@xlm", xmlContent)
                    updateCommand.Parameters.AddWithValue("@cuit", cuit)

                    updateCommand.ExecuteNonQuery()
                    Console.WriteLine("Registro actualizado correctamente.")
                Else
                    ' Insertar un nuevo registro
                    Dim insertQuery As String = "INSERT INTO feafip_loginticket (cuit, xlm) VALUES (@cuit, @xlm)"
                    Dim insertCommand As New SqlCommand(insertQuery, connection)
                    insertCommand.Parameters.AddWithValue("@cuit", cuit)
                    insertCommand.Parameters.AddWithValue("@xlm", xmlContent)

                    insertCommand.ExecuteNonQuery()
                    Console.WriteLine("Registro insertado correctamente.")
                End If

            Catch ex As Exception
                ' Manejo de errores
                Console.WriteLine("Error al guardar el XML: " & ex.Message)
            Finally
                connection.Close()
            End Try
        End Using
    End Sub
End Class
