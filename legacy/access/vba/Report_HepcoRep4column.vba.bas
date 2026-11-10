Attribute VB_Name = "Report_HepcoRep4column"
Attribute VB_Base = "0{4C62C780-3481-45E5-9ADC-E1B3EAE32934}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Dim S As String, mainstr As String, typestr As String, datestr As String, Nakstr As String
Dim datemstr As String, Ordstr As String, perstr As String, babstr As String, perbab As String
Private Sub Report_Close()
    Forms![HepcoForm].Visible = True
End Sub
Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)

    Forms![HepcoForm].Visible = False
    
   Select Case Forms!HepcoForm!Frameordfield
        Case 1
            Ordstr = "Maturity"
        Case 2
            Ordstr = "Rfpdate"
        Case 3
            Ordstr = "D3"
        Case 4
            Ordstr = "D4"
        Case 5
            Ordstr = "Person"
        Case 6
            Ordstr = "Vagno"
    End Select
    
    If Forms!HepcoForm!Frameord = 1 Then
       Ordstr = Ordstr
    Else
       Ordstr = Ordstr & " Desc"
    End If
    

    If IsNull(Forms!HepcoForm!Person) Then
        perstr = ""
    Else
        perstr = BSPACE(Forms!HepcoForm!Person)
        perstr = " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
    End If
    S = "SELECT Check.* ,Trim(Check.checktypen) as checkname , -DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) As Nak FROM Check WHERE ((Check.Dp)=1) And Checktype In (0,2,3,4) And Stcode Not In (14,15) " & perstr & " And Subtype=1 "
    
    Me.RecordSource = S
    Me.OrderBy = Ordstr

End Sub

