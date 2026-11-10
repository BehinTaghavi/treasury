app.controller('receiptController', function ($scope, $http, ReceiptSharedProperties) {
    $scope.getReceipt = function (id) {
        $http.get("/Payment/Receipt/Show?HeaderId=" + id).then(function (response) {
            $scope.model = response.data;          
            if (response.data.Header != undefined) {
                $scope.activeHeader = response.data.Header;
            }
            $scope.detailsLs = response.data.detailsLs;
            $scope.AccountNumLs = response.data.AccountNumLs;
            $scope.ProvinceLs = response.data.ProvinceLs;
            $scope.BankLs = response.data.BankLs;
            $scope.BehalfGroupLs = response.data.BehalfGroupLs;
            
        });

        $scope.changeBehalfGroupevent = function (item) {
            $http.get("/Payment/Receipt/GetBehalfLs?BehalfGroupId=" + item.behalfGroup.BehalfGroupID).then(function (response) {
                item.BehalfList = response.data.BehalfList;
            });
        }

        $scope.deleteItem = function (item) {
            var index = $scope.model.detailsLs.indexOf(item);
            if (index > -1) {
                $scope.model.detailsLs.splice(index, 1);
            }
        }
        //feilds are not compelete
        $scope.copyItem = function (item) {
            var Receipt_Detail = {
                Cheque_Number: item.Cheque_Number,
                DueDate: item.DueDate,
              
            }
            $scope.model.detailsLs.push(Receipt_Detail);
           
        }
        $scope.changeUniqueAccount = function (uc) {
            ReceiptSharedProperties.SetActiveUC(uc);
            $http.get("/Payment/Receipt/getReceiptCashUC").then(function (response) {
                modal(response.data, "انتخاب تفصیل", "md.ang");
                $('.modal.md.ang').on('hidden.bs.modal', function () {
                    uc = ReceiptSharedProperties.GetActiveUC();
                })
            });
        }
        //feilds are not complete
        $scope.addDetail = function () {
            var PettyCash_TransactionDetail = {
                Cheque_Number: 0,
                DueDate: "",
                BehalfGroupId: 0,
                BehalfID: 0,
                Unique_Account_No: "",
                UniqueAccount: { Code: 0, Name: "" },
              
            }
            $scope.model.PettyCash_TransactionLs.push(PettyCash_TransactionDetail)
        }
        //feilds are not compelete
        $scope.savePettyCash = function () {
            var details = [];
            var ReceiveDetail = $scope.model.detailsLs;
            for (var i = 0; i < ReceiveDetail.length; i++) {
                var a = {
                    PayDate: ReceiveDetail[i].PayDate,
                    account_code: ReceiveDetail[i].uniqAccountType.account_code,
                    BehalfGroupID: ReceiveDetail[i].behalfGroup.BehalfGroupID,
                    BehalfID: transactionDetail[i].BehalfID.BehalfID,
                  
                }
                details.push(a);
            }
            var model = {
                header: {
                    UniqueAccount2: $scope.UniqueAccount2.Id,
                    PettyCashID: $scope.ActivePetyCash.PettyCashID,
                    PettyCashTransactionDate: $scope.model.PettyCashTransactionDate
                },
                details: details
            }

            var config = {
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded;charset=utf-8;'
                }

            }



            $http.post('/Payment/receipt/Create', model, config)
             .success(function (data, status, headers, config) {
                 alertify.success("ثبت تنخواه با موفقیت انجام شد");

             })
             .error(function (model, status, header, config) {
                 alertify.error("ثبت تنخواه با خطا مواجه شد");
             });

        }
       
    }
});