Attribute VB_Name = "Form_Sperson"
Attribute VB_Base = "0{6F82E316-D94B-4F0E-B297-F60B29586B59}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim Fldname As String, fldsort As String
Dim A As String, S As String

Private Sub Command21_Click()
    DoCmd.Close
End Sub

Private Sub Form_Open(Cancel As Integer)
    If Forms!Sal!Accortaf = 1 Then
        Me.Frame1 = 1
    Else
        Me.Frame1 = 2
    End If
    Me.Text0.SetFocus
    Makesource
End Sub

Private Sub Frame0_AfterUpdate()
     Makesource
End Sub
Private Sub Frame1_AfterUpdate()
     Makesource
End Sub
Private Sub Frame2_AfterUpdate()
     Makesource
End Sub

Private Sub List1_DblClick(Cancel As Integer)
     If Isfinal = True Then Exit Sub
     Pubcontrol = Me.List1
     DoCmd.Close acForm, "SPerson"
End Sub

Private Sub Text0_Change()
    Makesource
End Sub

Private Sub Makesource()

    Select Case Me.Frame1
        Case 1
            S = "SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null)) "
            Fldname = "Person"
        Case 2
            S = "SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null)) " 'ORDER BY Check.Babat"
            Fldname = "Babat"
    End Select
    Me.Text0.SetFocus
    If IsNull(Me.Text0.Text) Or Me.Text0.Text = "" Then
        S = S & "ORDER BY " & Fldname
    Else
        A = BSPACE([Text0].Text)
        S = S & " And (((BSPACE(" & CStr(Fldname) & ")) Like '*" & Trim(A) & "*'))" & "ORDER BY " & Fldname
    End If
    
    Select Case Me.Frame2
        Case 1
            S = S & " ;"
        Case 2
            S = S & " Desc ;"
    End Select
    Me.List1.RowSource = S
End Sub
