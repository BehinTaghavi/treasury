Attribute VB_Name = "Form_Rights_m"
Attribute VB_Base = "0{5935E40E-1728-4705-A4B3-9CF0BF22C1FE}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim S As String, myapp As Byte

Private Sub Command19_Click()
    DoCmd.OpenForm "Emps"
End Sub

Private Sub Empid_AfterUpdate()
    Me.Recalc
    S = "INSERT INTO Rights ( Menuid, Mname,Ord,App, C1n, C2n, C3n, C4n, C5n, C6n, C7n, C8n, C9n, C10n, C11n, C12n, Empid ) SELECT Ms.Menuid, Ms.Mname,Ms.Ord, Ms.App,Ms.C1n, Ms.C2n, Ms.C3n, Ms.C4n, Ms.C5n, Ms.C6n, Ms.C7n, Ms.C8n, Ms.C9n, Ms.C10n, Ms.C11n, Ms.C12n," & Me.Empid & " FROM Ms where App= " & Me.Frame1 & " Order by Ord;"
    DoCmd.SetWarnings False
        DoCmd.RunSQL S
    DoCmd.SetWarnings True
    Me.Rights.Requery
End Sub
Private Sub Command6_Click()
On Error GoTo Err_Command6_Click


    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command6_Click:
    Exit Sub

Err_Command6_Click:
    MsgBox Err.Description
    Resume Exit_Command6_Click
    
End Sub
Private Sub Command7_Click()
On Error GoTo Err_Command7_Click


    DoCmd.Close

Exit_Command7_Click:
    Exit Sub

Err_Command7_Click:
    MsgBox Err.Description
    Resume Exit_Command7_Click
    
End Sub

Private Sub Empid_GotFocus()
    Me.Empid.Requery
    End Sub
