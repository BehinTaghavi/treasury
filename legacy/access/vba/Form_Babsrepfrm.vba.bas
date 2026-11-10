Attribute VB_Name = "Form_Babsrepfrm"
Attribute VB_Base = "0{1A5B630D-5D37-45A5-BCB0-A1A44AAC15B5}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim myd1 As String, myd2 As String, myform As String
Dim stDocName As String, rst As Recordset
Private Sub Command0_Click()
    
    Select Case Me.Frame1
        Case 1, 2
         stDocName = "Babrepnew1"
        Case Else
         stDocName = "Babrepnew2"
    End Select
  '  stDocName = "Babrep" & Me.Frame1
    DoCmd.OpenReport stDocName, acPreview

End Sub
Private Sub Command17_Click()
On Error GoTo Err_Command17_Click


    DoCmd.Close

Exit_Command17_Click:
    Exit Sub

Err_Command17_Click:
    MsgBox Err.Description
    Resume Exit_Command17_Click
    
End Sub

Private Sub Command30_Click()
    stDocName = "Babrepnewbalance"
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Command31_Click()
    Me.T0 = 1
    stDocName = "Babrepnew2"
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Command32_Click()
    Me.T0 = 2
    stDocName = "Babrepnew2"
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Command40_Click()
    DoCmd.OpenReport "Babrepnew1curs", acPreview

End Sub

Private Sub Command42_Click()
    DoCmd.OpenReport "Babrepnew1month", acPreview
End Sub

Private Sub Form_Close()
    Forms!Mainrep.Visible = True
End Sub

Private Sub Form_Open(Cancel As Integer)
    Dim Rstmak As Recordset
    Forms!Mainrep.Visible = False
    'D10 = DLookup("Vfrom", "Globals")
    'D20 = [Forms]![Mainrep]![DD1] - 1
    'S = "SELECT Sum([bed])-Sum([bes]) AS Bal FROM Subs WHERE (Subs.Sdate Between '" & CStr(D10) & "' And '" & CStr(D20) & "')AND (Subs.Curid='0' Or Subs.Curid='ريال');"
   ' S = "Select * from Babatrepbase"
   Me.T1 = Nz(DLookup("Bkol", "Babatrepbase"), 0)
   
'    Set Rstmak = CurrentDb.OpenRecordset("Babatrepbase", dbOpenDynaset, dbSeeChanges)
'    With Rstmak
'
'        If .RecordCount = 0 Then
'            Me.T1 = 0
'        Else
'            Me.T1 = Nz(!Bkol, 0)
'        End If
'    .Close
'    End With
    Me.Recalc
End Sub
 
Private Sub Frame1_AfterUpdate()
    Me.T0 = 0
End Sub
