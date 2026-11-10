Attribute VB_Name = "Form_Globals"
Attribute VB_Base = "0{E7E68906-8E1B-42EE-9AF9-3431224DB204}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Arziinfarsi_AfterUpdate()
    Forms!Sal.Arzifarsi = Me.Arziinfarsi
End Sub

Private Sub Command131_Click()
        S = "UPDATE [Check] SET [Check].Curid = 'ريال'WHERE (((Check.Curid) Is Null)) OR (((Check.Curid)='0'));"
        docommand S
        S = "UPDATE [Check] INNER JOIN Checktype ON (Check.Checktype = Checktype.Checktype) AND (Check.Dp = Checktype.Dp) SET [Check].Checktypen = [Checktype].[Checktypen];"
        docommand S
        S = "UPDATE [Check] LEFT JOIN Checkst ON Check.Stcode = Checkst.Stcode SET [Check].Status = [Checkst].[Stname];"
        docommand S
        MsgBox "Finished"
End Sub

Private Sub Command49_Click()
    On Error GoTo Err_Command49_Click

    Dim stDocName As String
    Dim stLinkCriteria As String
    Forms!Sal!Accortaf = 1
    Lockit = 1
    stDocName = "Sm"
    DoCmd.OpenForm stDocName, , , stLinkCriteria

Exit_Command49_Click:
    Exit Sub

Err_Command49_Click:
    MsgBox Err.Description
    Resume Exit_Command49_Click

End Sub

Private Sub Command71_Click()
On Error GoTo Err_Command71_Click


    DoCmd.Close

Exit_Command71_Click:
    Exit Sub

Err_Command71_Click:
    MsgBox Err.Description
    Resume Exit_Command71_Click
    
End Sub
Private Sub Command130_Click()
On Error GoTo Err_Command130_Click


    Screen.PreviousControl.SetFocus
    DoCmd.FindNext

Exit_Command130_Click:
    Exit Sub

Err_Command130_Click:
    MsgBox Err.Description
    Resume Exit_Command130_Click
    
End Sub
