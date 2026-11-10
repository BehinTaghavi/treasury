Attribute VB_Name = "Form_Check_s_Finder"
Attribute VB_Base = "0{B61CE26D-D6D9-4D14-9089-81C6DEEBF8BC}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Makesource()
    Dim A As String
    S = "SELECT Check.*, Switch([Checktype]=0,'چك نقدي',[Checktype]=1,'چك انتظامي',[Checktype]=2,'چك مدت دار',[Checktype]=9,'پرداخت نقدي') AS PT FROM [Check] WHERE Check.Dp=2 AND Check.Payit=True AND Check.Paied=False AND Check.Checktype In (0,1,2) And Rfpdate Between '" & Me.Date1 & "' And '" & Me.Date2 & "'"
    If IsNull(Me.Mat1) Then
        S = S
    Else
        S = S & " And Maturity Between '" & Me.Mat1 & "' And '" & Me.Mat2 & "'"
    End If
    
    
    If IsNull(Me.Myperson) Then
        S = S
    Else
        A = BSPACE(Me.Myperson)
        S = S & " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(A) & "*'"
    End If

    S = S & " ORDER BY Check.maturity,Check.Rfpdate, Check.Rfpid; "
    Me.RecordSource = S
End Sub

Private Sub Date1_AfterUpdate()
    Makesource
End Sub

Private Sub Date2_AfterUpdate()
    Makesource
End Sub

Private Sub Form_Open(Cancel As Integer)
    Makesource
End Sub

Private Sub Mat1_AfterUpdate()
    Me.Mat2 = Me.Mat1
    Makesource
End Sub

Private Sub Mat2_AfterUpdate()
    Makesource
End Sub

Private Sub Myperson_AfterUpdate()
    Makesource
End Sub
Private Sub Command38_Click()
On Error GoTo Err_Command38_Click

    Dim stDocName As String

    stDocName = "Check_s_Finder"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command38_Click:
    Exit Sub

Err_Command38_Click:
    MsgBox Err.Description
    Resume Exit_Command38_Click
    
End Sub
