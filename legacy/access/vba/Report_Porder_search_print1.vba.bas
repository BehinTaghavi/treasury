Attribute VB_Name = "Report_Porder_search_print1"
Attribute VB_Base = "0{CAC2AB25-1757-4D63-94F0-192C19E873A2}"
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
    
        'If Forms!Sal!Isadd = True Then
            originstr = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)<3))"
        'Else
        '    originstr = "SELECT Check.*, Switch([Checktype]=0,'چك نقدي',[Checktype]=1,'چك انتظامي',[Checktype]=2,'چك مدت دار') AS PT, IIf([Payit]=True,1,0) AS V, IIf([paied]=True,1,0) AS P FROM [Check] WHERE (((Check.Dp)=2) And ((Check.Sal)=Forms!sal!Tsal) And ((Check.Empid)=Forms!sal!tempid)) "
        'End If
        
'"SELECT Check.*, Switch([Checktype]=0,"چك نقدي",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار") AS PT, IIf([Payit]=True,1,0) AS V, IIf([paied]=True,1,0) AS P FROM [Check] WHERE (((IIf([paied]=True,1,0))=1) AND ((Check.Dp)=2) AND ((Check.Sal)=[Forms]![sal]![Tsal]) AND ((Check.Empid)=[Forms]![sal]![tempid]));"
    originstr = originstr & Repcondition(0)

    Me.RecordSource = originstr & ";"
    Me.OrderBy = "" & Reporder(0)

    Forms![SHART_M].Visible = False
    
Err1:
    If Err.number = 2450 Then
      MsgBox "كاربر و يا سال مالي نامشخص است"
   
      DoCmd.OpenForm "sal"
    End If


End Sub
