Attribute VB_Name = "Report_Check_s_Finder"
Attribute VB_Base = "0{5B3B903A-9C45-4C1D-8ADD-AA18BD51DD7C}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database


Private Sub Report_Close()
    Form_Check_s_Finder.Visible = True

End Sub

Private Sub Report_Open(Cancel As Integer)
    Form_Check_s_Finder.Visible = False
    Dim A As String
    S = "SELECT Check.*, Switch([Checktype]=0,'چك نقدي',[Checktype]=1,'چك انتظامي',[Checktype]=2,'چك مدت دار',[Checktype]=9,'پرداخت نقدي') AS PT FROM [Check] WHERE Check.Dp=2 AND Check.Payit=True AND Check.Paied=False AND Check.Checktype In (0,1,2) And Rfpdate Between '" & Form_Check_s_Finder.Date1 & "' And '" & Form_Check_s_Finder.Date2 & "'"
    If IsNull(Form_Check_s_Finder.Mat1) Then
        S = S
    Else
        S = S & " And Maturity Between '" & Form_Check_s_Finder.Mat1 & "' And '" & Form_Check_s_Finder.Mat2 & "'"
    End If
    
    
    If IsNull(Form_Check_s_Finder.Myperson) Then
        S = S
    Else
        A = BSPACE(Form_Check_s_Finder.Myperson)
        S = S & " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(A) & "*'"
    End If

    Me.RecordSource = S
End Sub
