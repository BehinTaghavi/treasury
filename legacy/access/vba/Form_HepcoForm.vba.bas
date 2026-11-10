Attribute VB_Name = "Form_HepcoForm"
Attribute VB_Base = "0{8CDCFF4C-FC38-4EFA-A695-239DD2774397}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Form_Close()
    Forms!Date_dcheck_entez.Visible = True
End Sub

Private Sub Form_Open(Cancel As Integer)
    Forms!Date_dcheck_entez.Visible = False
End Sub

Private Sub Frametype_AfterUpdate()
    Select Case Me.Frametype
        Case 0, 2, 10, 13, 20, 22
            Me.Check1.Enabled = False
            Me.Check2.Enabled = False
            Me.Check6.Enabled = False
            Me.Check4.Enabled = False
        Case Else
            Me.Check1.Enabled = True
            Me.Check2.Enabled = True
            Me.Check6.Enabled = True
            Me.Check4.Enabled = True
    End Select
End Sub
Private Sub Command71_Click()
On Error GoTo Err_Command71_Click
    Me.Recalc
    Dim stDocName As String
    
    If Me.Framewhatpay < 5 Then
    stDocName = "HepcoRep"
    Else
    stDocName = "HepcoRep4column"
    End If
    
    DoCmd.OpenReport stDocName, acPreview

Exit_Command71_Click:
    Exit Sub

Err_Command71_Click:
    MsgBox Err.Description
    Resume Exit_Command71_Click
    
End Sub
