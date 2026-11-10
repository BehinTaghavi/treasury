Attribute VB_Name = "Report_D_Check_Jaam_el"
Attribute VB_Base = "0{46428A34-944A-4862-A696-E659A709D76A}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim S As String, mainstr As String, typestr As String, datestr As String, Baccstr As String, kaz As String
Dim datemstr As String, Ordstr As String, perstr As String, babstr As String, perbab As String
Dim rst As Recordset, Myrow As Byte, Hasor As String


Private Sub Report_Activate()
    Me.Textco = Tellconame
    
End Sub

Private Sub Report_Close()
    Forms![Jaam_el].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Jaam_el].Visible = False
    
    'mainstr = "SELECT Check.*,Trim(Checktypen) as Itname FROM [Check] WHERE Dp=1 And (Checktype=0 or Checktype=2) And "
    mainstr = "SELECT Check.*, Trim([Checktypen]) AS Itname FROM [Check] WHERE (((Check.D4) Between [Forms]![Jaam_el]![D1] And [Forms]![Jaam_el]![D2]) AND ((Check.Dp)=1) AND ((Check.Checktype) In (0,2)) AND ((Check.Stcode)=13) AND ((Check.Comeel)="

    If Form_Jaam_el.Frame1 = 1 Then
        mainstr = mainstr & "0));"
    Else
        mainstr = mainstr & "1));"
    End If
    Me.RecordSource = mainstr

End Sub
