Attribute VB_Name = "Form_Bankacc"
Attribute VB_Base = "0{1040E772-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Baccno_BeforeUpdate(Cancel As Integer)
    mystr = "Select * from Bankacc Where Baccno=" & "'" & CStr(Me.Baccno) & "'"
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

Private Sub Combo59_AfterUpdate()
    Me.Combo59.Requery
End Sub

Private Sub Combo60_AfterUpdate()
    Me.Combo60.Requery
End Sub

Private Sub ComboA_AfterUpdate()
    Me.Reged = True
End Sub

Private Sub ComboA_DblClick(Cancel As Integer)
    Openaccs 25
End Sub

Private Sub ComboB_DblClick(Cancel As Integer)
    Openaccs 26
End Sub

Private Sub combobankid_AfterUpdate()
    Me.Bank = Me.combobankid.Column(1)
End Sub

Private Sub ComboC_DblClick(Cancel As Integer)
    Openaccs 27
End Sub

Private Sub Combocur_AfterUpdate()
    If IsNull(Me.Combocur) Then
       Me.Combocur = "ريال"
    End If
    Me.Combocur.Requery
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
    
    If IsNull(Me.combobankid) Or Me.combobankid = 0 Then
        MsgBox "نام بانك نامشخص است"
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

Private Sub Form_Current()
    If Me.Iscur = False Then
        Me.Combocur.Enabled = False
    Else
        Me.Combocur.Enabled = True
    End If

End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
End Sub

Private Sub Frame1_AfterUpdate()
    Select Case Me.Frame1
        Case 1
            Me.ComboA.RowSource = "SELECT Acc.Accno0, Acc.Accno, Acc.Accname FROM Acc WHERE (((Acc.Has_Sub_Subsidery)=False) AND ((Len(nz(Trim([Accno0]))))>=[forms]![sal]![Lmoeen])) ORDER BY Acc.Accno;"
            Me.ComboB.RowSource = "SELECT Acc.Accno0, Acc.Accno, Acc.Accname FROM Acc WHERE (((Acc.Has_Sub_Subsidery)=False) AND ((Len(nz(Trim([Accno0]))))>=[forms]![sal]![Lmoeen])) ORDER BY Acc.Accno;"
            Me.ComboC.RowSource = "SELECT Acc.Accno0, Acc.Accno, Acc.Accname FROM Acc WHERE (((Acc.Has_Sub_Subsidery)=False) AND ((Len(nz(Trim([Accno0]))))>=[forms]![sal]![Lmoeen])) ORDER BY Acc.Accno;"
            Me.ComboD.RowSource = "SELECT Acc.Accno0, Acc.Accno, Acc.Accname FROM Acc WHERE (((Acc.Has_Sub_Subsidery)=False) AND ((Len(nz(Trim([Accno0]))))>=[forms]![sal]![Lmoeen])) ORDER BY Acc.Accno;"
        Case 2
            Me.ComboA.RowSource = "SELECT Acc.Accno0, Acc.Accname, Acc.Accno FROM Acc WHERE (((Acc.Has_Sub_Subsidery)=False) AND ((Len(nz(Trim([Accno0]))))>=[forms]![sal]![Lmoeen])) ORDER BY Acc.Accname;"
            Me.ComboB.RowSource = "SELECT Acc.Accno0, Acc.Accname, Acc.Accno FROM Acc WHERE (((Acc.Has_Sub_Subsidery)=False) AND ((Len(nz(Trim([Accno0]))))>=[forms]![sal]![Lmoeen])) ORDER BY Acc.Accname;"
            Me.ComboC.RowSource = "SELECT Acc.Accno0, Acc.Accname, Acc.Accno FROM Acc WHERE (((Acc.Has_Sub_Subsidery)=False) AND ((Len(nz(Trim([Accno0]))))>=[forms]![sal]![Lmoeen])) ORDER BY Acc.Accname;"
            Me.ComboD.RowSource = "SELECT Acc.Accno0, Acc.Accname, Acc.Accno FROM Acc WHERE (((Acc.Has_Sub_Subsidery)=False) AND ((Len(nz(Trim([Accno0]))))>=[forms]![sal]![Lmoeen])) ORDER BY Acc.Accname;"
    End Select
    Me.ComboA.Requery
    Me.ComboB.Requery
    Me.ComboC.Requery
    Me.ComboD.Requery
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
Private Sub Command70_Click()
On Error GoTo Err_Command70_Click


    DoCmd.Close

Exit_Command70_Click:
    Exit Sub

Err_Command70_Click:
    MsgBox Err.Description
    Resume Exit_Command70_Click
    
End Sub
