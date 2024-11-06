<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="FacturaCv01.aspx.vb" Debug="true" Inherits="WebApplication1.FacturaCv01" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
             <asp:Label runat="server" CssClass="form-label" text="Acciones" />
             <asp:Button ID="btnAgregar" runat="server" Text="Agregar" OnClick="btnAgregar_Click" /><br /><br />      
            <asp:Button ID="btnConstatar" runat="server" Text="Agregar" OnClick="btnConstatar_Click" /><br /><br />    
        </div>
    </form>
</body>
</html>
