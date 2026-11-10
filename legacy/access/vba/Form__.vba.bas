Attribute VB_Name = "Form_\\"
Attribute VB_Base = "0{6EED12FF-D3D3-4790-B8F4-30A9F0B7AB9D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim S As String

Private Sub Command4_Click()
    DoCmd.Close
End Sub

Private Sub Form_Close()
'Checktypename
End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
    Spfilter = 0
End Sub
Private Sub Command7_Click()
On Error GoTo Err_Command7_Click

    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Check_s_Finder"
    DoCmd.OpenForm stDocName, , , stLinkCriteria

Exit_Command7_Click:
    Exit Sub

Err_Command7_Click:
    MsgBox Err.Description
    Resume Exit_Command7_Click
    
End Sub
