Attribute VB_Name = "Form_Shart"
Attribute VB_Base = "0{1040E905-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Combo10_GotFocus()
    If IsNull(Me.Combo8) Then
        MsgBox "نوع متغير را مشخص فرمائيد"
        Me.Combo8.SetFocus
    End If
End Sub

Private Sub Combo8_AfterUpdate()
    Me.Ittype = Me.Combo8.Column(5)
    Me.Fldtype = Me.Combo8.Column(4)
End Sub

Private Sub Command15_Click()
    Me.Requery
    Dim dbs As Database
    Dim rst As Recordset
    Set dbs = CurrentDb
    Set rst = dbs.OpenRecordset("SELECT * FROM SHART WHERE ITTYPE=" & Searchtype, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        End If
        
        While Not .EOF
        .Delete
        .MoveNext
        Wend
    End With
    Me.Requery

End Sub

Private Sub D1_AfterUpdate()
     Me.D2 = Me.D1
End Sub

Private Sub D1_GotFocus()
    If IsNull(Me.Combo8) Then
        MsgBox "نوع متغير را مشخص فرمائيد"
        Me.Combo8.SetFocus
    End If
    
    If IsNull(Me.Combo10) Then
        MsgBox "شرط را مشخص فرمائيد"
        Me.Combo10.SetFocus
    End If
End Sub

Private Sub D2_GotFocus()
    If IsNull(Me.Combo8) Then
        MsgBox "نوع متغير را مشخص فرمائيد"
        Me.Combo8.SetFocus
    End If
    
    If IsNull(Me.Combo10) Then
        MsgBox "شرط را مشخص فرمائيد"
        Me.Combo10.SetFocus
    End If

End Sub

Private Sub Form_Open(Cancel As Integer)
    Me.Text0 = Forms!Sal!Ttype
End Sub
