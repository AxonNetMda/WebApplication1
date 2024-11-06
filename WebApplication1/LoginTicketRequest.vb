
Imports System.Xml
Imports System.Security.Cryptography.X509Certificates
Imports System.Security.Cryptography.Pkcs
Imports System.IO
Public Class LoginTicketRequest
    Public Sub GenerarTRA(servicio As String)
        Dim xmlDoc As New XmlDocument()

        ' Crear el elemento raíz del TRA
        Dim root As XmlElement = xmlDoc.CreateElement("loginTicketRequest")
        root.SetAttribute("version", "1.0")
        xmlDoc.AppendChild(root)

        ' Crear el elemento header
        Dim header As XmlElement = xmlDoc.CreateElement("header")
        root.AppendChild(header)

        ' Crear elementos individuales de header
        Dim uniqueId As XmlElement = xmlDoc.CreateElement("uniqueId")
        uniqueId.InnerText = DateTime.Now.Ticks.ToString()
        header.AppendChild(uniqueId)

        Dim generationTime As XmlElement = xmlDoc.CreateElement("generationTime")
        generationTime.InnerText = DateTime.Now.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ss")
        header.AppendChild(generationTime)

        Dim expirationTime As XmlElement = xmlDoc.CreateElement("expirationTime")
        expirationTime.InnerText = DateTime.Now.AddMinutes(+10).ToString("yyyy-MM-ddTHH:mm:ss")
        header.AppendChild(expirationTime)

        Dim serviceElement As XmlElement = xmlDoc.CreateElement("service")
        serviceElement.InnerText = servicio
        root.AppendChild(serviceElement)

        ' Guardar el archivo TRA.xml en una ubicación accesible
        xmlDoc.Save("C:\Path\To\TRA.xml")
    End Sub
    Public Function FirmarTRA(rutaTRA As String, rutaCertificado As String, passwordCertificado As String) As String
        Dim certificado As New X509Certificate2(rutaCertificado, passwordCertificado)
        Dim contenidoTRA As Byte() = File.ReadAllBytes(rutaTRA)

        Dim cms As New ContentInfo(contenidoTRA)
        Dim signedCms As New SignedCms(cms)
        Dim cmsSigner As New CmsSigner(certificado)
        signedCms.ComputeSignature(cmsSigner)

        Dim firmaCMS As Byte() = signedCms.Encode()
        Return Convert.ToBase64String(firmaCMS)
    End Function
End Class
