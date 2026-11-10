Attribute VB_Name = "Report_P_search_print"
Attribute VB_Base = "0{1040EA90-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
    Dim dbs As Database
    Dim rst As Recordset
    Dim originstr As String

Private Sub Report_Activate()
    Me.Coname = Tellconame
    Select Case Forms!Sal!Ttype
        Case 0
            Me.Repname = "گزارش درخواست وجه هاي صادره"
        Case 1
            Me.Repname = "گزارش چكهاي دريافتي"
        Case 2
            Me.Repname = "گزارش چكهاي صادره"
    End Select
End Sub

Private Sub Report_Close()
    Forms![SHART_M].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "ركوردي جهت نمايش وجود ندارد"
    Cancel = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    On Error GoTo Err1
    
    Select Case Forms!Sal!Ttype
        Case 0
            originstr = "SELECT Check.*, IIf([Payit]=True,1,0) AS V, IIf([paied]=True,1,0) AS P FROM [Check] WHERE (((Check.Dp)=2)) "
        Case 1
            originstr = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1)) "
        Case 2
            originstr = "SELECT Check.*, IIf([Payit]=True,1,0) AS V, IIf([paied]=True,1,0) AS P FROM [Check] WHERE (((Check.Dp)=2) And ((Check.Paied)=true)) "
    End Select
        
    originstr = originstr & Repcondition(Forms!Sal!Ttype)

    Me.RecordSource = originstr & ";"
    Me.OrderBy = "" & Reporder(Forms!Sal!Ttype)

    Forms![SHART_M].Visible = False
    
Err1:
    If Err.number = 2450 Then
      MsgBox "كاربر و يا سال مالي نامشخص است"
      DoCmd.OpenForm "sal"
    End If

End Sub

