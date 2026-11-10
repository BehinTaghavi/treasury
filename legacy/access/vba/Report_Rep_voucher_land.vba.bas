Attribute VB_Name = "Report_Rep_voucher_land"
Attribute VB_Base = "0{1040EA9E-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit

Private Sub Command16_Click()
On Error GoTo Err_Command16_Click


    DoCmd.Close

Exit_Command16_Click:
    Exit Sub

Err_Command16_Click:
    MsgBox Err.Description
    Resume Exit_Command16_Click
    
End Sub

Private Sub Report_Activate()
    Me.Textco = Tellconame
    Forms![Voucher].Visible = False
End Sub

Private Sub Report_Close()
    Forms![Voucher].Visible = True
End Sub
