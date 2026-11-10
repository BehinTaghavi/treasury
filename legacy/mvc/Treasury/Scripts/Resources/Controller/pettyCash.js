app.controller('pettyCashController', function ($scope, $sce, $http, pettyCashSharedProperties) {
    // $scope.activeHeader.Settle_Limit = 0;
    $scope.ucno = "";
    $scope.ucname = "";
    $scope.deletedItem = [];
    $scope.DatePattern = /^[1-4]\d{3}\/((0[1-6]\/((3[0-1])|([1-2][0-9])|(0[1-9])))|((1[0-2]|(0[7-9]))\/(30|([1-2][0-9])|(0[1-9]))))$/;
    $scope.isDisable = false;
    $scope.getPettyCash = function (id) {
        $http.get("/Payment/PettyCash/Show?HeaderId=" + id).then(function (response) {
            $scope.model = response.data;
            $scope.model.ActivePetyCashId = 0;
            if (response.data.Header != undefined) {
                $scope.activeHeader = response.data.Header;
                $scope.model.ActivePetyCashId = response.data.Header.PettyCashID;
            }
            //else {
            //    $scope.activeHeader = response.data.Header;
            //}
            $scope.behalfGroupList = response.data.BehalfGroupList;
            $scope.accountNumLs = response.data.AccountNumLs;
            $scope.updateSumPrice();

        });
    }
    $scope.changeBehalfGroupevent = function (item) {
        $http.get("/Payment/PettyCash/ChangeBehalfGroup?BehalfGroupId=" + item.BehalfGroupID).then(function (response) {
            item.Refrences = response.data.Refrences;
            item.BehalfList = response.data.BehalfList;
        });
    }

    $scope.deleteItem = function (item) {
        if (confirm("آیا از حذف تنخواه اطمینان دارید؟")) {
            if (!item.newItem) {
                $scope.deletedItem.push(item.PettyCashTransactionDetailID);
            }
            var index = $scope.model.PettyCash_TransactionLs.indexOf(item);
            if (index > -1) {
                $scope.model.PettyCash_TransactionLs.splice(index, 1);
            }
            $scope.updateSumPrice();
            if (item.PettyCashTransactionDetailID != null) {               
                $scope.activeHeader.Mande = $scope.activeHeader.Mande  + item.Price;
            }
            $scope.calcSumMablagh(item);

        }
    }
    $scope.copyItem = function (item) {
        var copy = angular.copy(item);
        copy.IsAccept = false;
        copy.IsVakhahi = false;
        copy.newItem = true;
        copy.PettyCashTransactionDetailID = null;
        $scope.model.PettyCash_TransactionLs.push(copy);
        $scope.updateSumPrice();
        $scope.calcSumMablagh(copy);
    }
    $scope.guid = function () {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }
        return s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4();
    }
    $scope.addDetail = function () {
        var PettyCash_TransactionDetail = {
            PettyCashTransactionDetailID: null,
            PettyCashTransactionHeaderID: null,
            PayDate: "",
            BehalfGroupID: null,
            BehalfID: null,
            Description: "",
            Account_no: "",
            UniqueAccount1: { Code: null, Name: null },
            UniqueAccount2: { Code: null, Name: null },
            Price: null,
            IsAccept: false,
            IsVakhahi: false,
            EllateVakhahi: "",
            IsAcceptVakhahi: false,
            AcceptVakhahiDate: false,
            behalfGroup: null,
            Refrences: null,
            behalf: null,
            uniqAccountType: null,
            BehalfList: null,
            newItem: true,
        }
        $scope.model.PettyCash_TransactionLs.push(PettyCash_TransactionDetail);
    }


    $scope.savePettyCash = function () {
        $scope.isDisable = true;
        var details = [];
        var transactionDetail = $scope.model.PettyCash_TransactionLs;
        for (var i = 0; i < transactionDetail.length; i++) {
            var a = {
                PayDate: transactionDetail[i].PayDate,
                Account_no: transactionDetail[i].uniqAccountType,
                PettyCashTransactionDetailID: transactionDetail[i].PettyCashTransactionDetailID,
                BehalfGroupID: $scope.model.BehalfGroupID,
                //Refrences: transactionDetail[i].Refrences,
                //BehalfID: transactionDetail[i].BehalfID,
                BehalfID: $scope.model.BehalfId,
                UniqueAccount1: transactionDetail[i].UniqueAccount1.Code,
                UniqueAccount2: transactionDetail[i].UniqueAccount2.Code,
                Price: transactionDetail[i].Price,
                IsAccept: transactionDetail[i].IsAccept,
                IsVakhahi: transactionDetail[i].IsVakhahi,
                EllateVakhahi: transactionDetail[i].EllateVakhahi,
                Description: transactionDetail[i].Description,
                IsAcceptVakhahi: transactionDetail[i].IsAcceptVakhahi,
                AcceptVakhahiDate: transactionDetail[i].AcceptVakhahiDate,
                newItem: transactionDetail[i].newItem
            }
            details.push(a);
        }
        var model = {
            header: {
                UniqueAccount2: $scope.UniqueAccount2.Code,
                PettyCashID: $scope.model.ActivePetyCashId,
                PettyCashTransactionDate: $scope.model.PettyCashTransactionDate,
                Remained: ($scope.activeHeader.PettyCash_Ammount - $scope.sumIsAccept),
                AddRequestPayment: false,
                SumPrice: $scope.sumIsAccept,
                id: $scope.HeaderId
            },
            details: details,
            deletedItem: $scope.deletedItem
        }
        var config = {
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded;charset=utf-8;'
            }

        }
        if ($scope.sumIsAccept > 0 && $scope.model.AcceptPettyCash) {
            if (confirm('ثبت تنخواه انجام شود؟')) {
                //   if (confirm('آیا برای پرداخت های ثبت و تایید شده درخواست وجه صادر گردد؟')) {
                //       model.header.AddRequestPayment = true;
                //       $http.post('/Payment/PettyCash/Save', model, config)

                //.success(function (data, status, headers, config) {
                //    alert("ثبت تنخواه با موفقیت انجام شد");
                //     location.replace('/Payment/PettyCash/_Index')
                //    //location.replace("/Payment/PettyCash?headerId=" + data);
                //})
                //.error(function (model, status, header, config) {
                //    alertify.error("ثبت تنخواه با خطا مواجه شد");
                //    $scope.isDisable = false;
                //});
                //   }
                //   else {
                model.header.SumPrice = 0;
                $http.post('/Payment/PettyCash/Save', model, config)
                    .success(function (data, status, headers, config) {
                        alert("ثبت تنخواه با موفقیت انجام شد");
                        //location.replace("/Payment/PettyCash/_Index");
                        location.replace("/Payment/PettyCash?headerId=" + data);

                    })
                    .error(function (model, status, header, config) {
                        alertify.error("ثبت تنخواه با خطا مواجه شد");
                        $scope.isDisable = false;
                    });
                //}
            }
        }
        else {
            if (confirm('ثبت تنخوا ها انجام شود؟')) {
                $http.post('/Payment/PettyCash/Save', model, config)
                    .success(function (data, status, headers, config) {
                        alertify.success("ثبت تنخواه با موفقیت انجام شد");
                        //location.replace("/Payment/PettyCash/_Index");
                        location.replace("/Payment/PettyCash?headerId=" + data);

                    })
                    .error(function (model, status, header, config) {
                        alertify.error("ثبت تنخواه با خطا مواجه شد");
                        $scope.isDisable = false;
                    });
            }
        }
        $scope.isDisable = false;
    }

    $scope.ChangeVakhahiHeader = function () {

        $http.get('/Payment/PettyCash/ChangeVakhahiHeader?headerId=' + $scope.HeaderId)
            .success(function (data, status, headers, config) {
                if (data == -1) {
                    alert(data + "واخواهی با موفقیت انجام شد");
                }
                else {
                    alert(data + "موردی یافت نشد");
                }

                location.replace("/Payment/PettyCash?headerId=" + $scope.HeaderId);

            })
            .error(function (model, status, header, config) {
                alertify.error("اضافه کردن واخواهی با خطا مواجه شد");
                $scope.isDisable = false;
            });
    }
    $scope.changePTCN = function (ActivePetyCashId) {
        $scope.activeHeader = $scope.model.Header.PettyCashLs[$scope.model.Header.PettyCashLs.findIndex(x => x.PettyCashID == ActivePetyCashId)];

        $scope.InvisibleMande = $scope.activeHeader.Mande;
        if ($scope.HeaderId == 0) {
            $scope.model.PettyCash_TransactionLs = [];
            $scope.addDetail();
        }
    }
    $scope.sumIsAccept = 0;
    $scope.sumKol = 0;
    $scope.sumIsVakhahi = 0;
    $scope.SumMablagh = 0;

    $scope.calcSumMablagh = function (item) {
        $scope.sumIsAccept = 0;
        $scope.sumIsVakhahi = 0;
        $scope.SumMablagh = 0;
        var obj = $scope.model.PettyCash_TransactionLs;
     
        for (var el in obj) {     
            //console.log(item.PettyCashTransactionDetailID +"; obj[el]" + obj[el].PettyCashTransactionDetailID)
            if (obj[el].newItem) {
                console.log("new");
                $scope.SumMablagh += parseFloat(obj[el].Price);
            }
            else if (item.PettyCashTransactionDetailID == obj[el].PettyCashTransactionDetailID) {
                console.log("edit" + obj[el].PettyCashTransactionDetailID);
                $scope.SumMablagh += parseFloat(obj[el].Price);
            }
            else if (item.PettyCashTransactionDetailID == obj[el].PettyCashTransactionDetailID) {
                $scope.SumMablagh += parseFloat(obj[el].Price);               
                console.log("deleted item" + obj[el].PettyCashTransactionDetailID);
            }
            
        }
    
        console.log("sum"+$scope.SumMablagh);
        if ($scope.sumIsAccept > $scope.activeHeader.Settle_Limit) {
            alertify.warning("جمع تنخواه ها بیشتر از حد واریز میباشد.");
        }
    }
    $scope.updateSumPrice = function () {
        $scope.sumIsAccept = 0;
        $scope.sumIsVakhahi = 0;
        $scope.sumKol = 0;
        var obj = $scope.model.PettyCash_TransactionLs;
        for (var el in obj) {
            if (obj[el].IsAccept) {
                $scope.sumIsAccept += parseFloat(obj[el].Price);
                $scope.sumKol += parseFloat(obj[el].Price);
            }
            else if (obj[el].IsVakhahi) {
                $scope.sumIsVakhahi += parseFloat(obj[el].Price);
                $scope.sumKol += parseFloat(obj[el].Price);
            }
            else {
                $scope.sumKol += parseFloat(obj[el].Price);
            }


        }
        if ($scope.sumIsAccept > $scope.activeHeader.Settle_Limit) {
            alertify.warning("جمع تنخواه ها بیشتر از حد واریز میباشد.");
        }
    }
    $scope.enterEllatVakhahi = function (item) {
        var ellat = alertify.prompt("علت واخواهی", (item.EllateVakhahi != null) ? item.EllateVakhahi : "", function (evt, value) {
            item.EllateVakhahi = value;
            $scope.$apply();
        },
            function () {
                //item.IsVakhahi = false
            });
    }
    $scope.enterDescription = function (item) {
        var ellat = alertify.prompt("توضیحات (حداکثر 250 کاراکتر)", (item.Description != null) ? item.Description : "", function (evt, value) {
            item.Description = value;
            $scope.$apply();
        },
            function () {
                //item.IsVakhahi = false
            });
    }
    $scope.beforeRender = function ($view, $dates, $leftDate, $upDate, $rightDate) {
        var index = Math.floor(Math.random() * $dates.length);
        $dates[index].selectable = false;
    }
    $scope.init = function (id) {
        $scope.getPettyCash(id);
        $scope.HeaderId = id;
    }
    $scope.UniqueAccount2 = {
        Name: null,
        Code: null
    }
    $scope.checkMaxVariz = function (maxprice) {
        if ($scope.variz > maxprice) {
            $scope.variz = maxprice;
            // alertify.error("واریز تنخواه بیشتر از جمع تنخواه های تایید شده است.");
        }
    }
    $scope.checkMaxPrice = function (item, maxprice) {
        if (item.Price > maxprice) {
            item.Price = 0;
            alertify.error("مبلغ تنخواه بیشتر از سقف پرداخت میباشد.");
        }
    }
    $scope.addSerialHeader = function () {
        $scope.activeHeader.PettyCashTransactionHeaderID = $scope.activeHeader.PettyCashTransactionHeaderID + 1;
    }
    $scope.minusSerialHeader = function () {
        $scope.activeHeader.PettyCashTransactionHeaderID = $scope.activeHeader.PettyCashTransactionHeaderID - 1;
    }
    $scope.LoadHeader = function () {
        $scope.getPettyCash($scope.activeHeader.PettyCashTransactionHeaderID);
    }





    $scope.today = function () {
        $scope.dt = new Date();
    };
    $scope.today();
    $scope.clear = function () {
        $scope.dt = null;
    };
    $scope.disabled = function (date, mode) {
        return (mode === 'day' && date.getDay() === 5);
    };
    $scope.toggleMin = function () {
        $scope.minDate = $scope.minDate ? null : new Date();
    };
    $scope.toggleMin();
    $scope.openPersian = function ($event, item) {
        $event.preventDefault();
        $event.stopPropagation();
        item.DeuDateOpen = true;
    };
    $scope.dateOptions = {
        formatYear: 'yy',
        startingDay: 6
    };
    $scope.initDate = new Date('2016-15-20');
    $scope.formats = ['dd-MMMM-yyyy', 'yyyy/MM/dd', 'yyyy/MMMM/dd', 'dd.MM.yyyy', 'shortDate'];
    $scope.format = $scope.formats[2];


    $scope.getUniqueAccount = function (item) {
        return $scope.model.AccountNumLs.filter(function (el) {
            return el.account_code == item.uniqAccountType;
        })[0]
    }
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
        pettyCashSharedProperties.SetActiveUC($scope.uc);
    }
    $scope.select2Options = {
        dir: "rtl",
        containerCssClass: "form-control",
    };

})
    .filter('vakhahiFilter', function () {
        return function (x) {
            var vakhahiList = [];
            for (i = 0; i < x.length; i++) {
                if (x[i].IsVakhahi) {
                    vakhahiList.push(x);
                }
            }
            return vakhahiList;
        };
    })
    .service('pettyCashSharedProperties', function () {
        var activeUC = { Code: null, Name: null };
        return {
            GetActiveUC: function () {
                return activeUC;
            },
            SetActiveUC: function (uc) {
                activeUC = uc;
            }
        };
    })
