
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Drawing
Imports System.Collections
Imports System.Drawing.Drawing2D

Public Class TBarcodeBitmap
    Private Shared Function digitoVerificador(ByVal codigo As String) As String
        Dim totalpares As Integer
        Dim totalimpares As Integer
        Dim digito As Integer

        totalimpares = 0
        For x As Integer = 0 To codigo.Length - 1 Step 2
            totalimpares = totalimpares + [Byte].Parse(codigo.Substring(x, 1))
        Next
        totalimpares = totalimpares * 3


        totalpares = 0
        For x As Integer = 1 To codigo.Length - 1 Step 2
            totalpares = totalpares + [Byte].Parse(codigo.Substring(x, 1))
        Next

        Dim xx As Integer = totalimpares + totalpares
        digito = 0

        While CInt(xx / 10) * 10 <> xx
            xx = xx + 1
            digito = digito + 1
        End While

        Return digito.ToString()
    End Function

    Public Shared Function generarCodigoBarras(ByVal cuit As Int64, ByVal tipoComprobante As [Byte], ByVal puntoVenta As [Byte], ByVal cae As String, ByVal fechaVencimiento As DateTime, ByVal basewidth As Single, _
     ByVal height As Single, ByVal archivo As String) As String
        Dim code As String = cuit.ToString("00000000000") + tipoComprobante.ToString("00") + puntoVenta.ToString("0000") + cae & fechaVencimiento.ToString("yyyyMMdd")
        code = code & digitoVerificador(code)
        Dim xpos As Single = 0
        Dim ypos As Single = 0
        Dim flag As New Bitmap(6000, 1000)
        Dim flagGraphics As Graphics = Graphics.FromImage(flag)

        Dim brush As Brush = New SolidBrush(Color.White)
        flagGraphics.FillRectangle(brush, 0, 0, flag.Width, flag.Height)
        brush = New SolidBrush(Color.Black)
        'brush.Dispose();
        'Pen pen = new Pen(Color.Black);

        If basewidth = 0 Then
            basewidth = 2
        End If
        If height = 0 Then
            height = 30
        End If

        Dim wide As Single = basewidth
        Dim narrow As Single = basewidth / 3

        ' wide/narrow codes for the digits
        Dim barChar As IDictionary(Of String, String) = New Dictionary(Of String, String)()
        barChar.Add("0", "nnwwn")
        barChar.Add("1", "wnnnw")
        barChar.Add("2", "nwnnw")
        barChar.Add("3", "wwnnn")
        barChar.Add("4", "nnwnw")
        barChar.Add("5", "wnwnn")
        barChar.Add("6", "nwwnn")
        barChar.Add("7", "nnnww")
        barChar.Add("8", "wnnwn")
        barChar.Add("9", "nwnwn")
        barChar.Add("A", "nn")
        barChar.Add("Z", "wn")

        ' add leading zero if code-length is odd
        If code.Length Mod 2 <> 0 Then
            code = Convert.ToString("0") & code
        End If

        ' add start and stop codes
        code = "AA" + code.ToLower() + "ZA"

        For i As Integer = 0 To code.Length - 1 Step 2
            ' choose next pair of digits
            Dim charBar As String = code.Substring(i, 1)
            Dim charSpace As String = code.Substring(i + 1, 1)
            ' check whether it is a valid digit
            If Not barChar.ContainsKey(charBar) Then
                Return Convert.ToString("Caracter inválido en código de barras : ") & charBar
            End If
            If Not barChar.ContainsKey(charSpace) Then
                Return Convert.ToString("Caracter inválido en código de barras : ") & charSpace
            End If

            ' create a wide/narrow-sequence (first digit=bars, second digit=spaces)
            Dim seq As String = ""
            For s As Integer = 0 To barChar(charBar).Length - 1
                seq = (seq & barChar(charBar).Substring(s, 1)) + barChar(charSpace).Substring(s, 1)
            Next
            For bar As Integer = 0 To seq.Length - 1
                Dim lineWidth As Single
                ' set lineWidth depending on value
                If seq.Substring(bar, 1) = "n" Then
                    lineWidth = narrow
                Else
                    lineWidth = wide
                End If
                ' draw every second value, because the second digit of the pair is represented by the spaces
                If bar Mod 2 = 0 Then
                    flagGraphics.FillRectangle(brush, xpos, ypos, lineWidth, height)
                End If
                xpos = xpos + lineWidth
            Next
        Next

        Dim rect As New RectangleF(0, 0, xpos, height)
        flag = flag.Clone(rect, flag.PixelFormat)
        flag.Save(archivo)
        Return code
    End Function


End Class
