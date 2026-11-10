app.controller('directDebitController', function ($scope, $http, $sce, pettyCashSharedProperties) {
    $scope.ucno = "";
    $scope.ucname = "";
    $scope.hasDateError = false;
    $scope.isDisableBtn = false;
    $scope.getDirectDebit = function () {
        $http.get("/Payment/DirectDebit/Show").then(function (response) {
            $scope.model = response.data;
      
            $scope.behalfGroupList = response.data.BehalfGroupList;
            $scope.basicbankList = response.data.BasicBanksLs;
             $scope.bankList = response.data.BankLS;
            $scope.accountNumLs = response.data.AccountNumLs;
            $scope.updateSumPrice()
        });
    }
    $scope.DatePattern = /^[1-4]\d{3}\/((0[1-6]\/((3[0-1])|([1-2][0-9])|(0[1-9])))|((1[0-2]|(0[7-9]))\/(30|([1-2][0-9])|(0[1-9]))))$/;
    $scope.getDirectDebit();

    $scope.changeBehalfGroupevent = function (item) {
        $http.get("/Payment/DirectDebit/ChangeBehalfGroup?BehalfGroupId=" + item.behalfGroup.BehalfGroupID).then(function (response) {
            item.References = response.data.Refrences;
            item.BehalfList = response.data.BehalfList;
            item.account = response.data.account
        });
    }

    $scope.deleteItem = function (item) {
        var index = $scope.model.DirectDebit_TransactionLs.indexOf(item);
        if (index > -1) {
            $scope.model.DirectDebit_TransactionLs.splice(index, 1);
        }
        $scope.updateSumPrice();
    }
    $scope.copyItem = function (item) {
        var DirectDebit_TransactionDetail = {
            PayDate: item.PayDate,
            BehalfGroupID: item.BehalfGroupID,
            BehalfID: item.BehalfID,
            Description: item.Description,
            Account_no: item.Account_no,
            UniqueAccount1: { Name: item.UniqueAccount1.Name, Code: item.UniqueAccount1.Code },
            UniqueAccount2: { Name: item.UniqueAccount2.Name, Code: item.UniqueAccount2.Code },
            Price: item.Price,
            behalfGroup: item.behalfGroup,
            References: item.References,
            behalf: item.behalf,
            uniqAccountType: item.uniqAccountType,
            BehalfList: item.BehalfList,
            account: item.account
        }
        $scope.model.DirectDebit_TransactionLs.push(DirectDebit_TransactionDetail);
        $scope.updateSumPrice()
    }
    //$scope.changeUniqueAccount = function (uc) {
    //    directDebitSharedProperties.SetActiveUC(uc);
    //    $http.get("/Payment/DirectDebit/getDirectDebitUC").then(function (response) {
    //        modal(response.data, "انتخاب تفصیل", "md.ang");
    //        $('.modal.md.ang').on('hidden.bs.modal', function () {
    //            uc = directDebitSharedProperties.GetActiveUC();
    //        })
    //    });
    //}
    $scope.changeUniqueAccount = function (uc) {
        pettyCashSharedProperties.SetActiveUC(uc);
        $http.get("/Payment/PettyCash/getPettyCashUC").then(function (response) {
            $scope.thisCanBeusedInsideNgBindHtml = $sce.trustAsHtml(response.data)
            $(".modal.md.ang").find(".modal-title").html("انتخاب تفصیل");
            $(".modal.md.ang").modal('show');
            $("#UniqueAccountType, #Code, #Name").val('');
        });
    }
    $scope.setUC = function () {
        $scope.uc = pettyCashSharedProperties.GetActiveUC();
        $scope.uc.Name = $scope.ucname;
        $scope.uc.Code = $scope.ucno;
        $scope.uc.hasUnique_Account_no = true;
        console.log($scope.uc);
        pettyCashSharedProperties.SetActiveUC($scope.uc);
    }
    $scope.changeText = function (DateNow) {
        if ($scope.model.Header.ReceiptDate > DateNow) {
            $scope.hasDateError = true;
        }
        else {
            $scope.hasDateError = false;
        }
    }
    $scope.addDetail = function () {
        var DirectDebit_TransactionDetail = {
            DirectDebitTransactionDetailID: 0,
            DirectDebitTransactionHeaderID: 0,
            PayDate: "",
            BehalfGroupID: null,
            BehalfID: null,
            Description: "",
            Account_no: "",
            UniqueAccount1: { Code: null, Name: null },
            UniqueAccount2: { Code: null, Name: null },
            Price: null,
            behalfGroup: null,
            References: null,
            behalf: null,
            uniqAccountType: null,
            BehalfList: null,
            behalfGroupUniqueAccount: ""
        }
        $scope.model.DirectDebit_TransactionLs.push(DirectDebit_TransactionDetail)
    }
     $scope.getUniqueAccount = function(item){
        return $scope.model.AccountNumLs.filter(function (el) {
            return el.account_code == item;
        })[0]
    }
    $scope.saveDirectDebit = function () {
        $scope.isDisableBtn = true;
        var config = {
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded;charset=utf-8;'
            }
        }
        var model = [];
        var tList = $scope.model.DirectDebit_TransactionLs;
        for (var i = 0; i < tList.length; i++) {
            var references = [];
            for (var j = 0; j < tList[i].References.length; j++) {
                references.push({
                    id: tList[i].References[j].referenceTypeID,
                    value: tList[i].References[j].value
                });
            }
            var debit = {
                date: tList[i].PayDate,
                behalfGroupId: tList[i].behalfGroup.BehalfGroupID,
                behalfId: tList[i].BehalfID,
                uniqueAccount1: (tList[i].UniqueAccount1.Code != null) ? tList[i].UniqueAccount1.Code : null,
                uniqueAccount2: (tList[i].UniqueAccount2.Code != null) ? tList[i].UniqueAccount2.Code : null,
                price: tList[i].Price,
                references: references,
                bankId: $scope.bank.BankID
            }
            model.push(debit);
        }

        $http.post('/Payment/DirectDebit/Save', model, config)
         .success(function (data, status, headers, config) {
             if (data.result)
             {
                 alertify.success("ثبت برداشت مستقیم با موفقیت انجام شد");
                 location.replace("/Payment/DirectDebit/_Index");
             }
             else {
                 alertify.error(data.message);
                 $scope.isDisableBtn = false;
                 //location.replace("/Payment/DirectDebit/_Index");  
             }
           
         })
         .error(function (model, status, header, config) {
             alertify.error("ثبت برداشت مستقیم با خطا مواجه شد");
         });

    }
    $scope.changePTCN = function () {
        $scope.activeHeader = $scope.model.Header.DirectDebitLs[$scope.model.Header.DirectDebitLs.findIndex(x => x.DirectDebitID == $scope.ActivePetyCash.DirectDebitID)];
    }
    $scope.sumKol = 0;
    $scope.updateSumPrice = function () {
        $scope.sumKol = 0;
        var obj = $scope.model.DirectDebit_TransactionLs;
        console.log(obj);
        for (var el in obj) {

            $scope.sumKol += parseFloat(obj[el].Price);
        }
    }
    $scope.beforeRender = function ($view, $dates, $leftDate, $upDate, $rightDate) {
        var index = Math.floor(Math.random() * $dates.length);
        $dates[index].selectable = false;
    }
    $scope.init = function () {
        $scope.getDirectDebit();
        //$scope.HeaderId = id;
    }
    $scope.checkMaxVariz = function (maxprice) {
        if ($scope.variz > maxprice) {
            $scope.variz = maxprice;
            // alertify.error("واریز برداشت مستقیم بیشتر از جمع برداشت مستقیم های تایید شده است.");
        }
    }
    $scope.checkMaxPrice = function (item, maxprice) {
        if (item.Price > maxprice) {
            item.Price = maxprice;
            alertify.error("مبلغ برداشت مستقیم بیشتر از سقف پرداخت میباشد.");
        }
    }
    $scope.addSerialHeader = function () {
        $scope.activeHeader.DirectDebitTransactionHeaderID = $scope.activeHeader.DirectDebitTransactionHeaderID + 1;
    }
    $scope.minusSerialHeader = function () {
        $scope.activeHeader.DirectDebitTransactionHeaderID = $scope.activeHeader.DirectDebitTransactionHeaderID - 1;
    }
    $scope.LoadHeader = function () {
        $scope.getDirectDebit();
    }
    $scope.guid = function () {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
              .toString(16)
              .substring(1);
        }
        return s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4();
    }
})
.service('pettyCashSharedProperties', function () {
    var activeUC = { Code: 0, Name: "" };
    return {
        GetActiveUC: function () {
            return activeUC;
        },
        SetActiveUC: function (uc) {
            activeUC = uc;
        }
    };
}).filter('otherPropertyDetailsFilter', function () {
    return function (items, item, i) {
        var filtered = [];
        angular.forEach(items, function (e) {
            if (item.OtherProperyLS != null && item.OtherProperyLS.indexOf(e.Reciept_OtherProperyID) != -1 && i.Reciept_OtherProperyID == e.Reciept_OtherProperyID) {
                filtered.push(e);
            }
        });
        return filtered;
    };
});
