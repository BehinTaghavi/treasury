Attribute VB_Name = "Report_REP_VOUCHER"
Attribute VB_Base = "0{A2BE3E2D-E7AB-48C2-AE12-A61314C0307F}"
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
