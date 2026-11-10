Attribute VB_Name = "Form_Naghd_pay_m"
Attribute VB_Base = "0{1040E8A8-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim S As String

Private Sub Command4_Click()
    DoCmd.Close
End Sub

Private Sub Form_Close()
'Checktypename
'
'    DoCmd.SetWarnings False
'
'    S = "UPDATE [Check] SET [Check].Signed = False, [Check].Calcit = False WHERE (((Check.Signed)=True) AND ((Check.Paied)=False) AND ((Check.Dp)=2) AND ((Check.Checktype) In (0,1,2,9)));"
'    DoCmd.RunSQL S
'
'    S = "UPDATE [Check] SET [Check].Ctocash = False, [Check].D3 = Null, [Check].Calcit = False, [Check].D1 = Null, [Check].D5 = Null, [Check].Signed = False, [Check].bundleid = 0, [Check].Checkno = Null WHERE (((Check.Paied)=False) AND ((Check.Dp)=2) AND ((Check.Checktype) In (0,1,2)));"
'    DoCmd.RunSQL S
'
'    DoCmd.SetWarnings True

End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
    Spfilter = 0
End Sub
