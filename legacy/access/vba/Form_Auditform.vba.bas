Attribute VB_Name = "Form_Auditform"
Attribute VB_Base = "0{F0015CCA-84C9-4CB2-BFEF-831EC8F47111}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command17_Click()
On Error GoTo Err_Command17_Click
    Me.Recalc
    Dim stDocName As String

    stDocName = "D_Check_Audit"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command17_Click:
    Exit Sub

Err_Command17_Click:
    MsgBox Err.Description
    Resume Exit_Command17_Click
    
End Sub

Private Sub Form_Close()
    Form_DATE_DCHECK.Visible = True
End Sub
Private Sub Form_Open(Cancel As Integer)
    Form_DATE_DCHECK.Visible = False
End Sub

Private Sub Frame0_AfterUpdate()
    If Me.Frame0 = 1 And Me.Frame2 = 1 Then
        Me.Tadate = Me.Indate
    End If
End Sub

Private Sub Indate_AfterUpdate()
    If Me.Frame0 = 1 And Me.Frame2 = 1 Then
        Me.Tadate = Me.Indate
    End If
End Sub

Private Sub Tadate_BeforeUpdate(Cancel As Integer)
    If Me.Frame0 = 1 And Me.Frame2 = 1 Then
        Cancel = True
    End If
End Sub
