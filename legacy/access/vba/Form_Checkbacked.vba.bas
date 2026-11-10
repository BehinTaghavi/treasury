Attribute VB_Name = "Form_Checkbacked"
Attribute VB_Base = "0{BB1E9859-4938-4629-B03A-95138EC9274E}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Actid_BeforeUpdate(Cancel As Integer)
'    Dim Mystcode As Byte
'
'    S = "Checkid=" & Form_Check_d_dates.Checkid
'    Select Case Me.Actid
'        Case 13, 14
'            S = S & " And Actid=12"
'            If Nz(DCount("Checkid", "Checkbacked", S), 0) = 0 Then
'                MsgBox "ترتيب عمليات نادرست است"
'                Cancel = True
'            End If
'        Case 15, 16
'            S = S
'            If Nz(DCount("Checkid", "Checkbacked", S), 0) <> 0 Then
'                MsgBox "ترتيب عمليات نادرست است"
'                Cancel = True
'            End If
'    End Select
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    Select Case Me.Actid
        Case 12, 13, 14
            If IsNull(Me.Baccno) Or Me.Baccno = "" Then
                MsgBox "حساب واگذاري نامشخص است"
                Cancel = True
                Me.Baccno.SetFocus
            End If
        Case 16
            If IsNull(Me.Acckharj) Or Me.Acckharj = "" Or IsNull(Me.TafKharj) Or Me.TafKharj = "" Then
                MsgBox "حساب معين يا تفصيل واگذاري نامشخص است"
                Cancel = True
                Me.Acckharj.SetFocus
            End If
    End Select
End Sub
