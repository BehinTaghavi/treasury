Attribute VB_Name = "Report_HepcoRep"
Attribute VB_Base = "0{CECDE4B6-0F84-4241-B3E9-E5A98798C7D5}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim S As String, mainstr As String, typestr As String, datestr As String, Nakstr As String
Dim datemstr As String, Ordstr As String, perstr As String, babstr As String, perbab As String

Private Sub Report_Activate()
    Me.Textco = Tellconame
End Sub

Private Sub Report_Close()
    Forms![HepcoForm].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)

    Forms![HepcoForm].Visible = False
    mainstr = "SELECT Check.* ,Trim(Check.checktypen) as checkname , -DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) As Nak FROM Check WHERE ((Check.Dp)=1) And "
    Nakstr = "-DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False)"
    Select Case Forms!HepcoForm!Frametype
        Case 0
            typestr = typestr & "1=1 And "
        Case 1
            typestr = "Checktype In (0,2,3,4) And "
        Case 2
            typestr = "Checktype In (1,6) And "
            
        Case 10
            typestr = "Checktype In (0,1,2) And "
        Case 11
            typestr = "Checktype In (0,2) And "
        Case 12
            typestr = "Checktype In (1) And "
        Case 13
            typestr = "Checktype In (2) And "
            
        Case 20
            typestr = "Checktype In (4,6) And "
        Case 21
            typestr = "Checktype In (4) And "
        Case 22
            typestr = "Checktype In (6) And "
            
    End Select
    
    Select Case Forms!HepcoForm!Framestatus
        Case 10
            typestr = typestr & " Stcode not in (14,15) and "
        Case 11 To 15
            typestr = typestr & "  Stcode=" & Forms!HepcoForm!Framestatus & " And "
            
        Case 16
            typestr = Nakstr & "<=0 and Stcode=13 and "
            
        Case 17
            typestr = Nakstr & "<0 and Stcode in (11,12,14) and "
        Case 18
            typestr = Nakstr & "<0 and Stcode in (11) and "
        Case 19
            typestr = Nakstr & "<0 and Stcode in (12) and "
        Case 20
            typestr = Nakstr & "<0 and Stcode in (14) and "
    End Select
    
   Select Case Forms!HepcoForm!Framewhatpay
        Case 1
            mainstr = mainstr
        Case 2
            mainstr = mainstr & " Subtype=1 And "
        Case 3
            mainstr = mainstr & " Subtype=2 And "
        Case 4
            mainstr = mainstr & " Subtype=3 And "
    End Select
    
    
    Select Case Forms!HepcoForm!Framedate
        Case 0
            datestr = " 1=1 "
        Case 1
            datestr = "Maturity"
        Case 2
            datestr = "Rfpdate"
        Case 3, 5
            datestr = "D3"
        Case 4
            datestr = "D4"
    End Select
    
    If Forms!HepcoForm!Framedate = 0 Then
        datemstr = datestr
    Else
        datemstr = datestr & " Between Forms!HepcoForm![D1] And Forms!HepcoForm![D2] "
    End If
    
    If IsNull(Forms!HepcoForm!Vagno) Then
        perstr = ""
    Else
        perstr = " and Vagno='" & Forms!HepcoForm!Vagno & "'"
    End If
    
    If IsNull(Forms!HepcoForm!Person) Then
        perstr = ""
    Else
        perstr = BSPACE(Forms!HepcoForm!Person)
        perstr = " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
    End If

    If IsNull(Forms!HepcoForm!Babat) Then
        babstr = ""
    Else
        babstr = BSPACE(Forms!HepcoForm!Babat)
        babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
    End If
    perbab = perstr & babstr


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
    S = mainstr & typestr & datemstr & perbab
'   Select Case Forms!HepcoForm!Framewhatpay
'        Case 1
'            S = mainstr & typestr & datemstr & perbab
'        Case 2
'            S = "SELECT Check.* ,Trim(Check.checktypen) as checkname , -DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) As Nak FROM Check WHERE ((Check.Dp)=1) And Checktype In (0,2,3,4)  " & perstr & " And Subtype=1 "
'        Case 3
'            S = "SELECT Check.* ,Trim(Check.checktypen) as checkname , -DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) As Nak FROM Check WHERE ((Check.Dp)=1) And Checktype In (0,2,3,4)  " & perstr & " And Subtype=2 "
'        Case 4
'            S = "SELECT Check.* ,Trim(Check.checktypen) as checkname , -DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) As Nak FROM Check WHERE ((Check.Dp)=1) And Checktype In (0,2,3,4)  " & perstr & " And Subtype=3 "
'    End Select
    
    Me.RecordSource = S
    Me.OrderBy = Ordstr

End Sub
