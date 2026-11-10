Attribute VB_Name = "Form_Sorts"
Attribute VB_Base = "0{1040E913-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Combo4_AfterUpdate()
    Me.Ittype = Me.Combo4.Column(3)
End Sub

Private Sub Command9_Click()
    Me.Requery
    Dim dbs As Database
    Dim rst As Recordset
    Set dbs = CurrentDb
    Set rst = dbs.OpenRecordset("SELECT * FROM SORTS WHERE ITTYPE= " & Searchtype, dbOpenDynaset, dbSeeChanges)
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

Private Sub Form_Open(Cancel As Integer)
    Me.Text0 = Forms!Sal!Ttype
End Sub
