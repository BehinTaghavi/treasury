Attribute VB_Name = "Form_Pordersub_vouch"
Attribute VB_Base = "0{1040E8E7-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim rst As Recordset
Dim mystr As String

Private Sub ComboA_AfterUpdate()
    If Me.ComboA.Column(3) = -1 And IsNull(ComboB) Then
        MsgBox "سطر سند فاقد حساب تفصيل است"
        Me.ComboB.SetFocus
    End If
End Sub

Private Sub ComboA_DblClick(Cancel As Integer)
    Openaccs 31
End Sub

Private Sub ComboB_DblClick(Cancel As Integer)
    Openaccs 32
End Sub

Private Sub Command31_Click()
    If Me.RecordsetClone.RecordCount = 0 Then
        Exit Sub
    End If

    If MsgBox("همه سطرهاحذف شوند؟", vbYesNo) = vbNo Then
        Exit Sub
    Else
        mystr = "select * from Povouch where Checkid=" & Forms![Check_Print_single]![Checkid]
        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
        With rst
            While Not .EOF
                .Delete
                .MoveNext
            Wend
        End With
        Me.Requery
    End If
End Sub

Private Sub Command35_Click()
    S = "INSERT INTO Povouch ( Checkid, Sid, accno, tafno, bed, bes, rowdesc, Recno, Bid ) SELECT Povouch.Checkid, Povouch.Sid, Povouch.accno, Povouch.tafno, Povouch.bed, Povouch.bes, Povouch.rowdesc, Povouch.Recno, Povouch.Bid FROM Povouch WHERE (((Povouch.Sqlid)=" & Nz(Me.Sqlid) & "));"
    docommand S
    Me.Recalc
End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Checkid = Forms![Check_Print_single]![Checkid]
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Accno) Then
        MsgBox "سطر سند فاقد حساب معين است"
        Cancel = True
        Me.ComboA.SetFocus
        Exit Sub
    End If
    If IsNull(Me.Babat) Then
        MsgBox "سطر سند فاقد شرح است"
        Cancel = True
        Me.Babat.SetFocus
        Exit Sub
    End If
    
    If IsNull(Me.bed) Then
        Me.bed = 0
    End If
    
    If IsNull(Me.bes) Then
        Me.bes = 0
    End If
    
    If Me.bed = 0 And Me.bes = 0 Then
        MsgBox "سطر سند فاقد مبلغ است"
        Cancel = True
        Me.bed.SetFocus
        Exit Sub
    End If
End Sub

Private Sub Form_Close()
 myopenform = ""
End Sub

Private Sub Form_Open(Cancel As Integer)
    myopenform = Me.Name
End Sub


