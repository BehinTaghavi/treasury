Attribute VB_Name = "Report_Check_p_longsrep"
Attribute VB_Base = "0{DCDB38F4-B01B-411A-A020-8909BC09BA2C}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Report_Open(Cancel As Integer)
    S = "SELECT Check.*, -DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) AS Nak  FROM [Check] WHERE (((Check.Checktype)<>0 And (Check.Checktype)<>1) And ((Check.Ctocash)=False) And ((Check.Dp)=2) And ((Check.Paied)=True) And ((Check.EbtorKharj)=False)"
    If Form_Check_longtocash_m.Frame9 = 1 Then
        S = S & ");"
    Else
        S = S & " And ((Check.Maturity) Between Forms!Check_longtocash_m!DD1 And Forms!Check_longtocash_m!DD2)) ; "
    End If
    Me.RecordSource = S
End Sub
