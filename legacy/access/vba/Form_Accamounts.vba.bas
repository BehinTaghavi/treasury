Attribute VB_Name = "Form_Accamounts"
Attribute VB_Base = "0{1040E74F-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mystr As String
Dim rst As Recordset
Private Sub Bed_AfterUpdate()
    If IsNull(Me.bed) Then
        Me.bed = 0
    End If
End Sub

Private Sub ComboSal_AfterUpdate()
    Me.Sdate = Me.ComboSal & "0101"
End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Stime = Time()
    Me.Sdate = Nz(DLookup("Vfrom", "Globals"), 0)
    Me.bed = 0
    Me.bes = 0
    Me.Babat = "موجودي اول دوره"
    Me.Scode = 10
    Me.Transtype = "موجودي اول دوره"
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.ComboSal) Then
        MsgBox "سال نامشخص است"
        Cancel = True
        Exit Sub
    End If
    
    If IsNull(Me.Baccno) Then
        MsgBox "شماره حساب نامشخص است"
        Cancel = True
        Exit Sub
    End If
'    mystr = "Select * from Subs Where Scode=10 And Baccno=" & "'" & CStr(Me.Combo8) & "' and Sal=" & Me.ComboSal
'    Set rst = CurrentDb.OpenRecordset(mystr)
'    With rst
'        If .RecordCount = 0 Then
'            Exit Sub
'        Else
'            MsgBox "اطلاعات تكراري است"
'            Cancel = True
'        End If
'    End With
End Sub


