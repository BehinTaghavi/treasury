Attribute VB_Name = "Report_Sarresid_P_alarm"
Attribute VB_Base = "0{1040EAB7-F05E-11D7-BC94-8154B80D7F75}"
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
    Select Case Forms!Date_sarresid_alarm!Frame1
        Case 0
            Repname = "همه دريافتني ها"
            Tz = "صادركننده"
            Tvag = "حساب واگذاري"
            Tvagd = "تاريخ واگذاري"
        Case 1
            Repname = " چكهاي دريافتني "
            Tz = "صادركننده"
            Tvag = "حساب واگذاري"
            Tvagd = "تاريخ واگذاري"
        Case 2
            Repname = " سفته هاي دريافتني "
            Tz = "صادركننده"
            Tvag = "حساب واگذاري"
            Tvagd = "تاريخ واگذاري"
        Case 3
            Repname = "همه پرداختني"
            Tz = "ذينفع"
            Tvag = ""
        Case 4
            Repname = "چكهاي پرداختني"
            Tz = "ذينفع"
            Tvag = ""
        Case 5
            Repname = "اقساط پرذاختني"
            Tz = "ذينفع"
            Tvag = ""
        Case 6
            Repname = "سفته هاي پرداختني"
            Tz = "ذينفع"
            Tvag = ""
    End Select

    
End Sub

Private Sub Report_Close()
    Forms![Date_sarresid_alarm].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Date_sarresid_alarm].Visible = False
    
    Select Case Forms!Date_sarresid_alarm!Frame1
        Case 0
            mainstr = "SELECT Check.*,  [Checktypen] & ' - ' & [Status] AS S FROM [Check] WHERE Dp=1 And (Checktype<>1 And Checktype<>2)And(Stcode=11 or Stcode=12) And "
        Case 1
            mainstr = "SELECT Check.*, [Checktypen] & ' - ' & [Status] AS S FROM [Check] WHERE Dp=1 And (Checktype=0 Or Checktype=2)And(Stcode=11 or Stcode=12) And "
        Case 2
            mainstr = "SELECT Check.*, [Checktypen] & ' - ' & [Status] AS S FROM [Check] WHERE Dp=1 And (Checktype=3 Or Checktype=4 Or Checktype=5)And(Stcode=11 or Stcode=12) And "
        Case 3
            mainstr = "SELECT Check.*, [Checktypen] & ' - ' & [Status] AS S FROM [Check] WHERE Dp=2 And Paied = true And Ebtorkharj=false And Checktype in(2,5) And "
        Case 4
            mainstr = "SELECT Check.*,  [Checktypen] & ' - ' & [Status] AS S FROM [Check] WHERE Dp=2 And Paied = true And Ebtorkharj=false And Checktype =2 And "
        Case 5
            mainstr = "SELECT Check.*, [Checktypen] & ' - ' & [Status] AS S FROM [Check] WHERE Dp=2 And Paied = true And Ebtorkharj=false And Checktype =3 And "
        Case 6
            mainstr = "SELECT Check.*, [Checktypen] & ' - ' & [Status] AS S FROM [Check] WHERE Dp=2 And Paied = true And Ebtorkharj=false And Checktype =4 And "
    End Select

    datemstr = " Maturity Between Forms!Date_sarresid_alarm![D0] And Forms!Date_sarresid_alarm![D2] "
    
    If Forms!Date_sarresid_alarm!ChPerson = True Then
        If IsNull(Forms!Date_sarresid_alarm!Person) Then
            perstr = ""
        Else
            perstr = BSPACE(Forms!Date_sarresid_alarm!Person)
            perstr = " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
        End If
    End If
    
    If Forms!Date_sarresid_alarm!ChBabat = True Then
        If IsNull(Forms!Date_sarresid_alarm!Babat) Then
            babstr = ""
        Else
            babstr = BSPACE(Forms!Date_sarresid_alarm!Babat)
            babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
        End If
    End If
    perbab = perstr & babstr
    Me.RecordSource = mainstr & typestr & datemstr & perbab
End Sub
