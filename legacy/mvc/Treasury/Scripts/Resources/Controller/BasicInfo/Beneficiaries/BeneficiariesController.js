app.controller('BeneficiariesController', function ($scope, $sce, $http, BeneficiariesSharedProperties) {
    $scope.ucno = "";
    $scope.ucname = "";
    $scope.getBeneficiaries = function () {
        $http.get("/BasicInfo/Beneficiaries/Show").then(function (response) {
            $scope.model = response.data;
            console.log(response.data);
            $scope.BasicBanksLs = response.data.BasicBanksLs;
            $scope.BankAccountList = response.data.BankAccountList;
        });
    }
    $scope.getBeneficiaries();
    $scope.changeUniqueAccount = function (uc) {
        BeneficiariesSharedProperties.SetActiveUC(uc);
        $http.get("/BasicInfo/Beneficiaries/getBeneficiariesUC").then(function (response) {
            $scope.thisCanBeusedInsideNgBindHtml = $sce.trustAsHtml(response.data)
            $(".modal.md.ang").find(".modal-title").html("انتخاب تفصیل");
            $(".modal.md.ang").modal('show');
            $("#UniqueAccountType, #Code, #Name").val('');
        });
    }
    $scope.addBankAccount = function () {
        var BankAccount = {
            basicBank_ID: null,
            Bank_BranchName: "",
            Bank_AccountNumber: "",
            Bank_ShebaNo:""
        }
        $scope.model.BankAccountList.push(BankAccount)
    }
}).service('BeneficiariesSharedProperties', function () {
    var activeUC = { Code: 0, Name: "" };
    return {
        GetActiveUC: function () {
            return activeUC;
        },
        SetActiveUC: function (uc) {
            activeUC = uc;
        }
    };
});