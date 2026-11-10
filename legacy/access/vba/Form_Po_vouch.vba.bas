Attribute VB_Name = "Form_Po_vouch"
Attribute VB_Base = "0{6A729AFB-954B-4A48-97FD-9A23AD0E5655}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim rst As Recordset
Dim mystr As String, Myrowdesc As String

Private Sub Babat_AfterUpdate()
    Myrowdesc = Nz(Me.rowdesc, "")
End Sub

Private Sub babat_DblClick(Cancel As Integer)
    If IsNull(Myrowdesc) = False Or Myrowdesc <> "" Then Me.Babat = Myrowdesc
End Sub

Private Sub babat_GotFocus()
    If IsNull(Me.ComboA) Then
        MsgBox "سطر سند فاقد حساب معين است"
        Me.ComboA.SetFocus
        Exit Sub
    End If
    If Me.ComboA.Column(3) = -1 And IsNull(ComboB) Then
        MsgBox "سطر سند فاقد حساب تفصيل است"
        Me.ComboB.SetFocus
    End If

End Sub

Private Sub ComboA_AfterUpdate()
    If Me.ComboA.Column(3) = -1 Then
        Me.ComboB.Enabled = True
    Else
        Me.ComboB = Null
        Me.ComboB.Enabled = False
    End If
    If Me.ComboA.Column(3) = -1 And IsNull(ComboB) Then
'        MsgBox "سطر سند فاقد حساب تفصيل است"
        Me.ComboB.SetFocus
    End If
End Sub

Private Sub ComboA_BeforeUpdate(Cancel As Integer)
    If FLnot(Len(Me.ComboA)) = True Then
        MsgBox "سطح حساب نادرست است"
        Cancel = True
    End If

    If Me.ComboA.Column(4) = -1 Then
        MsgBox "حساب داراي سطح معين در معين است"
        Cancel = True
    End If
End Sub

Private Sub ComboA_DblClick(Cancel As Integer)
    If lockcontrol = 1 Then Exit Sub
    Openaccs 33
End Sub
Private Sub ComboB_DblClick(Cancel As Integer)
    If lockcontrol = 1 Then Exit Sub
    Openaccs 34
End Sub

Private Sub Command35_Click()
    S = "INSERT INTO Povouch ( Checkid, Sid, accno, tafno, bed, bes, rowdesc, Recno, Bid ) SELECT Povouch.Checkid, Povouch.Sid, Povouch.accno, Povouch.tafno, Povouch.bed, Povouch.bes, Povouch.rowdesc, Povouch.Recno, Povouch.Bid FROM Povouch WHERE (((Povouch.Sqlid)=" & Nz(Me.Sqlid) & "));"
    docommand S
    Me.Requery
    DoCmd.GoToRecord , , acLast
End Sub

Private Sub Form_AfterInsert()
    Me.Recalc
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.ComboA) Then
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

Private Sub Form_Current()
    If Me.ComboA.Column(3) = -1 Then
        Me.ComboB.Enabled = True
    Else
        Me.ComboB.Enabled = False
    End If
End Sub

Private Sub Form_LostFocus()
'    If Me.Tsumbed <> Me.Tsumbes Then
'        MsgBox "سند موازنه نيست"
'        Me.Bed.SetFocus
'    End If
End Sub

Private Sub Form_Open(Cancel As Integer)
    'myopenform = Me.Name
End Sub


