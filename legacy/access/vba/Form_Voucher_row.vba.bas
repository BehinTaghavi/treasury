Attribute VB_Name = "Form_Voucher_row"
Attribute VB_Base = "0{28D48D7E-D18D-43E9-A679-5FDA6B074252}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub ComboA_AfterUpdate()
    If Me.ComboA.Column(3) = -1 Then
        Me.ComboB.Enabled = True
    Else
        Me.ComboB = Null
        Me.ComboB.Enabled = False
    End If
    If Me.ComboA.Column(3) = -1 And IsNull(ComboB) Then
        MsgBox "سطر سند فاقد حساب تفصيل است"
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

Private Sub Form_Current()
    Me.ComboB.Enabled = Nz(Me.ComboA.Column(3), 0)
    'Me.ComboB.Locked = Not Nz(Me.ComboA.Column(3), 0)
'    If Me.ComboA.Column(3) = -1 Then
'        Me.ComboB.Enabled = True
'    Else
'        Me.ComboB.Enabled = False
'    End If
End Sub

Private Sub rowdesc_DblClick(Cancel As Integer)
    Dim stDocName As String
    Dim stLinkCriteria As String
    Select Case Forms![Voucher]![Vgroup]
        Case 21, 25, 22
            stDocName = "Check_Print_single"
            stLinkCriteria = "[Checkid]=" & Me![Checkid]
        Case 16
            stDocName = "D_variz"
            stLinkCriteria = "[Sid]=" & Me![Checkid]
        Case 17
            stDocName = "D_naghd"
            stLinkCriteria = "[Sid]=" & Me![Checkid]
        Case 26
            stDocName = "P_Bardasht"
            stLinkCriteria = "[Sid]=" & Me![Checkid]
        Case 27
            stDocName = "P_naghd"
            stLinkCriteria = "[Sid]=" & Me![Checkid]
        Case 11, 12, 13, 18, 19
            stDocName = "D_Check"
            stLinkCriteria = "[Checkid]=" & Me![Checkid]
    End Select
    
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub
