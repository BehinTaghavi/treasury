Attribute VB_Name = "Form_Bundle"
Attribute VB_Base = "0{386E0AC3-A4FB-4DB9-9237-6E52430FBAE7}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mystr As String, rst As Recordset
Private Sub Combo15_AfterUpdate()
    Me.Printcode = Forms![Bundle_m]![Combo0].Column(4) & Me.Sheets
    Me.Lsheet = Me.Fsheet + Me.Sheets - 1
End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.bundleid = tellmelast(12)
    Me.Baccno = Forms![Bundle_m]![Combo0]
End Sub

Private Sub Fsheet_AfterUpdate()
    Me.Lsheet = Me.Fsheet + Me.Sheets - 1
End Sub

Private Sub Fsheet_BeforeUpdate(Cancel As Integer)
    mystr = "Select * from Bundle Where Baccno=" & "'" & CStr(Forms![Bundle_m]![Combo0]) & "'And Fsheet=" & Me.Fsheet
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        Else
            MsgBox "شماره دسته چك تكراري است"
            Cancel = True
            Exit Sub
        End If
    End With

End Sub
