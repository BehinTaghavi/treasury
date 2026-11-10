Attribute VB_Name = "Form_Voucher"
Attribute VB_Base = "0{3611CA23-8422-48C6-95B3-B230B6B76774}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command17_Click()
        Dim stDocName As String
    Dim stLinkCriteria As String

    
    stLinkCriteria = "[Vid]=" & Me![Vid]

    
    stDocName = "Rep_Voucher"
    DoCmd.OpenReport stDocName, acPreview, , stLinkCriteria

End Sub

Private Sub Form_Close()
    Spfilter = 0
End Sub

Private Sub Form_Open(Cancel As Integer)
      If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If

    Spfilter = 1
End Sub
Private Sub Command22_Click()
On Error GoTo Err_Command22_Click


    DoCmd.Close

Exit_Command22_Click:
    Exit Sub

Err_Command22_Click:
    MsgBox Err.Description
    Resume Exit_Command22_Click
    
End Sub

Private Sub Command28_Click()
On Error GoTo Err_Command28_Click


    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command28_Click:
    Exit Sub

Err_Command28_Click:
    MsgBox Err.Description
    Resume Exit_Command28_Click
    
End Sub
