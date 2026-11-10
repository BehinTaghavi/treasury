Attribute VB_Name = "Report_D_Check"
Attribute VB_Base = "0{FD84396F-0CFE-485C-8524-41DD52CC5CF0}"
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

Private Sub Report_Activate()
    Me.Textco = Tellconame
    Select Case Forms!Date_dcheck!Frame1
        Case 10
            Repname = "همه چكهاي دريافتني"
        Case 11
            Repname = "چكهاي نزد صندوق"
        Case 12
            Repname = "چكهاي واگذار شده جهت وصول"
        Case 13
            Repname = "چكهاي وصول شده"
        Case 14
            Repname = "چكهاي برگشت شده"
        Case 15
            Repname = "چكهاي عودت شده"
        Case 16
            Repname = "چكهاي خرج شده"
        Case 19
            Repname = "چكهاي حقوقي شده"
    End Select

    Select Case Forms!Date_dcheck!Frame2
        Case 0
            Dfilter = "بدون محدوده تاريخي"
        Case 1
            Dfilter = "تاريخ دريافت"
        Case 2
            Dfilter = "سررسيد"
        Case 3, 5
            Dfilter = "تاريخ عودت/خرج چك"
        Case 4
            Dfilter = "تاريخ وصول/برگشت"
    End Select
    
End Sub

Private Sub Report_Close()
    Forms![Date_dcheck].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Date_dcheck].Visible = False
    
    mainstr = "SELECT Check.* FROM [Check] WHERE Dp=1 And (Checktype=0 or Checktype=2) And "
    
    Select Case Forms!Date_dcheck!Frame1
        Case 10
            typestr = " 1=1 and "
        Case Else
            'If Forms!Date_dcheck!Frame2 = 0 Then
            '    typestr = "Stcode=" & Forms!Date_dcheck!Frame1
            'Else
                typestr = "Stcode=" & Forms!Date_dcheck!Frame1 & " And "
            'End If
    End Select

    Select Case Forms!Date_dcheck!Frame2
        Case 0
            datestr = "1=1"
        Case 1
            datestr = "Rfpdate"
        Case 2
            datestr = "Maturity"
        Case 3, 5
            datestr = "D3"
        Case 4
            datestr = "D4"
    End Select
    
    If Forms!Date_dcheck!Frame2 = 0 Then
        datemstr = datestr
    Else
        datemstr = datestr & " Between Forms!DATE_DCHECK![D1] And Forms!DATE_DCHECK![D2] "
    End If
    
   Select Case Forms!Date_dcheck!Frame3
        Case 1
            Ordstr = ""
        Case 2
            Ordstr = " Desc "
    End Select
    
    If Forms!Date_dcheck!ChBacc = True Then
        If IsNull(Forms!Date_dcheck!Bacc) Then
            Baccstr = ""
        Else
            Baccstr = BSPACE(Forms!Date_dcheck!Bacc)
            Baccstr = " and IIf(IsNull([Vagno]), Null, BSPACE([Vagno])) Like '*" & Trim(Baccstr) & "*'"
        End If
    End If
     
    If Forms!Date_dcheck!ChPerson = True Then
        If IsNull(Forms!Date_dcheck!Person) Then
            perstr = ""
        Else
            perstr = BSPACE(Forms!Date_dcheck!Person)
            perstr = " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
            '"IIf(IsNull([BABAT]),Null,BSPACE([BABAT]))Like '*" & Trim(A) & "*'"
        End If
    End If
    
    If Forms!Date_dcheck!ChBabat = True Then
        If IsNull(Forms!Date_dcheck!Babat) Then
            babstr = ""
        Else
            babstr = BSPACE(Forms!Date_dcheck!Babat)
            babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
        End If
    End If
    
    If Forms!Date_dcheck!Chtype = True Then
        If IsNull(Forms!Date_dcheck!Markaz) Then
            kaz = ""
        Else
            kaz = " And Far=" & Forms!Date_dcheck!Markaz
        End If
    End If
    
    perbab = Baccstr & perstr & babstr & kaz
       
       
    If Forms!Date_dcheck!Frame2 = 0 Then
       Ordstr = "Maturity"
    Else
       Ordstr = datestr & Ordstr
    End If
    
    S = mainstr & typestr & datemstr & perbab
    Me.RecordSource = S
    Me.OrderBy = Ordstr

End Sub
