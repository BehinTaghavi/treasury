Attribute VB_Name = "Report_D_Check_audit"
Attribute VB_Base = "0{00308EB1-DDF2-4B50-8BBF-F882C7513BB6}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim babstr As String, perstr As String
Private Sub Report_Close()
    Form_Auditform.Visible = True
End Sub
Private Sub Report_Open(Cancel As Integer)
    Form_Auditform.Visible = False
    S = "SELECT Check.*, Trim([Checktypen]) AS Itname FROM [Check] WHERE Check.Dp=1 AND Check.Checktype In (0,2,4) AND  " ' ((Check.D3) Is Null Or (Check.D3)>[Forms]![AuditForm]![Indate]));"
    Select Case Form_Auditform.Frame2
        Case 1
            S = S & "Check.Rfpdate "
        Case 2
            S = S & "Check.Maturity "
    End Select
    S = S & " Between [Forms]![AuditForm]![Fromdate] And [Forms]![AuditForm]![Tadate] AND("
    Select Case Form_Auditform.Frame0
        Case 1
            S = S & " Check.D3 Is Null Or Check.D3>[Forms]![AuditForm]![Indate]) "
        Case 2
            S = S & " Check.D3<=[Forms]![AuditForm]![Indate]) And Stcode Not in (15,16) And (Check.D4 Is Null Or Check.D4>[Forms]![AuditForm]![Indate]) "
    End Select
    
    If IsNull(Form_Auditform.Person) Or Form_Auditform.Person = "" Then
        perstr = ""
    Else
        perstr = BSPACE(Form_Auditform.Person)
        perstr = " And IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
    End If

    If IsNull(Form_Auditform.Babat) Or Form_Auditform.Babat = "" Then
        babstr = ""
    Else
        babstr = BSPACE(Form_Auditform.Babat)
        babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
    End If
    S = S & perstr & babstr & ";"
    Me.RecordSource = S
End Sub
