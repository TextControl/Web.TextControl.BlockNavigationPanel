﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="index.aspx.cs" Inherits="tx_sample_html5.index" %>

<%@ Register assembly="TXTextControl.Web, Version=22.0.200.500, Culture=neutral, PublicKeyToken=6b83fe9a75cfb638" namespace="TXTextControl.Web" tagprefix="cc1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link href="styles.css" rel="stylesheet" />
    <script src="Scripts/jquery-2.1.3.js"></script>
</head>
<body>
    <form id="form1" runat="server">
    <div style="width: 100%;">
    
        <cc1:TextControl id="TextControl1" runat="server" Dock="Window" />
        
        <!-- this is the navigation bar which is not visible by default -->
        <div id="navigationBar">
            <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                                
                <ContentTemplate>
                    
                    <asp:Button ID="hiddenBtnUpdateNavigationPanel" runat="server" Text="Button" OnClick="hiddenBtnUpdateNavigationPanel_Click" style="display: none;"/>
                    
                    <h3>Block Navigation</h3>
                    <img class="close" onclick="togglePanel();" src="images/cross.png" />
                    
                    <asp:TreeView style="clear: both;" ID="TreeView1" runat="server" AutoGenerateDataBindings="False" ShowLines="True" OnSelectedNodeChanged="TreeView1_SelectedNodeChanged" />

                </ContentTemplate>
                <Triggers>
                    <asp:AsyncPostBackTrigger ControlID="hiddenBtnUpdateNavigationPanel" EventName="Click" />
                </Triggers>
            </asp:UpdatePanel>
        </div>

    </div>

    <script>

        // enable the hidden JS API of TX Text Control
        TXTextControl.enableCommands();

        // add extra button, if the ribbon bar is loaded completely
        TXTextControl.addEventListener("ribbonTabsLoaded", function (e) {
            addButton();
        });

        // toggle the navigation panel
        function togglePanel() {
            $("#navigationBar").toggle();

            if ($("#navigationBar").css("display") == "none") {
                $("#navigationPanelButton").removeClass("ribbon-button-selected");
                TXTextControl.sendCommand(TXTextControl.Command.SetEditMode, TXTextControl.EditMode.Edit);
            }
            else {
                $("#navigationPanelButton").addClass("ribbon-button-selected");
                TXTextControl.sendCommand(TXTextControl.Command.SetEditMode, TXTextControl.EditMode.ReadAndSelect);
            }
        }

        // this method adds the additional button to the existing ribbon bar DOM
        // including the JS to post pack the hidden button in the UpdatePanel
        function addButton() {
            sNavigationPanelBtn = '<div class="ribbon-group" id="newGroup"> \
                <div class="ribbon-group-content"> \
                <div id="navigationPanelButton" class="ribbon-button ribbon-button-big"> \
                <div class="ribbon-button-big-image-container"> \
                <img src="images/mailmergefieldnavigation.png" class="ribbon-button-big-image" /> \
                </div> \
                <div class="ribbon-button-big-label-container"> \
                <p class="ribbon-button-label">Block<br />Navigation</p> \
                </div> \
                </div> \
                </div> \
                <div class="ribbon-group-label-container"> \
                <p class="ribbon-group-label">Navigation</p> \
                </div></div>';
 
            // add the new button and ribbon group using HTML 
            document.getElementById('ribbonGroupView').insertAdjacentHTML(
                'beforebegin', sNavigationPanelBtn);
 
            // force a post back on the invisible button 
            document.getElementById("navigationPanelButton").addEventListener(
                "click", 
                function () {
                    togglePanel();
                    __doPostBack('<%= hiddenBtnUpdateNavigationPanel.ClientID %>', '');
                });
    }
    </script>

    </form>
</body>
</html>