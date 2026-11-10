Attribute VB_Name = "Form_Cashandpetty"
Attribute VB_Base = "0{1040E78E-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Baccno_BeforeUpdate(Cancel As Integer)
    mystr = "Select * from Bankacc Where Baccno=" & "'" & CStr(Nz(Me.Baccno)) & "'"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        Else
            MsgBox "شماره حساب تكراري است"
            Cancel = True
            Exit Sub
        End If
    End With

    oldbankacc = IIf(IsNull(Me.Baccno.OldValue), 0, Me.Baccno.OldValue)
    If oldbankacc <> 0 Then
        If Me.Baccno <> Me.Baccno.OldValue Then
            Editbacc Me.Baccno.OldValue, Me.Baccno
        End If
    End If
End Sub

Private Sub ComboA_AfterUpdate()
    Me.Reged = True
End Sub

Private Sub ComboA_DblClick(Cancel As Integer)
    Openaccs 29
End Sub

Private Sub ComboB_DblClick(Cancel As Integer)
    Openaccs 30
End Sub

Private Sub Command49_Click()
    On Error GoTo Err_Command49_Click

    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Sm"
    DoCmd.OpenForm stDocName, , , stLinkCriteria

Exit_Command49_Click:
    Exit Sub

Err_Command49_Click:
    MsgBox Err.Description
    Resume Exit_Command49_Click

End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Baccid = tellmelast(7)
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Baccno) Then
        MsgBox "شماره حساب نامشخص است"
        Cancel = True
        Exit Sub
    End If
    
    If IsNull(Me.ComboA) Then
        MsgBox "حساب معين نامشخص است"
        Cancel = True
        Exit Sub
    End If
    
    If IsNull(Me.ComboB) Then
        MsgBox "حساب تفصيل نامشخص است"
        Cancel = True
        Exit Sub
    End If

End Sub

Private Sub Iscur_AfterUpdate()
    If Me.Iscur = False Then
        Me.Combocur = "ريال"
        Me.Combocur.Enabled = False
    Else
        Me.Combocur = ""
        Me.Combocur.Enabled = True
    End If

End Sub
