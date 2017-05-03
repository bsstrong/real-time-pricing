<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="com.LoanTek.API.Leads.Facebook.WebForm1" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Button runat="server" id="btnTest" text="Test" OnClick="btnTest_OnClick"/>
        <asp:Label runat="server" id="lblInfo"></asp:Label>
    </div>
    </form>
</body>
</html>
