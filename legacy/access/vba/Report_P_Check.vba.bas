Attribute VB_Name = "Report_P_Check"
Attribute VB_Base = "0{1040EA89-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim S As String, mainstr As String, typestr As String, datestr As String
Dim datemstr As String, Ordstr As String, perstr As String
Dim Baccstr As String, babstr As String, perbab As String

Private Sub Report_Activate()
    Me.Textco = Tellconame
    Select Case Forms!Date_pcheck!Frame1
        Case 10
            Repname = "همه چكهاواسنادپرداختني"
        Case 0
            Repname = "چكهاي نقدي"
        Case 1
            Repname = "چكهاي صادره انتظامي"
        Case 2
            Repname = "چكهاي صادره مدت دار"
        Case 3
            Repname = "اقساط پرداختني"
        Case 5
            Repname = "تعهدات پرداختني /انتظامي تبديل شده به نقد"
        Case 20
            Repname = "چكهاي صادره نزد صندوق"
        Case 21
            Repname = "چكهاي صادره تحويل نشده"
        Case 22
            Repname = "چكهاي تحويل شده"
        Case 26
            Repname = "چكهاي ابطال شده"
    End Select

    Select Case Forms!Date_pcheck!Frame2
        Case 0
            Dfilter = "بدون محدوده تاريخي"
        Case 1
            Dfilter = "تاريخ صدور"
        Case 2
            Dfilter = "سررسيد"
        Case 3
            Dfilter = "تاريخ تحويل"
        Case 4
            Dfilter = "تاريخ ابطال"
        Case 5
            Dfilter = "تاريخ تبديل به نقد"
    End Select
End Sub

Private Sub Report_Close()
    Forms![Date_pcheck].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)

    Forms![Date_pcheck].Visible = False
    
    'mainstr = "SELECT Check.* FROM [Check] Where Dp=2 And Paied=true And "
    mainstr = " SELECT Check.*, Bankacc.Bank FROM [Check] LEFT JOIN Bankacc ON Check.Baccno = Bankacc.Baccno WHERE Check.Dp=2 AND Check.Paied=True And "
    Select Case Forms!Date_pcheck!Frame1
        Case 10
            typestr = " 1=1 and "
        Case 0, 1, 2, 3
            typestr = "Checktype=" & Forms!Date_pcheck!Frame1 & " And "
        Case 5
            typestr = "Ctocash= -1 and "
        Case 20, 21, 22, 26
            typestr = "Checktype in (0,2) and Stcode=" & Forms!Date_pcheck!Frame1 & " And Delivervag=0 and "
    End Select
    

    Select Case Forms!Date_pcheck!Frame2
        Case 0
            datestr = "1=1"
        Case 1
            datestr = "D1"
        Case 2
            datestr = "Maturity"
        Case 3, 4
            datestr = "D2"
        Case 5
            datestr = "D3"
    End Select
    
    If Forms!Date_pcheck!Frame2 = 0 Then
        datemstr = datestr
    Else
        datemstr = datestr & " Between Forms!Date_pcheck![D1] And Forms!Date_pcheck![D2] "
    End If
    
   Select Case Forms!Date_pcheck!Frame3
        Case 1
            Ordstr = ""
        Case 2
            Ordstr = " DESC"
    End Select
    
    If Forms!Date_pcheck!ChBacc = True Then
        If IsNull(Forms!Date_pcheck!Bacc) Then
            Baccstr = ""
        Else
            Baccstr = BSPACE(Forms!Date_pcheck!Bacc)
            Baccstr = " and IIf(IsNull([CHECK.Baccno]), Null, BSPACE([CHECK.Baccno])) Like '*" & Trim(Baccstr) & "*'"
        End If
    End If
    
    If Forms!Date_pcheck!ChPerson = True Then
        If IsNull(Forms!Date_pcheck!Person) Then
            perstr = ""
        Else
            perstr = BSPACE(Forms!Date_pcheck!Person)
            perstr = " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
        End If
    End If
    
    If Forms!Date_pcheck!ChBabat = True Then
        If IsNull(Forms!Date_pcheck!Babat) Then
            babstr = ""
        Else
            babstr = BSPACE(Forms!Date_pcheck!Babat)
            babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
        End If
    End If
    perbab = Baccstr & perstr & babstr
       
    If Forms!Date_pcheck!Frame2 = 0 Then
       Ordstr = "Maturity"
    Else
       Ordstr = datestr & Ordstr
    End If
    
    S = mainstr & typestr & datemstr & perbab
    Me.OrderBy = Ordstr
    Me.RecordSource = S

End Sub
