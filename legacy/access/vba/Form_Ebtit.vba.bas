Attribute VB_Name = "Form_Ebtit"
Attribute VB_Base = "0{7E0E61BD-F0D6-45E8-B966-1DE9913E528D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub D2_BeforeUpdate(Cancel As Integer)
    If Me.D2 <= Nochangedate Then
        MsgBox "تاريخ ابطال در محدوده اسناد قطعي شده قرار دارد"
        Cancel = True
    End If
End Sub

Private Sub EbtorKharj_AfterUpdate()
    If Me.EbtorKharj = True Then
        Me.D2 = CurrentDateFarsi(Date)
        Me.Calcit = False
        Me.D5 = Null
        Me.D2.Enabled = True
        Me.D2.SetFocus
        Me.Stcode = 26
        Me.Status = "ابطال شده"
    Else
        Me.D2 = Null
        Me.D2.Enabled = False
        Me.Stcode = 21
        Me.Status = "چك صادره"
    End If
End Sub

Private Sub Form_Close()
    Form_Check_Print_single.Recalc
End Sub

Private Sub Form_Current()
    If Me.Checktype = 1 Or Me.Checktype = 2 Then
        Me.Ctocash.Enabled = True
        Me.D3.Enabled = True
    Else
        Me.Ctocash.Enabled = False
        Me.D3.Enabled = False
    End If
End Sub
