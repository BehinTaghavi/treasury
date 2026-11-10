Attribute VB_Name = "Report_Sarresid_P_byperson"
Attribute VB_Base = "0{1040EABE-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit

Dim mainstr As String, typestr As String, datestr As String
Dim datemstr As String, Ordstr As String, perstr As String, babstr As String, perbab As String
Dim Sors As String

Private Sub Report_Activate()
    Me.Textco = Tellconame
    Select Case Forms!Date_sarresid!Frame1
        Case 0
            Repname = "چكهاي دريافتني"
            Tvag = "حساب واگذاري"
            Tvagd = "تاريخ واگذاري"
            Tp = "صادركننده"
        Case 1
            Repname = "سفته هاي دريافتني"
            Tvag = "حساب واگذاري"
            Tvagd = "تاريخ واگذاري"
            Tp = "صادركننده"
        Case 2
            Repname = "چكهاي پرداختني"
            Tvag = ""
            Tp = "ذينفع"
        Case 3
            Repname = "اقساط پرذاختني"
            Tvag = ""
            Tp = "ذينفع"
        Case 4
            Repname = "سفته هاي پرداختني"
            Tvag = ""
            Tp = "ذينفع"
    End Select

    Select Case Forms!Date_sarresid!Frame2
        Case 0
            Dfilter = "بدون محدوده تاريخي"
        Case 1
            Dfilter = "سررسيد"
    End Select
    
End Sub

Private Sub Report_Close()
    Forms![Date_sarresid].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Date_sarresid].Visible = False
    
    Select Case Forms!Date_sarresid!Frame1
        Case 0
            mainstr = "SELECT Check.* , [Checktypen] & ' - ' & [Status] AS S FROM [Check] WHERE Dp=1 And Checktype=0 And(Stcode=11 or Stcode=12) And "
        Case 1
            mainstr = "SELECT Check.*, [Checktypen] & " - " & [Status] AS S FROM [Check] WHERE Dp=1 And Checktype=4 And(Stcode=11 or Stcode=12) And "
        Case 2
            mainstr = "SELECT Check.*, [Checktypen] & " - " & [Status] AS S FROM [Check] WHERE Dp=2 And Paied = true AND Ebtorkharj=false And (Checktype=0 Or Checktype=2) And "
        Case 3
            mainstr = "SELECT Check.*, [Checktypen] & " - " & [Status] AS S FROM [Check] WHERE Dp=2 And Paied = true AND Ebtorkharj=false And Checktype=3  And "
        Case 4
            mainstr = "SELECT Check.*, [Checktypen] & " - " & [Status] AS S FROM [Check] WHERE Dp=2 And Paied = true AND Ebtorkharj=false And Checktype=4 And "
    End Select

    Select Case Forms!Date_sarresid!Frame2
        Case 0
            datestr = "1=1"
        Case 1
            datestr = "Maturity"
    End Select
    
    If Forms!Date_sarresid!Frame2 = 0 Then
        datemstr = datestr
    Else
        datemstr = datestr & " Between Forms!DATE_Sarresid![D1] And Forms!DATE_Sarresid![D2] "
    End If
    
    If Forms!Date_sarresid!ChPerson = True Then
        If IsNull(Forms!Date_Date_sarresid!Person) Then
            perstr = ""
        Else
            perstr = BSPACE(Forms!Date_Date_sarresid!Person)
            perstr = " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
        End If
    End If
    
    If Forms!Date_sarresid!ChBabat = True Then
        If IsNull(Forms!Date_sarresid!Babat) Then
            babstr = ""
        Else
            babstr = BSPACE(Forms!Date_sarresid!Babat)
            babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
        End If
    End If
    perbab = perstr & babstr
    Me.RecordSource = mainstr & typestr & datemstr & perbab
End Sub
